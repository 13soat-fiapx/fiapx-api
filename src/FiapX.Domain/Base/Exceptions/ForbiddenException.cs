namespace FiapX.Domain.Base.Exceptions;

public sealed class ForbiddenException : BusinessException
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
