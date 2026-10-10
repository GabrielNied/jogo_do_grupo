# 0007. Repo privado com proteção local da main

**Data:** 2026-10-10
**Status:** aceita (substitui a parte de ruleset do [ADR 0006](0006-fluxo-do-git.md))

## Contexto

O [ADR 0006](0006-fluxo-do-git.md) previa proteger a `main` com um ruleset do GitHub. O repositório passou a ser **privado**, e no plano gratuito o GitHub não oferece ruleset, proteção de branch nem pedido automático de review pelo `CODEOWNERS` em repo privado. As alternativas eram pagar o GitHub Pro ou deixar o repo público.

## Decisão

- Manter o repositório **privado**, sem GitHub Pro.
- A proteção da `main` fica **local e por combinado**:
  - o hook `pre-push` bloqueia push na `main` e branch fora do padrão;
  - o hook `commit-msg` valida as mensagens com o commitlint;
  - o CI valida título do PR, nome da branch, formatação e build;
  - um job do CI (`main-via-pr`) marca com check vermelho qualquer commit que entre na `main` sem PR;
  - o grupo combina as regras de merge: checks verdes, 1 aprovação de outra pessoa, review pedido manualmente ao responsável.
- As opções de merge do repo continuam aplicadas: só squash, mensagem com o título do PR e os commits, branch apagada depois do merge.
- O ruleset continua versionado em `.github/rulesets/proteger-main.json`, pronto para ser aplicado se o repo ficar público ou a conta passar para o Pro.

## Consequências

- Nada no GitHub **impede** um push direto (com `--no-verify`) ou um merge sem aprovação; o grupo depende do combinado e do aviso do CI.
- O `CODEOWNERS` vira só um mapa de responsáveis: o review precisa ser pedido à mão no PR.
- Sem custo.
- Se o grupo crescer ou acontecerem pushes diretos, rever esta decisão (GitHub Pro ou repo público) e aplicar o ruleset.
