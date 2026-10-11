# Desenvolvimento com vários agentes

O projeto é feito para vários agentes de IA implementarem partes diferentes **ao mesmo tempo**, cada um com uma área fixa, sem pisar no trabalho do outro. Pessoas decidem, revisam, montam o que é visual no Unity e fazem o merge.

A organização por funcionalidade ([arquitetura.md](arquitetura.md)) é o que torna isso possível: cada agente mexe em pastas diferentes, então o trabalho em paralelo não gera conflito.

## Papéis

| Agente | Área (só edita aqui) | Quando usar | Branch |
|---|---|---|---|
| `designer-docs` | `docs/features/`, validação, referências, visão geral | Uma decisão de design precisa virar regra escrita, ou uma funcionalidade precisa ser detalhada antes de implementar | `docs/<x>/<descricao>` |
| `shared-contratos` | `shared/Package/Runtime/`, `docs/features/` | **Antes** de server e client: mensagens, IDs, eventos e tipos que os dois lados usam | `feature/<x>/<descricao>-contrato` |
| `regras-do-jogo` | `shared/Package/Runtime/Features/`, `tests/`, `docs/features/` | Fórmulas puras (dano, fusão, sorteio de skills, experiência), com testes | `feature/<x>/<descricao>-regras` |
| `server-core` | `server/.../Core/`, `Program.cs`, `.csproj` e configuração (`appsettings`, `Properties/`) do servidor, ADRs, arquitetura | Infra do servidor: rede, sessões de grupo, despacho de mensagens, eventos, persistência | `feature/core/<descricao>-server` |
| `server-funcionalidade` | `server/.../Features/`, migrações em `server/.../Core/Persistencia/Migrations/`, `docs/features/` | Backend de **uma** funcionalidade | `feature/<x>/<descricao>-server` |
| `client-core` | `client/Assets/_Game/Core/` (só scripts) | Infra do client: conexão, cenas, input, câmera e controle em 3ª pessoa, UI base | `feature/core/<descricao>-client` |
| `client-funcionalidade` | `client/Assets/_Game/Features/` (só scripts) | Client de **uma** funcionalidade | `feature/<x>/<descricao>-client` |
| `revisor` | Nenhuma (somente leitura) | Revisar cada PR contra as regras antes do review humano | — |

As definições completas (regras de cada papel, passo a passo e formato do relatório) estão em [.claude/agents/](../.claude/agents/). São arquivos markdown que **qualquer IA consegue ler**.

O que fica com as **pessoas**:

- decidir regras de design e técnicas;
- montar cenas, prefabs e assets no editor do Unity (agentes só escrevem scripts);
- compilar e testar o client no Unity;
- aprovar e fazer o merge dos PRs.

## Fluxo de uma tarefa

```
                       ┌─ regras-do-jogo ──────── PR ─┐
designer-docs ─► shared-contratos ─► merge ─┼─ server-funcionalidade ── PR ─┼─► revisor ─► pessoa: Unity, review, merge
(se precisar)        (PR pequeno)           └─ client-funcionalidade ── PR ─┘
```

1. **Regra clara?** Se `docs/features/<x>.md` não diz o que implementar, o `designer-docs` detalha antes, ou a dúvida vai para o grupo.
2. **Contrato primeiro.** O `shared-contratos` define mensagens, IDs e eventos num PR pequeno, que é revisado e mergeado. Os outros agentes partem da `main`, então precisam do contrato lá.
3. **Em paralelo.** `regras-do-jogo`, `server-funcionalidade` e `client-funcionalidade` trabalham ao mesmo tempo, cada um no seu **worktree** (uma cópia separada do repo) e na sua branch, e cada um abre o seu PR.
4. **Revisão.** O `revisor` confere cada PR e aponta o que bloqueia.
5. **Pessoas.** Montam no Unity o que os agentes de client listaram, testam, aprovam e fazem o merge.

Várias funcionalidades podem andar ao mesmo tempo, cada uma com o seu trio de agentes. O limite real é a capacidade de review das pessoas: prefira PRs pequenos.

### Por que não conflita

- **Pastas separadas:** cada agente só edita a sua área. No Claude Code, um hook bloqueia a edição fora dela.
- **Worktrees:** cada agente trabalha numa cópia própria do repo, então um não vê os arquivos pela metade do outro.
- **Contrato antes:** server e client implementam o mesmo contrato, já mergeado.
- **Uma funcionalidade por tarefa:** os agentes de funcionalidade nunca mexem em duas.

## Regras para todos os agentes

- **Regra 0:** nunca apagar o que as pessoas criaram. No Claude Code, um hook do projeto bloqueia comandos de apagar ([scripts/agentes/bloquear-apagar.mjs](../scripts/agentes/bloquear-apagar.mjs)).
- **Autorização:** quando uma pessoa pede uma orquestração, ela autoriza os agentes a commitar, enviar as próprias branches e abrir PRs. **Merge é sempre humano.**
- **Fora da área:** se a tarefa exige mudar algo fora da área do agente (um contrato, o Core, outra funcionalidade), o agente para e descreve a mudança no relatório, para o agente certo fazer.
- **Relatório:** todo agente termina com o que fez, a branch e o PR, as verificações, o que outros agentes ou pessoas precisam fazer e as dúvidas.

## Como rodar

### Claude Code

Os agentes estão em [.claude/agents/](../.claude/agents/). Cada um roda num git worktree isolado (`isolation: worktree`) e tem um hook que limita as pastas.

- **Orquestrado (recomendado):** na sessão principal, peça a tarefa e a IA segue a skill [orquestrar-tarefa](../.claude/skills/orquestrar-tarefa/SKILL.md). Ela monta o plano, pede a sua aprovação, roda o contrato e depois lança os agentes em paralelo, em segundo plano.
- **Manual:** peça diretamente "use o agente server-funcionalidade para ..." ou abra um terminal por agente com `claude --agent <nome>`.
- `/agents` lista os agentes disponíveis.

### Outras IAs (Codex, Cursor, Copilot, Gemini...)

Abra **uma sessão por agente**, cada uma no seu worktree:

```
git fetch origin
git worktree add ../jdg-itens-server -b feature/itens/fusao-server origin/main
cd ../jdg-itens-server
npm install
```

E comece a conversa com:

> Atue como o agente definido em `.claude/agents/server-funcionalidade.md`, para a funcionalidade Itens. Tarefa: ...

Nessas ferramentas os hooks não rodam: o limite de pastas e a regra 0 dependem das instruções, do `revisor` e do review humano.

Worktrees que sobrarem depois do merge são removidos por uma **pessoa** (`git worktree remove <pasta>`); a IA não apaga.

## Limitações atuais

- **Unity:** os agentes não compilam o client (o Unity não está configurado para compilar pela linha de comando). Uma pessoa confere no editor. Compilar em modo batch é uma tarefa futura do core.
- **Testes:** o framework de testes ainda não foi decidido (ADR em aberto). Até lá, `regras-do-jogo` descreve os casos de teste no relatório.
- **Fundação primeiro:** agentes de funcionalidade dependem da API do Core (rede, sessões, despacho de mensagens). Enquanto o marco M0 não terminar, os trabalhos que já podem andar em paralelo são: `server-core`, `client-core`, `regras-do-jogo` (fórmulas puras) e `designer-docs`.
