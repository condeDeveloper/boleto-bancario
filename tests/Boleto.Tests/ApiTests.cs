using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Boleto.Tests;

public class ApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;

    public ApiTests(WebApplicationFactory<Program> f) => _http = f.CreateClient();

    [Fact]
    public async Task GeraDecodificaValidaESvg()
    {
        var r = await _http.PostAsJsonAsync("/api/boletos", new { banco = "237", valor = 150.75, vencimento = "2026-10-15", agencia = "1234", carteira = "09", nossoNumero = "12345678901", conta = "1234567", incluirSvg = true });
        r.StatusCode.Should().Be(HttpStatusCode.Created);
        var b = await r.Content.ReadFromJsonAsync<JsonElement>(Json);
        b.GetProperty("nomeBanco").GetString().Should().Be("Bradesco");
        b.GetProperty("svg").GetString().Should().StartWith("<svg");
        var linha = b.GetProperty("linhaDigitavelFormatada").GetString()!;

        var lido = await _http.GetFromJsonAsync<JsonElement>($"/api/boletos/{Uri.EscapeDataString(linha)}?referencia=2026-09-16", Json);
        lido.GetProperty("valor").GetDecimal().Should().Be(150.75m);
        lido.GetProperty("vencimento").GetString().Should().Be("2026-10-15");
        lido.GetProperty("codigoDeBarras").GetString().Should().Be(b.GetProperty("codigoDeBarras").GetString());

        var svg = await _http.GetAsync($"/api/boletos/{b.GetProperty("codigoDeBarras").GetString()}/svg");
        svg.Content.Headers.ContentType!.MediaType.Should().Be("image/svg+xml");

        var invalido = await _http.GetFromJsonAsync<JsonElement>("/api/boletos/12345/validar", Json);
        invalido.GetProperty("valido").GetBoolean().Should().BeFalse();
        var valido = await _http.GetFromJsonAsync<JsonElement>($"/api/boletos/{Uri.EscapeDataString(linha)}/validar", Json);
        valido.GetProperty("valido").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ErrosDeNegocioViram422()
    {
        var r = await _http.PostAsJsonAsync("/api/boletos", new { banco = "001", valor = 10, vencimento = "2026-10-15" });
        r.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await r.Content.ReadFromJsonAsync<JsonElement>(Json)).GetProperty("title").GetString().Should().Contain("obrigatório");
        (await _http.GetAsync("/api/boletos/0000000000000000000000000000000000000000000000/validar")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _http.GetAsync("/api/boletos/bancos")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
