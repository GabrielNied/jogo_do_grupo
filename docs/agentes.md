# Desenvolvimento com vários agentes

O projeto é feito para vários agentes de IA implementarem partes diferentes **ao mesmo tempo**, cada um com uma área fixa, sem pisar no trabalho do outro. Pessoas decidem, revisam, montam o que é visual no Unity e fazem o merge.

A organização por funcionalidade ([arquitetura.md](arquitetura.md)) é o que torna isso possível: cada agente mexe em pastas diferentes, então o trabalho em paralelo não gera conflito.

## Papéis

| Agente | Área (só edita aqui) | Quando usar | Branch | Modelo |
|---|---|---|---|---|
| `designer-docs` | `docs/features/`, validação, referências, visão geral | Uma decisão de design precisa virar regra escrita, ou uma funcionalidade precisa ser detalhada antes de implementar | `docs/<x>/<descricao>` | Sonnet, effort baixo |
| `shared-contratos` | `shared/Package/Runtime/`, `docs/features/` | **Antes** de server e client: mensagens, IDs, eventos e tipos que os dois lados usam | `feature/<x>/<descricao>-contrato` | Sonnet |
| `regras-do-jogo` | `shared/Package/Runtime/Features/`, `tests/`, `docs/features/` | Fórmulas puras (dano, fusão, sorteio de skills, experiência), com testes | `feature/<x>/<descricao>-regras` | Sonnet |
| `server-core` | `server/.../Core/`, `Program.cs`, `.csproj` e configuração (`appsettings`, `Properties/`) do servidor, ADRs, arquitetura | Infra do servidor: rede, sessões de grupo, despacho de mensagens, eventos, persistência | `feature/core/<descricao>-server` | Opus |
| `server-funcionalidade` | `server/.../Features/`, migrações em `server/.../Core/Persistencia/Migrations/`, `docs/features/` | Backend de **uma** funcionalidade | `feature/<x>/<descricao>-server` | Sonnet (Opus em tarefas de risco) |
| `client-core` | `client/Assets/_Game/Core/` (só scripts) | Infra do client: conexão, cenas, input, câmera e controle em 3ª pessoa, UI base | `feature/core/<descricao>-client` | Opus |
| `client-funcionalidade` | `client/Assets/_Game/Features/` (só scripts) | Client de **uma** funcionalidade | `feature/<x>/<descricao>-client` | Sonnet (Opus em tarefas de risco) |
| `artista-mundo` | `Features/Mundo/`, `ThirdParty/`, `docs/arte/` | Hub, peças modulares, gerador de dungeon, ambientação | `feature/mundo/<descricao>-arte` | Sonnet |
| `artista-personagens` | `Features/Personagem/`, `ThirdParty/`, `docs/arte/` | Modelos, animações, Animator, prefabs por raça, encaixes | `feature/personagem/<descricao>-arte` | Sonnet |
| `artista-itens` | `Features/Itens/`, `ThirdParty/`, `docs/arte/` | Modelos de armas e equipamentos, ícones, encaixe | `feature/itens/<descricao>-arte` | Sonnet |
| `itens` | `Features/Itens/` no shared, no servidor e no client, mais `docs/features/itens.md` | Inventário, baú, equipamentos e loot. A arte 3D continua com o `artista-itens` | `feature/itens/<descricao>-contrato`, `-server` ou `-client` | Sonnet (Opus se a regra decidir drop, preço ou fusão de verdade) |
| `economia` | `Features/Economia/` no shared, no servidor e no client, mais `docs/features/economia.md` | Coroas, loja de NPC, mercado, baú de grupo, taxas e saídas de moeda; mantém o fluxo de moeda coerente. Balanceamento de combate não é dele | `feature/economia/<descricao>-contrato`, `-server` ou `-client` | Sonnet (Opus se a regra decidir preço, taxa ou troca de verdade) |
| `revisor` | Nenhuma (somente leitura) | Só PRs de risco: Core, segurança, concorrência, contrato ou regra que decide resultado (dano, drop, fusão, preço). O resto fica com o CI e a verificação do autor | — | Sonnet (Opus só nesses PRs de risco) |

As definições completas (regras de cada papel, passo a passo e formato do relatório) estão em [.claude/agents/](../.claude/agents/). São arquivos markdown que **qualquer IA consegue ler**.

