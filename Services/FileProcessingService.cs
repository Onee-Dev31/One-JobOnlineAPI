namespace JobOnlineAPI.Services
{
    public class FileProcessingService(
        INetworkShareService networkShareService,
        ILogger<FileProcessingService> logger)
    {
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly ILogger<FileProcessingService> _logger = logger;
        private const long MaxFileSize = 40 * 1024 * 1024; // 40MB
        private static readonly string[] AllowedExtensions =
        { ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx" };

        private static readonly string[] AllowedMimeTypes =
        {
            "application/pdf",
            "image/png",
            "image/jpeg",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-excel",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation"
        };

        public async Task<List<Dictionary<string, object>>> ProcessFilesAsync(IFormFileCollection files, string sectionFile = "Section1")
        {
            var fileMetadatas = new List<Dictionary<string, object>>();
            if (files == null || files.Count == 0)
                return fileMetadatas;

            // var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".png", ".jpg" };

            foreach (var file in files)
            {
                if (file.Length == 0)
                {
                    _logger.LogWarning("Skipping empty file: {FileName}", file.FileName);
                    continue;
                }

                // var extension = Path.GetExtension(file.FileName).ToLower();
                // if (!allowedExtensions.Contains(extension))
                //     throw new InvalidOperationException($"Invalid file type for {file.FileName}. Only PNG, JPG, PDF, DOC, and DOCX are allowed.");
                if (file.Length > MaxFileSize)
                {
                    _logger.LogWarning(
                        "Rejected file upload: {FileName}, Size: {Size}, Type: {Type}",
                        file.FileName,
                        file.Length,
                        file.ContentType
                    );
                    throw new InvalidOperationException($"File too large: {file.FileName}. Maximum allowed size is 40MB.");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions .Contains(extension))
                    throw new InvalidOperationException($"Invalid file type for {file.FileName}. Only PNG, JPG, JPEG, PDF, DOC, and DOCX are allowed.");

                if (!AllowedMimeTypes.Contains(file.ContentType))
                    throw new InvalidOperationException($"Invalid MIME type for {file.FileName}: {file.ContentType}");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                // var originalName = Path.GetFileName(file.FileName);
                // var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalName)}";
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

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                // var extension = Path.GetExtension(file.FileName).ToLower();
                // if (!allowedExtensions.Contains(extension))
                //     throw new InvalidOperationException($"Invalid file type: {extension}");

                // var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                // var filePath = Path.Combine(tempPath, fileName);
                if (file.Length > MaxFileSize)
                {
                    {
                        _logger.LogWarning(
                            "Rejected file upload: {FileName}, Size: {Size}, Type: {Type}",
                            file.FileName,
                            file.Length,
                            file.ContentType
                        );
                        throw new InvalidOperationException($"File too large: {file.FileName}. Maximum allowed size is 40MB.");
                    }

                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions .Contains(extension))
                    throw new InvalidOperationException($"Invalid file type: {extension}");

                if (!AllowedMimeTypes.Contains(file.ContentType))
                    throw new InvalidOperationException($"Invalid MIME type for {file.FileName}: {file.ContentType}");

                // var originalName = Path.GetFileName(file.FileName);
                // var fileName = $"{Guid.NewGuid()}{Path.GetExtension(originalName)}";
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

        // public void MoveFilesToApplicantDirectory(int applicantId, List<Dictionary<string, object>> fileMetadatas)
        // {
        //     if (fileMetadatas.Count == 0 || applicantId <= 0)
        //         return;

        //     var applicantPath = Path.Combine(_networkShareService.GetBasePath(), $"applicant_{applicantId}");
        //     if (!Directory.Exists(applicantPath))
        //     {
        //         try
        //         {
        //             Directory.CreateDirectory(applicantPath);
        //             _logger.LogInformation("Created applicant directory: {ApplicantPath}", applicantPath);
        //         }
        //         catch (Exception ex)
        //         {
        //             _logger.LogError(ex, "Failed to create applicant directory: {ApplicantPath}", applicantPath);
        //             throw;
        //         }
        //     }
        //     else
        //     {
        //         foreach (var oldFile in Directory.GetFiles(applicantPath))
        //         {
        //             try
        //             {
        //                 System.IO.File.Delete(oldFile);
        //                 _logger.LogInformation("Deleted old file: {OldFile}", oldFile);
        //             }
        //             catch (Exception ex)
        //             {
        //                 _logger.LogWarning(ex, "Failed to delete old file: {OldFile}", oldFile);
        //             }
        //         }
        //     }

        //     foreach (var metadata in fileMetadatas)
        //     {
        //         var oldFilePath = metadata.GetValueOrDefault("FilePath")?.ToString();
        //         var fileName = metadata.GetValueOrDefault("FileName")?.ToString();
        //         if (string.IsNullOrEmpty(oldFilePath) || string.IsNullOrEmpty(fileName))
        //         {
        //             _logger.LogWarning("Skipping file with invalid metadata: {Metadata}", System.Text.Json.JsonSerializer.Serialize(metadata));
        //             continue;
        //         }

        //         var newFilePath = Path.Combine(applicantPath, fileName);
        //         if (System.IO.File.Exists(oldFilePath))
        //         {
        //             try
        //             {
        //                 System.IO.File.Move(oldFilePath, newFilePath, overwrite: true);
        //                 _logger.LogInformation("Moved file from {OldFilePath} to {NewFilePath}", oldFilePath, newFilePath);
        //                 metadata["FilePath"] = newFilePath.Replace('\\', '/');
        //             }
        //             catch (Exception ex)
        //             {
        //                 _logger.LogError(ex, "Failed to move file from {OldFilePath} to {NewFilePath}", oldFilePath, newFilePath);
        //                 throw;
        //             }
        //         }
        //         else
        //         {
        //             _logger.LogWarning("File not found for moving: {OldFilePath}", oldFilePath);
        //         }
        //     }
        // }
    

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