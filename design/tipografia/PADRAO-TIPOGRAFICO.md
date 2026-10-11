# Tipografia de Lumora — proposta para aprovação

## Estado e referência

A logo horizontal v5 permanece a matriz aprovada. Nenhuma destas fontes substitui o lettering da logo. A prancha de interface anterior sugeria uma linguagem; não identificava uma família real. Esta especificação fornece arquivos reais, licenciados e comparação local.

## Combinação recomendada

**Cinzel 600** em títulos curtos de telas e seções. **Source Sans 3 400/600/700** em leitura, botões, chat, filtros, números e tooltips. A recomendação é de design; a aprovação das famílias ainda depende do usuário.

Alternativas na comparação: Alegreya 600 com Source Sans 3; Cinzel 600 com Alegreya Sans 400/700. Não misturar as três combinações na mesma interface.

| Uso | Fonte/peso recomendado | Tamanho inicial a 1080p | Entrelinha |
|---|---|---|---|
| Título de página do site | Cinzel 600 | 40–56 px, adaptável | 1,15 |
| Título de tela do jogo | Cinzel 600 | 32–36 px | 1,2 |
| Seção | Cinzel 600 | 24–28 px | 1,25 |
| Texto de leitura | Source Sans 3 400 | 20 px | 1,5 |
| Botão e aba | Source Sans 3 600 | 18–20 px | 1,25 |
| Chat e tooltip | Source Sans 3 400/600 | 18–20 px | 1,45 |
| Números e atributos | Source Sans 3 600 | 18–20 px | 1,35 |
| Informação secundária | Source Sans 3 400 | 16–18 px | 1,4 |

São pontos de partida, não garantia de leitura em qualquer tela. Escalar a interface conforme resolução, distância e preferência. Evitar reduzir texto principal a 12–14 px para encaixar mais conteúdo.

## Regras

- Cinzel apenas em títulos curtos, sem parágrafos ou chat em caixa alta.
- Textos e controles em caixa normal, exceto rótulos curtos deliberados.
- Não aplicar textura de pedra, contorno escuro ou relevo ao texto funcional.
- Espaçamento normal em corpo e números; tracking até 0,03em em títulos curtos.
- Usar números tabulares em tabelas e contadores quando disponíveis.
- Prever quebra de linha, palavras longas, acentos e aumento do tamanho de leitura.
- Não depender de ícones da fonte: controles e estados precisam de ícones próprios com fallback.
- Marfim sobre azul e azul sobre dourado/pergaminho são pares iniciais; validar cada estado concreto.

## Arquivos e licenças

Arquivos originais baixados de https://github.com/google/fonts/tree/main/ofl em 11/10/2026, sem modificação. Cada família acompanha seu arquivo OFL. Preservar avisos e licença na distribuição.

- fontes/Cinzel[wght].ttf — títulos.
- fontes/SourceSans3[wght].ttf — leitura e controles.
- fontes/Alegreya[wght].ttf — alternativa para títulos.
- fontes/AlegreyaSans-Regular.ttf e AlegreyaSans-Bold.ttf — alternativa para leitura.

O HTML usa arquivos locais. Fontes variáveis funcionam na comparação web; para a engine, verificar suporte a variações e preparar instâncias estáticas dos pesos necessários caso exigido. Não presumir compatibilidade de importação só por carregar no navegador.

## Verificação e próximos passos

comparacao.html mostra as fontes reais, acentos, números, títulos, chat e descrição de item; permite alterar tamanho e combinação. Conferir a mensagem de carregamento ao abrir. A aprovação visual e o teste em tela do jogo ainda estão pendentes. Depois da escolha, fixar o par no kit, preparar os pesos para a engine e testar cortes/contraste em uma interface real.

## Revisão após avaliação do usuário — prevalece sobre recomendação anterior

O usuário gostou mais da opção 02 anterior, mas explicitou preferência por títulos SEM SERIFA, letras com mais preenchimento/peso e texto de 16 px como ponto de partida. A combinação Cinzel + Source Sans 3 deixa de ser a recomendação atual. A logo serifada aprovada continua intacta; esta preferência vale para títulos de interface e site.

Nova comparação: comparacao-sem-serifa.html.
- 01: Nunito Sans 800 nos títulos, Source Sans 3 500 na leitura — acolhedora e cheia.
- 02: Barlow Semi Condensed 700 nos títulos, Source Sans 3 500 na leitura — mais compacta.
- 03: Exo 2 700 nos títulos, Source Sans 3 500 na leitura — mais angular.

Títulos de painel 28 px, subtítulos 22 px, leitura inicial 16 px, controles 18 px. Tamanho ajustável para avaliação. A escolha ainda aguarda aprovação. Novos arquivos originais e licenças OFL foram adicionados à pasta fontes, a partir do repositório oficial Google Fonts. Não aplicar estas propostas como padrão final sem retorno do usuário.

## APROVADO — padrão atual

Em 11/10/2026 o usuário escolheu: "opção 1 tipo acolhedor". Esta aprovação substitui recomendações anteriores conflitantes.

- Títulos: Nunito Sans, peso 800, sem serifa, caráter acolhedor.
- Texto e leitura: Source Sans 3, peso 500, base 16 px.
- Botões e controles: Source Sans 3, peso 700, referência 18 px.
- Título de painel: 28 px; subtítulo: 22 px. Ajustáveis conforme contexto e escala de interface.
- Números: Source Sans 3 600, tabulares quando disponíveis.
- Logo: preservar imagem horizontal v5 aprovada; não trocar seu lettering pelas fontes da interface.
- Arquivo lumora-typography.css atualizado com o par aprovado e fontes locais.
- Página comparacao-sem-serifa.html conserva as três opções como histórico de comparação; apenas a opção 01 é o padrão atual.

As pranchas antigas não determinam tipografia: na implementação, prevalece este padrão. Validar importação de fontes na engine, contraste, escala e quebras de texto nas telas reais.
