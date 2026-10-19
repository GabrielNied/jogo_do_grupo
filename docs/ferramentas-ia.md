# Ferramentas de IA

Como rodar os agentes em cada ferramenta. O fluxo e as regras estão em [agentes.md](agentes.md); aqui fica só a configuração, lida uma vez por quem monta o ambiente.

## Claude Code

Os agentes estão em [.claude/agents/](../.claude/agents/). Cada um roda num git worktree isolado (`isolation: worktree`) e tem um hook que limita as pastas.

- **Orquestrado (recomendado):** `/trabalhe` escolhe as tarefas, monta os lotes e lança os donos em paralelo, sem esperar aprovação. Merge continua humano.
- **Manual:** "use o agente funcionalidade para ..." ou `claude --agent <nome>` num terminal. `/agents` lista os agentes.

**Permissão para a limpeza.** O modo automático pode negar `git worktree remove` e `git branch -D`, e a IA não edita a própria permissão. A pessoa libera uma vez em `.claude/settings.local.json` (já no `.gitignore`):

```json
{ "permissions": { "allow": [
  "Bash(git worktree unlock:*)",
  "Bash(git worktree remove:*)",
  "Bash(git branch -D worktree-agent-*)"
] } }
```

A regra cobre só worktrees e branches `worktree-agent-*`. Ou a pessoa roda os comandos pelo prompt com `!`.

## Codex

O Codex lê o `AGENTS.md` da raiz. Os perfis equivalentes aos agentes do Claude ficam em [.codex/agents/](../.codex/agents/): peça pelo nome do papel ao delegar. Cada perfil manda ler a definição canônica em [.claude/agents/](../.claude/agents/). Os procedimentos são descobertos em [.agents/skills/](../.agents/skills/), que encaminham para [.claude/skills/](../.claude/skills/).

Em projeto confiável, o Codex carrega [.codex/hooks.json](../.codex/hooks.json), que aplica a regra 0. Aprove o hook uma vez com `/hooks`.

Agentes que escrevem nunca dividem a cópia do repositório: crie os worktrees antes de delegar e passe o caminho de cada um. O revisor é só leitura e pode rodar em paralelo. O Codex limita a sessão a três subagentes além do coordenador.

## Antigravity

Reconhece o `AGENTS.md` e as skills em [.agents/skills/](../.agents/skills/). Os perfis ficam em [.agents/agents/](../.agents/agents/) e mandam ler a definição canônica. Habilite o hook [.agents/hooks.json](../.agents/hooks.json) em **Customizations > Hooks** (ou `/hooks` no CLI). No IDE que ainda não lista agentes personalizados, peça o papel no prompt. Escrita em paralelo só em worktrees isolados.

## OpenCode

Lê o `AGENTS.md` e as skills de [.claude/skills/](../.claude/skills/). Os hooks e os agentes de [.claude/agents/](../.claude/agents/) não valem: peça o papel no prompt, como em "Outras IAs" abaixo. No Windows, instale com `npm install -g --allow-scripts=@opencode/cli @opencode/cli`: sem o `--allow-scripts`, o binário não é baixado.

Os modelos `opencode/*-free` (`opencode models`) são grátis por tempo limitado e alguns guardam os dados para treino; o repo é público, mas não cole segredo. Servem para quem não tem Claude contribuir. Para cortar custo dentro do Claude Code, prefira um agente em Haiku, como o `conferente`.

## Outras IAs (Cursor, Copilot, Gemini...)

Uma sessão por agente, cada uma no seu worktree:

```
git fetch origin
git worktree add ../jdg-itens -b feature/itens/fusao origin/main
cd ../jdg-itens
npm install
```

E comece com:

> Atue como o agente definido em `.claude/agents/itens.md`. Tarefa: ...

Nessas ferramentas os hooks não rodam: o limite de pastas e a regra 0 dependem das instruções, do `revisor` e do review humano.
