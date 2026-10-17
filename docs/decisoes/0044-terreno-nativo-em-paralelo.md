# 0044. Terreno nativo em paralelo para a proxima versao do mapa

**Data:** 2026-10-16
**Status:** aceita

## Contexto

O mundo de 3 km usa malhas geradas por codigo a partir de `MapaDoMundo`, que tambem e a fonte de altura compartilhada com o servidor. A autoria atual de biomas funciona, mas a cobertura de grama por malhas instanciadas nao entrega o resultado visual desejado. O mapa sera remodelado; editar relevo, texturas e detalhes diretamente no Unity e mais adequado a esse trabalho.

## Decisao

- Criar uma versao paralela do mundo com `Terrain` nativo do Unity, sem apagar a montagem atual.
- Exportar o relevo atual como *heightmap* para servir de ponto de partida editavel, nao como conversao automatica definitiva.
- Usar os detalhes nativos do Terrain para a grama e conservar a autoria de Mata como camada de regras sobre o terreno.
- Antes de substituir o mundo atual, validar no prototipo: altura, personagem no chao e grama nativa.
- Definir um arquivo de heightmap assado como fonte unica que cliente e servidor possam consultar antes de aceitar edicao manual como mapa oficial.

## Consequencias

- O Unity passa a oferecer pincel de relevo, texturas e grama para a autoria visual do mapa.
- Estradas, agua, lava, caverna, texturas de bioma e consultas de altura precisarao de migracao gradual.
- O mapa atual permanece como referencia funcional enquanto o prototipo e validado.
- Nao se inicia outro bioma ate a decisao do prototipo; a proxima entrega e somente a conversao paralela minima.
