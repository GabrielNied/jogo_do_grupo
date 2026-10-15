---
name: segure
description: Reserva manualmente a fila de um escopo (prefixo [Personagem], [Itens], etc.) para quem rodou o comando, bloqueando puxar por outros. Use para "/segure personagem" ou trabalho manual num bloco. Não implementa jogo.
---

# Segure

Reserva tarefas **livres** de um escopo para o login GitHub de quem executou. Agentes de IA só podem `puxar` se rodarem com o **mesmo** `gh auth` do dono. O `/decida` **não remove** rótulos `segure:*`.

## Ativar

```
npm run tarefa -- segure <escopo>
```

- `<escopo>`: igual ao commitlint (`personagem`, `mundo`, `itens`, …), alinhado ao prefixo `[Personagem]` no título.
- Rotula issues abertas **sem** `em-andamento` cujo prefixo (ou label da funcionalidade) bate.
- **Não** interrompe quem já está `em-andamento`.

## Manter

```
npm run tarefa -- segure status
npm run tarefa -- segure rotular [#N ...]
npm run tarefa -- segure liberar <escopo>
```

- Depois de `gh issue create` (/melhore, /decida): `segure rotular <N>` (ou `rotular` sem número para varrer abertas).
- Subtarefas criadas por `npm run tarefa -- subtarefa` já herdam o label quando o bloco está ativo.
- Só o **dono** libera o escopo.

## Relatório

Escopos ativos, quantas issues rotuladas, lembrete de `rotular` para issues novas.
