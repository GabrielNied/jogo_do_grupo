# Base de implementação — provisória

**Histórico:** substituída como orientação vigente pelo [guia Lumora UI v1](../interface/GUIA-DE-ESTILO.md), após o usuário adotar o modelo visual. O texto abaixo registra a fase inicial e não deve bloquear a criação de novas telas no padrão atual.

Este documento descreve o laboratório; não aprova novas telas. Decisões de marca e tipografia continuam nas [fontes oficiais de design](../README.md). Quando o usuário validar a renderização real, os componentes aceitos poderão virar o guia definitivo para IAs.

## Regras para continuar o laboratório

1. Trabalhar apenas em `Features/LaboratorioVisual` e nesta pasta. Não ligar o laboratório ao bootstrap, cenas, menus ou estilos existentes do jogo.
2. Construir elementos reais de UI Toolkit. Não usar uma imagem de prancha inteira como interface clicável. A prancha só aparece no painel de comparação.
3. Aplicar o USS somente na raiz `lumora-lab`; classes próprias começam com `lab-`. Não carregar estilos do Core ou de outra feature.
4. Usar as fontes preparadas e a matriz da logo; nunca escrever “Lumora” com uma fonte de interface para substituir a logo aprovada. Não aplicar negrito sintético às fontes já instanciadas.
5. Texto funcional começa em 16 px, botões em 18 px, títulos em 28/22 px. Alterações maiores devem passar pelas três áreas de teste e pelos estados interativos.
6. Componentes base nesta versão: `Texto`, `Botao`, `Painel` e `Barra` em `TelaLaboratorio`. Estilo em `Laboratorio.uss`. Reaproveitar antes de criar uma nova aparência.
7. Folhas e calçada em painéis sociais/cidade; combate com ornamentação reduzida. Não inventar símbolos religiosos, sol ou estrela-guia.
8. Novos recursos devem entrar em `design/` com origem, licença quando aplicável, finalidade, limitações e estado de aprovação. Incluir no importador e no inventário de validação.
9. Conteúdo fictício não define regras de jogo. Não chamar rede, salvar personagem ou alterar dados reais.
10. Cada alteração visual precisa de ficha e capturas reais antes da aprovação. Registrar separadamente aprovação do lettering, cores, tipografia, ornamentação e layout; não presumir aprovação de todos pela aprovação de um.

## Espaçamento e geometria propostos

O USS utiliza principalmente múltiplos de 4 px, alvo de botão mínimo de 44 px, painéis de 24 px de preenchimento e margens maiores para proteger texto sob folhas/calçada. Estes números são propostas de implementação. O layout permite quebra e rolagem para que mais texto não force redução da fonte. A janela compara a composição completa com a prancha; futura aprovação pode exigir dividir a moldura em elementos independentes.

## Critério para virar padrão

Para cada componente: nome, variante/estado, fonte e tamanho, cores por função, medidas, recurso de origem, comportamento de teclado, capturas nas áreas testadas, resultado e aprovação explícita. Só componentes aceitos entram no guia definitivo. Componentes pendentes permanecem identificados, sem migração automática para telas do jogo.
