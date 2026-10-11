# Server

Servidor autoritativo em .NET, sem engine. Ele é a fonte da verdade: estado do mundo, combate, itens e economia são decididos aqui. O cliente só envia intenções ("quero usar a magia X") e mostra o resultado.

## Rodando

```
dotnet run --project server/src/JogoDoGrupo.Server
```

Sobe uma API HTTP em `http://localhost:5080` (desenvolvimento) e cria o banco SQLite local na primeira vez. Conta e login usam JWT ([ADR 0009](../docs/decisoes/0009-api-http-para-conta-e-personagens.md)); o banco é EF Core com SQLite ([ADR 0010](../docs/decisoes/0010-banco-de-dados.md)).

## Organização

- `Core/`: infraestrutura (rede, persistência, eventos). Dono: core.
- `Features/<X>/`: uma pasta por funcionalidade. Cada uma tem um `<X>Module.cs`, encontrado automaticamente na inicialização.

Para criar uma funcionalidade nova, crie a pasta e uma classe que implemente `IFeatureModule`. Não é preciso registrar em lugar nenhum. Rotas, serviços, entidades e migrações: veja [docs/arquitetura.md](../docs/arquitetura.md#servidor-como-uma-funcionalidade-se-pluga).

## Pendências

Decisões ainda em aberto (vão virar ADRs em `docs/decisoes/`): biblioteca de rede para o tempo real e testes automatizados. ~~Banco de dados~~ decidido no [ADR 0010](../docs/decisoes/0010-banco-de-dados.md).
