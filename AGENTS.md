# AGENTS.md

Regras para qualquer IA (e pessoa) neste repositório. Curto de propósito: cada regra aponta para o detalhe, que só se lê quando a tarefa pede ([orçamento de contexto](docs/agentes.md#orçamento-de-contexto)).

## O projeto

RPG de ação online para PC, grupos pequenos ([visão](docs/visao-geral.md)). Monorepo em C#:

- `client/`: Unity 6000.6.5f1, URP, C# 9. Código em `client/Assets/_Game/{Core,Features/<X>}/`, um `.asmdef` por funcionalidade.
- `server/`: .NET 10, sem engine, **autoritativo**. `server/src/JogoDoGrupo.Server/{Core,Features/<X>}/`.
- `shared/`: .NET Standard 2.1, **C# 9**. `shared/Package/Runtime/{Core,Features/<X>}/`.
- `docs/`: regras por funcionalidade (`docs/features/`) e ADRs (`docs/decisoes/`). IDs no [README](README.md#funcionalidades).

## Comandos

```
dotnet test JogoDoGrupo.slnx          # testes de server e shared (o CI roda)
dotnet csharpier format .             # formata C# antes do push
npm run unity:compilar                # compila o client (editor fechado)
npm run unity:meta                    # .meta de script, pasta, .asmdef, .uss/.tss e texto
npm run tarefa -- listar | revisar    # quadro na ordem de prioridade | fila do /decida
npm run tarefa -- puxar <N> [<N>...]  # trava a tarefa ou o lote e cria a branch
npm run tarefa -- liberar <N>         # devolve a tarefa se pausar
npm run entregar -- --titulo "..." --corpo pr-corpo.md   # commit, push e PR
npm run testar-scripts | contexto     # testes dos scripts | teto das instruções de IA
```

Outros: `npm run tarefa -- ajuda`, [README](README.md#dia-a-dia), [git.md](docs/git.md), [Unity](docs/agentes.md#usar-o-unity). Na nuvem: `npm run nuvem:preparar`.

## Regras

0. **Nunca apague nada do Figma** (arquivo, página, nó, componente, variável, comentário, script). Sujeira ou obsoleto pode sair: confira antes, cite no commit e ajuste links. Push forçado, apagar branch de PR aberto, repositório, release ou projeto: só com quem decide ([ADR 0026](docs/decisoes/0026-ia-pode-apagar-menos-o-figma.md)); exceção: o `reempilhar` na branch do PR ([ADR 0049](docs/decisoes/0049-reempilhar-com-push-forcado.md)).
1. **Fique nas pastas da tarefa** (`Features/<X>/` em client, server e shared, e `docs/features/<x>.md`). Um lote pode tocar várias funcionalidades ([Lote](docs/agentes.md#lote)). `Core/` fora do pedido: pare e avise.
2. **Funcionalidades não se referenciam.** Contratos: `shared/.../Features/<dono>/` (se o client usa) ou `server/.../Contratos/<Dono>/` (só servidor; [ADR 0016](docs/decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)). Sem referência entre `.asmdef` de features.
3. **Servidor autoritativo:** dano, drop, fusão, preço e economia no servidor; o client envia intenção e mostra resultado.
4. **IDs de mensagem** só na faixa `<X>MessageIds.Faixa`.
5. **Shared:** sem `UnityEngine`, até C# 9, `namespace X { }` com chaves.
6. **Server:** namespace de arquivo; módulos `IFeatureModule` autodescobertos, sem registro central.
7. **Nomes de domínio em português sem acento**, termos técnicos em inglês, campos `_camelCase` ([convenções](docs/convencoes.md)).
8. **Formatação é do CSharpier**, rodado antes do push.
9. **Unity:** cena, prefab, `.asset`, `.meta` e `ProjectSettings/` nunca à mão; o Unity cria (script de editor, `unity:executar`). `.meta` de texto/código: `npm run unity:meta`. Assets de terceiros só CC0/CC-BY em `client/Assets/ThirdParty/` ([licencas.md](docs/arte/licencas.md)).
10. **Testes:** contrato e regra que decide resultado; concorrência se item ou moeda puder duplicar. Client sem teste obrigatório ([convenções](docs/convencoes.md#testes), [preparar-pr](.claude/skills/preparar-pr/SKILL.md), [ADR 0034](docs/decisoes/0034-menos-verificacao-repetida.md)).
11. **Mudou regra do jogo:** `docs/features/<x>.md` na mesma mudança. **Decisão técnica:** ADR. [Invariantes de itens](docs/features/itens.md#invariantes) só com o Gabriel.
12. **Git:** nunca commite na `main`; branch `<tipo>/<escopo>/<descricao>`, commit `<tipo>(<escopo>): <descrição minúscula>`, sem `--no-verify`. Terminou: commit, push e PR sem esperar pedido. Merge é humano ([git.md](docs/git.md)).
13. **Economize IA:** leia só a tarefa, verifique por comando e não à mão, não repita o CI, decida o aberto e siga ([Decisões de design](docs/agentes.md#decisões-de-design)), relatório curto. Fechou: registre o pendente e sugira `/clear` ([retomada](docs/agentes.md#trocar-de-assunto-sem-perder-o-fio)).
14. **Informação em fonte única** ([docs/README.md](docs/README.md#onde-cada-coisa-fica-fonte-única)); aponte com link.
15. **Puxe antes de mexer:** `npm run tarefa -- puxar <N>` (com `--worktree <pasta>` se outro lote ativo ou agente na raiz; [Worktree ao puxar](docs/agentes.md#worktree-ao-puxar)). Tarefa de outro: pare. PR leva `Closes #<N>` por issue.
16. **Espera repetida vira corte na hora** (script, hook, skill ou doc), no mesmo PR ou num de processo. Sem corte, não escreva nada. Tropeço: `registrar-licao` ([Custo de IA](docs/agentes.md#custo-de-ia)).
17. **Balanceamento:** constantes nomeadas ou `Config<Assunto>`; sensação no client na `Sintonia` (F10) ([convenções](docs/convencoes.md#valores-de-balanceamento)).
18. **Entrega:** um dono por lote faz contrato, servidor, client e cena num PR e escreve o [plano](docs/agentes.md#plano-da-tarefa) na issue antes de codar. Acima de **3 lotes de jogo**, o `puxar` só avisa ([ADR 0037](docs/decisoes/0037-lotes-de-tarefas.md)).

## Skills

| Tarefa | Skill |
|---|---|
| Implementar em lotes até o PR | [`/trabalhe`](.claude/skills/trabalhe/SKILL.md) |
| Conferir o quadro: fechar, ordenar, fichar, destravar | [`/decida`](.claude/skills/decida/SKILL.md) |
| Quiz ao Gabriel do que falta decidir; doc e tarefas | [`/pergunte`](.claude/skills/pergunte/SKILL.md) |
| Tarefas de melhoria (produto, código, desempenho, IA) | [`/melhore`](.claude/skills/melhore/SKILL.md) |
| Reservar a fila de um escopo (trabalho manual) | [`/segure`](.claude/skills/segure/SKILL.md) |
| Puxar, liberar, conferir relevância | [puxar-tarefa](.claude/skills/puxar-tarefa/SKILL.md) |
| Antes do PR (verificação e descrição) | [preparar-pr](.claude/skills/preparar-pr/SKILL.md) |
| Branch, commit, ADR, lição, funcionalidade nova | [nova-branch](.claude/skills/nova-branch/SKILL.md), [fazer-commit](.claude/skills/fazer-commit/SKILL.md), [novo-adr](.claude/skills/novo-adr/SKILL.md), [registrar-licao](.claude/skills/registrar-licao/SKILL.md), [nova-funcionalidade](.claude/skills/nova-funcionalidade/SKILL.md) |

Agentes: [.claude/agents/](.claude/agents/), fluxo: [docs/agentes.md](docs/agentes.md). UI: [Lumora](design/interface/GUIA-DE-ESTILO.md), [client/Assets/_Game/AGENTS.md](client/Assets/_Game/AGENTS.md). FigJam só em [docs/brainstorm/figjam.md](docs/brainstorm/figjam.md).
