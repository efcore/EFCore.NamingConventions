using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class UpperCaseNameRewriter : NameRewriterBase
    {
        protected override string RewriteName(string name,CultureInfo info = null ) => name.ToUpper(info ?? CultureInfo.InvariantCulture);
    }
}
