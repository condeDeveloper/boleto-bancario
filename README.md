# Boleto Bancário

[![CI](https://github.com/condeDeveloper/boleto-bancario/actions/workflows/ci.yml/badge.svg)](https://github.com/condeDeveloper/boleto-bancario/actions/workflows/ci.yml)

Biblioteca e API em C# e .NET 8 para gerar, ler e validar boletos de cobrança no padrão FEBRABAN. Sem dependências além do ASP.NET Core na API.

- **Código de barras** de 44 posições: banco, moeda, DV módulo 11, fator de vencimento, valor e campo livre.
- **Linha digitável** de 47 posições com os três DVs módulo 10, formatação de impressão e reconstrução do código de barras.
- **Fator de vencimento** com a virada de 22/02/2025 (volta a 1000) e leitura escolhendo o ciclo mais próximo da data de referência, como fazem os bancos.
- **Campo livre** de Banco do Brasil (convênio 7), Bradesco, Itaú (com os DACs), Santander e Caixa (SIGCB), ou campo livre pronto para outros bancos.
- **Código de barras em SVG** na simbologia Interleaved 2 of 5, com a linha digitável impressa embaixo.
- Validação com mensagens específicas: qual DV falhou, campo obrigatório faltando, valor fora do limite.

## Rodar

```bash
dotnet run --project src/Boleto.Api
```

Documentação interativa em http://localhost:5000/docs.

```bash
# gerar um boleto do Bradesco
curl -s localhost:5000/api/boletos -H 'Content-Type: application/json' \
  -d '{"banco":"237","valor":150.75,"vencimento":"2026-10-15","agencia":"1234","carteira":"09","nossoNumero":"12345678901","conta":"1234567"}'

# decodificar uma linha digitável (com ou sem pontos e espaços)
curl -s "localhost:5000/api/boletos/23791.23405%2012345.678901%2012345.670009%201%20160000015075"

# código de barras em SVG
curl -s localhost:5000/api/boletos/23791160000015075123409123456789011234567/svg > boleto.svg
```

## Usar como biblioteca

```csharp
var boleto = BoletoCobranca.Gerar("001", new DateOnly(2026, 10, 15), 1234.56m,
    new DadosBeneficiario(Convenio: "1234567", NossoNumero: "123", Carteira: "17"));

boleto.CodigoDeBarras.Texto;            // 44 dígitos
boleto.LinhaDigitavel.Formatada;        // 00190.00009 01234.567001 00000.012317 1 15710000123456
var lido = BoletoCobranca.Ler("00190.00009 01234.567001 00000.012317 1 15710000123456");
lido.Vencimento();                      // 2026-10-15
CodigoDeBarrasSvg.Gerar(boleto.CodigoDeBarras.Texto);
```

## Testes

```bash
dotnet test
```

Vetores conhecidos do manual FEBRABAN para módulo 10 e módulo 11, fatores de vencimento nas datas-chave (03/07/2000, 21/02/2025, 22/02/2025), ida e volta entre código de barras e linha digitável, layouts de cada banco, detecção de DV adulterado, geometria do ITF e a API de ponta a ponta.

## Licença

MIT
