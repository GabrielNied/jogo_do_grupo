# Plano temporario: qualidade do mundo

**Inicio:** 2026-10-14. **Dono:** Mundo. **Estado:** planta e estrada do Passo mergeadas no PR #797; autoria de biomas no PR #871, branch `feature/mundo/integra-mapa-terreno-segue`; proxima etapa e autoria procedural de lagos.

Este arquivo acompanha a renovacao do mapa enquanto ela atravessa varias tarefas. Nao e a fonte das regras do jogo: decisoes finais vao para [mundo.md](../../../../../../docs/features/mundo.md) e, se forem tecnicas, para um ADR. O [guia de uso do criador de mapas](../../../../../../docs/guia-criador-de-mapas.md) e permanente. **Remover este arquivo e seu `.meta` ao concluir a ultima etapa**, depois de conferir que as decisoes permanentes e os links necessarios estao na documentacao final.

## Objetivo

Um mundo persistente de 3 km com sete biomas e tres cidades, legivel e interessante a pe: caminhos com destinos, desvios e marcos; floresta com cobertura vegetal; margens e encostas sem aspecto de grade; materiais proprios por bioma. Preservar a altura oficial compartilhada entre cliente e servidor.

## Referencias de aceite

- Capturas fornecidas na conversa em 2026-10-14: floresta parecendo areia, pantano com bordas quadradas e areia clara, colinas listradas, neve com rocha generica, deserto vazio e lava com facetas e liquido grosseiros.
- Comparar cada mudanca nas mesmas posicoes, ao nivel do personagem e em vista elevada. Conferir campo/mata, pantano, colinas, neve, deserto e lava. Registrar onde as capturas foram salvas.
- Conferir trajetos jogaveis entre as cidades e pontos de interesse; nenhuma rota obrigatoria depende de atravessar agua, lava ou declive acima do limite do personagem.
- Medir entrada no mundo, memoria e tempo de quadro no Unity antes de aumentar resolucao ou quantidade de objetos. Nao trocar desempenho por detalhe que o jogador nao percebe.

## Etapas

| Ordem | Trabalho | Criterio de conclusao | Tarefa |
|---|---|---|---|
| 1 | Materiais explicitos: separar pesos de material da cor macro; solo florestal, lama de pantano, areia de deserto, rocha e neve proprias por bioma. Corrigir listras repetitivas. | Mata verde/terrosa, pantano com lodo marrom, neve sem encosta verde generica, rocha legivel de perto e de longe. | #501 concluida; integrada a planta nova |
| 2 | Agua e lava: contorno das margens pela intersecao com o terreno; curso e altura proprios para rios; movimento e aparencia dos liquidos. | Sem quadrados nas margens, sem rio alto seco, agua e lava reconheciveis e sem falhas de malha. | #501 concluida; rios altos integrados |
| 3 | Encostas: perfis de crista e ravina guiados; detalhe de rocha onde a silhueta importa; textura e normal de qualidade. | Encosta aspera sem listras nem triangulos expostos, colisao coerente com o chao oficial. | silhueta e qualidade visual ainda em revisao |
| 4 | Mapa novo: planta autoral com tres cidades, caminhos principais, desvios, pontos de interesse, rio e delta; biomas de formas assimetricas. | Cidade a cidade por mais de uma rota interessante, sem quadrado central nem quatro setores obvios. | em andamento neste plano; sem nova tarefa |
| 5 | Vegetacao e identidade: bosque, clareira, grama, flores, raizes, cactos, pinheiros e pedras em conjuntos que acompanham agua e relevo. | Cada bioma e reconhecivel sem depender so da cor do chao; caminhos permanecem legiveis. | #502 concluida; distribuicao da planta nova integrada, arte ainda em revisao |
| 6 | Ajuste final: medir e aplicar carregamento por distancia, LOD e instanciamento conforme o Profiler. | Entrada e travessia sem pico perceptivel e qualidade visual preservada. | acompanhar neste plano; sem nova tarefa |

## Decisoes de trabalho

