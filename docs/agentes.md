# Desenvolvimento com vários agentes

Vários agentes de IA implementam ao mesmo tempo, cada um num worktree e num **lote** de tarefas, sem pisar no outro. Pessoas e agentes decidem o design; pessoas olham a cena e fazem o merge. Configuração por ferramenta: [ferramentas-ia.md](ferramentas-ia.md).

## Papéis

| Agente | Área (o hook barra o resto) | Quando |
|---|---|---|
| `funcionalidade` | `Features/` no shared, servidor (e `Contratos/`) e client, `tests/`, `docs/features/` | **Padrão** para lote de jogo, de uma ou várias funcionalidades |
| `itens`, `economia` | `Features/Itens/` ou `Features/Economia/` nas três partes, a doc deles, `tests/` | Lote só de Itens ou só de Economia |
| `server-core`, `client-core` | `Core/` do servidor (com `Program.cs`, configuração e ADRs) ou do client (scripts) | Infra: rede, sessões, despacho, persistência; conexão, cenas, input, câmera, UI base |
| `artista-mundo`, `-personagens`, `-itens` | A pasta da funcionalidade, `ThirdParty/`, `docs/arte/` | Conteúdo 3D pelo Unity ([ADR 0014](decisoes/0014-conteudo-3d-e-agentes-artistas.md)) |
| `designer-docs` | `docs/features/`, visão, referências | Decisão de design que precisa virar regra escrita |
| `revisor` | Só leitura | PR de risco: Core, segurança, regra de dano, preço ou fusão que a doc não fechou |
| `conferente` | Só leitura | No `/decida`: confere issues contra doc e código e propõe o veredito |

Sonnet é o padrão; Opus em Core e PRs de risco; Haiku no `conferente`, onde o trabalho é ler e citar. Comandos: `/trabalhe` implementa; `/decida`, `/melhore` e `/segure` não implementam (conferem, sugerem ou reservam).

## Doc e regras atuais

No `/trabalhe`, `/decida` e `/melhore`: **`git fetch origin main`** antes de ler doc, escrever ficha ou decidir. `AGENTS.md`, `docs/features/` e ADRs valem **`origin/main`**, não a branch local (`git show origin/main:<arquivo>`). O `puxar` refaz fetch ao criar a branch, mas não substitui o fetch inicial.

## Worktree ao puxar

**Uma pasta Git por lote.** A trava `em-andamento` é por issue, não por diretório — duas IAs no mesmo checkout se atropelam.

