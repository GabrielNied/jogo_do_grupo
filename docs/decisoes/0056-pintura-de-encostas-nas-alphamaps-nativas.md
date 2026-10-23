# 0056. Pintura de encostas nas alphamaps nativas

**Data:** 2026-10-22
**Status:** aceita

## Contexto

Gabriel quer pintar pedra nas laterais dos morros por um botão e continuar retocando com o pincel da Unity. O Procedural Terrain Painter 1.0.5 instalado assume a distribuição completa das camadas e suas novas aplicações sobrescrevem retoques. Terrain Tools já oferece filtro Slope por pincel.

## Decisão

Criar uma ferramenta de editor de Mundo que aplica uma máscara suave por inclinação diretamente nas alphamaps do Terrain selecionado. Reutilizar TerrainLayer existentes, preservar os outros pesos proporcionalmente, registrar Undo e nunca repintar automaticamente. Usar a maior cobertura entre rocha existente e máscara para reaplicação idempotente. Processar em faixas para limitar memória temporária e permitir cancelamento. Não depender do pacote local nem adicioná-lo ao repositório nesta entrega.

## Consequências

O resultado continua editável pelos pincéis nativos. Áreas planas e materiais fora da máscara são preservados. A operação pode cobrir retoques nas encostas; deve anteceder seu acabamento. Reduzir a máscara não remove a pintura anterior. O custo é proporcional à resolução das alphamaps, com memória adicional do Undo. A projeção e o estiramento das texturas continuam sendo responsabilidade do material.
