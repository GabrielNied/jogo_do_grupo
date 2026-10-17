# 0046. Character Playground como ferramenta de Editor de Personagem

**Data:** 2026-10-17
**Status:** aceita

## Contexto

O Human Male validado precisa de inspeção repetível antes de entrar no gameplay. O projeto já usa ferramentas de Editor por funcionalidade e um laboratório UI Toolkit. O input, a câmera de terceira pessoa e os apresentadores de combate/equipamentos pertencem ao fluxo real do jogador; iniciar esse fluxo seria desnecessário para inspecionar arte.

## Decisão

- Manter catálogo serializado, operações e EditorWindow em Personagem, compilados somente no Editor; cena separada em `Personagem/Dev`, fora dos builds e startup.
- Reutilizar prefab, Avatar, material e componentes visuais compartilhados; câmera de inspeção com enquadramentos e navegação livre da Scene View.
- Instanciar uma entrada do catálogo por vez; não implementar gameplay, seleção de raça ou sistemas paralelos. Integrações futuras devem reutilizar os sistemas reais.
- Não depender da animação Mixamo local com licença não estabelecida.

## Consequências

A ferramenta funciona sem sessão/servidor e novos prefabs entram pelo catálogo. Não é uma ferramenta runtime nem testa gameplay nesta versão. Uso e validação têm fonte única em [Character Playground](../arte/character-playground.md).
