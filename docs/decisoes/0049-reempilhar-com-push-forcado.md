# 0049. Reempilhar a leva com push forçado na branch do PR

**Data:** 2026-10-18
**Status:** aceita (abre uma exceção na regra 0 do [AGENTS.md](../../AGENTS.md) e no [ADR 0026](0026-ia-pode-apagar-menos-o-figma.md))

## Contexto

A `main` só aceita squash ([git.md](../git.md)). Quando um PR de uma [leva empilhada](../git.md#leva-empilhada) entra, o de cima ainda carrega os commits originais do de baixo, que na `main` viraram outro hash, e passa a conflitar consigo mesmo. Como push forçado era só das pessoas, o `reempilhar` abria um PR novo numa branch `-segue` e fechava o antigo. Em 2026-10-18 isso gerou três PRs substitutos seguidos (#1082, #1085, #1086), com o review e as aprovações presos nos PRs fechados.

Alternativas consideradas:

- **Merge commit no lugar do squash:** resolve na raiz (o hash não muda), mas leva para a `main` todos os commits pequenos dos agentes e os commits de merge; o título do PR deixa de ser o commit.
- **Rebase merge:** também reescreve os hashes; o conflito continua.
- **Não empilhar:** vale quando as funcionalidades são independentes, mas não quando o PR de cima usa o código do de baixo.

## Decisão

- O squash continua.
- O script `reempilhar` (no job [`reempilhar.yml`](../../.github/workflows/reempilhar.yml) ou à mão) pode fazer **push forçado na branch do próprio PR que está reempilhando**: aplica os commits próprios do PR sobre `origin/main` e publica com `--force-with-lease` preso à ponta que leu. O PR continua o mesmo (número, review, aprovações, conversas).
- A exceção é só essa. Push forçado na `main` segue barrado pelo ruleset, e qualquer outro push forçado continua com quem decide (regra 0).
- O job roda no merge de cada PR na `main` e reempilha os PRs abertos que citam o mergeado em `Depende de`. Conflito de verdade: o job não publica nada e comenta no PR como resolver.

## Consequências

- Um PR por funcionalidade do começo ao fim, mesmo empilhado.
- Quem tiver a branch do PR local precisa de `git pull --rebase` (ou recriar a branch) depois do reempilhamento; o comentário do job avisa a ponta anterior.
- `--force-with-lease` impede sobrescrever um push feito por alguém entre a leitura e a publicação; nesse caso o job falha e comenta.
- O segredo `REEMPILHAR_TOKEN` é necessário para os checks obrigatórios rodarem depois do push do job: o GitHub não dispara workflows a partir do token padrão do Actions.
