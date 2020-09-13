using System.Globalization;
using EFCore.NamingConventions.Internal;
using Xunit;

namespace EFCore.NamingConventions.Test
{
    public class RewriterTest
    {
        [Fact]
        public void SnakeCase()
            => Assert.Equal("full_name",
                new SnakeCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

        [Fact]
        public void UpperSnakeCase()
            => Assert.Equal("FULL_NAME",
                new UpperSnakeCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

        [Fact]
        public void LowerCase()
            => Assert.Equal("fullname",
                new LowerCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

        [Fact]
        public void UpperCase()
            => Assert.Equal("FULLNAME",
                new UpperCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));
    }
}
