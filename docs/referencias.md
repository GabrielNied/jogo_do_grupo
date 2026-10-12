# Jogos de referência

O grupo decidiu usar jogos existentes como base e ir modificando conforme o jogo for sendo construído. Esta página diz **no que cada jogo é famoso**, **o que pegar dele** e **onde o nosso jogo é diferente**.

Ao definir um sistema novo, comece pela referência dele, descreva o que muda e registre no `docs/features/<x>.md`.

## Preferências do grupo

- **Combate (2026-10-10):** o grupo gosta do equilíbrio do **New World**: ação, mas **não frenético e sem spam de skills**. Poucas skills, com recarga que pesa, e o resto do tempo ataques básicos, esquiva e posicionamento.
- **Troca de arma:** interessante, a pensar (veja S10 na [validação](brainstorm/validacao.md)).

## Referência por sistema

| Sistema | Base principal | Também olhar | Onde somos diferentes |
|---|---|---|---|
| Ritmo do combate | New World | Elden Ring (contato e leitura do corpo), Black Desert (o contrário: combos rápidos) | Esquiva sem invulnerabilidade; bloqueio pela defesa do equipamento |
| Build sem classe | Throne and Liberty, Albion Online | New World, Guild Wars 2 | As skills dos itens são sorteadas; a combinação de itens e a fusão criam builds que não existem nesses jogos |
| Troca de arma | New World, Throne and Liberty | Guild Wars 2 | A pensar (S10) |
| Skills nos itens | Albion Online (skill por peça) | Path of Exile (gemas), Ragnarok (cartas), Destiny 2 (perks aleatórios) | Sorteio aleatório + fusão |
| Itens e loot | Diablo IV, Path of Exile | Destiny 2 | Skills fazem parte do sorteio do item |
| Papéis e buffs | World of Warcraft | Throne and Liberty (papel pela arma), Guild Wars 2 (combos) | O papel vem dos itens; os buffs vêm das skills dos itens |
| Dungeons e raids | World of Warcraft | Lost Ark, Final Fantasy XIV | Raid de 10, não de 20 a 40 |
| Bosses cada vez mais fortes | WoW Mythic+ | Monster Hunter | — |
| Mecânicas de boss | Final Fantasy XIV, WoW | Lost Ark | — |
| Sessões e hub | Monster Hunter, Lost Ark | — | — |
| Farm de mapa em grupo e inimigos em massa | RYL, Diablo IV | Path of Exile (mapas) | — |
| Eventos | Guild Wars 2 (eventos dinâmicos) | Diablo IV (eventos de mundo) | Dentro das sessões de grupo |
| Fusão e utilidade de todo item | Runescape | Black Desert (aprimoramento com risco), Ragnarok (cartas) | A fusão tem teto e custo crescente |
| Economia entre jogadores | Albion Online | Runescape (Grand Exchange) | A decidir durante a construção |
| Sobrevivência e construção | Valheim | — | Ainda não decidido; fica para depois da fatia jogável |

## No que cada jogo é famoso

### New World (Amazon, 2021)

- **Famoso por:** combate de ação em 3ª pessoa com peso e cadência; armas definem o estilo, sem classes; coleta e craft muito presentes.
- **Pegar para a implementação:**
  - **duas armas equipadas** e troca entre elas no meio da luta;
  - **três skills por arma**, com recarga **separada por arma** (trocar de arma dá acesso às outras três);
  - **ataque leve e pesado** como base do dano; skills são o tempero, não o principal;
  - **stamina** gasta em **esquiva** (com uma janela curta de invulnerabilidade) e **bloqueio** (o custo depende do dano recebido);
  - atributos que mudam como cada arma escala.
- **Lição técnica:** no lançamento, um exploit de invencibilidade foi atribuído a decisões confiadas ao client, e houve duplicação de ouro quando o salvamento de personagens falhou durante transferências entre servidores. Para nós: **servidor autoritativo** em tudo que decide resultado, e **operações de economia atômicas** (o ouro sai e entra na mesma transação, ou nada acontece).

### Elden Ring (FromSoftware, 2022)

