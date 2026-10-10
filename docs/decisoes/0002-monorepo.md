# 0002. Monorepo

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Cliente, servidor e shared mudam juntos com frequência, principalmente no começo do projeto.

## Decisão

Um único repositório com `client/`, `server/` e `shared/`.

## Consequências

- Mudanças que tocam as três partes entram num único PR.
- O Shared não precisa ser publicado nem versionado como pacote.
- O repo cresce com os assets do Unity; mitigado com Git LFS e sparse checkout.
- Se o time crescer muito, dá para separar depois sem perder histórico.
