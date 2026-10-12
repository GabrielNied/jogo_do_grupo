# 0033. Mascarar dados sensíveis sem descartar logs

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O filtro da ADR 0022 descartava eventos inteiros por trechos do nome de propriedades:
`Contexto` era confundido com `texto`. Erros também desapareciam do contador operacional.
Os contadores de ouro nunca eram alimentados e apresentavam zeros sem significado.

## Decisão

- Manter Serilog, JSON compacto, rotação diária, retenção de 14 arquivos e acesso administrativo às métricas.
- Substituir o filtro por um enriquecedor que mascara valores de propriedades sensíveis com `***`.
  Comparar nomes completos sem distinguir maiúsculas, inclusive em objetos, listas e dicionários.
- Tratar `Mensagem` e `TextoChat` como conteúdo sensível; preservar o evento, o contexto e sua gravidade.
- Retirar as métricas de ouro sem uso até existir integração real com Economia.
- Configurar o arquivo com `Observabilidade:CaminhoLog`, relativo à raiz de conteúdo do servidor
  quando não for absoluto. Ignorar pastas `logs/` no Git.

## Consequências

Erros mascarados continuam contados. O endpoint administrativo deixa de expor `OuroCriado`
e `OuroDestruido`. Novos nomes de segredos devem ser incluídos explicitamente no mascaramento.
Segredos não devem ser interpolados diretamente no texto do log nem incluídos em exceções.
