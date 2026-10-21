# 0053. Sincronizar os assets usados pelo mapa via Git

**Data:** 2026-10-21
**Status:** aceita por solicitação expressa do Gabriel

## Contexto

O mapa publicado referencia casas, vegetação e texturas do Fantasy Landscape que
existiam apenas na máquina de autoria. Outros desenvolvedores recebiam o mapa sem
suas dependências. Gabriel pediu que os arquivos usados fossem enviados no Git,
evitando instalações e adaptações diferentes entre máquinas.

## Decisão

- Substituir a distribuição apenas local do ADR 0048 pela inclusão das dependências
  efetivamente referenciadas pelos assets versionados, seguindo GUIDs recursivamente.
- Preservar os arquivos e `.meta` originais, sem gerar GUIDs ou editar conteúdo Unity.
- Usar as regras existentes de Git LFS para modelos e texturas. Manter o restante do
  pacote ignorado e liberar explicitamente os arquivos selecionados no `.gitignore`.
- Preservar os avisos do fornecedor e a identificação da licença em
  [licenças](../arte/licencas.md#fantasy-landscape--assets-usados-pelo-mapa).

## Consequências

Os desenvolvedores recebem os mesmos assets pelo pull, sem reimportar o pacote.
A seleção inicial acrescenta cerca de 86 MiB ao armazenamento, com 108 assets e seus
metadados. Ao usar novos arquivos, é preciso incluir também suas dependências.
A autorização do Gabriel registra a decisão de sincronização, não uma alteração da
EULA da Asset Store ou uma concessão de direitos pelo fornecedor.
