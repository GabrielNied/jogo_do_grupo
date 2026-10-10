# 0005. AGENTS.md como fonte única de instruções para IA

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Cada pessoa do grupo usa um assistente diferente (Claude, ChatGPT/Codex, Copilot, Cursor, Gemini). Cada ferramenta tem seu arquivo de instruções (`CLAUDE.md`, `.cursorrules`, `GEMINI.md`, `.github/copilot-instructions.md`). Manter vários arquivos faz as regras divergirem.

## Decisão

- **`AGENTS.md`** na raiz é a fonte única de instruções. É o padrão aberto lido diretamente pela maioria das ferramentas.
- **`CLAUDE.md`** contém só `@AGENTS.md`, para o Claude Code carregar o mesmo conteúdo em qualquer versão.
- **Skills** no formato aberto de Agent Skills (`SKILL.md`) em `.claude/skills/`, que o Claude Code, o Cursor e o Copilot carregam sozinhos. O `AGENTS.md` lista cada skill com o caminho do arquivo, para as ferramentas que não carregam skills dessa pasta (Codex, Gemini) lerem quando precisarem.
- Nenhum outro arquivo de instruções específico de ferramenta.

## Consequências

- Uma regra nova é escrita uma vez e vale para todas as ferramentas.
- Codex e Gemini não descobrem as skills automaticamente; dependem da tabela no `AGENTS.md`. Se isso atrapalhar, dá para espelhar as skills em `.agents/skills/` (que essas ferramentas leem) com um script, em vez de copiar à mão.
- Symlinks foram evitados porque no Windows exigem modo de desenvolvedor.
