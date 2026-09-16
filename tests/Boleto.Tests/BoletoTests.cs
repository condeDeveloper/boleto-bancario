using Boleto.Core;

namespace Boleto.Tests;

public class BoletoTests
{
    private static readonly DateOnly Venc = new(2026, 10, 15);

    [Fact]
    public void GeraBoletoDoBancoDoBrasilEVoltaDaLinhaDigitavel()
    {
        var b = BoletoCobranca.Gerar("001", Venc, 1234.56m, new DadosBeneficiario(Convenio: "1234567", NossoNumero: "123", Carteira: "17"));
        b.CodigoDeBarras.Texto.Should().HaveLength(44).And.StartWith("0019");
        b.CodigoDeBarras.CampoLivre.Should().Be("000000" + "1234567" + "0000000123" + "17");
        b.LinhaDigitavel.Texto.Should().HaveLength(47);
        b.LinhaDigitavel.Formatada.Should().MatchRegex(@"^\d{5}\.\d{5} \d{5}\.\d{6} \d{5}\.\d{6} \d \d{14}$");
        b.Valor.Should().Be(1234.56m);
        b.Vencimento(new DateOnly(2026, 9, 16)).Should().Be(Venc);

        var lido = BoletoCobranca.Ler(b.LinhaDigitavel.Formatada);
        lido.CodigoDeBarras.Should().Be(b.CodigoDeBarras);
        BoletoCobranca.Ler(b.CodigoDeBarras.Texto).LinhaDigitavel.Should().Be(b.LinhaDigitavel);
    }

    [Fact]
    public void LayoutsDosOutrosBancos()
    {
        CampoLivre.Bradesco(new DadosBeneficiario(Agencia: "1234", Carteira: "09", NossoNumero: "12345678901", Conta: "1234567")).Should().Be("1234" + "09" + "12345678901" + "1234567" + "0");

        var itau = CampoLivre.Itau(new DadosBeneficiario(Carteira: "109", NossoNumero: "12345678", Agencia: "1234", Conta: "56789"));
        itau.Should().HaveLength(25).And.StartWith("10912345678").And.EndWith("000");
        itau[11].Should().Be((char)('0' + Digitos.Modulo10("1234" + "56789" + "109" + "12345678")));
        itau[21].Should().Be((char)('0' + Digitos.Modulo10("1234" + "56789")));

        CampoLivre.Santander(new DadosBeneficiario(CodigoBeneficiario: "1234567", NossoNumero: "1", Carteira: "101")).Should().Be("9" + "1234567" + "0000000000001" + "0" + "101");

        var caixa = CampoLivre.Caixa(new DadosBeneficiario(CodigoBeneficiario: "123456", NossoNumero: "24000000000000001"));
        caixa.Should().HaveLength(25).And.StartWith("123456");
        caixa[..24].Should().EndWith("000000001"[..0] + caixa[15..24]);
    }

    [Fact]
    public void CampoLivrePronto()
    {
        var b = BoletoCobranca.Gerar("999", Venc, 10m, new DadosBeneficiario(CampoLivre: "1234567890123456789012345"));
        b.CodigoDeBarras.CampoLivre.Should().Be("1234567890123456789012345");
        b.NomeBanco.Should().Be("Banco 999");
        var act = () => BoletoCobranca.Gerar("999", Venc, 10m, new DadosBeneficiario(NossoNumero: "1"));
        act.Should().Throw<BoletoInvalidoException>().WithMessage("*sem layout*");
    }

    [Fact]
    public void SemVencimentoTemFatorZero()
    {
        var b = BoletoCobranca.Gerar("237", null, 50m, new DadosBeneficiario(Agencia: "1", Carteira: "9", NossoNumero: "1", Conta: "1"));
        b.CodigoDeBarras.Fator.Should().Be(0);
        b.Vencimento().Should().BeNull();
        b.LinhaDigitavel.Campo5.Should().StartWith("0000");
    }

    [Fact]
    public void DetectaDigitosVerificadoresErrados()
    {
        var b = BoletoCobranca.Gerar("341", Venc, 99.9m, new DadosBeneficiario(Carteira: "109", NossoNumero: "1", Agencia: "1", Conta: "1"));
        var linha = b.LinhaDigitavel.Texto.ToCharArray();
        linha[9] = linha[9] == '0' ? '1' : '0'; // DV do campo 1
        var act = () => BoletoCobranca.Ler(new string(linha));
        act.Should().Throw<BoletoInvalidoException>().WithMessage("*campo 1*");

        var barras = b.CodigoDeBarras.Texto.ToCharArray();
        barras[4] = barras[4] == '1' ? '2' : '1';
        var (lido, erro) = BoletoCobranca.TentarLer(new string(barras));
        lido.Should().BeNull();
        erro.Should().Contain("DV do código de barras");

        BoletoCobranca.TentarLer("123").Erro.Should().Contain("47 dígitos");
    }

    [Fact]
    public void ValidaValores()
    {
        var neg = () => BoletoCobranca.Gerar("001", Venc, -1m, new DadosBeneficiario(Convenio: "1", NossoNumero: "1", Carteira: "17"));
        neg.Should().Throw<BoletoInvalidoException>();
        var centavos = () => BoletoCobranca.Gerar("001", Venc, 1.005m, new DadosBeneficiario(Convenio: "1", NossoNumero: "1", Carteira: "17"));
        centavos.Should().Throw<BoletoInvalidoException>();
        var grande = () => BoletoCobranca.Gerar("001", Venc, 100_000_000m, new DadosBeneficiario(Convenio: "1", NossoNumero: "1", Carteira: "17"));
        grande.Should().Throw<BoletoInvalidoException>();
    }

    [Fact]
    public void SvgInterleaved2of5()
    {
        var b = BoletoCobranca.Gerar("001", Venc, 1m, new DadosBeneficiario(Convenio: "1", NossoNumero: "1", Carteira: "17"));
        var larguras = CodigoDeBarrasSvg.Larguras(b.CodigoDeBarras.Texto);
        // início (4) + 22 pares x 10 + fim (3) = 227 elementos, alternando barra/espaço
        larguras.Should().HaveCount(4 + 22 * 10 + 3);
        larguras.Should().OnlyContain(w => w == 1 || w == 3);
        var svg = CodigoDeBarrasSvg.Gerar(b.CodigoDeBarras.Texto);
        svg.Should().StartWith("<svg").And.Contain("<rect").And.Contain(b.LinhaDigitavel.Formatada).And.EndWith("</svg>");
        svg.Split("<rect").Length.Should().Be(1 + 1 + (4 + 22 * 10 + 3 + 1) / 2); // fundo + barras (elementos ímpares)
    }
}
