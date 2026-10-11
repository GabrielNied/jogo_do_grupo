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
- [ ] As regiões são mapas fixos, gerados ou uma mistura? Em 2026-10-11 o terreno aberto passou a ser **gerado por semente fixa** (sempre o mesmo mundo; [ADR 0020](../decisoes/0020-mapa-do-mundo-no-shared.md), proposta). Lugares feitos à mão por cima dele continuam em aberto.
- [x] ~~Tamanho do mapa e quantidade de regiões. A primeira área (cidade e arredores) ficou em cerca de 1 km com três áreas (campo, mata e colinas); o resto do mundo continua em aberto.~~ Decidido em 2026-10-11 (Gabriel, issue #135): **3 km de lado e sete biomas**. Veja "Biomas e relevo".
- [ ] A lava causa dano? O pântano deixa o jogador mais lento? Frio e calor têm efeito? Hoje os biomas são só cenário.
- [ ] Quais inimigos vivem em cada bioma?
- [ ] Como funciona a viagem rápida?
- [ ] Clima dinâmico afeta gameplay?
- [ ] Luz e som como mecânica (noite escura, skills que iluminam)? Veja S8 na validação.
- [ ] **Livro de Asterra (proposta do board, 2026-10-11, não validada):** o grupo adota os nomes de região e de lugar (Vale das Fogueiras, Mata do Eco, Serra do Vidro Frio, Caldeira Rubra, Planalto Astral, Brasa Mansa...)? Como encaixam nos sete biomas de 3 km já decididos (o livro não tem deserto nem pântano e tem o Planalto Astral)? Veja "Livro de Asterra: regiões e cidade".
- [ ] Ciclo de 40 min de dia e 20 min de noite (proposta do livro): vale? Liga com a #163.
- [ ] Bases e construção em pontos marcados: FUTURO, fora do MVP.

## Regras

### Hub inicial (arte)

Primeira área jogável (issue #36): o Core chama o construtor do Mundo ao entrar ([Entrada no mundo](../arquitetura.md#entrada-no-mundo)) e o jogador nasce nela. ~~Sem rede.~~ A cidade é área do mundo persistente (D1 opção A, 2026-10-11). Esta primeira montagem ainda não sincroniza outros jogadores.

- **Montagem:** por código em tempo de execução (`ConstrutorHub`, registrado com `EntradaNoJogo.DefinirConstrutorDeMundo`), a partir dos prefabs de `Features/Mundo/Resources/Mundo/`. Os prefabs são gerados por script de editor (`GeradorDePecasDoHub`, menu *Jogo do Grupo > Mundo > Gerar peças do hub*) a partir do Kenney "Fantasy Town Kit" (CC0, em `ThirdParty/Kenney/`). O script é reproduzível: rodar de novo regenera tudo.
- ~~**Grade:** **módulo de 2 m** (peça Kenney de 1 m × 2). Peças modulares ficam em células de 2 × 2 m, com origem no centro da base.~~ Substituído em 2026-10-11: **módulo de 4 m** (peça Kenney de 1 m × 4). Peças modulares ficam em células de 4 × 4 m, com origem no centro da base. Os prefabs já gerados podem estar no módulo antigo; o `ConstrutorHub` mede o chão e amplia as peças até o layout. Rodar de novo o gerador grava o módulo novo e a ampliação passa a ser 1.
- ~~**Tamanho:** quadrado de **40 × 40 m** (20 × 20 células) centrado na origem, chão com a face de cima em Y = 0. Muros de 1 célula (2 m) de espessura e 3 m de altura em volta, com torres de 6 m nos 4 cantos. **Área andável: 36 × 36 m** (de -18 a +18 em X e Z). Medidas em `LayoutDoHub`.~~ Substituído em 2026-10-11: quadrado de **80 × 80 m** (20 × 20 células) centrado na origem, chão com a face de cima em Y = 0. Muros de 1 célula (4 m) de espessura e 6 m de altura em volta, com torres de 12 m nos 4 cantos. **Área andável: 72 × 72 m** (de -36 a +36 em X e Z). A porta fica acima da pessoa (1,8 m). Medidas em `LayoutDoHub`.
- ~~**Disposição:** praça de pedra de 12 × 12 m no centro com a fonte, duas estradas em cruz de 4 m de largura até os muros, bancos, lanternas (luz de ponto), 4 barracas, 2 carroças, 12 árvores e 4 pedras. Uma luz direcional (sol) e luz ambiente suave.~~ substituída em 2026-10-11 pela disposição abaixo (mais ruas e casas; o resto, como a luz, continua).
- ~~**Disposição (2026-10-11):** praça de pedra de 12 × 12 m no centro com a fonte, duas estradas em cruz de 4 m de largura até os muros e uma **rua em anel** de 2 m de largura (em `x` ou `z` = ±11) ligando as duas estradas. Em volta, **20 casas** do Kenney Fantasy Town Kit (paredes de painel, porta, janelas e telhado de quatro águas; 4 × 4 m ou 6 × 4 m, estilo pedra ou madeira por quadrante, de porta para a praça ou para a rua), bancos, lanternas (luz de ponto), 4 barracas, 2 carroças, 2 pedras e 24 árvores. Uma luz direcional (sol) e luz ambiente suave. As posições estão em `LayoutDoHub.Props`.~~ Substituído em 2026-10-11 (mesma disposição, o dobro da escala): praça de pedra de 24 × 24 m no centro com a fonte, duas estradas em cruz de 8 m de largura até os muros e uma **rua em anel** de 4 m de largura (em `x` ou `z` = ±22) ligando as duas estradas. Em volta, **20 casas** do Kenney Fantasy Town Kit (paredes de painel, porta, janelas e telhado de quatro águas; 8 × 8 m ou 12 × 8 m, estilo pedra ou madeira por quadrante, de porta para a praça ou para a rua), bancos, lanternas (luz de ponto), 4 barracas, 2 carroças, 2 pedras e 24 árvores. Uma luz direcional (sol) e luz ambiente suave. As posições estão em `LayoutDoHub.Props`.
- **Disposição (2026-10-11, issue #156):** mantém a praça, a cruz e o anel, e acrescenta **ruas laterais** (em `x` ou `z` = ±14) ligando a praça ao anel, **mercado** a leste e oeste da praça (barracas e carroça) (só o chão de pedra: as barracas, os quarteirões extras e as duas pedras grandes do portão saíram em 2026-10-11, porque ficavam em cima das casas, da rua em anel e do vão do portão). Fora dos muros, a faixa até 130 m deixa de ser um disco plano: a **estrada do portão sobe devagar** (`MapaDoMundo.SubidaMaximaDaFaixaInicial` = 5 m). Posições em `LayoutDoHub.Props`; relevo em `MapaDoMundo`.
- ~~**Terreno de fora (2026-10-11):** grama de **120 × 120 m** com a cidade no meio (prefab `Terreno_Fora`, face de cima em Y = 0, colisor de 1 m de espessura), com cerca de 46 árvores e pedras espalhadas por uma semente fixa (sempre o mesmo resultado; `LayoutDoHub.PropsFora`), a pelo menos 8 m dos muros e fora do corredor da estrada do portão. Barreiras invisíveis de 20 m de altura na borda seguram o jogador no terreno.~~ ~~Substituído em 2026-10-11: terreno de **1 km × 1 km** (1000 × 1000 m) com a cidade no meio, montado em código pelo `ConstrutorHub` (face de cima em Y = 0, um colisor de 1 m de espessura). Três áreas: **campo** ao norte (chão claro, árvores afastadas; a estrada do portão sai para ele), **mata** a leste e no sul leste (chão escuro, árvores próximas) e **colinas** a oeste e no sul oeste (chão seco, mais pedra que árvore). As peças vêm de uma semente fixa (`LayoutDoHub.PropsFora`), a pelo menos 24 m dos muros e fora do corredor da estrada. Barreiras invisíveis de 20 m de altura na borda do quilômetro seguram o jogador. O prefab `Terreno_Fora` continua no disco; o jogo não o instancia mais, para o tamanho e as cores não ficarem gravados nele.~~ Substituído em 2026-10-11 pelo mundo de 3 km com relevo: veja "Biomas e relevo" abaixo. Campo, mata e colinas continuam em volta da cidade, com as mesmas peças (`LayoutDoHub.PropsFora`).
- ~~**Portão (2026-10-11):** vão de **4 m** (duas células) no meio do muro **norte**, sem muro e sem colisor, ladeado por duas torres de 6 m (as células vizinhas). A estrada de 4 m de largura atravessa o vão e segue 20 m pelo terreno de fora. O nascimento (ao sul) olha para a fonte e, atrás dela, para o portão.~~ Substituído em 2026-10-11: vão de **8 m** (duas células) no meio do muro **norte**, sem muro e sem colisor, ladeado por duas torres de 12 m (as células vizinhas). A estrada de 8 m de largura atravessa o vão e segue 40 m pelo campo. O nascimento (ao sul) olha para a fonte e, atrás dela, para o portão.
- **Colisão:** chão, terreno de fora, muros, torres, casas e todos os props têm colisor; as estradas, a água e a lava, não (o chão já colide). O `CharacterController` do jogador tem 1,8 m de altura e 0,35 m de raio.
- ~~**Nascimento:** em (0, 0, -12), ao sul da fonte, olhando para ela (+Z), em área livre (nenhum prop a menos de 2,5 m). O teste EditMode confere que está dentro dos limites do chão, com os pés na superfície e fora de qualquer colisor.~~ Substituído em 2026-10-11: em (0, 0, -24), ao sul da fonte, olhando para ela (+Z), em área livre (nenhum prop a menos de 2,5 m). O teste EditMode confere que está dentro dos limites do chão, com os pés na superfície e fora de qualquer colisor.
- **Prefabs ausentes:** peça sem prefab é pulada com aviso; só o chão e os muros têm uma reserva (cubos), para o hub nunca ficar sem colisão.

### Biomas e relevo

Decidido pelo Gabriel em 2026-10-11 (issue #135). Substitui o terreno plano de 1 km. A decisão técnica está no [ADR 0020](../decisoes/0020-mapa-do-mundo-no-shared.md).

- **Tamanho:** quadrado de **3 km × 3 km** (de -1500 a +1500 m em X e Z), com a cidade no centro. Barreiras invisíveis na borda.
- **Biomas (sete):**

  | Bioma | Onde | Chão | Relevo | Peças |
  |---|---|---|---|---|
  | **Campo** | norte da cidade, até uns 500 m | verde claro | quase plano (até 1,5 m) | árvores afastadas |
  | **Mata** | leste da cidade, até uns 500 m | verde escuro | ondulado (até 3,5 m) | árvores próximas |
  | **Colinas** | oeste da cidade, até uns 500 m | terra seca | morros (até 14 m) | mais pedra que árvore |
  | **Deserto** | oeste do mundo | areia | dunas de 3 a 17 m | pedras e árvores secas |
  | **Montanhas de neve** | norte do mundo | branco, pedra nas encostas | picos de até uns 155 m, com vales | árvores altas até 85 m de altura, pedra acima |
  | **Montanhas de lava** | sul do mundo | pedra escura, brasa na beira da lava | picos de até uns 100 m, cortados por rios de lava | pedras e árvores queimadas |
  | **Pântano** | leste do mundo | lama | ilhas baixas e poças rasas (cerca de um terço alagado) | árvores tortas nas ilhas |

- **Fronteiras:** os biomas de fora começam a uns 500 m do centro e se dividem pelas diagonais do mapa. As fronteiras ondulam (não são retas) e os biomas se misturam numa faixa de dezenas de metros: a cor, a altura e a luz mudam aos poucos. Um morro baixo acompanha cada fronteira; é ele que impede a água do pântano de encontrar a lava.
- **Faixa plana:** até **130 m** do centro (medidos no eixo mais distante) o chão fica em **Y = 0**: a cidade, a estrada do portão e uma faixa em volta dos muros. O relevo cresce aos poucos dali até 230 m.
- **Altura do chão:** de **-2 a 220 m** em qualquer ponto do mundo. A água do pântano fica em **Y = -0,5** (poças de até 40 cm) e a lava em **Y = -1** (rios de até 60 cm). As duas são só visuais por enquanto: não têm colisor e **não causam dano nem deixam mais lento** (em aberto, acima).
- **Encostas:** o personagem sobe até 45 graus (regra do Core). Campo, mata e colinas são andáveis por inteiro. Nas montanhas, até um décimo do chão é íngreme demais: são os paredões, e o caminho é pelos vales.
- **Fonte única do relevo:** `MapaDoMundo`, no shared (`BiomaEm`, `PesosEm`, `AlturaEm`). Usa sementes fixas: o mundo é sempre o mesmo. A altura oficial é a de uma grade de 10 m, em triângulos, igual à malha que o client desenha.
- **Montagem no client:** o `ConstrutorDoTerreno` cria o chão em 225 blocos de 200 m (malha com colisor), pinta uma textura com a cor de cada ponto, e cria as malhas da água e da lava. Nada disso é cena ou asset: nasce ao entrar no mundo. O chão não existe sob a cidade (lá vale o chão dela).
- **Peças:** o miolo de 1 km continua com `LayoutDoHub.PropsFora`, agora apoiadas no relevo. Fora dele, `PecasDosBiomas` espalha perto de 2.800 árvores e pedras por semente fixa: no máximo uma por célula de 12 m, em manchas mais cheias e mais vazias, fora da água e da lava e fora das encostas íngremes.

### Estruturas desta leva

Quatro lugares fixos (issue #157), sempre nas mesmas coordenadas, montados pelo `ConstrutorHub` com peças do Kenney Fantasy Town Kit (CC0, já em [licencas.md](../arte/licencas.md)). Cada peça apoia a base na altura do `MapaDoMundo`. Ficam fora da cidade, da água, da lava e de encosta acima de 45 graus. Não há interior, loot, inimigo nem missão.

| Lugar | Tipo | Bioma | Onde |
|---|---|---|---|
| Ruína do oeste | muros e pedra | Colinas | por volta de (-250, -100) |
| Acampamento da mata | barraca, carroça e banco | Mata | por volta de (250, -100) |
| Passagem das colinas | lajes entre duas pedras | Colinas | por volta de (-300, -200) |
| Marco da estrada norte | pedra e lanterna à beira da estrada que sai da cidade | Campo | (14, 70) |

- ~~**Peças provisórias:** enquanto o pacote CC0 novo não entra (cactos, pinheiros com neve, rochas vulcânicas), os quatro biomas de fora usam as árvores e pedras do Kenney Fantasy Town Kit, com a cor multiplicada pela do bioma (seca no deserto, fria na neve, queimada na lava, musgosa no pântano).~~ Substituído em 2026-10-11: cacto e árvore seca só no deserto, pinheiro só na neve (até 85 m), rocha vulcânica na lava, árvore de pântano nas ilhas, arbusto no campo e na mata. São malhas geradas no client (`VegetacaoProcedural`), apoiadas no chão, sem colisor. Campo, mata e colinas continuam com as árvores do Kenney. Um pacote CC0 pode trocar essas malhas depois, sem mudar a posição.
- **Ambiente:** o `AmbienteDosBiomas` troca a luz ambiente, a cor e a força do sol e a névoa conforme o bioma em que o jogador está, misturando perto das fronteiras: dia claro em volta da cidade, ar quente no deserto, branco frio na neve, vermelho escuro na lava e névoa verde e fechada no pântano. A névoa também esconde o fim do alcance da câmera (500 m, do Core). O fundo da câmera é o céu do bioma; a névoa fica na frente dele.

### Céu e clima visual

De dia (issue #164). O ciclo de hora continua na #163; até lá a hora fica em meio-dia, e a noite (20h–5h) só entra quando alguém escrever `AmbienteDosBiomas.Hora`.

- **Céu:** cor própria por bioma, mais escura à noite. A névoa não pinta o fundo.
- **Névoa:** aberta no campo, um pouco mais densa e quente no deserto, curta na neve, fechada no pântano.
- **Efeito leve,** poucas partículas em volta do jogador: poeira no deserto, neve caindo na montanha, brasa na lava, névoa rasteira no pântano, vaga-lume na mata só de noite. O efeito do deserto não aparece na neve. Chuva, tempestade e qualquer clima que mude regra ficam de fora.

### Primeira caverna

Uma boca nas colinas, por volta de (-220, -80), e um interior **à parte** (issue #158): o piso fica em Y = -40, na mesma coluna, sem furar o chão do mundo e sem ser a dungeon da issue #66. Atravessar a boca (2 m) leva 4 m corredor adentro; voltar à boca de dentro devolve ao mesmo ponto do lado de fora. O client informa a posição nova; o servidor guarda, sem posição forçada. Sem loot, inimigo ou segunda entrada.

- **Luz de dia (2026-10-11, issue #162):** sem ciclo de dia e noite (continua em aberto). ~~Um sol direcional a 50° com sombra suave, luz ambiente suave e lanternas com a luz de ponto gravada no prefab (fraca, sem calibragem para o módulo de 4 m).~~ Substituído em 2026-10-11 por: o **sol** é a luz principal da cena, num ângulo de 42° que alonga a sombra de casas, torres e morros, com sombra suave que não chega a preto (chão claro sem estourar, mata sem ficar preta); uma **luz de preenchimento** direcional, sem sombra, do lado oposto ao sol e bem mais fraca que ele, mantém o lado do personagem fora do sol (e o rosto) visível; as **lanternas** da praça e do portão têm luz de ponto de cor quente, com alcance de 20 m (cinco módulos de 4 m) e força para clarear a base do poste. A cor e a força do sol e do preenchimento acompanham o bioma. Os valores moram em `ConfigIluminacao`. A distância das sombras vem do asset do URP (Core), não do Mundo.

### Livro de Asterra: regiões e cidade (proposta do board, 2026-10-11)

Tudo abaixo vem do "Livro de ideias" do [FigJam](../brainstorm/figjam.md) (seções 02, 06, 07 e 10). É **proposta do board (2026-10-11), não validada pelo grupo**; nada aqui muda o que já está em "Biomas e relevo". Nomes e números novos continuam propostas.

- **Regiões do livro:** **Vale das Fogueiras** (inicial: campos, bosques baixos, rios, Vila da Brasa Mansa, Fazenda dos Cascos, Ponte Partida, Colina do Sino), **Mata do Eco** (floresta de manadas, Dólmen das Vozes), **Serra do Vidro Frio** (gelo e travessias, dungeon curta Mina do Reflexo), **Caldeira Rubra** e **Planalto Astral** (avançadas, FUTURO). Encaixe aproximado nos biomas atuais, a validar: Vale = campo e arredores; Mata do Eco = mata; Serra = montanhas de neve; Caldeira = montanhas de lava; Planalto Astral ainda sem bioma. O livro não cita deserto nem pântano, que já existem.
- **Cidade "Brasa Mansa"** (nome proposto para o hub): já construída, com forja, cozinha, banco, mercado, vendedores e ponto de retorno; o jogador não constrói a cidade. Os serviços são de [itens](itens.md) e [economia](economia.md); aqui só o lugar.
- **Bases e construção: FUTURO** ("EM BREVE"). Pontos marcados no mapa poderão receber bases de jogadores, grupos ou guildas (não é construção livre). Acampamentos montáveis, faróis e pontes comunitárias também ficam para depois. No MVP, fogueiras e estações já colocadas servem à culinária e às pausas. Peças, custos e permissões não estão definidos.
- **Escopo do MVP no livro:** mundo persistente, mapa pronto com uma cidade e subáreas (floresta, rios, caverna), referência de 20 jogadores por instância, expedições que não apagam o progresso. Meteoros, invasões, fendas e PvP são posteriores.
- **Ciclo de dia e noite (proposta):** 40 min de dia e 20 min de noite; a noite reduz a leitura de mobs entre árvores, mas mantém contornos. Liga com a #163 (hora do mundo).
- **Luz e som (proposta):** tochas (perto), flechas de fogo (marcam destino) e orbes (área grande) como fontes de luz; cada espécie reage à luz e ao som (veja [mobs](mobs.md)). Marcas de impacto (martelada, fuligem) são superficiais e não destroem o relevo. **Ponte de gelo:** superfície temporária entre margens válidas, com aviso de rachadura; criaturas também atravessam.
- **Caído e retorno sem perdas (proposta):** ao zerar a vida, 45 s caído; aliado levanta em 3 s com 20% da vida; senão retorna à cidade mais próxima ou a teleporte descoberto, sem perder XP, moeda, equipamento ou durabilidade. A regra de combate e grupo é de [combate](combate.md) e [social](social.md); aqui só o retorno e os pontos de teleporte (ligados à viagem rápida em aberto).
- **Eventos de mundo: FUTURO** (Chuva de Ferro Estelar no Planalto, Cerco da Brasa Mansa, Fenda do Horizonte): sem prioridade no MVP, ficam no backlog de eventos de mundo.

## Depende de

## Expõe

### Regras compartilhadas – `MapaDoMundo`

No shared (`Features/Mundo/`), sem rede: `Bioma` (enum: `Cidade`, `Campo`, `Mata`, `Colinas`, `Deserto`, `Neve`, `Lava`, `Pantano`), `PesosDosBiomas` (quanto de cada bioma existe num ponto; somam 1) e `MapaDoMundo`, com `BiomaEm(x, z)`, `PesosEm(x, z)`, `AlturaEm(x, z)` e as medidas (`Lado`, `RaioPlano`, `NivelDaAgua`, `NivelDaLava`, `AlturaMinima`, `AlturaMaxima`). Qualquer funcionalidade pode perguntar a altura do chão ou o bioma de um ponto, no client e no servidor. As regras estão em "Biomas e relevo". `EstruturasDoMapa` lista os quatro lugares fixos desta leva; a tabela está em "Estruturas desta leva".

### Mensagens UDP – Core (faixa 0–999, `TempoRealIds`)

| ID | Nome | Direção | Campos |
|----|------|---------|--------|
| 1 | `AutenticarTempoReal` | client → servidor | `Token` (string), `PersonagemId` (long) |
| 2 | `ResultadoAutenticacaoTempoReal` | servidor → client | `Aceita` (bool), `Codigo` (string) — vazio quando aceita; `CodigosErro.NaoAutenticado` ou `CodigosErro.AcessoNegado` quando recusa |

### Tipo compartilhado – `JogadorVisivel`

Campos: `PersonagemId` (long), `X`, `Y`, `Z`, `RotacaoY` (float). Usado em `EstadoDaCidade`, `JogadorEntrou` e `PosicaoJogador`.

### Mensagens UDP – Mundo (faixa 1000–1999, `MundoMessageIds`)

| ID | Nome | Direção | Campos |
|----|------|---------|--------|
| 1000 | `EntrarNaCidade` | client → servidor | *(nenhum — a conexão já está autenticada)* |
| 1001 | `EstadoDaCidade` | servidor → client | `Eu` (JogadorVisivel), `Outros` (JogadorVisivel[]) |
| 1002 | `IntencaoDeMovimento` | client → servidor | `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int) |
| 1003 | `PosicaoJogador` | servidor → client | `PersonagemId` (long), `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int) |
| 1004 | `JogadorEntrou` | servidor → client | `Jogador` (JogadorVisivel) |
| 1005 | `JogadorSaiu` | servidor → client | `PersonagemId` (long) |
| 1006 | `VelocidadesDeMovimento` | servidor → client | `Andar`, `Correr` (float, m/s), `Sequencia` (int) |
| 1007 | `ConfirmarVelocidade` | client → servidor | `Sequencia` (int) |
| 1008 | `PedirVelocidadeDeTeste` | client → servidor | `Andar`, `Correr` (float, m/s); só em desenvolvimento |
| 1009 | `PosicaoForcada` | servidor → dono | `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int) |
| 1010 | `ConfirmarPosicaoForcada` | client → servidor | `Sequencia` (int) |

> **Proposta para o servidor (fora deste PR):** recusar `IntencaoDeMovimento` que implique deslocamento acima de 5,5 m/s (valor atual de `MovimentoTerceiraPessoa.VelocidadeCorrer` no client). ~~Esta checagem não está implementada aqui.~~ Implementada no servidor (#103), veja abaixo.

**Presença na cidade (servidor, #103):** em `Features/Mundo/`. Uma só cidade; `EntrarNaCidade` põe o jogador no nascimento ~~(0, 0, -12)~~ (0, 0, -24) (substituído em 2026-10-11: é o nascimento da cidade de 80 m) e devolve `EstadoDaCidade` (quem já estava recebe `JogadorEntrou`). `IntencaoDeMovimento` é validada no plano horizontal: no máximo ~~5,5 m/s~~ a velocidade do personagem no servidor (5,5 m/s ao entrar; substituído em 2026-10-11, veja "Velocidade autoritativa" abaixo) mais 0,5 m de folga, ~~com o tempo decorrido limitado a 1 s (parado não acumula)~~ por um orçamento de deslocamento (substituído em 2026-10-11, veja "Movimento sem voltar no tempo" abaixo), dentro de ~~±60 m~~ ~~±500 m em X e Z (substituído em 2026-10-11: o terreno passou a 1 km) e altura de -5 a 50 m~~ **±1500 m** em X e Z, com a altura entre **3 m abaixo e 90 m acima do chão daquele ponto** (substituído em 2026-10-11: o mundo passou a 3 km com relevo; o chão vem do `MapaDoMundo`, e os 90 m cobrem pulo e queda de paredão); NaN ou infinito também é recusado. ~~Aceita, o dono recebe `PosicaoJogador` com a `Sequencia` e os outros, a réplica (sequência 0); recusada, só o dono recebe a posição anterior com a `Sequencia`, para corrigir a predição.~~ Substituído em 2026-10-11 ([ADR 0024](../decisoes/0024-rede-sem-voltar-no-tempo.md)): só os outros recebem a réplica; o dono não recebe a própria posição, e o caso grave vira `PosicaoForcada`. Sair ou cair (`SessaoEncerrada`) remove o avatar e avisa `JogadorSaiu`. Movimento sem ter entrado é ignorado.

**Velocidade autoritativa (#263, 2026-10-11):** a velocidade de andar e correr é do servidor, por personagem, no modelo do WoW (`FORCE_RUN_SPEED_CHANGE` e o ACK). Ao entrar na cidade o servidor manda `VelocidadesDeMovimento` (5,5 e 2 m/s); quando a velocidade muda, manda de novo com a `Sequencia` seguinte. O client aplica e responde `ConfirmarVelocidade` com a mesma sequência; até essa confirmação, o servidor valida o deslocamento pela **maior** entre a velocidade antiga e a nova, para não punir quem ainda não recebeu o aviso. `PedirVelocidadeDeTeste` é o equivalente ao `.modify speed` de GM: o servidor só aceita com `Mundo:PermitirVelocidadeDeTeste` ligado (`appsettings.Development.json`) e entre 0,5 e 50 m/s (`RegrasVelocidade`); fora disso ignora. No editor, mudar andar ou correr no Inspector do `ControleTerceiraPessoa` durante o Play vira esse pedido; offline, vale o Inspector direto. Buffs, montarias e lentidão usam o mesmo caminho quando existirem.

**Movimento sem voltar no tempo (#277, 2026-10-11, [ADR 0024](../decisoes/0024-rede-sem-voltar-no-tempo.md)):** o jogador local é dono da própria posição; o servidor valida, repassa aos outros e não devolve a posição ao dono.
- **Orçamento de deslocamento:** cada personagem junta até 2 s de corrida (mais 0,5 m) e gasta o que anda. Pacotes que chegam em rajada só gastam o que já juntaram, então o atraso de rede nunca vira recusa. Quem reconecta ganha 10 s de folga, pelo que andou sem conexão.
- **Excesso pequeno** (até 6 m além do orçamento, o tamanho de uma esquiva recusada) é aceito e conta como suspeita; o contador esvazia 0,5 por segundo e, passando de 5, vira caso grave. O total de suspeitas fica guardado para a ferramenta de GM.
- ~~**Caso grave** (posição fora do mundo, longe do chão, não finita, excesso acima de 6 m ou suspeita demais): a posição do servidor fica, e o dono recebe `PosicaoForcada`. O client aplica e confirma (`ConfirmarPosicaoForcada`); até lá, o movimento dele é ignorado. Teleporte, renascer e empurrão de boss usam o mesmo caminho (`CidadeDoMundo.ForcarPosicao`).~~ Substituído em 2026-10-11: o jogador se move livremente e só informa a posição. O servidor grava e repassa aos outros; andar rápido, salto ou sair do limite **não** manda `PosicaoForcada`. Número não finito é ignorado. `ForcarPosicao` continua só para teleporte explícito (renascer, empurrão), que ainda não é o movimento normal.
- **No client:** o nascimento só é aplicado na primeira entrada; na reconexão o jogador continua onde está.

### Eventos e consulta para outras funcionalidades do servidor (#119)

Em `server/src/JogoDoGrupo.Server/Contratos/Mundo/` ([ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)); só o servidor usa, o client não vê.

| Tipo | O que é |
|---|---|
| `JogadorEntrouNaArea` (`PersonagemId`, `Area`) | evento publicado quando o personagem passa a estar numa área. Pedir para entrar de novo, já estando nela, não repete |
| `JogadorSaiuDaArea` (`PersonagemId`, `Area`) | evento publicado quando o personagem sai do jogo ou a conexão cai. Quando chega, a consulta já não encontra o personagem |
| `IPosicoesDoMundo` | consulta pela injeção de dependência: `TentarObter(personagemId, out PosicaoNoMundo)` (área, X, Y, Z, rotação: a posição que o servidor aceitou) e `PersonagensNaArea(area)` |
| `IDeslocamentosDoMundo` | pedido pela injeção de dependência: `AbrirJanela(personagemId, distanciaMetros, duracaoSegundos)` deixa o personagem se afastar até essa distância de onde está, nessa duração (mais 150 ms de folga de rede), mais rápido que a corrida; fora da janela volta o limite de 5,5 m/s. O Mundo continua validando cada posição. Usado pela esquiva (Combate, #233) |
| `AreasDoMundo.Cidade` | o nome da única área de hoje (a cidade e o terreno em volta) |

Os eventos saem depois das mensagens de rede da mesma mudança (`EstadoDaCidade`, `JogadorEntrou`, `JogadorSaiu`).

## Minimapa e mapa-múndi (issues #91 e #92)

~~Tudo abaixo é **proposta** para o grupo validar; nada aqui é decisão ainda.~~ Decidido em 2026-10-11, quando o mapa e o minimapa entraram no client. Norte fixo (não gira com a câmera), raio de 40 m, sem viagem rápida e sem mensagem nova do servidor. A dungeon desenhada no mapa espera a issue #66; até lá o mapa mostra o mundo de 3 km.

**Base:** WoW e New World (minimapa no canto, mapa em tela cheia). **Igual:** minimapa circular de cima; tecla M abre o mapa. **Diferente:** sem viagem rápida e sem névoa de exploração na fatia.

### Minimapa

- Fica no canto superior direito, visto de cima, centrado no jogador, com raio de **40 m**, norte para cima. Na cidade (~~40 × 40 m~~ 80 × 80 m, substituído em 2026-10-11) cobre o centro, não a cidade inteira.
- Aparece: seta do jogador (posição e direção); jogadores da mesma área; o chão que a câmera de cima já vê. ~~A **entrada da dungeon** (portão norte), NPCs e inimigos próximos como ícone próprio.~~ Substituído em 2026-10-11: NPC, inimigo e dungeon só ganham ícone quando a funcionalidade deles publicar a posição sem o Mundo referenciar outra feature. O que já está no mundo aparece na câmera de cima.
- **Visibilidade:** o minimapa mostra só o que o client já recebe. Jogadores vêm da presença da cidade (`EstadoDaCidade`, `JogadorEntrou`, `PosicaoJogador`). Inimigos e NPCs entram quando as respectivas funcionalidades enviarem posições; até lá não aparecem como ícone. **O servidor não envia nada extra** por causa do minimapa e não revela quem está fora do alcance de visão que já usa.
- ~~Jogadores do grupo (party, [social.md](social.md)) com marcador de cor diferente dos demais; inimigos em vermelho só se vivos e dentro do raio.~~ A cor de grupo espera um contrato que o Mundo possa ler sem referenciar Social. Inimigo em vermelho espera o mesmo de Mobs.
- Implementação no client: câmera ortográfica de cima para uma render texture, criada por script (sem cena/prefab à mão). Sem tecla própria na primeira versão (sempre visível); tamanho/ocultar fica para o HUD (#93). Debaixo do relevo (caverna) a câmera não desenha o chão de cima.
- **Fora da fatia:** zoom, ícones de recursos, rastro de caminho, marcador colocado pelo jogador.
- **Testes esperados** (client, EditMode): conversão posição do mundo para posição no minimapa (centro, borda, fora do raio); filtro por raio de 40 m; seta gira com `RotacaoY`.

### Mapa-múndi

- Tecla **M** abre/fecha uma tela cheia; Esc também fecha. O jogo continua rodando por baixo.
- ~~Na fatia mostra **duas áreas**: a **cidade** (com desenho do hub e o portão norte) e a **dungeon** (esquema: entrada, três salões e salão do boss). Cada área com o **nome** escrito. A área onde o jogador está aparece destacada; as outras ficam como ícone.~~ Substituído em 2026-10-11: o mapa pinta os biomas do mundo de 3 km. A cidade murada ganha contorno quando o jogador está dentro dela. O esquema da dungeon continua para a issue #66.
- Marcadores: o próprio jogador (seta) e os aliados da mesma área (mesma fonte do minimapa: presença); **marcadores de missão** só quando [missões](missoes.md) definir posições (fora da fatia por enquanto).
- **Visibilidade:** o mapa só mostra posições que o client já tem. Aliados em **outra** área não entram no mesmo mapa. ~~O ícone da área com a contagem de aliados, se o servidor já informar a instância do grupo,~~ continua em aberto.
- O mapa da dungeon mostra os salões já visitados pelo grupo? **Em aberto** (depende da #66).
- **Sem viagem rápida na fatia**: a cidade e a dungeon se ligam pelo portão. Viagem rápida segue em aberto acima ("Como funciona a viagem rápida?").
- **Fora da fatia:** várias regiões, névoa de exploração, zoom e arrasto, anotações, viagem rápida.
- **Quem decide:** nada do mapa é regra de jogo; o servidor só envia as posições da presença, o client desenha. Se algum dia houver viagem rápida, ela será validada pelo servidor.
- **Testes esperados** (client, EditMode): tecla M abre o mapa; a cidade só é destacada para quem está nela; sem posições de áreas diferentes no mesmo mapa.

### Em aberto (dúvidas ao grupo)

- [x] ~~Raio do minimapa (40 m) e se ele gira com a câmera ou fica fixo ao norte.~~ Decidido em 2026-10-11: 40 m, norte fixo.
- [ ] Mostrar inimigos no minimapa sempre ou só em combate? Pode tirar o susto do telegrafado. Enquanto isso, o ícone não existe; o modelo só aparece se a câmera de cima já o vê.
- [ ] Aliados em outra área aparecem no mapa-múndi (e o servidor envia a instância do grupo)?
- [ ] O mapa da dungeon revela salões só depois de visitados?
- [ ] Viagem rápida existe depois da fatia? Com custo, só a pontos descobertos?

- **Ajuste da luz para os modelos (2026-10-11, issue #311):** o ambiente padrão (campo, mata e colinas) ficou mais claro (céu, horizonte e solo) e o sol um pouco mais forte, o preenchimento subiu para 40% do sol e a sombra do sol ficou mais leve (0,55), para o lado do personagem e das criaturas fora do sol, de textura escura, continuar legível sem perder o volume. Horizonte e solo da lava e do pântano subiram um pouco. A névoa, o céu e o ângulo do sol não mudaram, e não há Volume no mundo (o URP não liga pós-processamento na câmera do Core), então não há tonemapping. Para conferir: `npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.CapturaDaIluminacao.Capturar` (editor fechado) salva em `%TEMP%` uma folha com o personagem e as criaturas vistos de quatro lados; exemplos em `docs/arte/renders/`.
