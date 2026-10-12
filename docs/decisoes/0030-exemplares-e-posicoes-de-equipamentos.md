# 0030. Exemplares e posições persistentes de equipamentos

**Data:** 2026-10-11
**Status:** proposta

## Contexto

O inventário de teste identifica apenas o modelo. Equipar dois anéis iguais exige distinguir exemplares, preservar seus sorteios e impedir que duas requisições ocupem a mesma posição. Atributos incrementais a cada troca duplicariam bônus.

## Decisão

- Reutilizar a identidade de `ItemInventario` como identidade do exemplar. Gravar os atributos gerados em JSON na mesma linha; guardar a posição em coluna própria, com índice único filtrado por personagem/posição. Posição zero significa inventário.
- Persistir o conjunto de armas ativo em uma linha por personagem. O nível da peça e o requisito de nível do personagem são campos diferentes.
- Executar trocas e verificação de capacidade em transações serializáveis. No SQLite isso serializa escritores. A mudança inteira confirma ou volta atrás.
- Itens publica `IBonusEquipamentos` em seus contratos de servidor. Combate adapta os totais atuais, sem referência à implementação de Itens e sem somar incrementos cumulativos.

## Consequências

Exemplares repetidos e sorteios sobrevivem às trocas e à reconexão; itens legados continuam válidos. JSON permite preservar valores fracionários e status ainda sem fórmula de combate. Consultas SQL por atributo individual exigirão modelagem adicional se forem necessárias para mercado ou busca. A migração e a integração com Combate exigem revisão dos respectivos responsáveis. Esta decisão não define balanceamento, catálogo completo, skills, infusão nem promoção.
