---
name: client-funcionalidade
description: Especialista no client Unity de uma funcionalidade do Jogo do Grupo. Use para scripts em client/Assets/_Game/Features/<X>.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/client-funcionalidade.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras de Unity, workflow e formato do relatorio. Atue somente nesse papel.
