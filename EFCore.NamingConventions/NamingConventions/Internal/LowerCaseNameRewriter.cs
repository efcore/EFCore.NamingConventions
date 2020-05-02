using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class LowerCaseNameRewriter : NameRewriterBase
    {
        readonly CultureInfo _cultureInfo;

        public LowerCaseNameRewriter(CultureInfo cultureInfo = null) => _cultureInfo = cultureInfo;
        protected override string RewriteName(string name) => name.ToLower(_cultureInfo ?? CultureInfo.InvariantCulture);
    }
}