- **Famoso por:** cada golpe se lê no corpo (preparo, o instante em que encosta, recuperação) e o acerto segura a pose por um momento. Chefe se anuncia pela animação, não por um círculo solto.
- **Pegar:** o dano no instante em que o modelo encosta; uma pausa visual curta nesse instante, só no client; o chefe conta o ataque com o corpo durante o aviso; o acerto interrompe o golpe de um inimigo comum e segura a próxima ação por um instante. Os números estão em [combate](features/combate.md#reação-ao-acerto) e em [mobs](features/mobs.md#contato-do-golpe).
- **Não pegar:** esquiva em que o dano some, quebra de postura do chefe, e o client decidir se o golpe acertou.

### Throne and Liberty (NCSoft / Amazon, 2024)

- **Famoso por:** **sem classes; o papel vem do par de armas equipado**, com troca de arma durante o combate.
- **Pegar:** a ideia de que **a combinação de duas armas define o papel**. Espada e escudo puxa para tank; varinha e tomo para healer; armas de duas mãos para dano em área. É o modelo mais próximo de "a combinação de itens decide o estilo" (D2).
- **Diferente:** nas nossas armas as skills são sorteadas, então duas espadas podem jogar de forma diferente.

### Albion Online (Sandbox Interactive, 2017)

- **Famoso por:** "você é o que veste"; economia feita pelos jogadores; PvP com perda do equipamento em zonas perigosas.
- **Pegar:** **cada peça de equipamento dá skills**. A arma dá as skills principais e define o propósito da build, a armadura dá skills de defesa e agro, e cada slot contribui com algo. É a base mais direta para "skills nos itens" (D3) e para papéis vindos do equipamento.
- **Também:** a árvore de progressão (Destiny Board) mede o quanto você domina cada tipo de equipamento. É uma ideia possível para os **talentos** num jogo sem classes.

### Guild Wars 2 (ArenaNet, 2012)

- **Famoso por:** **eventos dinâmicos** que surgem, escalam e terminam conforme a participação dos jogadores; esquiva para todos; troca de arma.
- **Pegar:**
  - **trocar de arma substitui as skills da barra** na hora;
  - **esquiva com barra de energia** que recarrega sozinha;
  - **campos de combo**: a skill de um jogador cria um campo e a de outro o "finaliza", gerando um efeito extra. É uma boa base para **buffs que complementam o time**;
  - eventos que escalam com o número de jogadores (base para os eventos e invasões do brainstorm).

### World of Warcraft (Blizzard, 2004)

- **Famoso por:** dungeons e raids; a trindade **tank, healer e DPS**; encontros de boss bem desenhados.
- **Pegar:**
  - dungeons de **5 jogadores** com composição 1 tank, 1 healer e 3 DPS;
  - **ameaça (agro)**: o tank gera mais ameaça e os inimigos atacam quem tem mais;
  - **buffs de grupo** que tornam útil ter papéis diferentes no time;
  - **Mythic+**: dungeons de 5 com **dificuldade que escala sem fim**. Cada nível aumenta a vida e o dano dos inimigos (hoje, cerca de 8% por nível). Há um **cronômetro**, uma porcentagem de inimigos comuns a derrotar antes do boss final, penalidade de tempo por morte e **modificadores semanais** (ex.: inimigos comuns mais fortes numa semana, bosses na outra). Terminar no tempo sobe o nível da chave. É o modelo pronto para "**derrotar bosses cada vez mais fortes**" (D5).

### Final Fantasy XIV (Square Enix, 2013)

- **Famoso por:** bosses com **mecânicas coreografadas e telegrafadas** (áreas marcadas no chão, sequências que o grupo aprende), história, e um personagem que pode jogar todos os papéis.
- **Pegar:** o desenho de boss como **coreografia que se aprende**: ataques avisados com antecedência e padrões em fases. Combina com combate cadenciado: errar é sair tarde da área marcada, não falta de reflexo extremo.

### Lost Ark (Smilegate, 2019)

- **Famoso por:** raids de boss em **etapas (gates)** com mecânicas fortes, para grupos pequenos (4 a 8); combate de ação em visão isométrica.
- **Pegar:** raids divididas em etapas curtas, cada uma com um boss ou fase. Facilita salvar o progresso e testar cada parte isoladamente.

### Monster Hunter (Capcom)

- **Famoso por:** o loop **caçar boss → usar as partes para craftar equipamento → caçar um boss mais forte**; combate de ação deliberado, com animações que comprometem o jogador.
- **Pegar:** bosses como objetivo central da progressão, cada um com drops próprios que viram equipamento. Mesmo não sendo MMO, é a melhor referência para "foco em bosses" com grupo pequeno.

### Path of Exile (Grinding Gear Games, 2013)

- **Famoso por:** builds com profundidade enorme; **skills vindas de gemas encaixadas nos itens**; endgame de mapas para farmar; filtro de loot.
- **Pegar:**
  - **gemas de skill** que vão em **encaixes (sockets)** dos itens, e **gemas de suporte** que só modificam a skill se estiverem em encaixes **ligados** a ela. É um modelo alternativo para "skills nos itens", em que o jogador monta a combinação;
  - alguns itens dão skills próprias, que não podem ser modificadas por gemas;
  - **filtro de loot** configurável (base para o loot rápido por tipo e nível do brainstorm);
  - **mapas** como endgame de farm.

### Diablo IV (Blizzard, 2023)

- **Famoso por:** loot com **propriedades aleatórias** (afixos), raridades, dungeons e eventos no mapa.
- **Pegar:** o modelo de **item com afixos sorteados por raridade** (mais raro, mais afixos). É a base para sortear as skills do item junto com os atributos; também eventos periódicos em regiões.

### Destiny 2 (Bungie, 2017)

- **Famoso por:** armas com **perks aleatórios** (a mesma arma cai com combinações diferentes, e o jogador caça a combinação ideal); raids de 6 com mecânicas.
- **Pegar:** o "caçar o sorteio certo" como motivação para repetir conteúdo, sem precisar de itens novos o tempo todo.

### Ragnarok Online (Gravity, 2002)

- **Famoso por:** **cartas** que caem de monstros e são inseridas em slots do equipamento, mudando o que o item faz; bosses MVP disputados.
- **Pegar:** cartas como forma de levar **uma habilidade de um monstro para um item**. Conversa com a fusão e com o "conhecimento de inimigos" do brainstorm.

### Black Desert Online (Pearl Abyss, 2015)

- **Famoso por:** combate de ação rápido, cheio de combos; **aprimoramento de equipamento com chance de falha**, que fica mais provável de dar certo a cada tentativa falha.
- **Pegar com cuidado:** o aprimoramento com risco é referência para a **fusão** (custo e retorno decrescente). O combate é o **oposto** do que o grupo quer (frenético, cheio de combos): serve de contraexemplo.

### RYL: Path of the Emperor (Risk Your Life, 2002)

- **Famoso por:** **agro longo** (juntar muitos inimigos e derrotar todos de uma vez com skills em área) e guerra entre nações.
- **Pegar:** o farm em grupo puxando hordas, base para "farm de mapa em grupo" e para os inimigos em massa do brainstorm.

### Runescape (Jagex, 2001)

- **Famoso por:** **todo item tem uso** (um material de nível baixo continua útil em receitas de nível alto); muitas habilidades de coleta e produção; mercado central (Grand Exchange).
- **Pegar:** a cadeia de receitas que mantém itens baixos valiosos, ligando Itens, Profissões e Economia.

### Valheim (Iron Gate, 2021)

- **Famoso por:** sobrevivência e construção livre em grupo pequeno, com bosses que marcam a progressão.
- **Pegar (depois da fatia jogável):** construção e sobrevivência para grupo pequeno, se o grupo decidir por elas (S5 e S7 da [validação](brainstorm/validacao.md)).

## Como usar uma referência

1. Jogue ou assista o sistema na referência, com foco no que o grupo quer copiar.
2. Escreva no `docs/features/<x>.md`: "Base: <jogo>. Igual: ... Diferente: ...".
3. Na hora de implementar, comece pelo mínimo que reproduz a referência; as diferenças entram depois, uma de cada vez.

## Projetos e assets de exemplo (código)

Para não começar do zero no Unity, vale avaliar pontos de partida oficiais e gratuitos antes de escrever do zero. Exemplo: o **Starter Assets – Third Person** da Unity (controle de personagem em 3ª pessoa com câmera). Qualquer asset novo é decisão do core: entra num PR próprio e, se for estrutural, com ADR.

## Fontes

Pesquisa de 2026-10-10. Números de balanceamento mudam a cada temporada; confira a fonte antes de copiar um valor.

- New World: [Fextralife: Combat](https://newworld.wiki.fextralife.com/Combat), [Vulkk: como funciona o combate](https://vulkk.com/2021/09/28/how-combat-works-in-new-world-a-beginners-guide/), [exploit de invencibilidade](https://techraptor.net/gaming/news/game-breaking-new-world-invincibility-exploit-discovered), [duplicação de ouro](https://www.pcgamesn.com/new-world/gold-duping-bug)
- Elden Ring: [Fextralife: Combat](https://eldenring.wiki.fextralife.com/Combat)
- Throne and Liberty: [AlcastHQ: por que é sem classes](https://alcasthq.com/why-throne-and-liberty-is-a-classless-game-tl-mmo-insights/), [pares de armas e papéis](https://guildorder.com/ru/games/throne_and_liberty/wiki/weapon-pairs-and-role-composition)
- Albion Online: [guia de equipamento](https://heaven-guardian.com/albion-online-gear-guide/), [Destiny Board](https://heaven-guardian.com/albion-online-destiny-board-guide/)
- Guild Wars 2: [wiki oficial: Combat](https://wiki.guildwars2.com/wiki/Combat)
- World of Warcraft: [warcraft.wiki.gg: Mythic+](https://warcraft.wiki.gg/wiki/Mythic%2B), [afixos](https://warcraft.wiki.gg/wiki/Affix)
- Path of Exile: [poewiki: gemas de skill](https://www.poewiki.net/wiki/Skill_gems), [encaixes e ligações](https://www.poewiki.net/wiki/Link)
