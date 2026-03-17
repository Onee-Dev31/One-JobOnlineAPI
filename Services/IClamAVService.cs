namespace JobOnlineAPI.Services
{
    public interface IClamAVService
    {
        Task<(bool IsSafe, string? VirusName)> ScanAsync(Stream fileStream, string fileName);
    }
}
