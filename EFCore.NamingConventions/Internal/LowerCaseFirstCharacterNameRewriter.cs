using System.Globalization;

namespace EFCore.NamingConventions.Internal
{
    public class LowerCaseFirstCharacterNameRewriter : INameRewriter
    {
        private readonly CultureInfo _culture;

        public LowerCaseFirstCharacterNameRewriter(CultureInfo culture) => _culture = culture;
        public string RewriteName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            return char.ToLower(name[0], _culture) + name.Substring(1);
        }
    }
}
