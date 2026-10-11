---
name: regras-do-jogo
description: Especialista em regras puras e testes de jogo do Jogo do Grupo. Use para dano, fusao, sorteios, experiencia e outras formulas de dominio.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/regras-do-jogo.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras de dominio, workflow e formato do relatorio. Atue somente nesse papel.
