namespace FiapX.Domain.Base.Exceptions;

public sealed class ConflictException : BusinessException
{
    public ConflictException(string message) : base(message)
    {
    }
}
