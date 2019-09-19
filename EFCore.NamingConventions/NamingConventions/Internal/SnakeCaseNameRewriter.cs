using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace EFCore.NamingConventions.Internal
{
    class SnakeCaseNameRewriter : IEntityTypeAddedConvention, IPropertyAddedConvention
    {
        public void ProcessEntityTypeAdded(
            IConventionEntityTypeBuilder entityTypeBuilder,
            IConventionContext<IConventionEntityTypeBuilder> context)
            => entityTypeBuilder.ToTable(
                ConvertToSnakeCase(entityTypeBuilder.Metadata.GetTableName()),
                entityTypeBuilder.Metadata.GetSchema());

        public void ProcessPropertyAdded(
            IConventionPropertyBuilder propertyBuilder,
            IConventionContext<IConventionPropertyBuilder> context)
            => propertyBuilder.HasColumnName(
                ConvertToSnakeCase(propertyBuilder.Metadata.GetColumnName()));

        static string ConvertToSnakeCase(string value)
        {
            const char underscore = '_';
            const UnicodeCategory noneCategory = UnicodeCategory.Control;

            var builder = new StringBuilder();
            var previousCategory = noneCategory;

            for (var currentIndex = 0; currentIndex < value.Length; currentIndex++)
            {
                var currentChar = value[currentIndex];
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
                        currentIndex + 1 < value.Length &&
                        char.IsLower(value[currentIndex + 1]))
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
