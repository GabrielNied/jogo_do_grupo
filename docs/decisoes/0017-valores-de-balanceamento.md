# 0017. Separar valores de balanceamento dos cálculos

**Data:** 2026-10-11
**Status:** aceita

## Contexto

XP, velocidades e capacidades serão calibrados durante o desenvolvimento. Números espalhados na lógica dificultam localizar ajustes e podem deixar client e servidor divergentes. O pedido da tarefa #136 estabelece que esses valores devem ser fáceis de alterar.

## Decisão

Adotar o padrão de [Valores de balanceamento](../convencoes.md#valores-de-balanceamento). A configuração pertence à funcionalidade; não haverá um arquivo global misturando todas as regras. Na #136, `ConfigExperiencia` reúne os valores provisórios da progressão.

## Consequências

Os ajustes ficam localizáveis e têm uma única fonte. Constantes exigem recompilar; configuração dinâmica, quando necessária, exige carregamento e validação no servidor. Esta mudança não migra os valores das outras funcionalidades.
