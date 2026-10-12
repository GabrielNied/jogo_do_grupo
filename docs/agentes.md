# Desenvolvimento com vários agentes

O projeto é feito para vários agentes de IA implementarem partes diferentes **ao mesmo tempo**, cada um com uma área fixa, sem pisar no trabalho do outro. Cada funcionalidade de jogo tem **um dono** que a faz de ponta a ponta ([ADR 0030](decisoes/0030-processo-v2-dono-da-funcionalidade.md)). Pessoas e agentes decidem o design; pessoas olham a cena no jogo e fazem o merge.

A organização por funcionalidade ([arquitetura.md](arquitetura.md)) é o que torna isso possível: cada agente mexe em pastas diferentes, então o trabalho em paralelo não gera conflito.

## Papéis

| Agente | Área (só edita aqui) | Quando usar | Branch | Modelo |
|---|---|---|---|---|
| `funcionalidade` | `Features/<X>/` no shared, no servidor (mais `Contratos/<X>/`) e no client, `tests/`, `docs/features/` | **Padrão para issue de jogo:** dono de uma funcionalidade de ponta a ponta (plano, decisões, contrato, servidor, client e cena, testes, doc) | `feature/<x>/<descricao>` | Sonnet (Opus em regra de dano, preço ou fusão) |
| `designer-docs` | `docs/features/`, validação, referências, visão geral | Uma decisão de design precisa virar regra escrita, ou uma funcionalidade precisa ser detalhada antes de implementar | `docs/<x>/<descricao>` | Sonnet, effort baixo |
| `shared-contratos` | `shared/Package/Runtime/`, `docs/features/` | **Antes** de server e client: mensagens, IDs, eventos e tipos que os dois lados usam | `feature/<x>/<descricao>-contrato` | Sonnet |
| `regras-do-jogo` | `shared/Package/Runtime/Features/`, `tests/`, `docs/features/` | Fórmulas puras (dano, fusão, sorteio de skills, experiência), com testes | `feature/<x>/<descricao>-regras` | Sonnet |
| `server-core` | `server/.../Core/`, `Program.cs`, `.csproj` e configuração (`appsettings`, `Properties/`) do servidor, ADRs, arquitetura | Infra do servidor: rede, sessões de grupo, despacho de mensagens, eventos, persistência | `feature/core/<descricao>-server` | Opus |
| `server-funcionalidade` | `server/.../Features/`, migrações em `server/.../Core/Persistencia/Migrations/`, `docs/features/` | Backend de **uma** funcionalidade | `feature/<x>/<descricao>-server` | Sonnet (Opus em tarefas de risco) |
| `client-core` | `client/Assets/_Game/Core/` (só scripts) | Infra do client: conexão, cenas, input, câmera e controle em 3ª pessoa, UI base | `feature/core/<descricao>-client` | Opus |
| `client-funcionalidade` | `client/Assets/_Game/Features/` (scripts; cena, prefab e asset pelo Unity) | Client de **uma** funcionalidade | `feature/<x>/<descricao>-client` | Sonnet (Opus em tarefas de risco) |
| `artista-mundo` | `Features/Mundo/`, `ThirdParty/`, `docs/arte/` | Hub, peças modulares, gerador de dungeon, ambientação | `feature/mundo/<descricao>-arte` | Sonnet |
| `artista-personagens` | `Features/Personagem/`, `ThirdParty/`, `docs/arte/` | Modelos, animações, Animator, prefabs por raça, encaixes | `feature/personagem/<descricao>-arte` | Sonnet |
| `artista-itens` | `Features/Itens/`, `ThirdParty/`, `docs/arte/` | Modelos de armas e equipamentos, ícones, encaixe | `feature/itens/<descricao>-arte` | Sonnet |
| `itens` | `Features/Itens/` no shared, no servidor e no client, mais `docs/features/itens.md` | Inventário, baú, equipamentos e loot. A arte 3D continua com o `artista-itens` | `feature/itens/<descricao>` | Sonnet (Opus se a regra decidir drop, preço ou fusão de verdade) |
| `economia` | `Features/Economia/` no shared, no servidor e no client, mais `docs/features/economia.md` | Coroas, loja de NPC, mercado, baú de grupo, taxas e saídas de moeda; mantém o fluxo de moeda coerente. Balanceamento de combate não é dele | `feature/economia/<descricao>` | Sonnet (Opus se a regra decidir preço, taxa ou troca de verdade) |
| `revisor` | Nenhuma (somente leitura) | Só PRs de risco: Core, segurança, concorrência, contrato ou regra que decide resultado (dano, drop, fusão, preço). O resto fica com o CI e a verificação do autor | — | Sonnet (Opus só nesses PRs de risco) |

