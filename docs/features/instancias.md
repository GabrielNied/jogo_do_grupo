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

## Em aberto

- [x] ~~Tamanho de grupo de dungeons e de raids?~~ Decidido: dungeon 5, raid 10
- [x] ~~Dificuldades diferentes?~~ Decidido em 2026-10-10: dificuldade que escala sem fim (veja "Definido")
- [x] ~~Raids divididas em etapas curtas (gates)?~~ Decidido em 2026-10-10: sim
- [ ] Quanto cada nível de dificuldade aumenta vida e dano dos inimigos? (WoW Mythic+: cerca de 8% por nível)
- [ ] Vai ter cronômetro e modificadores, como no Mythic+?
- [ ] **Conflito com o livro (proposta do board, 2026-10-11):** o livro fala em "instâncias de 20 jogadores" (seção 10); aqui continua valendo dungeon de 5 e raid de 10 (decididos em 2026-10-10). O grupo decide se os 20 são o limite do mundo/grupo (veja [social](social.md)) e não da dungeon.
- [ ] A Mina do Reflexo (três salões, chefe mineral) é a dungeon do MVP? Veja "Livro de Asterra: Mina do Reflexo".
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
- **Encerramento:** a instância encerra por **vitória**, **derrota** ou após ficar **5 minutos vazia** (`PrazoVazia`). No encerramento, todos os participantes recebem `DungeonEncerrada` com o resultado e são devolvidos à posição anterior na cidade.

## Depende de

- [Social](social.md): consulta de grupo e líder (`IConsultaDeGrupo`)
- [Mundo](mundo.md): posições e transição entre áreas (`IPosicoesDoMundo`, `ITransicoesDeAreaDoMundo`, `AreasDoMundo`)

## Expõe

- Mensagens de tempo real (`InstanciasMessageIds`, faixa 12000–12999):
  - `EntrarNaDungeon` (12000): cliente solicita entrar na dungeon
  - `DungeonIniciada` (12001): servidor confirma início/entrada na corrida com ID e área
  - `EntradaRecusada` (12002): servidor recusa com código (`NaoELider`, `GrupoEmOutraDungeon`, `LongeDemais`)
  - `DungeonEncerrada` (12003): servidor avisa encerramento com `ResultadoDungeon` (`Vitoria`, `Derrota`, `InstanciaVazia`)
- Serviço server-side `ServicoDeInstancias`: gerencia criação, entrada, varredura periódica e encerramento de corridas
