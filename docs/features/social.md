# Social

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 13000–13999

## Resumo

Party (time, chamado de **grupo** nas regras), guilda e chat.

Esta página detalha o **grupo** (convite, lista, líder, sair e expulsar; issue #94). ~~Chat (#95),~~ guilda e divisão de loot ficam para outras tarefas. O chat tem regras em proposta em [Chat de texto](#chat-de-texto-issue-95) (acrescentado em 2026-10-11). Amigos e ignorados têm regras em proposta em [Amigos e ignorar](#amigos-e-ignorar-issue-180) (acrescentado em 2026-10-11).

## Definido

- Party: tamanho e divisão de loot
- Party de até 5 jogadores (o tamanho de uma dungeon); raids juntam 10 (decidido em 2026-10-10)
- Guilda: níveis, base própria, benefícios
- **Decisões do Gabriel em 2026-10-16** (quiz de lacunas de MMO; números a propor):
  - **Grupo de raid:** um grupo só, de até 10, com um líder; a tela mostra duas listas de 5 ([instâncias](instancias.md#definido)).
  - **Fila automática por papel:** a pessoa escolhe tank, healer ou DPS livremente (o servidor não confere os itens); o grupo resolve se alguém escolher errado.
  - **Guilda:** até **100** membros; criar custa coroas; quatro cargos (líder, oficial, membro, recruta) com permissões de convidar, expulsar e usar o banco; chat e banco de guilda; **níveis e bônus** já na primeira versão, subindo com a atividade dos membros. Base própria fica para depois.
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3): **sem baú de party**; banco de guilda — **todos depositam**, só **líder e oficial retiram** ([economia](economia.md#definido)).
  - **Canais de chat novos:** Guilda, Mundo/Comércio (alcança o servidor todo, com intervalo mínimo entre mensagens), Raid e Procurar grupo.
  - **Denúncia, silenciar e filtro de palavrões:** todos juntos com a ferramenta de GM ([#187](https://github.com/GabrielNied/jogo_do_grupo/issues/187)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições; números em constante nomeada):
  - **Regras do grupo:** as propostas viram regra. Quando o líder sai ou cai, assume o membro mais antigo, e o líder pode passar a liderança. Só o líder expulsa, sem votação. A vaga fica guardada **60 s** depois de desconectar e o convite vale **60 s**. Convidar por nome só exige que o personagem esteja online. O grupo desfaz quando fica com 1 pessoa. O modo "não aceitar grupo" fica fora da primeira versão.
  - **Amizade e ignorar ficam na conta**, gravados no servidor: valem em qualquer computador e em todos os personagens, e o servidor filtra sussurro, convite e troca de quem foi ignorado. Quem foi ignorado recebe "não encontrado".
  - **O amigo vê em que área o outro está** (cidade, campo, dungeon), além do online e do nome.
  - **Moderação do chat:** o servidor não guarda mensagens por padrão. Ao denunciar, as últimas mensagens do denunciado vão junto com a denúncia ([#187](https://github.com/GabrielNied/jogo_do_grupo/issues/187), mascaradas como no [ADR 0033](../decisoes/0033-mascaramento-de-logs.md)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 6): a [#351](https://github.com/GabrielNied/jogo_do_grupo/issues/351) cobre só **localização/área** dos aliados na lista do HUD; **vida** (barra, número e sync) ficou na [#900](https://github.com/GabrielNied/jogo_do_grupo/issues/900) (entregue). Estados **longe** e **offline** seguem [interface.md](interface.md#hud-do-jogo).

## Em aberto

- [x] Tamanho máximo de party e guilda? Party decidida em 2026-10-10: 5 (veja "Definido"); ~~party~~ sai desta pergunta. Falta o tamanho da guilda. Guilda: 100, decidido em 2026-10-16.
- [x] Regras de divisão de loot. Respondido: loot individual para quem participou ([fatia, seção 5](fatia-jogavel.md#5-loot-no-chão)) e troca do loot da luta por 2 h ([itens](itens.md#definido)).
- [x] **Proposta do livro de Asterra (2026-10-11), arquivada em 2026-10-16 (quiz 3)** ([figjam.md](../brainstorm/figjam.md), seção 10, "Grupo, disputa de loot e PvP futuro") — **registro histórico**; em conflito vale **Definido** (loot por participação, party 5/raid 10):
  - **Tamanho:** instância de 20 jogadores e grupo que pode reunir os 20, com vida e localização dos aliados e baú compartilhado conforme permissão. **Decidido em 2026-10-11 (/decida, #351):** o limite fica **party de 5, raid de 10, dungeon de 5** (o definido não muda). Entra a lista com localização e vida dos aliados, no limite atual; o grupo de 20 fica só como proposta registrada. Antes: hoje está definido party de 5 e raid de 10 (2026-10-10); a regra não muda, a pergunta fica aberta (veja a pergunta de raid abaixo e [instancias.md](instancias.md)).
  - **Loot por dano:** o dano ao mob define quem recebe os itens **individualmente**, mesmo dentro do grupo; o primeiro golpe no recurso garante a coleta. Empate favorece o primeiro atacante; a contagem zera se o mob voltar e recuperar vida; só conta dano real, sem excedente após a morte. Cura e tanque sem prioridade automática; a participação por apoio e defesa pode dar XP, sem mudar o dono dos itens; amigos compensam por troca. **Conflito:** a [fatia](fatia-jogavel.md#5-loot-no-chão) decidiu em 2026-10-11 loot individual para quem participou, não por maior dano; a decisão vale e o livro fica registrado aqui.
  - **Aliados:** skills não causam dano entre membros do grupo.
- [x] Líder: quem herda quando o líder sai ou cai? (proposta: o membro mais antigo no grupo) e o líder pode passar a liderança? (proposta: sim) Decidido em 2026-10-16: a proposta.
- [x] Só o líder expulsa, ou qualquer membro pode pedir votação? (proposta: só o líder expulsa, sem votação) Decidido em 2026-10-16: a proposta.
- [x] Tempo que a vaga fica guardada após desconexão (proposta: 60 s) e tempo de validade do convite (proposta: 60 s) Decidido em 2026-10-16: a proposta.
- [x] Convidar por nome exige amizade ou proximidade? (proposta: não, basta o personagem estar online) Decidido em 2026-10-16: a proposta.
- [x] Bloquear convites (modo "não aceitar grupo") fica fora da primeira versão? ~~E a fila automática de grupo?~~ A fila entra (decidido em 2026-10-16). Decidido em 2026-10-16: fica fora.
- [x] Raid de 10: é um grupo maior ou dois grupos de 5 juntos? (veja [instancias.md](instancias.md)) Decidido em 2026-10-16: um grupo só, até 10.
- [x] O grupo desfaz ao ficar com 1 pessoa? (proposta: sim) Decidido em 2026-10-16: sim.
- [x] Chat (#95), decisões do grupo:
  - [x] Aprovar os três canais (Cidade, Grupo, Sussurro) e os números da tabela de limites (proposta). Decidido em 2026-10-16 (quiz de definições 2): aprovados.
  - [x] O canal **Cidade** vale só para quem está dentro dos muros (decidido, #500; veja [Canais](#canais)); não chega à dungeon nem ao terreno em volta.
  - [x] Respondido em 2026-10-16: entra com a ferramenta de GM (#187). Filtro de palavrões: fica fora da primeira versão (proposta) ou entra uma lista simples?
  - [x] **Ignorar jogador** gravado neste computador (proposta) ou na conta, pelo servidor? Decidido em 2026-10-16: na conta, pelo servidor.
  - [x] O servidor guarda as mensagens em algum lugar para moderação? (proposta: não guarda; só o histórico curto da Cidade em memória) Decidido em 2026-10-16: não guarda; a denúncia leva as últimas mensagens do denunciado.
  - [x] Denunciar jogador e silenciar por um moderador: ficam para depois? Decidido em 2026-10-16: junto com a #187.
  - [x] Decidido em 2026-10-16 (quiz de definições 2): pode (já é assim). Sussurro para quem está em outra área (ex.: você na cidade e a pessoa na dungeon): pode? (proposta: sim)
- [x] Amigos e ignorar (#180), decisões do grupo:
  - [x] Aprovar amizade e ignorar **por conta** (proposta) em vez de por personagem, e os números da tabela de [Amigos e ignorar](#amigos-e-ignorar-issue-180). Decidido em 2026-10-16: por conta.
  - [x] Quem foi ignorado recebe a mesma resposta de "não encontrado" (proposta) ou um aviso claro de que foi ignorado? Decidido em 2026-10-16: "não encontrado".
  - [x] O amigo vê **em que área** o outro está (cidade, dungeon)? (proposta: não na primeira versão; só online e o nome do personagem) Decidido em 2026-10-16: vê a área.

## Regras

### Grupo (issue #94)

Base: grupos do [World of Warcraft](../referencias.md) (convite por nome, líder, lista, sair e expulsar). Igual: grupo de até 5 com um líder, convite por nome que a outra pessoa aceita ou recusa. Diferente: não há classes nem papéis fixos (o papel vem dos itens), e o mundo é persistente e compartilhado (D1), então o grupo existe no mundo e não só dentro de uma dungeon.

Tudo marcado **proposta** é valor inicial para o grupo calibrar; não é decisão. O que está em "Definido" acima (grupo de até 5) é decisão.

Quem decide: **servidor**. O client só envia intenções (convidar, aceitar, recusar, sair, expulsar, passar liderança) e mostra a lista que o servidor manda.

#### Estado e limites

- Um personagem está em **no máximo um grupo** por vez. Quem não está em grupo está "sozinho" (não existe grupo de 1 guardado: o grupo nasce quando o primeiro convite é aceito, com 2 membros).
- Tamanho máximo: **10 membros**, contando o líder (`RegrasGrupo.MaximoDeMembros`): até 5 é grupo de dungeon, acima disso é grupo de raid ([instâncias](instancias.md#raid-de-10-grupo-de-raid-issue-190-recorte-1)). A fila automática segue montando grupos de 5.
- Cada grupo tem **1 líder** e a lista de membros em **ordem de entrada** (o líder fundador é o primeiro).
- O identificador do grupo é gerado pelo servidor; o client só o recebe.
- O grupo vale só **enquanto o servidor está no ar**. Reiniciar o servidor desfaz todos os grupos (**proposta**: o grupo não é salvo no banco).

#### Convidar (por nome) (**proposta**)

- Quem pode convidar: **qualquer personagem sem grupo** (vira líder se o convite for aceito) ou o **líder** de um grupo com vaga. Membro comum **não** convida.
- A pessoa é indicada pelo **nome do personagem** (sem diferenciar maiúsculas de minúsculas). O servidor resolve o nome; o client não envia IDs de outros jogadores.
- Alvo **offline** ou nome inexistente: recusa `AlvoNaoEncontrado` (a mesma resposta para os dois casos, para não revelar quem existe).
- Alvo **já em grupo** (qualquer grupo, inclusive o do convidante): recusa `AlvoJaEmGrupo`.
- Convidar a si mesmo: recusa `ConviteParaSiMesmo`.
- Grupo cheio (5): recusa `GrupoCheio`. Conta vagas já prometidas: membros mais convites pendentes não podem passar de 5 (um grupo de 4 só tem 1 convite pendente por vez).
- Convite **pendente para o mesmo alvo** do mesmo convidante: recusa `ConviteJaPendente`.
- O alvo recebe **no máximo 1 convite pendente por vez de cada convidante** e até **5 convites pendentes no total**; passando disso, `AlvoComConvitesDemais`.
- Cada convidante tem no máximo **4 convites pendentes** ao mesmo tempo (nunca mais que as vagas livres do grupo).
- **Validade do convite: 60 segundos**. Depois disso o servidor o descarta e avisa os dois lados (`ConviteExpirado` ao tentar aceitar).
- O convite é **cancelado** quando: o convidante fica offline, o convidante sai do grupo ou deixa de ser líder (convites do ex-líder caem), o alvo entra em outro grupo (os demais convites dele caem) ou o grupo desfaz.
- Limite de frequência: no máximo **1 convite por segundo** por convidante (**proposta**), para evitar abuso; acima disso, `ConvitesRapidosDemais`.

#### Aceitar e recusar

- Só o **alvo** do convite pode aceitar ou recusar. Outro personagem: `ConviteNaoEncontrado`.
- **Aceitar**: o servidor revalida na hora (convite ainda válido, convidante online, grupo com vaga, alvo ainda sem grupo). Se tudo bate, o alvo entra no fim da lista e todos recebem a lista nova. Se o convidante não tinha grupo, o grupo nasce com ele como líder e o alvo como segundo membro.
- Falhas ao aceitar: `ConviteNaoEncontrado`, `ConviteExpirado`, `GrupoCheio` (o grupo encheu por outro convite aceito antes), `AlvoJaEmGrupo` (o alvo já entrou em outro), `ConvidanteOffline`.
- **Recusar**: descarta o convite; o convidante recebe o aviso "recusou". Recusar um convite que já não existe não é erro grave: `ConviteNaoEncontrado`.
- Sem resposta em 60 s: expira, sem aviso de recusa (o convidante vê "expirou").

#### Líder

- O líder é quem **fundou** o grupo. Só ele convida, expulsa e passa a liderança.
- **Passar a liderança** (**proposta**): o líder escolhe outro membro **online** do grupo; a mudança é imediata e todos recebem a lista. Quem não é líder: `NaoELider`. Alvo fora do grupo: `AlvoNaoEstaNoGrupo`. Passar para si mesmo: `AlvoInvalido`.
- **O líder sai** (**proposta**): a liderança passa ao membro **mais antigo no grupo** que esteja online (o primeiro da lista depois do líder). Se não houver ninguém online além dele, vale o mais antigo entre os offline (o grupo continua; a vaga guardada segue as regras de desconexão).
- **O líder cai** (desconecta): não troca de imediato. Se continuar offline por mais que o tempo da vaga guardada (abaixo), a liderança passa como se tivesse saído, mas ele **continua membro** enquanto a vaga valer. Se voltar antes, **continua líder**.
- **Expulsar o líder** não é possível (`NaoPodeExpulsarLider`). O líder que quer sair usa "sair".

#### Sair e expulsar

- **Sair**: qualquer membro, a qualquer momento (inclusive o líder). Sair de uma instância ativa é decisão da dungeon (#66); aqui o servidor só remove o personagem da lista e avisa os outros.
- Se ao sair sobrar **1 membro**, o grupo se **desfaz** (**proposta**): o último membro volta a ficar sem grupo e recebe o aviso `GrupoDesfeito`.
- **Expulsar** (**proposta**): só o **líder** expulsa, um membro por vez, sem votação. Quem não é líder: `NaoELider`. Alvo fora do grupo: `AlvoNaoEstaNoGrupo`. O expulso recebe `ExpulsoDoGrupo`, e o servidor não deixa o grupo reconvidá-lo por **30 s** (**proposta**) para evitar o vai-e-volta.
- Expulsar quem está **offline** com a vaga guardada é permitido (libera a vaga na hora).

#### Desconexão e reconexão (**proposta**)

- O membro que desconecta **fica na lista marcado como offline** e a **vaga fica guardada por 60 segundos** (constante `RegrasGrupo.VagaGuardadaSegundos`; [#210](https://github.com/GabrielNied/jogo_do_grupo/issues/210) unifica o prazo do Social com o Core — ver [arquitetura](../arquitetura.md#reconexão-sem-perder-o-lugar-210)).
- Se voltar (reautenticar no tempo real) antes disso, o servidor devolve o grupo e ele continua com a mesma posição; os outros veem "voltou".
- Vencido o prazo, o servidor remove o membro como se tivesse saído. Se o grupo ficar com 1 pessoa, desfaz.
- Convites pendentes **para** quem desconecta caem na hora; convites **de** quem desconecta caem junto com a vaga guardada vencendo (ou na hora, se não tiver grupo).
- O servidor só mantém o grupo enquanto o tempo real está no ar; uma conta tem uma sessão só ([arquitetura](../arquitetura.md#servidor-tempo-real-udp)), então reconectar em outro aparelho assume o lugar.

#### O que o servidor decide e valida

- Quem é líder, quem está no grupo e em que ordem; tamanho (no máximo 5); a validade e a quantidade de convites.
- Que o personagem do pedido é mesmo quem diz ser (identidade vem da sessão autenticada, nunca de um campo do client).
- Que cada operação respeita as regras acima; o client nunca altera a lista sozinho.
- Que a lista só muda **em ordem**: duas operações do mesmo grupo nunca se misturam (processadas uma de cada vez por grupo), de modo que o resultado é o mesmo qualquer que seja a ordem de chegada de dois pedidos simultâneos, e perdedor recebe o erro correspondente.
- Em toda mudança, o servidor manda a **lista completa e atualizada** para todos os membros (não só a diferença), com o líder marcado e quem está offline.

#### Entrada na dungeon (ponteiro)

Quem pode entrar numa dungeon com o grupo e como o servidor cria a instância é decisão da tarefa #66 e de [instancias.md](instancias.md). Este documento só garante que o servidor sabe quem é o grupo, quem é o líder e quem está online; o resto não é definido aqui. Os grupos de inimigos de [fatia-jogavel.md](fatia-jogavel.md) não têm relação com o grupo de jogadores.

#### Erros e recusas que o servidor devolve (**proposta** de nomes)

| Código | Quando |
|---|---|
| `AlvoNaoEncontrado` | Nome inexistente ou personagem offline |
| `AlvoJaEmGrupo` | Alvo (ou quem aceita) já está em um grupo |
| `ConviteParaSiMesmo` | O convidante é o próprio alvo |
| `GrupoCheio` | Já tem 5 membros (ou vagas prometidas) ao convidar ou ao aceitar |
| `NaoELider` | Operação do líder feita por membro comum (convidar em grupo, expulsar, passar liderança) |
| `ConviteJaPendente` | Mesmo convidante já tem convite pendente para o alvo |
| `AlvoComConvitesDemais` | Alvo já tem 5 convites pendentes |
| `ConvitesRapidosDemais` | Mais de 1 convite por segundo do mesmo convidante |
| `ConviteNaoEncontrado` | Responder a convite que não existe ou que não é seu |
| `ConviteExpirado` | Aceitar depois de 60 s |
| `ConvidanteOffline` | Aceitar quando o convidante já saiu do jogo |
| `NaoEstaEmGrupo` | Sair, expulsar ou passar liderança sem estar em grupo |
| `AlvoNaoEstaNoGrupo` | Expulsar ou passar liderança para quem não é membro |
| `AlvoInvalido` | Passar liderança para si mesmo ou para membro offline |
| `NaoPodeExpulsarLider` | Tentar expulsar o líder |
| `ReconviteBloqueado` | Reconvidar quem foi expulso há menos de 30 s |

Avisos (não são erros): `ConviteRecebido`, `ConviteRecusado`, `ConviteExpirou`, `EntrouNoGrupo`, `SaiuDoGrupo`, `LiderMudou`, `ExpulsoDoGrupo`, `GrupoDesfeito`, `MembroOffline`, `MembroVoltou`, `ListaDoGrupo`.

#### Testes esperados (servidor)

Caminho feliz:
- Dois personagens: A convida B, B aceita; o grupo nasce com A líder e 2 membros, os dois recebem a lista.
- Recusar: o convidante recebe o aviso e nenhum grupo é criado.
- Líder convida até chegar a 5; todos recebem a lista a cada entrada.
- Membro sai; o líder sai e a liderança passa ao mais antigo online; o líder passa a liderança; o líder expulsa e o expulso recebe o aviso.
- Reconexão antes de 60 s devolve o grupo ao membro.

Cada erro da tabela: um teste por código, conferindo que **nada muda** no estado do grupo quando o pedido é recusado.

Limites:
- Exatamente 5 membros é aceito; o 6º convite é recusado com `GrupoCheio`.
- Convite com 59 s é aceito; com 60 s ou mais, `ConviteExpirado`.
- Vaga guardada: voltar com 59 s mantém a vaga; passar de 60 s remove.
- Grupo com 2 membros: um sai e o grupo se desfaz.
- Nome com maiúsculas diferentes encontra o mesmo personagem.
- Limite de 5 convites pendentes por alvo e de convites por convidante conforme as vagas.

Concorrência:
- Dois convites ao mesmo tempo para o mesmo alvo, de grupos diferentes: ele aceita um, o outro vira `AlvoJaEmGrupo` ao aceitar, e os demais convites dele caem.
- Aceitar com o grupo já cheio: grupo com 4 membros e dois convites pendentes; os dois aceitam ao mesmo tempo, só um entra e o outro recebe `GrupoCheio`.
- Líder sai ao mesmo tempo em que expulsa ou convida: o resultado é consistente (um líder só, sem membros fantasmas).
- Dois membros saem ao mesmo tempo deixando 1: o grupo desfaz uma vez só, sem erro duplicado.
- Convite aceito enquanto o convidante cai: `ConvidanteOffline`, e nenhum grupo meio criado.
- Duas expulsões simultâneas do mesmo alvo: uma vale, a outra recebe `AlvoNaoEstaNoGrupo`.

#### Servidor (issue #221)

Código em `server/src/JogoDoGrupo.Server/Features/Social/`: as regras vivem em `GruposDoServidor` (estado em memória, uma trava só, relógio injetável), o tempo real liga as mensagens a elas (`SocialModule`, `SaidaTempoReal`) e `VarreduraDosGrupos` aplica os prazos a cada segundo. Escolhas que as regras acima deixavam abertas:

- **Online** é ter uma sessão autenticada no tempo real (não precisa estar na cidade).
- **Ordem das recusas ao convidar:** `NaoELider`, `ConvitesRapidosDemais`, nome (`AlvoNaoEncontrado`, `ConviteParaSiMesmo`, alvo offline), `AlvoJaEmGrupo`, `ReconviteBloqueado`, `ConviteJaPendente`, `GrupoCheio`, `AlvoComConvitesDemais`. Toda tentativa que passa de `NaoELider` conta no limite de 1 por segundo, mesmo se for recusada.
- **Convite que acabou sem resposta** (expirou, o convidante caiu sem grupo ou o alvo entrou em outro grupo) fica lembrado por 60 s só para o alvo receber o código certo ao aceitar (`ConviteExpirado`, `ConvidanteOffline`, `AlvoJaEmGrupo`); depois disso vale `ConviteNaoEncontrado`. Convite cancelado por outro motivo (líder mudou, grupo desfeito, alvo caiu) some na hora.
- **Líder offline:** a vaga guardada e a troca de liderança vencem juntas, passados os 60 s; ali ele sai do grupo como se tivesse saído.
- **Reconvite bloqueado** vale para quem expulsou e para o grupo, e continua valendo se a expulsão desfez o grupo.

### Fila de grupo (Dungeon Finder, issue #1201)

Formação automática de grupos para dungeons no servidor (1 Tanque, 1 Healer e 3 DPS).

#### Regras do client e HUD
- **Persistência ao fechar janelas:** fechar a janela Social (`JanelaGrupo`, tecla O ou Esc) **não** cancela a busca nem descarta o estado da fila (`FilaDeGrupoDoClient`). Reabrir a janela a qualquer momento reflete exatamente o estado atualizado da fila (tempo decorrido, papel e contadores).
- **Indicador no HUD (Minimapa):** enquanto o jogador estiver na fila (`EmFila == true`), um ícone animado e giratório fica visível no canto superior direito do HUD (`RegiaoDoHud.SuperiorDireita`), próximo ao minimapa.
- **Interação pelo HUD:**
  - **Clique no corpo do ícone:** abre a janela Social diretamente focada na aba do Dungeon Finder (`JanelaGrupo.AbrirNaFila`).
  - **Botão Cancelar (✕):** cancela a fila imediatamente pelo HUD (`SairDaFilaDeGrupo`), sem necessidade de abrir a janela.
  - **Dica de contexto (tooltip):** exibe o papel selecionado, tempo decorrido e composição atual (Tanques, Healers e DPS).
- **Cancelamento e formação:** sair da fila ou ter o grupo formado (`GrupoDeFilaFormado`) encerra o estado ativo e remove automaticamente o indicador do HUD.

### Chat de texto (issue #95)

~~Tudo aqui é **proposta**; nada é decisão ainda.~~ Aprovado por Gabriel em 2026-10-16 (canais e limites). Base: chat do [World of Warcraft](../referencias.md) (canais com cor, sussurro por nome, comandos com barra). Igual: canal da área, canal do grupo, sussurro e abrir a caixa com Enter. Diferente: só três canais, sem canais criados por jogadores, sem canal de comércio e sem guilda por enquanto.

Quem decide: **servidor**. O client envia o texto e o canal; o servidor valida, define quem recebe, carimba a hora e repassa. O nome de quem falou vem da **sessão autenticada**, nunca de um campo do client.

#### Canais

| Canal | Quem recebe | Quem pode falar | Comando | Cor |
|---|---|---|---|---|
| **Cidade** | todos os personagens que estão **dentro dos muros** | quem está dentro dos muros | `/c texto` | branco |
| **Grupo** | todos os membros online do seu grupo, em qualquer área | quem está em um grupo | `/g texto` | azul |
| **Sussurro** | só o personagem indicado (e você vê a sua própria mensagem) | qualquer personagem online | `/s Nome texto` | rosa |
| **Sistema** | só você | ninguém (só leitura) | não tem | amarelo |

- **Cidade** vale **só dentro dos muros** (decidido em 2026-10-14, #500): o mundo tem 3 km e o canal não pode chegar a quem está na lava. Recebe e fala quem está na cidade com a posição dentro do quadrado de 40 m de cada lado do centro (`MapaDoMundo.MetadeDaCidade`, `AreasDoMundo.DentroDosMuros`); o servidor confere a posição de quem fala e de cada destinatário. Fora dos muros ou dentro da dungeon o canal não existe: falar nele devolve `CanalIndisponivel`. Não há canal "Local" por distância nesta versão: quem está fora dos muros usa Grupo e Sussurro (um canal por distância fica como evolução, se o grupo sentir falta). O histórico curto vai a quem entra na cidade, como antes.
- **Grupo** usa a lista do [grupo](#grupo-issue-94). Sem grupo: `NaoEstaEmGrupo`. Membro offline com a vaga guardada não recebe o que foi dito enquanto estava fora.
- **Sussurro** indica a pessoa pelo **nome do personagem**, sem diferenciar maiúsculas de minúsculas (mesma regra do convite). Nome inexistente ou personagem offline: `AlvoNaoEncontrado` (a mesma resposta nos dois casos). Para si mesmo: `SussurroParaSiMesmo`. Funciona entre áreas.
- **Sistema** não passa pelo servidor de chat: são as mensagens de sistema do HUD (issue #93, [interface.md](interface.md)) repetidas no histórico do chat do client.

#### Limites da mensagem

| Regra | Valor (proposta) | Recusa |
|---|---|---|
| Tamanho | 1 a **200** caracteres, depois de tirar os espaços do começo e do fim | `MensagemVazia`, `MensagemLongaDemais` |
| Quebras de linha e caracteres de controle | não são aceitos; o servidor troca por espaço antes de medir | — |
| Frequência | no máximo **5 mensagens em 10 s** por personagem, somando os canais | `MensagensRapidasDemais` |
| Repetição | a mesma mensagem (ignorando maiúsculas e espaços) no mesmo canal em menos de **30 s** | `MensagemRepetida` |
| Silêncio automático | estourar a frequência **3 vezes em 5 minutos** silencia o personagem por **60 s** em todos os canais | `Silenciado` (com os segundos que faltam) |

- O texto é **texto puro**. O servidor não interpreta formatação e o client mostra o texto com as marcas de formatação desligadas (rich text desativado no rótulo), para ninguém mudar cor, tamanho ou criar link falso com o que digita.
- Letras acentuadas, números e pontuação são aceitos (a restrição de A a Z vale só para o nome do personagem).
- Mensagem recusada **não é entregue a ninguém** e só quem enviou recebe o código.
- Mensagem aceita: o remetente recebe a própria mensagem de volta pelo servidor (é a confirmação), com a hora do servidor.

#### Histórico curto

- **Servidor:** guarda em memória as últimas **20** mensagens do canal Cidade, com no máximo **10 minutos**, e as envia a quem entra na cidade. Grupo e sussurro **não** têm histórico no servidor. Nada vai para o banco; reiniciar o servidor limpa tudo.
- **Client:** guarda as últimas **100** linhas (todos os canais) enquanto o jogo está aberto. Fechar o jogo apaga.

#### Moderação mínima

- O que existe na primeira versão: os limites da tabela acima (frequência, repetição e silêncio automático) e **ignorar jogador**.
- **Ignorar** (`/ignorar Nome`, `/designorar Nome`): a lista de até **50** nomes fica na conta e vem do servidor. O client esconde na apresentação as mensagens daquele nome em todos os canais; quem foi ignorado não fica sabendo.
- Ficam fora: filtro de palavrões, denúncia, moderadores e punição manual (itens em "Em aberto").
- O servidor **não escreve o texto das mensagens no log** (privacidade); registra só quem falou, em que canal e se foi recusada.

#### Na tela (client)

- Painel no canto inferior esquerdo, acima das barras do personagem. A área de leitura tem altura limitada e rola até as **100** linhas locais com roda, PageUp e PageDown. Mensagens novas levam ao fim se a pessoa já estava no fim; o botão **Ir ao fim** volta às recentes. Cada linha tem hora, canal e nome. Sem atividade por **10 s**, o painel fica quase transparente.
- No cabeçalho, o filtro **Todos, Cidade, Grupo, Sussurro ou Sistema** muda só o que se vê. **Ocultar** recolhe o painel para um botão discreto **Mostrar chat**; Enter também reabre a caixa. O histórico continua chegando com o painel oculto.
- As opções **Sistema** e **Combate** controlam separadamente a exibição dessas mensagens no chat e dos avisos comuns no HUD. A escolha e o estado oculto ficam neste computador. Avisos de erro continuam no HUD para não esconder uma falha que exige ação. Combate é identificado por categoria explícita ao publicar o aviso, sem inferência pelo texto ou pela cor. As mensagens ocultas continuam no histórico local e reaparecem se a opção for ligada.
- **Enter** abre a caixa de texto; **Enter** de novo envia; **Esc** fecha sem enviar. Com a caixa aberta, as ações do jogo não disparam (mesma regra do menu, [interface.md](interface.md)).
- A mensagem sem comando vai para o **último canal usado** (o primeiro é Cidade). `/r texto` responde ao último sussurro recebido.
- Comando desconhecido não é enviado ao servidor: aparece uma linha de Sistema explicando. **`/comandos`** lista chat e, no editor ou build de desenvolvimento, os [comandos de teste](desenvolvimento.md). **Tab** completa o verbo depois de `/`.
- ~~A tecla Enter para abrir o chat entra na tabela de teclas e atalhos quando o chat for implementado.~~ A tecla Enter já está na tabela de [teclas e atalhos](interface.md#teclas-e-atalhos) (client do chat, #240).
- No client, o histórico de 100 linhas vale enquanto o jogo está aberto; a lista de ignorados vem do servidor. A Social copia os avisos do HUD para o canal Sistema. Convites pendentes mostram **Aceitar (F1)** e **Recusar (F2)** no próprio aviso sobre o jogo (Y e N são atalhos das janelas de Conquistas e Talentos, #1198), além da janela do grupo; vale também para convite de troca e pedido de amizade recebido (a troca tem a vez). Os atalhos funcionam com o cursor travado e não disparam enquanto a pessoa digita no chat.

#### Balões de fala no mundo (Speech Bubbles, #1017)

- **Gatilho e Canais:** ao receber `MensagemDeChat` válida nos canais públicos (**Cidade** ou **Grupo**), o client projeta um balão de fala sobre o topo da cabeça do avatar (`Jogador_<id>` ou o próprio avatar local). Mensagens em **Sussurro** e linhas de **Sistema** são privadas e **nunca** exibem balão público no mundo.
- **Apresentação Lumora:** renderizado via UI Toolkit no painel overlay de mundo (`social-balao-camada`, classe `.social-balao-fala`), com fundo translúcido escuro, borda fina ouro e tipografia Marfim com quebra automática de linha.
- **Ancoragem e Visibilidade:** ancorado a 2,4 m acima da base dos pés do avatar (acima da etiqueta de nome). Só aparece se o ponto estiver à frente da câmera e a uma distância máxima de **25 metros** (`RegrasBaloesDeFala.DistanciaMaximaMetros`).
- **Duração e Transição:** duração proporcional de **4,0 s a 7,0 s** (`RegrasBaloesDeFala.CalcularDuracao`), com fade-out suave nos últimos **0,8 s** (`RegrasBaloesDeFala.TempoFadeOutSegundos`). Se o mesmo avatar enviar outra mensagem antes do término, o texto é atualizado, a opacidade reseta para 100% e o temporizador é renovado.

#### O que o servidor decide e valida

- Quem falou (pela sessão), a hora, o canal e quem recebe.
- Tamanho, frequência, repetição e silêncio; nada disso depende do client.
- Que o remetente pode usar o canal (está na cidade, está em um grupo, o alvo do sussurro existe e está online).
- A entrega é confiável e em ordem (`EntregaTempoReal.Confiavel`, veja [arquitetura](../arquitetura.md#servidor-tempo-real-udp)).

#### Erros que o servidor devolve (**proposta** de nomes)

| Código | Quando |
|---|---|
| `MensagemVazia` | texto vazio depois de tirar os espaços |
| `MensagemLongaDemais` | mais de 200 caracteres |
| `MensagensRapidasDemais` | mais de 5 mensagens em 10 s |
| `MensagemRepetida` | mesma mensagem no mesmo canal em menos de 30 s |
| `Silenciado` | personagem em silêncio automático |
| `CanalIndisponivel` | falar em Cidade fora dos muros (terreno em volta ou dungeon), ou canal que não existe |
| `NaoEstaEmGrupo` | falar em Grupo sem estar em grupo (mesmo código do grupo) |
| `AlvoNaoEncontrado` | sussurro para nome inexistente ou personagem offline (mesmo código do grupo) |
| `SussurroParaSiMesmo` | sussurro para o próprio personagem |

#### Testes esperados (servidor)

Caminho feliz: mensagem na Cidade chega a todos que estão na cidade e ao remetente; mensagem no Grupo chega só aos membros online, inclusive em outra área; sussurro chega só ao alvo e ao remetente; quem entra na cidade recebe o histórico curto.

Erros: um teste por código da tabela, conferindo que **ninguém recebe** a mensagem recusada.

Limites: 200 caracteres é aceito e 201 é recusado; só espaços é `MensagemVazia`; quebra de linha vira espaço; a 5ª mensagem em 10 s passa e a 6ª é recusada; passados os 10 s volta a aceitar; repetição com 29 s é recusada e com 30 s passa; a 3ª vez que estoura silencia por 60 s e depois libera; histórico da Cidade nunca passa de 20 nem entrega mensagem com mais de 10 minutos; nome do sussurro com maiúsculas diferentes encontra o mesmo personagem.

Concorrência: duas mensagens simultâneas do mesmo personagem contam as duas na frequência; sussurro enviado enquanto o alvo desconecta devolve `AlvoNaoEncontrado` ou entrega, nunca os dois; mensagem no Grupo enquanto um membro sai não chega a quem já saiu; o remetente nunca aparece com o nome de outro personagem, mesmo que o client envie um nome.

Client (EditMode): interpretar os comandos (`/c`, `/g`, `/s Nome texto`, `/r`, `/ignorar`); mensagem sem comando vai para o último canal; limite de 100 linhas; nome ignorado não aparece; texto com marcas de formatação é mostrado como texto.

### Amigos e ignorar (issue #180)

**Proposta**, a confirmar pelo grupo; os números são constantes de `RegrasAmigos` (shared). Nesta página estão as regras e o [contrato](#contrato-de-amigos-e-ignorar). O **servidor** de amigos e ignorados está implementado (#394); a **tela** no client segue em #395.

**Base:** Battle.net e World of Warcraft (lista de amigos da conta, ignorar). **Igual:** pedido e aceite, amigo de conta vê qualquer personagem seu online, ignorar corta sussurro e convite. **Diferente:** não há amizade só de personagem nem nota no amigo; quem foi ignorado não recebe aviso.

Quem decide: **servidor** (quem é amigo de quem, quem está online, o que o ignorar barra). O client envia intenções e mostra as listas.

#### Limites

| Regra | Valor (proposta) | Recusa |
|---|---|---|
| Amigos por conta, somando os pedidos enviados sem resposta | **100** | `ListaDeAmigosCheia` |
| Pedidos sem resposta que uma conta pode ter recebido | **50** | `AlvoComPedidosDemais` |
| Jogadores ignorados por conta | **50** | `ListaDeIgnoradosCheia` |
| Tempo mínimo entre dois pedidos da mesma conta | **1 s** | `PedidosRapidosDemais` |

#### Amizade

- A amizade é entre **contas**: pedir amizade a um personagem torna amigo o dono dele, e os dois veem qualquer personagem do outro online.
- **Pedir** é pelo nome de um personagem (sem diferenciar maiúsculas); o alvo não precisa estar online. O pedido não vence com o tempo e guarda uma vaga na lista de quem pediu.
- **Aceitar** cria a amizade nos dois lados; **recusar** apaga o pedido sem avisar quem pediu. Quem pediu pode **cancelar** enquanto não houver resposta.
- **Pedido cruzado:** pedir amizade a quem já tinha pedido a você vale como aceitar o pedido dele.
- **Remover** acaba com a amizade para os dois; o outro só recebe a lista nova, sem aviso.
- Cada linha da lista mostra o nome do personagem pelo qual a amizade foi feita, se a conta está online e, se estiver, com qual personagem e em qual **área** (Cidade, Campo ou Dungeon; #904).
- O servidor envia a lista inteira ao entrar no jogo e a cada mudança de amigos ou pedidos, para os dois lados (o alvo online vê o pedido na hora, #1200). Entrar, sair e trocar de personagem de um amigo chegam num aviso curto, só daquela linha.

#### Ignorar

- Vale para a **conta** e fica no **servidor** (segue o jogador em qualquer computador). Só o dono vê a própria lista.
- Ignorar alguém **desfaz a amizade** e apaga os pedidos entre os dois. Deixar de ignorar não traz a amizade de volta.
- Enquanto um dos dois ignora o outro, **nos dois sentidos**: sussurro não é entregue, convite de grupo não chega e pedido de amizade não é criado. Quem foi ignorado recebe `AlvoNaoEncontrado`, a mesma resposta de um nome que não existe, e não fica sabendo. Quem ignora e tenta pedir amizade recebe `AlvoIgnorado` (precisa tirar da lista antes).
- Mensagens do ignorado em **Cidade** e **Grupo** continuam sendo escondidas pelo client de quem ignora, agora a partir da lista do servidor.
- Com o servidor (#394), a lista de ignorados vem do servidor; o client ainda pode esconder mensagens em Cidade/Grupo localmente até #395 integrar a lista remota.

#### Recusas do pedido de amizade (ordem em que o servidor confere)

| # | Quando | Código |
|---|---|---|
| 1 | O alvo é da própria conta | `AmizadeConsigoMesmo` |
| 2 | Quem pede ignora o alvo | `AlvoIgnorado` |
| 3 | Já são amigos | `JaSaoAmigos` |
| 4 | Já existe pedido seu para o alvo | `PedidoJaPendente` |
| 5 | Menos de 1 s desde o último pedido | `PedidosRapidosDemais` |
| 6 | Amigos mais pedidos enviados já somam 100 | `ListaDeAmigosCheia` |
| 7 | O alvo ignora quem pede (ou o nome não existe) | `AlvoNaoEncontrado` |
| 8 | O alvo já tem 50 pedidos sem resposta | `AlvoComPedidosDemais` |

O pedido cruzado para em 6: depois disso é um aceite, e os itens 7 e 8 não se aplicam. O que depende só de quem pede (1 a 6) vem antes do que revelaria algo do alvo.

Outras recusas: `PedidoNaoEncontrado` (responder ou cancelar pedido que não existe ou não é seu), `ListaDeAmigosCheia` (aceitar com a própria lista cheia), `AmigoNaoEncontrado` (remover quem não é amigo), `IgnorarASiMesmo`, `JaIgnorado`, `ListaDeIgnoradosCheia` e `IgnoradoNaoEncontrado`.

#### Testes esperados

Regras puras (shared, feitos em #392): limites de 100, 50 e 50 nas bordas; vaga guardada por pedido enviado; cada linha da tabela de recusas e a ordem entre elas; pedido cruzado vira aceite; ignorado recebe `AlvoNaoEncontrado`; contato barrado nos dois sentidos.

Servidor (#394, implementado): pedido duplicado não cria outro; aceite cria a amizade nos dois lados; dois pedidos cruzados ao mesmo tempo viram uma amizade só; ignorado não consegue sussurrar, convidar nem pedir amizade; ignorar desfaz a amizade; o estado online muda ao entrar, sair e trocar de personagem; as listas persistem no banco e sobrevivem a reiniciar o servidor.

## Depende de

- Tempo real ([#77](https://github.com/GabrielNied/jogo_do_grupo/issues/77) e [arquitetura](../arquitetura.md#servidor-tempo-real-udp)): sessão autenticada por personagem e aviso de desconexão.
- Personagem ([personagem.md](personagem.md)): nome único do personagem.
- Dungeons ([instancias.md](instancias.md), #66): entrada em dungeon com o grupo (só ponteiro aqui).
- Mundo ([mundo.md](mundo.md)): presença na cidade, que define quem está no canal Cidade (por evento ou contrato do shared, sem referência direta entre funcionalidades). O Mundo também lê o grupo por `IConsultaDeGrupo` para mandar a posição dos aliados longe ao mapa e ao minimapa (#789, [área de interesse](mundo.md)); o Social só publica o grupo, não envia posição.
- Interface ([interface.md](interface.md)): mensagens de sistema do HUD e regra de teclas com painel aberto.

## Expõe

- Contrato do grupo na faixa 13000–13999 (mensagens de convidar, aceitar, recusar, sair, expulsar, passar liderança e a lista do grupo), ~~a definir pelo agente de contrato a partir das regras acima~~ definido em 2026-10-11, veja [Contrato do grupo](#contrato-do-grupo) abaixo.
- Contrato do chat na mesma faixa 13000–13999 (enviar mensagem, mensagem recebida, recusa e histórico da Cidade), ~~a definir pelo agente de contrato a partir das regras do chat~~ definido em 2026-10-11, veja [Contrato do chat](#contrato-do-chat) abaixo.
- Contrato de amigos e ignorados na mesma faixa (IDs 13200 a 13299), definido em 2026-10-11, veja [Contrato de amigos e ignorar](#contrato-de-amigos-e-ignorar) abaixo.
- Consulta atual do grupo de um personagem para outras funcionalidades do servidor por `IConsultaDeGrupo`, em `server/src/JogoDoGrupo.Server/Contratos/Social/`; personagens solo retornam `null`, e grupos incluem ID, líder e membros na ordem de entrada.
- Consulta de aliados para regras de combate por `IAliadosDeCombate` (`AliadosDeCombateDoSocial`, registrado em `AddSingleton<IAliadosDeCombate>`), em `server/src/JogoDoGrupo.Server/Contratos/Combate/`; personagens no mesmo grupo são aliados e não sofrem dano de área ou golpe aliado (#831).

### Contrato do grupo

Código em `shared/Package/Runtime/Features/Social/` (fonte dos nomes e tipos exatos). Tempo real; a identidade de quem pede vem sempre da sessão.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13000 | `ConvidarParaGrupo` | client → servidor | `Nome` |
| 13001 | `ResponderConvite` | client → servidor | `ConviteId`, `Aceitar` |
| 13002 | `SairDoGrupo` | client → servidor | nenhum |
| 13003 | `ExpulsarDoGrupo` | client → servidor | `PersonagemId` |
| 13004 | `PassarLideranca` | client → servidor | `PersonagemId` |
| 13010 | `ConviteRecebido` | servidor → convidado | `ConviteId`, `NomeDoConvidante`, `ValidadeSegundos` |
| 13011 | `ListaDoGrupo` | servidor → membros | `GrupoId`, `LiderId`, `Membros[]` (`MembroDoGrupo`: `PersonagemId`, `Nome`, `Online`, `Vida`, `VidaMaxima`, `Area`); lista vazia = sem grupo |
| 13012 | `AvisoDoGrupo` | servidor → client | `Tipo` (`TipoDeAvisoDoGrupo`), `PersonagemId`, `Nome` |
| 13013 | `OperacaoDoGrupoRecusada` | servidor → quem pediu | `Codigo` (`CodigosErroSocial`) |
| 13014 | `VidaDoMembroMudou` | servidor → membros | `PersonagemId`, `Vida`, `VidaMaxima` |
| 13015 | `AreaDoMembroMudou` | servidor → membros | `PersonagemId`, `Area` |

- `TipoDeAvisoDoGrupo`: os avisos da lista acima que não têm mensagem própria (`ConviteRecusado`, `ConviteExpirou`, `EntrouNoGrupo`, `SaiuDoGrupo`, `LiderMudou`, `ExpulsoDoGrupo`, `GrupoDesfeito`, `MembroOffline`, `MembroVoltou`). `ConviteRecebido` e `ListaDoGrupo` são mensagens.
- `CodigosErroSocial`: os 16 códigos da tabela de erros.
- `RegrasGrupo`: os números do grupo como constantes (5 membros, 60 s, 30 s, 1 convite por segundo, 5 convites por alvo) e as contas puras (`VagasLivres`, `ConviteExpirou`, `VagaGuardadaVenceu`, `ReconviteBloqueado`, `DeveDesfazer`, `PosicaoDoHerdeiro`).
- O grupo fica com os IDs 13000 a 13099; o chat começa em 13100 (#121).

### Client do grupo (issue #222)

Código em `client/Assets/_Game/Features/Social/`. O client só envia intenções e mostra o que o servidor manda: a lista, o líder e o offline vêm de `ListaDoGrupo`, na ordem dele.

- **Janela do grupo** (tecla **O**, **proposta**: é a sugestão "**O** social" de [interface.md](interface.md), que ainda espera aprovação): convidar por nome, convites recebidos com **Aceitar** e **Recusar**, a lista com o líder e os offline marcados, **Sair do grupo** e, só para o líder, **Expulsar** e **Passar liderança** (esta só para quem está online). Membro comum não vê o campo de convidar. Os botões só evitam pedidos que o servidor recusaria; quem decide é ele.
- **Convite recebido:** aparece uma linha por cima do jogo ("Fulano convidou você para o grupo. O: responder"); a resposta é dada na janela. A contagem de 60 s na tela é só para mostrar; vencido ou não, quem decide é o servidor.
- **Avisos e recusas** (`AvisoDoGrupo`, `OperacaoDoGrupoRecusada`) viram uma mensagem por 6 s, na janela e por cima do jogo.
- Trocar de personagem ou sair do mundo esquece o grupo na tela; o do próximo personagem chega pelo servidor.

#### HUD do grupo e sincronização de vida (#900)

- **Lista de membros no HUD:** na lateral esquerda (`RegiaoDoHud.LateralEsquerda`), exibe até 4 aliados (excluindo o jogador local). Sem grupo ou sozinho, o painel fica oculto.
- **Informações do membro:** nome, indicador se é líder (★), barra de vida proporcional com percentual e vida exata no formato `"{Vida}/{VidaMaxima}"`.
- **Vida crítica:** quando a vida estiver abaixo de 30% (`RegrasGrupo.LimiarVidaCritica`), a linha e a barra recebem destaque visual (`.vida-critica`).
- **Membro offline:** exibido com indicação visual (`.aliado-offline`, texto "(Offline)").
- **Sincronização em tempo real:** o servidor sincroniza alterações de vida (dano, cura, renascimento) através de `VidaDoMembroMudou` (ID 13014: `PersonagemId`, `Vida`, `VidaMaxima`).

#### Localização/área na lista ([#351](https://github.com/GabrielNied/jogo_do_grupo/issues/351))

- **Exibição no HUD:** quando o aliado estiver em área diferente da do jogador local, a linha é exibida no estado **longe** (`.aliado-longe`: opacidade 0.5, rótulo de status com `({Area})` e barra de vida oculta, conforme [HUD do jogo](interface.md#hud-do-jogo)). Aliados na mesma área exibem vida exata, percentual e barra completa.
- **Sincronização em tempo real:** o servidor propaga a área de cada membro em `ListaDoGrupo` (campo `Area` de `MembroDoGrupo`, resolvido via `RotulosDeArea.Descricao`) e atualiza mudanças de área/instância através de `AreaDoMembroMudou` (ID 13015: `PersonagemId`, `Area`).

### Contrato do chat

Código em `shared/Package/Runtime/Features/Social/`. Tempo real, entrega confiável; quem falou vem sempre da sessão.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13100 | `EnviarMensagemDeChat` | client → servidor | `Canal` (`CanalDeChat`: Cidade 0, Grupo 1, Sussurro 2), `Texto`, `NomeDoAlvo` (só no sussurro) |
| 13101 | `MensagemDeChat` | servidor → quem recebe e o remetente | `Canal`, `RemetenteId`, `NomeDoRemetente`, `NomeDoAlvo`, `Texto`, `HoraUnixMs` |
| 13102 | `MensagemDeChatRecusada` | servidor → remetente | `Codigo` (`CodigosErroSocial`), `SegundosRestantes` (só em `Silenciado`) |
| 13103 | `HistoricoDaCidade` | servidor → quem entra na cidade | `Mensagens[]` (`MensagemDeChat`, da mais antiga para a mais nova) |

- `CodigosErroSocial` ganhou os códigos do chat (`MensagemVazia`, `MensagemLongaDemais`, `MensagensRapidasDemais`, `MensagemRepetida`, `Silenciado`, `CanalIndisponivel`, `SussurroParaSiMesmo`); `NaoEstaEmGrupo` e `AlvoNaoEncontrado` são os mesmos do grupo.
- `RegrasChat`: os números da tabela de limites como constantes e as contas puras (`Limpar`, `Validar`, `ChaveDeRepeticao`, `FrequenciaEstourou`, `Repetida`, `DeveSilenciar`, `SilencioRestanteSegundos`, `VelhaDemaisParaOHistorico`). Os limites só do client (100 linhas e altura de leitura do painel) ficam no client.
- **Servidor do chat (2026-10-11, primeira fatia):** canais **Cidade** e **Sussurro** com os limites (tamanho, frequência, repetição e silêncio). O terceiro estouro de frequência já responde `Silenciado`. O canal **Grupo** responde `NaoEstaEmGrupo` para todos até o servidor do grupo existir, e o **histórico da Cidade** ainda não é enviado. Código em `server/src/JogoDoGrupo.Server/Features/Social/` (`ChatDoServidor`, `LimitesDoChat`).
- **Servidor do chat (2026-10-11, segunda fatia):** o **histórico da Cidade** (20 mensagens, 10 minutos) é enviado a quem entra na cidade, só quando há algo a mostrar, e o canal **Grupo** entrega aos membros online do grupo de quem falou (a lista vem do servidor do grupo). Com isso o trecho acima sobre o Grupo responder `NaoEstaEmGrupo` para todos e o histórico não ser enviado deixa de valer. Código: `HistoricoDaCidadeDoChat`.

### Contrato de amigos e ignorar

Código em `shared/Package/Runtime/Features/Social/`. Tempo real, entrega confiável; quem pede vem sempre da sessão, e o client nunca recebe o id da conta de outro jogador (`AmigoId`, `PedidoId` e `IgnoradoId` são gerados pelo servidor).

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13200 | `PedirAmizade` | client → servidor | `Nome` |
| 13201 | `ResponderPedidoDeAmizade` | client → servidor | `PedidoId`, `Aceitar` |
| 13202 | `CancelarPedidoDeAmizade` | client → servidor | `PedidoId` |
| 13203 | `RemoverAmigo` | client → servidor | `AmigoId` |
| 13204 | `IgnorarJogador` | client → servidor | `Nome` |
| 13205 | `DeixarDeIgnorar` | client → servidor | `IgnoradoId` |
| 13210 | `ListaDeAmigos` | servidor → client | `Amigos[]` (`AmigoDaLista`: `AmigoId`, `Nome`, `Online`, `PersonagemOnline`), `PedidosRecebidos[]` e `PedidosEnviados[]` (`PedidoDeAmizade`: `PedidoId`, `Nome`) |
| 13211 | `AmigoMudouDeEstado` | servidor → client | `AmigoId`, `Online`, `PersonagemOnline` |
| 13212 | `ListaDeIgnorados` | servidor → client | `Ignorados[]` (`JogadorIgnorado`: `IgnoradoId`, `Nome`) |
| 13213 | `OperacaoDeAmizadeRecusada` | servidor → quem pediu | `Codigo` (`CodigosErroSocial`) |

- `CodigosErroSocial` ganhou os 13 códigos de amigos e ignorados da seção de recusas; `AlvoNaoEncontrado` é o mesmo do grupo.
- `RegrasAmigos`: os números da tabela de limites como constantes e as decisões puras (`VagasLivres`, `TemVaga`, `AlvoComPedidosDemais`, `CabeMaisUmIgnorado`, `PedidoRapidoDemais`, `PedidoViraAceite`, `RecusaDoPedido`, `RecusaDoAceite`, `RecusaDeIgnorar`, `ContatoBarrado`). `RelacaoDeAmizade` é a entrada de `RecusaDoPedido`.
- Amigos e ignorados ficam com os IDs 13200 a 13299.

### Contrato da fila de dungeon (LFG / Procurar Grupo) (issue #209)

Código em `shared/Package/Runtime/Features/Social/`. Tempo real, entrega confiável.

- **Composição de grupo:** cada grupo de 5 para dungeon é formado estritamente por **1 Tanque, 1 Healer e 3 DPS** (`RegrasFilaGrupo`).
- **Escolha livre de papel:** o jogador seleciona seu papel (`PapelNoGrupo`: `Tank = 1`, `Healer = 2`, `Dps = 3`) ao entrar na fila; o servidor não valida equipamento (o grupo resolve papéis errados).
- **Canal de chat Procurar Grupo:** `CanalDeChat.ProcurarGrupo = 3` (comandos `/pg`, `/lfg`, `/procurargrupo`), broadcast global no servidor para buscar membros e grupos.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13300 | `EntrarNaFilaDeGrupo` | client → servidor | `Papel` (`PapelNoGrupo`: Tank 1, Healer 2, Dps 3) |
| 13301 | `SairDaFilaDeGrupo` | client → servidor | (vazio) |
| 13310 | `StatusDaFilaDeGrupo` | servidor → client | `EmFila`, `Papel`, `Tanques` (0 a 1), `Healers` (0 a 1), `Dps` (0 a 3), `SegundosNaFila` |
| 13311 | `GrupoDeFilaFormado` | servidor → membros | `GrupoId`, `SeuPapel`, `Mensagem` |

- Ao atingir a composição completa (1 Tanque, 1 Healer, 3 DPS), o servidor cria o grupo automaticamente através de `GruposDoServidor.FormarGrupoDeFila`, remove os 5 jogadores da fila, envia `GrupoDeFilaFormado` e dispara a `ListaDoGrupo` completa sincronizada a todos os membros.
- Fila de dungeon fica com os IDs 13300 a 13319.
### Marcadores táticos de grupo (Target Markers - issue #1020)

Marcadores visuais colocados sobre alvos (mobs, chefes ou jogadores) visíveis para todos os membros do grupo.

- **Ícones disponíveis (`TipoMarcadorDeAlvo`):**
  - `Nenhum` (0): remove o marcador.
  - `Caveira` (1): foco primário (dano prioritário).
  - `Cruz` (2): foco secundário.
  - `Escudo` (3): alvo do tank.
  - `Lua` (4): controle de grupo (CC / atordoamento / silêncio).
  - `Estrela` (5): alvo especial / utilitário.
- **Autoridade e exclusividade:** gerenciado exclusivamente pelo servidor (`ControleDeMarcadoresDeGrupo`). Apenas o líder do grupo pode definir marcadores. Cada marcador só pode estar em um alvo por vez no grupo (ao marcar outro alvo com o mesmo ícone, o anterior perde o marcador).
- **Sincronização em tempo real:**
  - ID 13320 `DefinirMarcadorDeAlvo` (client → servidor): `AlvoId`, `Marcador`.
  - ID 13321 `MarcadoresDeAlvoMudaram` (servidor → grupo): `Marcadores[]` (`MarcadorAlvoDto`).
  - ID 13322 `DefinirMarcadorRecusado` (servidor → remetente): `Motivo` (`CodigoErroMarcador`).
- **Comandos no chat:** `/marcar <caveira|x|escudo|lua|estrela> [id]` e `/desmarcar [id]`. Se o ID não for informado, busca automaticamente o alvo mais próximo do jogador.
- **Apresentação visual:** `ApresentadorDeMarcadoresTaticos` projeta o ícone no mundo 3D acima da cabeça do alvo com limite de distância e estilos do padrão Lumora.
### Inspeção de jogador (issue #1016)

Permite visualizar equipamentos equipados, tiers, raridades e atributos de outros jogadores presentes no mundo (na mesma área) ou membros do mesmo grupo.

#### Regras de inspeção

- **Quem pode ser inspecionado:** jogadores online presentes na mesma área que o solicitante ou membros do mesmo grupo (mesmo que estejam distantes ou em outra área). Inspecionar a si mesmo sempre é permitido.
- **Validações no servidor:**
  - Alvo inexistente ou nome não encontrado: recusa com `CodigoErroInspecao.AlvoNaoEncontrado`.
  - Alvo offline: recusa com `CodigoErroInspecao.AlvoOffline`.
  - Alvo em outra área e fora do grupo: recusa com `CodigoErroInspecao.AlvoForaDoAlcance`.
- **Acesso no client:**
  - Pelo quadro de grupo na janela Social (botão "Inspecionar" ao lado de cada membro).
  - Pelo clique no retrato do aliado no HUD lateral esquerdo.
  - Pelo comando de barra no chat: `/inspecionar <nome>` ou `/inspect <nome>`.
- **Janela de inspeção (`JanelaInspecao`):** interface modal somente leitura (estilo Lumora) exibindo o conjunto ativo, peças de equipamento equipadas com seus tiers (S+, A, etc.), raridade, nível, atributos base do personagem e atributos de combate acumulados pelo equipamento. Nenhuma ação de modificação ou transferência de item é permitida.

#### Contrato de inspeção

Código em `shared/Package/Runtime/Features/Social/`. Tempo real, entrega confiável.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13300 | `PedirInspecao` | client → servidor | `PersonagemId` (opcional se nome informado), `Nome` |
| 13310 | `RetratoInspecaoResponse` | servidor → client | `PersonagemId`, `Nome`, `Raca`, `Nivel`, `ConjuntoAtivo`, `Equipados[]` (`EntradaInventario`), `Atributos` (`AtributosDosEquipamentos`), `Forca`, `Vigor`, `Agilidade`, `Intelecto`, `Espirito` |
| 13311 | `InspecaoRecusada` | servidor → quem pediu | `PersonagemId`, `Motivo` (`CodigoErroInspecao`: `AlvoNaoEncontrado` 1, `AlvoOffline` 2, `AlvoForaDoAlcance` 3) |

### Troca direta (issue #175)

Itens e coroas entre dois jogadores, com os dois lados vendo a oferta antes de aceitar ([economia](economia.md#definido)). Feito em 2026-10-18; os números são constantes em `RegrasTroca` (shared).

#### Regras

- **Quem pode:** os dois online, **na mesma área, a até 10 m** (distância em 3D, posição que o servidor aceitou), **fora de combate**, e nenhum ignora o outro (quem ignora recebe "não encontrado"). Cada jogador tem **uma troca por vez**.
- **Convite:** `/trocar <nome>` (ou `/trade`). O convidado aceita ou recusa na tela (Y/N); o convite vale **30 s** e não prende ninguém se vencer.
- **Oferta:** cada lado põe **coroas** e **até 8 itens** da bolsa. Só entra o que a bolsa negocia: **equipado e favorito não entram** (o mesmo critério de vender ao NPC). O servidor confere saldo e itens a cada mudança.
- **Dupla confirmação:** cada lado aceita a oferta que vê. **Qualquer mudança de oferta, de qualquer lado, desfaz os dois aceites** (versão da oferta: um aceite de uma versão velha é ignorado). Quando os dois aceitaram, o servidor confere distância e combate de novo e troca tudo de uma vez.
- **Atomicidade:** uma transação só. Debita os dois lados antes de creditar, tira os itens dos dois e só depois entrega cruzado (então uma troca de um item por outro funciona com a bolsa cheia). Qualquer falha desfaz tudo e a troca **reabre sem aceites**, com o motivo (`SaldoInsuficiente`, `ItemIndisponivel`, `BolsaCheia`). O exemplar atravessa inteiro, com os mesmos atributos.
- **Cancelamento sem perda:** cancelar, fechar a janela, desconectar, afastar-se além de 10 m ou entrar em combate antes do último aceite cancelam a troca. Nada muda de dono antes do commit, então não há o que devolver.
- **Decisão de design:** o servidor guarda a troca só na memória (cai com o servidor, sem perda pelo mesmo motivo); a troca de dono do item é "retirar e devolver" na mesma transação (`ICustodiaDeItens`, de Itens), o molde do banco pessoal. Isso é um destino novo para o item além dos de [ITEM-002](itens.md#modelo-de-itens-v2-documento-mestre-2026-10-12): a troca entre jogadores já estava **Definida** em economia.md (2026-10-16), e o exemplar nunca duplica.

#### Contrato da troca

Código em `shared/Package/Runtime/Features/Social/` (`MensagensDeTroca.cs`, `RegrasTroca.cs`). Tempo real, entrega confiável.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13400 | `PedirTroca` | client → servidor | `Nome` |
| 13401 | `ResponderConviteDeTroca` | client → servidor | `TrocaId`, `Aceitar` |
| 13402 | `AtualizarOfertaDeTroca` | client → servidor | `TrocaId`, `Coroas`, `InstanciaIds[]` (substitui a oferta) |
| 13403 | `ConfirmarTroca` | client → servidor | `TrocaId`, `VersaoDaOferta` |
| 13404 | `CancelarTroca` | client → servidor | `TrocaId` (0 = o convite que enviei) |
| 13405 | `ConsultarBolsaParaTroca` | client → servidor | `TrocaId` |
| 13410 | `ConviteDeTroca` | servidor → convidado | `TrocaId`, `NomeDe`, `ValidadeSegundos` |
| 13411 | `EstadoDaTroca` | servidor → os dois | `TrocaId`, `NomeDoOutro`, `VersaoDaOferta`, `Minha`/`Dele` (`Coroas`, `Itens[]`), `MeuAceite`, `AceiteDele` |
| 13412 | `BolsaParaTroca` | servidor → jogador | `TrocaId`, `Coroas` (saldo), `Itens[]` negociáveis |
| 13413 | `TrocaEncerrada` | servidor → os dois | `TrocaId`, `Motivo` (`Concluida`, `CanceladaPorVoce`, `CanceladaPeloOutro`, `ConviteRecusado`, `Desconectou`, `LongeDemais`, `EmCombate`...), `NomeDoOutro` |
| 13414 | `TrocaRecusada` | servidor → quem pediu | `TrocaId`, `Codigo` (`CodigosErroTroca`) |

Depois de uma troca concluída, Social publica `Contratos/Economia/CoroasMovimentadas` para cada lado e Economia reenvia o saldo (`SaldoCoroasAlterado`).

#### Na tela (client)

`JanelaTroca` (abre sozinha quando a troca começa; fechar cancela): "Você oferece" com campo de coroas e a lista da oferta (clicar tira), "<nome> oferece" só leitura, a bolsa negociável (clicar oferece) e os botões Aceitar e Cancelar com a situação dos aceites. O convite aparece por cima do jogo (Y/N).

#### Testes (servidor)

Regras sem banco (`TrocaDiretaDoServidorTestes`): mudança de oferta desfaz os aceites, aceite de versão velha ignorado, distância e combate no pedido e no último aceite, desconectar cancela sem executar, convite vencido. Banco real (`ExecutorDaTrocaTestes`): troca completa, falha no meio desfaz tudo (saldo, item sumido, bolsa cheia), concorrência (mesmo item ou mesmas coroas em várias trocas nunca duplicam).

### Moderação e ferramentas de GM (issue #187)

Ferramentas administrativas autoritativas no servidor para moderação e auditoria, além de recepção e persistência de denúncias de jogadores.

#### Regras de moderação
- **Autoridade:** permissão de Game Master fica no banco na entidade `Conta.EhGm`. Contas sem `EhGm = true` têm qualquer comando de GM rejeitado com aviso estruturado de segurança no log.
- **Auditoria estruturada:** cada comando de GM executado gera registro persistente na tabela `AuditoriaGm` (`RegistroAuditoriaGm`) e log estruturado com prefixo `[GM-AUDIT]` contendo a conta do GM, personagem, ação, alvo e detalhes.
- **Comandos de GM no chat:**
  - `/tp <x y z>` ou `/tp <jogador>`: teleporte autoritativo.
  - `/puxar <jogador>`: puxa o jogador alvo até as coordenadas do GM.
  - `/silenciar <jogador> [minutos]`: silencia temporariamente o jogador no chat via `ISilenciadorDoChat` (padrão 10 minutos).
  - `/expulsar <jogador> [motivo]`: desconecta imediatamente o personagem da sessão de tempo real.
  - `/banir <jogador> [dias] [motivo]`: aplica banimento temporário na `Conta.BanidaAte` do jogador e encerra sua conexão de tempo real.
- **Denúncia de jogadores:**
  - Comando `/denunciar <jogador> <motivo> [detalhes]`.
  - Motivos válidos: `ofensa`, `trapaca`, `spam`, `comportamento`, `outro`.
  - Denúncias são persistidas na tabela `Denuncias` com autor, alvo, motivo, detalhes e data UTC para posterior análise da moderação.
- **Bloqueio de contas banidas:** contas com `BanidaAte > DateTime.UtcNow` têm tentativa de login HTTP recusada com 403 Forbidden (`CodigosErro.ContaBanida`) e autenticação UDP no tempo real recusada.

### Guildas (issue #181)

Sistema autoritativo de guildas para grupos permanentes de jogadores, com criação, convites, cargos hierárquicos e canal de chat dedicado.

#### Regras de negócio
- **Fundação:** custo de 1.000 coroas debitado autoritativamente via `ICarteiraCoroas`. O criador se torna o `Lider` da guilda.
- **Nome da guilda:** entre 3 e 24 caracteres alfanuméricos e espaços internos (sem espaços nas pontas nem múltiplos seguidos). Único no servidor (comparação sem diferenciar maiúsculas/minúsculas).
- **Limite de membros:** até 100 membros simultâneos por guilda na v1 (`RegrasGuilda.MaximoDeMembros`).
- **Cargos e permissões:**
  - `Lider` (1): controle total, pode convidar, expulsar qualquer membro, promover/rebaixar até Oficial, passar a liderança e dissolver a guilda se for o único membro. Não pode sair enquanto houver outros membros sem passar a liderança antes.
  - `Oficial` (2): pode convidar novos membros, expulsar Membros e Recrutas, e alterar cargos entre Membro e Recruta.
  - `Veterano` (3): membro sênior de confiança.
  - `Membro` (4): membro efetivo com acesso às interações da guilda.
  - `Recruta` (5): cargo inicial para quem aceita um convite.
- **Canal de Chat da Guilda (`CanalDeChat.Guilda` = 4):**
  - Mensagens transmitidas em tempo real apenas para membros online da mesma guilda e para o remetente.
  - Jogadores sem guilda têm mensagens no canal recusadas com `CodigosErroSocial.NaoEstaEmGuilda`.
- **Interface Lumora (`JanelaGuilda` - atalho 'G'):**
  - Sem guilda: exibe convite pendente (se houver, com Aceitar/Recusar) e formulário para fundação de nova guilda (com custo de 1.000 coroas).
  - Com guilda: cabeçalho com nome, contagem de membros, cargo atual, linha para convidar (se tiver permissão), lista de membros com status online e botões contextuais (Promover, Rebaixar, Expulsar, Liderar) e botão Sair da Guilda.

