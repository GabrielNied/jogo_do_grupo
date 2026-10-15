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

Sonnet é o padrão; Opus em Core e nos PRs de risco. Comandos: `/trabalhe` implementa, `/decida` confere e organiza o quadro, `/melhore` abre melhorias. Nenhum dos dois últimos implementa.

## Doc e regras atuais

Primeiro passo do `/trabalhe`, `/decida` e `/melhore`: **`git fetch origin main`**. Regras (`AGENTS.md`), `docs/features/` e ADRs valem o que está na **`origin/main`**, não o working tree da branch atual se ela estiver velha.

- **Ler doc:** `git show origin/main:docs/features/<x>.md` (ou outro caminho em `docs/`, e `git show origin/main:AGENTS.md` na raiz).
- **Buscar no código** (ex.: lente código ou desempenho do `/melhore`): `git grep … origin/main -- <pastas>`, depois do fetch.
- **`npm run tarefa -- puxar`** faz fetch de novo e cria a branch a partir de `origin/main`; isso **não substitui** o fetch no início da leva ou da varredura, quando você ainda lê doc, escreve ficha ou decide veredito.

Checkout principal limpo: `git switch main && git pull --rebase origin main` também serve. Em worktree ou com mudança local, prefira `fetch` + `git show` / `git grep`.

## Prioridade das tarefas

Cada issue tem um rótulo `prio:*` (decisão do grupo de 2026-10-11). Quebrado na `main` (CI) vem antes de tudo.

1. `prio:1-erros`: o que quebra, perde dado ou trava o jogador.
2. `prio:2-core-loop`: mobs, combate, XP, loot, nível.
3. `prio:3-experiencia`: UI, feedback, fluidez, desempenho percebido.
4. `prio:4-docs-definicoes`: documentação e definições (issues `decisao`).
5. `prio:5-features`: funcionalidades novas.
6. `prio:6-seguranca`: limites, anti-abuso, ferramentas de GM.

