# 0001. Unity no cliente e .NET no servidor, tudo em C#

**Data:** 2026-10-10
**Status:** aceita

## Contexto

O jogo é um MMORPG 3D de mundo aberto para PC. O grupo é mais forte em TypeScript e depois em C#. Foram avaliados Unity, Unreal, Godot, Cocos Creator e cliente sem engine.

## Decisão

- Cliente em **Unity**.
- Servidor em **.NET**, sem engine.
- Uma biblioteca **Shared** em C# usada pelos dois lados.

## Consequências

- Uma linguagem só, com regras compartilhadas sem duplicar código.
- O servidor .NET aproveita vários núcleos, importante para a simulação de um MMORPG.
- O grupo precisa se aprofundar em C#, que é próximo de TypeScript.
- O Shared fica limitado a .NET Standard 2.1 e C# 9 por causa do Unity.
