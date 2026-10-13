# Lições do processo

Problemas que já aconteceram trabalhando no repositório (com pessoas e com agentes de IA) e o que fazer para não repetir. É um registro vivo: **novo problema, nova entrada** (skill [registrar-licao](../.claude/skills/registrar-licao/SKILL.md)). Cada lição diz o que houve, a regra para evitar e onde a regra já está escrita.

Formato: `### AAAA-MM-DD · título` com **O que houve**, **Regra** e **Origem** (PR ou issue).

## Regras gerais

- **Commite em etapas pequenas, mesmo em trabalho em andamento.** O que não está num commit pode sumir.
- **Um problema novo vira lição no mesmo PR que o corrigiu** (ou num PR de docs logo depois), não só uma conversa.

### 2026-10-11 · Branch velha e checkout dividido (issue #442)

- **O que houve:** `npm run tarefa -- puxar` sem `--worktree` caiu no fallback e criou a branch a partir da `main` local velha (o `pull` falhou por um `.mat` modificado). Depois, outra sessão trocou o HEAD do checkout principal no meio da edição.
- **Regra:** o `puxar` cria a branch sempre da `origin/main`. Sessão que não é a dona do checkout principal trabalha num worktree próprio (`--worktree`).
- **Origem:** issue #442.

### 2026-10-11 · Entrega crua

