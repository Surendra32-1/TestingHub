namespace TeamHub.Application.Workflows;

public sealed class ReleaseWorkflowException(string message) : Exception(message);