- O mundo continua fixo por semente. O desenho das rotas e dos lugares sera deterministico, com variacao procedural apenas onde nao prejudicar a navegacao.
- A etapa 1 mantem a malha e os colisores existentes. Mudancas de geometria entram nas etapas 2 a 4 com validacao da altura no `shared`.
- Em 2026-10-14 o usuario descartou explicitamente usar o mapa atual como base. A referencia agora e a planta nova abaixo. As melhorias de material, liquidos e malha podem ser reaproveitadas; setores radiais, dois rios paralelos, miolo quadrado e posicoes das cidades propostas antes ficam descartados.
- O usuario autorizou em 2026-10-15 o PR da integracao manual no worktree `mundo-integracao`, para que o trabalho possa continuar. #501 e #502 foram concluidas na PR #785. O merge continua humano.
- Por orientacao expressa do usuario em 2026-10-15, a continuacao manual desta renovacao do mapa nao deve criar novas tarefas no GitHub nem puxar tarefas para as proximas etapas. Registrar o andamento em [RETOMADA.md](RETOMADA.md) e neste plano. Antes de agir em novo chat, pedir confirmacao ao usuario.
- Pausa solicitada em 2026-10-14: concluir apenas a verificacao dos bairros florestais desta rodada e parar antes de redesenhar o nucleo ou mover cidades. O Passo atual fica a cerca de 611 m da cidade central e foi considerado perto demais; revisar em conjunto seus patios, rotas, rios e modificadores ao escolher novas posicoes. As coordenadas abaixo sao provisiorias. O afastamento das cidades foi adiado para outro objetivo.
- Escopo esclarecido pelo usuario em 2026-10-14: construir uma ferramenta de autoria procedural, na qual desenhar um tracado gera uma estrada e desenhar outro tracado pode escavar um rio com agua. A primeira entrega e um tracado editavel que produz uma estrada; o rio usa a mesma base em etapa posterior. A autoria visual fica no Unity, mas os dados e o calculo de altura precisam ser reproduziveis no shared para cliente e servidor. Preservar a planta atual e nao mover cidades nem redesenhar biomas ou bairros nessa rodada. Nao abrir nem puxar tarefa para a ferramenta. Detalhes de retomada em [RETOMADA.md](RETOMADA.md).
- Confirmado pelo usuario em 2026-10-15: selecionar "Estrada" no editor, clicar pontos na vista 3D e ajustar parametros com previa imediata. A primeira rota e Cidade central--Passo; o relevo deve receber cortes e aterros suaves que deem aspecto de intervencao humana. A imagem enviada como referencia mostra terra batida larga, bordas organicas e vegetacao junto as margens. O usuario manteve 7 m, rejeitou o pontilhado procedural e pediu transicao gradual com o terreno lateral. A segunda captura em `%TEMP%/terreno-estrada-passo.png` usa fotografia CC0 Poly Haven e opacidade lateral suave; o usuario aprovou seguir com essa direcao visual.
- Esclarecido e confirmado pelo usuario em 2026-10-15: a edicao manual conjunta demorava e ficava longe do resultado desejado. O objetivo passou a ser um editor procedural em que ele desenha o mundo, ajusta parametros e ve as regras gerarem o ambiente. A ordem de desenvolvimento e autoria e **biomas → lagos → estradas**. Biomas geram relevo base, texturas e vegetacao; lagos formam bacia, agua e margens; estradas acomodam o terreno, criam piso e abrem passagem. A estrada do Passo e so o primeiro prototipo ja entregue, nao define a ordem seguinte. Atualizar o guia permanente de uso em cada entrega; nao apaga-lo com este plano.
- Confirmado pelo usuario em 2026-10-15 para os biomas: desenho por pontos e alcas na vista 3D, sete tipos disponiveis, mistura gradual ajustavel, regioes novas por cima das antigas, previa no mapa inteiro e controles de relevo e densidade vegetal. A mata de exemplo fica a leste da cidade inicial; o usuario pediu arvores altas com troncos espacados para caminhar e copas proximas na vista superior, aprovou a previa e autorizou salvar no mapa oficial. Os modelos de arvores ainda serao trocados por arte mais realista. A operacao de biomas foi concluida nesta ordem; o proximo trabalho e lagos.