`npm run tarefa -- puxar … --worktree <pasta>` cria a branch no worktree (`.claude/worktrees/…`, [ferramentas-ia.md](ferramentas-ia.md)). Agente já isolado: `--worktree .`. Checkout principal com outro lote ou `--agente`: pasta fora da raiz é obrigatória. Coordenador que codifica: worktree próprio ([Fechamento](#fechamento-o-coordenador-integra-e-limpa-o-que-criou)).

## Prioridade das tarefas

Cada issue tem um rótulo `prio:*` (decisão do grupo de 2026-10-11). Quebrado na `main` (CI) vem antes de tudo.

1. `prio:1-erros`: o que quebra, perde dado ou trava o jogador.
2. `prio:2-core-loop`: mobs, combate, XP, loot, nível.
3. `prio:3-experiencia`: UI, feedback, fluidez, desempenho percebido.
4. `prio:4-docs-definicoes`: documentação e definições (issues `decisao`).
5. `prio:5-features`: funcionalidades novas.
6. `prio:6-seguranca`: limites, anti-abuso, ferramentas de GM.

Dentro da faixa: a issue mais antiga primeiro. Não há foco de marco (Gabriel, 2026-10-19): toda tarefa livre pode ser puxada, na ordem de `prio:*`. O `listar` já sai nessa ordem. O /trabalhe não pega: `em-andamento`, `bloqueada`, `decisao`, `pessoa`, `pendente-unity` (só quem tem Unity) e quem `Depende de` issue aberta. `bloqueada` vale só para o que espera uma pessoa ou uma decisão; a ordem entre issues é o `Depende de:`. Quem ajusta a ordem é o /decida, pelo rótulo `prio:*`.

## Ficha da tarefa

Corpo da issue que o agente lê (/decida, /melhore ou coordenador). Template completo: skill [decida](../../.claude/skills/decida/SKILL.md). Texto original antigo fica em `<details><summary>Texto original</summary>`.

## Lote

Tarefas livres que mexem no **mesmo código**: mesmos arquivos, fluxo, dependência ou "Lote sugerido". Pode cruzar features e não tem limite de tarefas: juntar economiza tokens e rodadas de CI ([ADR 0037](decisoes/0037-lotes-de-tarefas.md)).

- **Dono:** `itens` ou `economia` se exclusivo deles; `funcionalidade` se cruza; `server-core` ou `client-core` para Core (Core não entra em lote de feature).
- **Entrega:** `npm run tarefa -- puxar <N> ... <branch>`, uma branch, um commit por issue, PR com `Closes #` por issue. Acima de ~1500 linhas: PRs empilhados por caso de uso ([Leva empilhada](git.md#leva-empilhada)).
- **Paralelo:** lotes sem arquivos comuns. **Recomendado** até 3 lotes de jogo em andamento ([ADR 0037](decisoes/0037-lotes-de-tarefas.md)); o `puxar` avisa se passar, sem bloquear. Core e desenvolvimento não contam. Trava sem branch no `origin` e sem PR expira em **24 h** (`soltar-travas`).
- **Pequeno:** lote de docs, config ou até ~50 linhas o coordenador faz direto, sem agente.

## Segure (reserva manual)

Reserva de tarefas livres de um escopo (sem concorrência de agente): `npm run tarefa -- segure <escopo>` (`rotular <N>`, `liberar <escopo>`).

- Escopo = prefixo do título (`[Mundo]` → `mundo`, alinhado ao commitlint).
- Só rotula livres; `em-andamento` segue até liberar, merge ou 24 h.
- Dono = login do `gh` (só ele puxa). Múltiplos segures permitidos; `/decida` não remove `segure:*`. Detalhes na skill [segure](../../.claude/skills/segure/SKILL.md).

## Entrega da funcionalidade

Termina na **cena** que o jogador usa, a partir do jogo aberto, com **ponto de entrada** (tecla, HUD, NPC, aproximar). Contrato, servidor e testes são o caminho. `Closes` vai no PR da cena; "Como testar" detalha o que o jogador faz e vê.

### Como o dono trabalha

1. Lê as fichas do lote, doc da feature e código a mudar.
2. Puxa o lote (`puxar <N> ... <branch> --agente <papel> --worktree .`) e registra o plano na issue antes de codar.
3. Por issue, um commit: contrato no shared, servidor com testes, client com cena e ponto de entrada. Migração EF por comando, nunca à mão.
4. Confere com `preparar-pr`, abre o PR e relata.

### Plano da tarefa

Quem puxa escreve na issue, antes de codar, e segue sem esperar:

- **Casos de uso:** do ponto de entrada até o resultado.
- **Estados:** itens da checklist que se aplicam e visual em cada um.
- **Decisões de design** tomadas.
- **Testes:** casos que mudam contrato ou resultado (xUnit). Tela conferida pelo "Como testar".

Checklist de experiência (vazio/erro/limite, espera, rede, rebind/ESC, [Lumora](../design/interface/GUIA-DE-ESTILO.md)): skill [preparar-pr](../../.claude/skills/preparar-pr/SKILL.md).

### Decisões de design

Quem implementa decide o que a doc não fecha, sem esperar ([ADR 0030](decisoes/0030-processo-v2-dono-da-funcionalidade.md)): escolhe o que serve à [visão](visao-geral.md), prefere o fácil de mudar (regra 17), registra em `docs/features/<x>.md` e no PR. **Definido** pelo grupo não se reabre: proponha na doc e siga com o definido. Quem discordar abre PR mudando doc e código.

## Usar o Unity

- **HUD:** UI Toolkit por código (`JanelasDoJogo.Registrar`); sem prefab/cena de tela.
- **Cena/prefab/asset:** editor em `Features/<X>/Editor/` + `unity:executar`; YAML/`.meta` à mão o hook barra. Comandos `unity:importar`, `unity:recompilar`, `unity:editor`.
- **Um Unity por máquina:** trava em [trava.mjs](../scripts/unity/trava.mjs) (`UNITY_ESPERA_MIN`).
- **Commit:** assets gerados no PR; `conferir-unity`. `ProjectSettings/` e cenas Core: pare e avise.
- **Sem editor:** `unity:*` sem janela. Ao vivo: a pessoa ([F9](features/desenvolvimento.md#report-beta-e-ajuste-ao-vivo)).
- **Nuvem:** `unity:*` pulam; falta vira `pendencia-unity` ([ADR 0019](decisoes/0019-unity-no-ci.md)).

### Prints da tela

Mudou tela ou HUD: `CapturaDaTelaComando.Capturar` via `unity:executar` (1280/1920/2560, pasta `prints/`). Variáveis e opções em [client/Assets/_Game/AGENTS.md](../client/Assets/_Game/AGENTS.md).

## Fechamento: o coordenador integra e limpa o que criou

- Worktree do agente parte do commit do checkout principal: commite antes de lançar.
- Coordenador que codifica usa worktree próprio ([Worktree ao puxar](#worktree-ao-puxar)).
- Agente sem PR commita local e informa branch e hash: integre com `git cherry-pick <hash>` e confira `git show --stat`.
- Worktrees e branches saem sozinhos após merge ([ADR 0031](decisoes/0031-limpeza-automatica.md)); log em `.git/limpar-agentes.log`.

## Regras para todos os agentes

- **Relatório** de até 15 linhas: o que fez (visão do jogador), PR/branch, decisões de design, verificações, pendências e corte de custo.
- **Fora da área:** pare e descreva no relatório para o agente certo.
- **Autorização:** pedir a implementação autoriza commit, push e PR. Merge é humano.
- **Shell no worktree:** um comando por chamada, sem heredoc; escrita por ferramenta de arquivo. `dotnet test` com 10 min de limite e sem concorrência no mesmo worktree. Bloqueio de hook: use `git -C <caminho>` pelo coordenador.
- **Scripts:** Node (`.mjs`); sem Python. Simulações em C# com shared ([ADR 0043](decisoes/0043-simulacoes-em-csharp-e-scripts-em-node.md)).

### Custo de IA

Regra 16: pergunte se a próxima vez desta tarefa sai mais barata. Espera repetida vira corte no PR atual ou de processo. Tropeço vira regra, script ou hook; [licoes.md](licoes.md) guarda só o que ainda não virou.

### Orçamento de contexto

AGENTS.md, este arquivo, agente e skill são reenviados a cada rodada. `npm run contexto` mede e falha acima do teto (7 / 11 / 3 / 6 mil caracteres). Para crescer, enxugue ou mova detalhe para doc linkada.

### Trocar de assunto sem perder o fio

Ao fechar um assunto, registre o pendente (issue ou `node scripts/agentes/retomar.mjs gravar "<texto>"`) e dê `/clear`. O hook de início mostra o bilhete e o apaga.
