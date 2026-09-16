using System.Globalization;

namespace Boleto.Core;

/// <summary>
/// Código de barras de 44 posições do boleto de cobrança:
/// banco(3) + moeda(1) + DV(1) + fator de vencimento(4) + valor(10) + campo livre(25).
/// </summary>
public sealed record CodigoDeBarras(string Banco, char Moeda, int Dv, int Fator, decimal Valor, string CampoLivre)
{
    public const int Tamanho = 44;

    public static CodigoDeBarras Montar(string banco, DateOnly? vencimento, decimal valor, string campoLivre, char moeda = '9')
    {
        banco = Digitos.Zeros(banco, 3, "código do banco");
        if (campoLivre.Length != 25 || !campoLivre.All(char.IsDigit)) throw new BoletoInvalidoException("campo livre deve ter 25 dígitos");
        if (valor < 0 || valor != decimal.Round(valor, 2)) throw new BoletoInvalidoException("valor deve ser positivo com até duas casas");
        if (valor > 99_999_999.99m) throw new BoletoInvalidoException("valor máximo é 99.999.999,99");
        var fator = vencimento is null ? 0 : FatorVencimento.Calcular(vencimento.Value);
        var semDv = banco + moeda + Digitos.Zeros(fator, 4, "fator") + ValorPara10(valor) + campoLivre;
        var dv = Digitos.Modulo11CodigoDeBarras(semDv);
        return new CodigoDeBarras(banco, moeda, dv, fator, valor, campoLivre);
    }

    /// <summary>Lê e valida os 44 dígitos.</summary>
    public static CodigoDeBarras Ler(string codigo)
    {
        var d = Digitos.Apenas(codigo);
        if (d.Length != Tamanho) throw new BoletoInvalidoException($"código de barras deve ter {Tamanho} dígitos; recebeu {d.Length}");
        var esperado = Digitos.Modulo11CodigoDeBarras(d[..4] + d[5..]);
        var dv = d[4] - '0';
        if (dv != esperado) throw new BoletoInvalidoException($"DV do código de barras inválido: esperado {esperado}, encontrado {dv}");
        var fator = int.Parse(d.Substring(5, 4), CultureInfo.InvariantCulture);
        var valor = decimal.Parse(d.Substring(9, 10), CultureInfo.InvariantCulture) / 100m;
        return new CodigoDeBarras(d[..3], d[3], dv, fator, valor, d[19..]);
    }

    public string Texto => Banco + Moeda + Dv + Digitos.Zeros(Fator, 4, "fator") + ValorPara10(Valor) + CampoLivre;

    public DateOnly? Vencimento(DateOnly? referencia = null) => FatorVencimento.Data(Fator, referencia);

    public LinhaDigitavel LinhaDigitavel() => Core.LinhaDigitavel.De(this);

    internal static string ValorPara10(decimal valor) => ((long)decimal.Round(valor * 100m)).ToString(CultureInfo.InvariantCulture).PadLeft(10, '0');

    public override string ToString() => Texto;
}
