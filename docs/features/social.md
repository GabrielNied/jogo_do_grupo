# Social

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 13000–13999

## Resumo

Party (time, chamado de **grupo** nas regras), guilda e chat.

Esta página detalha o **grupo** (convite, lista, líder, sair e expulsar; issue #94). ~~Chat (#95),~~ guilda e divisão de loot ficam para outras tarefas. O chat tem regras em proposta em [Chat de texto](#chat-de-texto-issue-95) (acrescentado em 2026-10-11).

## Definido

- Party: tamanho e divisão de loot
- Party de até 5 jogadores (o tamanho de uma dungeon); raids juntam 10 (decidido em 2026-10-10)
- Guilda: níveis, base própria, benefícios

## Em aberto

- [ ] Tamanho máximo de party e guilda? Party decidida em 2026-10-10: 5 (veja "Definido"); ~~party~~ sai desta pergunta. Falta o tamanho da guilda.
- [ ] Regras de divisão de loot
- [ ] **Proposta do livro de Asterra (2026-10-11), não validada pelo grupo** ([figjam.md](../brainstorm/figjam.md), seção 10, "Grupo, disputa de loot e PvP futuro"):
  - **Tamanho:** instância de 20 jogadores e grupo que pode reunir os 20, com vida e localização dos aliados e baú compartilhado conforme permissão. **Conflito:** hoje está definido party de 5 e raid de 10 (2026-10-10); a regra não muda, a pergunta fica aberta (veja a pergunta de raid abaixo e [instancias.md](instancias.md)).
  - **Loot por dano:** o dano ao mob define quem recebe os itens **individualmente**, mesmo dentro do grupo; o primeiro golpe no recurso garante a coleta. Empate favorece o primeiro atacante; a contagem zera se o mob voltar e recuperar vida; só conta dano real, sem excedente após a morte. Cura e tanque sem prioridade automática; a participação por apoio e defesa pode dar XP, sem mudar o dono dos itens; amigos compensam por troca. **Conflito:** a [fatia](fatia-jogavel.md#5-loot-no-chão) decidiu em 2026-10-11 loot individual para quem participou, não por maior dano; a decisão vale e o livro fica registrado aqui.
  - **Aliados:** skills não causam dano entre membros do grupo.
- [ ] Líder: quem herda quando o líder sai ou cai? (proposta: o membro mais antigo no grupo) e o líder pode passar a liderança? (proposta: sim)
- [ ] Só o líder expulsa, ou qualquer membro pode pedir votação? (proposta: só o líder expulsa, sem votação)
- [ ] Tempo que a vaga fica guardada após desconexão (proposta: 120 s) e tempo de validade do convite (proposta: 60 s)
- [ ] Convidar por nome exige amizade ou proximidade? (proposta: não, basta o personagem estar online)
- [ ] Bloquear convites (modo "não aceitar grupo") e fila automática de grupo: ficam fora da primeira versão?
- [ ] Raid de 10: é um grupo maior ou dois grupos de 5 juntos? (veja [instancias.md](instancias.md))
- [ ] O grupo desfaz ao ficar com 1 pessoa? (proposta: sim)
- [ ] Chat (#95), decisões do grupo:
  - [ ] Aprovar os três canais (Cidade, Grupo, Sussurro) e os números da tabela de limites (proposta)
  - [ ] O canal **Cidade** vale só para quem está na cidade (proposta) ou é um canal do servidor inteiro, que também chega dentro da dungeon?
  - [ ] Filtro de palavrões: fica fora da primeira versão (proposta) ou entra uma lista simples?
  - [ ] **Ignorar jogador** gravado neste computador (proposta) ou na conta, pelo servidor?
  - [ ] O servidor guarda as mensagens em algum lugar para moderação? (proposta: não guarda; só o histórico curto da Cidade em memória)
  - [ ] Denunciar jogador e silenciar por um moderador: ficam para depois?
  - [ ] Sussurro para quem está em outra área (ex.: você na cidade e a pessoa na dungeon): pode? (proposta: sim)

## Regras

### Grupo (issue #94)

Base: grupos do [World of Warcraft](../referencias.md) (convite por nome, líder, lista, sair e expulsar). Igual: grupo de até 5 com um líder, convite por nome que a outra pessoa aceita ou recusa. Diferente: não há classes nem papéis fixos (o papel vem dos itens), e o mundo é persistente e compartilhado (D1), então o grupo existe no mundo e não só dentro de uma dungeon.

Tudo marcado **proposta** é valor inicial para o grupo calibrar; não é decisão. O que está em "Definido" acima (grupo de até 5) é decisão.

Quem decide: **servidor**. O client só envia intenções (convidar, aceitar, recusar, sair, expulsar, passar liderança) e mostra a lista que o servidor manda.

#### Estado e limites

- Um personagem está em **no máximo um grupo** por vez. Quem não está em grupo está "sozinho" (não existe grupo de 1 guardado: o grupo nasce quando o primeiro convite é aceito, com 2 membros).
- Tamanho máximo: **5 membros**, contando o líder (decidido em 2026-10-10).
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

- O membro que desconecta **fica na lista marcado como offline** e a **vaga fica guardada por 120 segundos**.
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
- Reconexão antes de 120 s devolve o grupo ao membro.

Cada erro da tabela: um teste por código, conferindo que **nada muda** no estado do grupo quando o pedido é recusado.

Limites:
- Exatamente 5 membros é aceito; o 6º convite é recusado com `GrupoCheio`.
- Convite com 59 s é aceito; com 60 s ou mais, `ConviteExpirado`.
- Vaga guardada: voltar com 119 s mantém a vaga; passar de 120 s remove.
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
- **Líder offline:** a vaga guardada e a troca de liderança vencem juntas, passados os 120 s; ali ele sai do grupo como se tivesse saído.
- **Reconvite bloqueado** vale para quem expulsou e para o grupo, e continua valendo se a expulsão desfez o grupo.

### Chat de texto (issue #95)

Tudo aqui é **proposta**; nada é decisão ainda. Base: chat do [World of Warcraft](../referencias.md) (canais com cor, sussurro por nome, comandos com barra). Igual: canal da área, canal do grupo, sussurro e abrir a caixa com Enter. Diferente: só três canais, sem canais criados por jogadores, sem canal de comércio e sem guilda por enquanto.

Quem decide: **servidor**. O client envia o texto e o canal; o servidor valida, define quem recebe, carimba a hora e repassa. O nome de quem falou vem da **sessão autenticada**, nunca de um campo do client.

#### Canais

| Canal | Quem recebe | Quem pode falar | Comando | Cor |
|---|---|---|---|---|
| **Cidade** | todos os personagens que estão na cidade | quem está na cidade | `/c texto` | branco |
| **Grupo** | todos os membros online do seu grupo, em qualquer área | quem está em um grupo | `/g texto` | azul |
| **Sussurro** | só o personagem indicado (e você vê a sua própria mensagem) | qualquer personagem online | `/s Nome texto` | rosa |
| **Sistema** | só você | ninguém (só leitura) | não tem | amarelo |

- **Cidade** segue a presença na cidade ([mundo.md](mundo.md)): entra quem recebeu `EstadoDaCidade` e sai quem saiu da cidade. Dentro da dungeon o canal não existe: falar nele devolve `CanalIndisponivel`.
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
- **Ignorar** (`/ignorar Nome`, `/designorar Nome`): o client esconde as mensagens daquele nome em todos os canais. A lista fica **neste computador** (até **50** nomes), junto com as opções. O servidor não participa; quem foi ignorado não fica sabendo.
- Ficam fora: filtro de palavrões, denúncia, moderadores e punição manual (itens em "Em aberto").
- O servidor **não escreve o texto das mensagens no log** (privacidade); registra só quem falou, em que canal e se foi recusada.

#### Na tela (client)

- Painel no canto inferior esquerdo, acima das barras do personagem, mostrando as últimas **8** linhas; cada linha com a hora, o canal e o nome. Sem atividade por **10 s**, o painel fica quase transparente.
- **Enter** abre a caixa de texto; **Enter** de novo envia; **Esc** fecha sem enviar. Com a caixa aberta, as ações do jogo não disparam (mesma regra do menu, [interface.md](interface.md)).
- A mensagem sem comando vai para o **último canal usado** (o primeiro é Cidade). `/r texto` responde ao último sussurro recebido.
- Comando desconhecido não é enviado ao servidor: aparece uma linha de Sistema explicando.
- ~~A tecla Enter para abrir o chat entra na tabela de teclas e atalhos quando o chat for implementado.~~ A tecla Enter já está na tabela de [teclas e atalhos](interface.md#teclas-e-atalhos) (client do chat, #240).
- No client, o painel fica na região inferior esquerda do HUD; o histórico de 100 linhas e a lista de ignorados (PlayerPrefs, neste computador) valem enquanto o jogo está aberto. Lacunas do Core: os atalhos de janela (letras) e o Esc ainda disparam com a caixa aberta, e as mensagens de sistema do HUD ainda não são repetidas no chat (pendências do `client-core`).

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
| `CanalIndisponivel` | falar em Cidade fora da cidade, ou canal que não existe |
| `NaoEstaEmGrupo` | falar em Grupo sem estar em grupo (mesmo código do grupo) |
| `AlvoNaoEncontrado` | sussurro para nome inexistente ou personagem offline (mesmo código do grupo) |
| `SussurroParaSiMesmo` | sussurro para o próprio personagem |

#### Testes esperados (servidor)

Caminho feliz: mensagem na Cidade chega a todos que estão na cidade e ao remetente; mensagem no Grupo chega só aos membros online, inclusive em outra área; sussurro chega só ao alvo e ao remetente; quem entra na cidade recebe o histórico curto.

Erros: um teste por código da tabela, conferindo que **ninguém recebe** a mensagem recusada.

Limites: 200 caracteres é aceito e 201 é recusado; só espaços é `MensagemVazia`; quebra de linha vira espaço; a 5ª mensagem em 10 s passa e a 6ª é recusada; passados os 10 s volta a aceitar; repetição com 29 s é recusada e com 30 s passa; a 3ª vez que estoura silencia por 60 s e depois libera; histórico da Cidade nunca passa de 20 nem entrega mensagem com mais de 10 minutos; nome do sussurro com maiúsculas diferentes encontra o mesmo personagem.

Concorrência: duas mensagens simultâneas do mesmo personagem contam as duas na frequência; sussurro enviado enquanto o alvo desconecta devolve `AlvoNaoEncontrado` ou entrega, nunca os dois; mensagem no Grupo enquanto um membro sai não chega a quem já saiu; o remetente nunca aparece com o nome de outro personagem, mesmo que o client envie um nome.

Client (EditMode): interpretar os comandos (`/c`, `/g`, `/s Nome texto`, `/r`, `/ignorar`); mensagem sem comando vai para o último canal; limite de 100 linhas; nome ignorado não aparece; texto com marcas de formatação é mostrado como texto.

## Depende de

- Tempo real ([#77](https://github.com/GabrielNied/jogo_do_grupo/issues/77) e [arquitetura](../arquitetura.md#servidor-tempo-real-udp)): sessão autenticada por personagem e aviso de desconexão.
- Personagem ([personagem.md](personagem.md)): nome único do personagem.
- Dungeons ([instancias.md](instancias.md), #66): entrada em dungeon com o grupo (só ponteiro aqui).
- Mundo ([mundo.md](mundo.md)): presença na cidade, que define quem está no canal Cidade (por evento ou contrato do shared, sem referência direta entre funcionalidades).
- Interface ([interface.md](interface.md)): mensagens de sistema do HUD e regra de teclas com painel aberto.

## Expõe

- Contrato do grupo na faixa 13000–13999 (mensagens de convidar, aceitar, recusar, sair, expulsar, passar liderança e a lista do grupo), ~~a definir pelo agente de contrato a partir das regras acima~~ definido em 2026-10-11, veja [Contrato do grupo](#contrato-do-grupo) abaixo.
- Contrato do chat na mesma faixa 13000–13999 (enviar mensagem, mensagem recebida, recusa e histórico da Cidade), ~~a definir pelo agente de contrato a partir das regras do chat~~ definido em 2026-10-11, veja [Contrato do chat](#contrato-do-chat) abaixo.
- Consulta "quem é o grupo de um personagem" para outras funcionalidades (ex.: dungeons), por evento ou contrato do shared, sem referência direta entre funcionalidades.

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
| 13011 | `ListaDoGrupo` | servidor → membros | `GrupoId`, `LiderId`, `Membros[]` (`MembroDoGrupo`: `PersonagemId`, `Nome`, `Online`); lista vazia = sem grupo |
| 13012 | `AvisoDoGrupo` | servidor → client | `Tipo` (`TipoDeAvisoDoGrupo`), `PersonagemId`, `Nome` |
| 13013 | `OperacaoDoGrupoRecusada` | servidor → quem pediu | `Codigo` (`CodigosErroSocial`) |

- `TipoDeAvisoDoGrupo`: os avisos da lista acima que não têm mensagem própria (`ConviteRecusado`, `ConviteExpirou`, `EntrouNoGrupo`, `SaiuDoGrupo`, `LiderMudou`, `ExpulsoDoGrupo`, `GrupoDesfeito`, `MembroOffline`, `MembroVoltou`). `ConviteRecebido` e `ListaDoGrupo` são mensagens.
- `CodigosErroSocial`: os 16 códigos da tabela de erros.
- `RegrasGrupo`: os números do grupo como constantes (5 membros, 60 s, 120 s, 30 s, 1 convite por segundo, 5 convites por alvo) e as contas puras (`VagasLivres`, `ConviteExpirou`, `VagaGuardadaVenceu`, `ReconviteBloqueado`, `DeveDesfazer`, `PosicaoDoHerdeiro`).
- O grupo fica com os IDs 13000 a 13099; o chat começa em 13100 (#121).

### Client do grupo (issue #222)

Código em `client/Assets/_Game/Features/Social/`. O client só envia intenções e mostra o que o servidor manda: a lista, o líder e o offline vêm de `ListaDoGrupo`, na ordem dele.

- **Janela do grupo** (tecla **O**, **proposta**: é a sugestão "**O** social" de [interface.md](interface.md), que ainda espera aprovação): convidar por nome, convites recebidos com **Aceitar** e **Recusar**, a lista com o líder e os offline marcados, **Sair do grupo** e, só para o líder, **Expulsar** e **Passar liderança** (esta só para quem está online). Membro comum não vê o campo de convidar. Os botões só evitam pedidos que o servidor recusaria; quem decide é ele.
- **Convite recebido:** aparece uma linha por cima do jogo ("Fulano convidou você para o grupo. O: responder"); a resposta é dada na janela. A contagem de 60 s na tela é só para mostrar; vencido ou não, quem decide é o servidor.
- **Avisos e recusas** (`AvisoDoGrupo`, `OperacaoDoGrupoRecusada`) viram uma mensagem por 6 s, na janela e por cima do jogo.
- Trocar de personagem ou sair do mundo esquece o grupo na tela; o do próximo personagem chega pelo servidor.

### Contrato do chat

Código em `shared/Package/Runtime/Features/Social/`. Tempo real, entrega confiável; quem falou vem sempre da sessão.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 13100 | `EnviarMensagemDeChat` | client → servidor | `Canal` (`CanalDeChat`: Cidade 0, Grupo 1, Sussurro 2), `Texto`, `NomeDoAlvo` (só no sussurro) |
| 13101 | `MensagemDeChat` | servidor → quem recebe e o remetente | `Canal`, `RemetenteId`, `NomeDoRemetente`, `NomeDoAlvo`, `Texto`, `HoraUnixMs` |
| 13102 | `MensagemDeChatRecusada` | servidor → remetente | `Codigo` (`CodigosErroSocial`), `SegundosRestantes` (só em `Silenciado`) |
| 13103 | `HistoricoDaCidade` | servidor → quem entra na cidade | `Mensagens[]` (`MensagemDeChat`, da mais antiga para a mais nova) |

- `CodigosErroSocial` ganhou os códigos do chat (`MensagemVazia`, `MensagemLongaDemais`, `MensagensRapidasDemais`, `MensagemRepetida`, `Silenciado`, `CanalIndisponivel`, `SussurroParaSiMesmo`); `NaoEstaEmGrupo` e `AlvoNaoEncontrado` são os mesmos do grupo.
- `RegrasChat`: os números da tabela de limites como constantes e as contas puras (`Limpar`, `Validar`, `ChaveDeRepeticao`, `FrequenciaEstourou`, `Repetida`, `DeveSilenciar`, `SilencioRestanteSegundos`, `VelhaDemaisParaOHistorico`). Os limites só do client (100 linhas, 50 ignorados, 8 linhas no painel) ficam no client.
- **Servidor do chat (2026-10-11, primeira fatia):** canais **Cidade** e **Sussurro** com os limites (tamanho, frequência, repetição e silêncio). O terceiro estouro de frequência já responde `Silenciado`. O canal **Grupo** responde `NaoEstaEmGrupo` para todos até o servidor do grupo existir, e o **histórico da Cidade** ainda não é enviado. Código em `server/src/JogoDoGrupo.Server/Features/Social/` (`ChatDoServidor`, `LimitesDoChat`).
- **Servidor do chat (2026-10-11, segunda fatia):** o **histórico da Cidade** (20 mensagens, 10 minutos) é enviado a quem entra na cidade, só quando há algo a mostrar, e o canal **Grupo** entrega aos membros online do grupo de quem falou (a lista vem do servidor do grupo). Com isso o trecho acima sobre o Grupo responder `NaoEstaEmGrupo` para todos e o histórico não ser enviado deixa de valer. Código: `HistoricoDaCidadeDoChat`.
