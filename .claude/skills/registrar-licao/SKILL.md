---
name: registrar-licao
description: Transforma um tropeço de processo (comando que travou, trabalho perdido, retrabalho de agente, ferramenta que enganou) em script, hook ou regra curta, e registra em docs/licoes.md só o que ainda não virou. Use quando algo deu errado no fluxo ou pedirem para documentar o que se aprendeu.
---

# Registrar lição

Um tropeço não se repete quando uma máquina o impede. Texto é o último recurso: cada linha a mais nas instruções é paga em toda rodada de todo agente ([orçamento de contexto](../../../docs/agentes.md#orçamento-de-contexto)).

**Quando:** algo travou, sumiu ou foi refeito; uma ferramenta respondeu errado; você contornou algo que vai voltar. Dúvida de design ou estilo não é lição: vai em `docs/features/` ou em [convenções](../../../docs/convencoes.md).

## Passos, na ordem: pare no primeiro que resolve

1. **Script ou hook** que impede o erro ou faz o passo sozinho (regra 16), no mesmo PR, com teste se for script. Feito isso, não escreva lição: o commit conta a história.
2. **Regra curta onde ela será lida** (uma linha em [agentes.md](../../../docs/agentes.md), [git.md](../../../docs/git.md), `docs/arte/` ou na skill do passo), sem copiar texto entre docs.
3. **Entrada em [licoes.md](../../../docs/licoes.md)** só se nenhum dos dois cabe agora (é grande, ou é diagnóstico que ninguém automatiza): `### AAAA-MM-DD · título`, com **O que houve**, **Regra** e **Origem** (PR ou issue). Se for grande, abra também uma issue com o rótulo `desenvolvimento`.

Antes, veja se a lição já existe e reforce em vez de duplicar. Lição que virou script ou regra sai do licoes.md no mesmo PR.

**Entregue** na branch da tarefa; se o PR já foi mergeado, parta de `origin/main` num PR pequeno. Só Markdown: `git diff --check`.
