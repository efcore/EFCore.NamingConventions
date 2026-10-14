using System.Globalization;
using EFCore.NamingConventions.Internal;
using Xunit;

namespace EFCore.NamingConventions.Test;

public class RewriterTest
{
    [Fact]
    public void Snake_case()
        => Assert.Multiple(() =>
        {
            Assert.Equal("full_name", new SnakeCaseNameRewriter(CultureInfo.InvariantCulture, false).RewriteName("FullName"));
            Assert.Equal("xml_v2_row6", new SnakeCaseNameRewriter(CultureInfo.InvariantCulture, false).RewriteName("XmlV2Row6"));
            Assert.Equal("xml_v2row6", new SnakeCaseNameRewriter(CultureInfo.InvariantCulture, true).RewriteName("XmlV2Row6"));
            Assert.Equal("xml2linq", new SnakeCaseNameRewriter(CultureInfo.InvariantCulture, false).RewriteName("Xml2linq"));
        });

    [Fact]
    public void Upper_snake_case()
        => Assert.Equal("FULL_NAME", new UpperSnakeCaseNameRewriter(CultureInfo.InvariantCulture, false).RewriteName("FullName"));

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
