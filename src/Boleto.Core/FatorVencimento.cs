namespace Boleto.Core;

/// <summary>
/// Fator de vencimento: dias desde 07/10/1997. Chegou a 9999 em 21/02/2025 e, por norma FEBRABAN,
/// voltou a 1000 em 22/02/2025, repetindo a cada 9000 dias. Fator 0000 significa "sem vencimento".
/// </summary>
public static class FatorVencimento
{
    public static readonly DateOnly Base = new(1997, 10, 7);
    public const int Minimo = 1000;
    public const int Maximo = 9999;
    private const int Ciclo = Maximo - Minimo + 1; // 9000

    public static int Calcular(DateOnly vencimento)
    {
        var dias = vencimento.DayNumber - Base.DayNumber;
        if (dias < Minimo) throw new BoletoInvalidoException("vencimento anterior a 03/07/2000 não tem fator válido");
        return Minimo + (dias - Minimo) % Ciclo;
    }

    /// <summary>
    /// Data correspondente ao fator. Como o fator se repete a cada 9000 dias, escolhe a data mais
    /// próxima da referência (hoje, por padrão), como fazem os bancos na leitura.
    /// </summary>
    public static DateOnly? Data(int fator, DateOnly? referencia = null)
    {
        if (fator == 0) return null;
        if (fator is < Minimo or > Maximo) throw new BoletoInvalidoException($"fator de vencimento fora do intervalo {Minimo}-{Maximo}: {fator}");
        var hoje = referencia ?? DateOnly.FromDateTime(DateTime.Today);
        DateOnly? melhor = null;
        for (var ciclo = 0; ciclo < 4; ciclo++)
        {
            var candidata = Base.AddDays(fator + ciclo * Ciclo);
            if (melhor is null || Math.Abs(candidata.DayNumber - hoje.DayNumber) < Math.Abs(melhor.Value.DayNumber - hoje.DayNumber)) melhor = candidata;
        }
        return melhor;
    }
}
