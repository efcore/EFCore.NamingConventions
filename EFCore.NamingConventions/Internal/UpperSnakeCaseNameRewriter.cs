using System.Globalization;

namespace EFCore.NamingConventions.Internal;

public class UpperSnakeCaseNameRewriter : SnakeCaseNameRewriter
{
    private readonly CultureInfo _culture;

    public UpperSnakeCaseNameRewriter(CultureInfo culture, bool legacySnakeCase) : base(culture, legacySnakeCase)
        => _culture = culture;

    public override string RewriteName(string name)
        => base.RewriteName(name).ToUpper(_culture);
}