Dentro da faixa: `foco` ([foco do marco](features/fatia-jogavel.md#foco-atual)) primeiro, depois a issue mais antiga. O `listar` já sai nessa ordem. O /trabalhe não pega: `em-andamento`, `bloqueada`, `decisao`, `pessoa`, `pendente-unity` (só quem tem Unity) e quem `Depende de` issue aberta. Quem ajusta a ordem é o /decida, pelos rótulos `prio:*` e `foco`.

## Ficha da tarefa

O corpo da issue que o agente lê. Escrita pelo /decida, pelo /melhore ou pelo coordenador; o texto original fica num `<details><summary>Texto original</summary>` no fim.

```
<!-- ficha -->
**Objetivo:** o que funciona no final, em 2–3 linhas.
**Hoje:** o que já existe na main.
**Cena:** ponto de entrada → o que a pessoa faz → o que aparece.
**Regra:** link para a seção de docs/features/<x>.md (sem copiar).
**Onde mexer:** arquivos e fluxo atual.
**Contrato:** mensagens, DTOs e erros, se já souber.
**Testes esperados:** os casos que mudam contrato ou resultado.
**Depende de:** #N (o listar lê esta linha).
**Lote sugerido:** #N, #M (opcional).
**Fora do escopo:** ...
```

## Lote

Tarefas livres que mexem no **mesmo código**: mesmos arquivos ou fluxo, uma depende da outra, ou estão no "Lote sugerido". Pode cruzar funcionalidades e não tem limite de tarefas: cada agente custa 50–80 mil tokens fixos e cada PR uma rodada de CI, então juntar é mais barato ([ADR 0037](decisoes/0037-lotes-de-tarefas.md)).

- **Dono:** `itens` ou `economia` se o lote é só deles; `funcionalidade` se cruza funcionalidades; `server-core` ou `client-core` se é Core. Core não entra em lote de funcionalidade.
- **Entrega:** `npm run tarefa -- puxar <N> <M> ... <branch>`, uma branch, um commit por issue, um PR com um `Closes #` por issue. Passou de ~1500 linhas: o mesmo dono divide em PRs empilhados por caso de uso ([Leva empilhada](git.md#leva-empilhada)).
- **Paralelo:** lotes que não dividem arquivos. No máximo 3 lotes de jogo em andamento (o `puxar` recusa o quarto); Core e desenvolvimento não ocupam vaga. Trava sem branch no `origin` e sem PR sai sozinha em 2 h.
- **Pequeno:** lote só de docs, config ou mudanças de até ~50 linhas o coordenador faz direto, sem agente.

## Entrega da funcionalidade

A tarefa de jogo termina na **cena** que o jogador usa, a partir do jogo aberto, com **ponto de entrada** (tecla, botão do HUD, NPC, aproximar). Contrato, servidor e teste são o caminho. `Closes` vai no PR da cena; "Como testar" diz o que a pessoa faz e o que aparece. No merge, aba faltando, texto provisório ou tela fora do visual voltam para a branch.

### Como o dono trabalha

1. Lê as fichas do lote, a doc da funcionalidade e o código que vai mudar.
2. Puxa o lote (`puxar <N> <M> ... <branch> --agente <papel> --worktree .`) e escreve o plano em cada issue.
3. Por issue, um commit: contrato no shared, servidor com testes, client com a cena e o ponto de entrada. Migração com `dotnet dotnet-ef migrations add <X><Descricao> --project server/src/JogoDoGrupo.Server`, nunca à mão.
4. Verifica com a skill `preparar-pr`, abre o PR e termina com o relatório (abaixo).

### Plano da tarefa

Quem puxa escreve na issue, antes de codar, e segue sem esperar:

- **Casos de uso:** do ponto de entrada até o resultado.
- **Estados:** os itens da checklist que se aplicam, e o que aparece em cada um.
- **Decisões de design** que tomou.
- **Testes:** os casos que mudam contrato ou resultado (xUnit). A tela é conferida pelo "Como testar".

**Checklist de experiência:** vazio, erro e limite com texto que diz o que fazer · espera do servidor (botão não envia duas vezes, recusa mostra o motivo) · outros jogadores (entra, sai, mexe na mesma coisa, desconecta) · reconectar e relogar · entrada (rebind, ESC fecha, não rouba movimento nem combate) · retorno (aviso, som ou efeito, até com a tela fechada) · descoberta · visual [Lumora UI v1](../design/interface/GUIA-DE-ESTILO.md), sem corte em leitura aumentada.

### Decisões de design

Quem implementa decide o que a doc não fecha, sem esperar ([ADR 0030](decisoes/0030-processo-v2-dono-da-funcionalidade.md)): escolhe o que serve à [visão](visao-geral.md) e ao foco, prefere o fácil de mudar (regra 17), registra em `docs/features/<x>.md` e no PR. **Definido** pelo grupo não se reabre: escreva a proposta na doc e siga com o definido. Quem discordar abre PR mudando doc e código.

## Usar o Unity

- **Tela e HUD:** UI Toolkit montada por código e registrada sozinha (`[RuntimeInitializeOnLoadMethod]` + `JanelasDoJogo.Registrar`, como `JanelaMissoes`). Sem cena nem prefab.
- **Cena, prefab, material, asset:** script de editor em `Features/<X>/Editor/`, rodado com `npm run unity:executar -- <Classe.Metodo>` no seu worktree, ou Unity CLI no editor aberto. YAML e `.meta` à mão o hook barra. Outros: `unity:importar` (gera `.meta` de asset, editor fechado), `unity:recompilar` (editor aberto), `unity:editor` (diz se está aberto).
- **Um Unity por vez na máquina:** `unity:executar`, `unity:importar` e `unity:testes` esperam a vez numa trava ([trava.mjs](../scripts/unity/trava.mjs)), até 30 min (`UNITY_ESPERA_MIN`). A primeira importação de um worktree é lenta: não abra outro só para repetir.
- **Commit:** o que o Unity gerou entra no mesmo PR; `npm run conferir-unity` precisa passar. `ProjectSettings/` e cenas de Core: pare e avise.
- **Sem Unity (nuvem):** `unity:*` pulam; o PR sai normal e só o que precisa do Unity para existir vira `npm run tarefa -- pendencia-unity <PR> --falta "..."` ([ADR 0019](decisoes/0019-unity-no-ci.md)).

### Prints da tela

Mudou tela ou HUD: com o editor fechado, grave 1280×720, 1920×1080 e 2560×1440 e leia os PNG antes do PR. Caem em `prints/` (ignorada pelo git; cada execução apaga a leva anterior). Sem Unity ou com o editor aberto, pule.

```
$env:PRINT_PAINEL="itens.inventario"; npm run unity:executar -- JogoDoGrupo.Client.Features.Desenvolvimento.Editor.CapturaDaTelaComando.Capturar
```

`PRINT_CENA` (caminho da cena), `PRINT_PAINEL` (chave da janela, como `personagem.ficha`, ou da tela, como `core.login`) e `PRINT_SAIDA` (outra pasta, que não é apagada).

## Fechamento: o coordenador integra e limpa o que criou

- O worktree do agente parte do commit do checkout principal: o que o coordenador não commitou não chega. Commite antes e ponha no prompt `git merge <branch>` como passo 0.
- O coordenador trabalha num worktree próprio (`puxar --worktree`), porque o checkout principal é compartilhado.
- Agente que não abre PR commita local e informa branch e hash; integre com `git cherry-pick <hash>` (nunca `git merge` da branch dele) e confira `git show --stat`.
- Worktrees e branches saem sozinhos depois do merge ([ADR 0031](decisoes/0031-limpeza-automatica.md)); o motivo do que ficou está em `.git/limpar-agentes.log`.

## Regras para todos os agentes

- **Relatório** de até 15 linhas: o que fez (do ponto de vista do jogador), PR e branch, decisões de design, verificações ("ok" ou o que falhou), o que outros precisam fazer e o corte de custo, se houver.
- **Fora da área:** pare e descreva no relatório para o agente certo.
- **Autorização:** pedir a implementação autoriza commit, push e PR. Merge é humano.
- **Shell no worktree:** um comando por chamada, sem heredoc; arquivos pela ferramenta de escrita. `dotnet test` com 10 min de limite e sem duas execuções no mesmo worktree. Hook bloqueou o shell: não repita, use `git -C <caminho>` pelo coordenador.

### Custo de IA

Regra 16: antes do PR, pergunte se a próxima vez desta tarefa sai mais barata. Espera que vai se repetir (agente relançado, revisor em cima de teste, comando que falha e abre outra rodada, releitura do que a issue dizia, print pedido à pessoa, script ou hook que faltou) vira corte neste PR ou num pequeno de processo. Não vale para ampliar o jogo nem polir fora da tarefa. Tropeço de processo vira regra, script ou hook; o [licoes.md](licoes.md) guarda só o que ainda não virou.

### Orçamento de contexto

AGENTS.md, este arquivo, cada agente e cada skill são reenviados em todas as rodadas. `npm run contexto` mede e falha acima do teto (7 / 11 / 3 / 6 mil caracteres). Para crescer, enxugue outro trecho ou mova o detalhe para um doc lido só quando preciso.

### Trocar de assunto sem perder o fio

Sessão longa encarece até as mensagens curtas. Ao fechar um assunto, registre o pendente (trabalho que vira PR: numa issue; a próxima pergunta: `node scripts/agentes/retomar.mjs gravar "<texto>"`) e dê `/clear`. O hook de início mostra o bilhete e o apaga.
