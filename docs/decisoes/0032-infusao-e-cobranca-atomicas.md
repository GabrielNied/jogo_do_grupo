# 0032. Infusão, consumo e cobrança na mesma transação

**Data:** 2026-10-11
**Status:** proposta

## Contexto

A infusão destrói um exemplar e cobra coroas para aumentar outro. Confirmar só uma parte perderia itens ou permitiria melhorias sem pagamento. Uma prévia antiga pode apontar para peças que mudaram.

## Decisão

- Itens mantém o valor-base infusível nos metadados do exemplar. O ganho usa esse valor, sem juros compostos; itens anteriores sem infusões podem inicializá-lo a partir do atributo atual.
- A prévia não altera peças nem saldo. Seu identificador é um HMAC do personagem, identidades, metadados, posições e custo, com chave em memória do servidor. A confirmação recalcula e confere o identificador; mudanças relevantes ou reinício exigem outra prévia.
- Economia publica `ICarteiraCoroas`. A implementação usa o mesmo contexto com escopo de requisição e exige transação do chamador para créditos/débitos. O débito é condicional ao saldo, e o banco também proíbe saldo negativo.
- Itens abre a transação serializável, valida propriedade e compatibilidade, debita, altera o receptor e remove o doador. Confirma tudo junto. Erros revertem tudo; repetir ou disputar o mesmo doador não gera outra melhoria.
- A carteira é por personagem, com chave estrangeira e exclusão em cascata. A migração só cria a tabela de saldos; os itens anteriores continuam válidos.

## Consequências

Economia e Core precisam revisar o contrato e a migração; o relacionamento de persistência com Personagem também precisa de revisão. No SQLite, escritores são serializados. Requisições de confirmação não são repetidas automaticamente pelo cliente: após erro ou perda da resposta, consultar o estado revela o resultado. A chave em memória invalida prévias ao reiniciar. Distribuir o servidor entre processos exigirá compartilhar essa chave ou persistir prévias. Crédito de teste só existe em Development. O contrato não implementa loja, drops de moeda ou mercado.
