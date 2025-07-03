using System.Globalization;

namespace EFCore.NamingConventions.Internal;

public class StripEntitySuffixNameRewriter : INameRewriter
{
    private readonly CultureInfo _culture;
    private static readonly string EntitySuffix = "Entity";

    public StripEntitySuffixNameRewriter(CultureInfo culture)
    {
        _culture = culture;
    }

    public string RewriteName(string name)
    {
        string newName = name;
        if (name.EndsWith(EntitySuffix, false, _culture))
        {
            newName = name.Substring(0, name.Length - EntitySuffix.Length);
        }

        return newName;
    }
}
