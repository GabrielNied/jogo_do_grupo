# Documentação

| Documento | Conteúdo |
|---|---|
| [visao-geral.md](visao-geral.md) | O que é o jogo e seus pilares |
| [arquitetura.md](arquitetura.md) | Como cliente, servidor e shared se organizam |
| [convencoes.md](convencoes.md) | Padrões de código e nomes |
| [git.md](git.md) | Branches, commits (commitlint), PRs e proteção da `main` |
| [responsaveis.md](responsaveis.md) | Quem cuida de cada funcionalidade |
| [features/](features/) | Um documento por funcionalidade do jogo |
| [decisoes/](decisoes/) | Decisões técnicas registradas (ADRs) |

O brainstorm de ideias fica no board do FigJam **"Jogo do Grupo - Definição de Features"** (time "Gabriel Nied's team", pasta "Team project"). Quando uma ideia for aprovada, ela entra no documento da funcionalidade.

## Onde cada coisa fica (fonte única)

Cada informação mora em um lugar só. Os outros documentos apontam para ele em vez de copiar.

| Informação | Fonte |
|---|---|
| Requisitos, instalação e primeiros passos | [README da raiz](../README.md#primeiros-passos) |
| Lista de funcionalidades e faixas de IDs | [README da raiz](../README.md#funcionalidades) |
| Regras de dependência e organização | [arquitetura.md](arquitetura.md) |
| Nomes, formatação, CSharpier | [convencoes.md](convencoes.md) |
| Branches, commits, PRs, proteção da `main` | [git.md](git.md) |
| Tipos e escopos de commit | [commitlint.config.mjs](../commitlint.config.mjs) |
| Responsabilidades, Unity sem conflito, uso de IA | [CONTRIBUTING.md](../CONTRIBUTING.md) |
| Regras de cada funcionalidade | [features/](features/) |
| Instruções para assistentes de IA | [AGENTS.md](../AGENTS.md) |
