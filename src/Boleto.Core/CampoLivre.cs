namespace Boleto.Core;

/// <summary>Dados do beneficiário usados para montar o campo livre de 25 posições, que varia por banco.</summary>
public sealed record DadosBeneficiario(
    string? Agencia = null,
    string? Conta = null,
    string? Carteira = null,
    string? NossoNumero = null,
    string? Convenio = null,
    string? CodigoBeneficiario = null,
    string? CampoLivre = null);

/// <summary>Layouts de campo livre dos bancos suportados. Cada um devolve exatamente 25 dígitos.</summary>
public static class CampoLivre
{
    public static readonly IReadOnlyDictionary<string, string> Bancos = new Dictionary<string, string>
    {
        ["001"] = "Banco do Brasil", ["033"] = "Santander", ["104"] = "Caixa Econômica Federal", ["237"] = "Bradesco", ["341"] = "Itaú",
    };

    public static string Montar(string banco, DadosBeneficiario d)
    {
        if (d.CampoLivre is not null)
        {
            var livre = Digitos.Apenas(d.CampoLivre);
            if (livre.Length != 25) throw new BoletoInvalidoException("campo livre informado deve ter 25 dígitos");
            return livre;
        }
        return Digitos.Zeros(banco, 3, "banco") switch
        {
            "001" => BancoDoBrasil(d),
            "033" => Santander(d),
            "104" => Caixa(d),
            "237" => Bradesco(d),
            "341" => Itau(d),
            var b => throw new BoletoInvalidoException($"banco {b} sem layout de campo livre conhecido; informe o campo livre pronto (25 dígitos)"),
        };
    }

    /// <summary>Banco do Brasil, convênio de 7 posições: 000000 + convênio(7) + nosso número(10) + carteira(2).</summary>
    public static string BancoDoBrasil(DadosBeneficiario d) =>
        "000000" + Digitos.Zeros(Exigir(d.Convenio, "convênio"), 7, "convênio") + Digitos.Zeros(Exigir(d.NossoNumero, "nosso número"), 10, "nosso número") + Digitos.Zeros(Exigir(d.Carteira, "carteira"), 2, "carteira");

    /// <summary>Bradesco: agência(4) + carteira(2) + nosso número(11) + conta(7) + 0.</summary>
    public static string Bradesco(DadosBeneficiario d) =>
        Digitos.Zeros(Exigir(d.Agencia, "agência"), 4, "agência") + Digitos.Zeros(Exigir(d.Carteira, "carteira"), 2, "carteira")
        + Digitos.Zeros(Exigir(d.NossoNumero, "nosso número"), 11, "nosso número") + Digitos.Zeros(Exigir(d.Conta, "conta"), 7, "conta") + "0";

    /// <summary>Itaú: carteira(3) + nosso número(8) + DAC(agência+conta+carteira+nosso número) + agência(4) + conta(5) + DAC(agência+conta) + 000.</summary>
    public static string Itau(DadosBeneficiario d)
    {
        var carteira = Digitos.Zeros(Exigir(d.Carteira, "carteira"), 3, "carteira");
        var nn = Digitos.Zeros(Exigir(d.NossoNumero, "nosso número"), 8, "nosso número");
        var agencia = Digitos.Zeros(Exigir(d.Agencia, "agência"), 4, "agência");
        var conta = Digitos.Zeros(Exigir(d.Conta, "conta"), 5, "conta");
        var dacNn = Digitos.Modulo10(agencia + conta + carteira + nn);
        var dacAgConta = Digitos.Modulo10(agencia + conta);
        return carteira + nn + dacNn + agencia + conta + dacAgConta + "000";
    }

    /// <summary>Santander: 9 + código do beneficiário(7) + nosso número(13) + IOS 0 + carteira(3).</summary>
    public static string Santander(DadosBeneficiario d) =>
        "9" + Digitos.Zeros(Exigir(d.CodigoBeneficiario, "código do beneficiário"), 7, "código do beneficiário")
        + Digitos.Zeros(Exigir(d.NossoNumero, "nosso número"), 13, "nosso número") + "0" + Digitos.Zeros(Exigir(d.Carteira, "carteira"), 3, "carteira");

    /// <summary>
    /// Caixa (SIGCB): código do beneficiário(6) + DV + sequência 1 (3) + modalidade (1) + sequência 2 (3) + tipo (1) + sequência 3 (9) + DV do campo livre.
    /// Nosso número de 17 posições no formato modalidade(1) + tipo(1) + sequência(15).
    /// </summary>
    public static string Caixa(DadosBeneficiario d)
    {
        var codigo = Digitos.Zeros(Exigir(d.CodigoBeneficiario, "código do beneficiário"), 6, "código do beneficiário");
        var nn = Digitos.Zeros(Exigir(d.NossoNumero, "nosso número"), 17, "nosso número");
        var modalidade = nn[..1];
        var tipo = nn[1..2];
        var seq = nn[2..];
        var dvCodigo = Digitos.Modulo11Padrao(codigo);
        var semDv = codigo + dvCodigo + seq[..3] + modalidade + seq[3..6] + tipo + seq[6..];
        return semDv + Digitos.Modulo11Padrao(semDv);
    }

    private static string Exigir(string? valor, string campo) => string.IsNullOrWhiteSpace(valor) ? throw new BoletoInvalidoException($"{campo} é obrigatório para este banco") : valor;
}
