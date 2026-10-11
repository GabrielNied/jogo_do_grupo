---
name: client-core
description: Especialista na infraestrutura Unity do client do Jogo do Grupo. Use para conexao, cenas, input, camera e UI base em Core.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/client-core.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras de Unity, workflow e formato do relatorio. Atue somente nesse papel.
