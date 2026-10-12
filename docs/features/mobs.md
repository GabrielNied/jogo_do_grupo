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
- [ ] **Livro de Asterra (proposta do board, 2026-10-11, não validada):** o Javali do Campo vira o Javali Ferrugem (dano 18, grupo de 8 a 15) ou os dois convivem? As cinco espécies, o bestiário com conhecimento e a captura do cervo entram no MVP? As manadas que nunca entram na cidade e seguem rotas? Veja "Livro de Asterra: bestiário".
- [ ] O chefe raro avisa quando aparece (mensagem na área, marca no mapa) ou o jogador tem que achar?
- [ ] O chefe raro some sozinho se ninguém o matar? Proposta atual: fica até morrer.
- [ ] Dungeon tem chefe raro (um sorteio ao criar a instância)? As regras já servem; falta decidir.
- [ ] Loot próprio do chefe raro (item que só ele larga): depende de [itens.md](itens.md).
- [ ] As escolhas do servidor do inimigo comum (lista em "Inimigo comum da fatia"): o grupo perder o alvo de vista na desistência, o ataque ir até o fim, o golpe pegar todos no círculo e o inimigo levar dano na volta.
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
| Perseguição | **4 m/s**, em linha reta (mais lento que o jogador correndo) |
| Ataque | começa a até **2 m** do alvo; **1 s** de aviso, depois o golpe e **1,5 s** de recuperação |
| Área do golpe | círculo de **1,5 m** de raio no chão, com o centro **1,5 m** à frente do inimigo; fica fixa desde o aviso |
| Desistência | a mais de **30 m** do ponto de origem ele volta (a **6 m/s**), com a vida cheia, sem escolher alvo até chegar |
| Corpo | raio de **0,6 m** como alvo dos golpes dos jogadores |
| Atualização da IA | passo fixo, **10 vezes por segundo** |

- **Estados:** repouso → alerta → perseguição ⇄ ataque; alerta, perseguição e ataque → retorno → repouso; qualquer estado vivo → morto. Morto não muda mais: a volta é um inimigo novo (`InimigoApareceu`), com a variante sorteada de novo.
- **Alvo:** o jogador mais próximo que ele viu. Enquanto esse alvo continuar valendo, ele **não troca**, mesmo com outro mais perto. No empate de distância fica o de menor id.
- **Golpe telegrafado:** quem estiver na área quando o aviso acaba leva o dano; quem saiu a tempo não leva (a esquiva de 5 m tira o jogador da área).
- Sem desvio de obstáculos na fatia: o campo é aberto.

