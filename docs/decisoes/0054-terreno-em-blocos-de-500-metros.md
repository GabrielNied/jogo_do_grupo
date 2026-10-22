# 0054. Autoria do terreno em blocos de 500 metros

**Data:** 2026-10-21
**Status:** aceita

## Contexto

Um Terrain de 3 km torna a pintura de caminhos e vegetação grosseira, mesmo aumentando apenas o heightmap. Aumentar todas as resoluções do terreno único concentra dados e limita a precisão. Gabriel aprovou dividir a autoria em blocos, mantendo o mundo inteiro editável no Unity.

## Decisão

- Manter 3 × 3 km, em 6 × 6 Terrains de 500 m. Cada bloco usa heightmap 1025, pintura/base 1024 e detalhes 1024, com patches de 16. A precisão horizontal é aproximadamente 49 cm.
- Compartilhar TerrainLayers entre os blocos e conectar vizinhos em todas as direções, com Auto Connect e o mesmo Grouping ID. O pincel próprio segue os vizinhos e desfaz o gesto em conjunto.
- Migrar relevo pela interpolação nativa dos triângulos do Unity (`GetInterpolatedHeight`), pintura por interpolação bilinear e buracos por vizinho mais próximo. Bilinear nas alturas altera encostas com diagonais acentuadas; comparar 90.601 pontos, com tolerância de 5 cm pela quantização, antes de trocar a cena. Preservar a faixa vertical da autoria. Ajustar o tamanho de repetição das camadas para um número inteiro de repetições por bloco, evitando diferença de fase nas bordas.
- Não migrar instâncias de árvores nem detalhes, por pedido do Gabriel. Preservar protótipos para a nova pintura manual. Objetos manuais independentes do Terrain permanecem.
- Publicar o conjunto para o prefab; amostrar o bloco correspondente ao exportar a grade de 10 m do servidor, mantendo o ADR 0044. Não publicar uma nova grade autoritativa só por dividir o terreno.
- Guardar os dados originais como legado durante o teste local. A migração recusa reaplicação sobre blocos já criados.

## Consequências

Pintura e modelagem ganham precisão e atravessam as divisas. Os 36 blocos ficam carregados; esta mudança não implementa streaming. Crescem memória e volume dos assets: resolução maior permite novas edições finas, mas não recupera detalhes ausentes na origem. O teste local do Gabriel precede a abertura do PR.
