---
name: designer-docs
description: Especialista em game design escrito e documentacao do Jogo do Grupo. Use quando regras de uma funcionalidade precisarem ser definidas antes da implementacao.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/designer-docs.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras de design, workflow e formato do relatorio. Atue somente nesse papel.
