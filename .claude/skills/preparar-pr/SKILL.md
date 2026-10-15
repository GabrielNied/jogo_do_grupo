---
name: preparar-pr
description: Confere se a mudança está pronta para PR (pastas, regras, verificação proporcional, branch e commit) e escreve a descrição no template. Use antes de abrir PR, ao commitar ou para "ver se está tudo certo".
---

# Preparar PR

Rode na ordem e relate só o que falhou. Corrija o automático (formatação); o resto, explique.

## 1. O que mudou e onde

`git status`, `git diff --stat origin/main...HEAD`. Confira contra as regras 1 a 9 do AGENTS.md: arquivos fora das pastas do lote, `Core/` (incluindo `ProjectSettings/`, `Packages/`, `.github/`, cenas de `Core/Scenes/`) pede review do responsável; binário (PSD, FBX, Blend) só com `git lfs lock`. Valor de sensação novo no client está na `Sintonia` (regra 17)?

## 2. Verificação proporcional

Sempre `git diff --check`. Depois, **só** a linha da alteração; o CI é a confirmação completa ([ADR 0034](../../../docs/decisoes/0034-menos-verificacao-repetida.md)).

| Alteração | Verificação mínima |
|---|---|
| Markdown, docs | Links e comandos alterados. Nada de build. |
| Instruções de IA (`AGENTS.md`, `.claude/`, `.codex/`, `.agents/`) | `npm run contexto` e o formato alterado (`node --check` no hook, parse de JSON/TOML). |
| Script Node | `node --check` e `npm run testar-scripts`. |
| C# de servidor ou shared | `dotnet csharpier format .` e `dotnet test JogoDoGrupo.slnx -c Release` (já compila). |
| C# de client | `dotnet csharpier format .`, `npm run unity:compilar` e `npm run conferir-unity`. Tela ou HUD: [prints](../../../docs/agentes.md#prints-da-tela). Os testes EditMode ficam com o CI. |
| Shared usado pelo client | A linha de server/shared e `npm run unity:compilar`. |
| Tempo real / transporte UDP (`Core/TempoReal/`, `TransporteTempoReal`, canal de `Sequenciada`) | `npm run conferir-contratos-rede` e `node scripts/agentes/conferir-contratos-rede.mjs origin/main` se a branch divergiu. Mesmo PR: shared + `ServidorTempoReal` + `TransporteLiteNetLib`; label `testar-unity` se tocou teste EditMode. |
| Conteúdo Unity (cena, prefab, asset) | O comando que gerou, `npm run conferir-unity` e `unity:recompilar` (aberto) ou `unity:compilar` (fechado). |
| CI, dependência, build | O comando que usa a configuração e o teste afetado. |

Risco além da matriz (segurança, concorrência, migração, Core amplo): acrescente a verificação e diga por quê. Conexão ao mundo só se confirma com a autenticação UDP e a primeira posição aplicada, com outra janela em foco; chegar ao login não basta.

**Sem Unity no ambiente:** `unity:*` saem 0 com "pulado"; não repita nem peça à pessoa. Gere `.meta` com `npm run unity:meta` e abra o PR normal. Depois, só o que precisa do Unity para existir: `npm run tarefa -- pendencia-unity <PR> --falta "<item>"`. Tela, compilar e EditMode não viram pendência. A label `testar-unity` roda o Unity no PR quando o risco no client é alto.

## 3. Docs, branch e commit

- Regra do jogo mudou: `docs/features/<x>.md`. Decisão técnica: ADR (skill `novo-adr`).
- Antes de commitar, a [reflexão de custo](../../../docs/agentes.md#custo-de-ia): corte pequeno entra aqui; sem corte, não escreva nada.
- Branch: `npm run validar-branch -- "$(git branch --show-current)"`; fora do padrão ou na `main`, skill `nova-branch`.
- Commits pela skill `fazer-commit`; `npx --no -- commitlint --from origin/main --to HEAD`.
- Mexeu em `client/Assets/` ou `shared/Package/`: `npm run conferir-unity` antes do PR (`.meta` de script, pasta, `.asmdef`, estilo e texto com `unity:meta`; o resto com `unity:importar`, editor fechado). Sem Unity, abra assim mesmo e registre a pendência.
- Entregue com `npm run entregar` ([Entregar](../../../docs/git.md#entregar)), sem esperar pedido. Merge é humano.

## 4. Título e descrição

**Título:** cabeçalho de commit válido que resume o PR (vira o commit do squash).

**Descrição:** `.github/pull_request_template.md`. "O que muda" em 1–3 frases, checklist do que foi verificado e "Como testar" concreto. PR da cena de jogo: "Como testar" diz o que a pessoa faz e o que aparece, e o corpo leva `Closes #` de cada issue do lote ([Entrega](../../../docs/agentes.md#entrega-da-funcionalidade)). PR só de contrato ou servidor não fecha a issue.