O que o servidor acrescentou ao implementar (issue #268; **proposta**, decidido pelo loop autônomo em 2026-10-11, a confirmar no review):

- **O grupo divide o que vê:** o jogador que um inimigo viu vale para todos, e é assim que o grupo acorda junto. Quem sai da área deixa de ser visto, e quando ninguém do grupo está em alerta, perseguindo ou atacando, o grupo esquece todos (quem voltou para longe não é perseguido pelo inimigo que reaparece).
- **Desistência:** quando um inimigo passa dos 30 m e desiste, o grupo **perde o alvo dele de vista**; só volta a persegui-lo se alguém do grupo o vir de novo (a 12 m). Sem ninguém à vista, quem ainda perseguia também volta.
- **O ataque vai até o fim:** depois do aviso, o inimigo dá o golpe e cumpre a recuperação mesmo que o alvo fuja ou saia da área. Só a morte interrompe.
- **O golpe pega todos** os personagens que estão no círculo quando o aviso acaba, não só o alvo. Conta o centro do personagem (sem raio de corpo).
- **Na volta ele ainda leva dano** (só o morto não leva), mas não reage até chegar ao ponto de origem.
- **Onde:** o centro do grupo fica em (0, 190), 150 m ao norte do portão (que está em (0, 40)), com os pontos de origem num círculo de 4 m de raio. O campo faz parte da área `cidade`. Este é o primeiro de vários pontos: veja "Mais mobs no campo" (o tamanho do grupo é sorteado de novo a cada reaparição). O comando de teste `spawn` ([Desenvolvimento](desenvolvimento.md)) acrescenta um inimigo a mais no maior grupo da área, sem esperar a volta.
- **Id do inimigo:** começa em 1.000.000.000.000 e soma 1; cada volta depois de morto é um inimigo novo, com id novo. Nunca coincide com id de personagem nem com o boneco de treino.
- **Dano no jogador:** Mobs decide **quem** o golpe pegou e publica `GolpeDeInimigoAcertou`; tirar a vida é de quem é dono dela ([Combate](combate.md)), que ainda **não** assina o evento. Até lá o golpe é avisado e decidido, mas não tira vida.
- **Quem participou da morte:** quem causou dano ao inimigo desde que ele apareceu, em ordem crescente de id (`InimigoDerrotado.Participantes`; o Combate informa o atacante em `IAlvosDeCombate.TentarAplicarDano`). A lista zera quando ele desiste e volta (a vida também volta ao máximo) e quando ele reaparece. Quem só foi alvo do inimigo, sem causar dano, não participa.

O chefe raro no campo (issue #310; **proposta**, a confirmar no review):

- **Onde:** a Presa-Velha tem um ponto próprio no campo, com centro em (-60, 230) na área `cidade` (ele nasce 4 m ao norte do centro, como o inimigo de um grupo de um; uns 60 m a oeste do grupo de javalis, fora da visão deles). Valores em `ConfigInimigosDoCampo`.
- **Como aparece:** o servidor sobe sem o chefe; o `AparicaoDeChefeRaro` faz o primeiro sorteio no primeiro passo e depois um por intervalo (1800 s, chance de 20%). Quando sorteia, ele chega como `InimigoApareceu`, com `Variante` normal, e fica até morrer, sem novo sorteio. A morte manda `InimigoMorreu` e `InimigoDerrotado` como a de qualquer inimigo e recomeça a espera; a volta é um inimigo novo (id novo), por sorteio, nunca por tempo fixo.
- **IA:** a do inimigo comum, com vida (1500) e dano (80) da própria ficha. Quem entra na área recebe os inimigos vivos do campo, chefe incluído. Não há aviso de aparição (continua em aberto).

### Mais mobs no campo (#452)

**Proposta** do agente, a calibrar. Dados em `PontosDeSpawnDoCampo` (um ponto por linha), números em `ConfigNivelDeMobs`, `ComportamentoDeCriatura` e `PontosDeSpawnDoCampo`.

- **Espécies comuns** (comportamento distinto; o resto da IA, alcance, área do golpe e desistência, é igual para todas):

| Espécie | Jeito | Visão | Perseguição | Aviso / recuperação do golpe |
|---|---|---|---|---|
| Javali do Campo | equilibrado (o da fatia) | 12 m | 4 m/s | 1 s / 1,5 s |
| Lobo do Campo | rápido e frágil: vê longe, corre e golpeia depressa | 16 m | 5,5 m/s | 0,7 s / 1 s |
| Tatu Rochoso | lento e duro: vê pouco, golpeia forte e demora a recuperar | 8 m | 2,5 m/s | 1,4 s / 2 s |

- **Pontos de spawn** (área `cidade`; mais longe da cidade, mais forte): o campo norte (0, 190), o leste (130, 170) e o oeste (-150, 190) a uns 200 a 240 m, e os três "longe" (0, 330), (200, 300) e (-220, 280) a uns 330 a 360 m. Faixas de nível de 2 a 4 (perto) até 8 a 10 (longe); grupos de 3 a 5 (javali) e de 2 a 4 ou 2 a 3 (lobo, tatu). A tabela completa está no código.
- **O grupo volta inteiro:** o grupo só reaparece quando o último morre e passa o intervalo da ficha contado dessa morte. Nessa hora o tamanho é sorteado de novo na faixa do ponto, e cada inimigo sorteia o nível dele na faixa do ponto (e a variante fortalecida). Matar um de cada vez não repõe ninguém até limpar o grupo.
- **Nível conta** (só a criatura comum; chefes têm força fixa): em relação ao nível da ficha, cada nível acima soma **15%** de vida, **10%** de dano e **20%** de experiência (abaixo, tira; piso de 40%). A variante fortalecida multiplica por cima. O `InimigoNoMundo.Nivel` e o `InimigoDerrotado.Nivel` levam o nível; `InimigoDerrotado.Experiencia` já vem escalada (o ajuste por diferença de nível entre jogador e mob é de [Personagem](personagem.md)).
- **Limite de vivos:** no máximo **30** inimigos comuns do campo vivos ao mesmo tempo, somando todos os pontos. Sem vaga, o grupo (ou a parte que couber) espera e tenta de novo a cada 5 s. O chefe raro e os inimigos do comando de teste não contam.
- **Cuidados de experiência:** jogadores de qualquer número no mesmo grupo dividem o mesmo alvo (o grupo divide o que vê); quem cai ou desconecta deixa de ser alvo e o grupo volta ao repouso; o grupo reaparece nos pontos de origem, que são fixos (quem fica parado em cima de um ponto o vê reaparecer ao lado dele, como qualquer reaparição).

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
- **Manadas com rota (proposta):** pontos de rota (pasto, rio, repouso, retorno), sem ficar parada esperando o jogador. Provocada, persegue até a distância-padrão da espécie (180/240 m, valores de referência) e volta ao perder o alvo ou passar do limite. **Nunca entra na cidade** (encerra a perseguição na borda; não ataca moradores ou comerciantes). Pode cruzar ponte de gelo. Confronto entre gigantes só em zona selvagem que ambos alcançam.
- **Bestiário (proposta):** o primeiro encontro libera silhueta, região e comportamento; derrotas e observações revelam ataques e fraquezas (ex.: 5 derrotas de javali revelam o sinal da investida, 20 o ponto fraco). Cada jogador registra as próprias observações. **Captura:** só espécies marcadas como capturáveis, com conhecimento avançado e isca; o primeiro companheiro é o cervo jovem (fora do combate, sem ser montaria). Captura não vira pet de boss.
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
- `RegrasInimigo`: constantes de balanceamento e fórmulas puras (`TransicaoValida`, `EstaVivo`, `ProcuraAlvo`, `TamanhoDoGrupo`, `Ve`, `NoAlcanceDeAtaque`, `DeveDesistir`, `EscolherAlvo`, `DirecaoPara`, `Avancar`, `CentroDoGolpe`, `EstaNaAreaDoGolpe`). O servidor decide; o client usa só para mostrar.

No servidor (`server/.../Features/Mobs/`): `SorteadorDeCriaturas` (sorteia a variante e a aparição) e `AparicaoDeChefeRaro` (o controle de um ponto de chefe raro). ~~Ainda não estão ligados ao mundo.~~ Substituído em 2026-10-11 (issue #268): o sorteador já sorteia a variante dos inimigos do campo; o chefe raro ficou sem ligação com o mundo até a issue #310, que ligou a Presa-Velha ao campo (seção "O chefe raro no campo", abaixo).

Inimigos do campo no servidor (issue #268): `GrupoDeInimigos` (a IA, sem rede, e o alvo dos golpes), `ServicoDeInimigos` (o laço de 10 passos por segundo, que envia as mensagens acima e publica os eventos) e `ConfigInimigosDoCampo` (ponto do grupo, espalhamento e id inicial).

Boss da dungeon no servidor: `ChefeDaDungeon` (a IA, sem rede), `ChefesDaDungeon` (um por área, o alvo dos golpes) e `ConfigChefeDaDungeon`; o contrato `IChefesDaDungeon` (`Criar(areaId, x, z)` e `Remover(areaId)`) fica em `server/.../Contratos/Mobs/`.

Eventos para as outras funcionalidades do servidor (`server/.../Contratos/Mobs/`, [ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)). Saem do laço de IA, não do laço de rede: o assinante precisa ser seguro entre threads e não bloquear.

| Evento | Quando | Campos |
|---|---|---|
| `GolpeDeInimigoAcertou` | o golpe de um inimigo pegou um personagem (um evento por personagem) | `InimigoId`, `CriaturaId`, `PersonagemId`, `Dano` (já com a variante, antes de defesa), `Area` |
| `InimigoDerrotado` | um inimigo morreu (uma vez por morte) | `InimigoId`, `CriaturaId`, `Variante`, `Experiencia`, `MultiplicadorDeRecompensa`, `Area`, `X`, `Z`, `Participantes`, `Nivel` |
