# 0027. Configuração visual compartilhada de Lumora

**Data:** 2026-10-11
**Status:** proposta (implementada para revisão do Core)

## Contexto

O usuário adotou a direção visual do laboratório como padrão das novas telas e pediu ajustes globais de cores, tamanhos e legibilidade. Copiar valores e código da feature de demonstração criaria divergências e dependências entre funcionalidades.

## Decisão

- Manter valores editáveis em `design/interface/config-visual.json`; gerar tokens USS e constantes C# para não manter duas configurações independentes.
- Oferecer a nova biblioteca `JogoDoGrupo.Client.Visual` no Core, sem dependências de features. Adesão explícita por raiz; telas antigas não são migradas automaticamente.
- Usar eventos de geometria da raiz para classes responsivas. Testes de consistência não substituem avaliação real no Unity.
- Registrar o padrão adotado e o processo dos agentes no [guia de estilo](../../design/interface/GUIA-DE-ESTILO.md).

## Consequências

Mudanças visuais propagam às telas aderentes pelo gerador, que precisa ser executado e conferido na tarefa. Fontes e licenças são copiadas para Resources a partir das matrizes de design, com metadados binários gerados pelo Unity. A nova área compartilhada exige review do Core e validação na engine antes de considerar a migração confirmada.
