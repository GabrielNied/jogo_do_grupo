# Social

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 13000–13999

## Resumo

Party (time, chamado de **grupo** nas regras), guilda e chat.

Esta página detalha o **grupo** (convite, lista, líder, sair e expulsar; issue #94). Chat (#95), guilda e divisão de loot ficam para outras tarefas.

## Definido

- Party: tamanho e divisão de loot
- Party de até 5 jogadores (o tamanho de uma dungeon); raids juntam 10 (decidido em 2026-10-10)
- Guilda: níveis, base própria, benefícios

## Em aberto

- [ ] Tamanho máximo de party e guilda? Party decidida em 2026-10-10: 5 (veja "Definido"); ~~party~~ sai desta pergunta. Falta o tamanho da guilda.
- [ ] Regras de divisão de loot
- [ ] Líder: quem herda quando o líder sai ou cai? (proposta: o membro mais antigo no grupo) e o líder pode passar a liderança? (proposta: sim)
- [ ] Só o líder expulsa, ou qualquer membro pode pedir votação? (proposta: só o líder expulsa, sem votação)
- [ ] Tempo que a vaga fica guardada após desconexão (proposta: 120 s) e tempo de validade do convite (proposta: 60 s)
- [ ] Convidar por nome exige amizade ou proximidade? (proposta: não, basta o personagem estar online)
- [ ] Bloquear convites (modo "não aceitar grupo") e fila automática de grupo: ficam fora da primeira versão?
- [ ] Raid de 10: é um grupo maior ou dois grupos de 5 juntos? (veja [instancias.md](instancias.md))
- [ ] O grupo desfaz ao ficar com 1 pessoa? (proposta: sim)

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

## Depende de

- Tempo real ([#77](https://github.com/GabrielNied/jogo_do_grupo/issues/77) e [arquitetura](../arquitetura.md#servidor-tempo-real-udp)): sessão autenticada por personagem e aviso de desconexão.
- Personagem ([personagem.md](personagem.md)): nome único do personagem.
- Dungeons ([instancias.md](instancias.md), #66): entrada em dungeon com o grupo (só ponteiro aqui).

## Expõe

- Contrato do grupo na faixa 13000–13999 (mensagens de convidar, aceitar, recusar, sair, expulsar, passar liderança e a lista do grupo), a definir pelo agente de contrato a partir das regras acima.
- Consulta "quem é o grupo de um personagem" para outras funcionalidades (ex.: dungeons), por evento ou contrato do shared, sem referência direta entre funcionalidades.
