using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Boleto.Core;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Boleto Bancário",
    Version = "v1",
    Description = "Geração, leitura e validação de boletos de cobrança no padrão FEBRABAN: código de barras (44), linha digitável (47), "
                  + "módulo 10 e 11, fator de vencimento com a virada de 2025, campo livre de BB, Bradesco, Itaú, Santander e Caixa, e código de barras em SVG (ITF).",
}));

var app = builder.Build();
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (BoletoInvalidoException e) { ctx.Response.StatusCode = 422; await ctx.Response.WriteAsJsonAsync(new { title = e.Message, status = 422 }); }
    catch (BadHttpRequestException e) { ctx.Response.StatusCode = 400; await ctx.Response.WriteAsJsonAsync(new { title = e.Message, status = 400 }); }
});
app.UseSwagger();
app.UseSwaggerUI(o => { o.RoutePrefix = "docs"; o.DocumentTitle = "Boleto Bancário"; });

var g = app.MapGroup("/api/boletos").WithTags("Boletos");

g.MapPost("/", (GerarRequest req) =>
{
    var venc = req.Vencimento is null ? null : (DateOnly?)DateOnly.Parse(req.Vencimento, System.Globalization.CultureInfo.InvariantCulture);
    var b = BoletoCobranca.Gerar(req.Banco, venc, req.Valor, new DadosBeneficiario(req.Agencia, req.Conta, req.Carteira, req.NossoNumero, req.Convenio, req.CodigoBeneficiario, req.CampoLivre));
    return Results.Created($"/api/boletos/{b.LinhaDigitavel.Texto}", BoletoResponse.De(b, req.IncluirSvg));
}).WithSummary("Gera código de barras e linha digitável a partir dos dados do título")
  .WithDescription("Informe os campos do banco escolhido (BB: convenio, nossoNumero, carteira · Bradesco: agencia, carteira, nossoNumero, conta · Itaú: carteira, nossoNumero, agencia, conta · Santander: codigoBeneficiario, nossoNumero, carteira · Caixa: codigoBeneficiario, nossoNumero de 17) ou o campoLivre pronto para outros bancos.");

g.MapGet("/{codigo}", (string codigo, string? referencia) =>
{
    var b = BoletoCobranca.Ler(codigo);
    var refData = referencia is null ? (DateOnly?)null : DateOnly.Parse(referencia, System.Globalization.CultureInfo.InvariantCulture);
    return Results.Ok(BoletoResponse.De(b, false, refData));
}).WithSummary("Decodifica e valida uma linha digitável (47) ou código de barras (44), com ou sem pontuação");

g.MapGet("/{codigo}/validar", (string codigo) =>
{
    var (b, erro) = BoletoCobranca.TentarLer(codigo);
    return Results.Ok(new { valido = b is not null, erro, banco = b?.NomeBanco, valor = b?.Valor, vencimento = b?.Vencimento() });
}).WithSummary("Valida sem lançar erro: devolve válido/inválido e o motivo");

g.MapGet("/{codigo}/svg", (string codigo, int? altura) =>
{
    var b = BoletoCobranca.Ler(codigo);
    return Results.Content(CodigoDeBarrasSvg.Gerar(b.CodigoDeBarras.Texto, Math.Clamp(altura ?? 50, 20, 200)), "image/svg+xml");
}).WithSummary("Código de barras ITF em SVG");

g.MapGet("/bancos", () => CampoLivre.Bancos.Select(kv => new { codigo = kv.Key, nome = kv.Value })).WithSummary("Bancos com layout de campo livre conhecido");

app.MapGet("/saude", () => Results.Ok(new { status = "ok" }));
app.Run();

public sealed record GerarRequest([Required] string Banco, decimal Valor, string? Vencimento, string? Agencia, string? Conta, string? Carteira, string? NossoNumero, string? Convenio, string? CodigoBeneficiario, string? CampoLivre, bool IncluirSvg = false);

public sealed record BoletoResponse(string Banco, string NomeBanco, decimal Valor, DateOnly? Vencimento, int FatorVencimento, string CodigoDeBarras, string LinhaDigitavel, string LinhaDigitavelFormatada, string CampoLivre, string? Svg)
{
    public static BoletoResponse De(BoletoCobranca b, bool comSvg, DateOnly? referencia = null) => new(b.Banco, b.NomeBanco, b.Valor, b.Vencimento(referencia), b.CodigoDeBarras.Fator,
        b.CodigoDeBarras.Texto, b.LinhaDigitavel.Texto, b.LinhaDigitavel.Formatada, b.CodigoDeBarras.CampoLivre, comSvg ? CodigoDeBarrasSvg.Gerar(b.CodigoDeBarras.Texto) : null);
}

public partial class Program { }
