using Boleto.Core;

namespace Boleto.Tests;

public class FatorVencimentoTests
{
    [Theory]
    [InlineData(2000, 7, 3, 1000)]     // primeiro fator válido
    [InlineData(2000, 7, 4, 1001)]
    [InlineData(2025, 2, 21, 9999)]    // último antes da virada
    [InlineData(2025, 2, 22, 1000)]    // virada FEBRABAN
    [InlineData(2026, 9, 16, 1571)]
    public void CalculaOFator(int ano, int mes, int dia, int fator) => FatorVencimento.Calcular(new DateOnly(ano, mes, dia)).Should().Be(fator);

    [Fact]
    public void RecusaDatasAnterioresAoPrimeiroFator()
    {
        var act = () => FatorVencimento.Calcular(new DateOnly(1999, 1, 1));
        act.Should().Throw<BoletoInvalidoException>();
    }

    [Fact]
    public void DataDoFatorEscolheOCicloMaisProximoDaReferencia()
    {
        // fator 1000 lido em 2026 é 22/02/2025, não 03/07/2000
        FatorVencimento.Data(1000, new DateOnly(2026, 9, 16)).Should().Be(new DateOnly(2025, 2, 22));
        // o mesmo fator lido em 2001 é 03/07/2000
        FatorVencimento.Data(1000, new DateOnly(2001, 1, 1)).Should().Be(new DateOnly(2000, 7, 3));
        FatorVencimento.Data(1571, new DateOnly(2026, 9, 1)).Should().Be(new DateOnly(2026, 9, 16));
        FatorVencimento.Data(0).Should().BeNull();
    }

    [Fact]
    public void IdaEVoltaEmTodoOIntervalo()
    {
        for (var d = new DateOnly(2025, 2, 22); d < new DateOnly(2030, 1, 1); d = d.AddDays(37))
            FatorVencimento.Data(FatorVencimento.Calcular(d), d.AddDays(-10)).Should().Be(d);
    }
}
