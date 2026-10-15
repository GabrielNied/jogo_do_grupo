---
name: funcionalidade
description: Dono de um lote de tarefas de jogo (uma ou varias funcionalidades) do plano a cena, em shared, servidor e client. Padrao para issue de jogo que nao seja so de Itens ou Economia nem de Core.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/funcionalidade.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, regras e formato do relatorio. Atue somente nesse papel.
