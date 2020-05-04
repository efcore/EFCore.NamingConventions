using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class LowerCaseNameRewriter : NameRewriterBase
    {
        readonly CultureInfo _culture;

        public LowerCaseNameRewriter(CultureInfo culture) => _culture = culture;
        protected override string RewriteName(string name) => name.ToLower(_culture);
    }
}
