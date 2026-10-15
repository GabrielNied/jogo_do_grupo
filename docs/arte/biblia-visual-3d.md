# Bíblia visual 3D

Regras de direção de arte para o que aparece **no jogo** em 3D: personagens, criaturas, equipamento e mundo. Tom da marca ("fantasia medieval alegre", WoW como linguagem) em [DIRETRIZES.md](../../design/historico/DIRETRIZES.md); números (polígonos, textura, escala, esqueleto) em [padrao-de-modelos.md](padrao-de-modelos.md); interface em [Lumora](../../design/interface/GUIA-DE-ESTILO.md). As imagens que ilustram cada regra estão na [biblioteca de referências](referencias-visuais/indice.md); os IDs entre parênteses são do [manifesto](referencias-visuais/manifesto-referencias.json).

**Princípio:** forma simples e exagerada na malha, todo o detalhe na textura pintada. Inspiração em WoW (silhueta, biomas, equipamento), Torchlight II (formas e cores), Darksiders II (volume e proporção), Sea of Thieves (superfície pintada, luz), Overwatch (legibilidade) e Palia (ambiente coeso), traduzida para o nosso pipeline de low poly por script.

## Forma e silhueta

- O modelo precisa ser reconhecível **só pelo contorno preto**, na distância da câmera de 3ª pessoa. Teste: render com o modelo em uma cor só, contra fundo claro (`sea-of-thieves-contraluz`).
- Uma ou duas formas dominantes por modelo (telhado da casa, ombreira, chifre, cajado); o resto é secundário e menor (`wow-casa`, `wow-conjuntos-de-armadura`).
- Formas grandes, poucas e com ângulo claro; evite detalhe que só existe de perto em geometria. Chanfre as bordas duras, em vez de deixar quina reta (`palia-oficina`).

## Proporção

