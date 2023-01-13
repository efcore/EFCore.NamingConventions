using System.Globalization;

namespace EFCore.NamingConventions.Internal;

public class UpperSnakeCaseNameRewriter : SnakeCaseNameRewriter
{
    private readonly CultureInfo _culture;

    public UpperSnakeCaseNameRewriter(CultureInfo culture) : base(culture)
        => _culture = culture;

    public override string RewriteName(string name)
    {
        var snakeCaseName = base.RewriteName(name);

        return string.IsNullOrEmpty(snakeCaseName) ? snakeCaseName : snakeCaseName.ToUpper(_culture);
    }
}
