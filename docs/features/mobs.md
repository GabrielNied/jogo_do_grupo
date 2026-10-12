# Mobs e bosses

**Responsável:**
**Status:** em definição
**Faixa de IDs de mensagem:** 11000–11999

## Resumo

Tipos de mobs, comportamento por região e bosses com mecânicas marcantes.

## Definido

- Regras iniciais da primeira fatia jogável (números em proposta): [fatia-jogavel.md](fatia-jogavel.md)
- Mobs por região
- Bosses com mecânicas marcantes e **ataques telegrafados** (veja [combate.md](combate.md)); **bosses cada vez mais fortes são o objetivo do loop principal** (D5 da [validação de 2026-10-10](../brainstorm/validacao.md))
- Referência de agro: RYL: Path of the Emperor (agro longo, juntar muitos inimigos e derrotar de uma vez); veja S3 na validação
- **Três categorias de criatura** (pedido do Gabriel em 2026-10-11; os números são **proposta**, veja "Regras"):
  - **Comum:** pode aparecer **fortalecida**: mais forte, maior e com **aura vermelha**, e dá mais recompensa.
  - **Chefe:** sempre aparece e sempre com a mesma força. Nunca vem fortalecido.
  - **Chefe raro:** força fixa como a do chefe, mas **pode aparecer ou não**, no estilo dos raros de World of Warcraft.

## Em aberto