O que fica com as **pessoas**:

- decidir regras de design e técnicas;
- montar cenas, prefabs e assets no editor do Unity (agentes só escrevem scripts);
- compilar e testar o client no Unity;
- aprovar e fazer o merge dos PRs.

## Fluxo de uma tarefa

```
                       ┌─ regras-do-jogo ──────── PR ─┐
designer-docs ─► shared-contratos ─► merge ─┼─ server-funcionalidade ── PR ─┐
(se precisar)        (PR pequeno)           └─ client-funcionalidade ── PR ─┴─► CI ─► pessoa: Unity e merge
                                                                              (revisor só em PR de risco)
```

0. **Ficha da tarefa.** O coordenador escreve na issue o objetivo, a cena, o contrato, as regras e os testes esperados, e a pessoa aprova uma vez. A cena é o que a pessoa faz e o que o jogo mostra ([Entrega da funcionalidade](#entrega-da-funcionalidade)). É o que mais evita retrabalho.
1. **Puxar para em andamento (trava de concorrência).** Antes de qualquer agente ou pessoa começar a codificar, a tarefa precisa ser puxada para em andamento com `npm run tarefa -- puxar <N> [branch] [--agente <papel>]`. Isso marca a issue com a label `em-andamento`, atualiza o GitHub Projects para `In Progress` e impede que outra pessoa ou agente trabalhe na mesma tarefa ao mesmo tempo. Em orquestrações com múltiplos agentes em paralelo, o coordenador cria subtarefas (`npm run tarefa -- subtarefa <pai> --titulo "..." --agente <papel>`) para que cada agente puxe e trave sua própria issue.
2. **Regra clara?** Se `docs/features/<x>.md` não diz o que implementar, o `designer-docs` detalha antes, ou a dúvida vai para o grupo. Tarefa pequena: as regras vão direto na ficha.
3. **Contrato primeiro.** O `shared-contratos` define mensagens, IDs e eventos num PR pequeno, que é revisado e mergeado. Os outros agentes partem da `main`, então precisam do contrato lá.
4. **Em paralelo.** `regras-do-jogo`, `server-funcionalidade` e `client-funcionalidade` trabalham ao mesmo tempo, cada um no seu **worktree** (uma cópia separada do repo) e na sua branch, e cada um abre o seu PR.
5. **Revisão.** O CI confere formatação, padrão de commit e testes. O `revisor` só entra em PR de risco (veja a tabela de papéis). No resto, a verificação proporcional do autor basta.
6. **Pessoas.** Olham a cena no jogo, aprovam e fazem o merge. Conteúdo de Unity que não sai de script (cena, prefab) continua com quem monta.

Várias funcionalidades podem andar ao mesmo tempo, cada uma com o seu trio de agentes. O limite real é a capacidade de review das pessoas: prefira PRs pequenos.

## Entrega da funcionalidade

A tarefa de jogo termina na cena que o jogador usa. Contrato, servidor e teste são o caminho até ela. A entrega é a cena pronta.

A ficha traz a **cena**, em frases que uma pessoa reconhece no jogo:

- o que a pessoa faz (tecla, clique, aproximar);
- o que o jogo mostra (tela, texto, modelo, barra), no padrão de [Lumora UI v1](../design/interface/GUIA-DE-ESTILO.md);
- os cantos da própria regra: vazio, erro e o limite, em português.

Quem faz o client entrega essa cena nos scripts. Uma tela que só escreve o nome da mensagem não é a cena.

O PR de contrato ou de servidor não fecha a issue da funcionalidade. `Closes` vai no PR em que a cena está. Em "Como testar", a pessoa lê o que fazer e o que aparece.

No merge, olhe a cena. Aba faltando, texto provisório ou tela fora do visual do jogo voltam para a branch.

A [regra 16](../AGENTS.md) continua: polimento fora desta tarefa não ganha uma rodada extra. Acabar a cena desta tarefa é a entrega, não um extra.

### Agentes artistas e o Unity

Os artistas ([ADR 0014](decisoes/0014-conteudo-3d-e-agentes-artistas.md)) buscam assets CC0 conforme a necessidade e montam o conteúdo **pelo Unity** (editor aberto via Unity CLI, ou scripts de editor em modo batch). Como o projeto Unity é um só, **os passos com Unity rodam um agente por vez, no checkout principal**; buscar assets e escrever código pode andar em paralelo. Enquanto um artista usa o Unity, ninguém troca a branch do checkout principal. Antes de abrir o PR, `npm run conferir-unity` confirma que `.meta` e materiais alterados pelo Unity entraram no commit. O CI repete a parte dos `.meta` que faltam.

Lições de processo (Blender, commits em etapas, Unity aberto) estão em [licoes.md](licoes.md).

### Fechamento: o coordenador integra e limpa o que criou

Vale para quem coordena agentes (IA ou pessoa). Aprendido na orquestração da [#319](https://github.com/GabrielNied/jogo_do_grupo/issues/319), onde quatro agentes pararam sem editar nada e o trabalho foi refeito.

- **A base do worktree do agente pode estar atrás.** Um agente com `isolation: worktree` parte do commit do checkout principal quando é lançado. Edição não commitada do coordenador (ex.: um arquivo recém-gerado) **nunca chega** ao agente. Commite antes numa branch e ponha no prompt o passo 0: `git merge <branch>` (local, sem push) e conferir o arquivo que ele precisa. Sem isso o agente para sem editar nada e a rodada é paga duas vezes.
- **Trabalhe num worktree seu.** O checkout principal é compartilhado e outra sessão pode trocar de branch nele, levando junto o que não foi commitado. O coordenador faz o trabalho do PR num worktree próprio (`npm run tarefa -- puxar <N> --worktree <pasta>`) e commita cedo.
- **O agente commita local.** Na própria branch `worktree-agent-*`, sem push, e informa a branch e o hash no relatório. Sem commit, a mudança fica só no worktree dele, e um worktree sem mudança some quando o agente termina.
- **Integrar com `git cherry-pick <hash>`, não com `git merge` da branch do agente.** A branch herda commits da base dele; o merge arrasta trabalho alheio para o PR. Antes de aceitar, `git show --stat <hash>` confere que só há arquivos da área do agente. Se o agente deixou edição sem commit, extraia com `git diff` e aplique com `git apply --3way`.
- **Quem coordena limpa o que criou, depois do merge do PR:** `npm run limpar-agentes -- --aplicar` (`--horas 0` se os agentes já terminaram; `--remotas` para o GitHub). Integrar por cherry-pick faz o script **não reconhecer** o merge e manter o worktree ("N commit(s) sem PR mesclado"). Nesse caso, confira que o conteúdo já está na `main` (`git diff origin/main <branch> -- <arquivos>`) e remova só os **seus** com `git worktree remove` e `git branch -D`. Não toque em worktree de outra pessoa ou agente que apareça na mesma lista.
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

- **Lock de tarefa (em andamento):** ninguém trabalha em tarefa sem puxá-la (`npm run tarefa -- puxar <N>`). Se já estiver marcada com `em-andamento` ou `In Progress`, outros agentes e desenvolvedores são impedidos de iniciar a mesma tarefa. Em paralelo, cada agente opera na sua subtarefa própria.
- **Pastas separadas:** cada agente só edita a sua área. No Claude Code, um hook bloqueia a edição fora dela.
- **Worktrees:** cada agente trabalha numa cópia própria do repo, então um não vê os arquivos pela metade do outro.
- **Contrato antes:** server e client implementam o mesmo contrato, já mergeado.
- **Uma funcionalidade por tarefa:** os agentes de funcionalidade nunca mexem em duas.

## Regras para todos os agentes

- **Consumo:** tarefa pequena (até ~50 linhas) o coordenador faz direto, sem agente; tarefas pequenas da mesma área vão num agente só; sessão do coordenador curta, com `/clear` por rodada. Relatórios curtos (até 15 linhas; revisor até 20) e verificação proporcional à alteração (matriz da skill `preparar-pr`). Não repita o que o CI já confere. O `revisor` é uma passada só, e só em PR de risco: Core, segurança, concorrência, contrato ou regra que decide resultado. Arte, tela, conteúdo e ligar um botão não levam revisor. Sonnet é o padrão; Opus só nessas tarefas de risco.

- **Entrega:** a tarefa de jogo termina na cena que o jogador usa ([Entrega da funcionalidade](#entrega-da-funcionalidade)). O PR de contrato ou de servidor não leva `Closes` dessa issue.
- **Lock de tarefa:** confira que a tarefa está livre e puxe-a para em andamento antes de alterar código (`npm run tarefa -- puxar <N>`). Se a tarefa já estiver em andamento, pare e avise. Se precisar interromper sem concluir, libere com `npm run tarefa -- liberar <N>`.
- **Regra 0:** nunca apagar nada do Figma; o resto a IA pode apagar ([ADR 0026](decisoes/0026-ia-pode-apagar-menos-o-figma.md)). No Claude Code, um hook do projeto ([scripts/agentes/bloquear-apagar.mjs](../scripts/agentes/bloquear-apagar.mjs)) barra a remoção no Figma, o push forçado e apagar repositório, release ou projeto. Worktrees e branches temporários dos agentes saem com `npm run limpar-agentes -- --aplicar` depois do merge ([ADR 0018](decisoes/0018-limpeza-do-que-os-agentes-criam.md)).
- **Autorização:** quando uma pessoa pede uma orquestração, ela autoriza os agentes a commitar, enviar as próprias branches e abrir PRs. **Merge é sempre humano.**
- **Fora da área:** se a tarefa exige mudar algo fora da área do agente (um contrato, o Core, outra funcionalidade), o agente para e descreve a mudança no relatório, para o agente certo fazer.
- **Relatório:** todo agente termina com o que fez, a branch e o PR, as verificações, o que outros agentes ou pessoas precisam fazer e as dúvidas.

## Como rodar

### Claude Code

Os agentes estão em [.claude/agents/](../.claude/agents/). Cada um roda num git worktree isolado (`isolation: worktree`) e tem um hook que limita as pastas.

- **Orquestrado (recomendado):** na sessão principal, peça a tarefa e a IA segue a skill [orquestrar-tarefa](../.claude/skills/orquestrar-tarefa/SKILL.md). Ela monta o plano, pede a sua aprovação, roda o contrato e depois lança os agentes em paralelo, em segundo plano.
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

~~Worktrees que sobrarem depois do merge são removidos por uma **pessoa** (`git worktree remove <pasta>`); a IA não apaga.~~ Substituído em 2026-10-11 pelo [ADR 0018](decisoes/0018-limpeza-do-que-os-agentes-criam.md): o coordenador (ou a pessoa) roda `npm run limpar-agentes -- --aplicar` depois dos merges. O script remove só worktrees e branches cujo conteúdo já está na `main` e explica o que ficou.

## Limitações atuais

- **Unity:** um editor por vez. `npm run unity:editor` distingue o editor do Hub. `npm run unity:compilar` confere se o client compila; o visual só uma pessoa testa no editor. A primeira importação de um worktree novo é lenta: não abra outro só para repetir. Se o editor estiver aberto, o agente para uma vez. Quem gera o `.meta` depois é o coordenador, com `npm run unity:importar`, sem relançar o agente.
- **Sem Unity (nuvem):** desde o [ADR 0019](decisoes/0019-unity-no-ci.md), os comandos `unity:*` pulam com aviso, `npm run unity:meta` gera os `.meta` de script, pasta, `.asmdef` e estilo, e o CI "Client (Unity)" compila e testa o client depois do merge. O agente abre o PR normal e registra o que só o Unity faz (menos compilar e rodar testes EditMode, que o CI faz) com `npm run tarefa -- pendencia-unity <PR> --falta "..."`. Numa máquina com Unity, o início da sessão avisa das pendências abertas, e quem puxa resolve na branch do PR (se ainda estiver aberto) ou numa branch nova.
- ~~**Testes:** o framework de testes ainda não foi decidido (ADR em aberto). Até lá, `regras-do-jogo` descreve os casos de teste no relatório.~~ Substituído em 2026-10-11 pelo [ADR 0013](decisoes/0013-testes-automatizados.md): todo PR de código traz testes, e o CI roda `dotnet test`.
- **Fundação primeiro:** agentes de funcionalidade dependem da API do Core (rede, sessões, despacho de mensagens). Enquanto o marco M0 não terminar, os trabalhos que já podem andar em paralelo são: `server-core`, `client-core`, `regras-do-jogo` (fórmulas puras) e `designer-docs`.
