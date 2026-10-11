# 0008. Desenvolvimento com vários agentes em paralelo

**Data:** 2026-10-10
**Status:** aceita

## Contexto

O grupo quer que vários agentes de IA implementem partes diferentes do jogo ao mesmo tempo: um agente para cada parte do backend e um para cada parte do client. Sem regras, agentes em paralelo editam os mesmos arquivos, quebram contratos uns dos outros e geram conflitos. O grupo também usa IAs diferentes (Claude, ChatGPT/Codex, Cursor, Copilot).

## Decisão

- **Oito papéis de agente**, cada um com uma área fixa de pastas: `designer-docs`, `shared-contratos`, `regras-do-jogo`, `server-core`, `server-funcionalidade`, `client-core`, `client-funcionalidade` e `revisor` (somente leitura).
- **Definições em `.claude/agents/<nome>.md`**: são subagentes do Claude Code e também instruções legíveis por qualquer outra IA.
- **Fluxo com contrato primeiro**: o `shared-contratos` define e mergeia o contrato; depois `regras-do-jogo`, `server-funcionalidade` e `client-funcionalidade` trabalham em paralelo, cada um num **git worktree** e numa branch própria, com um PR cada.
- **Proteções no Claude Code**:
  - cada agente tem um hook que bloqueia edição fora da sua área (`scripts/agentes/limitar-pastas.mjs`); os agentes de client também não editam cenas, prefabs nem `.meta`;
  - um hook do projeto (`.claude/settings.json`) bloqueia comandos que apagam ou descartam trabalho, aplicando a regra 0 (`scripts/agentes/bloquear-apagar.mjs`).
- **Coordenação** pela skill `orquestrar-tarefa`: plano aprovado pela pessoa, contrato primeiro, agentes em paralelo. ~~Revisor em cada PR~~ substituído em 2026-10-11 por: revisor só em PR de risco, como em [agentes.md](../agentes.md#regras-para-todos-os-agentes).
- **Pessoas** decidem, montam o que é visual no Unity, testam e fazem o merge.

## Consequências

- Várias funcionalidades e camadas avançam ao mesmo tempo sem conflito, porque as áreas não se sobrepõem.
- O gargalo passa a ser o review humano; os PRs precisam ser pequenos.
- Os hooks só existem no Claude Code. Nas outras IAs, o limite de pastas e a regra 0 dependem das instruções, do `revisor` e do review.
- Os agentes de client não compilam o Unity; até o core configurar a compilação em modo batch, uma pessoa confere no editor.
- Enquanto o Core (marco M0) não estiver pronto, os agentes de funcionalidade têm pouco a fazer; o paralelismo começa pelos agentes de core, de regras e de docs.
