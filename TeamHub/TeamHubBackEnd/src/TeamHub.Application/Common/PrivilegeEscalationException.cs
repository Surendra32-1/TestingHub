namespace TeamHub.Application.Common;

public sealed class PrivilegeEscalationException(string message) : Exception(message);
