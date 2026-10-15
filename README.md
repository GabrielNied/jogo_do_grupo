# Jogo do Grupo

RPG de ação online para PC, medieval, de mundo aberto. ~~feito para jogar com poucas pessoas.~~ Sem classes: as skills estão nos itens, e o grupo, com papéis de tank, healer e DPS, sobe de nível, junta loot e derrota bosses cada vez mais fortes em dungeons e raids. Veja a [visão geral](docs/visao-geral.md).

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
| Editor de código | VS Code, Cursor, Rider ou Visual Studio | Escrever C# | — |
| [GitHub CLI](https://cli.github.com) (recomendado) | Mais recente | Abrir e revisar PRs pelo terminal; usado pelos assistentes de IA | `gh --version` |

**Instalando o Unity certo:** no Unity Hub, vá em **Installs > Install Editor > Archive** e procure a versão `6000.6.5f1`. Também dá para colar `unityhub://6000.6.5f1/3ff58d469c8a` na barra do navegador, que abre o Hub direto nela. Os módulos extras não são necessários para começar. Versão diferente **não serve**: o Unity atualizaria os arquivos do projeto e geraria conflitos para todo mundo.

**Extensões do editor de código:**

| Editor | Instale |
|---|---|
| VS Code | Aceite as extensões recomendadas que o VS Code sugere ao abrir o repo (CSharpier, C# Dev Kit, Unity, EditorConfig). Lista em [.vscode/extensions.json](.vscode/extensions.json). |
| Cursor | CSharpier, EditorConfig e **C# (Anysphere)** (`anysphere.csharp`). As extensões C# Dev Kit e Unity, da Microsoft, não existem no marketplace do Cursor. |
| Rider | Plugin "CSharpier" (o suporte a Unity já vem embutido) |
| Visual Studio | Extensão "CSharpier" e a carga de trabalho "Desenvolvimento de jogos com Unity" |

No VS Code e no Cursor, o **formatar ao salvar** com o CSharpier já vem ligado pelo [.vscode/settings.json](.vscode/settings.json). No Rider e no Visual Studio, ative manualmente ([como fazer](docs/convencoes.md#formatação)).

### 3. Configure o Git (uma vez por máquina)

```
git config --global user.name "Seu Nome"
git config --global user.email "seu-email-do-github@exemplo.com"
git lfs install
```

Se instalou o GitHub CLI, faça login (abre o navegador; escolha **HTTPS** e aceite autenticar o Git com as credenciais do GitHub):

```
gh auth login
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
npm run server:rodar        # o mesmo que dotnet run --project server/src/JogoDoGrupo.Server
```

O build termina com `0 Warning(s)` e `0 Error(s)`. O servidor lista os módulos, cria o banco local (`server/src/JogoDoGrupo.Server/jogodogrupo.db`, fora do git) e fica rodando:

```
Servidor iniciado com 14 funcionalidades.
Now listening on: http://localhost:5080
```

Deixe-o rodando enquanto testa o client; para parar, `Ctrl+C`. Detalhes de configuração em [docs/arquitetura.md](docs/arquitetura.md#servidor-como-uma-funcionalidade-se-pluga).

### 7. Abra o cliente no Unity

1. No Unity Hub: **Projects > Add > Add project from disk** e selecione a pasta `client/` do repositório.
2. Abra o projeto. A primeira abertura demora alguns minutos, porque o Unity monta a pasta `Library/`.
3. Confira:
   - o **Console** (Window > General > Console) está sem erros vermelhos;
   - no **Package Manager** (Window > Package Manager > In Project) aparece **"Jogo do Grupo - Shared"**;
   - a cena `Assets/_Game/Core/Scenes/SampleScene` abre normalmente.
4. Em **Edit > Project Settings > Player > Other Settings**, confira que **Allow downloads over HTTP** está em "Allowed in development builds". Sem isso, o client não consegue falar com o servidor local.
5. Com o servidor rodando (passo 6), dê Play: aparece a abertura e depois o login. Crie uma conta para testar.
6. Em **Edit > Preferences > External Tools**, escolha seu editor de código em **External Script Editor**. É isso que faz o autocomplete funcionar nos scripts do Unity.
   - **Cursor:** o Unity ainda não reconhece o Cursor (o pacote de IDE do Unity só conhece Visual Studio, VS Code e Rider), então não gera os `.csproj` e o autocomplete das APIs do Unity não funciona. Até o core decidir uma solução (ex.: um pacote de integração com o Cursor), use o Cursor para server e shared, e o VS Code, o Rider ou o Visual Studio para os scripts do Unity.

Mais detalhes, e quais configurações do projeto não devem ser alteradas: [client/README.md](client/README.md).

### 8. Confira se está tudo pronto

| Conferir | Comando | Resultado esperado |
|---|---|---|
| Formatação | `dotnet csharpier check .` | `Checked N files` sem erros |
| Build | `dotnet build JogoDoGrupo.slnx` | `0 Warning(s)`, `0 Error(s)` |
| Testes | `dotnet test JogoDoGrupo.slnx` | `Passed!` nos dois projetos de teste |
| Client compila | `npm run unity:compilar` | `✔ Client compila contra o Unity ...` |
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

Depois, abra o PR no GitHub com o título no mesmo padrão do commit e preencha o template. Peça review ao responsável pela pasta (Reviewers, no PR). O combinado é fazer o merge só com os checks verdes (`csharpier`, `build`, `padrao-git`) e a aprovação de outra pessoa. Detalhes em [docs/git.md](docs/git.md).

## Dia a dia

| Quando | Comando |
|---|---|
| Começar o dia / nova tarefa | `git switch main && git pull --rebase origin main` |
| Atualizar sua branch com a `main` | `git pull --rebase origin main` |
| Antes de commitar C# | `dotnet csharpier format .` |
| Compilar e rodar o servidor | `dotnet build JogoDoGrupo.slnx` e `npm run server:rodar` |
| Mudou o `package-lock.json` | `npm install` |
| Mudou o `dotnet-tools.json` | `dotnet tool restore` |
| Vai editar um binário (PSD, FBX, Blend) | `git lfs lock <arquivo>` antes; `git lfs unlock <arquivo>` depois |

## Problemas comuns

| Sintoma | Solução |
|---|---|
| `Could not execute because the specified command or file was not found` ao rodar `dotnet csharpier` | Rode `dotnet tool restore` na raiz do repo. |
| O commit passa sem validação, ou `git config core.hooksPath` vem vazio | Rode `npm install` na raiz do repo. |
| O hook recusou a mensagem de commit | Corrija para `<tipo>(<escopo>): <descrição em minúsculas>`. Veja [docs/git.md](docs/git.md#commits). |
| `npm` falha no PowerShell por política de execução | Use `npm.cmd` nesta máquina ou peça ao administrador uma política de execução compatível. |
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
├── tests/           Testes automatizados (xUnit) do shared e do servidor
├── docs/            Documentação (visão, arquitetura, features, decisões)
├── .claude/
│   ├── agents/      Agentes especializados para trabalho em paralelo (ver docs/agentes.md)
│   └── skills/      Procedimentos para assistentes de IA (ver AGENTS.md)
├── .agents/         Entradas das skills para descoberta automática pelo Codex
├── .codex/          Perfis e hook equivalentes para sessões locais do Codex
├── .github/         CODEOWNERS, templates de PR/issue, CI, ruleset da main
├── .husky/          Hooks do git (commitlint, bloqueio de push na main)
├── scripts/         Validador de branch, export do FigJam, hooks dos agentes
└── AGENTS.md        Instruções para qualquer assistente de IA
```

A regra principal: **cada funcionalidade vive na própria pasta, nas três partes (client, server, shared)**. Quem é responsável por uma funcionalidade mexe só nas pastas dela. Veja [docs/arquitetura.md](docs/arquitetura.md).

## Funcionalidades

| Funcionalidade | Pasta | IDs de mensagem | Descrição |
|---|---|---|---|
| Core | `Core` | 0–999 | Rede, autenticação, persistência, eventos |
| Mundo | `Mundo` | 1000–1999 | Cidade e regiões de um mundo persistente; clima e ciclo de dia e noite. ~~Regiões jogadas em sessões de grupo pequeno~~ substituído em 2026-10-11. |
| NPCs | `Npcs` | 2000–2999 | Papel dos NPCs no mundo, comerciantes e reputação. |
| Personagem | `Personagem` | 3000–3999 | Raças (com skill base), atributos e progressão de nível. Sem classes. |
| Skills (magias) | `Skills` | 4000–4999 | Skills sorteadas nos itens e skill base da raça; custo e recarga. |
| Árvore de atributos (talentos) | `Talentos` | 5000–5999 | Árvore circular de atributos: nós, pontos por nível, compra no servidor e tela (tecla N). Redistribuição e perfis em andamento. |
| Combate | `Combate` | 6000–6999 | Combate de ação (estilo New World): dano, esquiva, efeitos de status. Usado por PvE e PvP. |
| Itens | `Itens` | 7000–7999 | Inventário, equipamentos, raridades e sistema de fusão. |
| Profissões | `Profissoes` | 8000–8999 | Craft e coleta, conectados à economia. |
| Economia | `Economia` | 9000–9999 | Moedas, mercado, comércio entre jogadores, saídas de recursos e taxas. |
| Missões | `Missoes` | 10000–10999 | História principal, missões secundárias e diárias. |
| Mobs e bosses | `Mobs` | 11000–11999 | Tipos de mobs, comportamento por região e bosses com mecânicas marcantes. |
| Dungeons e raids | `Instancias` | 12000–12999 | Dungeons (5 jogadores) e raids (10), com bosses cada vez mais fortes. |
| Social | `Social` | 13000–13999 | Party (time), guilda e chat. |
| PvP | `Pvp` | 14000–14999 | Arenas, PvP no mundo aberto e guerra de guildas. |
| Desenvolvimento | `Desenvolvimento` | 15000–15999 | Comandos de teste no editor e no build de desenvolvimento (experiência, teleporte, spawn). |
| Montarias | `Montarias` | 16000–16999 | Chamada de montaria, velocidade aumentada autoritativa e estado montado compartilhado. |

Responsáveis por cada uma: [docs/responsaveis.md](docs/responsaveis.md).

## Documentação

- [Visão geral do jogo](docs/visao-geral.md) · [Jogos de referência](docs/referencias.md)
- [Arquitetura](docs/arquitetura.md)
- [Convenções de código](docs/convencoes.md)
- [Fluxo do Git: branches, commits e PRs](docs/git.md)
- [Funcionalidades](docs/features/)
- [Decisões técnicas (ADRs)](docs/decisoes/)
- [Cliente Unity](client/README.md) · [Servidor](server/README.md) · [Shared](shared/README.md)

## Usando IA no projeto

Qualquer assistente (Claude, ChatGPT/Codex, Copilot, Cursor, Gemini...) deve seguir o [AGENTS.md](AGENTS.md): ele resume as regras do repo e lista os procedimentos prontos (skills) em [.claude/skills/](.claude/skills/). Veja [CONTRIBUTING.md](CONTRIBUTING.md#usando-ia). Para vários agentes implementando partes diferentes ao mesmo tempo, veja [docs/agentes.md](docs/agentes.md).

## Status e próximos passos

Estrutura do repo, projeto Unity, CI (build, CSharpier, commitlint), hooks do git e documentação base prontos e publicados. Falta:

1. ~~Proteger a `main` com ruleset~~ decidido: repo privado com proteção local ([ADR 0007](docs/decisoes/0007-protecao-local-da-main.md)). Opções de merge já aplicadas.
2. Preencher [docs/responsaveis.md](docs/responsaveis.md) e o [.github/CODEOWNERS](.github/CODEOWNERS); convites de collaborator enviados (aguardando aceite).
3. Decidir como integrar o Cursor ao Unity (o Unity não gera `.csproj` para ele).
4. ~~Decidir a biblioteca de rede~~ decidido: LiteNetLib por UDP ([ADR 0015](docs/decisoes/0015-litenetlib-para-o-tempo-real.md)). Ainda em aberto: divisão do mundo entre servidores. ~~Banco de dados e testes~~ já foram decididos nos ADRs 0010 e 0013.
5. Hospedagem gratuita para o grupo jogar junto: proposta no [ADR 0021](docs/decisoes/0021-hospedagem-gratuita.md), tarefas na issue #197.
6. Primeiro marco: a [fatia jogável](docs/visao-geral.md#primeiro-marco-fatia-jogável).