As definições completas (regras de cada papel, passo a passo e formato do relatório) estão em [.claude/agents/](../.claude/agents/). São arquivos markdown que **qualquer IA consegue ler**.

O que fica com as **pessoas**:

- olhar a cena no jogo, aprovar e fazer o merge dos PRs;
- rever, quando quiserem, uma decisão de design que um agente tomou (com um PR).

Design se decide por quem implementa, pessoa ou agente, sem esperar ninguém ([Decisões de design](#decisões-de-design)). O `/decida` organiza o quadro e registra o que está pendente, sem implementar. O `/melhore` abre tarefas de melhoria do que já está no jogo, sem implementar. O `/trabalhe` implementa.

Os agentes por camada (`shared-contratos`, `regras-do-jogo`, `server-funcionalidade`, `client-funcionalidade`) ficam para Core e para a fatia grande demais para um PR.

Cenas, prefabs e assets saem dos próprios agentes, pelo Unity (script de editor ou Unity CLI), nunca com YAML escrito à mão (regra 9).

## Prioridade das tarefas

Decidida pelo grupo em 2026-10-11. Cada issue aberta tem um rótulo `prio:*`; ao escolher, vá da menor para a maior, respeitando o [foco do marco](features/fatia-jogavel.md#foco-atual) e o limite de 3 funcionalidades:

1. `prio:1-erros`: tratamento de erros e bugs (o que quebra, perde dado ou trava o jogador).
2. `prio:2-core-loop`: o ciclo do jogo (mobs, combate, XP, loot, nível).
3. `prio:3-experiencia`: experiência do usuário (UI, feedback, fluidez, desempenho percebido).
4. `prio:4-docs-definicoes`: documentação e definições de design (issues `decisao`).
5. `prio:5-features`: funcionalidades novas.
6. `prio:6-seguranca`: segurança e afins (limites, anti-abuso, ferramentas de GM).

Issue nova ganha o rótulo na criação. Quebrado na `main` (CI) continua acima de tudo.

## Fluxo de uma tarefa

```
issue de jogo ─► funcionalidade (worktree): plano ─► contrato ─► servidor ─► client e cena ─► PR ─► CI ─► pessoa: cena e merge
                                                                                     (revisor só em PR de risco)
```

0. **Escolher pelo foco.** A tarefa sai do [foco atual do marco](features/fatia-jogavel.md#foco-atual). No máximo **3 funcionalidades de jogo em andamento** ao mesmo tempo; a quarta espera uma fechar. O `puxar` recusa essa quarta. Trava sem branch no `origin` e sem PR aberto sai sozinha ([lições](licoes.md)).
1. **Puxar para em andamento (trava de concorrência).** `npm run tarefa -- puxar <N> [branch] [--agente <papel>] --worktree <caminho>` marca a issue com `em-andamento`, move o Project para `In Progress` e cria a branch a partir da `origin/main`. Sessão que não é dona do checkout principal sempre usa `--worktree`.
2. **Plano na issue.** O dono escreve o [plano](#plano-da-tarefa), com as decisões de design que tomou, e segue sem esperar.
3. **Implementar a fatia.** No mesmo worktree e PR: contrato no shared, servidor com testes, client com a cena e o ponto de entrada. Funcionalidades diferentes andam em paralelo, cada uma com o seu dono.
4. **Revisão.** O CI confere formatação, padrão de commit, testes e o Unity. O `revisor` só entra em PR de risco (veja a tabela de papéis).
5. **Pessoas.** Olham a cena no jogo e fazem o merge.

Fatia grande demais para um PR (mais de ~1500 linhas) se divide por **caso de uso**, cada PR jogável, nunca por camada.

## Entrega da funcionalidade

A tarefa de jogo termina na cena que o jogador usa. Contrato, servidor e teste são o caminho até ela.

A cena é o que a pessoa faz e o que o jogo mostra, a partir do jogo aberto: não basta a tela existir, ela precisa de **ponto de entrada** (tecla, botão do HUD, NPC, aproximar). O dono entrega a cena inteira, inclusive cena, prefab e asset gerados pelo Unity. Uma tela que só escreve o nome da mensagem não é a cena.

`Closes` vai no PR em que a cena está; PR só de contrato ou de servidor não fecha a issue. Em "Como testar", a pessoa lê o que fazer e o que aparece. No merge, olhe a cena: aba faltando, texto provisório ou tela fora do visual do jogo voltam para a branch.

A [regra 16](../AGENTS.md) continua: polimento fora desta tarefa não ganha uma rodada extra. Acabar a cena desta tarefa é a entrega, não um extra.

### Plano da tarefa

Quem puxa uma tarefa de jogo escreve o plano na issue **antes de codar**. Curto, em frases que uma pessoa reconhece no jogo:

- **Casos de uso:** o que o jogador quer fazer, do ponto de entrada até o resultado.
- **Estados:** os itens da checklist abaixo que se aplicam, e o que aparece em cada um. Os que não se aplicam ficam de fora, sem justificar.
- **Decisões de design:** o que a doc não fechava e você decidiu ([Decisões de design](#decisões-de-design)).
- **Testes:** os casos que mudam o contrato ou o resultado viram teste xUnit (regra 10). Os estados da tela são conferidos na cena pelo "Como testar" do PR, sem teste.

Siga em frente depois de escrever, sem esperar resposta.

**Checklist de experiência (MMORPG)**

- **Vazio, erro e limite** da própria regra, com texto em português que diz o que fazer.
- **Espera do servidor:** o que aparece enquanto a resposta não vem; o botão não envia duas vezes; recusa do servidor mostra o motivo.
- **Outros jogadores:** membro do grupo entra ou sai, outro jogador mexe na mesma coisa, alguém desconecta.
- **Conexão e sessão:** reconectar e relogar mostram o estado certo; o que persiste.
- **Entrada:** tecla (rebind), ESC fecha, a tela não rouba o movimento nem o combate.
- **Retorno:** aviso, som ou efeito quando algo acontece, inclusive com a tela fechada.
- **Descoberta:** como o jogador encontra a funcionalidade da primeira vez.
- **Visual:** [Lumora UI v1](../design/interface/GUIA-DE-ESTILO.md), sem texto provisório, sem corte em leitura aumentada.

### Decisões de design

Todo mundo decide o design, inclusive o agente, sem esperar ninguém ([ADR 0030](decisoes/0030-processo-v2-dono-da-funcionalidade.md)). Quem implementa:

- escolhe o que serve à [visão](visao-geral.md) e ao foco do marco, preferindo o que é fácil de mudar (valores em constante nomeada, regra 17);
- registra a decisão em `docs/features/<x>.md` (fonte única) e lista no PR, em "Decisões de design";
- não reabre o que o grupo marcou como **Definido**; para mudar isso, escreve a proposta na doc e segue com o definido.

Quem discordar abre um PR mudando a doc e o código. Issue de `decisao` parada pode ser resolvida assim por quem pegar a funcionalidade.

### Usar o Unity

Vale para o dono da funcionalidade e para os agentes de client, no worktree deles.

- **Tela e HUD:** prefira UI Toolkit montada por código e registrada sozinha, como `JanelaMissoes` (`[RuntimeInitializeOnLoadMethod]` + `JanelasDoJogo.Registrar`). Não precisa de cena nem prefab.
- **Cena, prefab, material ou asset:** script de editor em `Features/<X>/Editor/`, rodado com `npm run unity:executar -- <Classe.Metodo>` no próprio worktree (a primeira importação demora). Script reproduzível, não clique único. Com o editor aberto nesse worktree, o Unity CLI (`unity command`) também serve. YAML do Unity e `.meta` escritos à mão o hook barra (regra 9).
- **Um Unity por vez na máquina, sozinho:** `unity:executar`, `unity:importar` e `unity:testes` pegam uma trava da máquina ([scripts/unity/trava.mjs](../scripts/unity/trava.mjs)) e esperam a vez enquanto outro comando ou o editor estiver aberto, até 30 min (`UNITY_ESPERA_MIN`). Trava de processo morto é retomada sozinha.
- Commite o que o Unity gerou na sua pasta no mesmo PR; `npm run conferir-unity` precisa passar. `ProjectSettings/` e cenas de `Core` ficam de fora: se o ponto de entrada exigir mexer nelas, pare e avise.

### Agentes artistas e o Unity

Os artistas ([ADR 0014](decisoes/0014-conteudo-3d-e-agentes-artistas.md)) buscam assets CC0 conforme a necessidade e montam o conteúdo **pelo Unity** (editor aberto via Unity CLI, ou scripts de editor em modo batch). Cada artista trabalha no seu worktree, como os outros agentes, e a trava do Unity ([Usar o Unity](#usar-o-unity)) põe os passos com Unity em fila. O checkout principal fica com a pessoa. Antes de abrir o PR, `npm run conferir-unity` confirma que `.meta` e materiais alterados pelo Unity entraram no commit. O CI repete a parte dos `.meta` que faltam.

Lições de processo (Blender, commits em etapas, Unity aberto) estão em [licoes.md](licoes.md).

### Fechamento: o coordenador integra e limpa o que criou

Vale para quem coordena agentes (IA ou pessoa). Aprendido na orquestração da [#319](https://github.com/GabrielNied/jogo_do_grupo/issues/319), onde quatro agentes pararam sem editar nada e o trabalho foi refeito.

- **A base do worktree do agente pode estar atrás.** Um agente com `isolation: worktree` parte do commit do checkout principal quando é lançado. Edição não commitada do coordenador (ex.: um arquivo recém-gerado) **nunca chega** ao agente. Commite antes numa branch e ponha no prompt o passo 0: `git merge <branch>` (local, sem push) e conferir o arquivo que ele precisa. Sem isso o agente para sem editar nada e a rodada é paga duas vezes.
- **Trabalhe num worktree seu.** O checkout principal é compartilhado e outra sessão pode trocar de branch nele, levando junto o que não foi commitado. O coordenador faz o trabalho do PR num worktree próprio (`npm run tarefa -- puxar <N> --worktree <pasta>`) e commita cedo.
- **O agente commita local.** Na própria branch `worktree-agent-*`, sem push, e informa a branch e o hash no relatório. Sem commit, a mudança fica só no worktree dele, e um worktree sem mudança some quando o agente termina.
- **Integrar com `git cherry-pick <hash>`, não com `git merge` da branch do agente.** A branch herda commits da base dele; o merge arrasta trabalho alheio para o PR. Antes de aceitar, `git show --stat <hash>` confere que só há arquivos da área do agente. Se o agente deixou edição sem commit, extraia com `git diff` e aplique com `git apply --3way`.
- **A limpeza é automática ([ADR 0031](decisoes/0031-limpeza-automatica.md)):** depois do merge, worktrees e branches saem sozinhos na próxima sessão, `git pull` ou `puxar`, inclusive os integrados por cherry-pick (o script compara o conteúdo com a `main`) e o worktree que ficou fora de `.claude/worktrees/` ([lição](licoes.md#2026-10-12--worktree-fora-da-pasta-de-agentes-sobrevive-ao-merge)). Não é preciso remover nada à mão. Para ver o que ficou e por quê: `.git/limpar-agentes.log`, ou `npm run limpar-agentes` para simular agora.
- **Permissão para remover.** O modo automático do Claude Code pode negar `git worktree remove` e `git branch -D`, e a IA não edita a própria permissão. A pessoa libera uma vez em `.claude/settings.local.json` (já está no `.gitignore`):

  ```json
  { "permissions": { "allow": [
    "Bash(git worktree unlock:*)",
    "Bash(git worktree remove:*)",
    "Bash(git branch -D worktree-agent-*)"
  ] } }
  ```

  A regra cobre só worktrees e branches `worktree-agent-*`. Ou a pessoa roda os comandos pelo prompt com `!`.

### Por que não conflita

- **Lock de tarefa (em andamento):** ninguém trabalha em tarefa sem puxá-la (`npm run tarefa -- puxar <N>`). Se já estiver marcada com `em-andamento` ou `In Progress`, outros agentes e desenvolvedores são impedidos de iniciar a mesma tarefa. Em paralelo, cada dono opera na sua funcionalidade.
- **Pastas separadas:** cada agente só edita a sua área. No Claude Code, um hook bloqueia a edição fora dela.
- **Worktrees:** cada agente trabalha numa cópia própria do repo, então um não vê os arquivos pela metade do outro.
- **Um dono por funcionalidade:** contrato, servidor e client mudam juntos no mesmo PR, então não há contrato desencontrado entre camadas.
- **Uma funcionalidade por tarefa:** os agentes de funcionalidade nunca mexem em duas.

## Regras para todos os agentes

- **Consumo:** tarefa pequena (até ~50 linhas) o coordenador faz direto, sem agente; tarefas pequenas da mesma área vão num agente só; sessão do coordenador curta, com `/clear` por rodada. Relatórios curtos (até 15 linhas; revisor até 20) e verificação proporcional à alteração (matriz da skill `preparar-pr`). Não repita o que o CI já confere. O `revisor` é uma passada só, e só em PR de risco: Core, segurança, concorrência, contrato ou regra que decide resultado. Arte, tela, conteúdo e ligar um botão não levam revisor. Sonnet é o padrão; Opus só nessas tarefas de risco.

- **Entrega:** a tarefa de jogo termina na cena que o jogador usa ([Entrega da funcionalidade](#entrega-da-funcionalidade)).
- **Lock de tarefa:** confira que a tarefa está livre e puxe-a para em andamento antes de alterar código (`npm run tarefa -- puxar <N>`). Se a tarefa já estiver em andamento, pare e avise. Se precisar interromper sem concluir, libere com `npm run tarefa -- liberar <N>`.
- **Regra 0:** nunca apagar nada do Figma; o resto a IA pode apagar ([ADR 0026](decisoes/0026-ia-pode-apagar-menos-o-figma.md)). No Claude Code, um hook do projeto ([scripts/agentes/bloquear-apagar.mjs](../scripts/agentes/bloquear-apagar.mjs)) barra a remoção no Figma, o push forçado e apagar repositório, release ou projeto. Worktrees e branches temporários dos agentes saem sozinhos depois do merge ([ADR 0031](decisoes/0031-limpeza-automatica.md)).
- **Autorização:** quando uma pessoa pede uma orquestração, ela autoriza os agentes a commitar, enviar as próprias branches e abrir PRs. **Merge é sempre humano.**
- **Fora da área:** se a tarefa exige mudar algo fora da área do agente (um contrato, o Core, outra funcionalidade), o agente para e descreve a mudança no relatório, para o agente certo fazer.
- **Relatório:** todo agente termina com o que fez, a branch e o PR, as verificações, o que outros agentes ou pessoas precisam fazer e as dúvidas.

## Como rodar

### Claude Code

Os agentes estão em [.claude/agents/](../.claude/agents/). Cada um roda num git worktree isolado (`isolation: worktree`) e tem um hook que limita as pastas.

- **Orquestrado (recomendado):** na sessão principal, peça a tarefa e a IA segue a skill [orquestrar-tarefa](../.claude/skills/orquestrar-tarefa/SKILL.md). Ela monta o plano, registra as decisões e lança os agentes em paralelo, em segundo plano, sem esperar aprovação. Merge continua humano.
- **Manual:** peça diretamente "use o agente server-funcionalidade para ..." ou abra um terminal por agente com `claude --agent <nome>`.
- `/agents` lista os agentes disponíveis.

### Codex

O Codex lê automaticamente o `AGENTS.md` da raiz. Os perfis equivalentes aos
agentes do Claude ficam em [`.codex/agents/`](../.codex/agents/): peça pelo
nome do papel (por exemplo, `server-funcionalidade`) ao delegar uma subtarefa.
Cada perfil manda ler a definição canônica em [`.claude/agents/`](../.claude/agents/),
que continua sendo a fonte única das responsabilidades, escopos e relatórios.
Os procedimentos também são descobertos pelo Codex em [`.agents/skills/`](../.agents/skills/);
cada entrada encaminha para a skill canônica em [`.claude/skills/`](../.claude/skills/).

Em um projeto confiável, o Codex também carrega [`.codex/hooks.json`](../.codex/hooks.json),
que aplica a regra 0 (push forçado e apagar repositório, release ou projeto). Revise e aprove o hook uma vez com `/hooks` ao iniciar o
Codex localmente.

Agentes que escrevem nunca trabalham na mesma cópia do repositório em paralelo.
Crie os worktrees antes de delegar e passe o caminho de cada um no pedido; o
agente revisor é somente leitura e pode rodar em paralelo. O Codex limita a
sessão a três subagentes além do coordenador.

### Antigravity

O Antigravity reconhece o `AGENTS.md` da raiz e as skills em
[`.agents/skills/`](../.agents/skills/), que apontam para os procedimentos
canônicos em [`.claude/skills/`](../.claude/skills/). Os perfis equivalentes
aos agentes do Claude ficam em [`.agents/agents/`](../.agents/agents/): cada
um manda ler a definição canônica correspondente antes de atuar, sem duplicar
as regras do papel.

Em uma sessão confiável, habilite o hook
[`.agents/hooks.json`](../.agents/hooks.json) em **Customizations > Hooks**
(ou use `/hooks` no CLI). Ele chama o mesmo bloqueio de comandos destrutivos
usado pelas outras ferramentas. Os perfis são para Antigravity 2.0 e CLI; no
IDE que ainda não listar agentes personalizados, as regras, skills e hook
continuam funcionando e o papel pode ser pedido explicitamente no prompt.

Ao delegar escrita em paralelo, use worktrees isolados. Escolha o papel em
`/agents` ou peça ao agente principal para delegar o nome apropriado; mantenha
o `revisor` somente leitura.

### Outras IAs (Cursor, Copilot, Gemini...)

Abra **uma sessão por agente**, cada uma no seu worktree:

```
git fetch origin
git worktree add ../jdg-itens-server -b feature/itens/fusao-server origin/main
cd ../jdg-itens-server
npm install
```

E comece a conversa com:

> Atue como o agente definido em `.claude/agents/server-funcionalidade.md`, para a funcionalidade Itens. Tarefa: ...

Nessas ferramentas os hooks não rodam: o limite de pastas e a regra 0 dependem das instruções, do `revisor` e do review humano.

~~Worktrees que sobrarem depois do merge são removidos por uma **pessoa** (`git worktree remove <pasta>`); a IA não apaga.~~ Substituído em 2026-10-11 pelo [ADR 0018](decisoes/0018-limpeza-do-que-os-agentes-criam.md): o coordenador (ou a pessoa) roda `npm run limpar-agentes -- --aplicar` depois dos merges. ~~O script remove só worktrees e branches cujo conteúdo já está na `main`~~ Substituído em 2026-10-11 pelo [ADR 0031](decisoes/0031-limpeza-automatica.md): a limpeza roda sozinha e remove o que não se perde.

## Limitações atuais

- **Unity:** um por vez na máquina, em fila pela trava ([Usar o Unity](#usar-o-unity)). Editor da pessoa aberto por mais de 30 min faz o comando do agente desistir com aviso de quem ocupa. A primeira importação de um worktree novo é lenta: não abra outro só para repetir.
- **Sem Unity (nuvem):** desde o [ADR 0019](decisoes/0019-unity-no-ci.md), os comandos `unity:*` pulam com aviso, `npm run unity:meta` gera os `.meta` de script, pasta, `.asmdef` e estilo, e o CI "Client (Unity)" compila e testa o client depois do merge. O agente abre o PR normal e registra o que só o Unity faz (menos compilar e rodar testes EditMode, que o CI faz) com `npm run tarefa -- pendencia-unity <PR> --falta "..."`. Numa máquina com Unity, o início da sessão avisa das pendências abertas, e quem puxa resolve na branch do PR (se ainda estiver aberto) ou numa branch nova.
- ~~**Testes:** o framework de testes ainda não foi decidido (ADR em aberto). Até lá, `regras-do-jogo` descreve os casos de teste no relatório.~~ Substituído em 2026-10-11 pelo [ADR 0013](decisoes/0013-testes-automatizados.md): todo PR de código traz testes, e o CI roda `dotnet test`.
