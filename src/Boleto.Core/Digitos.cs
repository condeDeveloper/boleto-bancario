namespace Boleto.Core;

/// <summary>Dígitos verificadores e utilidades numéricas do padrão FEBRABAN.</summary>
public static class Digitos
{
    /// <summary>
    /// Módulo 10: pesos 2 e 1 alternados da direita para a esquerda; produtos maiores que 9 têm os
    /// algarismos somados. DV = (10 − soma mod 10) mod 10. Usado nos três primeiros campos da linha digitável.
    /// </summary>
    public static int Modulo10(string numero)
    {
        SoDigitos(numero);
        var soma = 0;
        var peso = 2;
        for (var i = numero.Length - 1; i >= 0; i--)
        {
            var produto = (numero[i] - '0') * peso;
            soma += produto > 9 ? produto - 9 : produto;
            peso = peso == 2 ? 1 : 2;
        }
        return (10 - soma % 10) % 10;
    }

    /// <summary>
    /// Módulo 11 do código de barras: pesos de 2 a 9 cíclicos da direita para a esquerda.
    /// DV = 11 − (soma mod 11); resultados 0, 10 e 11 viram 1.
    /// </summary>
    public static int Modulo11CodigoDeBarras(string numero)
    {
        var resto = Modulo11Soma(numero) % 11;
        var dv = 11 - resto;
        return dv is 0 or 10 or 11 ? 1 : dv;
    }

    /// <summary>Módulo 11 "bancário" (pesos 2 a 9) usado em nosso número de vários bancos: 0 e 1 viram 0 (ou letra, conforme o banco).</summary>
    public static int Modulo11Padrao(string numero, int resultadoPara0e1 = 0)
    {
        var resto = Modulo11Soma(numero) % 11;
        var dv = 11 - resto;
        return dv >= 10 ? resultadoPara0e1 : dv;
    }

    private static int Modulo11Soma(string numero)
    {
        SoDigitos(numero);
        var soma = 0;
        var peso = 2;
        for (var i = numero.Length - 1; i >= 0; i--)
        {
            soma += (numero[i] - '0') * peso;
            peso = peso == 9 ? 2 : peso + 1;
        }
        return soma;
    }

    /// <summary>Preenche com zeros à esquerda; lança se exceder o tamanho.</summary>
    public static string Zeros(string valor, int tamanho, string campo)
    {
        var d = Apenas(valor);
        if (d.Length > tamanho) throw new BoletoInvalidoException($"{campo} tem {d.Length} dígitos; o máximo é {tamanho}");
        return d.PadLeft(tamanho, '0');
    }

    public static string Zeros(long valor, int tamanho, string campo) => Zeros(valor.ToString(), tamanho, campo);

    /// <summary>Remove tudo que não for dígito.</summary>
    public static string Apenas(string? s) => new((s ?? string.Empty).Where(char.IsDigit).ToArray());

    public static void SoDigitos(string s)
    {
        if (string.IsNullOrEmpty(s) || !s.All(char.IsDigit)) throw new BoletoInvalidoException("esperava apenas dígitos");
    }
}

public sealed class BoletoInvalidoException : Exception
{
    public BoletoInvalidoException(string mensagem) : base(mensagem) { }
}
