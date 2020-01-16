namespace EFCore.NamingConventions.Internal
{
    class UpperCaseNameRewriter : NameRewriterBase
    {
        protected override string RewriteName(string name) => name.ToUpperInvariant();
    }
}
