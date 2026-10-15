---
name: melhore
description: Procura melhorias no que já existe no Jogo do Grupo (produto, qualidade de código, desempenho do jogo e custo das IAs), com evidência, e abre as issues já com a ficha para o /trabalhe. Use para "/melhore" ou "criar tarefas de melhoria". Não implementa.
---

# Melhore

Você procura o que melhorar no que **já existe** e abre a tarefa com o caminho pesquisado. Não implementa, não puxa as issues que criar. `/melhore` já autoriza escolher e abrir; não pergunte, não ofereça lista.

Argumentos (opcionais): **filtro** de funcionalidade (`itens`) ou de lente (`produto`, `codigo`, `desempenho`, `ia`), e **N** issues novas (padrão 5, as piores primeiro, somando as lentes).

## 1. Olhar, lente por lente

- [Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais): `git fetch origin main` antes de qualquer lente. Doc e regras: `git show origin/main:…`. Código: `git grep` em `origin/main` (build local só se o checkout já estiver alinhado).
- Leia só as fontes da lente. O [foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) vem primeiro (pela `origin/main`). Ideia sem código não entra.

- **Produto:** funcionalidades com cena no jogo contra a [entrega](../../../docs/agentes.md#entrega-da-funcionalidade) e a checklist de experiência. Busque, nesta ordem: o jogador não completa o que a doc diz que existe; tela crua ou sem estado; fluxo que quebra num canto da própria regra.
- **Código:**
  - regras 2, 3 e 5 violadas (`using` de outra `Features`, `UnityEngine` no shared, resultado decidido no client);
  - contrato ou regra de resultado sem teste;
  - arquivo grande demais ou duplicação entre funcionalidades;
  - `TODO`/`FIXME`;
  - avisos de build.
- **Desempenho:** alocação por quadro ou por pacote, laço de tick do servidor, consulta dentro de laço, carga de cena. Só com o trecho que prova.
- **Custo de IA:**
  - `npm run contexto` (instrução que passou do teto ou cresceu);
  - `gh run list --status failure --limit 30` (CI que falha e repete);
  - PRs recentes com várias rodadas de "fix";
  - pendências de Unity que voltam;
  - lições recentes em [licoes.md](../../../docs/licoes.md) que ainda não viraram script ou hook;
  - skill ou agente que relê o que a issue já dizia.

  Corte pequeno é regra 16 (faz-se na hora, num PR de processo): só vira issue o que não cabe num PR pequeno.

Não reabra o que está **Definido** e não peça sistema novo: isso é `/decida` ou fica de fora.

## 2. Evidência e duplicata

Cada melhoria tem evidência com link: `arquivo:linha`, run do CI ou PR. Sem evidência, não abre. Antes de criar, busque entre as abertas e as fechadas recentes (`gh issue list --state all --search "<termos>"`). Se já existe aberta, acrescente a evidência nela em vez de abrir outra.

## 3. Abrir

Até N issues, uma melhoria de uma área cada. Título `[<Funcionalidade>] Melhoria: <o que passa a acontecer>`. O corpo é a [ficha](../../../docs/agentes.md#ficha-da-tarefa), com **Hoje** (o que existe), **Onde mexer** (arquivos e fluxo, para o `/trabalhe` não refazer a leitura), **Fora do escopo** (o que já funciona e fica) e a linha **Evidência:**. Melhorias no mesmo código se citam em **Lote sugerido**.

Rótulos:

- a funcionalidade, se o rótulo existir;
- `prio:*`: `prio:1-erros` se quebra ou trava, `prio:2-core-loop` se está em mobs, combate, XP ou loot, `prio:3-experiencia` se é tela crua ou desempenho percebido, `prio:6-seguranca` se é abuso;
- `qualidade` nas lentes de código, desempenho e IA;
- `desenvolvimento` na lente de IA.

Não atribua nem marque `em-andamento`. Logo depois de criar, `npm run tarefa -- relevancia <N> --conferida "aberta pelo /melhore"`, para o `/decida` não reconferir.

## 4. Relatório

As issues criadas (número, lente e uma linha) e o que você olhou e deixou quieto (já bom, já tinha issue ou sem evidência).
