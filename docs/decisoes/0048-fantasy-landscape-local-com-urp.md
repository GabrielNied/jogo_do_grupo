# 0048. Fantasy Landscape local com materiais URP

**Data:** 2026-10-18
**Status:** parcialmente substituída por [0053](0053-assets-usados-no-git.md) (distribuição e instalação); adaptação URP mantida

## Contexto

Gabriel importou Fantasy Landscape, de Pxltiger (Asset Store 103573), e autorizou sua adaptação para uso no jogo. O pacote declara suporte a Built-in, sem suporte a URP. As árvores próximas aparecem rosas por usarem Tree Creator antigo. O repositório é público e a licença é Standard Unity Asset Store EULA, não CC0.

## Decisão

- Exceção específica à seleção de fontes do [ADR 0014](0014-conteudo-3d-e-agentes-artistas.md): permitir a cópia licenciada local deste pacote, por autorização do Gabriel nesta tarefa.
- Manter URP. Adaptar os materiais pelo editor, preservando GUIDs e referências das árvores pintadas. Folhas e plantas usam recorte de alpha e duas faces; troncos e construções usam Lit; partículas usam Particles/Unlit.
- Adicionar LOD Group na raiz dos prefabs de árvore para usar o caminho de árvores com LOD do Terrain e permitir rotação aleatória. Inicialmente manter uma malha 3D até o corte por tamanho aparente; sem billboard Tree Creator incompatível. Isso pode custar mais geometria que um LOD simplificado, a produzir depois se necessário.
- Guardar o pacote local em `client/Assets/ThirdParty/Pxltiger/FantasyLandscape/`. Ignorar no Git tanto a pasta original de importação quanto o destino. Publicar somente o conversor próprio e instruções, nunca os arquivos do pacote.
- Cada integrante obtém sua cópia pela Asset Store e executa a adaptação. A autorização de uso não altera os [termos da licença](https://unity.com/legal/as-terms).

## Consequências

Árvores já pintadas recebem os materiais corrigidos sem repintar o terreno. Reimportar o pacote pode restaurar materiais Built-in; o conversor pode ser executado novamente. O vento e o tratamento específico de luz/oclusão do Tree Creator antigo não são reproduzidos: a vegetação adaptada fica estática. Compilar o projeto sem o pack continua possível; mapas que o referenciem exigem a instalação licenciada local. Distribuição final do jogo continua sujeita à EULA; esta decisão não autoriza redistribuir os arquivos-fonte do pack.
