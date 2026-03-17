using nClam;

namespace JobOnlineAPI.Services
{
    public class ClamAVService(IConfiguration configuration, ILogger<ClamAVService> logger) : IClamAVService
    {
        private readonly string _host = configuration["ClamAV:Host"] ?? "localhost";
        private readonly int _port = int.Parse(configuration["ClamAV:Port"] ?? "3310");
        private readonly ILogger<ClamAVService> _logger = logger;

        public async Task<bool> IsSafeAsync(Stream fileStream)
        {
            try
            {
                var clam = new ClamClient(_host, _port);
                var result = await clam.SendAndScanFileAsync(fileStream);
                if (result.Result == ClamScanResults.VirusDetected)
                {
                    _logger.LogWarning("ClamAV: virus detected — {VirusName}", result.InfectedFiles?.FirstOrDefault()?.VirusName);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ClamAV: scan failed, allowing file through");
                return true; // fail-open: don't block upload if ClamAV is down
            }
        }
    }
}
