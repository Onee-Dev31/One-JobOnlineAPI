namespace JobOnlineAPI.Services
{
    public interface IClamAVService
    {
        Task<bool> IsSafeAsync(Stream fileStream);
    }
}
