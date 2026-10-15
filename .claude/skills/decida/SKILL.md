---
name: decida
description: Confere as tarefas abertas do Jogo do Grupo contra a doc atual (as antigas primeiro), fecha o feito ou obsoleto, ajusta a prioridade, escreve a ficha no corpo e tira bloqueios, para o /trabalhe implementar sem reler. Use para "/decida", "organizar as tasks" ou "destravar". Não implementa.
---

# Decida

Você confere e organiza o quadro. Não escreve código de jogo, não puxa issue e não abre PR de funcionalidade. Decisão que vira regra vai para `docs/features/` num PR de docs. `/decida` já autoriza fechar, reescrever, rotular e decidir; não pergunte, não ofereça cardápio.

Argumentos (opcionais): **N** issues a conferir (padrão 15) e um **filtro** de funcionalidade.

## 1. Ler

- [Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais): `git fetch origin main` antes de conferir issues ou editar doc. Vereditos e fichas seguem a `origin/main` (`git show`); trechos de `docs/features/` que você citar ou reescrever vêm dali.
- `npm run tarefa -- revisar <N>`: as livres há mais tempo sem conferência, com idade, prioridade e sinais (`sem ficha`, `rótulos v1`, `⏳ depende de`, `doc mudou Nx`, `decisao`, `bloqueada`).
- `gh pr list` (PR aberto que já cumpre uma issue), o [foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) e a [visão](../../../docs/visao-geral.md).
- Por issue: o corpo, e o `git diff` que o `relevancia <N>` mostra quando a doc mudou. Abra a doc da funcionalidade ou o código só para decidir o veredito.

## 2. Veredito, uma issue por vez

- **Feita** (PR ou commit já entregou): `gh issue close <N> --reason completed` com o link.
- **Obsoleta** (a doc, um ADR ou a visão mudou o que ela pede): `--reason "not planned"` com o trecho ou commit.
- **Duplicada:** `--reason "not planned"` com "Duplicada de #M"; o que só ela tinha vai para a que fica.
- **Válida:**
  - Reescreva o corpo como [ficha](../../../docs/agentes.md#ficha-da-tarefa) (`gh issue edit <N> --body-file`), com o original num `<details>` no fim. A doc atual vence a issue.
  - `prio:*` pela [ordem](../../../docs/agentes.md#prioridade-das-tarefas); `foco` se está no foco do marco, sem ele se não está.
  - Rótulos `agente:shared-contratos`, `server-funcionalidade`, `client-funcionalidade`, `regras-do-jogo` saem; entra o dono (`agente:funcionalidade`, `itens`, `economia`, `server-core`, `client-core` ou `artista-*`). Tarefa só de pessoa leva `pessoa`.
  - `Depende de:` e `Lote sugerido:` (issues que mexem no mesmo código) na ficha.
- **Épico grande:** fica como épico, com "Tarefas, nesta ordem". Sem recorte jogável aberto, crie o próximo (`gh issue create`, já com ficha e `prio:*`).
- Em todos os casos: `npm run tarefa -- relevancia <N> --conferida "<veredito em uma linha>"`, para a próxima varredura pular a issue.

## 3. Destravar

Para `bloqueada`, `decisao` ou dúvida em aberto que muda o resultado, nesta ordem:

1. A doc ou um ADR já respondeu: comente o trecho e tire o rótulo.
2. A issue traz proposta alinhada à visão e ao foco: adote, registre em `docs/features/<x>.md` (fonte única), comente o que vale e tire o rótulo.
3. Mudaria algo **Definido**: não mude; deixe a proposta escrita e siga com o definido.

Prefira o que é fácil de mudar (constante nomeada, regra 17). `bloqueada` cuja dependência fechou: tire o rótulo.

## 4. Pesquisar como implementar (só se a escolha técnica muda o resultado)

Biblioteca, algoritmo ou padrão do Unity/.NET em aberto:

1. Primeiro o repo (ADR, `shared/`, `Core/`).
2. Depois até 2 buscas na web.

Filtros: licença MIT/Apache/BSD (assets CC0/CC-BY), Unity 6000.6 e C# 9 se tocar client ou shared, mantida, sem tirar autoridade do servidor nem ligar funcionalidades. Registre a escolha na ficha ("Onde mexer"). Dependência nova vira ADR no PR de docs. Não instale nem prototipe.

## 5. Entregar

Doc mudou: um PR `docs` (skill [preparar-pr](../preparar-pr/SKILL.md)), sem esperar pedido. Relatório curto:

- fechadas, por motivo;
- fichadas, na ordem em que o `/trabalhe` vai pegar, com os lotes sugeridos;
- destravadas e a decisão de cada uma;
- o que continua parado e por quê.
