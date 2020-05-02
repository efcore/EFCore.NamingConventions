using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class UpperCaseNameRewriter : NameRewriterBase
    {
        readonly CultureInfo _cultureInfo;

        public UpperCaseNameRewriter(CultureInfo cultureInfo = null) => _cultureInfo = cultureInfo;
        protected override string RewriteName(string name) => name.ToUpper(_cultureInfo ?? CultureInfo.InvariantCulture);
    }
}
