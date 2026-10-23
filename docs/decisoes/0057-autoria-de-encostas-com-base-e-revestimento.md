# 0057. Autoria de encostas com base preservada e revestimento

**Data:** 2026-10-23
**Status:** aceita

## Contexto

A pintura do ADR 0056 só considerava inclinação e usava a própria saída como entrada. Reduzir parâmetros não retirava a rocha. Gabriel pediu altitude, transições naturais e definição de pedra nos paredões, dentro da ferramenta existente e sem mapas de demonstração ou novos comandos no menu Mundo.

## Decisão

- Continuar nas alphamaps nativas e calcular a maior máscara suave entre inclinação e altitude mundial. Variações de limiar consultam ruído mundial por semente, sem reiniciar em cada bloco. Região circular opcional delimita pintura e revestimento.
- Preservar, por GUID de TerrainData, somente os pesos originais compactados em Deflate e referências de camadas num asset de editor. Não duplicar relevo, vegetação ou TerrainData completo. Hash de pesos quantizados e ordem das camadas bloqueia sobrescrita silenciosa de retoques. Incorporar a pintura atual requer comando explícito na janela, com Undo.
- Recalcular a partir da base somente dentro do escopo atual. Fora de uma região circular, manter o resultado anterior. Prévia é uma sobreposição descartável da Scene, sem alterar assets ou estado da cena. Aplicação e cancelamento são atômicos no lote, com Undo/Redo de pesos e metadados.
- Oferecer material URP opcional que reutiliza passes nativos e substitui a amostragem da camada escolhida por triplanar, com normal/máscara e transição ao basemap. Criar materiais novos por terreno, sem editar os existentes ou reordenar camadas. Nesta versão, triplanar só nas primeiras quatro; pintura por altitude não tem essa restrição.
- Revestir encostas opcionalmente com modelos atribuídos na receita, grade mundial determinística, subdivisão por inclinação, limite e espaçamento em 3D. Registrar âncoras das peças para substituir/remover somente gerados na região. Instâncias são decoração sem física ou scripts de gameplay, pois não há exportação de obstáculos ao servidor nesta entrega.

## Consequências

Receitas podem ser experimentadas e reduzidas sem acumular pintura. O custo permanente é o backup compactado; Undo acrescenta memória temporária proporcional às alphamaps do lote. O backup deve acompanhar a autoria no controle de versão. Assumir uma nova base incorpora também a rocha procedural já presente. Diminuir ou mover o círculo não apaga áreas que ficaram fora: para removê-las, restaurar explicitamente aquela região ou o terreno inteiro.

Os grandes contornos vêm de malhas reais, sem gerar relevos ou plataformas novas para o servidor. O efeito depende dos modelos escolhidos; não é erosão nem reconstrução automática de falésias. A projeção opcional tem limite explícito de quatro camadas e retorna gradualmente à aparência nativa distante. Trocar parâmetros estruturais das camadas exige reaplicar o material. Não há pacote novo, geração em execução, alteração de mapa real pelo código ou cena de demonstração persistente.
