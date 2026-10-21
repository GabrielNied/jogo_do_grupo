# Licenças dos assets de terceiros

Todo asset que entra em `client/Assets/ThirdParty/` precisa de uma linha aqui, no mesmo PR ([ADR 0014](../decisoes/0014-conteudo-3d-e-agentes-artistas.md)).

- **CC0:** uso livre, inclusive comercial, sem crédito obrigatório.
- **CC-BY:** uso livre com **crédito ao autor** no jogo (tela de créditos). Preencha a coluna "Crédito".
- **Outras licenças** (Asset Store, pagas, "só uso pessoal"): não entram sem decisão do grupo.

## Fantasy Landscape — cópia licenciada local

Gabriel autorizou em 2026-10-18 a adaptação do pack **Fantasy Landscape 2.0**, de **Pxltiger**, para uso local com URP ([ADR 0048](../decisoes/0048-fantasy-landscape-local-com-urp.md)). Fonte: [Unity Asset Store, 103573](https://assetstore.unity.com/packages/3d/environments/fantasy-landscape-103573). Licença: [Standard Unity Asset Store EULA](https://unity.com/legal/as-terms), **não CC0 nem CC-BY**. Obtido/importado pelo Gabriel; sem declaração de crédito CC-BY. Preservar README e avisos do fornecedor.

O conteúdo fica apenas local em `client/Assets/ThirdParty/Pxltiger/FantasyLandscape/`; tanto essa pasta quanto `Assets/FantasyEnvironments/` são ignoradas pelo Git. Não incluir modelos, texturas, prefabs ou materiais do pacote no repositório público. Cada integrante deve adquirir/importar sua cópia no Package Manager antes de usar os mapas que a referenciam.

Após importar, use **Jogo do Grupo → Mundo → Adaptar Fantasy Landscape para URP**. O comando move a pasta pelo Unity, preserva GUIDs e converte os materiais no lugar. Árvores já pintadas continuam referenciando os mesmos prefabs. Para reimportar, faça pela própria pasta organizada; se surgirem duas cópias, o conversor interrompe sem mesclá-las. O vento Tree Creator não é mantido; a vegetação usa materiais URP estáticos. Os prefabs de árvores recebem LOD Group na raiz, habilitando **Random Tree Rotation** e evitando o billboard legado que escurecia à distância. Por enquanto há um único nível de malha 3D, ocultado abaixo de 2% da altura da tela; não há malha reduzida nem billboard substituto. LODs já configurados pelo artista são preservados. Se o Terrain estiver aberto durante uma atualização, clique em **Refresh** na lista de árvores antes de pintar de novo.

## Fontes da identidade visual aprovadas pelo usuário

Nunito Sans e Source Sans 3 foram escolhidas explicitamente para Lumora. **Cinzel 700** (SIL OFL 1.1, The Cinzel Project Authors) entrou em 2026-10-21 nos títulos da interface Asterra, como já recomendava o [padrão tipográfico](../../design/tipografia/PADRAO-TIPOGRAFICO.md); a licença fica em `design/laboratorio-unity/fontes/cinzel-OFL.txt` e na cópia de `Core/UI/Visual/Resources/Lumora/Fontes/`. Suas licenças são **SIL OFL 1.1**, preservadas com as fontes em `design/tipografia/fontes/` e nas cópias geradas para `Core/UI/Visual/Resources/Lumora/Fontes/`. As instâncias estáticas usam nomes internos próprios `LumoraNunito` e `LumoraSource`; avisos de autoria e licenças acompanham a distribuição. Origem: Google Fonts, [Nunito Sans](https://github.com/google/fonts/tree/main/ofl/nunitosans) e [Source Sans 3](https://github.com/google/fonts/tree/main/ofl/sourcesans3). Procedimento de derivação e fonte única: [guia visual](../../design/interface/GUIA-DE-ESTILO.md).

## Animações Mixamo fornecidas pelo usuário — protótipo do Playground

Em 2026-10-17 o usuário forneceu `Idle.fbx`, `Walking.fbx` e `Running.fbx` e informou origem Mixamo. Originais em `J:/BlenderAssetsAI/Human_Male_Base_v1_UnityTest/`; cópias byte a byte em `client/Assets/_Game/Features/Personagem/Dev/CharacterPlayground/Locomotion/`. Uso autorizado nesta TASK 06 para o teste de locomoção; sem pesquisa externa de licença nem declaração de CC0/CC-BY. Antes de distribuição definitiva, conservar/verificar os termos e comprovantes aplicáveis com quem forneceu os arquivos. Não confundir com `OldManIdle.fbx`, que continua local, ignorado e sem dependência da ferramenta.

Configuração, Avatars, clips e retarget: [Character Playground](character-playground.md#locomotion-animation-set-v1-task-06).

## Modelos de terceiros

| Pacote / modelo | Fonte | Licença | Link | Pasta | Crédito | Data | Quem baixou |
|---|---|---|---|---|---|---|---|
| Quaternius "LowPoly RPG Characters" (só `Monk.fbx`, `Monk_Texture.png` e `License.txt`; jogador: **não usado** desde 2026-10-11, cada raça tem modelo próprio, [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md); **NPCs da cidade** usam o Monk como base, #1064) | OpenGameArt (Quaternius) | CC0 | https://opengameart.org/content/lowpoly-rpg-characters | `client/Assets/ThirdParty/Quaternius/RPGCharacters/` | não obrigatório (Quaternius) | 2026-10-10 | agente artista-personagens |
| Quaternius "Stylized Nature MegaKit" (amostra para a Mata: cinco árvores, dois arbustos, dois cogumelos e três pedras) | Quaternius | CC0 | https://quaternius.itch.io/stylized-nature-megakit | `client/Assets/ThirdParty/Quaternius/StylizedNatureMegaKit/` | não obrigatório (Quaternius) | 2026-10-16 | agente Mundo |
| Kenney "Fantasy Town Kit" 2.0 (~~só 13 modelos `.fbx`~~ 22 modelos `.fbx` desde 2026-10-11: `wall-block`, `road`, `fountain-round`, `stall-red`, `stall-green`, `stall-bench`, `cart`, `tree`, `tree-high`, `rock-large`, `rock-small`, `rock-wide`, `lantern`, e, para as casas e árvores do terreno de fora, `wall`, `wall-door`, `wall-window-shutters`, `wall-wood`, `wall-wood-door`, `wall-wood-window-shutters`, `roof-point`, `tree-crooked`, `tree-high-round`, mais `Textures/colormap.png` e `License.txt`) | Kenney | CC0 | https://kenney.nl/assets/fantasy-town-kit | `client/Assets/ThirdParty/Kenney/FantasyTownKit/` | não obrigatório (Kenney) | 2026-10-10 | agente artista-mundo |
| Quaternius "LowPoly Animated Farm Animal Pack" (só `Pig.fbx` e `License.txt`; ~350 KB; ~~base do Javali do Campo e da Presa-Velha, com materiais recoloridos~~ **não usado nos prefabs** desde 2026-10-11: as criaturas usam modelos próprios, [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md)) | OpenGameArt (Quaternius) | CC0 | https://opengameart.org/content/lowpoly-animated-farm-animal-pack | `client/Assets/ThirdParty/Quaternius/FarmAnimals/` | não obrigatório (Quaternius) | 2026-10-11 | agente artista (mobs) |
| Quaternius "LowPoly Animated Monsters" (só `Dragon.fbx` e `License.txt`; ~860 KB; ~~Guardião do Reflexo~~ **não usado nos prefabs** desde 2026-10-11: o Guardião do Reflexo usa modelo próprio, [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md)) | OpenGameArt (Quaternius) | CC0 | https://opengameart.org/content/lowpoly-animated-monsters | `client/Assets/ThirdParty/Quaternius/MonsterPack/` | não obrigatório (Quaternius) | 2026-10-11 | agente artista (mobs) |
| Poly Haven "Dirt" (mapa difuso JPG 2K, estrada do Passo) | Poly Haven, Charlotte Baglioni | CC0 | https://polyhaven.com/a/dirt | `client/Assets/ThirdParty/PolyHaven/Resources/Mundo/Estrada/` | não obrigatório (Charlotte Baglioni) | 2026-10-15 | Codex |
| Poly Haven "Leafy Grass" (mapa difuso JPG, solo de grama) | Poly Haven | CC0 | https://polyhaven.com/a/leafy_grass | `client/Assets/ThirdParty/PolyHaven/Resources/Terreno/` | não obrigatório | 2026-10-16 | agente Mundo |
| Poly Haven: Forest Ground 04, Mud Forest, Sand 01, Rock Face 03 (difuso e normal), Snow 02 e Dark Rock | Poly Haven | CC0 | https://polyhaven.com | `client/Assets/ThirdParty/PolyHaven/Resources/Terreno/` | não obrigatório | 2026-10-17 | agente Mundo |
| game-icons.net, 15 ícones de silhueta (SVG rasterizado em PNG 128 px, branco sobre transparente, um por `TipoEquipamento`): `sbed/helmet`, `lorc/breastplate`, `delapouite/gloves`, `lorc/trousers`, `lorc/boots`, `lucasms/belt`, `delapouite/ring`, `delapouite/earrings`, `lorc/gem-pendant`, `lorc/broadsword`, `delapouite/warhammer`, `delapouite/bow-arrow`, `lorc/wizard-staff`, `sbed/shield`, `lorc/crystal-ball` | game-icons.net | CC-BY 3.0 | https://game-icons.net (repositório https://github.com/game-icons/icons) | `client/Assets/ThirdParty/GameIcons/Resources/Itens/Icones/` | **obrigatório** na tela de créditos: "Ícones de itens: Lorc, Delapouite, sbed e lucasms, game-icons.net, CC BY 3.0" | 2026-10-12 | agente artista-itens |
| game-icons.net, 69 ícones da interface Asterra (SVG rasterizado em PNG 128 px, branco sobre transparente): 30 do menu rápido e da barra de ação, 32 de skills (um por skill do catálogo) e 7 de efeitos de status. Lista de autor/arquivo em `Interface/ORIGEM.txt` (lorc, delapouite, skoll, willdabeast) | game-icons.net | CC-BY 3.0 | https://game-icons.net (repositório https://github.com/game-icons/icons) | `client/Assets/ThirdParty/GameIcons/Resources/Interface/` | **obrigatório** na tela de créditos: "Ícones da interface: Lorc, Delapouite, Skoll e Willdabeast, game-icons.net, CC BY 3.0" | 2026-10-21 | Claude (interface) |
| OpenGameArt "Deer Low Poly (rigged)" de crownjoshua (só `Deer.obj`, `Deer.mtl` e `License.txt`; ~40 KB; base do cervo jovem dos Pets, com materiais URP refeitos pelo gerador de editor `GeradorDosPets`) | OpenGameArt (crownjoshua) | CC0 | https://opengameart.org/content/deer-low-poly-rigged | `client/Assets/ThirdParty/OpenGameArt/DeerLowPoly/` | não obrigatório | 2026-10-15 | agente funcionalidade (Pets) |

## Pacotes do mapa autoral autorizados em 2026-10-19

O usuário confirmou origem na Unity Asset Store e autorizou explicitamente a inclusão de
`3D Haven/Free Fantasy Terrain Textures`, `Rowlan/Terrain` e `TerrainSampleAssets`
no repositório, como exceção à regra geral de CC0/CC-BY. Esta autorização não os
reclassifica como CC0 ou CC-BY nem substitui os termos dos respectivos fornecedores.
As pastas e os GUIDs originais de importação foram preservados para manter as referências
do terreno. Os recursos `HDRPDefaultResources` foram gerados pelo Unity após a importação.
Entrega e revisão das configurações gráficas: issue #1111.