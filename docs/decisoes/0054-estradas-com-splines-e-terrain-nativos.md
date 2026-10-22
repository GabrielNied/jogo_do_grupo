# 0054. Estradas autorais com Splines e Terrain nativos

**Data:** 2026-10-21
**Status:** aceita

## Contexto

O mapa medieval precisa de caminhos de terra compactada, levemente afundados pelo uso de pessoas e cavalos, e de trechos com lajotas. A estrada do Passo existente apenas sobrepõe uma superfície visual ao mapa procedural. A autoria manual precisa permitir novos traçados e remover vegetação pintada sem exigir limpeza por árvore ou tufo.

## Decisão

- Usar o pacote oficial `com.unity.splines` e o `Terrain` do Unity, com ferramentas de autoria na funcionalidade Mundo. Não acrescentar infraestrutura ao Core.
- Guardar o traçado como spline editável e gerar o relevo e as superfícies no editor. A terra tem perfil côncavo suave, bordas orgânicas e detalhes de cascos discretos; não há sulcos de rodas. Terra e lajotas compartilham o mesmo fluxo.
- Limpar `treeInstances` e camadas de detalhes nativas na faixa configurada. Objetos de vegetação colocados manualmente não fazem parte dessa limpeza.
- Aplicar por comando explícito, mantendo dados persistentes de recuperação das amostras afetadas. Reaplicação não acumula rebaixo; restauração preserva as áreas externas. Conflitos de autoria são informados antes de substituir dados.
- Na primeira versão, recusar corredores que sobreponham a área modificada por outra estrada. Cruzamentos automáticos precisam de uma operação própria que componha as modificações e sua restauração.
- Salvar malhas, materiais e texturas gerados como assets da funcionalidade. Os objetos ficam sob a raiz publicada do mapa e entram no prefab pelo fluxo existente.
- Continuar publicando alturas pelo exportador atual para o shared; criar uma estrada não publica nem reinicia o servidor automaticamente.

## Consequências

A pessoa edita curvas com as ferramentas oficiais e reaproveita a pintura nativa do Terrain. O jogo recebe geometria pronta, sem geração de estradas ou limpeza de vegetação por quadro e sem uma dependência paga.

A resolução do heightmap limita o relevo físico. Os detalhes menores usam material e geometria visual, sem um colisor separado. A grade de alturas publicada continua com intervalos de 10 m: o microperfil de uma estrada estreita pode não aparecer nas amostras do servidor. Por isso o rebaixo permanece raso e a ferramenta não promete alinhamento centimétrico entre o acabamento e a grade publicada. Este criador não acrescenta navegação automática, simulação de erosão em tempo real, exportação de obstáculos de árvores nem composição automática de cruzamentos.
