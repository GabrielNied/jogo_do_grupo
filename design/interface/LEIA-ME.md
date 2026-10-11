# Lumora — kit visual de exploração v1

11 de outubro de 2026. Identidade expandida a partir da paleta, logo e painel social aprovados. Este catálogo separa referências aprovadas, novas propostas e elementos raster de apoio.

## O que está aprovado

Nome de trabalho Lumora e assinatura A New Horizon; paleta azul profundo, turquesa, dourado, marfim e verde; emblema de duas cascas de arenito mel fosco envolvendo essência; logo horizontal v5 com O rachado e essência inclinada atrás da letra. A linguagem inicial de folhas nas bordas e calçada na base recebeu aprovação.

As novas telas e simplificações deste kit ainda são propostas. Não substituem as matrizes aprovadas. Nos layouts finais, inserir o PNG aprovado diretamente; a reprodução da logo nas pranchas pode variar.

## Catálogo das seis pranchas

| Prancha | Conteúdo | Uso |
|---|---|---|
| [01 — Componentes](pranchas/01-componentes.png) | Botões, abas, controles, busca, dropdown, paginação, rolagem, confirmação, carregamento, tooltip, notificações e estado vazio | Referência da biblioteca de interface |
| [02 — Aventura e equipamentos](pranchas/02-hud-inventario.png) | HUD, grupo, minimapa, habilidades, loot, inventário, equipamento e comparação | Direção de telas de aventura |
| [03 — Grupo e comunidade](pranchas/03-grupo-social.png) | Expedição, amigos, chat, perfil, guilda, convite e reanimação | Direção social com folhas e calçada |
| [04 — Cidade, criação e trocas](pranchas/04-cidade-economia.png) | Mercado, receitas, fusão, banco, decoração e aplicações no mundo | Visão visual de sistemas atuais e futuros |
| [05 — Mini marcas](pranchas/05-mini-marcas.png) | Oito estudos: detalhe, miniatura, duas cores, monocromia, compacto e relevo | Selecionar simplificações antes de finalizar |
| [06 — Materiais](pranchas/06-materiais.png) | Painel, arenito, tecido, pergaminho, calçada e folhagem | Atlas de referências; não é textura pronta para repetição |

## Elementos separados

- [Logo horizontal aprovada](elementos/logo-horizontal-aprovada.png): matriz raster preservada, fundo transparente.
- [Referência do símbolo aprovado](elementos/referencia-simbolo-aprovado.png): emblema detalhado na composição vertical; cor do texto dessa referência não é a matriz atual.
- [Fundo azul](elementos/fundo-petrol.png): superfície de apoio para menus e painéis, centro calmo para texto.
- [Pergaminho](elementos/pergaminho.png): superfície clara para leitura, documentos e confirmação.
- [Moldura de folhas e calçada](elementos/moldura-folhas-calcada.png): borda separada com centro transparente, base para composição de painéis.
- [Silhueta v1](exploracoes/silhueta-v1.png) e [v2](exploracoes/silhueta-v2.png): estudos com resíduos nas bordas e espaços vazios. Não usar como matriz de produção; requerem limpeza ou redesenho vetorial.

Fundos são imagens inteiras, sem validação de repetição contínua. Não foram produzidos mapas de normal/roughness nem texturas PBR. A moldura ainda não está dividida em cantos e faixas redimensionáveis; evitar esticar as folhas e pedras.

## Hierarquia da marca

| Aplicação | Versão proposta |
|---|---|
| Menu inicial, cabeçalhos e divulgação horizontal | Logo horizontal aprovada, sem símbolo grande ao lado |
| Tela de entrada e peça de apresentação ampla | Emblema detalhado e nome, com bastante respiro |
| Estandartes e tecidos | Duas cores ou uma cor, sem efeitos de luz pintados excessivos |
| Relevo, selo e gravação em pedra | Silhueta sólida com abertura central negativa |
| Ícones de 32–64 px | Estudo compacto 07 da prancha, sem fragmentos pequenos; validar por tamanho |
| Ícones de 16–24 px | Precisam de matriz específica ainda mais simples, não apenas reduzir o emblema detalhado |

Estudos 03, 04 e 07 são candidatos recomendados, não aprovados pelo usuário. O estudo 06 se afasta da forma original e não deve virar referência principal sem decisão explícita.

## Paleta e papéis

| Papel | Valor nominal | Direção |
|---|---|---|
| Fundo de marca | #173B50 | Azul profundo |
| Essência e foco | #39B9AE | Turquesa |
| Ação principal e detalhes | #F5BE63 | Dourado quente |
| Texto sobre azul | #F4E6CB | Marfim |
| Natureza e apoio | #688A48 | Verde |

Dourado das letras usa a referência v5, com variação de luz/sombra; não trocar o material por uma cor chapada ou clarear automaticamente. Vermelho, verde e âmbar de estado são extensões funcionais propostas, não novas cores principais. Sempre acompanhar estados e raridades com texto, símbolo ou forma.

