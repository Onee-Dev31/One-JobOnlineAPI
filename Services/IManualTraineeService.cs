using JobOnlineAPI.Models;

namespace JobOnlineAPI.Services;

public interface IManualTraineeService
{
    Task<CreateManualTraineeResult> CreateAsync(
        ManualTraineeRequest request,
        IReadOnlyDictionary<string, IReadOnlyList<IFormFile>> files,
        int? adminId,
        CancellationToken cancellationToken);
}

public sealed class ManualTraineeConflictException(string message) : Exception(message);
