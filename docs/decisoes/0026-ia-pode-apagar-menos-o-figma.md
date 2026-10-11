# 0026. A IA pode apagar, menos o Figma

**Data:** 2026-10-11
**Status:** aceita (substitui a regra 0 original do [AGENTS.md](../../AGENTS.md), ajusta o [ADR 0008](0008-desenvolvimento-multiagente.md) e o [ADR 0018](0018-limpeza-do-que-os-agentes-criam.md))

## Contexto

A regra 0 dizia "nunca apague o que as pessoas criaram", e o hook `bloquear-apagar` barrava `rm`, `git branch -D`, `git restore` e afins. Na prática isso deixava sujeira no projeto: arquivos e materiais sem uso que a própria IA tinha gerado, `.meta` órfãos, branches velhas e código morto ficavam parados esperando uma pessoa apagar, e quase ninguém apagava. O medo que originou a regra (uma IA sumir com o trabalho do grupo) vale de verdade só para o **Figma**, onde o conteúdo não tem histórico fácil de recuperar como o git.

## Decisão

- A regra 0 passa a ser: **nunca apague nada do Figma** (arquivos, páginas, frames, nós, componentes, variáveis, comentários, nem por script).
- O resto pode ser apagado quando for sujeira ou estiver obsoleto: arquivos, pastas, assets, trechos de docs, código morto, branches, worktrees. O commit explica o que saiu e por quê, e os links que apontavam para lá são atualizados.
- **Olhar antes de apagar:** conferir o que é, quem usa (referências, guids do Unity) e se há mudança não commitada. Dúvida real sobre algo de outra pessoa em andamento: perguntar.
- **Confirmar antes do que não volta ou afeta o grupo todo:** push forçado (nunca na `main`), apagar branch remota de PR aberto, apagar repositório, release ou projeto no GitHub.
- **Atualização (2026-10-11):** fechar issue ou PR de outra pessoa deixa de pedir confirmação. A IA fecha o que a evidência mostra resolvido (ex.: pendência de Unity que o CI já confirmou) e comenta o motivo.
- O hook `bloquear-apagar` fica só com o que a regra ainda protege: remoção no Figma por script (`use_figma` com `.remove()` ou `.delete()`), push forçado e `gh repo|release|project delete`. Os comandos de apagar arquivo e branch deixam de ser bloqueados.
- Worktrees e branches temporários dos agentes seguem saindo por `npm run limpar-agentes`, que só remove o que já está na `main` (ADR 0018), porque apagar à mão perde commits feitos depois de um squash merge.

## Consequências

- Menos sujeira: agentes e pessoas removem o que sobra no mesmo PR que o deixou obsoleto.
- Mais risco de apagar algo útil por engano; o git guarda o histórico, e o item "olhar antes de apagar" e o review do PR são a proteção.
- A proteção do Figma depende do hook (só no Claude Code) e das instruções; nas outras ferramentas vale a instrução.
- O `.claude/settings.json` precisa do matcher do Figma no `PreToolUse` para o hook enxergar as chamadas do `use_figma` (`mcp__claude_ai_Figma__.*`); sem ele, o hook só vê comandos de shell.
