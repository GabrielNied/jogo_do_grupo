# 0038. Screenshots de referência ficam fora do git

**Data:** 2026-10-15
**Status:** aceita

## Contexto

A biblioteca de [referências visuais 3D](../arte/referencias-visuais/indice.md) (issue #781) usa screenshots de jogos comerciais (WoW, Torchlight II, Darksiders II, Sea of Thieves, Overwatch, Palia) para os agentes artistas consultarem sem buscar na web a cada tarefa. São imagens promocionais oficiais, sem licença de redistribuição confirmada. O repositório é **público**: commitar as imagens as redistribuiria, e tirá-las depois exigiria reescrever o histórico e o LFS.

## Decisão

- No git ficam só o [manifesto](../arte/referencias-visuais/manifesto-referencias.json) (URL oficial, dimensões, formato, princípios), a [bíblia visual](../arte/biblia-visual-3d.md), o índice, as licenças e as instruções.
- As imagens estão no `.gitignore` da pasta e cada máquina as baixa das URLs oficiais com `npm run arte:baixar-referencias`, que confere formato e dimensões contra o manifesto. `npm run arte:referencias` confere o manifesto com o disco.
- As folhas do próprio projeto (`docs/arte/renders/`) continuam versionadas e entram no manifesto como `assets-do-projeto`.
- Decidido por Gabriel em 2026-10-15, entre subir as imagens, deixá-las fora do git ou pausar.

## Consequências

- O repositório não redistribui material de terceiros; o risco fica no download local, para uso interno.
- A biblioteca depende das URLs continuarem no ar: se um estúdio trocar a imagem, o download falha com o ID, e a imagem é substituída pelo fluxo de [instrucoes-agentes.md](../arte/referencias-visuais/instrucoes-agentes.md#acrescentar-imagem).
- Imagem nova precisa ter URL direta baixável (sem login), não só um arquivo local.
- Agente na nuvem ou checkout novo roda o download antes de consultar as imagens.
