# 0055. Redes de estradas com backup composto

- **Data:** 2026-10-22
- **Status:** aceita

## Contexto

Os backups esparsos independentes da [ADR 0054](0054-estradas-com-splines-e-terrain-nativos.md) recusavam faixas sobrepostas. Aplicá-las sequencialmente somaria rebaixos e remover uma via poderia ressuscitar árvores dentro de outra. Até faixas separadas podem compartilhar uma célula grande de grama.

## Decisão

- Recompor as vias aplicadas de uma cena sobre uma base original esparsa compartilhada. Por célula de altura vence o maior rebaixo; detalhes e árvores usam a união dos corredores.
- Guardar o último traçado e configuração aplicados por via. Reaplicar uma delas não incorpora edições pendentes das vizinhas. Backups anteriores conservam seu perfil esparso exato até reaplicar a própria via, pois não registravam todos os parâmetros.
- Persistir o diário com a base, resultado anterior e resultado planejado antes de escrever no Terrain. Recuperação interrompida retorna a rede à base original. Conflitos manuais continuam bloqueando a escrita.
- Gerar uma única superfície por conjunto visual conectado, em células esparsas de 30 cm (até 600.000). Lajotas têm prioridade por peso de vértice; cascalhos têm dono único. Vias isoladas preservam a geometria e o material anteriores.

## Consequências

A ordem de aplicação não acumula depressões. Mover ou restaurar uma via recompõe também as faixas antigas, preservando vias restantes e vegetação externa. O backup composto ocupa um asset compartilhado, que deve acompanhar todas as vias ao versionar.

Recompor considera as vias da cena inteira e pode ser mais caro em mapas grandes; cenas de autoria menores limitam esse custo. Cruzamentos exigem o shader nativo e recusam materiais personalizados, enquanto vias isoladas continuam aceitando-os. A publicação e a grade autoritativa de 10 m permanecem como na ADR 0054.
