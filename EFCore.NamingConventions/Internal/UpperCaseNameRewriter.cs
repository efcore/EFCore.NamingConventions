using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    public class UpperCaseNameRewriter : INameRewriter
    {
        private readonly CultureInfo _culture;

        public UpperCaseNameRewriter(CultureInfo culture) => _culture = culture;
        public string RewriteName(string name) => name.ToUpper(_culture);
    }
}
