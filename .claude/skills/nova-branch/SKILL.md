---
name: nova-branch
description: Cria a branch de uma tarefa no padrão do repo (<tipo>/<escopo>/<descricao>) a partir da main atualizada. Use ao começar qualquer tarefa que vá virar commit/PR, quando pedirem para criar ou renomear uma branch, ou quando estiver na main com mudanças para commitar.
---

# Nova branch

Ninguém trabalha na `main`. Toda tarefa começa numa branch. Regras completas: `docs/git.md#branches`.

## 1. Montar o nome

Formato: `<tipo>/<escopo>/<descricao>`

- **tipo:** `feature` (para `feat`), `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`. Use o tipo que o commit principal vai ter.
- **escopo:** um dos `escopos` em `commitlint.config.mjs` (a funcionalidade em minúsculas, ou `core`). Leia o arquivo; não invente escopo.
- **descricao:** 2 a 5 palavras em kebab-case, minúsculas, sem acento (`fusao-basica`, `taxa-leilao`).

Confira antes de criar:

```
npm run validar-branch -- <nome>
```

Se a tarefa não deixar claro o tipo ou o escopo, pergunte à pessoa.

## 2. Criar

Se houver mudanças não commitadas, elas vêm junto para a branch nova (não precisa de stash).

```
git switch main
git pull --rebase origin main
git switch -c <nome>
```

Se o `git switch main` falhar por causa de mudanças locais em conflito, não descarte nada: crie a branch direto de onde está (`git switch -c <nome>`) e avise a pessoa que ela precisará atualizar com a `main` depois (`git pull --rebase origin main`).

## Casos especiais

- **Commits feitos na `main` local por engano:** `git switch -c <nome>` (leva os commits para a branch nova). A `main` local continua com esses commits; **não rode `git reset --hard`** (regra 0 do `AGENTS.md`). Mostre `git log <nome> --oneline -5` para a pessoa conferir que está tudo na branch nova e diga que, se ela quiser alinhar a `main` local, o comando é `git switch main && git reset --hard origin/main`, para ela mesma rodar.
- **Renomear a branch atual (antes do push):** `git branch -m <novo-nome>`. Se já foi enviada, pergunte antes: é preciso apagar a remota e enviar de novo.
- **Primeiro push:** `git push -u origin <nome>`. O hook `pre-push` valida o nome de novo e bloqueia push na `main`.
