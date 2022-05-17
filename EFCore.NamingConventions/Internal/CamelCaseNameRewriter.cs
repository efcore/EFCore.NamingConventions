using System.Globalization;

namespace EFCore.NamingConventions.Internal;

public class CamelCaseNameRewriter : INameRewriter
{
    private readonly CultureInfo _culture;

    public CamelCaseNameRewriter(CultureInfo culture)
        => _culture = culture;

    public string RewriteName(string name)
        => string.IsNullOrEmpty(name) ? name: char.ToLower(name[0], _culture) + name.Substring(1);
}
