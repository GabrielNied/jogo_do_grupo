# Shared

Código C# usado **pelo cliente e pelo servidor**: regras do jogo, tipos, constantes e mensagens de rede.

- Alvo: .NET Standard 2.1, C# 9 (limite do Unity).
- Use `namespace X { }` com chaves (namespace de arquivo é C# 10 e o Unity não aceita).
- Nada de `UnityEngine` aqui: o servidor também compila este código.
- O código fica em `Package/Runtime/`, que o Unity importa como pacote local.

## O que colocar aqui

- Fórmulas e regras que os dois lados precisam (dano, fusão de itens, custo de talentos)
- Definições de mensagens de rede e seus IDs
- Enums e tipos de domínio (raridade, tipo de item, raça)

## O que não colocar

- Lógica que só o servidor deve conhecer (tabelas de drop, anti-cheat)
- Código de interface ou visual
