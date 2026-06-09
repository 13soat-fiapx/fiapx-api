namespace FiapX.Domain.ProcessingJobs
{
    public enum ProcessingStatus
    {
        UploadPending = 0,
        Queued = 1,
        Processing = 2,
        Succeeded = 3,
        Failed = 4
    }
}