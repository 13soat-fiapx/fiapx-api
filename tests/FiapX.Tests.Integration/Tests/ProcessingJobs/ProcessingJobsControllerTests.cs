using System.Net;
using System.Net.Http.Json;
using FiapX.Application.ProcessingJobs.Requests;
using FiapX.Application.ProcessingJobs.Responses;
using FiapX.Tests.Integration.Helpers;
using static Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace FiapX.Tests.Integration.Tests.ProcessingJobs;

[TestClass]
[TestCategory("ProcessingJobs")]
[TestCategory("Integration")]
public sealed class ProcessingJobsControllerTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod("Creates a processing job and returns a presigned upload URL")]
    public async Task It_ShouldCreateProcessingJob_WhenRequestIsValid()
    {
        var client = AssemblyInitializer.Factory.GetAuthenticatedClient();
        var request = new CreateProcessingJobRequest
        {
            Description = "Integration test video",
            Author = "FIAP X",
            ClientReference = $"test-{Guid.NewGuid():N}",
            InputFile = new RequestedFileRequest
            {
                OriginalFileName = "sample.mp4",
                ContentType = "video/mp4",
                SizeBytes = 1024,
                Checksum = "abc123"
            }
        };

        var response = await client.PostAsJsonAsync(
            "/api/v1/processing-jobs",
            request,
            TestContext.CancellationTokenSource.Token);

        AreEqual(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadFromJsonAsync<ProcessingJobCreatedResponse>(
            TestContext.CancellationTokenSource.Token);

        IsNotNull(content);
        AreNotEqual(Guid.Empty, content.Id);
        AreEqual("upload_pending", content.Status);
        AreEqual("PUT", content.Upload.Method);
        IsTrue(content.Upload.Url.Contains("sample.mp4") || content.Upload.Url.Contains("original.mp4"));
    }
}