## Andamento

- [x] Analise do codigo e das oito capturas.
- [x] Worktree `feature/mundo/materiais-e-agua-do-terreno` criado para a #501; o usuario informou que outro agente assumiu a issue. Nao tentar refazer a reserva.
- [x] Etapa 1 implementada e conferida no Unity: pesos de bioma separados da cor macro, materiais CC0 por solo, encosta de neve e lodo marrom. A mata ainda precisa de densidade vegetal (etapa 5).
- [x] Etapa 2 implementada e conferida no Unity: margens da agua e lava recortadas pelos triangulos do chao, shader de liquidos e rios altos com leito e nivel continuo. A agua precisa de reflexos e variacao mais rica na etapa de arte; conferir custo no Profiler na etapa 6.
- [ ] Concluir a silhueta das encostas na etapa 3 e o desenho de tres cidades na etapa 4, registrando as proximas etapas neste plano sem abrir novas tarefas.
- [ ] Decisoes permanentes transferidas para `docs/features/mundo.md`/ADR; plano temporario removido.

## Evidencia e proximos ajustes

- Capturas do Unity em `%TEMP%/terreno-rio-calha-larga-*.png` (2026-10-14). A vista `rio-alto` mostra a calha larga com agua continua; `pantano`, `neve`, `mata`, `deserto`, `colinas` e `lava` permitem comparar as mesmas posicoes.
- O shader de terreno usa muitas amostras de textura por pixel. Medir GPU e memoria antes de aumentar detalhes; simplificar as amostras se o custo aparecer no Profiler.
- O aspecto vazio do deserto, a baixa densidade da floresta e as silhuetas lisas das montanhas continuam nas etapas 3 a 5. Nao considerar textura isolada como solucao para esses pontos.
- A malha dos rios altos ainda e uma superficie sem reflexo e sem quedas locais. Na etapa 4, conferir o percurso completo, travessias e quedas d'agua antes de encerrar a hidrologia.
- Testes EditMode de Mundo aprovados (2026-10-14, 690 executados no total). O comando geral ainda falha por 8 testes de Core, Combate e Itens fora desta funcionalidade; nomes e resultados estao no XML `%TEMP%/jogo-do-grupo-unity-testes.xml`.
- Materiais `Resources/Terreno/Chao` e `Liquido` criados pelo Unity para manter os shaders referenciados no build.
- Inicio da etapa 3: novas capturas `%TEMP%/terreno-antes-encostas-colinas-alto.png` e `-mapa.png` mostram relevo pouco legivel a oeste e os quatro setores externos ainda muito evidentes. As dorsais das colinas e a erosao das montanhas foram adicionadas no shared; falta conferir no Unity e ajustar.
- A vista elevada das colinas mostrou que o rio oeste passava pela ruina e pela passagem. O curso foi curvado para oeste nesses pontos; conferir a vista superior e os acessos depois que o Unity liberar as capturas.
- A rocha das encostas foi trocada de cascalho para uma face rochosa CC0 com normal map, e o solo vulcanico por rocha escura. Capturas `%TEMP%/terreno-face-rochosa-*.png` mostram a parede rochosa mais legivel e a ruina livre do rio oeste. A silhueta da montanha ainda e lisa; a amplitude do detalhe fisico foi aumentada no shared, com 45 testes de mapa aprovados, mas continua discreta na vista do vale (`%TEMP%/terreno-relevo-fino-rio-alto.png`). Nao marcar a etapa 3 como concluida apenas pelo normal map.
- A neve e a areia ainda repetiam marcas reconheciveis a cada 4 m. O shader agora usa amostragem maior e rotacionada nesses dois materiais. A areia em `%TEMP%/terreno-solo-amplo-deserto.png` perdeu as listras regulares; a neve mostrou marcas escuras ampliadas, corrigidas com menor contraste em `%TEMP%/terreno-neve-suave-neve.png`.
- A tentativa de restaurar o CSharpier pela rede falhou por bloqueio de socket. A DLL 1.3.0 ja presente no cache local formatou os arquivos C#; nenhuma dependencia nova foi instalada.
- A experiencia com a fronteira radial (`%TEMP%/terreno-fronteira-radial-mapa.png`) mostrou que trocar o quadrado por um circulo mantinha as quatro fatias. Essa variante foi descartada ao iniciar a planta nova. O leito precisa continuar sendo escavado depois da altura de transicao para nao secar.
- A vegetacao da #502 foi integrada a planta nova em 2026-10-15; continuar a revisao visual sem abrir outra tarefa.

