namespace JobOnlineAPI.Services
{
    public class FileProcessingService(
        INetworkShareService networkShareService,
        ILogger<FileProcessingService> logger)
    {
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly ILogger<FileProcessingService> _logger = logger;

        private static readonly Dictionary<string, byte[][]> _magicBytes = new()
        {
            { ".pdf",  [[(byte)'%', (byte)'P', (byte)'D', (byte)'F']] },
            { ".png",  [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]] },
            { ".jpg",  [[0xFF, 0xD8, 0xFF]] },
            { ".doc",  [[0xD0, 0xCF, 0x11, 0xE0]] },
            { ".docx", [[0x50, 0x4B, 0x03, 0x04]] },
        };

        private static async Task<bool> HasValidMagicBytesAsync(IFormFile file, string extension)
        {
            if (!_magicBytes.TryGetValue(extension, out var signatures))
                return false;

            var maxLen = signatures.Max(s => s.Length);
            var header = new byte[maxLen];
            using var stream = file.OpenReadStream();
            var read = await stream.ReadAsync(header.AsMemory(0, maxLen));

            return signatures.Any(sig => read >= sig.Length && header.Take(sig.Length).SequenceEqual(sig));
        }

        public async Task<List<Dictionary<string, object>>> ProcessFilesAsync(IFormFileCollection files, string sectionFile = "Section1")
        {
            var fileMetadatas = new List<Dictionary<string, object>>();
            if (files == null || files.Count == 0)
                return fileMetadatas;

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg" };
            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                    continue;
                }

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"Invalid file type for {file.FileName}. Only PNG, JPG, PDF, DOC, and DOCX are allowed.");

                if (!await HasValidMagicBytesAsync(file, extension))
                    throw new InvalidOperationException($"File content does not match its extension for {file.FileName}.");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                // var fileName = $"{file.FileName}";
                var filePath = Path.Combine(_networkShareService.GetBasePath(), fileName);
                var directoryPath = Path.GetDirectoryName(filePath) ?? throw new InvalidOperationException($"Invalid directory path for: {filePath}");

                try
                {
                    Directory.CreateDirectory(directoryPath);
                    _logger.LogInformation("Created directory: {DirectoryPath}", directoryPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create directory: {DirectoryPath}", directoryPath);
                    throw;
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                fileMetadatas.Add(new Dictionary<string, object>
                {
                    { "FilePath", filePath.Replace('\\', '/') },
                    { "FileName", fileName },
                    { "FileSize", file.Length },
                    { "FileType", file.ContentType },
                    { "SectionFile", sectionFile }
                });
            }

            return fileMetadatas;
        }
        public async Task<List<Dictionary<string, object>>> ProcessFilesForApplicantAsync(IFormFileCollection files, int applicantId)
        {
            var fileMetadatas = new List<Dictionary<string, object>>();
            if (files == null || files.Count == 0 || applicantId <= 0)
                return fileMetadatas;

            // temp folder (ใช้ applicantId)
            var tempPath = Path.Combine(_networkShareService.GetBasePath(), $"temp_applicant_{applicantId}");
            Directory.CreateDirectory(tempPath);

            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg" };

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    throw new InvalidOperationException($"Invalid file type: {extension}");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(tempPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                fileMetadatas.Add(new Dictionary<string, object>
                {
                    { "FilePath", filePath.Replace('\\','/') },
                    { "FileName", fileName },
                    { "FileSize", file.Length },
                    { "FileType", file.ContentType }
                });
            }

            return fileMetadatas;
        }


        public void MoveFilesToApplicantDirectory(int applicantId, List<Dictionary<string, object>> fileMetadatas)
        {
            if (fileMetadatas.Count == 0 || applicantId <= 0)
                return;

            var applicantPath = Path.Combine(_networkShareService.GetBasePath(), $"applicant_{applicantId}");

            if (!Directory.Exists(applicantPath))
            {
                try
                {
                    Directory.CreateDirectory(applicantPath);
                    _logger.LogInformation("Created applicant directory: {ApplicantPath}", applicantPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create applicant directory: {ApplicantPath}", applicantPath);
                    throw;
                }
            }

            foreach (var metadata in fileMetadatas)
            {
                var oldFilePath = metadata.GetValueOrDefault("FilePath")?.ToString();
                var fileName = metadata.GetValueOrDefault("FileName")?.ToString();

                if (string.IsNullOrEmpty(oldFilePath) || string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning("Skipping file with invalid metadata: {Metadata}", 
                        System.Text.Json.JsonSerializer.Serialize(metadata));
                    continue;
                }

                var newFilePath = Path.Combine(applicantPath, fileName);

                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        if (System.IO.File.Exists(newFilePath))
                        {
                            System.IO.File.Delete(newFilePath);
                            _logger.LogInformation("Deleted duplicate file: {File}", newFilePath);
                        }

                        System.IO.File.Move(oldFilePath, newFilePath);
                        _logger.LogInformation("Moved file from {OldFilePath} to {NewFilePath}", oldFilePath, newFilePath);

                        metadata["FilePath"] = newFilePath.Replace('\\', '/');
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, 
                            "Failed to move file from {OldFilePath} to {NewFilePath}", 
                            oldFilePath, newFilePath);
                        throw;
                    }
                }
                else
                {
                    _logger.LogWarning("File not found for moving: {OldFilePath}", oldFilePath);
                }
            }
        }
    }
}