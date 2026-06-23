using FiapX.Domain.ProcessingJobs;

namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record ListProcessingJobsRequest
{
    public ProcessingStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int Size { get; init; } = 20;
}
