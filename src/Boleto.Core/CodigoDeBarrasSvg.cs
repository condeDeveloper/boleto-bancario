using System.Globalization;
using System.Text;

namespace Boleto.Core;

/// <summary>
/// Renderiza o código de barras em SVG usando a simbologia Interleaved 2 of 5 (ITF), exigida pela FEBRABAN:
/// cada par de dígitos vira cinco barras (primeiro dígito) intercaladas com cinco espaços (segundo dígito).
/// </summary>
public static class CodigoDeBarrasSvg
{
    // n = estreito, w = largo
    private static readonly string[] Padroes = { "nnwwn", "wnnnw", "nwnnw", "wwnnn", "nnwnw", "wnwnn", "nwwnn", "nnnww", "wnnwn", "nwnwn" };
    private const string Inicio = "nnnn"; // barra, espaço, barra, espaço
    private const string Fim = "wnn";     // barra larga, espaço, barra

    /// <summary>Sequência de larguras (1 = estreito, 3 = largo) alternando barra/espaço, começando por barra.</summary>
    public static IReadOnlyList<int> Larguras(string codigo, int razao = 3)
    {
        Digitos.SoDigitos(codigo);
        if (codigo.Length % 2 != 0) codigo = "0" + codigo;
        var l = new List<int>();
        foreach (var c in Inicio) l.Add(c == 'w' ? razao : 1);
        for (var i = 0; i < codigo.Length; i += 2)
        {
            var barras = Padroes[codigo[i] - '0'];
            var espacos = Padroes[codigo[i + 1] - '0'];
            for (var k = 0; k < 5; k++)
            {
                l.Add(barras[k] == 'w' ? razao : 1);
                l.Add(espacos[k] == 'w' ? razao : 1);
            }
        }
        foreach (var c in Fim) l.Add(c == 'w' ? razao : 1);
        return l;
    }

    public static string Gerar(string codigo, int alturaPx = 50, double larguraEstreitaPx = 1.2, bool comTexto = true)
    {
        var larguras = Larguras(codigo);
        var totalUnidades = larguras.Sum();
        var margem = 10 * larguraEstreitaPx;
        var largura = totalUnidades * larguraEstreitaPx + margem * 2;
        var altura = alturaPx + (comTexto ? 16 : 0);
        var sb = new StringBuilder();
        var inv = CultureInfo.InvariantCulture;
        sb.Append(inv, $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{largura:0.##}\" height=\"{altura}\" viewBox=\"0 0 {largura:0.##} {altura}\" shape-rendering=\"crispEdges\">");
        sb.Append(inv, $"<rect width=\"{largura:0.##}\" height=\"{altura}\" fill=\"#fff\"/>");
        var x = margem;
        for (var i = 0; i < larguras.Count; i++)
        {
            var w = larguras[i] * larguraEstreitaPx;
            if (i % 2 == 0) sb.Append(inv, $"<rect x=\"{x:0.##}\" y=\"0\" width=\"{w:0.##}\" height=\"{alturaPx}\" fill=\"#000\"/>");
            x += w;
        }
        if (comTexto)
        {
            var texto = codigo.Length == 44 ? LinhaDigitavel.De(CodigoDeBarras.Ler(codigo)).Formatada : codigo;
            sb.Append(inv, $"<text x=\"{largura / 2:0.##}\" y=\"{alturaPx + 12}\" font-family=\"monospace\" font-size=\"10\" text-anchor=\"middle\" fill=\"#000\">{texto}</text>");
        }
        sb.Append("</svg>");
        return sb.ToString();
    }
}
