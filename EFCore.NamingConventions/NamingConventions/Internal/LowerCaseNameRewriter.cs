using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    class LowerCaseNameRewriter : NameRewriterBase
    {
        protected override string RewriteName(string name,CultureInfo info = null) => name.ToLower(info ?? CultureInfo.InvariantCulture);
    }
}
