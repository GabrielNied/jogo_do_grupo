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
npm run server:rodar                 # sobe o servidor (dotnet run --project server/src/JogoDoGrupo.Server)
dotnet csharpier format .            # formata (rode antes de terminar qualquer mudança em C#)
dotnet csharpier check .             # o que o CI confere
dotnet test JogoDoGrupo.slnx         # todos os testes (o CI também roda)
npm run unity:compilar               # compila o client contra as DLLs do Unity (editor fechado)
npm run unity:recompilar             # compila no editor aberto (erros reais do Unity)
npm run unity:testes                 # testes EditMode do client
npm run unity:executar -- <Metodo>   # roda um script de editor em modo batch (editor fechado)
npm run unity:editor                 # sai 1 se o editor do projeto está aberto (ignora o Hub)
npm run unity:meta                   # gera os .meta de scripts, pastas, .asmdef, .uss/.tss e textos, sem Unity
npm run unity:importar               # gera os outros .meta (prefab, asset...) e fecha (editor fechado)
npm run limpar-agentes               # simula a limpeza de worktrees e branches já mescladas
npm run limpar-agentes -- --aplicar  # remove (só o que já está na main; --remotas inclui o GitHub)
npm run validar-branch -- <branch>   # confere nome de branch
npm run tarefa -- listar             # lista tarefas livres e em andamento
npm run tarefa -- puxar <N>          # puxa a tarefa para em andamento e cria a branch
npm run tarefa -- liberar <N>        # libera a tarefa de volta se pausar/cancelar
printf '%s
' "<msg>" | npx --no -- commitlint   # confere mensagem de commit
```

Testes em `tests/` (xUnit; [ADR 0013](docs/decisoes/0013-testes-automatizados.md), guia em [docs/convencoes.md](docs/convencoes.md#testes)). O editor do Unity não roda pela linha de comando, mas `npm run unity:compilar` confere se o client compila.

## Regras que você deve seguir

0. **Nunca apague nada do Figma.** Esta regra vem antes de todas as outras. O resto pode ser apagado quando for sujeira ou estiver obsoleto (decisão de 2026-10-11, [ADR 0026](docs/decisoes/0026-ia-pode-apagar-menos-o-figma.md)).
   - **Figma:** não apague arquivos, páginas, frames, nós, componentes, variáveis nem comentários, nem por script (`use_figma`). Pode ler e criar; editar só o que a tarefa pede.
   - **O resto pode sair:** arquivos, pastas, assets, trechos de docs, código morto, branches, worktrees e `.meta` órfãos. Apagou? Deixe o commit dizer o quê e por quê, e atualize os links que apontavam para lá.
   - **Olhe antes de apagar:** confira o que é, quem usa (referências, guids de assets do Unity) e se há mudança sua não commitada. Dúvida real sobre o que é, ou sobre algo de outra pessoa em andamento: pergunte.
   - **Confirme antes do que não volta ou é do grupo todo:** push forçado (nunca na `main`), apagar branch remota de PR aberto, fechar issue ou PR de outra pessoa, apagar repositório, release ou projeto no GitHub. O hook `bloquear-apagar` barra o push forçado e o apagar repositório, release ou projeto; peça a quem decide.
   - **Worktrees e branches de agentes** continuam saindo com `npm run limpar-agentes -- --aplicar` ([ADR 0018](docs/decisoes/0018-limpeza-do-que-os-agentes-criam.md)), que só remove o que já está na `main`.
1. **Fique nas pastas da funcionalidade da tarefa** (`Features/<X>/` em client, server e shared, e `docs/features/<x>.md`). Se precisar mexer em outra funcionalidade ou em `Core/`, pare e avise a pessoa: isso exige review do responsável.
2. **Funcionalidades não se referenciam diretamente.** Comunicação por eventos e contratos definidos por quem publica: se o **client usa** (mensagens de rede, DTOs, regras puras), em `shared/Package/Runtime/Features/<dono>/`; se **só o servidor usa** (eventos e consultas entre funcionalidades), em `server/src/JogoDoGrupo.Server/Contratos/<Dono>/` ([ADR 0016](docs/decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)). Não adicione referência de um `.asmdef` de feature para outro.
3. **Servidor autoritativo:** regras que decidem resultado (dano, drop, fusão, preço) rodam no servidor. O cliente envia intenções e mostra resultados.
4. **IDs de mensagem** só dentro da faixa da funcionalidade (`<X>MessageIds.Faixa`).
5. **Shared:** nada de `UnityEngine`, nada acima de C# 9. Use `namespace X { }` com chaves (namespace de arquivo não compila no Unity).
6. **Server:** C# moderno com namespace de arquivo (`namespace X;`). Módulos implementam `IFeatureModule` e são descobertos sozinhos; não crie arquivo central de registro.
7. **Nomes de domínio em português sem acento** (`Itens`, `Fusao`, `Profissoes`); termos técnicos em inglês são ok (`Module`, `Handler`). Campos privados `_camelCase`. Detalhes em [docs/convencoes.md](docs/convencoes.md).
8. **Formatação é do CSharpier.** Não ajuste estilo à mão nem discuta formatação; rode `dotnet csharpier format .`. Sem .NET (sessão de IA na nuvem)? Abra o PR: se o check `csharpier` falhar, o CI publica o diff num comentário do PR; aplique com `git apply` e faça push.
9. **Unity:** nunca escreva à mão cenas (`.unity`), prefabs, `.asset` ou `.meta`, nem mexa em `ProjectSettings/` sem pedido explícito. A exceção é o `.meta` de **script C#, pasta, `.asmdef`, `.uss`/`.tss` e texto**, que não depende do Unity: gere com `npm run unity:meta` (nunca à mão, e nunca troque um `.meta` que já existe). Conteúdo do Unity é **criado pelo próprio Unity**: por script de editor (`npm run unity:executar`) ou pelo editor aberto via Unity CLI ([ADR 0014](docs/decisoes/0014-conteudo-3d-e-agentes-artistas.md)). Assets de terceiros só CC0 (ou CC-BY com crédito), em `client/Assets/ThirdParty/`, registrados em [docs/arte/licencas.md](docs/arte/licencas.md).
10. **Todo PR de código traz testes** do que mudou (caminho feliz, erros, limites, concorrência quando houver). A verificação local é proporcional ao que mudou, conforme a matriz da skill `preparar-pr`: não rode build, testes .NET ou Unity por ritual em PR só de documentação, configuração ou instruções de IA. Código server/shared roda `dotnet test`; client/shared também roda `npm run unity:compilar`; scripts e hooks têm validação direcionada. **Sem Unity no ambiente** (ex.: nuvem), os comandos `unity:*` dizem "pulado": não tente de novo nem peça à pessoa para rodar na máquina dela. Abra o PR normal (pode ir para merge sem essa validação) e registre o que ficou sem fazer ou sem validar com `npm run tarefa -- pendencia-unity <PR> --falta "..."`. O próximo agente numa máquina com Unity puxa a pendência e resolve. O CI "Client (Unity)" compila e testa o client depois do merge ([ADR 0019](docs/decisoes/0019-unity-no-ci.md)); se falhar, ele mesmo abre uma pendência.
11. **Mudou regra do jogo?** Atualize `docs/features/<x>.md` na mesma mudança. **Decisão técnica?** Crie um ADR.
12. **Git:** nunca commite nem dê push na `main`; trabalhe numa branch `<tipo>/<escopo>/<descricao>`. Commits e títulos de PR no padrão `<tipo>(<escopo>): <descrição em português, minúscula>`, validados pelo commitlint (tipos e escopos em `commitlint.config.mjs`). Nunca use `--no-verify`. Só faça commit ou push quando a pessoa pedir (pedir uma orquestração de agentes conta como pedido para as branches dos agentes; merge é sempre humano). Detalhes em [docs/git.md](docs/git.md).
13. **Economize IA sem perder qualidade.** Qualidade vem primeiro; economize no que não a afeta:
    - Leia só o que a tarefa precisa (AGENTS.md, a doc da funcionalidade, o código que vai mudar); não varra o repo inteiro.
    - Verifique por comando adequado à alteração (matriz da skill `preparar-pr`), não à mão: nada de subir servidor, `curl` ou projetos temporários se um teste resolve. Se faltar teste de código, escreva-o em vez de testar à mão.
    - Não repita o que o CI já confere, nem reverifique o que já passou.
    - Dúvida de regra ou contrato: pergunte antes de implementar, para não refazer.
    - Respostas e relatórios curtos: o que mudou, links e pendências. Nada de repetir o pedido ou listar verificações que passaram.
    - Reaproveite padrões e ajudantes que já existem (ex.: `ServidorDeTeste`) em vez de criar outros.
14. Cada informação mora em um documento só (tabela em [docs/README.md](docs/README.md#onde-cada-coisa-fica-fonte-única)). Não copie conteúdo entre docs; aponte com link.
15. **Nunca trabalhe em tarefa já em andamento:** antes de alterar código, confira as tarefas abertas e puxe a tarefa para em andamento (`npm run tarefa -- puxar <N>`). Se a tarefa já estiver marcada com `em-andamento` ou `In Progress` por outra pessoa ou agente, pare e avise: não duplique trabalho. Se precisar pausar ou transferir a tarefa sem concluir, libere-a com `npm run tarefa -- liberar <N>`. Em orquestrações paralelas, cada agente atua em sua própria subtarefa/issue (`npm run tarefa -- subtarefa`). O PR sempre referencia a issue com `Closes #<N>`.
16. **Gargalo ou melhoria de custo de IA: implemente na hora.** Se a tarefa mostrar um retrabalho que se repete (agente relançado, revisor em cima de teste que já existe, comando que falha e abre outra rodada, espera que um script resolve), corrija no mesmo PR ou num PR pequeno de processo. Não deixe só anotado.
    - O corte é de consumo: menos agentes, menos releitura, menos ida e volta. A qualidade mínima continua: teste do que mudou, servidor autoritativo, CI e a regra 0.
    - Não use isso para ampliar o jogo, trocar regra de design ou reescrever o que já funciona.
    - Agente de funcionalidade que esbarrar nisso descreve o corte no relatório. O coordenador implementa antes de encerrar, se couber numa mudança pequena de skill, script ou doc de processo.
    - Uma curiosidade ou um polimento visual não é gargalo. Gargalo é a mesma espera acontecendo de novo.

17. **Balanceamento:** separe valores ajustáveis dos cálculos, em constantes nomeadas no topo da classe ou `Config<Assunto>` da funcionalidade. Siga [Valores de balanceamento](docs/convencoes.md#valores-de-balanceamento).

## Procedimentos (skills)

Para as tarefas abaixo, **leia o arquivo indicado e siga os passos**. Algumas ferramentas (Claude Code, Cursor, Copilot) já carregam esses arquivos sozinhas; nas outras, abra o arquivo.

| Tarefa | Arquivo |
|---|---|
| Puxar ou liberar uma tarefa / conferir concorrência | [.claude/skills/puxar-tarefa/SKILL.md](.claude/skills/puxar-tarefa/SKILL.md) |
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
