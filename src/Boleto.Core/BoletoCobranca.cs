namespace Boleto.Core;

/// <summary>Boleto pronto: código de barras, linha digitável e dados legíveis.</summary>
public sealed record BoletoCobranca(CodigoDeBarras CodigoDeBarras, LinhaDigitavel LinhaDigitavel)
{
    public string Banco => CodigoDeBarras.Banco;
    public string NomeBanco => CampoLivre.Bancos.GetValueOrDefault(Banco, "Banco " + Banco);
    public decimal Valor => CodigoDeBarras.Valor;
    public DateOnly? Vencimento(DateOnly? referencia = null) => CodigoDeBarras.Vencimento(referencia);

    /// <summary>Gera um boleto a partir dos dados do beneficiário e do título.</summary>
    public static BoletoCobranca Gerar(string banco, DateOnly? vencimento, decimal valor, DadosBeneficiario beneficiario)
    {
        var livre = CampoLivre.Montar(banco, beneficiario);
        var barras = CodigoDeBarras.Montar(banco, vencimento, valor, livre);
        return new BoletoCobranca(barras, barras.LinhaDigitavel());
    }

    /// <summary>Interpreta uma linha digitável (47) ou um código de barras (44), com ou sem pontuação.</summary>
    public static BoletoCobranca Ler(string texto)
    {
        var d = Digitos.Apenas(texto);
        return d.Length switch
        {
            LinhaDigitavel.Tamanho => new BoletoCobranca(LinhaDigitavel.Ler(d).ParaCodigoDeBarras(), LinhaDigitavel.Ler(d)),
            CodigoDeBarras.Tamanho => new BoletoCobranca(CodigoDeBarras.Ler(d), CodigoDeBarras.Ler(d).LinhaDigitavel()),
            _ => throw new BoletoInvalidoException($"informe a linha digitável (47 dígitos) ou o código de barras (44); recebeu {d.Length}"),
        };
    }

    /// <summary>Tenta ler sem lançar; devolve o motivo em caso de falha.</summary>
    public static (BoletoCobranca? Boleto, string? Erro) TentarLer(string texto)
    {
        try { return (Ler(texto), null); }
        catch (BoletoInvalidoException e) { return (null, e.Message); }
    }
}
