using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    public class UpperSnakeCaseNameRewriter : SnakeCaseNameRewriter
    {
        readonly CultureInfo _culture;

        public UpperSnakeCaseNameRewriter(CultureInfo culture) : base(culture) => _culture = culture;

        public override string RewriteName(string name) => base.RewriteName(name).ToUpper(_culture);
    }
}