## Planta nova da etapa 4 (2026-10-14)

Referencia visual: [ConceitoMapaV2.png](../Art/ConceitoMapaV2.png), gerado para esta nova planta; e direcao artistica, nao heightmap nem fonte de coordenadas exatas. A fonte do mapa jogavel sera o shared, com marcos e rotas deterministicas. A foto do mapa antigo em `%TEMP%/terreno-antes-encostas-mapa.png` serve somente como comparacao do problema que nao deve voltar.

Estado jogavel atual: [MapaJogavelEmProgresso.png](../Art/MapaJogavelEmProgresso.png), captura do Unity de 2026-10-14. Manter esta imagem para comparar as proximas iteracoes; remover junto com este plano quando o mapa e as cidades estiverem concluidos.

Integracao de 2026-10-15: [MapaIntegradoEmProgresso.png](../Art/MapaIntegradoEmProgresso.png), vista superior do Unity com os materiais e a agua de #501 e a vegetacao em lotes de #502. A vista alta nao desenha as arvores distantes por causa do corte por distancia; usar capturas proximas para julgar densidade.

- A cidade inicial permanece na origem por compatibilidade de entrada, mas seu entorno vira uma floresta central com clareiras de formas irregulares. O quadrado murado atual e provisorio e precisa de redesenho proprio, maior e com bairros, sem usar a disposicao antiga como planta.
- Cidade do Passo em (-520, 320), no pe acessivel da serra nevada do noroeste; patio compartilhado plano, pedra e madeira. Oasis em (-900, -620), no deserto sudoeste, com ruas em torno da agua e pedra clara. Sao assentamentos distintos, nao copias simetricas da cidade inicial.
- Serra nevada recortada no noroeste com linguas no norte; deserto em crescente oeste-sudoeste; pantano e delta ramificado no leste; caldeira e lava no sudeste e sul. Mata atravessa o centro como rede de bosques, vales e clareiras. Colinas e campos sao faixas locais, nao setores iguais. A nova regra de biomas usa massas elipticas sobrepostas e coordenadas desviadas, sem aneis nem divisoes diagonais.
- O rio principal nasce na neve e dobra para leste da cidade florestal, terminando no delta do pantano; um afluente curto vem da serra e se une ao norte. Testar margens, nivel continuo, passagens e conflito com os tres patios. Nada de dois rios norte-sul paralelos.
- Rotas previstas: cidade central--Passo pelo vale oeste; central--Oasis pelas colinas secas; Passo--Oasis pelo ombro oeste da serra. Um desvio oriental entre floresta e pantano fecha outra volta. Cada bifurcacao recebe um marco legivel e uma alternativa de retorno. O caminho seguro para a caldeira para antes das linguas de lava.
- A primeira malha nova e uma prova do macro mapa. Antes de preencher as cidades e estradas, comparar sua vista inteira com o conceito, verificar se as tres regioes e o rio ficaram coerentes e se as rotas propostas cabem no relevo. Nao fechar esta etapa com cidades apenas decorativas sem acesso.

## Verificacao da planta nova

