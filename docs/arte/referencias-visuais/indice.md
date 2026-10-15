# Biblioteca de referências visuais 3D

Imagens de jogos comerciais para a direção de arte **3D in-game**: 30 screenshots oficiais (WoW, Torchlight II, Darksiders II, Sea of Thieves, Overwatch, Palia) e as folhas do próprio projeto. As regras que elas ilustram estão na [bíblia visual](../biblia-visual-3d.md); o passo a passo de uso, em [instrucoes-agentes.md](instrucoes-agentes.md). Dados de cada imagem (princípios, dimensões, origem): [manifesto-referencias.json](manifesto-referencias.json). Licença: [licencas-referencias.md](licencas-referencias.md).

**Só referência interna.** Nada daqui entra no jogo, em `client/` ou em `client/Assets/ThirdParty/`. Copiar forma reconhecível de outro jogo também não: tire o princípio, não o desenho.

**As imagens não estão no git** (o repositório é público, [ADR 0038](../../decisoes/0038-referencias-visuais-fora-do-git.md)). Num checkout novo, baixe-as das URLs oficiais com `npm run arte:baixar-referencias`; os links abaixo abrem depois disso. Conferir o manifesto com o disco: `npm run arte:referencias`.

## personagens

Silhueta, biotipo e adornos de personagem jogável. Pasta: [personagens/](personagens/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-haranir](personagens/wow-haranir.jpg) | World of Warcraft | Biotipo e adornos de raça; comparar com os personagens das raças (Silvano, Cinéreo). |
| [torchlight2-berserker](personagens/torchlight2-berserker.jpg) | Torchlight II | Proporção atarracada e pose de herói; referência para Graníticos; o pet ao lado mostra a mesma estilização em animal. |
| [torchlight2-maga](personagens/torchlight2-maga.jpg) | Torchlight II | Contraste de biotipo com o berserker; referência para Silvanos e classes de magia. |
| [sea-of-thieves-tripulacao](personagens/sea-of-thieves-tripulacao.jpg) | Sea of Thieves | Como destacar um personagem num grupo e usar emissão com moderação. |

## criaturas

Monstros, chefes e animais. Pasta: [criaturas/](criaturas/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-dragao](criaturas/wow-dragao.jpg) | World of Warcraft | Criatura grande e legível; referência para chefes e montarias. |
| [darksiders2-golem](criaturas/darksiders2-golem.jpg) | Darksiders II | Peso e proporção de criatura pesada; ler junto com o Guardião do Reflexo. |
| [darksiders2-constructo](criaturas/darksiders2-constructo.jpg) | Darksiders II | Chefe mecânico; ponto de brilho que guia o olhar do jogador. |
| [palia-animais](criaturas/palia-animais.jpg) | Palia | Bichos do campo amigáveis; referência direta para javali, lobo e tatu. |

## armaduras-armas

Equipamento como parte da silhueta. Pasta: [armaduras-armas/](armaduras-armas/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-conjuntos-de-armadura](armaduras-armas/wow-conjuntos-de-armadura.jpg) | World of Warcraft | Conjuntos de equipamento; ombros e elmo são as peças que mais mudam a leitura. |
| [darksiders2-foices](armaduras-armas/darksiders2-foices.jpg) | Darksiders II | Arma como parte da silhueta do personagem. |
| [torchlight2-canhao](armaduras-armas/torchlight2-canhao.jpg) | Torchlight II | Tamanho exagerado de arma lido a distância; encaixe no ombro. |

## ambientes

Biomas, chão, composição de área. Pasta: [ambientes/](ambientes/).

| ID | Jogo | Para que serve |
|---|---|---|
| [torchlight2-campo](ambientes/torchlight2-campo.jpg) | Torchlight II | Campo aberto visto de cima; variação do chão sem polígonos extras. |
| [torchlight2-neve](ambientes/torchlight2-neve.jpg) | Torchlight II | Bioma frio; cor quente para guiar o caminho. |
| [darksiders2-gelo](ambientes/darksiders2-gelo.jpg) | Darksiders II | Profundidade e escala de paisagem. |
| [wow-vilarejo](ambientes/wow-vilarejo.jpg) | World of Warcraft | Composição de vila; referência para o hub. |

## arquitetura

Casas, ruínas e espaço jogável. Pasta: [arquitetura/](arquitetura/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-casa](arquitetura/wow-casa.jpg) | World of Warcraft | Casa medieval estilizada; proporção entre telhado e parede. |
| [palia-casas](arquitetura/palia-casas.jpg) | Palia | Casas de vila aconchegante vistas de cima. |
| [torchlight2-templo](arquitetura/torchlight2-templo.jpg) | Torchlight II | Ruína ou templo de masmorra; forma de pedra estilizada. |
| [overwatch-rua](arquitetura/overwatch-rua.jpg) | Overwatch | Legibilidade do espaço jogável; superfície limpa. |

