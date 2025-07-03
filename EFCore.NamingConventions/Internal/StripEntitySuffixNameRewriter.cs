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
        // If the name has the expected suffix and
        // the name is longer than the suffix itself, then
        // remove the suffix.
        // Example:
        //    "FooEntity" becomes "Foo"
        //    "Entity" remains as "Entity"
        //    "Foo" remains as "Foo"
        int substringLength = name.Length - EntitySuffix.Length;
        if (substringLength > 0 && name.EndsWith(EntitySuffix, false, _culture))
        {
            string newName = name.Substring(0, substringLength);
            return newName;
        }

        // If we are here, is because we didn't modify the name.
        return name;
    }
}
