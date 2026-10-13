# 0036. Limites de entrada no protocolo UDP

**Data:** 2026-10-11
**Status:** proposta

## Contexto

Pacotes fragmentados e tentativas repetidas podem gastar memória, desserialização e consultas sem encerrar a conexão. O limite de mensagens por segundo não limita custo individual nem conexões pendentes.

## Decisão

- Limitar o tamanho total antes de copiar o leitor LiteNetLib e desserializar; padrão de 16 KiB incluindo cabeçalho.
- Validar JSON por tokens com profundidade configurável (32), inclusive campos desconhecidos, antes de construir DTOs.
- Permitir até 10 recusas e 3 exceções de manipulador durante toda a conexão; a ocorrência seguinte encerra a sessão pelo caminho comum, com `SessaoEncerrada` uma vez para autenticadas.
- Permitir 3 tentativas de autenticar, contando corpos inválidos e pedidos enquanto outra tentativa está em andamento; a quarta desconecta sem validar JWT nem consultar banco.
- Reservar no aceite até 16 conexões pendentes por IP; autenticar, sair ou expirar libera a vaga. O prazo inclui consultas em andamento; resultados tardios não restauram sessões encerradas.
- Validar todos os limites ao ler a configuração. Valores podem ser ajustados sem mudar os contratos das funcionalidades.

## Consequências

JSON passa por duas leituras limitadas, sem construir uma árvore intermediária. Contadores não se recuperam ao longo da sessão, portanto erros persistentes do client ou do servidor exigem reconexão. Usuários no mesmo NAT compartilham vagas pendentes, mas conexões autenticadas deixam de consumi-las.

O limite de aplicação acontece depois da remontagem de fragmentos pelo LiteNetLib: não limita sozinho a memória usada no transporte para fragmentos incompletos. A proteção desse estágio continua sendo responsabilidade do transporte; este ADR limita cópia, parsing e despacho na aplicação.
