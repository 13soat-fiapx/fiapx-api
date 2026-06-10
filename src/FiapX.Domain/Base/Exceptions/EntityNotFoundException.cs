using System.Diagnostics.CodeAnalysis;

namespace FiapX.Domain.Base.Exceptions;

public sealed class EntityNotFoundException : BusinessException
{
    public EntityNotFoundException(string entityName, object? id = null)
        : base(id is null
            ? $"{entityName} not found."
            : $"{entityName} with id '{id}' not found.")
    {
    }

    public static void ThrowIfNull<T>([NotNull] T? entity, object? id = null)
    {
        if (entity is not null)
            return;

        throw new EntityNotFoundException(typeof(T).Name, id);
    }

    public static void ThrowIfNotFound<T>([DoesNotReturnIf(false)] bool found, object? id = null)
    {
        if (found)
            return;

        throw new EntityNotFoundException(typeof(T).Name, id);
    }
}
