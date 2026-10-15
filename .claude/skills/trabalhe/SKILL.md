---
name: trabalhe
description: Implementa tarefas do Jogo do Grupo do começo ao PR, na ordem de prioridade, juntando num lote (um agente, um PR) as que mexem no mesmo código. Use para "/trabalhe", "pegar as próximas tarefas", "implemente a issue X" ou "coloque os agentes para trabalhar". Não pede input humano.
---

# Trabalhe

Você é o **coordenador**: escolhe, agrupa, puxa, lança os donos, junta e entrega. `/trabalhe` já autoriza tudo isso; não pergunte o que puxar, não peça confirmação, não ofereça alternativas. Conferir e organizar o quadro é o `/decida`; abrir melhorias é o `/melhore`.

Argumentos (todos opcionais):

- **Issues** (`#123 #130`): formam o lote; pule o passo 2.
- **Filtro** (`mundo`, `bugs`): restringe a escolha.
- **N** (`2`): máximo de lotes (agentes) em paralelo. Sem N, 3.

## 1. Estado inicial

- `git status`. Mudança que não é sua: não leve e não apague; cite no relatório. Branch sua com commit sem PR: abra o PR (regra 12) e siga.
- [Doc e regras atuais](../../../docs/agentes.md#doc-e-regras-atuais): `git fetch origin main` antes do `listar`, de escrever ficha ou de decidir design. Leia `docs/` e `AGENTS.md` pela `origin/main` (`git show`), não assuma que os arquivos locais já refletem o remoto.
- Na nuvem, `npm run nuvem:preparar`. Pendência de Unity do hook de início é candidata se houver Unity na máquina.

## 2. Escolher pela ordem

`npm run tarefa -- listar`. A lista "Disponíveis" já sai na [ordem de prioridade](../../../docs/agentes.md#prioridade-das-tarefas) e marca o que não entra (`⏳ depende de`, `decisao`) e o que falta (`sem ficha`, `rótulos v1`). Pegue de cima para baixo.

Tarefa sem ficha: se for pequena e clara, escreva a [ficha](../../../docs/agentes.md#ficha-da-tarefa) no corpo antes de lançar. Épico (`planejamento`, sem cena): pegue o próximo recorte jogável listado nele; se não houver, pule (é do `/decida`).

## 3. Montar os lotes

- A **semente** é a melhor tarefa livre ainda sem lote.
- Entram junto **todas** as livres com afinidade de código com ela: mesmos arquivos ou fluxo, dependência, "Lote sugerido" da ficha. Pode cruzar funcionalidades e não tem limite; tarefa de prioridade menor entra se mexe no mesmo código (reabrir depois custa mais).
- O próximo lote parte da melhor que sobrou e não pode dividir arquivos com os já montados. Pare em N lotes ou no limite de 3 lotes de jogo em andamento.
- Dono e regras do lote: [Lote](../../../docs/agentes.md#lote). Lote pequeno (docs, config, até ~50 linhas cada) você faz direto, sem agente.

Mostre a tabela (lote, issues, funcionalidades, dono, por que) e siga no mesmo turno. O que a doc deixa em aberto, o dono decide ([Decisões de design](../../../docs/agentes.md#decisões-de-design)). Decisão que mudaria algo **Definido**: tire a issue do lote, comente nela e siga.

## 4. Lançar os donos

Lote que precisa de algo novo em Core: lance `server-core`/`client-core` antes e espere o merge. Os outros donos vão **na mesma mensagem, em segundo plano**, um por lote. O prompt precisa ser completo, porque o agente não vê esta conversa:

```
Lote: #<a>, #<b> (nesta ordem; um commit por issue; Closes de todas no PR)
Funcionalidades: <X, Y> (escopo de commit <x>)
Objetivo: <a cena de cada issue, 1–2 linhas cada>
Fichas: o corpo de cada issue. Regras: docs/features/<x>.md, seções <...>.
Puxar: `npm run tarefa -- puxar <a> <b> <branch> --agente <papel> --worktree .` no seu worktree, antes de codar.
Fora do escopo: <...>
Pronto quando: a cena de cada issue no jogo, com os estados do plano (docs/agentes.md#entrega-da-funcionalidade).
Autorização: commitar, enviar a branch e abrir PR. Não faça merge.
Base: <só se depender de PR aberto: `--base <branch-do-PR>` no puxar>
```

Se o `puxar` avisar **REVISAR RELEVÂNCIA**, o dono confere pelo `git diff` mostrado ([Conferir a relevância](../puxar-tarefa/SKILL.md#21-conferir-a-relevância)); issue já feita ou obsoleta sai do lote. Nunca dois donos com os mesmos arquivos.

## 5. Juntar e entregar

- Cada lote vira **um PR** pela skill [preparar-pr](../preparar-pr/SKILL.md), com `Closes #` de cada issue e "Como testar" por issue.
- "Para outros" do relatório vira tarefa nova. Decisões de design vão no resumo.
- `revisor` só em PR de Core, segurança ou regra de dano, preço ou fusão que a doc não fechou; uma passada. Achados que valem viram commits na mesma branch, pelo agente que abriu o PR (retome-o).
- Agente que só commitou local: `git cherry-pick <hash>` e `git show --stat` ([Fechamento](../../../docs/agentes.md#fechamento-o-coordenador-integra-e-limpa-o-que-criou)).
- PR empilhado: depois do squash do de baixo, `npm run reempilhar -- <PR>`. PR mergeado antes dos ajustes: não envie mais nada à branch dele; branch nova da `origin/main` com `cherry-pick`.

## 6. Fechar

Antes do relatório, a [reflexão de custo](../../../docs/agentes.md#custo-de-ia): corte que cabe entra num PR; sem corte, silêncio. Relatório curto: PRs (links) por lote, como testar, decisões de design, o que ficou de fora e por quê, o que o `/decida` precisa destravar. Só diga "pronto para merge" com o CI verde (e o revisor, se rodou). Merge é humano; a limpeza é automática.
