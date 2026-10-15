# Retomada da ferramenta de autoria do mundo

Este e um bilhete continuo para abrir quantos chats forem necessarios. Leia primeiro o [plano temporario](PLANO-TEMPORARIO-TERRENO.md). Ele guarda as etapas, decisoes, capturas e pendencias. As regras permanentes do jogo ficam em [mundo.md](../../../../../../docs/features/mundo.md); este bilhete guarda o estado operacional mais recente.

## Como usar em cada novo chat

1. Abra o worktree indicado abaixo, leia este bilhete e o plano, e confira o estado real dos arquivos. Datas e testes registrados aqui sao fotografias do momento em que foram escritos. Para a continuacao deste mapa, nao abra nem puxe tarefas no GitHub; acompanhe as etapas neste bilhete e no plano.
2. Trabalhe somente no **proximo objetivo** abaixo. Se ele terminar, pare antes de iniciar outro objetivo e registre qual sera o seguinte.
3. Antes de encerrar o chat, atualize neste mesmo arquivo o estado do trabalho, os arquivos ou entregas relevantes, as verificacoes feitas e as pendencias. Acrescente uma linha ao registro de progresso; mantenha o proximo objetivo em uma unica secao atualizada, sem acumular instrucoes antigas conflitantes. Atualize o plano apenas quando etapas ou decisoes mudarem.
4. Nenhum chat novo herda automaticamente as mudancas da conversa anterior. Este arquivo e o ponto de passagem entre chats; se a implementacao nao avancar, registre o impedimento e mantenha o objetivo pendente.

## Estado atual (atualizado em 2026-10-15)

