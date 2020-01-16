namespace EFCore.NamingConventions.Internal
{
    class LowerCaseNameRewriter : NameRewriterBase
    {
        protected override string RewriteName(string name) => name.ToLowerInvariant();
    }
}
