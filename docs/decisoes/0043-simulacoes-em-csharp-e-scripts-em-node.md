# 0043. Simulações em C# com o shared e scripts avulsos em Node

**Data:** 2026-10-16
**Status:** aceita

## Contexto

O balanceamento vai precisar de simulação: quantas fusões e coroas até +10, quanto tempo até o nível 50, fluxo de coroas por hora contra as saídas, drop por hora. A pergunta era se valia instalar Python para isso e para os scripts que os agentes rodam (editar docs em lote, analisar arquivos). Na máquina do Gabriel não há Python (`python` cai no atalho da Microsoft Store), e um agente perdeu tempo tentando usá-lo.

Opções: Python (pandas e matplotlib prontos, mas exige instalar e reescreve as fórmulas do jogo), C# com as regras do `shared` (a mesma conta do servidor) e Node (já é a linguagem dos scripts do repositório).

## Decisão

1. **Simulação de balanceamento em C#**, chamando as regras puras do `shared` (`RegrasFusao`, `ConfigExperiencia`, `RegrasAmeaca` e as outras), nunca uma cópia das fórmulas. Quando a primeira chegar, é um console .NET em `tools/JogoDoGrupo.Simulacoes/` (como os [robôs de carga](0042-robos-de-carga-em-tools.md)), rodado com `dotnet run`, que grava o CSV e uma página HTML com o gráfico em SVG embutido, gerada pelo próprio console, sem biblioteca externa, para abrir no navegador. Meta que deve virar regressão continua como teste, no estilo de `RitmoDeProgressaoTestes`.
2. **Script avulso de agente em Node** (`.mjs` escrito pela ferramenta de escrita, não em heredoc), como os de `scripts/`.
3. **Sem Python.** Nenhum script, hook, simulação ou CI depende dele; o gráfico sai do HTML da simulação, e o CSV abre numa planilha.

## Consequências

- A simulação não diverge do jogo: calibrar uma constante muda o servidor e a simulação juntos.
- Nada novo para instalar; `dotnet` e Node já são exigidos.
- O gráfico é simples (linha e barra em SVG, escrito à mão no console); análise mais sofisticada fica na planilha.
- Fórmula que só existe no servidor precisa ir para o `shared` antes de ser simulada.
