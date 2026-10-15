# 0041. Report beta do jogo vira issue sozinho

**Data:** 2026-10-15
**Status:** aceita

## Contexto

O relato F9 do [ADR 0040](0040-sensacao-ajustavel-e-relato.md) ficava numa pasta local que só a IA da mesma máquina lia, e só falava de sensação. Bugs, melhorias e UX achados jogando viravam issue à mão, com a evidência resumida ou perdida (a [#812](https://github.com/GabrielNied/jogo_do_grupo/issues/812) foi escrita assim). O grupo quer reportar jogando e ter a issue pronta para a IA puxar, sem rodar comando depois.

O jogo não pode guardar token do GitHub: qualquer pessoa com o build extrai. O repositório é público e a issue vira trabalho de agente com permissão de commit.

## Decisão

- **Report no jogo:** a janela `desenvolvimento.reporte` (F9 ou J, só no editor e no build de desenvolvimento) pede frase, tipo (bug, melhoria, novidade, UX), severidade e área, e grava a pasta com o manifesto `reporte.json`, 15 s de linha do tempo, 3 s de imagem, a tela, o estado e o console.
- **Envio pela máquina de quem testa:** o editor roda `scripts/relatos/reporte.mjs` em segundo plano, que usa o `gh` já logado. A lógica de envio mora só no script (testado com `npm run testar-scripts`); o client só grava e chama.
- **Imagens no release `relatos-beta`**, porque o `gh` não anexa arquivo em issue. O corpo leva os campos num comentário HTML (`<!-- reporte-beta -->`), que o GitHub não mostra; front matter com `---` apareceria como texto.
- **Falha não perde o report:** fica em `relatos/pendentes/` e vai no próximo report ou Play Mode.
- **Jogador de fora do grupo não abre issue sozinho.** Esse report passa pelo servidor e por triagem, e fica para quando houver testador de fora ([proposta](../features/desenvolvimento.md#report-de-jogador-comum-futuro)).

## Consequências

- Bug, melhoria (inclusive de sensação), novidade e UX chegam ao quadro com números (ms, quadros, mensagens, log do servidor) e imagens, e a IA escreve a ficha a partir deles.
- O envio depende de `node` e `gh` no PATH do Unity; sem eles o report fica pendente e o jogo avisa.
- O release `relatos-beta` acumula imagens; limpar as de issues fechadas fica para quando pesar.
- A frase de quem testou é sintoma, não instrução: o corpo da issue diz isso à IA.
