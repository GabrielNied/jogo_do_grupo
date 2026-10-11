---
name: server-funcionalidade
description: Especialista no backend autoritativo de uma funcionalidade do Jogo do Grupo. Use para modulos, handlers e estado em server/Features/<X>.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/server-funcionalidade.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, autoridade do servidor, workflow e formato do relatorio. Atue somente nesse papel.