## Tipografia

- Logo: desenho próprio representado pela matriz aprovada; não substituir por uma fonte semelhante.
- Títulos de telas: serifas legíveis e expressivas, sem aplicar o relevo da logo a todo título.
- Textos, chat, números e filtros: sem serifa de alta legibilidade.
- Famílias de apoio ainda não escolhidas; avaliar acentos em português, números, tamanhos pequenos e licenças antes de incorporar.
- Escala inicial para testar em 1080p: texto 18–20 px, título de seção 24–28 px, título de tela 32–40 px. Escalar com configurações do jogador, não fixar tudo em pixels físicos.

## Interface e ornamentação

### Três intensidades

1. Social e cidade: folhas nos cantos externos, base de calçada e detalhes de madeira/tecido.
2. Gestão de itens: painel azul, moldura dourada fina, decoração reduzida e área de leitura tranquila.
3. Combate: elementos compactos, fundo discreto e mínimo ornamento; a ação deve continuar visível.

O rodapé de pedra e folhas no HUD ilustrado é uma exploração mais decorativa. Na implementação, preferir opcional ou reduzido, sem cobrir combate ou loot.

### Estados e comportamento visual

- Primário: dourado com texto escuro.
- Secundário: azul com texto marfim.
- Foco: contorno visível turquesa sem mudar o tamanho do componente.
- Pressionado: leve escurecimento e recuo visual, sem saltos de layout.
- Desabilitado: reduzir destaque, manter rótulo legível e explicar a condição quando necessário.
- Destrutivo: aviso e ícone explícitos; reservar para ações com perda, não para todo botão de sair.
- Confirmações de fusão: informar claramente consumo de doadores antes da ação.
- Notificações: texto curto, ícone, contraste suficiente e fechamento claro.
- Tooltips: evitar textura forte por trás dos números e explicar diferenças com sinal e texto além da cor.

### Movimento proposto

Entrada e troca de painéis com transições curtas e discretas. Brilho da essência lento e suave nas telas de marca; evitar pulsação contínua competindo com informação do HUD. Prever redução de animações e flashes. São diretrizes propostas, não animações já produzidas.

## Dentro do mundo

- Estandartes: tecido azul, emblema simplificado dourado/turquesa.
- Placas: reservar a marca de Lumora a locais institucionais; lojas de jogadores podem ter símbolos próprios no mesmo vocabulário de materiais.
- Memoriais: relevo de pedra com texto legível e espaço para nomes, sem depender de brilho.
- Marcos mágicos: pedra fosca e essência localizada, mantendo coerência com o emblema.
- Cidades e territórios: identidade da marca serve de base; permitir personalização local sem repetir o logo em todas as construções.

Construção e territórios representam visão de longo prazo, não confirmação de entrega no MVP.

## Correções de conteúdo antes de implementar

As imagens são conceitos e podem inventar conteúdo. Não incorporar automaticamente:

- Classe fixa “Guerreiro” no perfil: jogo é sem classes. Trocar por equipamento, estilo ou papel atual conforme regras reais.
- Seis vagas visuais na expedição: validar composição por conteúdo; dungeon documentada tem cinco jogadores.
- Facções, reputações, raças, retratos, nomes, datas, valores, receitas e poderes ilustrados: exemplos, não decisões.
- Ícones por cor apenas: adicionar texto/forma nos filtros de raridade.
- Botão de fusão e resultado ilustrado: conectar à regra real de compatibilidade e consumo, sem adotar a receita inventada.

## Critérios para finalizar o kit de produção

1. Aprovar novas propostas de interface e selecionar mini marcas.
2. Fixar fontes de apoio e validar licença, acentos e números.
3. Limpar/vetorizar silhuetas, preparar tamanhos e verificar 16/24/32/64 px em fundos claros e escuros.
4. Preparar botões, bordas, cantos, faixas e ícones individualmente; definir regiões redimensionáveis da moldura.
5. Conferir contraste, leitura em diferentes resoluções, escala de UI e navegação por teclado.
6. Inserir logo aprovada diretamente nas telas e preservar versão da matriz.
7. Testar HUD com combate em massa e reduzir ornamentação que atrapalhe a visão.

Este kit entrega a exploração visual e elementos de apoio; não é uma biblioteca de interface já implementada nem um pacote vetorial final.

## Atualização aprovada de tipografia

Padrão escolhido pelo usuário: opção 01 acolhedora. Títulos Nunito Sans 800; leitura Source Sans 3 500 a partir de 16 px; controles Source Sans 3 700. Logo aprovada permanece imagem. Esta definição prevalece sobre títulos serifados das pranchas e sobre a indicação anterior de fontes ainda não escolhidas. Especificação atual: ../tipografia/PADRAO-TIPOGRAFICO.md; CSS em ../tipografia/lumora-typography.css a partir da pasta interface.
