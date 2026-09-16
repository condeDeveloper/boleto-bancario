using Boleto.Core;

namespace Boleto.Tests;

public class DigitosTests
{
    [Theory]
    [InlineData("001905009", 5)]      // exemplo clássico do manual FEBRABAN para módulo 10
    [InlineData("4014481606", 9)]
    [InlineData("0680935031", 4)]
    [InlineData("0", 0)]
    public void Modulo10VetoresConhecidos(string numero, int dv) => Digitos.Modulo10(numero).Should().Be(dv);

    [Fact]
    public void Modulo11DoCodigoDeBarrasNuncaEhZeroDezOuOnze()
    {
        for (var i = 0; i < 2000; i++)
        {
            var numero = i.ToString().PadLeft(43, '0');
            Digitos.Modulo11CodigoDeBarras(numero).Should().BeInRange(1, 9);
        }
    }

    [Fact]
    public void Modulo11ExemploDoManual()
    {
        // 0019373700000001000500940144816060680935031 -> DV 3 (exemplo FEBRABAN)
        Digitos.Modulo11CodigoDeBarras("0019373700000001000500940144816060680935031").Should().Be(3);
    }

    [Fact]
    public void ZerosPreencheELimita()
    {
        Digitos.Zeros("123", 6, "x").Should().Be("000123");
        var act = () => Digitos.Zeros("1234567", 6, "campo");
        act.Should().Throw<BoletoInvalidoException>().WithMessage("*campo*máximo é 6*");
    }

    [Fact]
    public void ApenasDigitos()
    {
        Digitos.Apenas("23793.38128 60000.000003 00000.000400 1 84340000010000").Should().HaveLength(47);
        var act = () => Digitos.SoDigitos("12a");
        act.Should().Throw<BoletoInvalidoException>();
    }
}
