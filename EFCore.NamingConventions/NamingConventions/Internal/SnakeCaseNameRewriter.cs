using System.Globalization;
using System.Text;

namespace EFCore.NamingConventions.Internal
{
    class SnakeCaseNameRewriter : NameRewriterBase
    {
        protected override string RewriteName(string name)
        {
            const char underscore = '_';
            const UnicodeCategory noneCategory = UnicodeCategory.Control;

            var builder = new StringBuilder();
            var previousCategory = noneCategory;

            for (var currentIndex = 0; currentIndex < name.Length; currentIndex++)
            {
                var currentChar = name[currentIndex];
                if (currentChar == underscore)
                {
                    builder.Append(underscore);
                    previousCategory = noneCategory;
                    continue;
                }

                var currentCategory = char.GetUnicodeCategory(currentChar);
                switch (currentCategory)
                {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                    if (previousCategory == UnicodeCategory.SpaceSeparator ||
                        previousCategory == UnicodeCategory.LowercaseLetter ||
                        previousCategory != UnicodeCategory.DecimalDigitNumber &&
                        currentIndex > 0 &&
                        currentIndex + 1 < name.Length &&
                        char.IsLower(name[currentIndex + 1]))
                    {
                        builder.Append(underscore);
                    }

                    currentChar = char.ToLower(currentChar);
                    break;

                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.DecimalDigitNumber:
                    if (previousCategory == UnicodeCategory.SpaceSeparator)
                        builder.Append(underscore);
                    break;

                default:
                    if (previousCategory != noneCategory)
                        previousCategory = UnicodeCategory.SpaceSeparator;
                    continue;
                }

                builder.Append(currentChar);
                previousCategory = currentCategory;
            }

            return builder.ToString();
        }
    }
}
