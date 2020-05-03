using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class UpperCaseNameRewriter : NameRewriterBase
    {
        readonly CultureInfo _culture;

        public UpperCaseNameRewriter(CultureInfo culture) => _culture = culture;
        protected override string RewriteName(string name) => name.ToUpper(_culture);
    }
}
