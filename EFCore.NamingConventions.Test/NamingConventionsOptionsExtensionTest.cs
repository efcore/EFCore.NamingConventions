using System;
using System.Collections.Generic;
using EFCore.NamingConventions.Internal;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class NamingConventionsOptionsExtensionTest
    {
        [Theory]
        [MemberData(nameof(Convention))]
        public void logFragment_not_throw_exception(NamingConvention convention)
        {
            var extension = GetNamingConventionsOptionsExtension(convention);

            var exception = Record.Exception(() => extension.Info.LogFragment);

            Assert.Null(exception);
        }

        public static IEnumerable<object[]> Convention()
        {
            foreach (var convention in Enum.GetValues<NamingConvention>())
            {
                yield return new object[] { convention };
            }
        }

        public static NamingConventionsOptionsExtension GetNamingConventionsOptionsExtension(NamingConvention convention) => convention switch
        {
            NamingConvention.None => new NamingConventionsOptionsExtension().WithoutNaming(),
            NamingConvention.SnakeCase => new NamingConventionsOptionsExtension().WithSnakeCaseNamingConvention(),
            NamingConvention.LowerCase => new NamingConventionsOptionsExtension().WithLowerCaseNamingConvention(),
            NamingConvention.CamelCase => new NamingConventionsOptionsExtension().WithCamelCaseNamingConvention(),
            NamingConvention.KebabCase => new NamingConventionsOptionsExtension().WithKebabCaseNamingConvention(),
            NamingConvention.UpperCase => new NamingConventionsOptionsExtension().WithUpperCaseNamingConvention(),
            NamingConvention.UpperSnakeCase => new NamingConventionsOptionsExtension().WithUpperSnakeCaseNamingConvention(),
            _ => throw new ArgumentOutOfRangeException($"Unhandled enum value: {convention}, NamingConventionsOptionsExtension not defined for the test")
        };
    }
}