- O mapa jogavel agora usa massas de biomas sobrepostas e deformadas por ruido, com a mata conectando as regioes. A geracao antiga em aneis e fatias foi retirada do `MapaDoMundo`.
- As tres pracas tem altura oficial no shared e modificadores suaves. A primeira posicao proposta para o Passo, mais ao norte e acima da serra, formava um poco circular artificial; a cidade foi movida para o pe da serra em (-520, 320).
- Os eixos de seis rotas fixas estao no `CaminhosDoMundo`: triangulo entre as cidades, trilha oriental, acesso seguro a caldeira e subida nevada. O terreno visual pinta as rotas; arvores e pedras evitam o corredor. Falta conferir se as travessias dos rios realmente permitem passagem.
- Capturas da primeira versao: `%TEMP%/terreno-novo-mundo-primeiro-mapa.png`, `-cidade-neve.png` e `-cidade-deserto.png`. Esta versao ainda parecia vazia e a cidade da neve estava mal posicionada; comparar com as capturas seguintes.
- Nao encerrar a etapa 4 enquanto o quadrado da cidade central continuar e os assentamentos externos forem apenas agrupamentos das pecas antigas.
- A captura `%TEMP%/terreno-novo-mundo-rotas-mapa.png` mostrou a floresta central muito vazia e o Passo marrom. A captura `%TEMP%/terreno-novo-mundo-vale-mapa.png` mostrou uma mancha circular de neve isolada e um vale com borda alta. A faixa nevada foi ligada a serra e o patamar da cidade alargado; conferir nas proximas capturas.
- A trilha oriental recebe um vau raso em (385, 109), com profundidade controlada no shared. Conferir agua e chegada pelas duas margens em vista ao nivel do jogador.
- Primeira revisao provisoria da vegetacao: o miolo deixa de usar as antigas fatias fixas, bosques da mata recebem mais arvores e o deserto favorece cactos. Medir o impacto na entrada do mundo antes de fechar a etapa 5; caso necessario, trocar os objetos repetidos por instanciamento/LOD.
- Capturas `%TEMP%/terreno-novo-mundo-bairros-mapa.png`, `-cidade-central-alto.png`, `-cidade-neve.png` e `-vau-leste.png`: o mapa ja tem floresta central, tres assentamentos e rotas identificaveis, mas o nucleo inicial de 80 m ainda parece um quadrado isolado. Os pequenos bairros fora dele iniciam uma planta urbana maior; nao considerar a cidade concluida.
- Proxima frente da etapa 4: trocar o nucleo quadrado por pracas e ruas assimetricas preservando o ponto de nascimento; conectar os bairros com caminho visivel; diferenciar a silhueta, o piso e os marcos do Passo e do Oasis; acrescentar destinos e retornos as trilhas. Rever o vau ao nivel do personagem, inclusive profundidade, material e declive nas duas margens.
- Proxima frente da etapa 5: formar copas continuas em bosques, abrir clareiras jogaveis, adicionar vegetacao rasteira e substituir os modelos temporarios de cada bioma. Conferir o custo de objetos e de sombras antes de subir mais a densidade. O desenho do solo sozinho nao produz floresta.
- A checagem completa do shared encontrou a caverna fora da faixa de colinas, o acampamento antigo no campo e o marco do portao na mata. Uma dorsal local preserva a caverna, o acampamento foi transferido para (350, -250), e a saida norte ganhou uma clareira pequena. Os 1037 testes .NET passaram depois desses ajustes.
- O deserto recebeu duas dunas largas e suaves, longe do patio do Oasis, e uma proporcao maior de cactos; comparar a proxima captura ao nivel do personagem com `%TEMP%/terreno-novo-mundo-bosques-deserto.png`.
- A captura `%TEMP%/terreno-novo-mundo-dunas-deserto-dunas.png` ainda nao mostra uma silhueta forte de dunas na entrada do Oasis; a casa ocupa a vista e o relevo distante permanece discreto. Rever os enquadramentos e o perfil do deserto na etapa de arte, sem criar declives que bloqueiem a rota oeste. A cidade nevada em `-cidade-neve-serra.png` ja esta sobre neve e tem encosta acessivel, mas continua com arquitetura provisoria.
