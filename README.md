# Jogo do Grupo

MMORPG para PC desenvolvido em grupo. Mundo aberto, economia movida pelos jogadores e sistema de fusão de itens.

| Parte | Tecnologia | Pasta |
|---|---|---|
| Cliente | Unity 6000.6.5f1, URP (C# 9) | `client/` |
| Servidor | .NET 10 (C#), sem engine, autoritativo | `server/` |
| Código compartilhado | Biblioteca C# (.NET Standard 2.1, C# 9) | `shared/` |

**Chegou agora?** Siga [Primeiros passos](#primeiros-passos) do começo ao fim: em cerca de uma hora você tem tudo rodando e pronto para o primeiro PR.

## Primeiros passos

### 1. Acessos

Peça ao responsável pelo core (hoje, [@GabrielNied](https://github.com/GabrielNied)):

- [ ] Acesso de escrita (**collaborator**) ao repositório [GabrielNied/jogo_do_grupo](https://github.com/GabrielNied/jogo_do_grupo). Sem ele você consegue clonar, mas não enviar branches.
- [ ] Acesso ao board do FigJam **"Jogo do Grupo - Definição de Features"**, onde ficam as ideias.
- [ ] Qual funcionalidade será sua (fica registrado em [docs/responsaveis.md](docs/responsaveis.md)).

Você também precisa de uma conta no [GitHub](https://github.com) e de uma conta [Unity ID](https://id.unity.com) (a licença **Unity Personal**, gratuita, basta).

### 2. Instale os requisitos

Os comandos deste guia são para **Windows** e funcionam no Git Bash e no PowerShell.

| Ferramenta | Versão | Para quê | Conferir com |
|---|---|---|---|
| [Git for Windows](https://git-scm.com/download/win) | 2.40 ou mais nova | Controle de versão (inclui o Git Bash, usado pelos hooks) | `git --version` |
| [Git LFS](https://git-lfs.com) | 3.x (já vem no Git for Windows) | Arquivos grandes: imagens, modelos, áudio | `git lfs version` |
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | **10.0** | Compilar e rodar servidor e shared; CSharpier | `dotnet --version` |
| [Node.js](https://nodejs.org) | **20 ou mais nova** (recomendado: 24 LTS) | Só para as ferramentas do git (commitlint, hooks). O jogo não usa Node. | `node --version` |
| [Unity Hub](https://unity.com/download) | Mais recente | Instalar e abrir o Unity | — |
| Unity Editor | **6000.6.5f1** (exatamente essa) | Cliente do jogo | Unity Hub > Installs |
| Editor de código | VS Code, Rider ou Visual Studio | Escrever C# | — |

**Instalando o Unity certo:** no Unity Hub, vá em **Installs > Install Editor > Archive** e procure a versão `6000.6.5f1`. Também dá para colar `unityhub://6000.6.5f1/3ff58d469c8a` na barra do navegador, que abre o Hub direto nela. Os módulos extras não são necessários para começar. Versão diferente **não serve**: o Unity atualizaria os arquivos do projeto e geraria conflitos para todo mundo.

**Extensões do editor de código:**

| Editor | Instale |
|---|---|
| VS Code | Aceite as extensões recomendadas que o VS Code sugere ao abrir o repo (CSharpier, C# Dev Kit, Unity, EditorConfig). Lista em [.vscode/extensions.json](.vscode/extensions.json). |
| Rider | Plugin "CSharpier" (o suporte a Unity já vem embutido) |
| Visual Studio | Extensão "CSharpier" e a carga de trabalho "Desenvolvimento de jogos com Unity" |

Em todos, ative **formatar ao salvar** com o CSharpier ([como fazer](docs/convencoes.md#formatação)).

### 3. Configure o Git (uma vez por máquina)

```
git config --global user.name "Seu Nome"
git config --global user.email "seu-email-do-github@exemplo.com"
git lfs install
```

### 4. Clone o repositório

Clone **fora do OneDrive, Dropbox ou Google Drive** e num caminho curto e sem espaços (ex.: `C:\Dev`). A sincronização dessas pastas corrompe o `.git` e a `Library` do Unity.

```
cd C:/Dev
git clone https://github.com/GabrielNied/jogo_do_grupo.git
cd jogo_do_grupo
```

### 5. Instale as ferramentas do projeto (uma vez por clone)

```
dotnet tool restore
npm install
```

- `dotnet tool restore` instala o **CSharpier** na versão fixada do projeto.
- `npm install` instala o **commitlint** e ativa os **hooks do git**, que conferem as mensagens de commit, o nome das branches e bloqueiam push na `main`. Veja [docs/git.md](docs/git.md).

Configure também o **merge inteligente do Unity** para cenas e prefabs. É uma configuração local do clone; ajuste o caminho se instalou o Unity em outra pasta:

```
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.6.5f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
git config merge.unityyamlmerge.recursive binary
```

### 6. Rode o servidor

```
dotnet build JogoDoGrupo.slnx
dotnet run --project server/src/JogoDoGrupo.Server
```

O build termina com `0 Warning(s)` e `0 Error(s)`, e o servidor lista os módulos e termina com:

```
Servidor iniciado com 14 funcionalidades.
```

### 7. Abra o cliente no Unity

1. No Unity Hub: **Projects > Add > Add project from disk** e selecione a pasta `client/` do repositório.
2. Abra o projeto. A primeira abertura demora alguns minutos, porque o Unity monta a pasta `Library/`.
3. Confira:
   - o **Console** (Window > General > Console) está sem erros vermelhos;
   - no **Package Manager** (Window > Package Manager > In Project) aparece **"Jogo do Grupo - Shared"**;
   - a cena `Assets/_Game/Core/Scenes/SampleScene` abre normalmente.
4. Em **Edit > Preferences > External Tools**, escolha seu editor de código em **External Script Editor**. É isso que faz o autocomplete funcionar nos scripts do Unity.

Mais detalhes, e quais configurações do projeto não devem ser alteradas: [client/README.md](client/README.md).

### 8. Confira se está tudo pronto

| Conferir | Comando | Resultado esperado |
|---|---|---|
| Formatação | `dotnet csharpier check .` | `Checked N files` sem erros |
| Build | `dotnet build JogoDoGrupo.slnx` | `0 Warning(s)`, `0 Error(s)` |
| Hooks ativos | `git config core.hooksPath` | `.husky/_` |
| commitlint | `echo "feat(itens): testa commitlint" \| npx commitlint` | Nenhuma mensagem de erro |
| Validador de branch | `npm run validar-branch -- feature/itens/teste` | `✔ Branch "feature/itens/teste" no padrão.` |
| Unity | Abrir o projeto | Console sem erros e pacote Shared presente |

### 9. Leia antes do primeiro PR

1. [CONTRIBUTING.md](CONTRIBUTING.md): a regra de não mexer na pasta dos outros e como evitar conflitos no Unity.
2. [docs/git.md](docs/git.md): branches, commits e PRs.
3. [docs/arquitetura.md](docs/arquitetura.md) e [docs/convencoes.md](docs/convencoes.md): como o código se organiza e os nomes.
4. A página da sua funcionalidade em [docs/features/](docs/features/).

### 10. Sua primeira contribuição

```
git switch main
git pull --rebase origin main
git switch -c feature/itens/minha-tarefa          # <tipo>/<escopo>/<descricao>
# ... trabalhe ...
dotnet csharpier format .
git add <arquivos>
git commit -m "feat(itens): adiciona minha tarefa"  # <tipo>(<escopo>): <descrição>
git push -u origin feature/itens/minha-tarefa
```

Depois, abra o PR no GitHub com o título no mesmo padrão do commit e preencha o template. O PR entra na `main` com os checks verdes (`csharpier`, `build`, `padrao-git`) e a aprovação do responsável pela pasta. Detalhes em [docs/git.md](docs/git.md).

## Dia a dia

| Quando | Comando |
|---|---|
| Começar o dia / nova tarefa | `git switch main && git pull --rebase origin main` |
| Atualizar sua branch com a `main` | `git pull --rebase origin main` |
| Antes de commitar C# | `dotnet csharpier format .` |
| Compilar e rodar o servidor | `dotnet build JogoDoGrupo.slnx` e `dotnet run --project server/src/JogoDoGrupo.Server` |
| Mudou o `package-lock.json` | `npm install` |
| Mudou o `dotnet-tools.json` | `dotnet tool restore` |
| Vai editar um binário (PSD, FBX, Blend) | `git lfs lock <arquivo>` antes; `git lfs unlock <arquivo>` depois |

## Problemas comuns

| Sintoma | Solução |
|---|---|
| `Could not execute because the specified command or file was not found` ao rodar `dotnet csharpier` | Rode `dotnet tool restore` na raiz do repo. |
| O commit passa sem validação, ou `git config core.hooksPath` vem vazio | Rode `npm install` na raiz do repo. |
| O hook recusou a mensagem de commit | Corrija para `<tipo>(<escopo>): <descrição em minúsculas>`. Veja [docs/git.md](docs/git.md#commits). |
| `Não envie direto para a main` ao dar push | Crie uma branch: `git switch -c feature/<escopo>/<descricao>`. |
| Imagens ou modelos aparecem quebrados no Unity, ou arquivos com poucas linhas de texto no lugar de binários | O LFS não baixou os arquivos: `git lfs install` e depois `git lfs pull`. |
| O Unity pede para atualizar ou trocar a versão do projeto | Cancele e abra com a **6000.6.5f1**. Nunca aceite o upgrade. |
| O Unity mostra erros de compilação logo após um `git pull` | Feche o Unity, rode `git status` para conferir se não há conflitos e abra de novo. Se persistir, apague `client/Library/` (o Unity recria). |
| Conflito em `.unity` ou `.prefab` | Confirme que o merge do Unity está configurado (passo 5). Na dúvida, fale com o dono da cena antes de resolver. |
| Pasta do repo dentro do OneDrive | Mova para fora (ex.: `C:\Dev`) e clone de novo. |

## Estrutura

```
jogo_do_grupo/
├── client/          Projeto Unity
│   └── Assets/_Game/
│       ├── Core/            Infra do cliente (dono: core)
│       └── Features/<X>/    Uma pasta por funcionalidade
├── server/
│   └── src/JogoDoGrupo.Server/
│       ├── Core/            Infra do servidor (dono: core)
│       └── Features/<X>/    Uma pasta por funcionalidade
├── shared/          Regras, tipos e mensagens usados por cliente e servidor
│   └── Package/Runtime/
│       ├── Core/
│       └── Features/<X>/
├── docs/            Documentação (visão, arquitetura, features, decisões)
├── .claude/skills/  Procedimentos para assistentes de IA (ver AGENTS.md)
├── .github/         CODEOWNERS, templates de PR/issue, CI, ruleset da main
├── .husky/          Hooks do git (commitlint, bloqueio de push na main)
├── scripts/git/     Validador de nome de branch
└── AGENTS.md        Instruções para qualquer assistente de IA
```

A regra principal: **cada funcionalidade vive na própria pasta, nas três partes (client, server, shared)**. Quem é responsável por uma funcionalidade mexe só nas pastas dela. Veja [docs/arquitetura.md](docs/arquitetura.md).

## Funcionalidades

| Funcionalidade | Pasta | IDs de mensagem | Descrição |
|---|---|---|---|
| Core | `Core` | 0–999 | Rede, autenticação, persistência, eventos |
| Mundo | `Mundo` | 1000–1999 | Mapa aberto, regiões, clima, exploração e viagem rápida. |
| NPCs | `Npcs` | 2000–2999 | Papel dos NPCs no mundo, comerciantes e reputação. |
| Personagem | `Personagem` | 3000–3999 | Raças, classes, atributos e progressão de nível. |
| Skills (magias) | `Skills` | 4000–4999 | Magias de cada classe, custo e recarga. |
| Habilidades (talentos) | `Talentos` | 5000–5999 | Árvore de pontos de talento. |
| Combate | `Combate` | 6000–6999 | Cálculo de dano, seleção de alvo e efeitos de status. Usado por PvE e PvP. |
| Itens | `Itens` | 7000–7999 | Inventário, equipamentos, raridades e sistema de fusão. |
| Profissões | `Profissoes` | 8000–8999 | Craft e coleta, conectados à economia. |
| Economia | `Economia` | 9000–9999 | Moedas, mercado, comércio entre jogadores, saídas de recursos e taxas. |
| Missões | `Missoes` | 10000–10999 | História principal, missões secundárias e diárias. |
| Mobs e bosses | `Mobs` | 11000–11999 | Tipos de mobs, comportamento por região e bosses com mecânicas marcantes. |
| Dungeons e raids | `Instancias` | 12000–12999 | Conteúdo instanciado: dungeons para grupos pequenos e raids para grupos grandes (endgame). |
| Social | `Social` | 13000–13999 | Party (time), guilda e chat. |
| PvP | `Pvp` | 14000–14999 | Arenas, PvP no mundo aberto e guerra de guildas. |

Responsáveis por cada uma: [docs/responsaveis.md](docs/responsaveis.md).

## Documentação

- [Visão geral do jogo](docs/visao-geral.md)
- [Arquitetura](docs/arquitetura.md)
- [Convenções de código](docs/convencoes.md)
- [Fluxo do Git: branches, commits e PRs](docs/git.md)
- [Funcionalidades](docs/features/)
- [Decisões técnicas (ADRs)](docs/decisoes/)
- [Cliente Unity](client/README.md) · [Servidor](server/README.md) · [Shared](shared/README.md)

## Usando IA no projeto

Qualquer assistente (Claude, ChatGPT/Codex, Copilot, Cursor, Gemini...) deve seguir o [AGENTS.md](AGENTS.md): ele resume as regras do repo e lista os procedimentos prontos (skills) em [.claude/skills/](.claude/skills/). Veja [CONTRIBUTING.md](CONTRIBUTING.md#usando-ia).

## Status e próximos passos

Estrutura do repo, projeto Unity, CI (build, CSharpier, commitlint), hooks do git e documentação base prontos. Falta:

1. Primeiro commit na `main` e push; conferir os workflows passando no GitHub.
2. Proteger a `main`: importar o ruleset e ajustar as opções de merge ([docs/git.md](docs/git.md#proteção-da-main-para-o-admin-do-repo)).
3. Preencher [docs/responsaveis.md](docs/responsaveis.md), ativar o [.github/CODEOWNERS](.github/CODEOWNERS) e adicionar o grupo como collaborators.
4. Decidir (e registrar em ADR): biblioteca de rede, banco de dados, divisão do mundo entre servidores, testes.
5. Primeiro marco: a [fatia jogável](docs/visao-geral.md#primeiro-marco-fatia-jogável).
