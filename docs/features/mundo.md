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
- [x] **Livro de Asterra, decidido em 2026-10-11 (/decida, #326):** os nomes entram como **rótulos** sobre os biomas atuais, sem criar bioma: Vale das Fogueiras = campo e arredores, Mata do Eco = mata, Serra do Vidro Frio = montanhas de neve, Caldeira Rubra = montanhas de lava. Deserto e pântano ficam sem nome do livro por ora; Planalto Astral é FUTURO, sem bioma. Estruturas nomeadas entram em tarefas próprias. Pergunta original: o grupo adota os nomes de região e de lugar (Vale das Fogueiras, Mata do Eco, Serra do Vidro Frio, Caldeira Rubra, Planalto Astral, Brasa Mansa...)? Como encaixam nos sete biomas de 3 km já decididos (o livro não tem deserto nem pântano e tem o Planalto Astral)? Veja "Livro de Asterra: regiões e cidade".
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
- **Planta em integração (2026-10-15):** a cidade inicial fica na origem; duas vilas provisórias ocupam o Passo da Neve (-520, 320) e o Oásis do Deserto (-900, -620). A mata envolve a cidade em manchas e clareiras, as colinas acompanham o oeste, e neve, deserto, pântano e lava ocupam massas irregulares que se misturam nas bordas. Essas coordenadas são provisórias para revisão visual.
- **Caminhos:** seis corredores ligam as três cidades, o pântano, a caldeira e a serra. `CaminhosDoMundo` guarda os eixos compartilhados; o relevo fica mais suave e a vegetação abre passagem. `PesosDoTerreno` usa o mesmo eixo para desenhar terra batida. A ligação da cidade central ao Passo usa `EstradaDoPasso.Tracado`: pontos de controle, largura, acomodação lateral e suavização longitudinal são dados compartilhados. A altura oficial é ajustada com cortes e aterros suaves ao redor desse eixo. `MontadorDaEstrada` põe uma superfície visual fina com textura de terra sobre a malha oficial, sem colisão separada, para a rua continuar legível abaixo da resolução de 10 m da textura do terreno. As outras cinco rotas continuam fixas até passarem pela mesma autoria.
- **Objetivo do editor procedural de mapas:** a pessoa desenha o mundo no editor Unity, ajusta parâmetros e vê o resultado antes de salvar, sem editar código. A ordem confirmada de desenvolvimento e de autoria é **biomas → lagos → estradas**. Desenhar um bioma deve gerar sua forma de relevo, texturas e vegetação conforme as regras daquele bioma; desenhar um lago deve formar a bacia, água e margens; traçar uma estrada deve acomodar o relevo, aplicar o piso e abrir a passagem. Os elementos salvos precisam continuar editáveis e produzir a mesma altura oficial para cliente e servidor. O [guia permanente](../guia-criador-de-mapas.md) documenta o uso real de cada operação quando ela estiver disponível.
- **Protótipo atual da estrada do Passo:** a primeira janela desenha somente a ligação Cidade central--Passo. Seus extremos ficam fixos nas cidades; ao salvar, o traçado compartilhado determina a altura usada por cliente e servidor. Ela valida as regras da estrada, mas ainda não é o editor completo nem permite criar estradas independentes. Biomas e lagos são as próximas operações a desenvolver, nessa ordem; só depois a operação de estradas será generalizada.
- **Rios altos:** o rio glacial e o rio das cascatas seguem o leito definido em `MapaDoMundo`. `ConstrutorDoTerreno` cria uma superfície de água que acompanha sua altura; o shader de água recebe a profundidade pelo canal vermelho da cor dos vértices, como nas demais águas. Árvores não nascem dentro do leito.
- **Vegetação:** a distribuição do novo mapa usa `PecasDosBiomas` e continua desenhada em lotes por `MontadorDaVegetacao` e `BlocosDeVegetacao`, preservando a otimização de desenho por blocos.
- **Biomas (sete):**

  | Bioma | Onde | Chão | Relevo | Peças |
  |---|---|---|---|---|
  | **Campo** | clareiras perto da cidade e entre as massas externas | verde claro | baixo e suave | árvores afastadas |
  | **Mata** | em torno da cidade, com manchas densas e clareiras | verde escuro misturado a terra | ondulado | árvores próximas |
  | **Colinas** | oeste e sudoeste da cidade, inclusive perto da caverna | terra seca | morros e platôs | mais pedra que árvore |
  | **Deserto** | oeste e sudoeste do mundo | areia | dunas | pedras, cactos e árvores secas |
  | **Montanhas de neve** | norte e noroeste, com uma passagem a oeste | branco, pedra nas encostas | picos altos e vales | pinheiros até 85 m de altura, pedra acima |
  | **Montanhas de lava** | sul e sudeste | pedra escura, brasa na beira da lava | picos cortados por rios de lava | rochas vulcânicas |
  | **Pântano** | leste e manchas a nordeste e sudeste | lama | ilhas baixas e poças rasas | árvores tortas nas ilhas |

- **Fronteiras:** as massas de bioma têm formas elípticas irregulares e se misturam numa faixa de dezenas de metros: cor, altura e luz mudam aos poucos. A área inicial tem clareiras e manchas de mata, e o relevo separa os líquidos do pântano e da lava.
- **Transições e relevo da cidade (2026-10-13):**
  - **Dentro dos muros (até 40 m do centro):** Chão rigorosamente plano em **Y = 0**.
  - **Transição 1 (40 a 60 m - faixa de 20 m fora dos muros):** Acomodação imediata ao sair da cidade com subida gradual da estrada norte.
  - **Transição 2 (60 a 120 m):** Conexão intermediária suave ligando as imediações da cidade ao relevo pleno dos biomas.
  - **Relevo imediato:** O relevo dinâmico (ondulações campestres vivas, platôs escalonados em degraus e colinas) começa logo a partir de 100 m do centro, eliminando o plano artificial central.
- **Rede de rios de água doce e cascatas:**
  - Rios navegáveis escavados organicamente no terreno, inspirados no mapa de conceito artístico: Rio Glacial do Leste (norte/montanhas em direção à costa) e Rio das Cascatas do Oeste (platôs da floresta contornando a caverna).
  - Leitos e margens com calha suave e seixos e cascalho (`MargemDeRio`). Os trechos baixos usam a malha de água em Y = -0,5 m; os trechos altos acompanham `NivelDoRioAlto(z)` em malha própria, com o mesmo material de água.
- **Curvas e cumes das montanhas:** Montanhas imponentes e orgânicas com curvas multifractais suaves e arredondadas (sem cristas pontudas ou picos em V artificiais).
- **Texturas em alta resolução e Shader dedicado (`GeradorDeTexturasDoTerreno`, `TerrenoMundo.shader`):**
  - Texturas procedurais contínuas (seamless) de 512×512 geradas deterministicamente para cada elemento (grama com folhagens e tufos, terra e pedregulhos, areia fina dourada, rocha com estratificação e neve cristalina).
  - Amostragem em coordenadas de mundo (*world-space tiling* de 4 m e 25 m em dual-scale), eliminando o efeito xadrez/grelha repetitiva em distâncias médias e longas.
  - Projeção triplanar em encostas (transição a partir de 15° a 25°), cobrindo a encosta inteira até o topo plano e eliminando qualquer estiramento de textura vertical em paredões de pedra e penhascos.
  - Acentuação geométrica da quina no topo das encostas (*crest sharpening* via derivadas de tela), mantendo o platô reto até a borda sem arredondamento inflado de balão.
  - Rugosidade modelada 3D nas partes de rocha via Normal Map procedural contínuo com perfil de ondas serrilhadas periódicas (degraus e terraços geológicos) e textura albedo mineral lisa.
  - Mapa de micro-detalhe albedo de 1024×1024 integrado com URP Lit (`_DetailAlbedoMap`), proporcionando fidelidade visual e nitidez tanto colado na câmera quanto ao olhar para o horizonte.
- **Classificação de superfícies e texturas (`ClassificacaoTerreno`, `CoresDosBiomas`):** Diferenciação entre chão padrão do bioma, **praias** (areia dourada suave contornando 100% da linha d'água do pântano e lagoas), **margens de rios** (seixos e cascalho em leitos com bordas cortadas), **encostas e paredões de rocha** e o **caminho da estrada norte**.
- **Materiais do chão por mapa de pesos (2026-10-14, #501):** `PesosDoTerreno` (puro, testado) converte os pesos dos biomas, a altura e a distância do rio em cinco pesos que somam 1: grama (campo, mata, parte das colinas), terra (colinas, lava, estrada), areia (deserto, praias, margem de rio), neve e lama (pântano e fundo das poças). O `ConstrutorDoTerreno` grava quatro numa textura RGBA linear (`_PesosMap`, um pixel por ponto da grade; a lama é o que falta para 1) e o `TerrenoMundo.shader` mistura as texturas procedurais com ela, em vez de adivinhar o material pela cor macro. A rocha continua pela inclinação, e a cor macro só tinge. A textura de lama é procedural como as outras (sem licença nova). Colisão e altura não mudam.
- **Água e lava (2026-10-14, #501):** shaders `AguaMundo` e `LavaMundo` em `Resources/Mundo/Shaders/` (entram no build). Água transparente: a profundidade sob cada vértice vai no canal R da cor do vértice (0 a 3 m), que escurece e opacifica, com espuma fina na margem e normal em movimento (duas ondas somadas, sem textura). Lava emissiva: crosta escura que corre sobre o magma (ruído deslocado no tempo), sem depender do sol. Se o shader faltar, volta o Lit de uma cor só.
- **Modificadores de terreno (`ModificadorDeTerreno`, `ModificadoresDoMapa`):** Sistema modular e determinístico no shared que permite assentar monumentos e estruturas no relevo, além de suportar terraplanagens e estradas de jogadores em tempo de execução com transições suaves (Hermite smoothstep).
- **Altura do chão:** de **-2 a 220 m** em qualquer ponto do mundo. A água do pântano e rios fica em **Y = -0,5** (malha refinada em sub-quadrados de 2,5 m) e a lava em **Y = -1** (rios de até 60 cm). As duas são visuais por enquanto: não têm colisor e **não causam dano nem deixam mais lento** (em aberto, acima).
- **Encostas:** o personagem sobe até 45 graus (regra do Core). Campo, mata e colinas são andáveis por inteiro. Nas montanhas, até um décimo do chão é íngreme demais: são os paredões, e o caminho é pelos vales.
- **Fonte única do relevo:** `MapaDoMundo`, no shared (`BiomaEm`, `PesosEm`, `AlturaEm`). Usa sementes fixas: o mundo é sempre o mesmo. A altura oficial é a de uma grade de 10 m, em triângulos, igual à malha que o client desenha.
- **Montagem no client:** o `ConstrutorDoTerreno` cria o chão em 225 blocos de 200 m (malha com colisor), pinta uma textura com a cor de cada ponto, e cria as malhas da água e da lava. Nada disso é cena ou asset: nasce ao entrar no mundo. O chão não existe sob a cidade (lá vale o chão dela).
- **Peças:** o miolo de 1 km continua com `LayoutDoHub.PropsFora`, agora apoiadas no relevo. Fora dele, `PecasDosBiomas` espalha perto de 2.800 árvores e pedras por semente fixa: no máximo uma por célula de 12 m, em manchas mais cheias e mais vazias, fora da água e da lava e fora das encostas íngremes.
- **Vegetação leve (2026-10-14, #502):** cada tipo de peça (`ModeloDePeca`) tem uma malha e materiais só, divididos por todas as peças (cacto, árvores secas, pinheiro, rocha vulcânica, árvore de pântano e arbusto: os blocos fundidos numa malha, uma submalha por cor; as árvores e pedras do Kenney: as malhas do prefab). Não há objeto nem `Renderer` por peça: o `BlocosDeVegetacao` agrupa as peças em blocos de 200 m e desenha cada (malha, material) por instancing (`Graphics.RenderMeshInstanced`), com um material por superfície e bioma (`MateriaisDosBiomas`, no máximo 80). **Corte por distância em duas faixas** (`ConfigVegetacao`): peças grandes até 320 m da câmera, pequenas (arbusto, cacto, pedra pequena, rocha vulcânica) até 140 m. Cada peça com colisor ganha um objeto só com ele: **cápsula do chão ao topo** em cacto, árvore seca, pinheiro, árvore de pântano e rocha vulcânica (atravessar é só o arbusto); as peças do Kenney levam o colisor do prefab. A montagem (`MontadorDaVegetacao`) trabalha por orçamento de tempo (6 ms por quadro) e devolve a vez, no lugar de um quadro só.

### Estruturas desta leva

Quatro lugares fixos (issue #157), sempre nas mesmas coordenadas, montados pelo `ConstrutorHub` com peças do Kenney Fantasy Town Kit (CC0, já em [licencas.md](../arte/licencas.md)). Cada peça apoia a base na altura do `MapaDoMundo`. Ficam fora da cidade, da água, da lava e de encosta acima de 45 graus. Não há interior, loot, inimigo nem missão.

| Lugar | Tipo | Bioma | Onde |
|---|---|---|---|
| Ruína do oeste | muros e pedra | Colinas | por volta de (-250, -100) |
| Acampamento da mata | barraca, carroça e banco | Mata | por volta de (250, -100) |
| Passagem das colinas | lajes entre duas pedras | Colinas | por volta de (-300, -200) |
| Marco da estrada norte | pedra e lanterna à beira da estrada que sai da cidade | Campo | (14, 70) |

- ~~**Peças provisórias:** enquanto o pacote CC0 novo não entra (cactos, pinheiros com neve, rochas vulcânicas), os quatro biomas de fora usam as árvores e pedras do Kenney Fantasy Town Kit, com a cor multiplicada pela do bioma (seca no deserto, fria na neve, queimada na lava, musgosa no pântano).~~ Substituído em 2026-10-11: cacto e árvore seca só no deserto, pinheiro só na neve (até 85 m), rocha vulcânica na lava, árvore de pântano nas ilhas, arbusto no campo e na mata. São malhas geradas no client (`VegetacaoProcedural`), apoiadas no chão, com colisor de cápsula no tronco desde 2026-10-14 (#502). Campo, mata e colinas continuam com as árvores do Kenney. Um pacote CC0 pode trocar essas malhas depois, sem mudar a posição.
- **Ambiente:** o `AmbienteDosBiomas` troca a luz ambiente, a cor e a força do sol e a névoa conforme o bioma em que o jogador está, misturando perto das fronteiras: dia claro em volta da cidade, ar quente no deserto, branco frio na neve, vermelho escuro na lava e névoa verde e fechada no pântano. A névoa também esconde o fim do alcance da câmera (500 m, do Core). O fundo da câmera é o céu do bioma; a névoa fica na frente dele.

### Céu e clima visual

Por bioma (issue #164), escurecendo com a hora do servidor ([Dia e noite](#dia-e-noite-163-contrato-da-380)).

- **Céu:** cor própria por bioma, mais escura à noite. A névoa não pinta o fundo.
- **Névoa:** aberta no campo, um pouco mais densa e quente no deserto, curta na neve, fechada no pântano. À noite escurece junto com o céu (menos que ele, para o horizonte não sumir).
- **Efeito leve,** poucas partículas em volta do jogador: poeira no deserto, neve caindo na montanha, brasa na lava, névoa rasteira no pântano, vaga-lume na mata só de noite. O efeito do deserto não aparece na neve. Chuva, tempestade e qualquer clima que mude regra ficam de fora.

### Primeira caverna

Um túnel **escavado num morro das colinas**, contínuo com o terreno (decisão de 2026-10-11, issue #437; substitui o "interior à parte" da #158, que tinha teleporte e piso em Y = -40). O jogador entra andando pela boca, em (-220, -80), sem troca de espaço nem posição forçada. Não é a dungeon da issue #66. Sem loot, inimigo ou segunda entrada.

- **Layout** (inspirado nas cavernas de início de zona do WoW; fonte única em `PrimeiraCaverna`, no shared): boca estreita (4 m de largura) na encosta, túnel sinuoso de cerca de 120 m com quatro curvas suaves que desce uns 10 m, **câmara média** (13 m) no meio, corredor com **rampa** de 2 m de desnível e **câmara grande** (19 m, teto de 7 m) no fim, mais um **beco** lateral sem saída (3 m de largura). Corredor com 3,2 m de teto no mínimo e 3 m de largura no mínimo, para o personagem passar.
- **Morro:** `MapaDoMundo` soma ao relevo das colinas um domo de 16 m e 42 m de raio (`AlturaDoMorro`, centro em (-220, -47)). É ele que cobre o túnel: depois de uns 10 m da boca, o terreno passa do teto.
- **Funções puras** (`PrimeiraCaverna`, client, servidor e testes usam as mesmas): `PisoEm(x, z, y)` (altura do piso se o ponto está no túnel, senão ausente), `TetoEm(x, z)` e `DentroDoTunel(x, z, y)`. O ponto só é "do túnel" entre o piso (1 m de folga abaixo) e o teto; em cima do morro ou dentro da rocha vale o terreno. Valores (nós do percurso, larguras, alturas) em constantes nomeadas no topo da classe.
- **Servidor:** o `ValidadorDeMovimento` aceita a posição dentro do túnel pelo piso dele e continua recusando posição na rocha (atravessar parede, subir pelo teto, afundar abaixo do piso). O movimento é livre, como no resto do mundo.
- **Client:** o `ConstrutorDoTerreno` fura o chão onde a boca atravessa (as células saem do bloco e voltam num remendo de 1 m, sem os quadrados sobre o túnel); `GeometriaDaCaverna` monta a malha de rocha (piso, paredes e teto, com colisão) a partir do contorno; pedras na moldura da boca; uma luz de ponto quente a cada 11 m e uma em cada câmara e na ponta do beco. Dentro, o `AmbienteDosBiomas` tira o sol e o clima e escurece o ambiente e a névoa (valores em `ConfigIluminacao`).
- **Mapa e minimapa:** a boca aparece como ponto no mapa (M); dentro do túnel o minimapa não desenha o chão de cima (`ProjecaoDoMapa.SobOChao` usa `DentroDoTunel`).

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

Campos: `PersonagemId` (long), `X`, `Y`, `Z`, `RotacaoY` (float), `Nome` (string) e `Raca` (int: o valor do enum `Raca` de [personagem](personagem.md); número para o Mundo não depender do enum de outra funcionalidade; 0 se desconhecida). O servidor preenche nome e raça pelo `IDiretorioDePersonagens` (#498). Usado em `EstadoDaCidade`, `EstadoDaArea` e `JogadorEntrou` (a `PosicaoJogador` leva só a posição).

### Mensagens UDP – Mundo (faixa 1000–1999, `MundoMessageIds`)

| ID | Nome | Direção | Campos |
|----|------|---------|--------|
| 1000 | `EntrarNaCidade` | client → servidor | *(nenhum — a conexão já está autenticada)* |
| 1001 | `EstadoDaCidade` | servidor → client | `Eu` (JogadorVisivel), `Outros` (JogadorVisivel[]) |
| 1002 | `IntencaoDeMovimento` | client → servidor | `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int) |
| 1003 | `PosicaoJogador` | servidor → client | `PersonagemId` (long), `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int), `TempoServidorMs` (long) |
| 1004 | `JogadorEntrou` | servidor → client | `Jogador` (JogadorVisivel) |
| 1005 | `JogadorSaiu` | servidor → client | `PersonagemId` (long) |
| 1006 | `VelocidadesDeMovimento` | servidor → client | `Andar`, `Correr` (float, m/s), `Sequencia` (int) |
| 1007 | `ConfirmarVelocidade` | client → servidor | `Sequencia` (int) |
| 1008 | `PedirVelocidadeDeTeste` | client → servidor | `Andar`, `Correr` (float, m/s); só em desenvolvimento |
| 1009 | `PosicaoForcada` | servidor → dono | `X`, `Y`, `Z`, `RotacaoY` (float), `Sequencia` (int) |
| 1010 | `ConfirmarPosicaoForcada` | client → servidor | `Sequencia` (int) |
| 1012 | `EstadoDaArea` | servidor → client | `Area` (string), `Eu` (JogadorVisivel), `Outros` (JogadorVisivel[]) |

> **Proposta para o servidor (fora deste PR):** recusar `IntencaoDeMovimento` que implique deslocamento acima de 5,5 m/s (valor atual de `MovimentoTerceiraPessoa.VelocidadeCorrer` no client). ~~Esta checagem não está implementada aqui.~~ Implementada no servidor (#103), veja abaixo.

**Retorno da queda (#658):** quando Combate publica `PersonagemVoltouParaACidade` e o personagem está na área da cidade, o Mundo chama `ForcarPosicao` em `ValidadorDeMovimento.Nascimento`, manda `PosicaoForcada` ao dono e replica `PosicaoJogador` aos outros (mesmo caminho do teleporte de teste). Levantar por aliado (`PersonagemLevantou`) não muda a posição. Regras de queda em [combate](combate.md) e [personagem](personagem.md).

**Presença e movimento por área (#103, #682):** o servidor mantém estado isolado por identificador de área. `EntrarNaCidade` põe o jogador na cidade e devolve `EstadoDaCidade`; `ITransicoesDeAreaDoMundo` permite que outra funcionalidade autorizada mova o personagem ativo para uma posição de destino, incluindo áreas `instancia:<id>`. O servidor remove a presença da origem, envia `JogadorSaiu` aos presentes, publica `JogadorSaiuDaArea`, envia `EstadoDaArea` ao personagem e `JogadorEntrou` aos presentes no destino, e publica `JogadorEntrouNaArea`. `IntencaoDeMovimento` só altera a posição e replica dentro da área atual. Consultas por `IPosicoesDoMundo` retornam a área atual e filtram `PersonagensNaArea` por identificador. Sair ou cair (`SessaoEncerrada`) remove o avatar apenas da área atual. A cidade continua usando o nascimento da cidade de 80 m; a validação de movimento conserva orçamento, limites horizontais e altura do mapa descritos abaixo.

**Velocidade autoritativa (#263, 2026-10-11):** a velocidade de andar e correr é do servidor, por personagem, no modelo do WoW (`FORCE_RUN_SPEED_CHANGE` e o ACK). Ao entrar na cidade o servidor manda `VelocidadesDeMovimento` (5,5 e 2 m/s); quando a velocidade muda, manda de novo com a `Sequencia` seguinte. O client aplica e responde `ConfirmarVelocidade` com a mesma sequência; até essa confirmação, o servidor valida o deslocamento pela **maior** entre a velocidade antiga e a nova, para não punir quem ainda não recebeu o aviso. `PedirVelocidadeDeTeste` é o equivalente ao `.modify speed` de GM: o servidor só aceita com `Mundo:PermitirVelocidadeDeTeste` ligado (`appsettings.Development.json`) **e** o ambiente em Development (#552; chave ligada em outro ambiente fica desligada e avisa no log, como os comandos de teste) e entre 0,5 e 50 m/s (`RegrasVelocidade`); fora disso ignora. No editor, mudar andar ou correr no Inspector do `ControleTerceiraPessoa` durante o Play vira esse pedido; offline, vale o Inspector direto. Buffs, montarias e lentidão usam o mesmo caminho quando existirem.

**Movimento sem voltar no tempo (#277, 2026-10-11, [ADR 0024](../decisoes/0024-rede-sem-voltar-no-tempo.md)):** o jogador local é dono da própria posição; o servidor valida, repassa aos outros e não devolve a posição ao dono.
- **Orçamento de deslocamento:** cada personagem junta até 2 s de corrida (mais 0,5 m) e gasta o que anda. Pacotes que chegam em rajada só gastam o que já juntaram, então o atraso de rede nunca vira recusa. Quem reconecta ganha 10 s de folga, pelo que andou sem conexão.
- **Excesso pequeno** (até 6 m além do orçamento, o tamanho de uma esquiva recusada) é aceito e conta como suspeita; o contador esvazia 0,5 por segundo e, passando de 5, vira caso grave. O total de suspeitas fica guardado para a ferramenta de GM.
- ~~**Caso grave** (posição fora do mundo, longe do chão, não finita, excesso acima de 6 m ou suspeita demais): a posição do servidor fica, e o dono recebe `PosicaoForcada`. O client aplica e confirma (`ConfirmarPosicaoForcada`); até lá, o movimento dele é ignorado. Teleporte, renascer e empurrão de boss usam o mesmo caminho (`CidadeDoMundo.ForcarPosicao`).~~ Substituído em 2026-10-11: o jogador se move livremente e só informa a posição. O servidor grava e repassa aos outros; andar rápido, salto ou sair do limite **não** manda `PosicaoForcada`. Número não finito é ignorado. `ForcarPosicao` continua só para teleporte explícito (renascer, empurrão), que ainda não é o movimento normal. O comando de teste `tp` ([Desenvolvimento](desenvolvimento.md)) usa esse caminho e avisa os outros com `PosicaoJogador`.
- **No client:** o nascimento só é aplicado na primeira entrada; na reconexão o jogador continua onde está.
- Na primeira entrada, a presença sinaliza ao Core depois de aplicar a posição do servidor; a tela de carregamento e o bloqueio de input seguem as regras de [Interface](interface.md#definido).

**Interpolação e movimento suave dos outros jogadores (#499):**
- **Servidor:** réplicas `PosicaoJogador` carimbam `Sequencia` estritamente crescente por personagem emissor e `TempoServidorMs` (`Environment.TickCount64`).
- **Buffer de interpolação:** `AvatarRemoto` usa `InterpoladorDeMovimento` com buffer atrasado em 100 ms (`ConfigInterpolacaoMundo.AtrasoDeRenderSegundos`), interpolando suavemente posição e rotação pelo caminho angular mais curto entre os dois pacotes mais próximos.
- **Descarte de desordem:** amostras com `Sequencia` menor ou igual à última aceita são descartadas (evita jitter por pacotes atrasados ou duplicados).
- **Extrapolação curta:** se um pacote atrasar ou for perdido, o avatar continua na velocidade horizontal recente por até 150 ms (`ExtrapolacaoMaximaSegundos`), freando suavemente caso novos pacotes não cheguem.
- **Salto:** deslocamentos acima de 8 m (`DistanciaDeSalto`) teleportam imediatamente sem deslizar.
- **Correção local:** posições forçadas recebidas pelo jogador acima de 3 m (`DistanciaDeCorrecao`) teleportam na hora; abaixo disso, a posição do CharacterController avança suavemente a 15 m/s (`VelocidadeSuavizacaoCorrecao`), eliminando trancos de câmera.

**Área de interesse na cidade (#500):** cada um recebe só quem está perto.
- **Grade espacial:** células de **100 m** (`GradeDeInteresse.TamanhoDaCelula`). Dois personagens passam a se ver a até **150 m** (`RaioDeVisao`) e só deixam de se ver além de **180 m** (`RaioDeSaida`): a folga evita que quem anda na borda apareça e suma a cada passo. A relação é simétrica e conta só o plano horizontal.
- **Mensagens:** `EstadoDaCidade` traz só quem está no raio. `PosicaoJogador` vai só a quem vê o personagem. Quem entra no raio recebe `JogadorEntrou` (com nome e raça) um do outro; quem sai recebe `JogadorSaiu` um do outro. Sair do jogo, cair ou ir para uma instância avisa só quem via o personagem. Teleporte e retorno da queda passam pelo mesmo caminho: quem fica perto vê o salto, quem fica longe recebe a saída.
- **Instâncias** (dungeons) são pequenas e isoladas: seguem replicando para todos da área, sem grade.
- **Contrato `IVizinhanca`** (`Contratos/Mundo`): `QuemVe(personagemId)` e `PersonagensPerto(area, x, z, raio)`, para Mobs e Combate não varrerem a área inteira. Na cidade responde pela grade; em instância, pela área toda. Mobs e Combate ainda não usam: o interesse deles é outra tarefa.
- **Efeitos para quem usa a presença:** o minimapa e o mapa mostram só quem o client conhece, ou seja, quem está perto. Aliados longe do grupo precisam de outro caminho (posição do grupo, fora desta tarefa).
- **Fora do escopo:** canais do mundo e lotação (#188); trocar JSON por binário.

**Outros jogadores com a raça, o nome e a animação (#498):**
- O client cria o modelo pelo gancho `EntradaNoJogo.DefinirVisualDeOutroJogador` (Core), que Personagem preenche com o prefab da raça (`RegistroVisualJogador.CarregarVisualDaRaca`). Raça desconhecida ou sem prefab usa o prefab de reserva (Valdren); sem prefab nenhum, o Core devolve a cápsula. O Mundo não referencia Personagem.
- O objeto de cada um continua `Jogador_<id>` (Combate acha o Animator por esse nome e Itens prende a arma nele); o modelo é filho dele, sem colisor.
- **Animação:** o Animator recebe `Velocidade` (a velocidade horizontal interpolada, a mesma que move o corpo; sem deslizar parado nem andar no lugar) e `NoChao` (sempre verdadeiro por enquanto).
- **Nome:** rótulo em UI Toolkit (Lumora UI v1, `EstiloNomesDeJogadores.uss`) a 2,2 m dos pés, visível até 45 m da câmera e só à frente dela. Sem nome ainda (posição chegou antes da entrada), não mostra rótulo. Se a raça chegar depois, o modelo é trocado.

### Dia e noite (#163, contrato da #380)

Um relógio só, do servidor: todos na área veem a mesma hora, e entrar no meio do dia não recomeça o ciclo. Nesta leva a noite é **cenário**: não esconde inimigo, não muda spawn e não pede tocha (o efeito de jogo continua em aberto, S8). Este é o contrato; o servidor (#381) e o client (#382) vêm depois.

| ID | Mensagem | Direção | Campos |
|---|---|---|---|
| 1011 | `HoraDoMundo` | servidor → client | `Hora` (0 a menos de 24), `DuracaoDoDiaSegundos` |

O servidor manda `HoraDoMundo` ao entrar na área e repete de tempos em tempos para corrigir o relógio do client; entre as mensagens o client avança a hora com `RegrasDiaENoite.HoraApos` e escreve o resultado em `AmbienteDosBiomas.Hora`.

Valores em `RegrasDiaENoite`, **a calibrar**:

| Valor | Constante | Hoje |
|---|---|---|
| Duração de um dia inteiro | `DuracaoDoDiaSegundos` | 1200 s (20 min reais; uma hora do mundo = 50 s) |
| Hora quando o servidor liga | `HoraInicial` | 8h |
| Noite (lanternas acesas) | `InicioDaNoite`, `FimDaNoite` | das 20h às 5h |

**No servidor (#381):** o `RelogioDoMundo` começa às 8h quando o servidor liga e fica em memória (reiniciar o servidor volta às 8h). Quem entra na cidade recebe `HoraDoMundo` logo depois de `EstadoDaCidade`; a cada 30 s (`ConfigDiaENoiteServidor.IntervaloDeSincroniaSegundos`) todos os presentes recebem a mesma hora de novo. Quem não está na área não recebe.

**No client (#382):** o `CicloDoDia` (na cena do mundo) ouve `HoraDoMundo`, avança a hora entre as mensagens e escreve em `AmbienteDosBiomas.Hora`. Antes da primeira mensagem o client não inventa hora (fica o meio-dia de sempre). Com a hora:

- **Escuridão:** a troca é suave no crepúsculo, de 1 h antes a 1 h depois do início e do fim da noite (19h–21h e 4h–6h). O céu, a luz ambiente, a névoa e o sol escurecem; à noite a luz principal vira a lua, azul e fraca.
- **Sol e lua:** a luz principal gira no céu. Ao meio do dia (12h30) fica no ângulo calibrado da luz de dia (42°); ao nascer e ao se pôr desce a 8°; à noite a lua faz o mesmo caminho do outro lado, até 35°. O giro dá a volta sem pular. A luz de preenchimento fica sempre do lado oposto.
- **Lanternas:** acendem às 20h e apagam às 5h (`RegrasDiaENoite.EhNoite`), no meio do crepúsculo.

Valores em `ConfigDiaENoite` (client), **a calibrar** no editor.

A hora anda no mesmo ritmo o dia todo, então a noite dura 7,5 min de cada 20. A proposta do livro (40 min de dia e 20 de noite) continua em [Em aberto](#em-aberto); adotá-la é mudar constante e, se o ritmo da noite for diferente do dia, a função `HoraApos`.

### Eventos e consulta para outras funcionalidades do servidor (#119)

Em `server/src/JogoDoGrupo.Server/Contratos/Mundo/` ([ADR 0016](../decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)); só o servidor usa, o client não vê.

| Tipo | O que é |
|---|---|
| `JogadorEntrouNaArea` (`PersonagemId`, `Area`) | evento publicado quando o personagem passa a estar numa área. Pedir para entrar de novo, já estando nela, não repete |
| `JogadorSaiuDaArea` (`PersonagemId`, `Area`) | evento publicado quando o personagem deixa a área, sai do jogo ou a conexão cai. Em uma transição, a consulta global pode já apontar para a nova área |
| `IPosicoesDoMundo` | consulta pela injeção de dependência: `TentarObter(personagemId, out PosicaoNoMundo)` (área, X, Y, Z, rotação: a posição que o servidor aceitou) e `PersonagensNaArea(area)` |
| `ITransicoesDeAreaDoMundo` | operação pela injeção de dependência: `TentarMoverParaArea(personagemId, destino)` transfere um personagem ativo para a posição informada e publica saída/entrada. Os IDs de dungeon seguem `AreasDoMundo.Instancia(id)` (`instancia:<id>`) |
| `IDeslocamentosDoMundo` | pedido pela injeção de dependência: `AbrirJanela(personagemId, distanciaMetros, duracaoSegundos)` deixa o personagem se afastar até essa distância de onde está, nessa duração (mais 150 ms de folga de rede), mais rápido que a corrida; fora da janela volta o limite de 5,5 m/s. O Mundo continua validando cada posição. Usado pela esquiva (Combate, #233) |
| `ILimiteDeVelocidadeDoMundo` | pedido pela injeção de dependência: `LimitarVelocidade(personagemId, fator, duracaoSegundos)` prende a velocidade a essa fração da corrida por essa duração (mais 150 ms de folga de rede); um pedido novo substitui o anterior e fator 1 tira o teto. Sob o teto, a posição mais longe de onde ele começou do que a velocidade reduzida permite (mais 1,5 m de folga para rede e jitter) é recusada e conta como suspeita; a janela da esquiva vale antes do teto. Usado pelo preparo do golpe e pela guarda (Combate, #598) |
| `AreasDoMundo.Cidade` e `AreasDoMundo.Instancia(id)` | identificadores da cidade e das instâncias (`instancia:<id>`); cada uma mantém presença e replicação isoladas |

Os eventos saem depois das mensagens de rede da mesma mudança (`EstadoDaCidade`, `JogadorEntrou`, `JogadorSaiu`).

## Minimapa e mapa-múndi (issues #91 e #92)

~~Tudo abaixo é **proposta** para o grupo validar; nada aqui é decisão ainda.~~ Decidido em 2026-10-11, quando o mapa e o minimapa entraram no client. Norte fixo (não gira com a câmera), raio de 40 m, sem viagem rápida e sem mensagem nova do servidor. A dungeon desenhada no mapa espera a issue #66; até lá o mapa mostra o mundo de 3 km.

**Base:** WoW e New World (minimapa no canto, mapa em tela cheia). **Igual:** minimapa circular de cima; tecla M abre o mapa. **Diferente:** sem viagem rápida e sem névoa de exploração na fatia.

### Minimapa

- Fica no canto superior direito, visto de cima, centrado no jogador, com raio de **40 m**, norte para cima. Na cidade (~~40 × 40 m~~ 80 × 80 m, substituído em 2026-10-11) cobre o centro, não a cidade inteira.
- Aparece: seta do jogador (posição e direção); jogadores da mesma área; o chão que a câmera de cima já vê. ~~A **entrada da dungeon** (portão norte), NPCs e inimigos próximos como ícone próprio.~~ Substituído em 2026-10-11: NPC, inimigo e dungeon só ganham ícone quando a funcionalidade deles publicar a posição sem o Mundo referenciar outra feature. O que já está no mundo aparece na câmera de cima.
- **Visibilidade:** o minimapa mostra só o que o client já recebe. Jogadores vêm da presença da cidade (`EstadoDaCidade`, `JogadorEntrou`, `PosicaoJogador`). Inimigos e NPCs entram quando as respectivas funcionalidades enviarem posições; até lá não aparecem como ícone. **O servidor não envia nada extra** por causa do minimapa e não revela quem está fora do alcance de visão que já usa.
- ~~Jogadores do grupo (party, [social.md](social.md)) com marcador de cor diferente dos demais; inimigos em vermelho só se vivos e dentro do raio.~~ A cor de grupo espera um contrato que o Mundo possa ler sem referenciar Social. Inimigo em vermelho espera o mesmo de Mobs.
- Implementação no client: câmera ortográfica de cima para uma render texture, criada por script (sem cena/prefab à mão). Sem tecla própria na primeira versão (sempre visível); tamanho/ocultar fica para o HUD (#93). Dentro do túnel da caverna a câmera não desenha o chão de cima.
- **Fora da fatia:** zoom, ícones de recursos, rastro de caminho, marcador colocado pelo jogador.
- **Testes esperados** (client, EditMode): conversão posição do mundo para posição no minimapa (centro, borda, fora do raio); filtro por raio de 40 m; seta gira com `RotacaoY`.

### Mapa-múndi

- Tecla **M** abre/fecha uma tela cheia; Esc também fecha. O jogo continua rodando por baixo.
- A tela inteira cobre a HUD do personagem. Título, fechar, norte, legenda e coordenadas ficam sobre o próprio mapa; em telas estreitas, legenda e coordenadas se empilham. A textura de 256 × 256 amostras usa o relevo e os biomas do terreno, é gerada aos poucos na primeira abertura e reaproveitada depois. Ruína, acampamento, passagem e marco aparecem como símbolos nas posições de suas estruturas; o nome do lugar surge ao passar o mouse ou focar o símbolo, sem cobrir os lugares vizinhos.
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
