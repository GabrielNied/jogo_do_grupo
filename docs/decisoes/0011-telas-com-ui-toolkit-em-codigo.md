# 0011. Telas com UI Toolkit montadas em código

**Data:** 2026-10-10
**Status:** aceita

## Contexto

As primeiras telas do client são abertura/carregamento, login, seleção e criação de personagem. Pelo fluxo multiagente ([ADR 0008](0008-desenvolvimento-multiagente.md)), os agentes não editam cenas, prefabs nem assets do Unity; se as telas dependerem do editor, cada uma precisa de uma pessoa montando.

## Decisão

- Telas feitas com **UI Toolkit**, com a árvore de elementos **montada em C#** e o estilo em arquivos **USS** (texto) carregados de pastas `Resources`.
- O Core do client cria em tempo de execução o que a interface precisa (objeto com `UIDocument` e `PanelSettings`), então não é preciso montar nada numa cena.
- **Navegação entre telas** pelo Core, por chave: cada funcionalidade registra as próprias telas e o Core troca de tela pelo nome. O Core nunca referencia uma funcionalidade.

## Consequências

- Os agentes conseguem entregar telas funcionando sem depender do editor; as pessoas só ajustam o visual depois.
- Arte e layout mais elaborados podem migrar para UXML e assets visuais com o tempo, sem mudar a navegação.
- A compilação e o teste das telas continuam sendo feitos por uma pessoa no editor do Unity.
