# 0022. Serilog para observabilidade do servidor

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O servidor precisava de logs estruturados em arquivo, com rotação e sem serviço pago, além de
contadores simples para operação. O logger padrão não oferece arquivo estruturado com rotação.

## Decisão

- Usar Serilog para os logs do servidor.
- Gravar JSON compacto em arquivo diário, mantendo os últimos 14 arquivos.
- Descartar eventos que tragam senha, token ou conteúdo de chat.
- Expor saúde em `/saude` e métricas apenas a tokens com `permissao=administrar`.

## Consequências

Os logs podem ser consultados localmente ou enviados depois a uma ferramenta sem custo. A
Economia deve chamar os contadores de ouro ao criar ou destruir moeda, e a configuração de contas
administrativas continua necessária para liberar o comando de métricas.
