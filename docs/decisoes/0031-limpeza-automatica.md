# 0031. Limpeza automática de worktrees e branches

**Data:** 2026-10-11
**Status:** aceita (ajusta o [ADR 0018](0018-limpeza-do-que-os-agentes-criam.md))

## Contexto

O `npm run limpar-agentes` do ADR 0018 quase nunca removia nada. Em 2026-10-11 havia 20 worktrees e 32 branches locais; a simulação removeria 4. Três causas:

- **Ninguém disparava.** A limpeza ficava com quem coordena, depois do merge, mas o coordenador termina antes do merge e não volta.
- **"Usado há pouco" olhava o mtime do `index`**, que o VS Code atualiza sozinho em todo worktree aberto na pasta do projeto. Worktree parado havia horas parecia recente.
- **Só PR casado pelo nome da branch liberava commits.** Agente integrado por cherry-pick (`worktree-agent-*`), PR fechado sem merge e branch já enviada ao GitHub ficavam para sempre.

## Decisão

- **Disparo automático e local**, porque o GitHub não alcança a máquina. A limpeza roda em segundo plano (`--fundo`), uma rodada por vez, em três momentos:
  - no início de cada sessão do Claude Code (hook `SessionStart`);
  - em todo merge ou `git pull` (hook `post-merge` do husky);
  - no `npm run tarefa -- puxar`.

  O relatório da última rodada fica em `.git/limpar-agentes.log`.
- **O critério é "nada se perde".** Sai o worktree ou branch sem mudança não commitada cujos commits estão em algum destes lugares:
  - na `main`, inclusive pelo conteúdo, o que cobre cherry-pick e squash (`git cherry` e, para cada commit `-`, o mesmo assunto num commit da `main` — branch local atrás da `main` deixa de ficar presa);
  - num PR do GitHub, mesclado ou fechado (o GitHub guarda a ponta em `refs/pull/N/head`);
  - numa branch do `origin`.
- **PR aberto sempre fica.** Branch sem PR mexida nas últimas 2 h também fica, porque pode ser um agente começando. A recência vem do reflog (commit, checkout), não do `index`. PR mesclado sai mesmo se recente: o trabalho acabou.
- **Worktree fora de `.claude/worktrees/` sai na rodada automática** quando o PR foi mesclado, foi fechado com a ponta ainda no GitHub, ou o conteúdo já está na `main`. Checkout pessoal da `main`, ou de uma branch que só está no `origin`, continua de fora; `--todos` inclui esses.
- No GitHub (`--remotas`), a branch do `origin` não conta como cópia de si mesma, e PR fechado sem merge fica.

## Consequências

- O disco e a lista de branches se limpam sozinhos depois do merge, sem depender de ninguém.
- Commit que só existe nesta máquina continua guardado e aparece no log ("só nesta máquina, sem PR"). Commit feito depois do merge também, a não ser que a branch já esteja no `origin`.
- Worktree com processo usando a pasta (Unity, terminal) não sai: o `git worktree remove` falha e o log registra.
- Sessão de agente parada num worktree cujo PR já entrou pode perder a pasta. Depois do merge não há mais o que fazer nela; a pessoa abre outra a partir da `main`.
