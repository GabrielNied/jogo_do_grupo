# 0025. Modelos próprios gerados no Blender por script

**Data:** 2026-10-11
**Status:** aceita

## Contexto

Os primeiros inimigos usavam modelos prontos de pacotes diferentes (porco e dragão da Quaternius, [ADR 0014](0014-conteudo-3d-e-agentes-artistas.md)), recoloridos. Cada pacote tem seu estilo, então o jogo não tinha um padrão visual, e o dragão não tinha nada a ver com o Guardião do Reflexo. O Gabriel decidiu (issue #291) ter modelos próprios, low poly com texturas bem feitas. Modelar à mão não cabe no fluxo dos agentes, e o Blender roda sem janela (`--background`) com Python e numpy embutidos.

## Decisão

- Os modelos próprios são **gerados por scripts Python no Blender headless**, em [arte/blender/](../../arte/blender/) (pasta da raiz, porque os scripts não são do Unity nem de uma funcionalidade só): uma biblioteca reutilizável (`lib/`) e um script por modelo (`criaturas/`).
- O script faz tudo: malha por partes, UV, textura pintada calculada em numpy no espaço 3D (sem emendas), oclusão assada no Cycles, esqueleto, animações por keyframes e FBX para o Unity. Os valores ajustáveis ficam em constantes no topo.
- O FBX e os PNG ficam na pasta `Art/Modelos/` da funcionalidade; material, controlador e prefab continuam sendo criados pelo **gerador de editor do Unity** (regra 9).
- O padrão visual (polígonos, textura, escala, esqueleto, nomes das animações) fica em [padrao-de-modelos.md](../arte/padrao-de-modelos.md).
- O agente confere o resultado com renders do Blender (`--previa`) antes de entregar.
- Os assets CC0 de terceiros continuam valendo para o que não tiver modelo próprio; os que deixam de ser usados ficam no repo e são marcados em [licencas.md](../arte/licencas.md).

## Consequências

- Um estilo só para as criaturas, reproduzível: mudar uma cor ou uma proporção é editar uma constante e rodar um comando.
- Sem licença de terceiros nos modelos próprios.
- Precisa do Blender 5.2 na máquina para regerar (o FBX e os PNG vão no repo, então o Unity e o CI não precisam dele).
- A forma é limitada ao que dá para descrever por código (partes simples, pele rígida sem deformação suave); personagens com roupa ou rosto expressivo vão pedir outro caminho.
- O visual final ainda pede o olho de uma pessoa no editor, com a luz do jogo.