- A integracao ativa esta na branch `feature/mundo/integra-mapa-terreno`, PR [#797](https://github.com/GabrielNied/jogo_do_grupo/pull/797), worktree `P:/JOGO DA GALERA/Diretorio jogo/jogo_do_grupo/.claude/worktrees/mundo-integracao`. O worktree antigo `mundo-terreno-501` guarda o prototipo e nao deve receber novas mudancas de implementacao.
- As PRs #785 e #791 ja entraram na `main`; #785 concluiu #501 e #502 (materiais, agua e vegetacao otimizada). A integracao traz a planta nova do prototipo para cima dessa base, preservando `MontadorDaVegetacao`, `BlocosDeVegetacao` e os shaders atuais. O desenho das seis rotas segue em `CaminhosDoMundo`; cidades, bairros e rios altos foram ligados ao construtor atual.
- Orientacao expressa do usuario: nesta renovacao manual do mapa 3D, nao criar nem puxar novas tarefas no GitHub. O acompanhamento fica neste bilhete e no plano. Em novo chat, apresente o proximo passo e aguarde a confirmacao dele antes de agir. Em 2026-10-15, o usuario autorizou subir esta integracao em PR para continuar o trabalho; o merge continua humano.
- O mapa anterior de aneis e setores foi descartado. A [captura integrada](../Art/MapaIntegradoEmProgresso.png) mostra a planta atual; a [captura do prototipo](../Art/MapaJogavelEmProgresso.png) permite comparar. O [conceito visual](../Art/ConceitoMapaV2.png) nao e um heightmap nem define coordenadas. As cidades continuam em (0, 0), (-520, 320) e (-900, -620); nao foram movidas nesta integracao.
- Verificacao no worktree de integracao: 1.113 testes shared, 1.227 testes server e 764 testes Unity EditMode aprovados; `npm run unity:compilar` com 0 erros e 0 avisos. Capturas em `%TEMP%/terreno-integracao.png` e `%TEMP%/terreno-integracao-mapa.png`. A mancha oval de neve no nordeste e o nucleo murado pequeno ja existiam na planta e continuam pendentes de revisao visual.
- A analise inicial confirmou que `CaminhosDoMundo` guarda seis polilinhas usadas pelo material e pela vegetacao, enquanto `MapaDoMundo` fornece a altura oficial em grade de 10 m.
- O usuario confirmou a rota Cidade central--Passo, a autoria por cliques na vista 3D e a acomodacao automatica do relevo. A entrega da estrada foi enviada ao PR #797: `TracadoDoMundo` e `EstradaDoPasso` no shared, janela `FerramentaDeEstradas` com previa editavel, superficie visual em `MontadorDaEstrada`, captura e documentacao. O usuario manteve 7 m e pediu trocar a textura procedural pontilhada e suavizar a transicao lateral. A segunda captura `%TEMP%/terreno-estrada-passo.png` mostra terra fotografica CC0 Poly Haven e borda com opacidade gradual; o usuario confirmou seguir com esta direcao visual. A ferramenta de rio continua para outro objetivo.
- Verificacoes desta rodada: `dotnet test JogoDoGrupo.slnx -c Release --no-restore` aprovado; 129 testes de Mundo no shared aprovados; `npm run unity:compilar` sem erros nem avisos; Unity executou `FerramentaDeEstradas.VerificarPrevia` e gerou duas versoes da captura, incluindo a atual com foto e borda suave. `npm run conferir-unity` aprovado com os arquivos da entrega no stage. Shader e textura foram importados pelo Unity; o shader fica em `Resources` para ser incluido na build. `npm run testar-scripts` teve 120 aprovacoes e tres falhas preexistentes nos testes dos hooks Git/LFS; o comando de compilacao Unity foi ajustado para restaurar dependencias pelo cache local antes de tentar a rede.

## Proximo objetivo

O usuario esclareceu o objetivo em 2026-10-14: construir **uma ferramenta de autoria procedural do mundo**. Ele quer desenhar uma linha e obter uma estrada acompanhando o terreno; em outra operacao, desenhar o curso de um rio e fazer o terreno afundar para formar o leito e a agua aparecer. Outros elementos do mapa devem poder usar a mesma ideia de tracados e parametros. Presumir uma ferramenta de edicao no Unity, destinada a quem cria o mundo, ate que o usuario indique que deseja desenho durante a gameplay.

Depois da entrega da estrada Cidade central--Passo, retomar a operacao **Rio** na ferramenta de autoria. Reutilizar `TracadoDoMundo` como base e confirmar com o usuario a forma de definir nivel da agua, largura variavel e margens antes de implementar a escavacao do leito. O resultado precisa ser deterministico e reproduzivel no `shared`, porque cliente e servidor devem consultar a mesma altura oficial. Encerrar este chat apos entregar a estrada; nao comecar o rio nesta rodada.

Preservar a planta jogavel atual como referencia. Nao mover cidades, redesenhar biomas, rios existentes, bairros ou vegetacao nesse objetivo. Obter a confirmacao do usuario antes de codificar a ferramenta; nao abrir nem puxar uma tarefa para ela.

O afastamento das cidades permanece pendente para outro objetivo: a cidade inicial esta em `(0, 0)`, o Passo em `(-520, 320)` e o Oasis em `(-900, -620)`; o Passo, a cerca de 611 m da cidade central, foi considerado perto demais. Nao resolver isso no proximo chat da ferramenta.

## Registro de progresso

| Data | Resultado | Verificacao ou impedimento |
|---|---|---|
| 2026-10-14 | Protótipo do novo terreno, materiais, margens, rotas e cidades provisórias preservado no worktree; escopo seguinte redefinido como ferramenta de autoria. | 1037 testes .NET aprovados e client compilado antes da mudanca de escopo; #501 livre, mas reserva recusada pelo limite de tres funcionalidades. |
| 2026-10-15 | Usuario informou que outro agente assumiu a #501 e decidiu acompanhar a continuacao manual do mapa neste bilhete e no plano, sem novas tarefas no GitHub. | Ferramenta de autoria aguarda a conclusao da #501 e confirmacao do usuario; nenhuma implementacao iniciada nesta retomada. |
| 2026-10-15 | #501 e #502 foram incorporadas pela PR #785. Planta nova integrada sobre a `main` no worktree `mundo-integracao`, com rios altos, caminhos, cidades e vegetacao em lotes. | 2.340 testes .NET e 764 testes EditMode aprovados; compilacao Unity sem erros. Nenhum commit, push ou PR feito nesta integracao. |
| 2026-10-15 | Retomada da ferramenta de autoria: estado e pontos de integracao examinados; duas escolhas de interacao foram apresentadas ao usuario. | Sem implementacao nem novos testes nesta rodada; aguarda confirmacao do fluxo de desenho e do primeiro trecho. |
| 2026-10-15 | Primeira versao da autoria da estrada central--Passo no worktree, com relevo compartilhado, janela de edicao e captura visual para revisao. | 129 testes de Mundo, compilacao Unity e abertura da previa aprovados. |
| 2026-10-15 | Largura de 7 m mantida; textura pontilhada trocada por fotografia CC0 Poly Haven e bordas da malha passaram a misturar com o terreno. | 129 testes de Mundo e compilacao Unity aprovados; captura atual em `%TEMP%/terreno-estrada-passo.png`, com direcao visual aprovada pelo usuario. |
| 2026-10-15 | Direcao visual da estrada aprovada pelo usuario; entrega enviada ao PR #797. Proximo objetivo registrado como a operacao Rio, em outra rodada. | Solucao .NET em Release, compilacao Unity, previa no Unity e conferencia dos assets aprovadas; tres testes antigos de hook Git/LFS seguem falhando na suite de scripts. |

Ao terminar toda a renovacao do mapa, remover este bilhete, o plano temporario e seus `.meta` depois de transferir as decisoes permanentes para a documentacao da funcionalidade.
