# Arquitetura

## Visão geral

```
┌────────────┐      mensagens de rede      ┌────────────┐
│  Cliente   │  ─────────────────────────► │  Servidor  │
│  (Unity)   │  ◄───────────────────────── │   (.NET)   │
└─────┬──────┘                             └─────┬──────┘
      │            ┌────────────────┐            │
      └──────────► │     Shared     │ ◄──────────┘
                   │ regras e tipos │
                   └────────────────┘
```

- **Servidor autoritativo:** o servidor decide tudo que importa (dano, drops, fusão, economia). O cliente envia intenções e mostra resultados. Isso evita trapaças.
- **Shared:** regras e tipos compilados nos dois lados, para não duplicar código.

## Organização por funcionalidade

O projeto é dividido em **fatias verticais**: cada funcionalidade tem uma pasta própria no cliente, no servidor e no shared.

```
client/Assets/_Game/Features/Itens/
server/src/JogoDoGrupo.Server/Features/Itens/
shared/Package/Runtime/Features/Itens/
docs/features/itens.md
```

Assim, cada pessoa trabalha em pastas diferentes e os conflitos de merge ficam raros.

### Regras de dependência

1. Uma funcionalidade pode usar o **Core** e o **Shared**.
2. Uma funcionalidade **não referencia outra diretamente**. Se Economia precisa saber que um item foi fundido, Itens publica um evento e Economia escuta. No Unity, os Assembly Definitions impedem referências diretas.
3. Contratos entre funcionalidades (eventos, interfaces) ficam no `Shared/Features/<dono>/`, definidos por quem publica.

### Pontos de conflito eliminados

| Ponto de conflito comum | Como evitamos |
|---|---|
| Arquivo central registrando sistemas | O servidor descobre os módulos sozinho (`IFeatureModule`) |
| Arquivo central com IDs de mensagem | Cada funcionalidade tem uma faixa de IDs própria |
| Cenas grandes editadas por várias pessoas | Uma cena por região, carregadas de forma aditiva |
| Arquivos binários editados ao mesmo tempo | Git LFS com lock |
| Um assembly único no Unity | Um Assembly Definition por funcionalidade |

## Faixas de IDs de mensagem

Cada funcionalidade recebe uma faixa de 1000 IDs, declarada em `shared/Package/Runtime/Features/<X>/<X>MessageIds.cs` com um `MessageIdRange`. A tabela com todas as faixas fica no [README da raiz](../README.md#funcionalidades). Uma funcionalidade nova pega a próxima faixa livre.

## Decisões em aberto

- Biblioteca de rede (ex.: LiteNetLib)
- Banco de dados e persistência
- Como dividir o mundo aberto entre servidores (por região?)
- Testes automatizados

Cada uma vira um ADR em [decisoes/](decisoes/) quando for decidida.
