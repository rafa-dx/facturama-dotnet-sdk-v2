using Facturama.Sdk.Configuration;

namespace Facturama.Sdk.Tests;


public sealed class RetryOptionsValidationTests
{
    [Fact]
    public void PorDefecto_NoLanza()
    {
        new RetryOptions().Validate();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void MaxRetries_negativo_Lanza(int maxRetries)
    {
        var retry = new RetryOptions { MaxRetries = maxRetries };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => retry.Validate());
        Assert.Equal(nameof(RetryOptions.MaxRetries), ex.ParamName);
    }


    /// <summary>
    /// Regresión: sin tope, <c>BaseDelay * 2^(intento-1)</c> desborda el <c>TimeSpan</c>
    /// del presupuesto total y revienta con <c>OverflowException</c> dentro del SDK.
    /// </summary>

    [Theory]
    [InlineData(11)]
    [InlineData(64)]
    [InlineData(int.MaxValue)]
    public void MaxRetries_SobreElTope_Lanza(int maxRetriesd)
    {
        var retry = new RetryOptions { MaxRetries = maxRetriesd };

        Assert.Throws<ArgumentOutOfRangeException>(() => retry.Validate());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(RetryOptions.MaxRetriesLimit)]
    public void MaxRetries_EnLosLimites_EsValido(int maxRetries)
    {
        new RetryOptions { MaxRetries = maxRetries }.Validate();
    }

    [Fact]
    public void BaseDelay_Cero_Lanza()
    {
        var retry = new RetryOptions { BaseDelay = TimeSpan.Zero };

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => retry.Validate());
        Assert.Equal(nameof(RetryOptions.BaseDelay), ex.ParamName);
    }

    [Fact]
    public void BaseDelay_Negativo_Lanza()
    {
        var retry = new RetryOptions { BaseDelay = TimeSpan.FromMilliseconds(-1) };

        Assert.Throws<ArgumentOutOfRangeException>(() => retry.Validate());
    }

    [Fact]
    public void Deshabilitado_NoValidaSusUmbrales()
    {
        new RetryOptions
        {
            Enabled = false,
            MaxRetries = -5,
            BaseDelay = TimeSpan.Zero
        }.Validate();
    }

}

public sealed class FacturamaOptionsValidationTests
{
    private static FacturamaOptions Valid()
        => new() { Username = "user", Password = "pass" };

