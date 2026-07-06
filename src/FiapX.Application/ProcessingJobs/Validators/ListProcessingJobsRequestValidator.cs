using FiapX.Application.ProcessingJobs.Requests;
using FluentValidation;

namespace FiapX.Application.ProcessingJobs.Validators;

public sealed class ListProcessingJobsRequestValidator : AbstractValidator<ListProcessingJobsRequest>
{
    public ListProcessingJobsRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than zero.");

        RuleFor(request => request.Size)
            .InclusiveBetween(1, 100)
            .WithMessage("Size must be between 1 and 100.");
    }
}
