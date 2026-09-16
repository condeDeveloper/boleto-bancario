using System.Globalization;

namespace Boleto.Core;

/// <summary>
/// Linha digitável de 47 posições, derivada do código de barras:
/// campo 1 = banco + moeda + livre[0..5) + DV10 · campo 2 = livre[5..15) + DV10 · campo 3 = livre[15..25) + DV10 ·
/// campo 4 = DV geral · campo 5 = fator + valor.
/// </summary>
public sealed record LinhaDigitavel(string Campo1, string Campo2, string Campo3, string Campo4, string Campo5)
{
    public const int Tamanho = 47;

    public static LinhaDigitavel De(CodigoDeBarras b)
    {
        var t = b.Texto;
        var c1 = t[..4] + b.CampoLivre[..5];
        var c2 = b.CampoLivre[5..15];
        var c3 = b.CampoLivre[15..25];
        return new LinhaDigitavel(c1 + Digitos.Modulo10(c1), c2 + Digitos.Modulo10(c2), c3 + Digitos.Modulo10(c3), b.Dv.ToString(), t.Substring(5, 14));
    }

    /// <summary>Lê os 47 dígitos (com ou sem pontuação), valida os três DVs de campo e o DV geral, e reconstrói o código de barras.</summary>
    public static LinhaDigitavel Ler(string linha)
    {
        var d = Digitos.Apenas(linha);
        if (d.Length != Tamanho) throw new BoletoInvalidoException($"linha digitável deve ter {Tamanho} dígitos; recebeu {d.Length}");
        var l = new LinhaDigitavel(d[..10], d[10..21], d[21..32], d[32..33], d[33..47]);
        l.ValidarCampos();
        l.ParaCodigoDeBarras(); // valida o DV geral
        return l;
    }

    private void ValidarCampos()
    {
        foreach (var (campo, n) in new[] { (Campo1, 1), (Campo2, 2), (Campo3, 3) })
        {
            var corpo = campo[..^1];
            var dv = campo[^1] - '0';
            var esperado = Digitos.Modulo10(corpo);
            if (dv != esperado) throw new BoletoInvalidoException($"DV do campo {n} inválido: esperado {esperado}, encontrado {dv}");
        }
    }

    public CodigoDeBarras ParaCodigoDeBarras()
    {
        var livre = Campo1[4..9] + Campo2[..10] + Campo3[..10];
        var texto = Campo1[..4] + Campo4 + Campo5 + livre;
        return CodigoDeBarras.Ler(texto);
    }

    public string Texto => Campo1 + Campo2 + Campo3 + Campo4 + Campo5;

    /// <summary>Formato de impressão: AAABC.CCCCX DDDDD.DDDDDY EEEEE.EEEEEZ K UUUUVVVVVVVVVV</summary>
    public string Formatada => $"{Campo1[..5]}.{Campo1[5..]} {Campo2[..5]}.{Campo2[5..]} {Campo3[..5]}.{Campo3[5..]} {Campo4} {Campo5}";

    public decimal Valor => decimal.Parse(Campo5[4..], CultureInfo.InvariantCulture) / 100m;

    public override string ToString() => Formatada;
}
