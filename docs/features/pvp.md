# PvP

**Responsável:** funcionalidade
**Status:** ativo
**Faixa de IDs de mensagem:** 14000–14999

## Resumo

Arenas 3v3, duelos 1v1 e PvP por bandeira opcional no mundo aberto.

## Definido

- **Decidido por Gabriel em 2026-10-16** (quiz de lacunas de MMO):
  - **Mundo aberto:** PvP por **bandeira opcional**, ligada na cidade; só luta com quem também ligou. Algumas **zonas marcadas** são PvP para todos, com loot melhor. No resto do mundo não há PvP forçado. Base: New World.
  - **Sem perda na morte em PvP**, igual ao PvE ([personagem](personagem.md#morte-e-caído-issue-97)).
  - **Territórios (futuro):** guildas disputam os pontos marcados do mapa (os das bases, [mundo](mundo.md)) em guerra agendada e cobram imposto nas estações de lá. Sem facção de jogador.
- **Decidido por Gabriel em 2026-10-16** (quiz de definições): o primeiro PvP é **duelo 1v1** (pedido e aceite entre jogadores próximos) e **arena 3v3 com o equipamento real**, sem normalizar atributos, com ranking por temporada ([#193](https://github.com/GabrielNied/jogo_do_grupo/issues/193)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3): **primeiro só bandeira opcional** no mundo aberto; **zonas PvP forçadas** entram depois, no mesmo épico PvP (#193).

## Em aberto

- [x] ~~Com o jogo em sessões de grupo pequeno (D1 da [validação de 2026-10-10](../brainstorm/validacao.md)), como fica o "PvP no mundo aberto"? Zonas PvPvE dentro das sessões?~~ Substituído em 2026-10-11: a D1 opção A devolve o PvP ao mundo aberto. Áreas seguras, penalidades e consentimento continuam em aberto (S4 da [validação](../brainstorm/validacao.md)).
- [x] PvP aberto é livre ou por consentimento? Decidido em 2026-10-16: por consentimento (bandeira), fora das zonas PvP.
- [x] Atributos normalizados nas arenas? Decidido em 2026-10-16: não; vale o equipamento real.
- [x] Superada em 2026-10-16 pela bandeira opcional, pelas zonas PvP e pelo duelo e a arena (Definido). **Proposta do livro de Asterra (2026-10-11), não validada pelo grupo** ([figjam.md](../brainstorm/figjam.md), seção 10): PvP só em guerra de guilda, evento PvP ou área específica; a primeira versão concentra PvE (PvP é conteúdo posterior). Responde em parte "livre ou por consentimento?": nada de PvP livre no mundo aberto na primeira versão, sem decidir as arenas. Skills não causam dano entre membros de um grupo.

## Regras

- **Duelo 1v1**:
  - Pedido e aceite entre jogadores a até 15 metros (`RegrasPvp.DistanciaMaximaConviteDueloMetros`).
  - Convite expira em 30 segundos se não for respondido.
  - Contagem regressiva de 3 segundos antes do início do combate.
  - Limite de arena do duelo em raio de 50 metros a partir do centro (`RegrasPvp.RaioLimiteDueloMetros`). Afastar-se por mais de 10 segundos fora do raio resulta em derrota por fuga.
  - Dano fatal em duelo é interceptado pelo servidor: o jogador perdedor não cai, não morre e não perde itens, tendo seu HP restaurado ao final.
- **Arena 3v3**:
  - Fila de matchmaking individual ou em equipe (`EntrarNaFilaDeArena` / `SairDaFilaDeArena`).
  - Partida inicia ao reunir 6 combatentes (Time Azul vs Time Vermelho).
  - Equipamento real e atributos mantidos sem normalização.
  - Eliminação da equipe oponente concede vitória (+25 pontos de rating por vitória, -15 por derrota, piso em 0).
  - Término da partida envia relatório de resultado e teletransporta os jogadores de volta ao hub.
- **Bandeira PvP no Mundo Aberto**:
  - Pode ser alternada na cidade (`AlternarBandeiraPvp`).
  - Jogadores com bandeira ativa podem se engajar mutuamente em combate fora das zonas seguras.
  - Derrota no PvP de mundo aberto não causa perda de itens.
- **Integração Desacoplada (ADR 0016)**:
  - O PvP registra `AlvosDePvp : IAlvosDeCombate` no container de injeção de dependência do servidor. Combate e habilidades consultam alvos PvP válidos sem referência direta ao módulo de PvP.

## Depende de

- `Core`: rede, despacho de mensagens, sessões de jogadores e loop de jogo.
- `Personagem`: posição dos jogadores no mundo e atributos de combate.
- `Combate`: contratos `IAlvosDeCombate` e `ICombatenteAlvo`.

## Expõe

- `shared`:
  - `PvpMessageIds` (faixa 14000–14021): mensagens de convite, contagem, encerramento de duelo, matchmaking de arena e bandeira PvP.
  - `RegrasPvp`: distâncias de convite e arena, cálculo de rating e validações.
  - DTOs de rede e serialização de eventos PvP.
- `server`:
  - `ServicoDePvp`: orquestração autoritativa de duelos, arenas e bandeira.
  - `AlvosDePvp`: implementação de `IAlvosDeCombate` para acertos entre duelistas, membros de times rivais e jogadores com bandeira.
  - `PvpModule`: registro no DI, tratamento de mensagens e tick no game loop.
- `client`:
  - `EstadoDePvp`: gerenciador de rede e estado local do PvP.
  - `ControleDePvp`: atalhos interativos (Y/N para aceitar/recusar duelo).
  - `TextosPvp`: formatação localizada de avisos e notificações.
  - `ObservadorDePvp`: ciclo de vida Unity e inicialização.
