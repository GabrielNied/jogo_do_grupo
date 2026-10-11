# AGENTS.md

Instruções para assistentes de IA (Claude, Codex/ChatGPT, Copilot, Cursor, Gemini e outros) que trabalham neste repositório. Vale também como resumo rápido para pessoas.

## O projeto

RPG de ação online para PC, para grupos pequenos (sem classes, skills nos itens, foco em bosses, dungeons e raids; veja [docs/visao-geral.md](docs/visao-geral.md)). Feito em grupo. Monorepo em C#:

- `client/`: Unity 6000.6.5f1 com URP (C# 9). Código em `client/Assets/_Game/{Core,Features/<X>}/`, um `.asmdef` por funcionalidade.
- `server/`: .NET 10, sem engine, **autoritativo** (decide dano, drops, fusão, economia). Código em `server/src/JogoDoGrupo.Server/{Core,Features/<X>}/`.
- `shared/`: .NET Standard 2.1 e **C# 9** (limite do Unity), usado pelos dois lados. Código em `shared/Package/Runtime/{Core,Features/<X>}/`.
- `docs/`: visão, arquitetura, convenções, uma página por funcionalidade (`docs/features/`) e ADRs (`docs/decisoes/`).

A lista de funcionalidades e as faixas de IDs de mensagem estão no [README.md](README.md#funcionalidades).

## Comandos

```
dotnet tool restore                  # uma vez: instala o CSharpier
npm install                          # uma vez: commitlint e hooks do git (husky)
dotnet build JogoDoGrupo.slnx        # compila server e shared
dotnet run --project server/src/JogoDoGrupo.Server
dotnet csharpier format .            # formata (rode antes de terminar qualquer mudança em C#)
dotnet csharpier check .             # o que o CI confere
dotnet test JogoDoGrupo.slnx         # todos os testes (o CI também roda)
npm run unity:compilar               # compila o client contra as DLLs do Unity
npm run validar-branch -- <branch>   # confere nome de branch
printf '%s
' "<msg>" | npx --no -- commitlint   # confere mensagem de commit
```

Testes em `tests/` (xUnit; [ADR 0013](docs/decisoes/0013-testes-automatizados.md), guia em [docs/convencoes.md](docs/convencoes.md#testes)). O editor do Unity não roda pela linha de comando, mas `npm run unity:compilar` confere se o client compila.

## Regras que você deve seguir

0. **Nunca apague o que as pessoas criaram. Ajuste ou melhore, nunca remova.** Esta regra vem antes de todas as outras e vale mesmo que pareça óbvio que algo está sobrando.
   - Não apague arquivos, pastas, assets, seções ou itens de documentação, ideias, comentários de outras pessoas, conteúdo do Figma, branches, issues ou PRs (nem feche issues/PRs).
   - Não use comandos git que descartam trabalho: `git reset --hard`, `git clean`, `git checkout -- <arquivo>`, `git restore` sem `--staged`, `git stash drop`, `git push --force` (inclusive `--force-with-lease`), `git branch -D`. Se um deles for necessário, explique e deixe a pessoa rodar.
   - Editar para melhorar é permitido, desde que a intenção original continue lá. Ao reescrever o texto de alguém, preserve as ideias; se mudar o sentido, pergunte antes.
   - Algo ficou obsoleto, duplicado ou errado? **Marque** (ex.: "~~texto~~ substituído por ..." ou status `substituída` num ADR) ou mova para o lugar certo, e avise a pessoa.
   - Se algo realmente precisar sumir, diga à pessoa exatamente o que apagar e deixe que ela apague.
1. **Fique nas pastas da funcionalidade da tarefa** (`Features/<X>/` em client, server e shared, e `docs/features/<x>.md`). Se precisar mexer em outra funcionalidade ou em `Core/`, pare e avise a pessoa: isso exige review do responsável.
2. **Funcionalidades não se referenciam diretamente.** Comunicação por eventos e contratos definidos em `shared/Package/Runtime/Features/<dono>/`. Não adicione referência de um `.asmdef` de feature para outro.
3. **Servidor autoritativo:** regras que decidem resultado (dano, drop, fusão, preço) rodam no servidor. O cliente envia intenções e mostra resultados.
4. **IDs de mensagem** só dentro da faixa da funcionalidade (`<X>MessageIds.Faixa`).
5. **Shared:** nada de `UnityEngine`, nada acima de C# 9. Use `namespace X { }` com chaves (namespace de arquivo não compila no Unity).
6. **Server:** C# moderno com namespace de arquivo (`namespace X;`). Módulos implementam `IFeatureModule` e são descobertos sozinhos; não crie arquivo central de registro.
7. **Nomes de domínio em português sem acento** (`Itens`, `Fusao`, `Profissoes`); termos técnicos em inglês são ok (`Module`, `Handler`). Campos privados `_camelCase`. Detalhes em [docs/convencoes.md](docs/convencoes.md).
8. **Formatação é do CSharpier.** Não ajuste estilo à mão nem discuta formatação; rode `dotnet csharpier format .`.
9. **Unity:** não edite cenas (`.unity`) nem `ProjectSettings/` sem pedido explícito. Prefira prefabs. Não crie nem apague `.meta` à mão.
10. **Todo PR de código traz testes** do que mudou (caminho feliz, erros, limites, concorrência quando houver). Rode `dotnet test` antes de terminar; mexeu no client, rode `npm run unity:compilar`.
11. **Mudou regra do jogo?** Atualize `docs/features/<x>.md` na mesma mudança. **Decisão técnica?** Crie um ADR.
12. **Git:** nunca commite nem dê push na `main`; trabalhe numa branch `<tipo>/<escopo>/<descricao>`. Commits e títulos de PR no padrão `<tipo>(<escopo>): <descrição em português, minúscula>`, validados pelo commitlint (tipos e escopos em `commitlint.config.mjs`). Nunca use `--no-verify`. Só faça commit ou push quando a pessoa pedir (pedir uma orquestração de agentes conta como pedido para as branches dos agentes; merge é sempre humano). Detalhes em [docs/git.md](docs/git.md).
13. Cada informação mora em um documento só (tabela em [docs/README.md](docs/README.md#onde-cada-coisa-fica-fonte-única)). Não copie conteúdo entre docs; aponte com link.

## Procedimentos (skills)

Para as tarefas abaixo, **leia o arquivo indicado e siga os passos**. Algumas ferramentas (Claude Code, Cursor, Copilot) já carregam esses arquivos sozinhas; nas outras, abra o arquivo.

| Tarefa | Arquivo |
|---|---|
| Criar uma funcionalidade nova (client + server + shared + docs) | [.claude/skills/nova-funcionalidade/SKILL.md](.claude/skills/nova-funcionalidade/SKILL.md) |
| Registrar uma decisão técnica (ADR) | [.claude/skills/novo-adr/SKILL.md](.claude/skills/novo-adr/SKILL.md) |
| Começar uma tarefa / criar ou renomear branch | [.claude/skills/nova-branch/SKILL.md](.claude/skills/nova-branch/SKILL.md) |
| Escrever mensagem de commit ou título de PR, commitar | [.claude/skills/fazer-commit/SKILL.md](.claude/skills/fazer-commit/SKILL.md) |
| Deixar uma mudança pronta para PR | [.claude/skills/preparar-pr/SKILL.md](.claude/skills/preparar-pr/SKILL.md) |
| Dividir uma tarefa entre vários agentes e coordenar | [.claude/skills/orquestrar-tarefa/SKILL.md](.claude/skills/orquestrar-tarefa/SKILL.md) |

## Vários agentes em paralelo

O trabalho é dividido entre agentes especializados, cada um com uma área fixa (ex.: `server-funcionalidade` só edita `server/.../Features/`). Se você foi chamado como um desses agentes, siga a definição em [.claude/agents/](.claude/agents/) e fique na sua área. O fluxo (contrato primeiro, depois server e client em paralelo, cada um no seu worktree) está em [docs/agentes.md](docs/agentes.md).

## Onde ler mais

- [docs/arquitetura.md](docs/arquitetura.md): organização e regras de dependência
- [docs/convencoes.md](docs/convencoes.md): nomes, C#, formatação
- [docs/git.md](docs/git.md): branches, commits, PRs e proteção da `main`
- [CONTRIBUTING.md](CONTRIBUTING.md): responsabilidades, Unity sem conflito, uso de IA
- [docs/features/](docs/features/): regras de cada funcionalidade
- [docs/brainstorm/figjam.md](docs/brainstorm/figjam.md): ideias do FigJam, **não validadas** (não trate como regra). Não leia o FigJam direto pelo conector de Figma: a cota do plano gratuito é mínima; use este arquivo.
