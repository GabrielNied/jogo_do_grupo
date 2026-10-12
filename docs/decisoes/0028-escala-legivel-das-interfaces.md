# 0028. Escala legível das interfaces Lumora

**Data:** 2026-10-11
**Status:** substituída por [0035](0035-fonte-sdf-e-escala-pela-resolucao.md)

## Contexto

O padrão aprovado pede leitura mínima e layout conforme a largura real disponível. Os documentos do jogo usavam `ScaleWithScreenSize`, referência 1920 × 1080 e correspondência de largura/altura. Em 1280 × 720, essa política reduz o texto de 16 para aproximadamente 10,7 pixels e mantém a área lógica em 1920 × 1080, impedindo a composição de reagir à redução da janela.

## Decisão

- Aplicar `ConstantPixelSize` nos documentos existentes, com escala base centralizada na configuração visual.
- Compartilhar a política por `EstiloCore.ConfigurarEscala`, incluindo os documentos separados de Itens, Social, Combate e Mobs.
- Manter tamanho mínimo dos textos; reduzir a área de conteúdo por empilhamento, quebra e rolagem conforme a geometria da raiz, como previsto no [guia visual](../../design/interface/GUIA-DE-ESTILO.md).

## Consequências

O tamanho dos textos deixa de diminuir automaticamente com a resolução. Em monitores de alta densidade, pode ser necessário aumentar a leitura; a preferência persistente de acessibilidade continua uma evolução separada. Todos os documentos precisam adotar a mesma política para evitar desalinhamento do HUD. Validar no Unity a composição das resoluções previstas, rolagem e cliques. Esta proposta complementa a configuração compartilhada do ADR 0027.
