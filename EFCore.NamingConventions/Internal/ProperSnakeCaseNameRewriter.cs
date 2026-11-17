using System;
using System.Globalization;
using System.Text;

namespace EFCore.NamingConventions.Internal;

public class ProperSnakeCaseNameRewriter : SnakeCaseNameRewriter
{
    private readonly CultureInfo _culture;

    public ProperSnakeCaseNameRewriter(CultureInfo culture) : base(culture)
        => _culture = culture;

    public override string RewriteName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        name = base.RewriteName(name);

        var arr = name.ToCharArray();
        var shouldCapitalizeNext = true;

        for (var i = 0; i < arr.Length; i++)
        {
            if (arr[i] == '_')
            {
                shouldCapitalizeNext = true;
            }
            else if (shouldCapitalizeNext)
            {
                arr[i] = char.ToUpper(arr[i]);
                shouldCapitalizeNext = false;
            }
        }

        return new(arr);
    }
}
