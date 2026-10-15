# Referências visuais: fluxo para agentes

Vale para quem modela, busca asset CC0, mexe no Blender ou monta conteúdo 3D no Unity ([agentes artistas](../agentes-artistas.md) e qualquer agente com tarefa visual). Use as imagens locais; **não busque referência na web** se a categoria já tem imagem aqui.

## Antes de modelar ou buscar asset

0. As imagens não estão no git ([ADR 0038](../../decisoes/0038-referencias-visuais-fora-do-git.md)). Se a pasta da categoria estiver vazia, rode `npm run arte:baixar-referencias`.
1. Leia a [bíblia visual](../biblia-visual-3d.md) (regras gerais + a seção do seu assunto) e o trecho de [padrao-de-modelos.md](../padrao-de-modelos.md) que vale para o modelo (orçamento, escala, esqueleto).
2. Filtre o [manifesto](manifesto-referencias.json) pela `category` do assunto:
   ```
   node -e "require('./docs/arte/referencias-visuais/manifesto-referencias.json').imagens.filter(i=>i.category==='criaturas').forEach(i=>console.log(i.id, i.relative_path, '|', i.artistic_principles.join('; ')))"
   ```
   Categorias: `personagens`, `criaturas`, `armaduras-armas`, `ambientes`, `arquitetura`, `materiais`, `luz-cor`, `vegetacao`, `assets-do-projeto`.
3. Abra 2 a 4 imagens da categoria (o `relative_path`, pela leitura de imagem da sua ferramenta) e pelo menos uma folha de `assets-do-projeto` do mesmo tipo.
4. Escreva, antes de abrir o Blender, o Unity ou a busca CC0, uma lista curta de **princípios concretos** que vai aplicar (ex.: "massa no tronco, pernas curtas, núcleo luminoso ciano como ponto focal"), citando os IDs. Ela vai no plano da issue.

## Na revisão

5. Compare o render do resultado (folha `--previa` ou print do jogo) com as mesmas referências e com [renders/](../renders/): silhueta, proporção, paleta, malha × textura. O que divergir de propósito vai explicado no PR.
6. O PR de arte leva a seção **Referências consultadas** com os IDs do manifesto.

## Acrescentar imagem

Só quando uma categoria não cobre o assunto. Fonte oficial do estúdio (press kit, página de loja, site oficial), sem login, sem contornar bloqueio, sem imagem gerada "no estilo de X". Mais de 50 MB num lote: pare e peça aprovação (LFS).

1. A fonte precisa de **URL direta** da imagem (é dela que os outros baixam). Salve em `docs/arte/referencias-visuais/<categoria>/<jogo>-<assunto>.<ext>`, sem recomprimir; o `.gitignore` da pasta deixa a imagem fora do commit. Nunca em `client/`.
2. Acrescente a entrada no manifesto com todos os campos (`width`, `height` e `format` do arquivo real, `source_url` exato, `retrieved_at` ISO 8601, `license_note` "desconhecido" se não houver confirmação), a linha em [licencas-referencias.md](licencas-referencias.md) e a linha da tabela em [indice.md](indice.md).
3. Rode `npm run arte:referencias` (confere arquivo, formato, dimensões, campos e imagem fora do manifesto) e, para provar que a URL serve, apague a imagem e rode `npm run arte:baixar-referencias`.
