# Documentação

| Documento | Conteúdo |
|---|---|
| [visao-geral.md](visao-geral.md) | O que é o jogo e seus pilares |
| [referencias.md](referencias.md) | Jogos usados como base, por sistema |
| [arquitetura.md](arquitetura.md) | Como cliente, servidor e shared se organizam |
| [convencoes.md](convencoes.md) | Padrões de código e nomes |
| [git.md](git.md) | Branches, commits (commitlint), PRs e proteção da `main` |
| [agentes.md](agentes.md) | Vários agentes de IA trabalhando em paralelo: papéis, áreas e fluxo |
| [responsaveis.md](responsaveis.md) | Quem cuida de cada funcionalidade |
| [brainstorm/](brainstorm/) | Cópia em texto do board do FigJam (ideias não validadas) |
| [features/](features/) | Um documento por funcionalidade do jogo |
| [decisoes/](decisoes/) | Decisões técnicas registradas (ADRs) |

O brainstorm de ideias fica no board do FigJam **"Jogo do Grupo - Definição de Features"** (time "Gabriel Nied's team", pasta "Team project"). Uma cópia em texto, para ler sem acessar o Figma, fica em [brainstorm/figjam.md](brainstorm/figjam.md) ([como atualizar](brainstorm/README.md)). Quando uma ideia for aprovada, ela entra no documento da funcionalidade.

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
| Ideias do brainstorm (não validadas) | [brainstorm/figjam.md](brainstorm/figjam.md), gerado do FigJam |
| Decisões de design em aberto (validação das ideias) | [brainstorm/validacao.md](brainstorm/validacao.md) |
| Regras de cada funcionalidade | [features/](features/) |
| Padrão dos modelos 3D próprios e como gerá-los | [arte/padrao-de-modelos.md](arte/padrao-de-modelos.md) |
| Jogar com o grupo com o servidor no PC de alguém | [hospedagem.md](hospedagem.md) |
| Instruções para assistentes de IA | [AGENTS.md](../AGENTS.md) |
| Papéis e fluxo dos agentes | [agentes.md](agentes.md) e [.claude/agents/](../.claude/agents/) |