    [Fact]
    public void PorDefecto_LosDefaultsDeFabricaSonCoherentes()
    {
        Valid().Validate();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SinUsername_Lanza(string? username)
    {
        var options = Valid();
        options.Username = username;

        var ex = Assert.Throws<ArgumentException>(()=> options.Validate());
        Assert.Equal(nameof(FacturamaOptions.Username), ex.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SinPassword_Lanza(string? password)
    {
        var options = Valid();
        options.Password = password!;

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Equal(nameof(FacturamaOptions.Password), ex.ParamName);
    }

    [Fact]
    public void TimeoutCero_Lanza()
    {
        var options = Valid();
        options.Timeout = TimeSpan.Zero;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
        Assert.Equal(nameof(FacturamaOptions.Timeout), ex.ParamName);
    }


    [Fact]
    public void LasOpcionesAnidadasInvalidas_SePropagan()
    {
        var options = Valid();
        options.Retry = new RetryOptions { BaseDelay = TimeSpan.Zero };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    // --- BaseUrlOverride ---

    [Theory]
    [InlineData("http://localhost:5000")]
    [InlineData("http://localhost:5000/")]
    [InlineData("http://127.0.0.1:8080")]
    [InlineData("http://[::1]:8080")]
    [InlineData("https://mock.interno.example.com")]
    [InlineData("https://localhost:5001/facturama")]
    public void BaseUrlOverride_HttpEnLoopbackOHttps_EsValido(string url)
    {
        var options = Valid();
        options.BaseUrlOverride = new Uri(url);

        options.Validate();
    }

    /// <summary>
    /// Regresión: sin esta validación las credenciales de Basic Auth —base64, no cifradas—
    /// se enviaban a cualquier host que el consumidor configurara por error.
    /// </summary>
    [Theory]
    [InlineData("http://api.facturama.mx")]
    [InlineData("http://192.168.1.50:8080")]
    [InlineData("http://mock.interno.example.com/facturama")]
    public void BaseUrlOverride_HttpFueraDeLoopback_Lanza(string url)
    {
        var options = Valid();
        options.BaseUrlOverride = new Uri(url);

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Equal(nameof(FacturamaOptions.BaseUrlOverride), ex.ParamName);
    }

    [Theory]
    [InlineData("ftp://localhost/facturama")]
    [InlineData("file:///C:/mock")]
    [InlineData("ws://localhost:5000")]
    public void BaseUrlOverride_EsquemaNoHttp_Lanza(string url)
    {
        var options = Valid();
        options.BaseUrlOverride = new Uri(url);

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Equal(nameof(FacturamaOptions.BaseUrlOverride), ex.ParamName);
    }

    /// <summary>
    /// Regresión: una Uri relativa llegaba sin validar hasta <c>FacturamaHttpClientFactory</c>
    /// y reventaba con una <c>UriFormatException</c> cruda de .NET en lugar de un error del SDK.
    /// </summary>
    [Fact]
    public void BaseUrlOverride_Relativa_LanzaArgumentExceptionYNoUriFormatException()
    {
        var options = Valid();
        options.BaseUrlOverride = new Uri("Client/1", UriKind.Relative);

        var ex = Assert.Throws<ArgumentException>(() => options.Validate());
        Assert.Equal(nameof(FacturamaOptions.BaseUrlOverride), ex.ParamName);
    }


    [Fact]
    public void BaseUrlOverride_Nulo_UsaLaUrlDelAmbiente()
    {
        var options = Valid();
        options.BaseUrlOverride = null;

        options.Validate();
        Assert.Equal("https://apisandbox.facturama.mx", options.BaseUrl);
    }
}

public sealed class FacturamaOptionsBaseUrlTests
{
    [Fact]
    public void SinOverrided_Sandbox_EsLaUrlDeSandbox()
    {
        var options = new FacturamaOptions
        { Environment = FacturamaEnvironment.Sandbox };

        Assert.Equal("https://apisandbox.facturama.mx", options.BaseUrl);
    }

    [Fact]
    public void SinOverride_Produccion_EsLaUrlDeProduccion()
    {
        var options = new FacturamaOptions
        { Environment = FacturamaEnvironment.Production };

        Assert.Equal("https://api.facturama.mx", options.BaseUrl);
    }

    /// <summary>
    /// El override gana sobre el ambiente: es el escape hatch para pruebas locales.
    /// </summary>

    [Fact]
    public void ConOverrride_IgnoraElAmbiente()
    {
        var options = new FacturamaOptions
        {
            Environment = FacturamaEnvironment.Production,
            BaseUrlOverride = new Uri("http://localhost:5000/facturama")
        };

        Assert.Equal("http://localhost:5000/facturama", options.BaseUrl);
    }


    /// <summary>
    /// Regresión: <c>FacturamaHttpClientFactory</c> concatena <c>$"{BaseUrl}/{prefijo}/"</c>,
    /// así que una diagonal final aquí produce <c>http://localhost:5000//3/</c>. Uri normaliza
    /// la forma sin path a <c>"http://localhost:5000/"</c>, de modo que el caso se da siempre.
    /// </summary>
    [Theory]
    [InlineData("http://localhost:5000", "http://localhost:5000")]
    [InlineData("http://localhost:5000/", "http://localhost:5000")]
    [InlineData("https://localhost:5001/facturama/", "https://localhost:5001/facturama")]
    [InlineData("https://localhost:5001/facturama", "https://localhost:5001/facturama")]
    public void ConOverride_NuncaTerminaEnDiagonal(string url, string expected)
    {
        var options = new FacturamaOptions { BaseUrlOverride = new Uri(url) };

        Assert.Equal(expected, options.BaseUrl);
    }
}