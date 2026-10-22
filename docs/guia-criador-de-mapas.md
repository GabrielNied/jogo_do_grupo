# Guia de uso do criador de mapas

Este guia ensina a usar as ferramentas de mapa dentro do **editor Unity**. Ele é permanente: cada nova operação deve ganhar aqui um passo a passo antes de ser considerada entregue. As regras do mundo ficam em [Mundo](features/mundo.md). O contexto provisório de trabalho fica fora do repositório.

## Direção da ferramenta

O objetivo é **pintar o mundo**, sem montar árvore, textura e morro um a um nem editar código. Você escolhe uma receita, marca sua forma no mapa, ajusta poucos controles, vê a prévia e salva quando ela ficar jogável. A receita reúne as regras do lugar: solo, relevo, vegetação, pedras e limites de passagem.

O primeiro foco foi fazer a receita **Mata** realmente boa. Ela tem solo de floresta, relevo suave, troncos com espaço para caminhar e copas que formam uma massa vista de cima; foi aprovada no Unity. A próxima versão do mapa passa a usar o **Terrain nativo**: o relevo atual é exportado como ponto de partida editável no Unity, sem apagar a montagem procedural enquanto a conversão é validada.

A ferramenta deve crescer como um único **Pintor do Mundo**. Lago usa uma área fechada; estrada usa uma linha. O [criador de estradas no Terrain](#criar-estradas-medievais-no-terrain) permite novas vias independentes com splines nativas. A estrada Cidade central → Passo descrita mais abaixo continua como um protótipo do mapa procedural anterior.

## Desenhar biomas

No Unity, abra **Jogo do Grupo > Mundo > Autoria de biomas**. A janela **Biomas do mundo** monta uma prévia temporária do mapa 3D inteiro na aba **Scene**. A **Mata de exemplo** a leste da cidade inicial já está salva no mapa oficial. As mudanças que você fizer nela ou em outras regiões ficam apenas na prévia até clicar em salvar.

1. Escolha uma região na lista ou clique em **Nova região**. Dê um nome e escolha um dos sete biomas: campo, mata, colinas, deserto, neve, lava ou pântano.
2. Ative **Adicionar ponto por clique** e clique no chão da vista **Scene** em pelo menos três lugares. A ferramenta fecha o contorno entre o último ponto e o primeiro. Use **Enquadrar região** para aproximar a câmera.
3. Desative **Adicionar ponto por clique**. Arraste as alças amarelas na vista 3D, altere as coordenadas na janela ou use **×** para tirar um ponto. A prévia é refeita após parar de arrastar.
4. Ajuste **Mistura da borda** para suavizar a passagem entre biomas, **Intensidade do relevo** para aumentar ou reduzir a forma do chão, **Densidade da vegetação** para mudar a quantidade de árvores e peças naturais e **Enterrar árvores** para rebaixar levemente os troncos no terreno. Árvores não nascem em inclinações fortes e permanecem em pé.
5. Se duas regiões se cruzarem, a que estiver mais à frente na lista redefine o centro da área; a borda mistura o resultado com a região anterior. Use **Trazer para frente** e **Enviar para trás** para mudar essa ordem.
6. Observe o relevo, o material do solo e a vegetação de perto e de longe. O editor mantém os pátios das cidades e suas transições protegidos, sem deslocar as cidades.
7. Clique em **Salvar regiões no mapa compartilhado** para tornar o desenho oficial. Aguarde a recompilação. Entre no mundo pelo fluxo normal e visite a região para conferir o resultado. A altura salva é a mesma que o cliente desenha e o servidor usa.

A prévia não é salva na cena. **Reverter** descarta o rascunho atual e recarrega as regiões salvas; se ainda não houver nenhuma, mostra a mata de exemplo. Para recuperar uma versão antiga depois de salvar, use o histórico do projeto. O mapa inteiro aparece na prévia, mas as malhas editadas são atualizadas por região; em máquinas mais lentas, a resposta pode levar alguns segundos. O desenho não altera rios ou cria lagos e estradas: essas são operações posteriores.

## Criar o Terrain nativo inicial

No menu do Unity, abra **Jogo do Grupo > Mundo > Gerar Terrain nativo a partir do mapa**. O comando cria ou atualiza `TerrenoDoMundo.asset` a partir da altura atual do mundo de 3 km. Ele é o ponto de partida da edição manual: gerar de novo substitui somente essa cópia pelo relevo procedural mais recente; não altera o mapa compartilhado, o servidor ou a montagem atual.

Para comparar visualmente sem trocar o mapa do jogo, use **Jogo do Grupo > Mundo > Montar prévia do Terrain nativo**. A prévia recebe colisor nativo e é criada na cena aberta; use **Remover prévia do Terrain nativo** ao terminar. Não salve essa cena: o asset do Terrain é a única saída persistente desse passo.

## Reunir texturas para pintar o Terrain

As imagens dos pacotes ficam nos locais de origem. A pintura usa **Terrain Layers**, que associam a textura do chao aos seus mapas de detalhes; texturas de personagens, objetos e mapas de altura nao entram nessa lista.

1. Abra **Jogo do Grupo > Mundo > Texturas do terreno > Selecionar camadas disponiveis**. A janela **Project** seleciona as camadas originais dos pacotes. Nenhum arquivo e criado, copiado ou movido, e nada e executado automaticamente ao carregar os scripts.
2. Para escolher poucas camadas, selecione **Hub/TerrenoFora/ChaoNativo** na **Hierarchy**. No **Inspector**, abra **Paint Terrain > Paint Texture > Edit Terrain Layers > Add Layer** e escolha as camadas desejadas.
3. Para disponibilizar todas de uma vez, com esse mesmo objeto selecionado, abra **Jogo do Grupo > Mundo > Texturas do terreno > Adicionar ao terreno selecionado**. As camadas existentes e sua pintura sao preservadas; camadas ja presentes nao sao adicionadas novamente. **Ctrl+Z** desfaz a adicao. Muitas camadas podem aumentar o custo de renderizacao: acrescente somente as necessarias ao mapa quando possivel.
4. Escolha uma camada na lista de pintura e pinte normalmente. Salve o projeto depois de editar e use o fluxo de publicacao do mapa.

Se usou a primeira versao do organizador, saia do Play e execute **Jogo do Grupo > Mundo > Texturas do terreno > Corrigir copias criadas pelo organizador**. O comando identifica apenas as copias intactas daquela ferramenta, troca suas referencias pelas originais e soma os pesos de pintura quando a mesma camada aparecia duas vezes. Copias editadas ou ainda usadas por outros assets ou terrenos nao salvos sao preservadas. Antes de modificar TerrainData ou remover copias, grava os arquivos e metadados em `client/Library/BackupCamadasDeTerreno/<instante>/Assets/`; o Console informa o caminho. A limpeza nao tem Undo: a recuperacao usa esse backup com o editor fechado. Os pacotes originais continuam instalados e suas licencas permanecem aplicaveis.

## Criar estradas medievais no Terrain

Esta ferramenta serve para caminhos de terra compactada por pessoas e cavalos, com o centro levemente mais baixo que as margens, e para trechos de lajotas. Trabalhe fora do Play na cena de autoria. Salve a cena antes de começar.

1. Selecione o Terrain na **Hierarchy** e abra **Jogo do Grupo > Mundo > Estradas > Criar estrada de terra**. Na cena de autoria, a estrada fica sob `Hub`, para acompanhar a publicação do mapa.
2. Selecione a nova estrada e edite o componente **Spline Container** com as ferramentas nativas de Splines na vista **Scene**. Mova e acrescente pontos para definir o caminho. Cada estrada é independente; não há extremos presos às cidades.
3. No Inspector da estrada, escolha terra ou lajotas e ajuste largura, rebaixo e bordas. Configure também a margem de limpeza da vegetação e os detalhes opcionais. O rebaixo é suave: comece pequeno e observe a escala do personagem.
4. Ative **Prévia do corredor na cena** para conferir as linhas do traçado e da faixa de limpeza. Clique em **Aplicar e salvar** quando estiver satisfeito: essa operação modifica a altura do Terrain, limpa árvores, plantas e grama nativas dentro da faixa configurada e salva a cena e os assets, inclusive quando a estrada passa de um bloco a outro.
5. Depois de editar a curva ou os parâmetros, clique em **Reaplicar e salvar**. A ferramenta recupera sua base anterior antes de refazer o resultado, evitando aprofundar o solo a cada aplicação. Use **Restaurar terreno e vegetação** para recuperar o trecho anterior. Restaure antes de apagar o objeto da estrada.
6. Salve a cena e o projeto. Para levar o resultado ao jogo e ao servidor, siga o comando existente **Jogo do Grupo > Mundo > Publicar mapa feito à mão**. A estrada aplicada permanece somente na autoria até publicar.

As árvores removidas são as pintadas por **Paint Trees** ou pelo pincel de vegetação; a grama e as plantas são as de **Paint Details**. Objetos decorativos colocados manualmente precisam ser movidos à mão. A limpeza não procura objetos pelo nome nem altera prefabs de espécies.

Preserve os dados de recuperação da estrada enquanto precisar restaurar o trecho. Se você editar o mesmo solo ou vegetação depois da aplicação, a ferramenta pode indicar conflito para impedir a substituição dessas alterações. Restaure antes de repintar a faixa. As áreas fora do corredor continuam editáveis.

Nesta versão, áreas modificadas por estradas diferentes não podem se sobrepor: a ferramenta informa o conflito. A resolução do Terrain limita depressões estreitas; o material fornece o detalhe fino. O servidor continua usando uma grade de alturas de **10 m**, que pode não captar o microperfil de uma estrada estreita. Mantenha o rebaixo pequeno; a superfície visual não cria um colisor independente nem altera a navegação automaticamente.

O padrão é largura de **4 m**, rebaixo de **12 cm**, **Transição visual de 1,5 m**, **Suavização do relevo de 1 m** e margem adicional de limpeza de **0,5 m**. O rebaixo tem limite de **35 cm**. Aumente **Transição visual (m)** para prolongar a transparência entre estrada e grama, sem mudar o afundamento. **Suavização do relevo (m)** controla apenas a depressão física (era o antigo campo “Transição”). A faixa azul da prévia marca o limite da mistura, e a verde marca a limpeza, calculada pela maior das duas faixas mais a margem. Clique em **Reaplicar e salvar** depois de ajustar; mudar o Inspector não refaz a malha automaticamente.

Ative **Cascalhos** e ajuste **Cascalhos por m²** (padrão **2**, até **8**) e **Cor dos cascalhos** (padrão terroso, independente das lajotas). Há pedras pelo leito e pelas margens, agrupadas em uma malha, sem um objeto por pedra. O limite é **8.192 pedras por estrada**, com aviso no Console quando a densidade solicitada exceder esse orçamento; divida caminhos muito longos em trechos para preservar o detalhe. A semente mantém bordas e pedras reproduzíveis. Um material próprio pode substituir o padrão pelo campo do Inspector, mas seu shader precisa respeitar o alpha da cor dos vértices para suavizar as bordas; materiais opacos não fazem essa mistura.

A aplicação também reduz o erro de simplificação visual do Terrain somente nos blocos tocados, para que o solo não atravesse a superfície da estrada à distância. Isso pode aumentar a quantidade de triângulos renderizados nesses blocos. Quando a última estrada do bloco é restaurada, a configuração anterior volta; um ajuste manual posterior de precisão é preservado.

As saídas e os backups ficam em `Assets/_Game/Features/Mundo/EstradasGeradas/<id>/`; mantenha essa pasta junto da estrada ao versionar o mapa. Para uma nova estrada, use o menu: duplicar o objeto também duplica sua identidade e por isso é recusado. Pontes e estradas sobre buracos do Terrain precisam de autoria própria.

Para experimentar sem mexer no mapa, use **Jogo do Grupo > Mundo > Estradas > Criar demonstração isolada**. O comando cria uma cena própria com duas estradas, dois blocos de terreno e vegetação de demonstração. A validação reproduzível, com o editor fechado, é:

```text
npm run unity:executar -- JogoDoGrupo.Client.Features.Mundo.Editor.Estradas.DemoDasEstradas.Validar
```

O comando mantém os assets da demonstração para inspeção e grava a captura local em `client/Library/ValidacaoEstradas.png`. A demonstração não publica nem altera o mapa de autoria.

Para conferir só a independência das bordas/relevo e a densidade reproduzível dos cascalhos, sem criar assets ou salvar cenas, use **Jogo do Grupo > Mundo > Estradas > Validar bordas e cascalhos**. A mesma conferência também faz parte da validação completa acima.

## Protótipo da estrada do Passo

A ferramenta permite **redesenhar a estrada Cidade central → Passo**. Você pode clicar pontos no terreno 3D, mover os pontos intermediários e ajustar a aparência da passagem enquanto vê uma prévia. A largura inicial é de **7 m**.

Nesta ferramenta antiga, a opção “Estrada” e a rota “Cidade central → Passo” são as únicas disponíveis. Salvar substitui o traçado dessa rota. Para criar vias independentes no Terrain nativo, use o [criador de estradas](#criar-estradas-medievais-no-terrain).

## Abrir a ferramenta

1. Abra o **Unity Hub**. Se o projeto ainda não aparecer, escolha **Projects > Add > Add project from disk** e selecione a pasta `client/` da sua cópia do projeto. Use o Unity **6000.6.5f1**.
2. Espere o Unity terminar de abrir e importar o projeto.
3. No menu superior do Unity, abra **Jogo do Grupo > Mundo > Autoria de estradas**. Surge a janela **Estradas do mundo**.
4. Na janela, confira **Elemento: Estrada** e **Rota: Cidade central → Passo**. Clique em **Enquadrar estrada** para localizar a prévia na aba **Scene** (vista 3D).

## Desenhar a estrada manualmente

1. Na janela **Estradas do mundo**, ative **Adicionar ponto por clique**.
2. Na aba **Scene**, clique com o botão esquerdo sobre o chão da prévia no lugar por onde a estrada deve passar. Cada clique acrescenta um ponto entre os trechos existentes. A prévia é atualizada na hora.
3. Desative **Adicionar ponto por clique** para ajustar os pontos. Os marcadores **amarelos** são os pontos intermediários: arraste-os na vista 3D ou altere seus valores na lista **Pontos do traçado**. Use o botão **×** ao lado de um ponto intermediário para removê-lo.
4. Os marcadores **azuis** são as extremidades da rota e ficam fixos nas cidades. Use **Enquadrar estrada** novamente se perder o caminho de vista.

Os controles abaixo mudam a prévia assim que você os move:

| Controle | O que muda | Valor inicial |
|---|---|---:|
| **Largura da rua (m)** | A largura da parte principal da estrada. | 7 m |
| **Acomodação lateral (m)** | A faixa onde o terreno é ajustado gradualmente até encontrar a estrada. | 8 m |
| **Suavização do relevo (m)** | Quanto o perfil da estrada é suavizado ao longo do caminho, reduzindo subidas e descidas bruscas. | 18 m |

Observe a estrada de perto e de longe antes de salvar. Ela deve continuar legível, sem cortes abruptos nas laterais e sem árvores no corredor de passagem.

## Salvar, testar e voltar atrás

1. Quando gostar do resultado, clique em **Salvar traçado compartilhado**.
2. Aguarde a recompilação automática do Unity. O traçado salvo passa a ser a estrada oficial usada pelo jogo. **Não é necessário salvar a cena**: os objetos da prévia são temporários.
3. Entre no mundo pelo fluxo normal do projeto e percorra a ligação entre a cidade central e o Passo para conferir o resultado no terreno completo.

**Reverter** descarta as alterações ainda não salvas e recarrega o traçado salvo. O botão não é um histórico de desfazer: depois de salvar outra versão, ele não recupera automaticamente versões anteriores. Para recuperar uma versão já substituída, use o histórico do projeto ou redesenhe e salve novamente.

Se um clique não adicionar ponto, confira se **Adicionar ponto por clique** está ativo e se clicou no chão da prévia na aba **Scene**. Arrastar pontos só funciona com esse botão desativado.

## Próximas operações

| Operação | Situação |
|---|---|
| Pintar e aprovar uma receita Mata jogável | Concluído. A primeira mata está salva no mapa como base. |
| Converter o relevo atual para Terrain nativo | Em andamento. O heightmap inicial já pode ser gerado e pré-visualizado no Unity; a edição manual ainda não substitui a altura compartilhada. |
| Criar estradas independentes no Terrain | Splines nativas, terra/lajotas, rebaixo e limpeza da vegetação; procedimento acima. |
| Outras receitas de bioma, lagos e rios | Esperam novas operações de autoria. |

Ao concluir cada tarefa ou funcionalidade do criador de mapas, atualizar esta página com o caminho da tela, os controles, como salvar, como conferir o resultado e as limitações atuais. Se a tarefa não mudar as ações na interface, registrar seu efeito e indicar que o procedimento continua igual.
