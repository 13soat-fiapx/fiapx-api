using FiapX.Application.Abstractions.Auth;
using FiapX.Application.Abstractions.Messaging;
using FiapX.Application.Abstractions.Storage;
using FiapX.Application.ProcessingJobs.Messages;
using FiapX.Application.ProcessingJobs.Repositories;
using FiapX.Application.ProcessingJobs.Requests;
using FiapX.Application.ProcessingJobs.Services;
using FiapX.Domain.Base.Exceptions;
using FiapX.Domain.ProcessingJobs;
using FiapX.Domain.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FiapX.Tests.Unit.ProcessingJobs;

[TestClass]
public sealed class ProcessingJobAppServiceTests
{
    private const string UserId = "auth0|user-1";
    private const string UserName = "User One";
    private const string UserEmail = "user.one@example.com";

    private Mock<IProcessingJobRepository> _processingJobRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<IMessagePublisher> _messagePublisherMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUserProfileService> _userProfileServiceMock = null!;
    private ProcessingJobAppService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _processingJobRepositoryMock = new Mock<IProcessingJobRepository>(MockBehavior.Strict);
        _storageServiceMock = new Mock<IStorageService>(MockBehavior.Strict);
        _messagePublisherMock = new Mock<IMessagePublisher>(MockBehavior.Strict);
        _currentUserServiceMock = new Mock<ICurrentUserService>(MockBehavior.Strict);
        _userProfileServiceMock = new Mock<IUserProfileService>(MockBehavior.Strict);

        _currentUserServiceMock.SetupGet(currentUser => currentUser.UserId).Returns(UserId);

