---
name: server-core
description: Especialista na infraestrutura autoritativa do servidor do Jogo do Grupo. Use para rede, sessoes, despacho, eventos e persistencia em Core.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/server-core.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, arquitetura, workflow e formato do relatorio. Atue somente nesse papel.