- **Personagem:** mãos, pés, ombros e cabeça um pouco maiores que o real; cintura e pulsos finos. O biotipo de cada raça muda a proporção, não o detalhe (`torchlight2-berserker` × `torchlight2-maga`). Números de cada raça em [padrao-de-modelos.md](padrao-de-modelos.md#personagens-das-raças).
- **Criatura pesada:** massa no tronco e nos braços, pernas curtas e grossas, cabeça baixa (`darksiders2-golem`). Criatura amigável: formas arredondadas, focinho curto, olhos grandes (`palia-animais`, `wow-dragao`).
- **Arma:** maior que o realista; a arma de duas mãos chega perto da altura do personagem e entra na silhueta (`darksiders2-foices`, `torchlight2-canhao`).

## Malha × textura

- Malha dá a forma; textura dá o material, o desgaste, a luz e o detalhe. Pelo, veio de madeira, rebite, costura, musgo: **pintados** (`palia-animais`, `wow-interior-de-madeira`).
- Use geometria extra só onde ela muda a silhueta (chifre, crista, ombreira). Detalhe que não muda o contorno vai para a textura.
- Orçamento de triângulos e textura: [padrao-de-modelos.md](padrao-de-modelos.md#estilo). Não ultrapasse para "caber detalhe".

## Materiais pintados

- Toda superfície tem **variação de tom** (claro em cima, escuro embaixo) e **oclusão escura nas dobras**; nada de cor chapada (`darksiders2-trono-de-pedra`).
- **Madeira:** tábuas com tom diferente por peça, veios largos, bordas gastas mais claras (`sea-of-thieves-conves`).
- **Pedra:** blocos grandes e arredondados, rachaduras pintadas, musgo na parte de cima (`torchlight2-templo`).
- **Metal:** brilho só nas bordas e pontas, pintado; o mapa de brilho (`_Brilho`) segura o resto. Dourado como acento, não como base.
- **Emissão** (mágico, olhos, cristais): pequena área, cor única por elemento; não ilumine o modelo inteiro (`sea-of-thieves-tripulacao`).

## Paleta e valores

- Cada modelo tem **uma cor dominante, uma secundária e um acento**. O acento fica no ponto que o jogador deve ver (rosto, núcleo, arma).
- Bichos do campo em marrons quentes e dessaturados; o mágico em azul-ardósia com ciano de destaque (paleta de [padrao-de-modelos.md](padrao-de-modelos.md#estilo); `torchlight2-masmorra-azul`).
- Separe por **valor** (claro/escuro) antes de separar por cor: personagem mais contrastado que o fundo, fundo em valores médios (`overwatch-cores-legiveis`).
- Saturação alta em pouca área; áreas grandes (chão, parede) mais calmas.

## Equipamento

- Ombreira, elmo e arma são as peças que mais mudam a leitura; capriche nelas. Peitoral e calça seguem a cor do conjunto (`wow-conjuntos-de-armadura`).
- Raridade e poder se leem por **forma e acento** (borda dourada, emissão pequena), não por excesso de peças.
- Arma com cor diferente do corpo, para ler a pose de ataque (`torchlight2-canhao`).

## Criaturas

- Ameaça = tamanho, massa e ponto de brilho; simpatia = curvas, olhos grandes, cores quentes.
- Chefe tem um **ponto focal** luminoso (núcleo, olhos) que mostra para onde olhar e, se a regra do chefe pedir, o ponto fraco (`darksiders2-constructo`).
- Comparar sempre com as folhas próprias em [renders/](renders/) para manter a família.

## Arquitetura

- Telhados grandes, levemente tortos e com beiral fundo; paredes curtas. Madeira e pedra misturadas, janelas com luz quente (`wow-casa`, `palia-casas`).
- A forma do chão e das escadas mostra por onde andar; superfícies jogáveis mais limpas que as decorativas (`overwatch-rua`).
- Kit modular: peças grandes que se repetem, com variação na textura; detalhe pequeno (barril, lanterna, cerca) dá a escala.

## Vegetação

- Copa em **massas grandes** com tom variando de dentro para fora, não folha a folha; tronco retorcido e exagerado (`wow-arvore`).
- Uma cor de folha por bioma, com um acento (`torchlight2-arvores`).
- Planta de coleta: forma simples e fruto de cor saturada, para ser achada no meio do verde (`palia-horta`).

## Ambientes, luz e cor

- Cada bioma tem uma paleta e uma luz próprias (`torchlight2-campo`, `torchlight2-neve`, `darksiders2-gelo`). Céu e luz de contorno separam os planos; névoa clareia o fundo.
- Contraste quente × frio: luz quente nos pontos de interesse (fogo, janela, vela) contra ambiente frio, ou o inverso (`sea-of-thieves-por-do-sol`).
- Caminho e marcos (estátua, árvore grande, torre) guiam o jogador sem HUD (`wow-vilarejo`, `torchlight2-campo`).
- Estado atual da luz do jogo: `projeto-iluminacao-depois`.

## Legibilidade em 3ª pessoa

- Confira o modelo na distância e no ângulo da câmera do jogo, não só de perto: silhueta, cor dominante e acento precisam aparecer.
- Em grupo, cada jogador e cada efeito de habilidade precisa de uma cor própria; o ambiente não compete com eles (`overwatch-cores-legiveis`).
- Inimigo se distingue do cenário por valor e saturação.

## Originalidade

- Referência serve para tirar **princípio** (proporção, contraste, leitura), nunca forma reconhecível: não reproduza personagem, logo, arma, armadura ou prédio de outro jogo.
- Nenhuma imagem da biblioteca entra no jogo, nem como textura ou base de pintura. Asset de terceiros só CC0/CC-BY, pelo fluxo de [agentes-artistas.md](agentes-artistas.md#buscar-assets).

## Como usar a biblioteca

Passo a passo em [instrucoes-agentes.md](referencias-visuais/instrucoes-agentes.md). As imagens não estão no git: `npm run arte:baixar-referencias` ([ADR 0038](../decisoes/0038-referencias-visuais-fora-do-git.md)). Categoria → pasta → IDs:

| Para | Categoria (pasta) | IDs |
|---|---|---|
| Personagem jogável, raça | [personagens/](referencias-visuais/personagens/) | `wow-haranir`, `torchlight2-berserker`, `torchlight2-maga`, `sea-of-thieves-tripulacao` |
| Monstro, chefe, animal | [criaturas/](referencias-visuais/criaturas/) | `wow-dragao`, `darksiders2-golem`, `darksiders2-constructo`, `palia-animais` |
| Arma, armadura | [armaduras-armas/](referencias-visuais/armaduras-armas/) | `wow-conjuntos-de-armadura`, `darksiders2-foices`, `torchlight2-canhao` |
| Bioma, área | [ambientes/](referencias-visuais/ambientes/) | `torchlight2-campo`, `torchlight2-neve`, `darksiders2-gelo`, `wow-vilarejo` |
| Casa, ruína, kit modular | [arquitetura/](referencias-visuais/arquitetura/) | `wow-casa`, `palia-casas`, `torchlight2-templo`, `overwatch-rua` |
| Textura, superfície | [materiais/](referencias-visuais/materiais/) | `wow-interior-de-madeira`, `sea-of-thieves-conves`, `darksiders2-trono-de-pedra`, `palia-oficina` |
| Luz, paleta | [luz-cor/](referencias-visuais/luz-cor/) | `sea-of-thieves-por-do-sol`, `sea-of-thieves-contraluz`, `torchlight2-masmorra-azul`, `overwatch-cores-legiveis` |
| Árvore, planta | [vegetacao/](referencias-visuais/vegetacao/) | `wow-arvore`, `palia-horta`, `torchlight2-arvores` |
| O que o jogo já tem | [renders/](renders/) (`assets-do-projeto`) | `projeto-javali-do-campo-vistas`, `projeto-presa-velha-vistas`, `projeto-guardiao-do-reflexo-vistas`, `projeto-iluminacao-antes`, `projeto-iluminacao-depois` |
