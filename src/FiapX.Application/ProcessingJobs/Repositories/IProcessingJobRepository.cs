using FiapX.Domain.ProcessingJobs;

namespace FiapX.Application.ProcessingJobs.Repositories;

public interface IProcessingJobRepository
{
    Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ProcessingJob?> GetByIdempotencyKeyAsync(
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ProcessingJob?> GetByResultFileIdAsync(Guid fileId, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProcessingJob>> ListByUserAsync(
        string userId,
        ProcessingStatus? status,
        int page,
        int size,
        CancellationToken cancellationToken);

    Task<int> CountByUserAsync(
        string userId,
        ProcessingStatus? status,
        CancellationToken cancellationToken);

    Task SaveAsync(ProcessingJob processingJob, CancellationToken cancellationToken);
}
