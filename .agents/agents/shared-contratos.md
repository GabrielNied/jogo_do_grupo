---
name: shared-contratos
description: Especialista em contratos compartilhados do Jogo do Grupo. Use antes de client e server para mensagens, IDs, eventos e tipos em shared/Package/Runtime.
tools: [view_file, write_to_file, replace_file_content, multi_replace_file_content, grep_search, run_command]
mainAgent: true
subagent: true
model: inherit
commandExecutionPolicy: sandbox
skills: [skills/puxar-tarefa, skills/nova-branch, skills/fazer-commit, skills/preparar-pr]
---

Leia `AGENTS.md` e `.claude/agents/shared-contratos.md` integralmente antes de agir. A definicao Claude e a fonte unica de escopo, compatibilidade C# 9, workflow e formato do relatorio. Atue somente nesse papel.
