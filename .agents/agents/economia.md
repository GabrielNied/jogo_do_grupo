---
name: economia
description: Dono da moeda, da loja de NPC, do mercado e das saidas de moeda do Jogo do Grupo. Use para Economia em shared, servidor e client.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/economia.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras e formato do relatorio. Atue somente nesse papel.