- **O que houve:** Funcionalidades chegaram com contrato, servidor e teste, e a parte que o jogador usa ficou pela metade (a aba de amigos não entrou no PR, a queda ficou sem tela). A issue parecia encaminhada e o resultado não dava para usar.
- **Regra:** A issue de jogo só fecha no PR da cena pronta. A regra está em [Entrega da funcionalidade](agentes.md#entrega-da-funcionalidade).
- **Origem:** conversa de 2026-10-11; exemplos nas issues #395 e #125. Registrada na #438.

### 2026-10-11 · Raças dos personagens (PR #383)

- **Um Blender por vez.** Vários `blender.exe` ao mesmo tempo (agentes ou comandos em paralelo) travaram e reiniciaram o computador. O artista roda um modelo por vez, em primeiro plano, com limite de tempo, e confere com `tasklist` que nenhum ficou vivo antes do próximo. Prévia em baixa resolução, uma por modelo. Coordenador nenhum dispara Blender em paralelo nem por subagente.
- **Commit por etapa, mesmo em WIP.** Uma mudança grande não commitada no checkout principal foi perdida quando algo fora da sessão reverteu a árvore de trabalho (nem `stash` guardou). O agente commita ao fim de cada etapa grande (scripts, binários gerados, gerador, docs) na branch da tarefa; o commit pode ser ajustado depois, o arquivo perdido não volta. Trabalho longo de artista pede worktree próprio quando houver outras sessões no mesmo diretório.
- **`npm run unity:editor` olha qualquer Unity aberto**, até o de outro projeto (ex.: `jogo_do_grupo_pendencias`), e então bloqueia o modo batch. Se bloquear, confira quem está aberto na saída do comando e espere ou peça para fechar essa janela.
- **O coordenador reparte por arquivo.** Artista só em `arte/` e `Features/Personagem/{Art,Editor,Resources}`; contrato, servidor, tela e testes ficam com o coordenador (ou outro agente). Assim os dois trabalham ao mesmo tempo sem mexer no mesmo arquivo; o nome do prefab (`Jogador_<NomeDoEnum>`) é o único combinado entre eles.
- **Push depois do merge não entra na `main`.** O PR #383 foi mergeado enquanto eu ainda commitava docs na mesma branch; o último commit ficou de fora. Antes do push final, `gh pr view <N> --json state`; se já foi mergeado, parta de `origin/main` e abra um PR novo (foi o #389).
- **Shell: heredoc grande com muitas aspas e `sed` em cadeia falha por inteiro.** Um erro de parse não executou nada e quase passou despercebido. Para arquivos de código use a ferramenta de escrita de arquivos; `sed` só para trocas de uma linha.

### 2026-10-11 · CI do Unity cancelado na main (issue #402)

- **`cancel-in-progress` na `main` com merges frequentes = nenhuma execução termina.** O Client (Unity) leva ~9 min e os merges vinham a cada poucos minutos: 9 de 10 execuções na `main` foram canceladas, o client ficou sem validação e pendências de "compilar/rodar EditMode" se acumularam esperando alguém com Unity. Na `main`, a execução em andamento termina; só PR cancela. Pendência que só pede compilar e testar EditMode não é criada, e as antigas fecham sozinhas quando o CI passa ([ADR 0019](decisoes/0019-unity-no-ci.md)).

### 2026-10-11 · Rodadas perdidas na nuvem e subtarefas vazias (issue #227)

- **Sem .NET na nuvem, cada erro de formatação custava uma rodada de CI** e, no PR de client, reiniciava os 10 min do Unity. A rede do ambiente já deixa baixar o .NET, e o npm só funciona pelo proxy (o `registry.npmjs.org` vem na lista "sem proxy" sem ter saída direta). `npm run nuvem:preparar` instala npm, .NET 10 e CSharpier; um hook de início de sessão já dispara isso em segundo plano na nuvem. Formate e rode `dotnet test` antes do push.
- **Subtarefa só com "Subtarefa de #N" obrigava a reler a issue pai, a doc e o contrato.** `npm run tarefa -- subtarefa` agora escreve onde ler e onde mexer, e o coordenador passa `--contexto` com o que fazer.

### 2026-10-12 · Preparação da nuvem rodando duas vezes ao mesmo tempo

- **O que houve:** o hook de início de sessão dispara `nuvem:preparar` em segundo plano e manda o agente rodar `npm run nuvem:preparar` antes do primeiro `dotnet`. As duas rodavam juntas, e o `dotnet tool restore` da segunda falhava com "file is being used by another process": o agente tinha de rodar de novo.
- **Regra:** a preparação do hook grava o pid em `~/.cache/jogo-do-grupo/preparar-nuvem.pid`, e o `npm run nuvem:preparar` espera ela terminar antes de começar. O comando está em [scripts/agentes/preparar-nuvem.mjs](../scripts/agentes/preparar-nuvem.mjs).
- **Origem:** rodada do loop autônomo de 2026-10-12 (PR #525).

### 2026-10-11 · Hooks no PowerShell sem sh no PATH

- **O que houve:** o commit da #519 falhou com `/usr/bin/env: sh: No such file or directory`, apesar de o Git estar instalado.
- **Regra:** se isso ocorrer no PowerShell, rode o commit pelo Git Bash, com `/usr/bin:/bin` no PATH da sessão. Apenas localizar `sh.exe` pelo PowerShell pode não resolver o PATH visto pelos hooks. Mantenha os hooks ativos, sem `--no-verify`.
- **Origem:** issue #519.

### 2026-10-12 · Worktree de agente sem node_modules

- **O que houve:** o agente de Core da #518 trabalhou num worktree criado por `npm run tarefa -- puxar --worktree`; o primeiro commit foi recusado porque o commitlint do hook não existia ali, e ele teve de rodar `npm install` no meio da tarefa.
- **Regra:** o `puxar --worktree` liga o `node_modules` do checkout principal no worktree ([worktree-npm.mjs](../scripts/git/worktree-npm.mjs)). Quem cria worktree à mão faz o mesmo ou roda `npm install` antes de commitar.
- **Origem:** PR #533 (relato do agente).

### 2026-10-12 · Unity do CI morto por outro job com a mesma licença (issue #529)

- **O que houve:** três execuções seguidas do Client (Unity) na `main` (e uma de PR) morreram com `Killed` / exit 137 segundos depois de ativar a licença, sem compilar nada. Cada uma coincidiu com outro job do Unity rodando: a licença Personal é uma só. Uma pendência "Client falhou na main" chegou a ser corrigida com mudança de código enquanto a causa da falha seguinte era essa.
- **Regra:** um Unity por vez no repositório (grupo de concorrência `client-unity-licenca` no `client.yml`, [ADR 0019](decisoes/0019-unity-no-ci.md)). Falha com `Killed`/137 logo após a licença não é erro de código. Confira se havia outro job do Unity ao mesmo tempo e qual cache da `client/Library` ele restaurou antes de mexer no client. Depois do primeiro ajuste, a causa que sobrou era um cache antigo da `Library`, e o remédio foi trocar o prefixo da chave (`Library-v2-`, PR #550). Voltou no #600 (`Library-v3-`); desde então, se o editor morre sem gravar resultados, o próprio job apaga os caches `Library-*`. Outra lição do #600: um `Killed` seguido esconde falhas reais de teste. Leia o log da execução que chegou a rodar os testes antes de culpar só a licença.
- **Origem:** issue #529.

### 2026-10-12 · `dotnet test` preso na telemetria na nuvem

- **O que houve:** no `/trabalhe` das #489 e #493, o primeiro `dotnet test` do agente de Itens ficou parado tentando enviar telemetria do .NET, que o proxy da nuvem bloqueia, e estourou o tempo do comando; a rodada teve de ser refeita.
- **Regra:** as sessões de agente rodam com `DOTNET_CLI_TELEMETRY_OPTOUT=1` e `DOTNET_NOLOGO=1` (bloco `env` do [.claude/settings.json](../.claude/settings.json)), como o CI já faz no `server.yml`. Em outra ferramenta de IA na nuvem, exporte as duas antes do primeiro `dotnet`.
- **Origem:** relato do agente de Itens no PR #540.

### 2026-10-12 · Agentes em worktree: comando composto recusado e teste que estoura o tempo

- **O que houve:** na leva de 2026-10-12, os agentes de Combate (#478) e Mobs (#487), cada um no seu worktree fora da pasta principal, tiveram comandos com heredoc, variável ou `$(...)` recusados pela permissão da sessão e precisaram dividir as chamadas ou escrever scripts no rascunho. O `dotnet test JogoDoGrupo.slnx` leva cerca de 3 min e estourou o limite padrão de 2 min da ferramenta de shell, e a rodada teve de ser repetida.
- **Regra:** no worktree, prefira comandos simples (um por chamada) e escreva arquivos com a ferramenta de escrita, não com heredoc. Rode `dotnet test JogoDoGrupo.slnx` com tempo limite de 10 min, ou filtre pelo que mudou (`--filter`) e deixe a suíte inteira para o fim.
- **Origem:** PRs #545 e #548 (relato dos agentes).

### 2026-10-12 · Worktree fora da pasta de agentes sobrevive ao merge

- **O que houve:** depois do squash, o worktree `C:/Dev/jdg-custo` e a branch `chore/core/reduz-verificacoes` (PR #581) continuaram no Git. A limpeza automática só olhava `.claude/worktrees/`; o worktree de fora segura a branch, então ela nunca entra na lista de branches soltas.
- **Regra:** worktree limpo cujo PR já foi mesclado, foi fechado com a ponta no GitHub, ou cujo conteúdo já está na `main`, sai na limpeza automática mesmo fora de `.claude/worktrees/`. Checkout da `main` ou de branch que só está no `origin` fica, a menos que se passe `--todos`.
- **Origem:** PR #583.

### 2026-10-12 · Trava sem dono e branch que volta depois do merge

- **O que houve:** nove issues ficaram com `em-andamento` sem branch no remoto, e seis branches voltaram porque um push chegou depois do squash. O quadro parecia cheio e o commit tardio ficava de fora da `main`.
- **Regra:** o `puxar` não passa de 3 funcionalidades de jogo. Trava sem branch no `origin` e sem PR aberto sai depois de 2 h (`npm run tarefa -- listar` e a limpeza do `pull`). Push numa branch cujo PR já foi mergeado é recusado: parta de `origin/main` e abra um PR novo.
- **Origem:** PR #559.

### 2026-10-12 · Print da tela em vez de outra rodada

- **O que houve:** o inventário passou por três PRs (#575, #586, #591) porque o agente compilava e quem via a tela era a pessoa, que mandava o print de volta.
- **Regra:** mudança de tela ou HUD grava o print em 1280×720, 1920×1080 e 2560×1440, e o agente lê os PNG antes do PR. O comando está em [Prints da tela](agentes.md#prints-da-tela).
- **Origem:** issue #593.

### 2026-10-12 · A regra 16 esperava a espera se repetir

- **O que houve:** o corte de custo só entrava quando o retrabalho já tinha aparecido de novo. A pergunta "a próxima vez sai mais barata?" não era passo do fim da tarefa, então o primeiro tropeço ficava só na conversa.
- **Regra:** antes do PR, a reflexão da [regra 16](../AGENTS.md). Sem corte, não escreva nada.
- **Origem:** conversa de 2026-10-12.

### 2026-10-12 · Commit e PR no PowerShell

- **O que houve:** o corpo do commit passava de 100 caracteres, `gh pr create --body` quebrava na crase, e o push numa branch já mergeada deixava o commit de fora da `main`. Cada tentativa repetia a rodada.
- **Regra:** `npm run entregar` commita por arquivo, abre o PR com `--body-file` e, se o PR da branch já foi mergeado, segue numa branch nova a partir de `origin/main`. O procedimento está em [Entregar](git.md#entregar).
- **Origem:** conversa de 2026-10-12.

### 2026-10-12 · Leva empilhada conflita depois do squash

- **O que houve:** a leva 2 de Combate (#602) partiu da branch da leva 1 (#597). O squash do #597 deu outro hash ao mesmo conteúdo, e o #602 passou a conflitar em 9 arquivos consigo mesmo. O push forçado para refazê-lo é barrado pela regra 0.
- **Regra:** leva empilhada nasce com `puxar --base` e cita `Depende de #<N>` no corpo; depois do squash da leva de baixo, `npm run reempilhar -- <PR>` refaz o PR de cima sobre a `main` num PR novo ([Leva empilhada](git.md#leva-empilhada)).
- **Origem:** PR #607.

### 2026-10-13 · `.meta` faltando só aparecia no CI

- **O que houve:** na leva de Itens, o PR #698 subiu um script novo do shared sem o `.meta`. O agente rodou `npm run unity:meta` nos PRs seguintes, mas não neste, e só o check `padrao-git` do CI acusou, uma rodada depois do push.
- **Regra:** o `pre-push` confere os `.meta` que o `npm run unity:meta` gera (script, pasta, `.asmdef`, `.uss`/`.tss`, texto) e barra o push com o comando para gerá-los. O resto continua com o `npm run conferir-unity` e, sem Unity, com a pendência ([git.md](git.md)).
- **Origem:** PR #698.

### 2026-10-11 · Edição e testes no Windows

- **O que houve:** na #516, o alias Python do Windows não executou o script; a edição exigiu corrigir codificação e quebras de linha. Uma segunda execução de testes também encontrou DLL em uso pela anterior.
- **Regra:** prefira a ferramenta de patch para editar código. Quando Python for necessário, use o runtime disponível no ambiente e codificação UTF-8 e quebras de linha explícitas. Espere a sessão anterior de `dotnet test` terminar antes de iniciar outra no mesmo worktree.
- **Origem:** issue #516.
