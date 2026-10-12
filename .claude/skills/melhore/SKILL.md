---
name: melhore
description: Analisa o que já está implementado no Jogo do Grupo, pesquisa como melhorar e abre a tarefa com essa explicação. Use quando pedirem "/melhore", "melhorar o que já existe" ou "criar tarefas de melhoria". Não implementa.
---

# Melhore (tarefas do que já existe)

Você analisa o que já está no jogo, pesquisa como melhorar e abre a tarefa com essa explicação. Não implementa. Não puxe as issues que criar e não escreva código de client, server ou shared. Implementar é o `/trabalhe`. Organizar decisão pendente é o `/decida`.

`/melhore` já autoriza escolher e abrir as issues. Não pergunte o que melhorar, não ofereça uma lista para a pessoa escolher e não espere OK.

Argumentos opcionais (todos podem faltar):

- **Filtro** (`itens`, `mundo`): só essa funcionalidade.
- **N** (`5`): máximo de tarefas novas. Sem N, use 5.

## 1. Ver o que já está feito

Na `origin/main`, leia `docs/features/` e o código de `Features/` das funcionalidades que já têm cena, regra ou fluxo no jogo. Confira as issues abertas para não repetir. O [foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) vem primeiro. Ideia do livro que ainda não tem código não entra.

## 2. Achar a melhoria

Uma melhoria é um buraco numa coisa que já existe. A régua é a [entrega](../../../docs/agentes.md#entrega-da-funcionalidade): ponto de entrada, o que a pessoa faz, o que aparece, vazio, erro, limite, texto em português e o visual do [Lumora](../../../design/interface/GUIA-DE-ESTILO.md).

Prefira, nesta ordem: o jogador não consegue completar o que a doc diz que existe; a tela está crua ou sem estado; o fluxo quebra num canto da própria regra. Não peça sistema novo. Não reabra o que está marcado como Definido.

## 3. Pesquisar como melhorar

Antes de abrir a issue, leia o código e a doc do que já existe e escreva o caminho. Isso é a pré-pesquisa: o `/trabalhe` implementa a partir dela, sem refazer a leitura.

Para cada melhoria, deixe claro:

- **Onde está:** arquivos e o fluxo atual (quem manda, quem mostra).
- **O que falta:** o buraco, em uma frase.
- **Como fazer:** a mudança concreta (tela, estado, mensagem ou regra), no padrão que o código ao redor já usa. Sem código novo. Sem passo que implemente.
- **O que não refazer:** o que já funciona e permanece.

Se a pesquisa mostrar que a melhoria é outra funcionalidade ou uma decisão ainda aberta, não abra a issue: isso é `/decida` ou fica de fora.

## 4. Abrir as tarefas

Até N issues, as piores primeiro. Cada uma é uma melhoria só, de uma funcionalidade só.

Título: `[<Funcionalidade>] Melhoria: <o que o jogador passa a conseguir>`.

Corpo:

- **Hoje:** o que já está na `main`, em uma ou duas frases.
- **Cena:** o que a pessoa faz e o que passa a aparecer.
- **Como melhorar:** a pré-pesquisa do passo 3 (onde está, o que falta, como fazer, o que não refazer).
- **Pronto quando:** a cena está no jogo, com vazio, erro e limite.

Use a label da funcionalidade se ela já existir. Crie com um rótulo `prio:*` ([Prioridade das tarefas](../../../docs/agentes.md#prioridade-das-tarefas)): `prio:1-erros` se o fluxo quebra ou trava, `prio:2-core-loop` se o buraco está em mobs, combate, XP ou loot, `prio:3-experiencia` se é tela crua, sem estado ou sem feedback. Não atribua e não marque `em-andamento`. Crie com `gh issue create`.

## 5. Relatório

Liste as issues criadas (número e uma linha) e o que você olhou e deixou quieto, porque já estava bom ou já tinha tarefa.
