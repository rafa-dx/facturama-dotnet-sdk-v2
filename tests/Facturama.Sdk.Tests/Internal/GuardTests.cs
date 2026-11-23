using System;
using System.Collections.Generic;
using System.Text;
using Facturama.Sdk.Internal;

namespace Facturama.Sdk.Tests.Internal;

public sealed class GuardTests
{
    public static TheoryData<string> TextosEnBlanco => new() { "", " ", "   ", "\t", "\r\n" };

    [Fact]
    public void NotNullOrWhiteSpace_ConNulo_lanzaArgumentNullException()
    {
       var ex = Assert.Throws<ArgumentNullException>(
           () => Guard.NotNullOrWhiteSpace(null, "id"));
       Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [MemberData(nameof(TextosEnBlanco))]
    public void NotNullOrWhiteSpace_ConTextoEnBlanco_lanzaArgumentException(string value)
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Guard.NotNullOrWhiteSpace(value, "id"));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [InlineData("a")]
    [InlineData(" b ")]
    [InlineData("J6MjEYRjnLnV6aMCK8dV-w2")]

    public void NotNullOrWhiteSpace_ConTextoValido_NoLanza(string value)
    {
        Guard.NotNullOrWhiteSpace(value, "id");
    }

    [Theory]
    [InlineData("J6MjEYRjnLnV6aMCK8dV-w2", "J6MjEYRjnLnV6aMCK8dV-w2")]
    [InlineData("a/b", "a%2Fb")]
    [InlineData("a b", "a%20b")]
    [InlineData("a?b=1&c=2", "a%3Fb%3D1%26c%3D2")]
    [InlineData("a#b", "a%23b")]
    [InlineData("a%2Fb", "a%252Fb")]
    [InlineData("ñ", "%C3%B1")]
    [InlineData("a.b", "a.b")]
    [InlineData("...", "...")]

    public void PathSegment_CodificaLoQueAlteraLaRuta(string value, string expected)
    {
        Assert.Equal(expected, Guard.PathSegment(value, "id"));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    public void PathSegment_ConSegmentoDeNavegacion_LanzaArgumentException(string value)
    {
        var ex = Assert.Throws<ArgumentException>(() => Guard.PathSegment(value, "id"));

        Assert.Equal("id", ex.ParamName);
    }

    [Fact]
    public void PathSegment_ConNulo_LanzaArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Guard.PathSegment(null, "id"));
    }

    [Theory]
    [MemberData(nameof(TextosEnBlanco))]
    public void PathSegment_ConTextoEnBlanco_LanzaArgumentException(string value)
    {
        Assert.Throws<ArgumentException>(() => Guard.PathSegment(value, "id"));
    }


}
