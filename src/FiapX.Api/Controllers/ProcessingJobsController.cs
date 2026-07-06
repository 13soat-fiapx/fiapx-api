using FiapX.Application.ProcessingJobs.Requests;
using FiapX.Application.ProcessingJobs.Responses;
using FiapX.Application.ProcessingJobs.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FiapX.Api.Controllers;

/// <summary>
///     Manages asynchronous video processing jobs for the authenticated user.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("v1/processing-jobs")]
public sealed class ProcessingJobsController(ProcessingJobAppService processingJobAppService) : ControllerBase
{
    /// <summary>
    ///     Lists processing jobs for the authenticated user.
    /// </summary>
    /// <response code="200">Processing jobs returned.</response>
    /// <response code="401">Authentication is required.</response>
    /// <response code="403">The authenticated user cannot access the resource.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProcessingJobStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ListAsync(
        [FromQuery] ListProcessingJobsQuery query,
        CancellationToken cancellationToken = default)
    {
        var request = new ListProcessingJobsRequest
        {
            Status = ProcessingStatusContractMapper.ToDomainOrNull(query.Status),
            Page = query.Page,
            Size = query.Size
        };

        var result = await processingJobAppService.ListAsync(request, cancellationToken);
        return Ok(ProcessingJobResponseMapper.ToPagedResponse(result, query.Status, Request.PathBase.Value));
    }

    /// <summary>
    ///     Creates a processing job and returns a presigned upload URL.
    /// </summary>
    /// <response code="201">Processing job created.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="409">The idempotency key was already used with incompatible data.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProcessingJobCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateProcessingJobRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await processingJobAppService.CreateAsync(request, idempotencyKey, cancellationToken);
        var response = ProcessingJobResponseMapper.ToCreatedResponse(result, Request.PathBase.Value);

        return Created(response.Links["self"].Href, response);
    }

    /// <summary>
    ///     Confirms that the upload was completed and queues the processing job.
    /// </summary>
    /// <response code="202">Upload confirmed and processing job queued.</response>
    /// <response code="404">Processing job not found.</response>
    /// <response code="409">Processing job cannot be queued from its current status.</response>
    [HttpPost("{processingJobId:guid}/upload-completion")]
    [ProducesResponseType(typeof(ProcessingJobStatusResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CompleteUploadAsync(
        [FromRoute] Guid processingJobId,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CompleteProcessingJobUploadRequest? request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var processingJob = await processingJobAppService.CompleteUploadAsync(
            processingJobId,
            request ?? new CompleteProcessingJobUploadRequest(),
            idempotencyKey,
            cancellationToken);
        var response = ProcessingJobResponseMapper.ToStatusResponse(processingJob, Request.PathBase.Value);

        Response.Headers.RetryAfter = "5";
        return Accepted(response.Links["self"].Href, response);
    }

    /// <summary>
    ///     Returns the current processing job status.
    /// </summary>
    /// <response code="200">Processing job status returned.</response>
    /// <response code="303">Processing finished; follow the Location header to the result file.</response>
    /// <response code="404">Processing job not found.</response>
    [HttpGet("{processingJobId:guid}")]
    [ProducesResponseType(typeof(ProcessingJobStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status303SeeOther)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetStatusAsync(
        [FromRoute] Guid processingJobId,
        CancellationToken cancellationToken)
    {
        var processingJob = await processingJobAppService.GetStatusAsync(processingJobId, cancellationToken);
        var response = ProcessingJobResponseMapper.ToStatusResponse(processingJob, Request.PathBase.Value);

        if (response.Status == ProcessingStatusContractMapper.Succeeded &&
            response.Links.TryGetValue("result", out var resultLink))
        {
            Response.Headers.Location = resultLink.Href;
            Response.Headers.RetryAfter = "0";
            return StatusCode(StatusCodes.Status303SeeOther);
        }

        Response.Headers.CacheControl = "no-store";
        if (response.Status == ProcessingStatusContractMapper.Queued ||
            response.Status == ProcessingStatusContractMapper.Processing)
            Response.Headers.RetryAfter = "5";

        return Ok(response);
    }
}
