# Guia de uso do criador de mapas

Este guia ensina a usar as ferramentas de mapa dentro do **editor Unity**. Ele é permanente: cada nova operação deve ganhar aqui um passo a passo antes de ser considerada entregue. As regras do mundo ficam em [Mundo](features/mundo.md); a situação provisória do desenvolvimento fica na [retomada](../client/Assets/_Game/Features/Mundo/MapaEmDesenvolvimento/RETOMADA.md).

## Direção da ferramenta

O objetivo é **desenhar o mapa**, sem montar cada trecho manualmente com código. Você escolhe uma operação, desenha sua forma no mapa, ajusta parâmetros, vê o resultado em tempo real e salva para continuar editando depois. A ordem confirmada de desenvolvimento e de uso é:

1. **Biomas:** desenhar as regiões; cada uma recebe relevo base, texturas e vegetação conforme suas regras, não apenas uma cor.
2. **Lagos:** desenhar a área da água; a ferramenta forma a bacia e as margens junto com a superfície da água.
3. **Estradas:** desenhar linhas em qualquer ponto do mapa; a ferramenta cria o piso, acomoda o terreno e deixa a passagem livre.

A estrada Cidade central → Passo descrita abaixo é um **protótipo já disponível**, criado antes da definição dessa ordem. Com os biomas disponíveis, as próximas entregas são lagos e depois estradas livres.

## Desenhar biomas

No Unity, abra **Jogo do Grupo > Mundo > Autoria de biomas**. A janela **Biomas do mundo** monta uma prévia temporária do mapa 3D inteiro na aba **Scene**. A **Mata de exemplo** a leste da cidade inicial já está salva no mapa oficial. As mudanças que você fizer nela ou em outras regiões ficam apenas na prévia até clicar em salvar.

1. Escolha uma região na lista ou clique em **Nova região**. Dê um nome e escolha um dos sete biomas: campo, mata, colinas, deserto, neve, lava ou pântano.
2. Ative **Adicionar ponto por clique** e clique no chão da vista **Scene** em pelo menos três lugares. A ferramenta fecha o contorno entre o último ponto e o primeiro. Use **Enquadrar região** para aproximar a câmera.
3. Desative **Adicionar ponto por clique**. Arraste as alças amarelas na vista 3D, altere as coordenadas na janela ou use **×** para tirar um ponto. A prévia é refeita após parar de arrastar.
4. Ajuste **Mistura da borda** para suavizar a passagem entre biomas, **Intensidade do relevo** para aumentar ou reduzir a forma do chão e **Densidade da vegetação** para mudar a quantidade de árvores e peças naturais.
5. Se duas regiões se cruzarem, a que estiver mais à frente na lista redefine o centro da área; a borda mistura o resultado com a região anterior. Use **Trazer para frente** e **Enviar para trás** para mudar essa ordem.
6. Observe o relevo, o material do solo e a vegetação de perto e de longe. O editor mantém os pátios das cidades e suas transições protegidos, sem deslocar as cidades.
7. Clique em **Salvar regiões no mapa compartilhado** para tornar o desenho oficial. Aguarde a recompilação. Entre no mundo pelo fluxo normal e visite a região para conferir o resultado. A altura salva é a mesma que o cliente desenha e o servidor usa.

A prévia não é salva na cena. **Reverter** descarta o rascunho atual e recarrega as regiões salvas; se ainda não houver nenhuma, mostra a mata de exemplo. Para recuperar uma versão antiga depois de salvar, use o histórico do projeto. O mapa inteiro aparece na prévia, mas as malhas editadas são atualizadas por região; em máquinas mais lentas, a resposta pode levar alguns segundos. O desenho não altera rios ou cria lagos e estradas: essas são operações posteriores.

## Protótipo da estrada do Passo

A ferramenta permite **redesenhar a estrada Cidade central → Passo**. Você pode clicar pontos no terreno 3D, mover os pontos intermediários e ajustar a aparência da passagem enquanto vê uma prévia. A largura inicial é de **7 m**.

**Ainda não existe um botão para criar uma segunda estrada independente.** A opção “Estrada” e a rota “Cidade central → Passo” são as únicas disponíveis nesta versão. Salvar substitui o traçado dessa rota. Quando a criação de novas rotas estiver pronta, este guia ganhará uma seção própria.

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
| Desenhar biomas com relevo, texturas e vegetação próprios | Disponível no Unity; primeira mata salva no mapa. |
| Desenhar lagos com bacia, água e margens | Depois dos biomas. |
| Criar várias estradas independentes | Depois dos lagos. A janela atual só altera a rota do Passo. |
| Desenhar rios com leito e água corrente | Ideia separada dos lagos; ordem ainda não definida. |

Ao concluir cada tarefa ou funcionalidade do criador de mapas, atualizar esta página com o caminho da tela, os controles, como salvar, como conferir o resultado e as limitações atuais. Se a tarefa não mudar as ações na interface, registrar seu efeito e indicar que o procedimento continua igual.
