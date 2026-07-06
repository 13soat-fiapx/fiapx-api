using System.ComponentModel.DataAnnotations;

namespace FiapX.Application.ProcessingJobs.Requests;

public sealed record ListProcessingJobsQuery
{
    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int Size { get; init; } = 20;
}
