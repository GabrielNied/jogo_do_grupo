# Server

Servidor autoritativo em .NET, sem engine. Ele é a fonte da verdade: estado do mundo, combate, itens e economia são decididos aqui. O cliente só envia intenções ("quero usar a magia X") e mostra o resultado.

## Rodando

```
dotnet run --project server/src/JogoDoGrupo.Server
```

## Organização

- `Core/`: infraestrutura (rede, persistência, eventos). Dono: core.
- `Features/<X>/`: uma pasta por funcionalidade. Cada uma tem um `<X>Module.cs`, encontrado automaticamente na inicialização.

Para criar uma funcionalidade nova, crie a pasta e uma classe que implemente `IFeatureModule`. Não é preciso registrar em lugar nenhum.

## Pendências

Decisões ainda em aberto (vão virar ADRs em `docs/decisoes/`): biblioteca de rede, banco de dados e testes automatizados.
