# Dungeons e raids

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 12000–12999

## Resumo

Conteúdo instanciado para grupos pequenos: dungeons e raids, com foco em derrotar bosses cada vez mais fortes.

## Definido

- Regras iniciais da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- Dungeons: grupo pequeno
- **Derrotar bosses mais fortes em dungeons e raids é o foco do loop principal** (D5 da [validação de 2026-10-10](../brainstorm/validacao.md))
- ~~O jogo é feito para jogar em poucas pessoas (D1)~~ Substituído em 2026-10-11: o mundo é persistente e compartilhado (D1 opção A da [validação](../brainstorm/validacao.md)). Dungeons (5) e raids (10) continuam conteúdo instanciado dentro desse mundo.
- ~~Raids: grupos grandes, endgame~~ substituído em 2026-10-10: raids de 10 jogadores
- **Dungeons de 5 jogadores e raids de 10** (decidido em 2026-10-10), com composição tank, healer e DPS no estilo de World of Warcraft (veja [combate.md](combate.md))
- Base: dungeons e raids do [World of Warcraft](../referencias.md)
- **Progressão infinita no estilo Diablo**: dungeons com **dificuldade que escala sem fim**, como o Mythic+ do WoW; quanto mais alto o nível, mais fortes os inimigos e melhor o loot (decidido em 2026-10-10)
- **Raids divididas em etapas** curtas (gates), cada uma com um boss ou fase, como no Lost Ark (decidido em 2026-10-10)
- **Materiais de fusão** (decidido por Gabriel em 2026-10-12): dungeons dão os materiais da fusão de +5 até +8 e raids, os de +9 e +10. Raid nunca é exigida antes disso. Regras em [itens](itens.md#fusão); materiais na [#617](https://github.com/GabrielNied/jogo_do_grupo/issues/617)
- **Decisões do Gabriel em 2026-10-16** (quiz de lacunas de MMO):
  - **Raid:** entra quantas vezes quiser; cada etapa dá loot **uma vez por semana** por personagem, então dá para ajudar amigos sem ganhar de novo. O reset semanal é o mesmo das [missões](missoes.md).
  - **Dificuldade sem fim:** o líder escolhe o nível, até um acima do maior que já venceu. Cada nível soma **8%** de vida e de dano aos inimigos. Há cronômetro: vencer no tempo libera o próximo nível. Sem chave e sem modificador semanal. Se "o maior já vencido" é do grupo ou de cada um fica para a implementação.
  - **Grupo de raid:** o grupo vira de raid ao passar de 5, até 10, com um líder só ([social](social.md#definido)).
  - **Fila automática:** a pessoa escolhe o papel sem conferência do servidor, e o servidor monta 1 tank, 1 healer e 3 DPS ([social](social.md#definido)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições):
  - **A Mina do Reflexo é a dungeon do MVP** (conteúdo da #800, abaixo).
  - **Voltar à dungeon depois de cair:** quem retornou à **entrada da instância** (quiz 3) permanece na mesma corrida; quem foi à cidade pode reentrar enquanto a instância estiver aberta ([personagem](personagem.md#retorno-à-cidade)).
- **Decisões do Gabriel em 2026-10-16** (quiz de definições 3): retorno por queda (manual ou 45 s) na dungeon = **safe zone na entrada da instância**, vida cheia, **sem** expulsar ao hub da cidade.

## Em aberto

- [x] ~~Tamanho de grupo de dungeons e de raids?~~ Decidido: dungeon 5, raid 10
- [x] ~~Dificuldades diferentes?~~ Decidido em 2026-10-10: dificuldade que escala sem fim (veja "Definido")
- [x] ~~Raids divididas em etapas curtas (gates)?~~ Decidido em 2026-10-10: sim
- [x] Quanto cada nível de dificuldade aumenta vida e dano dos inimigos? (WoW Mythic+: cerca de 8% por nível) Decidido em 2026-10-16: 8% por nível.
- [x] Vai ter cronômetro e modificadores, como no Mythic+? Decidido em 2026-10-16: cronômetro sim, modificadores não.
- [x] **Conflito com o livro (proposta do board, 2026-10-11):** o livro fala em "instâncias de 20 jogadores" (seção 10); aqui continua valendo dungeon de 5 e raid de 10 (decididos em 2026-10-10). Respondido em 2026-10-11 (/decida, #351, [social](social.md#em-aberto)): party de 5, raid de 10, dungeon de 5; os 20 ficam só como proposta registrada.
- [x] A Mina do Reflexo (três salões, chefe mineral) é a dungeon do MVP? Veja "Livro de Asterra: Mina do Reflexo". Decidido em 2026-10-16: sim.
- [ ] Veja as [referências](../referencias.md) de dungeons, raids e bosses

## Regras

### Livro de Asterra: Mina do Reflexo (proposta do board, 2026-10-11)

Do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seções 06 e 10). **Proposta do board, não validada pelo grupo.**

- **Mina do Reflexo** seria a dungeon do MVP: entrada física na Serra do Vidro Frio (veja [mundo](mundo.md)), **três salões e um chefe mineral**, túneis que levam o grupo a arenas amplas com luz e travessias. Cena marcante: cristais apagados escondem mobs, orbes de luz revelam o enxame e uma ponte de gelo abre posição de ataque. Combina com o Guardião do Reflexo de [mobs](mobs.md).
- **Fenda do Horizonte** (portal com ondas, âncoras e guardião): FUTURO, não exige sistema de eventos para jogar a dungeon.
- **Recompensas:** equipamentos, essências e materiais; o dono do loot de chefe é quem causou mais dano. Insumos essenciais também vêm de exploração e mobs comuns.
- **Expedição inicial:** a dungeon integra a primeira expedição (preparar na cidade, explorar, combater, voltar).

### Instância da dungeon (#425, #66)

Entrada, ciclo de vida e isolamento das corridas de dungeon no servidor autoritativo:

- **Entrada solo ou em grupo:** o líder do grupo (ou jogador solo) envia `EntrarNaDungeon` a partir da cidade (`AreasDoMundo.Cidade`). Se um membro não líder tentar iniciar uma corrida nova, o pedido é recusado com `NaoELider`.
- **Membros próximos e retardatários:** ao iniciar a corrida, membros do grupo que estão a até **10 m** da entrada na cidade são teleportados para a área isolada da instância (`AreasDoMundo.Instancia(id)`). Membros a mais de 10 m ficam de fora e podem enviar `EntrarNaDungeon` mais tarde para entrar na corrida ativa.
- **Isolamento de área:** cada corrida gera uma área exclusiva no Mundo. Posições e presenças não vazam para a cidade nem para corridas simultâneas de outros grupos.
- **Reconexão e tolerância a quedas:** se um participante desconectar dentro da instância, sua vaga fica reservada por até **120 s** (`PrazoDeReconexao`). Reconectar e pedir entrada nesse intervalo recoloca o jogador na mesma corrida. Após 120 s sem retornar, a vaga é liberada.
- **Encerramento:** a instância encerra por **vitória** (boss derrotado e prazo de coleta expirado), **derrota** (todos os membros caídos ou ausentes) ou após ficar **5 minutos vazia** (`PrazoVazia`). No encerramento, todos os participantes recebem `DungeonEncerrada` com o resultado e são devolvidos à posição anterior na cidade.
- **Vitória do chefe e prazo de coleta (issue #809):** ao derrotar o Guardião do Reflexo, o servidor não expulsa o grupo imediatamente. Ele envia `ChefeDerrotadoNaDungeon` aos participantes e mantém a instância ativa durante um prazo de coleta de **20 s** (`PrazoParaColetaDeLoot`), permitindo que os participantes coletem o loot gerado no chão da sala do chefe. Jogadores que desejarem podem enviar `SairDaDungeon` para sair antes do fim do prazo.
- **Caído e socorro na dungeon (issue #809):** quando a vida de um participante chega a 0, ele entra em `FaseDeQueda.Caido` por 45 s. Um aliado próximo (até 2 m) pode segurar a tecla de interagir por 3 s para levantá-lo (`FaseDeQueda.Vivo` com 20% da vida máxima); dano ao socorrista cancela o canal. Esgotado o prazo de 45 s, o jogador retorna à cidade sem perder itens. Se todos os membros da dungeon estiverem caídos ou tiverem saído, a instância encerra em **derrota**.
- **Conteúdo da Mina do Reflexo (issue #800):** ao abrir a corrida o servidor popula a área com três salões de Javali do Campo **sem respawn** e o Guardião do Reflexo no salão final (`LayoutDaMinaDoReflexo`, shared). A entrada física fica na colina oeste (~200 m da cidade); o client monta a entrada e os salões por código e mostra recusas e resultado na tela.
- **Dificuldade sem fim e cronômetro (issue #189):** o líder seleciona o nível da corrida (`Nivel` em `EntrarNaDungeon`, padrão 1, até 1 acima do maior nível já vencido pelo líder). Cada nível adiciona autoritativamente **+8% de vida e de dano** a todos os inimigos comuns e ao chefe (`RegrasDificuldadeSemFim.CalcularMultiplicadorVida` e `CalcularMultiplicadorDano`: `1.0 + 0.08 * (nivel - 1)`). A dungeon possui um cronômetro com tempo limite padrão de **15 minutos (900 s)**; derrotar o chefe dentro do tempo qualifica como vitória "no tempo" e desbloqueia o próximo nível de dificuldade. Tentativas de entrar num nível não liberado são recusadas com `CodigosErroInstancias.NivelNaoDesbloqueado`.

### Raid de 10: grupo, três etapas e trava semanal (issue #190)

- O grupo que passa de 5 membros é **de raid**, até **10** (`RegrasRaid`), com um líder só. O líder envia o mesmo `EntrarNaDungeon`; o servidor abre a corrida como raid (`DungeonIniciada.Raid`) e recusa acima de 10 com `GrupoGrandeDemais`. Membro não líder segue recusado (`NaoELider`).
- **Três etapas com chefe:** a raid avança em 3 etapas sequenciais (`TotalEtapas = 3`). Cada etapa possui um chefe específico posicionado na sua arena (`Presa Velha`, `Guardião do Reflexo`, `Colosso Basáltico`). Ao derrotar o chefe de uma etapa, o servidor avisa os participantes com `EtapaRaidConcluida` e abre a próxima etapa. Na etapa 3, a vitória encerra a raid.
- **Trava semanal de loot (lockout):** cada etapa concede recompensa de materiais de fusão (`CreditarRaidAsync`) no máximo **uma vez por semana por jogador**. O reset semanal ocorre toda **terça-feira às 06:00 BRT (UTC-3)**. O jogador pode refazer a raid quantas vezes quiser na semana para ajudar aliados, mas chefes já derrotados na semana não concedem novo crédito semanal (indicado via `GanhouLootSemanal = false` em `EtapaRaidConcluida`). O cliente pode consultar o status das etapas via `ConsultarBloqueioSemanalRaid`.

## Depende de

- [Social](social.md): consulta de grupo e líder (`IConsultaDeGrupo`)
- [Mundo](mundo.md): posições e transição entre áreas (`IPosicoesDoMundo`, `ITransicoesDeAreaDoMundo`, `AreasDoMundo`)
- [Combate](combate.md): regras de estado de queda e socorro (`IEstadoDeQueda`, `RegrasQueda`)

## Expõe

- Mensagens de tempo real (`InstanciasMessageIds`, faixa 12000–12999):
  - `EntrarNaDungeon` (12000): cliente solicita entrar na dungeon (informa `Nivel`)
  - `DungeonIniciada` (12001): servidor confirma início/entrada na corrida com ID, área, `Nivel` e `TempoLimiteSegundos`
  - `EntradaRecusada` (12002): servidor recusa com código (`NaoELider`, `GrupoEmOutraDungeon`, `LongeDemais`, `NivelNaoDesbloqueado`)
  - `DungeonEncerrada` (12003): servidor avisa encerramento com `ResultadoDungeon`, `Nivel`, `TempoTotalSegundos`, `NoTempo` e `ProximoNivelLiberado`
  - `ChefeDerrotadoNaDungeon` (12004): servidor avisa que o chefe caiu, prazo para coleta, `Nivel`, `TempoTotalSegundos` e `NoTempo`
  - `GrupoGrandeDemais` (código de `EntradaRecusada`): grupo com mais de 10 membros
  - `SairDaDungeon` (12005): cliente pede saída antecipada da corrida após a vitória
  - `EtapaRaidConcluida` (12006): servidor avisa conclusão de etapa da raid com etapa concluída, próxima etapa, total de etapas e se o jogador ganhou loot semanal
  - `ConsultarBloqueioSemanalRaid` (12007): cliente consulta quais etapas já concederam loot semanal
  - `RespostaBloqueioSemanalRaid` (12008): servidor responde com chave da semana e máscara de bits das etapas já concluídas
- Regras de balanceamento compartilhadas (`RegrasDificuldadeSemFim`): multiplicadores de vida/dano, validação de cronômetro e progressão de recorde de nível
- Serviço server-side `ServicoDeInstancias`: gerencia criação, entrada, validação de nível, varredura periódica e encerramento de corridas