## materiais

Madeira, pedra, metal e água pintados. Pasta: [materiais/](materiais/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-interior-de-madeira](materiais/wow-interior-de-madeira.jpg) | World of Warcraft | Madeira e tecido pintados; referência para interiores e baús. |
| [sea-of-thieves-conves](materiais/sea-of-thieves-conves.jpg) | Sea of Thieves | Madeira pintada ao ar livre e superfícies molhadas. |
| [darksiders2-trono-de-pedra](materiais/darksiders2-trono-de-pedra.jpg) | Darksiders II | Pedra entalhada; oclusão de ambiente nas dobras, como no nosso padrão. |
| [palia-oficina](materiais/palia-oficina.jpg) | Palia | Objetos de profissão; madeira e ferro em tom aconchegante. |

## luz-cor

Paleta, valor, contraluz e legibilidade pela cor. Pasta: [luz-cor/](luz-cor/).

| ID | Jogo | Para que serve |
|---|---|---|
| [sea-of-thieves-por-do-sol](luz-cor/sea-of-thieves-por-do-sol.jpg) | Sea of Thieves | Céu e hora do dia; contraste quente e frio. |
| [sea-of-thieves-contraluz](luz-cor/sea-of-thieves-contraluz.jpg) | Sea of Thieves | Teste de silhueta: o personagem precisa ser reconhecível em contraluz. |
| [torchlight2-masmorra-azul](luz-cor/torchlight2-masmorra-azul.jpg) | Torchlight II | Masmorra mágica; casa com a paleta azul-ardósia e ciano do projeto. |
| [overwatch-cores-legiveis](luz-cor/overwatch-cores-legiveis.jpg) | Overwatch | Legibilidade de combate em grupo pela cor. |

## vegetacao

Árvores, arbustos e plantas de coleta. Pasta: [vegetacao/](vegetacao/).

| ID | Jogo | Para que serve |
|---|---|---|
| [wow-arvore](vegetacao/wow-arvore.jpg) | World of Warcraft | Árvore marco de área; massas de folhagem. |
| [palia-horta](vegetacao/palia-horta.jpg) | Palia | Plantas de coleta e profissão. |
| [torchlight2-arvores](vegetacao/torchlight2-arvores.jpg) | Torchlight II | Bordas de área com vegetação; acento de cor na copa. |

## assets-do-projeto

Folhas do próprio jogo, sem cópia: o manifesto aponta para [docs/arte/renders/](../renders/). Comparar o que se faz com elas é o teste de coerência com o que já existe.

| ID | Arquivo | O que é |
|---|---|---|
| projeto-javali-do-campo-vistas | [JavaliDoCampo_vistas.png](../renders/JavaliDoCampo_vistas.png) | Folha de conferência do Javali do Campo (modelo próprio, padrao-de-modelos.md). Categoria de assunto: criaturas. |
| projeto-presa-velha-vistas | [PresaVelha_vistas.png](../renders/PresaVelha_vistas.png) | Folha de conferência da Presa-Velha (modelo próprio, padrao-de-modelos.md). Categoria de assunto: criaturas. |
| projeto-guardiao-do-reflexo-vistas | [GuardiaoDoReflexo_vistas.png](../renders/GuardiaoDoReflexo_vistas.png) | Folha de conferência do Guardião do Reflexo (modelo próprio, padrao-de-modelos.md). Categoria de assunto: criaturas. |
| projeto-iluminacao-antes | [iluminacao-antes.png](../renders/iluminacao-antes.png) | Cena do jogo antes do ajuste de iluminação (PR #315). Categoria de assunto: luz-cor. |
| projeto-iluminacao-depois | [iluminacao-depois.png](../renders/iluminacao-depois.png) | Cena do jogo depois do ajuste de iluminação (PR #315); estado de referência da luz. Categoria de assunto: luz-cor. |

## Limitações

- **Sem press kit aberto.** Blizzard, Rare e THQ Nordic têm sites de imprensa com login; Runic Games fechou. As imagens vêm das páginas públicas de loja (Steam) e do site oficial de WoW, publicadas pelos estúdios para divulgação. Não há confirmação de que possam ser redistribuídas, por isso `license_note` é "desconhecido".
- **WoW:** só imagens da página da expansão Midnight. As ilustrações de zona da mesma página (pinturas conceituais de ~5 MB, não in-game) ficaram de fora.
- **Darksiders II** é a Deathinitive Edition (remasterização de 2015); volume e proporção são os mesmos do original.
- **Torchlight II e Darksiders II** mostram HUD e câmera isométrica ou de ação; leia a forma e o material, não o enquadramento.
- **armaduras-armas** e **vegetacao** têm 3 imagens: as outras fontes oficiais não mostravam equipamento ou planta de perto. Equipamento também aparece em [personagens](#personagens).
