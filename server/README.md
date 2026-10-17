# Server

Servidor autoritativo em .NET, sem engine. Ele é a fonte da verdade: estado do mundo, combate, itens e economia são decididos aqui. O cliente só envia intenções ("quero usar a magia X") e mostra o resultado.

## Rodando em desenvolvimento

```
dotnet run --project server/src/JogoDoGrupo.Server
```

Sobe uma API HTTP em `http://localhost:5080` (desenvolvimento) e cria o banco SQLite local na primeira vez. Conta e login usam JWT ([ADR 0009](../docs/decisoes/0009-api-http-para-conta-e-personagens.md)); o banco é EF Core com SQLite ([ADR 0010](../docs/decisoes/0010-banco-de-dados.md)).

## Publicação para produção (Linux)

O servidor pode ser publicado de forma independente (`self-contained`) para execução em VM Linux (Oracle Cloud ARM64 ou x64), sem necessidade de runtime .NET pré-instalado na máquina de destino.

### Via script do repositório

```
npm run server:publicar -- --rid linux-arm64
# ou para x64:
npm run server:publicar -- --rid linux-x64
```

### Via CLI do .NET

```
dotnet publish server/src/JogoDoGrupo.Server/JogoDoGrupo.Server.csproj -c Release -r linux-arm64 --self-contained -o server/publish/linux-arm64
```

## Configuração em produção

Em ambiente de produção (`ASPNETCORE_ENVIRONMENT=Production`):

| Variável de Ambiente | Descrição | Exemplo |
|---|---|---|
| `ASPNETCORE_ENVIRONMENT` | Define o ambiente | `Production` |
| `ASPNETCORE_URLS` | URLs de escuta HTTP local (atrás de proxy reverso) | `http://127.0.0.1:5080` |
| `Jwt__Chave` | Segredo HMAC-SHA256 (mínimo 32 bytes; não pode ter o prefixo dev) | Chave segura gerada |
| `Banco__Provedor` | Provedor de banco de dados (`Sqlite` ou `Postgres`) | `Postgres` |
| `ConnectionStrings__Jogo` | String de conexão para o banco principal | `Host=localhost;Database=jogodogrupo;Username=...;Password=...` |
| `Banco__MigrarAoIniciar` | Aplica migrações na inicialização (opcional em prod, padrão `false`) | `true` |
| `Observabilidade__ConsoleEstruturado` | Força logs em formato JSON compacto para systemd/journald | `true` (já padrão em Production) |

### Portas e rede

- **HTTP (5080):** Escuta internamente atrás de um proxy reverso (Caddy ou Nginx) que gerencia HTTPS/TLS na porta 443 e repassa cabeçalhos `X-Forwarded-For` e `X-Forwarded-Proto`.
- **Tempo Real UDP (5081):** Escuta diretamente no endereço público da VM (`0.0.0.0:5081`), sem passar pelo proxy reverso.

### Verificação de saúde (`/saude`)

Endpoint aberto sem autenticação para health check, monitoramento e validação de deploy contínuo:
- **Status 200 OK:** Banco de dados conectado e API pronta para receber requisições. Retorna `{"Estado":"ok","Versao":1,"Banco":"conectado"}`.
- **Status 503 Service Unavailable:** Banco de dados inacessível ou desconectado. Retorna `{"Estado":"erro","Versao":1,"Banco":"indisponivel"}`.

## Organização

- `Core/`: infraestrutura (rede, persistência, eventos). Dono: core.
- `Features/<X>/`: uma pasta por funcionalidade. Cada uma tem um `<X>Module.cs`, encontrado automaticamente na inicialização.

Para criar uma funcionalidade nova, crie a pasta e uma classe que implemente `IFeatureModule`. Não é preciso registrar em lugar nenhum. Rotas, serviços, entidades e migrações: veja [docs/arquitetura.md](../docs/arquitetura.md#servidor-como-uma-funcionalidade-se-pluga).

## Pendências

~~Biblioteca de rede para o tempo real~~ decidida: LiteNetLib por UDP ([ADR 0015](../docs/decisoes/0015-litenetlib-para-o-tempo-real.md)). ~~Testes automatizados~~ decididos no [ADR 0013](../docs/decisoes/0013-testes-automatizados.md); ~~banco de dados~~ decidido no [ADR 0010](../docs/decisoes/0010-banco-de-dados.md); ~~servidor de produção~~ configurado ([ADR 0021](../docs/decisoes/0021-hospedagem-gratuita.md)). Ainda em aberto: divisão do mundo entre servidores.