- [ ] Bosses de mundo aberto (world boss)?
- [ ] Respawn: tempo fixo ou dinâmico?
- [ ] O nome "fortalecida" fica, ou o grupo prefere outro (elite, enfurecida)?
- [ ] Os números da criatura fortalecida e do chefe raro (tabelas em "Regras").
- [ ] As três primeiras criaturas (Javali do Campo, Guardião do Reflexo, Presa-Velha) são só um ponto de partida: nomes, espécies e onde ficam.
- [x] **Livro de Asterra, decidido em 2026-10-11 (/decida, #322):** o Javali do Campo **permanece** (dano 40, grupo de 3 a 5, já implementado); o Javali Ferrugem do livro (dano 18, grupo de 8 a 15) só entra depois, como espécie à parte. Lobo de Geada, Carapaça de Brasa, Cervo e Mariposa viram fichas em `Config<Assunto>`, de força fixa, fora do campo da fatia até o foco do marco mudar. Ancião e Colosso só com o chefe de mundo (#191). Bestiário e captura ficam para depois do MVP. As manadas com rota e limite urbano estão feitas (issue #321: veja "Manadas com rota (#321)"). Pergunta original: o Javali do Campo vira o Javali Ferrugem (dano 18, grupo de 8 a 15) ou os dois convivem? As cinco espécies, o bestiário com conhecimento e a captura do cervo entram no MVP? As manadas que nunca entram na cidade e seguem rotas? Veja "Livro de Asterra: bestiário".
- [ ] O chefe raro avisa quando aparece (mensagem na área, marca no mapa) ou o jogador tem que achar?
- [ ] O chefe raro some sozinho se ninguém o matar? Proposta atual: fica até morrer.
- [ ] Dungeon tem chefe raro (um sorteio ao criar a instância)? As regras já servem; falta decidir.
- [ ] Loot próprio do chefe raro (item que só ele larga): depende de [itens.md](itens.md).
- [ ] As escolhas do servidor do inimigo comum (lista em "Inimigo comum da fatia"): o grupo perder o alvo de vista na desistência, o golpe pegar todos no volume de contato e o inimigo ser imune na volta. O ataque ir até o fim quando o alvo foge, e ser cancelado por um acerto no preparo, ficou decidido em 2026-10-12.
- [x] ~~O tamanho do grupo do campo é sorteado de novo a cada volta do grupo inteiro?~~ Sim (issue #452, proposta): veja "Mais mobs no campo".
- [ ] Os pontos, níveis, tamanhos e as duas espécies novas (Lobo do Campo, Tatu Rochoso) de "Mais mobs no campo" são proposta do agente (issue #452): o dono confirma ou troca, alinhado ao bestiário ([#322](https://github.com/GabrielNied/jogo_do_grupo/issues/322)).

## Regras

Tudo abaixo é **proposta** de 2026-10-11, a calibrar. Quem decide é o **servidor**: ele sorteia a variante e a aparição, e o client só mostra. Força sempre **fixa**: nenhuma criatura escala com o número ou o nível dos jogadores ([fatia-jogavel.md](fatia-jogavel.md), seções 4 e 7).

### Criatura fortalecida

Só a criatura **comum**. O sorteio acontece uma vez, quando ela aparece (inclusive no respawn), e vale até ela morrer.

| Item | Valor |
|---|---|
| Chance de vir fortalecida | **5%** (1 em 20) |
| Vida | **2x** |
| Dano | **1,5x** (meio arredonda para cima) |
| Tamanho do modelo | **1,3x** |
| Aura | vermelha |
| Recompensa | **2x**: experiência em dobro e o dobro de sorteios de loot |

- O loot é da funcionalidade [Itens](itens.md): Mobs só informa o multiplicador de recompensa na morte.
- Desistir da perseguição e voltar ao ponto de origem **não** sorteia de novo: a fortalecida continua fortalecida.

### Chefe e chefe raro

- **Chefe:** sempre aparece (chance 100%), com os valores de base. O de dungeon não volta sozinho: só numa instância nova.
- **Chefe raro:** tem um ponto de aparição no mundo. A cada **intervalo** o servidor sorteia a **chance** de ele aparecer.
  - Não apareceu: só tenta de novo no intervalo seguinte.
  - Apareceu: fica no mundo até morrer; enquanto isso não há sorteio.
  - Morreu: a espera recomeça a partir da morte.
  - O primeiro sorteio é quando o servidor sobe. Se o servidor ficar um tempo sem sortear, os intervalos perdidos **não** acumulam: é um sorteio só.
- Com chance de 20% e intervalo de 30 minutos, o chefe raro aparece em média a cada 2 h 30 min, mas pode vir em sequência ou demorar bem mais.

### Primeiras criaturas

| Id | Nome | Categoria | Nível | Vida | Dano | XP | Aparece | Intervalo |
|---|---|---|---|---|---|---|---|---|
| 1 | Javali do Campo | comum | 3 | 180 | 40 | 20 | sempre | volta em 120 s |
| 2 | Lobo do Campo | comum | 4 | 110 | 28 | 18 | sempre | volta em 120 s |
| 3 | Tatu Rochoso | comum | 5 | 320 | 55 | 30 | sempre | volta em 150 s |
| 100 | Guardião do Reflexo | chefe | 5 | 6000 | 120 | 500 | sempre | não volta (dungeon) |
| 200 | Presa-Velha | chefe raro | 5 | 1500 | 80 | 200 | 20% por sorteio | sorteio a cada 30 min |

- O Javali do Campo usa os números do inimigo comum da fatia (seção 4 e ficha da issue #63).
- Faixas de id: comuns de 1 a 99, chefes de 100 a 199, chefes raros de 200 a 299. Id publicado não muda nem é reaproveitado.

### Inimigo comum da fatia (issue #63)

As regras de base estão em [fatia-jogavel.md](fatia-jogavel.md), seção 4. O que a ficha da issue #63 acrescentou, e o que faltava para o contrato, fica aqui. Tudo **proposta a calibrar**, em constantes de `RegrasInimigo` (vida, dano e tempo de volta vêm da tabela acima).

| Item | Valor |
|---|---|
| Criatura | Javali do Campo (id 1), em grupos de 3 a 5 |
| Onde | campo aberto, a uns 150 m do portão norte da cidade (decidido em 2026-10-11); a dungeon reaproveita o mesmo inimigo, sem volta |
| Visão | vê o jogador a **12 m** |
| Alerta | **0,5 s** entre ver e começar a perseguir; o grupo todo entra em alerta junto |
| Perseguição | **4 m/s**, até o posto dele no cerco (veja abaixo; mais lento que o jogador correndo) |
| Ataque | começa a até **2 m** do alvo; **1 s** de preparo (a animação), o contato no fim dele, e **1,5 s** de recuperação |
| Contato do golpe | ver [abaixo](#contato-do-golpe); o círculo de 1,5 m no chão, travado no início do aviso, saiu em 2026-10-12 |
| Desistência | a mais de **30 m** do ponto de origem ele volta (a **6 m/s**), com a vida cheia, sem escolher alvo até chegar |
| Corpo | raio de **0,6 m** como alvo dos golpes dos jogadores |
| Atualização da IA | passo fixo, **10 vezes por segundo** |

- **Estados:** repouso → alerta → perseguição ⇄ ataque; alerta, perseguição e ataque → retorno → repouso; qualquer estado vivo → morto. Morto não muda mais: a volta é um inimigo novo (`InimigoApareceu`), com a variante sorteada de novo.
- **Alvo:** o jogador mais próximo que ele viu. Enquanto esse alvo continuar valendo, ele **não troca**, mesmo com outro mais perto. No empate de distância fica o de menor id. Depois do primeiro dano, o alvo segue a tabela de ameaça adotada em 2026-10-12 ([combate.md](combate.md#ameaça-agro-169), #169).
- **Golpe telegrafado:** quem estiver no volume de contato quando o preparo acaba leva o dano; quem saiu a tempo não leva (a esquiva de 5 m tira o jogador da frente do modelo).
- Sem desvio de obstáculos na fatia: o campo é aberto.
- **Cerco em anel (issue #487; proposta, a calibrar):** o grupo cerca o alvo em vez de empilhar. Cada inimigo tem um posto fixo (pelo número dele no grupo) num anel de **1,9 m** em volta do alvo, com os postos igualmente espaçados; ninguém chega a menos de **1,2 m** (2x o raio do corpo) de outro, e quem chegaria é empurrado para fora. No máximo **2** inimigos atacam o mesmo alvo ao mesmo tempo (preparando o golpe ou se recuperando); os outros **rondam** num anel de **3,1 m**, fora do alcance, e quem ronda não conta no limite de 8 s sem atacar (esperar vaga não é kite). Quando uma vaga abre, o próximo a passar pelo passo da IA (por ordem no grupo) vai ao anel de dentro. Valores em `RegrasCerco` (shared). Fica de fora: desvio de obstáculo e escolha do posto mais perto do lado de onde o inimigo vem (o ângulo é fixo por inimigo, então caminhos podem se cruzar).

O que o servidor acrescentou ao implementar (issue #268; **proposta**, decidido pelo loop autônomo em 2026-10-11, a confirmar no review):

- **O grupo divide o que vê:** o jogador que um inimigo viu vale para todos, e é assim que o grupo acorda junto. Quem sai da área deixa de ser visto, e quando ninguém do grupo está em alerta, perseguindo ou atacando, o grupo esquece todos (quem voltou para longe não é perseguido pelo inimigo que reaparece).
- **Desistência:** quando um inimigo passa dos 30 m e desiste, o grupo **perde o alvo dele de vista**; só volta a persegui-lo se alguém do grupo o vir de novo (a 12 m). Sem ninguém à vista, quem ainda perseguia também volta.
- **O ataque vai até o fim** se o alvo fugir: depois do aviso, o inimigo dá o golpe e cumpre a recuperação mesmo que o alvo saia da área. Um acerto de jogador no preparo cancela o golpe do inimigo comum; o chefe não é cancelado. A duração da reação está em [combate](combate.md#reação-ao-acerto).
- **O golpe pega todos** os personagens que estão no volume de contato quando o preparo acaba, não só o alvo. Conta o centro do personagem (sem raio de corpo).
- **Na volta ele é imune** ("Evade": o dano é recusado; revisto na issue #488, antes levava dano e chegava ferido). Também desiste quando passa 8 s perseguindo sem conseguir atacar (kite na borda do alcance; `ConfigInimigosDoCampo.LimiteSemAtacarSegundos`).
- **Onde:** o centro do grupo fica em (0, 190), 150 m ao norte do portão (que está em (0, 40)), com os pontos de origem num círculo de 4 m de raio. O campo faz parte da área `cidade`. Este é o primeiro de vários pontos: veja "Mais mobs no campo" (o tamanho do grupo é sorteado de novo a cada reaparição). O comando de teste `spawn` ([Desenvolvimento](desenvolvimento.md)) acrescenta um inimigo a mais no maior grupo da área, sem esperar a volta.
- **Id do inimigo:** começa em 1.000.000.000.000 e soma 1; cada volta depois de morto é um inimigo novo, com id novo. Nunca coincide com id de personagem nem com o boneco de treino.
- **Dano no jogador:** Mobs decide **quem** o golpe pegou e publica `GolpeDeInimigoAcertou`; tirar a vida é de quem é dono dela ([Combate](combate.md)), que assina o evento e tira a vida.
- **Quem participou da morte:** quem causou dano ao inimigo desde que ele apareceu, em ordem crescente de id (`InimigoDerrotado.Participantes`; o Combate informa o atacante em `IAlvosDeCombate.TentarAplicarDano`). A lista zera quando ele desiste e volta (a vida também volta ao máximo) e quando ele reaparece. Quem só foi alvo do inimigo, sem causar dano, não participa.

### Contato do golpe

Decidido em 2026-10-12. O golpe comum (javali, lobo, tatu e quem usa a mesma IA, inclusive o chefe raro) deixa de ser um círculo no chão separado do modelo. O dano sai no fim do preparo, no instante em que a animação encosta.

- No começo do preparo a direção trava. Durante o preparo o inimigo dá um passo de até **1,0 m** nessa direção (`PassoDoGolpeMetros`), sem atravessar parede nem chegar a menos de 1,2 m de outro inimigo. Se o passo para antes, o contato é medido de onde ele parou.
- No fim do preparo o volume é um cone à frente do corpo: **1,2 m** a partir do centro e **90°** (`AlcanceDoGolpeMetros`, `AberturaDoGolpeGraus`), na direção travada. Não fica desenhado no chão e não fica preso na posição do início do aviso.
- O client toca o clipe `Atacar` na velocidade que faz a pose de contato cair nesse mesmo instante. O aviso visível é o preparo do modelo, não um decalque.
- Investida, pisada e raios do chefe de dungeon continuam com a área marcada no chão, fixa desde o aviso.
- Para implementar: a conta do cone reaproveita `RegrasAtaque.EstaNoVolume`, no instante do contato. Sem colisor de malha e sem física no servidor. O círculo antigo não volta. O client não manda "acertou" nem "errou"; quem decide o contato é o servidor. O corte entre animação e resultado está em [combate](combate.md#o-que-o-client-mostra-e-o-que-o-servidor-decide).

O chefe raro no campo (issue #310; **proposta**, a confirmar no review):

- **Onde:** a Presa-Velha tem um ponto próprio no campo, com centro em (-60, 230) na área `cidade` (ele nasce 4 m ao norte do centro, como o inimigo de um grupo de um; uns 60 m a oeste do grupo de javalis, fora da visão deles). Valores em `ConfigInimigosDoCampo`.
- **Como aparece:** o servidor sobe sem o chefe; o `AparicaoDeChefeRaro` faz o primeiro sorteio no primeiro passo e depois um por intervalo (1800 s, chance de 20%). Quando sorteia, ele chega como `InimigoApareceu`, com `Variante` normal, e fica até morrer, sem novo sorteio. A morte manda `InimigoMorreu` e `InimigoDerrotado` como a de qualquer inimigo e recomeça a espera; a volta é um inimigo novo (id novo), por sorteio, nunca por tempo fixo.
- **IA:** a do inimigo comum, com vida (1500) e dano (80) da própria ficha. Quem entra na área recebe os inimigos vivos do campo, chefe incluído. Não há aviso de aparição (continua em aberto).

### Mais mobs no campo (#452)

**Proposta** do agente, a calibrar. Dados em `PontosDeSpawnDoCampo` (um ponto por linha), números em `ConfigNivelDeMobs`, `ComportamentoDeCriatura` e `PontosDeSpawnDoCampo`.

- **Espécies comuns** (comportamento distinto; o resto da IA, alcance, volume do contato e desistência, é igual para todas):

| Espécie | Jeito | Visão | Perseguição | Aviso / recuperação do golpe |
|---|---|---|---|---|
| Javali do Campo | equilibrado (o da fatia) | 12 m | 4 m/s | 1 s / 1,5 s |
| Lobo do Campo | rápido e frágil: vê longe, corre e golpeia depressa | 16 m | 5,5 m/s | 0,7 s / 1 s |
| Tatu Rochoso | lento e duro: vê pouco, golpeia forte e demora a recuperar | 8 m | 2,5 m/s | 1,4 s / 2 s |

- **Pontos de spawn** (issue #489): **15 pontos** na área `cidade`, mais longe da cidade, mais forte. Grandes: o campo norte (0, 190), o leste (130, 170) e o oeste (-150, 190), e os três "longe" (0, 330), (200, 300) e (-220, 280), mais a manada do pasto. Pequenos (2 a 3 inimigos, perto dos caminhos): `portao-oeste` (-80, 150), `portao-leste` (80, 150), `oeste-perto` (-180, 110), `leste-perto` (180, 100), `norte-meio` (0, 260), `oeste-meio` (-90, 260), `leste-meio` (95, 255) e `noroeste-longe` (-100, 330). Faixas de nível de 1 a 2 (perto) até 8 a 10 (longe). A tabela completa está no código (`PontosDeSpawnDoCampo`).
- **Meta de densidade:** de qualquer ponto do campo próximo (x entre -200 e 200, z de 40 a 340) há um ponto de spawn a no máximo **150 m**, cerca de 30 s correndo (5,5 m/s) descontada a visão do inimigo; saindo do portão norte, o grupo mais próximo é a manada do pasto, a uns 50 m. Os números moram em `PontosDeSpawnDoCampo`; um teste confere a grade do campo.
- **Reposição:** o grupo com **sobrevivente** repõe cada morto **individualmente** depois do intervalo da ficha contado da morte dele, no lugar onde ele estava (a manada, no ponto da rota para onde vai), com id e sorteio de nível e variante novos. Só repõe com o grupo **todo fora de luta** (repouso ou retorno) e com vaga no teto; em luta ou sem vaga espera e tenta de novo (a cada 5 s sem vaga). O grupo com **todos mortos** volta **inteiro** depois do intervalo da última morte, com o tamanho sorteado de novo na faixa do ponto. Inimigos do comando de teste não voltam.
- **Variação:** cada intervalo ganha uma folga sorteada de **0 a 25%** a mais (`ConfigReposicaoDeInimigos.VariacaoDoIntervalo`), para os grupos e os membros não voltarem juntos; o intervalo da ficha é o mínimo.
- **Nível conta** (só a criatura comum; chefes têm força fixa): em relação ao nível da ficha, cada nível acima soma **15%** de vida, **10%** de dano e **20%** de experiência (abaixo, tira; piso de 40%). A variante fortalecida multiplica por cima. O `InimigoNoMundo.Nivel` e o `InimigoDerrotado.Nivel` levam o nível; `InimigoDerrotado.Experiencia` já vem escalada (o ajuste por diferença de nível entre jogador e mob é de [Personagem](personagem.md)).
- **Limite de vivos:** no máximo **40** inimigos comuns do campo vivos ao mesmo tempo, somando todos os pontos. Os máximos dos pontos somam mais que isso (57), então o teto age: na subida do servidor os pontos enchem na ordem da lista (perto do portão primeiro) e os últimos esperam vaga. Sem vaga, o grupo (ou a parte que couber) espera e tenta de novo a cada 5 s. O chefe raro e os inimigos do comando de teste não contam.
- **Cuidados de experiência:** jogadores de qualquer número no mesmo grupo dividem o mesmo alvo (o grupo divide o que vê); quem cai ou desconecta deixa de ser alvo e o grupo volta ao repouso; o grupo reaparece nos pontos de origem, que são fixos (quem fica parado em cima de um ponto o vê reaparecer ao lado dele, como qualquer reaparição).

### Manadas com rota (#321)

**Proposta** do agente, a calibrar; números em `RegrasManada` (shared) e dados em `PontosDeSpawnDoCampo` e `RotaDaManada` (servidor). Faz a parte "manadas com rota" do Livro de Asterra para a criatura comum do campo.

- **Rota:** uma volta fechada de pontos (`TipoDePontoDeRota`: pasto, rio, repouso, retorno). Em repouso a manada espera em cada ponto (20 s; 10 s no retorno) e então anda para o próximo a **1,5 m/s**, cada javali no seu lugar do bando. Ela só parte quando todos estão em repouso: se uma luta atrasa alguém, os outros esperam. Não há estado novo: andar pela rota é `Repouso` com a posição mudando (o client anima a caminhada).
- **A manada do pasto:** 5 a 8 javalis de nível 1 a 2 entre a cidade e o campo: pasto (-40, 110), rio (40, 125), repouso (55, 90) e retorno (0, 85). Entra no teto de vivos do campo, que subiu de 30 para **40**.
- **Limite urbano:** a **zona urbana** é o quadrado da cidade (80 m) mais **10 m** de margem (|x| e |z| até 50 m). A manada nunca entra nela: quem está lá não é visto nem atacado (moradores, comerciantes e jogadores na cidade ficam a salvo). Se o alvo entra na zona, a perseguição **acaba na borda** e a manada volta; se o passo da perseguição cairia dentro dela, ela para e volta. Ponto de rota dentro da zona é recusado (`RotaDaManada.Validada`).
- **Limite de perseguição:** a mais de **80 m** do ponto de origem (o lugar dela no ponto da rota onde está), a manada desiste, volta e recupera a vida; o grupo perde o alvo de vista (a regra do inimigo comum, com 80 m em vez de 30 m).
- **Só a manada** tem rota e zona urbana: os outros grupos do campo seguem como antes. Os inimigos do comando de teste (`spawn`) não acompanham a rota.
- Fora desta tarefa: reação a luz e som, ponte de gelo, bestiário e captura (continuam proposta do livro).

### Boss da dungeon (#66)

O Guardião do Reflexo (id 100), o boss da primeira dungeon ([fatia-jogavel.md](fatia-jogavel.md), seção 7). Tudo abaixo é **proposta a calibrar**, em `ConfigChefeDaDungeon` (servidor); vida, XP e nível vêm da ficha da tabela de criaturas.

| Item | Valor |
|---|---|
| Força | **fixa**: 6000 de vida para 1 a 5 jogadores, nunca escala |
| Luta | desperta com um jogador a até **30 m** da origem; sala sem ninguém: ele volta, cura tudo e esquece quem participou |
| Ritmo | **0,5 s** de alerta, ataque, **2 s** de pausa, próximo ataque |
| Aviso | **3 s** em todo ataque; as áreas ficam fixas desde o aviso e só leva o dano quem está dentro de uma delas **no momento do acerto** (esquivar ou sair a tempo evita) |
| Ordem | investida, raios, pisada, e recomeça |
| Investida | área de **3 m** de raio no ponto onde o alvo (o mais próximo) estava ao começar o aviso, até **25 m** de alcance; no acerto o boss vai até lá. Dano **150**, recuperação **2 s** |
| Raios | **4** círculos de **2,5 m** de raio, um por jogador em rodízio; se faltam jogadores, os que sobram caem a **4 m** de um deles. Dano **80** por raio, recuperação **1,5 s** |
| Pisada | círculo de **9 m** de raio centrado no boss. Dano **100**, recuperação **2,5 s** |
| Janela exposta | abre no acerto da **investida** e da **pisada**, dura **5 s**, o dano que ele recebe é **x2** (raios não expõem) |
| Corpo | raio de **2 m** como alvo dos golpes dos jogadores |

- **Corpo no mesmo tempo da marca** (decidido em 2026-10-12): durante o aviso de 3 s o modelo faz o preparo daquele ataque, e o dano sai no fim dessa pose. A marca no chão continua. Até existir um clipe por ataque, o `Atacar` que já existe é alongado para durar o aviso. O dono de Mobs liga o tempo; clipe novo, quando faltar, fica com o `artista-personagens`.
- **Dano nos jogadores:** um `GolpeDeInimigoAcertou` por personagem atingido em cada ataque (mesmo em duas áreas), com o dano da tabela; o Combate é quem tira a vida.
- **Estados:** repouso, alerta, perseguição (aqui só "em combate", entre ataques: ele não anda), ataque, retorno, morto. Os estados são os do inimigo comum.
- **Morte:** publica `InimigoDerrotado` uma vez, com os participantes (quem causou dano desde que a luta começou; a lista zera se a sala esvazia) e `InimigoMorreu`. **Não volta sozinho**: só a instância nova cria outro boss (`IChefesDaDungeon.Criar`) ou `ChefeDaDungeon.Reiniciar`. Loot e XP são os de qualquer inimigo.
- **Onde:** a instância cria o boss pelo contrato `IChefesDaDungeon` (`server/.../Contratos/Mobs/`), um por área. Enquanto ela não existe, o servidor sobe com um **boss de teste** na área `cidade`, em (-200, 0) (`ConfigChefeDaDungeon.CriarChefeDeTesteAoSubir`; desligue quando a instância o criar).
- Fora desta tarefa: o desenho do salão, o client (aviso no chão e janela exposta) e loot próprio do boss.

### Livro de Asterra: bestiário (proposta do board, 2026-10-11)

Do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seções 05, 09 e 10). **Proposta do board, não validada pelo grupo**; não troca nenhuma ficha acima. Os números do livro são só referência.

- **Conflito com o já implementado:** o livro descreve o **Javali Ferrugem** (nível 3, vida 180, ataque-base 18, grupos de 8 a 15, perseguição de 180 m) como a primeira grande puxada. O **Javali do Campo** acima tem nível 3 e vida 180 como ele, mas dano 40 e grupo de 3 a 5. Não foi trocado; veja "Em aberto".
- **Espécies propostas** (nível, vida, grupo): Lobo de Geada (22, 1.400, 6 a 12; perseguição 240 m), Carapaça de Brasa (30, 3.200, 15 a 30), Cervo de Galhada Oca (12, 650, manadas de 60 a 150, não hostil, foge de fogo e ruído), Mariposa de Breu (10, 90, enxames de 20 a 50, atraída pela luz). Chefes de mundo: **Ancião Trovejante** (nível 45, vida 250.000, FUTURO) e **Colosso Basáltico** (nível 50, vida 350.000, FUTURO), hostis entre si. Todos de força fixa, sem escalar com jogadores (já é a regra do jogo).
- **Cinco espécies, um boss e uma dungeon** é o escopo do MVP no livro; o boss é o mineral da Mina do Reflexo (veja [instâncias](instancias.md)).
- **Reação a luz e som (proposta):** cada espécie tem resposta própria (mariposa busca luz, lobo exposto hesita, gigante vê clarões de longe, cervo foge de ruído).
- **Manadas com rota:** a primeira manada do campo está em "Manadas com rota (#321)". O resto do livro (perseguição de 180/240 m por espécie, ponte de gelo, confronto entre gigantes) continua proposta.
- **Bestiário (proposta):** o primeiro encontro libera silhueta, região e comportamento; derrotas e observações revelam ataques e fraquezas (ex.: 5 derrotas de javali revelam o sinal da investida, 20 o ponto fraco). Cada jogador registra as próprias observações. **Captura (proposta):** só espécies marcadas como capturáveis, com conhecimento avançado e isca. O primeiro companheiro, que segue e não luta, está em [pets](pets.md). Captura não vira pet de boss.
- **Loot por dano (proposta):** o item de cada mob vai a quem causou mais dano (empate: o primeiro atacante); não há prioridade de loot para cura ou tanque. Depende de [itens](itens.md) e é regra de combate/loot, não de Mobs.

## Depende de

- ~~Ainda nada no código. A IA, o dano e a posição no mundo dependem de [Combate](combate.md) (alvo e dano) e de [Mundo](mundo.md) (posição dos jogadores), que as issues #61 e #119 preparam.~~ Substituído em 2026-10-11: os dois contratos já existem no servidor (`Contratos/Combate/IAlvosDeCombate` e `Contratos/Mundo/IPosicoesDoMundo`). O inimigo da issue #63 vai usá-los; o contrato do shared (abaixo) ainda não depende de nenhuma outra funcionalidade.
- **Mundo** (desde a issue #268): a posição dos jogadores da área por `IPosicoesDoMundo`, o evento `JogadorEntrouNaArea` (para mandar `InimigosDaArea`) e a altura do chão por `MapaDoMundo.AlturaEm`.
- **Combate** (desde a issue #268): o grupo de inimigos é um `IAlvosDeCombate` (o `InimigoId` é o `AlvoId`) e usa o cone de `RegrasAtaque.EstaNoVolume`.

## Expõe

Código em `shared/Package/Runtime/Features/Mobs/` (regras e números acima, não copiados aqui). ~~Ainda **não há mensagens de rede**: as criaturas ainda não aparecem no mundo (issue #63).~~ Substituído em 2026-10-11: o contrato do inimigo comum está abaixo; o servidor e o client que o usam são as issues #268 e #269.

- `CategoriaDeCriatura`: `Comum`, `Chefe`, `ChefeRaro`.
- `VarianteDeCriatura`: `Normal`, `Fortalecida`.
- `DefinicaoDeCriatura` e `CatalogoCriaturas`: a ficha fixa de cada criatura e `Obter` por id.
- `RegrasCriaturas`: fórmulas puras (variante, aparição, vida, dano, experiência, multiplicador de recompensa e tamanho).

Inimigo comum (tempo real; regras em "Inimigo comum da fatia"):

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 11000 | `InimigosDaArea` | servidor → quem entra na área | `Inimigos[]` (`InimigoNoMundo`: `InimigoId`, `CriaturaId`, `Variante`, `Nivel`, `X`, `Y`, `Z`, `RotacaoY`, `Vida`, `VidaMaxima`, `Estado`, `AlvoId`); só os vivos |
| 11001 | `InimigoMudou` | servidor → quem está na área | `InimigoId`, `X`, `Y`, `Z`, `RotacaoY`, `Estado`, `AlvoId`, `Vida`, `Sequencia` (o client descarta a que não for maior que a última) |
| 11002 | `InimigoPreparaAtaque` | servidor → quem está na área | `InimigoId`, `PreparoMs`, `CentroX`, `CentroZ`, `Raio`, `DirecaoY`, `Tipo` (`TipoDeAtaqueDeInimigo`: `Golpe` 0, `Investida` 1, `Pisada` 2, `Raios` 3), `Areas[]` (`AreaDeAtaque`: `CentroX`, `CentroZ`, `Raio`; os campos soltos repetem a primeira) |
| 11003 | `InimigoMorreu` | servidor → quem está na área | `InimigoId` |
| 11004 | `InimigoApareceu` | servidor → quem está na área | `Inimigo` (`InimigoNoMundo`) |
| 11005 | `InimigoExposto` | servidor → quem está na área | `InimigoId`, `DuracaoMs`, `Multiplicador` (boss exposto a mais dano; acaba sozinho depois da duração) |

- `EstadoDeInimigo`: `Repouso` 0, `Alerta` 1, `Perseguicao` 2, `Ataque` 3, `Retorno` 4, `Morto` 5.
- `InimigoId` é o mesmo `AlvoId` que o Combate manda em `AcertoDeAtaque`: o dano de um golpe do jogador chega por `AtaqueAcertou`, não por `InimigoMudou`. `AlvoId` 0 (`RegrasInimigo.SemAlvo`) é sem alvo.
- `CriaturaId` aponta para o `CatalogoCriaturas` (nome, nível, categoria); a ficha da issue chamava esse campo de `Tipo`.
- `RegrasCerco`: o cerco em anel, a separação mínima e o limite de atacantes por alvo (issue #487).
- `RegrasManada` e `TipoDePontoDeRota`: a zona urbana, o limite de perseguição e os tipos de ponto da rota das manadas (issue #321).
- `RegrasInimigo`: constantes de balanceamento e fórmulas puras (`TransicaoValida`, `EstaVivo`, `ProcuraAlvo`, `TamanhoDoGrupo`, `Ve`, `NoAlcanceDeAtaque`, `DeveDesistir`, `EscolherAlvo`, `DirecaoPara`, `Avancar`, `CentroDoGolpe`, `EstaNaAreaDoGolpe`). O servidor decide; o client usa só para mostrar.

No servidor (`server/.../Features/Mobs/`): `SorteadorDeCriaturas` (sorteia a variante e a aparição) e `AparicaoDeChefeRaro` (o controle de um ponto de chefe raro). ~~Ainda não estão ligados ao mundo.~~ Substituído em 2026-10-11 (issue #268): o sorteador já sorteia a variante dos inimigos do campo; o chefe raro ficou sem ligação com o mundo até a issue #310, que ligou a Presa-Velha ao campo (seção "O chefe raro no campo", abaixo).

Inimigos do campo no servidor (issue #268): `GrupoDeInimigos` (a IA, sem rede, e o alvo dos golpes), `ServicoDeInimigos` (o laço de 10 passos por segundo, que envia as mensagens acima e publica os eventos) e `ConfigInimigosDoCampo` (ponto do grupo, espalhamento e id inicial).

Boss da dungeon no servidor: `ChefeDaDungeon` (a IA, sem rede), `ChefesDaDungeon` (um por área, o alvo dos golpes) e `ConfigChefeDaDungeon`; o contrato `IChefesDaDungeon` (`Criar(areaId, x, z)` e `Remover(areaId)`) fica em `server/.../Contratos/Mobs/`.

Eventos para as outras funcionalidades do servidor (`server/.../Contratos/Mobs/`, [ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)). Saem do laço de IA, não do laço de rede: o assinante precisa ser seguro entre threads e não bloquear.

| Evento | Quando | Campos |
|---|---|---|
| `GolpeDeInimigoAcertou` | o golpe de um inimigo pegou um personagem (um evento por personagem) | `InimigoId`, `CriaturaId`, `PersonagemId`, `Dano` (já com a variante, antes de defesa), `Area` |
| `InimigoDerrotado` | um inimigo morreu (uma vez por morte) | `InimigoId`, `CriaturaId`, `Variante`, `Experiencia`, `MultiplicadorDeRecompensa`, `Area`, `X`, `Z`, `Participantes`, `Nivel` |
