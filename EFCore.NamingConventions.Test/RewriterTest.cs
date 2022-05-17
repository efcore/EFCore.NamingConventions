using System.Globalization;
using EFCore.NamingConventions.Internal;
using Xunit;

namespace EFCore.NamingConventions.Test;

public class RewriterTest
{
    [Fact]
    public void Snake_case()
        => Assert.Equal("full_name", new SnakeCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

    [Fact]
    public void Upper_snake_case()
        => Assert.Equal("FULL_NAME", new UpperSnakeCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

    [Fact]
    public void Lower_case()
        => Assert.Equal("fullname", new LowerCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

    [Fact]
    public void Camel_case()
        => Assert.Equal("fullName", new CamelCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));

    [Fact]
    public void Upper_case()
        => Assert.Equal("FULLNAME", new UpperCaseNameRewriter(CultureInfo.InvariantCulture).RewriteName("FullName"));
}
