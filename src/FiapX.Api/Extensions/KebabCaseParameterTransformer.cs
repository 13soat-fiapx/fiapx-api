using Microsoft.AspNetCore.Routing;
using System.Text.RegularExpressions;

namespace FiapX.Api.Extensions;

public sealed partial class KebabCaseParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        if (value is null)
            return null;

        return KebabCaseRegex()
            .Replace(value.ToString()!, "$1-$2")
            .ToLowerInvariant();
    }

    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex KebabCaseRegex();
}
