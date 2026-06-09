namespace FiapX.Domain.ProcessingJobs;

public static class ProcessingMessageSeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";

    public static bool IsValid(string severity) =>
        severity is Info or Warning or Error;
}
