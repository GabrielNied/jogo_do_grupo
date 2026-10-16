# Simulações de balanceamento

Console .NET que chama as regras puras do `shared` (a mesma conta do servidor), nunca uma cópia delas ([ADR 0043](../../docs/decisoes/0043-simulacoes-em-csharp-e-scripts-em-node.md)). Calibrar uma constante no `shared` muda o jogo e a simulação juntos.

## Atributos e árvore (#627)

```
dotnet run --project tools/JogoDoGrupo.Simulacoes [pasta-de-saida]
```

Sem argumento grava em `tools/JogoDoGrupo.Simulacoes/saida/` (ignorada pelo git):

- `relatorio.md` e `relatorio.html`: orçamento de pontos, builds no nível 100, custo de redistribuir por nível e a conversão dos atributos de item no 0 e no 100. O HTML traz o gráfico do maior atributo por nível.
- `builds-nivel-100.csv`: cada build contra cada conjunto de equipamento (sem, qualidade 60 e 100).
- `por-nivel.csv`: os atributos que cada build tem em cada nível de 1 a 100.

Builds e conjuntos de equipamento de referência ficam em `Program.cs`. Meta que deve virar regressão vira teste, como `NumerosDaArvoreTestes`.