        _service = new ProcessingJobAppService(
            _processingJobRepositoryMock.Object,
            _storageServiceMock.Object,
            _messagePublisherMock.Object,
            _currentUserServiceMock.Object,
            _userProfileServiceMock.Object,
            NullLogger<ProcessingJobAppService>.Instance);
    }

    [TestMethod]
    public async Task CreateAsync_ShouldCreateProcessingJob_WhenIdempotencyKeyIsNew()
    {
        var request = BuildCreateRequest();
        ProcessingJob? savedProcessingJob = null;

        _processingJobRepositoryMock
            .Setup(repository => repository.GetByIdempotencyKeyAsync(UserId, "idempotency-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProcessingJob?)null);

        _userProfileServiceMock
            .Setup(service => service.GetCurrentUserAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile(UserId, UserName, UserEmail));

        _storageServiceMock
            .Setup(storage => storage.CreatePresignedUploadAsync(
                It.IsAny<Guid>(),
                request.InputFile.OriginalFileName,
                request.InputFile.ContentType,
                It.IsAny<CancellationToken>()))
            .Returns((Guid processingJobId, string originalFileName, string contentType, CancellationToken _) =>
                Task.FromResult(BuildUploadTarget(processingJobId, originalFileName, contentType)));

        _processingJobRepositoryMock
            .Setup(repository => repository.SaveAsync(It.IsAny<ProcessingJob>(), It.IsAny<CancellationToken>()))
            .Callback<ProcessingJob, CancellationToken>((processingJob, _) => savedProcessingJob = processingJob)
            .Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request, " idempotency-1 ", CancellationToken.None);

        Assert.IsNotNull(savedProcessingJob);
        Assert.AreEqual(savedProcessingJob.Id, result.ProcessingJob.Id);
        Assert.AreEqual(UserName, savedProcessingJob.UserName);
        Assert.AreEqual(UserEmail, savedProcessingJob.UserEmail);
        Assert.AreEqual("idempotency-1", savedProcessingJob.IdempotencyKey);
        Assert.AreEqual(ProcessingStatus.UploadPending, savedProcessingJob.Status);
        Assert.AreEqual(request.InputFile.OriginalFileName, result.Upload.S3Object.Key.Split('/').Last());

        _processingJobRepositoryMock.Verify(
            repository => repository.SaveAsync(It.IsAny<ProcessingJob>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [TestMethod]
    public async Task CreateAsync_ShouldReplayExistingUpload_WhenIdempotencyKeyAndRequestMatch()
    {
        var request = BuildCreateRequest();
        var processingJobId = Guid.NewGuid();
        var existingProcessingJob = BuildProcessingJob(processingJobId, request, "idempotency-1");

        _processingJobRepositoryMock
            .Setup(repository => repository.GetByIdempotencyKeyAsync(UserId, "idempotency-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProcessingJob);

        _storageServiceMock
            .Setup(storage => storage.CreatePresignedUploadAsync(
                processingJobId,
                request.InputFile.OriginalFileName,
                request.InputFile.ContentType,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUploadTarget(
                processingJobId,
                request.InputFile.OriginalFileName,
                request.InputFile.ContentType));

        var result = await _service.CreateAsync(request, "idempotency-1", CancellationToken.None);

        Assert.AreEqual(processingJobId, result.ProcessingJob.Id);
        Assert.AreEqual("idempotency-1", result.ProcessingJob.IdempotencyKey);
        Assert.AreEqual(ProcessingStatus.UploadPending, result.ProcessingJob.Status);

        _processingJobRepositoryMock.Verify(
            repository => repository.SaveAsync(It.IsAny<ProcessingJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CreateAsync_ShouldThrowConflict_WhenIdempotencyKeyHasDifferentRequest()
    {
        var originalRequest = BuildCreateRequest(clientReference: "client-a");
        var replayRequest = BuildCreateRequest(clientReference: "client-b");
        var existingProcessingJob = BuildProcessingJob(Guid.NewGuid(), originalRequest, "idempotency-1");

        _processingJobRepositoryMock
            .Setup(repository => repository.GetByIdempotencyKeyAsync(UserId, "idempotency-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProcessingJob);

        await Assert.ThrowsExactlyAsync<ConflictException>(
            () => _service.CreateAsync(replayRequest, "idempotency-1", CancellationToken.None));

        _storageServiceMock.Verify(
            storage => storage.CreatePresignedUploadAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CompleteUploadAsync_ShouldMarkProcessingJobAsFailed_WhenPublishFails()
    {
        var request = BuildCreateRequest();
        var processingJobId = Guid.NewGuid();
        var existingProcessingJob = BuildProcessingJob(processingJobId, request);
        var savedStatuses = new List<ProcessingStatus>();

        _processingJobRepositoryMock
            .Setup(repository => repository.GetByIdAsync(processingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProcessingJob);

        _storageServiceMock
            .Setup(storage => storage.GetObjectMetadataAsync(
                existingProcessingJob.InputFile.S3Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata
            {
                SizeBytes = request.InputFile.SizeBytes,
                Checksum = request.InputFile.Checksum
            });

        _processingJobRepositoryMock
            .Setup(repository => repository.SaveAsync(existingProcessingJob, It.IsAny<CancellationToken>()))
            .Callback<ProcessingJob, CancellationToken>((processingJob, _) => savedStatuses.Add(processingJob.Status))
            .Returns(Task.CompletedTask);

        _messagePublisherMock
            .Setup(publisher => publisher.PublishAsync(
                It.IsAny<VideoProcessingRequestedMessage>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SQS publish failed."));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.CompleteUploadAsync(
                processingJobId,
                new CompleteProcessingJobUploadRequest
                {
                    SizeBytes = request.InputFile.SizeBytes,
                    Checksum = request.InputFile.Checksum
                },
                CancellationToken.None));

        CollectionAssert.AreEqual(
            new[] { ProcessingStatus.Queued, ProcessingStatus.Failed },
            savedStatuses);
        Assert.AreEqual(ProcessingStatus.Failed, existingProcessingJob.Status);
    }

    [TestMethod]
    public async Task CompleteUploadAsync_ShouldReplayQueuedProcessingJob_WhenIdempotencyKeyIsProvided()
    {
        var request = BuildCreateRequest();
        var processingJobId = Guid.NewGuid();
        var existingProcessingJob = BuildProcessingJob(processingJobId, request);
        existingProcessingJob.ConfirmUpload();

        _processingJobRepositoryMock
            .Setup(repository => repository.GetByIdAsync(processingJobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProcessingJob);

        _storageServiceMock
            .Setup(storage => storage.GetObjectMetadataAsync(
                existingProcessingJob.InputFile.S3Object,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new S3ObjectMetadata
            {
                SizeBytes = request.InputFile.SizeBytes,
                Checksum = request.InputFile.Checksum
            });

        var result = await _service.CompleteUploadAsync(
            processingJobId,
            new CompleteProcessingJobUploadRequest
            {
                SizeBytes = request.InputFile.SizeBytes,
                Checksum = request.InputFile.Checksum
            },
            "completion-key-1",
            CancellationToken.None);

        Assert.AreEqual(ProcessingStatus.Queued, result.Status);
        _processingJobRepositoryMock.Verify(
            repository => repository.SaveAsync(It.IsAny<ProcessingJob>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _messagePublisherMock.Verify(
            publisher => publisher.PublishAsync(It.IsAny<VideoProcessingRequestedMessage>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static CreateProcessingJobRequest BuildCreateRequest(string clientReference = "client-123")
    {
        return new CreateProcessingJobRequest
        {
            Description = "Video processing",
            Author = "FIAP X",
            ClientReference = clientReference,
            InputFile = new RequestedFileRequest
            {
                OriginalFileName = "video.mp4",
                ContentType = "video/mp4",
                SizeBytes = 1024,
                Checksum = "abc123"
            }
        };
    }

    private static ProcessingJob BuildProcessingJob(
        Guid processingJobId,
        CreateProcessingJobRequest request,
        string? idempotencyKey = null)
    {
        return ProcessingJob.Create(
            processingJobId,
            UserId,
            UserName,
            UserEmail,
            BuildInputFile(processingJobId, request),
            idempotencyKey,
            request.Description,
            request.Author,
            request.ClientReference);
    }

    private static ProcessingInputFile BuildInputFile(
        Guid processingJobId,
        CreateProcessingJobRequest request)
    {
        return new ProcessingInputFile(
            new S3ObjectReference("fiapx-input", $"uploads/{processingJobId}/{request.InputFile.OriginalFileName}"),
            request.InputFile.OriginalFileName,
            request.InputFile.ContentType,
            request.InputFile.SizeBytes,
            request.InputFile.Checksum);
    }

    private static PresignedUploadTarget BuildUploadTarget(
        Guid processingJobId,
        string originalFileName,
        string contentType)
    {
        return new PresignedUploadTarget
        {
            Method = "PUT",
            Url = $"https://storage.local/{processingJobId}/{originalFileName}",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = contentType
            },
            S3Object = new S3ObjectReference("fiapx-input", $"uploads/{processingJobId}/{originalFileName}")
        };
    }
}
