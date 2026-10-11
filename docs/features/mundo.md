# Mundo

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 1000–1999

## Resumo

Áreas do jogo: cidade e regiões de um mundo persistente; clima e ciclo de dia e noite. ~~Hub social e regiões jogadas em sessões de grupo pequeno~~ substituído em 2026-10-11 (D1 opção A).

## Definido

- ~~**Sessões de grupo pequeno** (D1 da [validação de 2026-10-10](../brainstorm/validacao.md)): o objetivo é jogar em poucas pessoas, não um mundo aberto com centenas de jogadores.~~ Substituído em 2026-10-11 por: **mundo persistente** (D1 opção A da [validação](../brainstorm/validacao.md)): um mundo único e contínuo. A cidade é compartilhada por quem está nela.
- Ambientação medieval
- **Farm de mapa em grupo**: o grupo limpa áreas cheias de inimigos para ganhar experiência e loot (decidido em 2026-10-10)
- ~~Mundo aberto~~ substituído em 2026-10-10 por sessões de grupo pequeno. ~~Sessões de grupo pequeno~~ substituídas em 2026-10-11: o mundo aberto volta (D1 opção A).

## Em aberto

- [x] ~~Quantas pessoas por sessão? (a fatia jogável propõe até 4)~~ Substituído em 2026-10-11: a sessão deixou de ser o formato do jogo. O tamanho do grupo da dungeon continua em [instâncias](instancias.md).
- [x] ~~Existe um hub social compartilhado entre grupos ou cada grupo tem o seu?~~ Decidido em 2026-10-11: a cidade faz parte do mundo único. Quem está na área se vê.
- [ ] As regiões são mapas fixos, gerados ou uma mistura?
- [ ] Tamanho do mapa e quantidade de regiões
- [ ] Como funciona a viagem rápida?
- [ ] Clima dinâmico afeta gameplay?
- [ ] Luz e som como mecânica (noite escura, skills que iluminam)? Veja S8 na validação.

## Regras

### Hub inicial (arte)

Primeira área jogável (issue #36): o Core chama o construtor do Mundo ao entrar ([Entrada no mundo](../arquitetura.md#entrada-no-mundo)) e o jogador nasce nela. ~~Sem rede.~~ A cidade é área do mundo persistente (D1 opção A, 2026-10-11). Esta primeira montagem ainda não sincroniza outros jogadores.

- **Montagem:** por código em tempo de execução (`ConstrutorHub`, registrado com `EntradaNoJogo.DefinirConstrutorDeMundo`), a partir dos prefabs de `Features/Mundo/Resources/Mundo/`. Os prefabs são gerados por script de editor (`GeradorDePecasDoHub`, menu *Jogo do Grupo > Mundo > Gerar peças do hub*) a partir do Kenney "Fantasy Town Kit" (CC0, em `ThirdParty/Kenney/`). O script é reproduzível: rodar de novo regenera tudo.
- **Grade:** **módulo de 2 m** (peça Kenney de 1 m × 2). Peças modulares ficam em células de 2 × 2 m, com origem no centro da base.
- **Tamanho:** quadrado de **40 × 40 m** (20 × 20 células) centrado na origem, chão com a face de cima em Y = 0. Muros de 1 célula (2 m) de espessura e 3 m de altura em volta, com torres de 6 m nos 4 cantos. **Área andável: 36 × 36 m** (de -18 a +18 em X e Z). Medidas em `LayoutDoHub`.
- ~~**Disposição:** praça de pedra de 12 × 12 m no centro com a fonte, duas estradas em cruz de 4 m de largura até os muros, bancos, lanternas (luz de ponto), 4 barracas, 2 carroças, 12 árvores e 4 pedras. Uma luz direcional (sol) e luz ambiente suave.~~ substituída em 2026-10-11 pela disposição abaixo (mais ruas e casas; o resto, como a luz, continua).
- **Disposição (2026-10-11):** praça de pedra de 12 × 12 m no centro com a fonte, duas estradas em cruz de 4 m de largura até os muros e uma **rua em anel** de 2 m de largura (em `x` ou `z` = ±11) ligando as duas estradas. Em volta, **20 casas** do Kenney Fantasy Town Kit (paredes de painel, porta, janelas e telhado de quatro águas; 4 × 4 m ou 6 × 4 m, estilo pedra ou madeira por quadrante, de porta para a praça ou para a rua), bancos, lanternas (luz de ponto), 4 barracas, 2 carroças, 2 pedras e 24 árvores. Uma luz direcional (sol) e luz ambiente suave. As posições estão em `LayoutDoHub.Props`.
- **Terreno de fora (2026-10-11):** grama de **120 × 120 m** com a cidade no meio (prefab `Terreno_Fora`, face de cima em Y = 0, colisor de 1 m de espessura), com cerca de 46 árvores e pedras espalhadas por uma semente fixa (sempre o mesmo resultado; `LayoutDoHub.PropsFora`), a pelo menos 8 m dos muros e fora do corredor da estrada do portão. Barreiras invisíveis de 20 m de altura na borda seguram o jogador no terreno.
- **Portão (2026-10-11):** vão de **4 m** (duas células) no meio do muro **norte**, sem muro e sem colisor, ladeado por duas torres de 6 m (as células vizinhas). A estrada de 4 m de largura atravessa o vão e segue 20 m pelo terreno de fora. O nascimento (ao sul) olha para a fonte e, atrás dela, para o portão.
- **Colisão:** chão, terreno de fora, muros, torres, casas e todos os props têm colisor; as estradas, não (o chão já colide). O `CharacterController` do jogador tem 1,8 m de altura e 0,35 m de raio.
- **Nascimento:** em (0, 0, -12), ao sul da fonte, olhando para ela (+Z), em área livre (nenhum prop a menos de 2,5 m). O teste EditMode confere que está dentro dos limites do chão, com os pés na superfície e fora de qualquer colisor.
- **Prefabs ausentes:** peça sem prefab é pulada com aviso; só o chão e os muros têm uma reserva (cubos), para o hub nunca ficar sem colisão.

## Depende de

## Expõe
