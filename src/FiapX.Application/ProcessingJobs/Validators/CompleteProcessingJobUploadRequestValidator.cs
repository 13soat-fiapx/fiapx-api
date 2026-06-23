using FiapX.Application.ProcessingJobs.Requests;
using FluentValidation;

namespace FiapX.Application.ProcessingJobs.Validators;

public sealed class CompleteProcessingJobUploadRequestValidator : AbstractValidator<CompleteProcessingJobUploadRequest>
{
    public CompleteProcessingJobUploadRequestValidator()
    {
        RuleFor(request => request.SizeBytes)
            .GreaterThan(0)
            .When(request => request.SizeBytes.HasValue)
            .WithMessage("SizeBytes must be greater than zero.");

        RuleFor(request => request.Checksum)
            .MaximumLength(128)
            .WithMessage("Checksum cannot exceed 128 characters.");
    }
}
