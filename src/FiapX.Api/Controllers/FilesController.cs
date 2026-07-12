using FiapX.Application.ProcessingJobs.Responses;
using FiapX.Application.ProcessingJobs.Services;
using Microsoft.AspNetCore.Mvc;

namespace FiapX.Api.Controllers;

/// <summary>
///     Exposes metadata and download redirects for generated processing result files.
/// </summary>
[ApiController]
[Produces("application/json")]
[Route("v1/files")]
public sealed class FilesController(ProcessingJobAppService processingJobAppService) : ControllerBase
{
    /// <summary>
    ///     Returns metadata for a generated result file.
    /// </summary>
    /// <response code="200">Result file metadata returned.</response>
    /// <response code="404">Result file not found.</response>
    [HttpGet("{fileId:guid}")]
    [ProducesResponseType(typeof(FileResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMetadataAsync(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await processingJobAppService.GetFileMetadataAsync(fileId, cancellationToken);
        var response = ProcessingJobResponseMapper.ToFileResultResponse(result, Request.PathBase.Value);
        Response.Headers.CacheControl = "private, max-age=60";

        return Ok(response);
    }

    /// <summary>
    ///     Generates a presigned URL for downloading the generated result file.
    /// </summary>
    /// <response code="200">A presigned URL for downloading the generated result file.</response>
    /// <response code="404">Result file not found.</response>
    [HttpGet("{fileId:guid}/content")]
    [ProducesResponseType(typeof(FileDownloadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadAsync(
        [FromRoute] Guid fileId,
        CancellationToken cancellationToken)
    {
        var result = await processingJobAppService.GetFileDownloadAsync(fileId, cancellationToken);
        var response = ProcessingJobResponseMapper.ToFileDownloadResponse(result);

        return Ok(response);
    }
}
