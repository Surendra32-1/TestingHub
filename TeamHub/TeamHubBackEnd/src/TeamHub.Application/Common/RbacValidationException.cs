namespace TeamHub.Application.Common;

public sealed class RbacValidationException(string message) : Exception(message);
