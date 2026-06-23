using FiapX.Application.ProcessingJobs.Requests;
using FluentValidation;

namespace FiapX.Application.ProcessingJobs.Validators;

public sealed class CreateProcessingJobRequestValidator : AbstractValidator<CreateProcessingJobRequest>
{
    public CreateProcessingJobRequestValidator()
    {
        RuleFor(request => request.InputFile)
            .NotNull()
            .WithMessage("InputFile is required.")
            .SetValidator(new RequestedFileRequestValidator()!);

        RuleFor(request => request.Description)
            .MaximumLength(200)
            .WithMessage("Description cannot exceed 200 characters.");

        RuleFor(request => request.Author)
            .MaximumLength(120)
            .WithMessage("Author cannot exceed 120 characters.");

        RuleFor(request => request.ClientReference)
            .MaximumLength(64)
            .WithMessage("ClientReference cannot exceed 64 characters.");
    }
}

public sealed class RequestedFileRequestValidator : AbstractValidator<RequestedFileRequest>
{
    private static readonly string[] AllowedContentTypes =
    [
        "video/mp4",
        "video/mpeg",
        "video/quicktime",
        "video/x-msvideo",
        "video/x-matroska",
        "video/webm"
    ];

    public RequestedFileRequestValidator()
    {
        RuleFor(request => request.OriginalFileName)
            .NotEmpty()
            .WithMessage("OriginalFileName is required.")
            .MaximumLength(255)
            .WithMessage("OriginalFileName cannot exceed 255 characters.");

        RuleFor(request => request.ContentType)
            .NotEmpty()
            .WithMessage("ContentType is required.")
            .Must(IsAllowedVideoContentType)
            .WithMessage("ContentType must be a supported video MIME type.");

        RuleFor(request => request.SizeBytes)
            .GreaterThan(0)
            .WithMessage("SizeBytes must be greater than zero.");

        RuleFor(request => request.Checksum)
            .MaximumLength(128)
            .WithMessage("Checksum cannot exceed 128 characters.");
    }

    private static bool IsAllowedVideoContentType(string contentType)
    {
        return AllowedContentTypes.Contains(contentType?.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
