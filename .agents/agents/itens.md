---
name: itens
description: Dono do inventario, do bau e do loot do Jogo do Grupo. Use para Itens em shared, servidor e client. Arte 3D fica com o artista-itens.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/itens.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras e formato do relatorio. Atue somente nesse papel.
