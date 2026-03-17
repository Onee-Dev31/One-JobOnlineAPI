using nClam;

namespace JobOnlineAPI.Services
{
    public class ClamAVService(IConfiguration configuration, ILogger<ClamAVService> logger) : IClamAVService
    {
        private readonly string _host = configuration["ClamAV:Host"] ?? "localhost";
        private readonly int _port = int.Parse(configuration["ClamAV:Port"] ?? "3310");
        private readonly ILogger<ClamAVService> _logger = logger;

        public async Task<(bool IsSafe, string? VirusName)> ScanAsync(Stream fileStream, string fileName)
        {
            try
            {
                var clam = new ClamClient(_host, _port);
                var result = await clam.SendAndScanFileAsync(fileStream);
                if (result.Result == ClamScanResults.VirusDetected)
                {
                    var virusName = result.InfectedFiles?.FirstOrDefault()?.VirusName ?? "Unknown";
                    _logger.LogWarning("ClamAV: virus detected in {FileName} — {VirusName}", fileName, virusName);
                    return (false, virusName);
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "ClamAV: unreachable — fail-open for {FileName}", fileName);
                return (true, null); // fail-open: ถ้า ClamAV down ให้ผ่าน
            }
        }
    }
}
