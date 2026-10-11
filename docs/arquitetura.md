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

- **Servidor autoritativo:** o servidor decide tudo que importa (dano, drops, fusão, economia). O cliente envia intenções e mostra resultados. Isso evita trapaças. (O lançamento do New World teve exploits atribuídos a decisões confiadas ao client; veja as [referências](referencias.md#new-world-amazon-2021).)
- **Economia atômica:** qualquer operação que move ouro ou itens (troca, venda, fusão, craft) acontece por inteiro ou não acontece, numa única transação. Falha no meio nunca pode duplicar nem sumir com nada.
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

## Comunicação

| Canal | Usado para | Contrato |
|---|---|---|
| **API HTTP** (JSON, token JWT) | Conta, login, seleção e criação de personagem | DTOs e rotas em `shared/Package/Runtime/{Core,Features/<X>}/` ([ADR 0009](decisoes/0009-api-http-para-conta-e-personagens.md)) |
| **Tempo real** (UDP, a decidir) | Movimento, combate, sessões de jogo | Mensagens com IDs na faixa de cada funcionalidade |

Telas do client: UI Toolkit montada em código ([ADR 0011](decisoes/0011-telas-com-ui-toolkit-em-codigo.md)).

## Servidor: como uma funcionalidade se pluga

O servidor é um app ASP.NET Core em `http://localhost:5080` (desenvolvimento). O `Program.cs` encontra todos os `IFeatureModule` sozinho e chama, nesta ordem, `ConfigurarServicos`, `MapearRotas` e `Inicializar`. Os dois primeiros são opcionais ([ADR 0012](decisoes/0012-pontos-de-extensao-do-servidor.md)).

```csharp
public sealed class PersonagemModule : IFeatureModule
{
    public string Nome => "Personagem";

    public void ConfigurarServicos(IServiceCollection servicos, IConfiguration configuracao) =>
        servicos.AddScoped<ServicoPersonagens>();

    public void MapearRotas(IEndpointRouteBuilder rotas)
    {
        var grupo = rotas.MapGroup("/api/personagens").RequireAuthorization();
        grupo.MapGet("", ListarAsync);
    }

    public void Inicializar(ServerContext contexto) { }

    private static async Task<IResult> ListarAsync(ClaimsPrincipal usuario, JogoDbContext db) { ... }
}
```

- **Serviços:** registre em `ConfigurarServicos`. Prefira `AddScoped` para o que usa o banco.
- **Rotas:** mapeie em `MapearRotas` usando as constantes de rota do shared. `RequireAuthorization()` exige login; sem token válido, o Core responde 401 `NaoAutenticado`.
- **Conta logada:** `usuario.ObterContaId()` (`using JogoDoGrupo.Server.Core.Autenticacao;`) devolve o Id da conta (`long`) numa rota com `RequireAuthorization()`. Receba o `ClaimsPrincipal` como parâmetro da rota.
- **Erros:** toda resposta 4xx/5xx sai como `ErroApi`. Use `RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroX.Algo)` (`using JogoDoGrupo.Server.Core.Api;`). Corpo ausente ou JSON malformado (400 `RequisicaoInvalida`) e exceções (500 `ErroInterno`) o Core já trata.
- **JSON:** campos públicos e nomes como no C# (compatível com o `JsonUtility` do Unity), enums como número. Os DTOs do shared funcionam direto como parâmetro e retorno (`Results.Ok(dto)`).
- **Entidades:** crie a classe e uma `IEntityTypeConfiguration<T>` na pasta da funcionalidade; o `JogoDbContext` (`JogoDoGrupo.Server.Core.Persistencia`) as descobre sozinho. Acesse com `db.Set<T>()`. Para ligar à conta, guarde o `ContaId` (`long`) com índice.
- **Unicidade:** cheque antes de salvar e, para a corrida entre duas requisições, capture `catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())` e responda 409.
- **Banco ocupado:** `ErrosBanco.EhBancoOcupado()` reconhece SQLite ocupado/bloqueado (5/6) e falha de serialização SQLSTATE `40001`, inclusive em exceções internas. Ao sair da transação sem commit, ela é desfeita; responda 503 com `CodigosErro.ServidorOcupado` para permitir nova tentativa. Outras falhas continuam como 500 `ErroInterno`. O reconhecimento de SQLSTATE prepara a classificação de erros para PostgreSQL; o provedor e a migração ainda não foram implementados.
- **Migrações:** mudou entidade, gere a migração no mesmo PR. Ela vai para `Core/Persistencia/Migrations/` sozinha. Da raiz do repo:

  ```
  dotnet build JogoDoGrupo.slnx
  dotnet dotnet-ef migrations add <NomeEmPascalCase> --project server/src/JogoDoGrupo.Server
  dotnet csharpier format .
  ```

  Em desenvolvimento o servidor aplica as migrações ao iniciar (o banco é o arquivo `server/src/JogoDoGrupo.Server/jogodogrupo.db`, fora do git; para zerar, pare o servidor e apague o arquivo). Se outro PR com migração entrar antes, atualize a branch, rode `migrations remove` e gere de novo.

**Configuração** (`server/src/JogoDoGrupo.Server/appsettings.json`): `ConnectionStrings:Jogo` e a seção `Jwt` (`Chave`, `Emissor`, `Audiencia`, `ValidadeHoras`). A URL `http://localhost:5080` e a chave de desenvolvimento estão em `appsettings.Development.json`. Em produção, defina as variáveis de ambiente `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS` (a URL em que o servidor escuta) e `Jwt__Chave` (pelo menos 32 bytes). O servidor não sobe com chave curta, com `Jwt:Emissor` ou `Jwt:Audiencia` vazios, nem com a chave de desenvolvimento fora do ambiente Development.

## Client: como uma funcionalidade se pluga

O client monta a interface em código ([ADR 0011](decisoes/0011-telas-com-ui-toolkit-em-codigo.md)): ao dar Play em qualquer cena, o Core cria o `UIDocument`, abre a tela de abertura e depois o login. As funcionalidades só registram as próprias telas; o Core nunca referencia uma funcionalidade. Código do Core em `client/Assets/_Game/Core/Scripts/`.

```csharp
public static class TelasPersonagem
{
    public const string Selecao = "personagem.selecao";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Registrar()
    {
        Navegacao.Registrar(Selecao, () => new TelaSelecao());
        Navegacao.DefinirTelaAposLogin(Selecao);
    }
}

public sealed class TelaSelecao : Tela
{
    protected override void Montar(VisualElement raiz) { /* monta os elementos com as classes de EstiloCore */ }

    protected override async void AoAbrir()
    {
        var resposta = await ClienteApi.Get<ListaPersonagensResponse>(RotasPersonagem.Personagens);
        if (!Aberta) return; // a tela pode ter sido fechada durante a espera (ex.: sessão expirou)
        // ...
    }
}
```

- **Telas:** herde de `Tela` (`JogoDoGrupo.Client.Core.UI`), monte os elementos em `Montar` e carregue dados em `AoAbrir`. Confira `Aberta` depois de **cada** `await`.
- **Registro:** num método `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`, chame `Navegacao.Registrar(chave, () => new MinhaTela())`. Chaves no formato `"<funcionalidade>.<tela>"`. O Core abre a primeira tela só depois (`AfterSceneLoad`), então a ordem é garantida.
- **Navegação:** `Navegacao.Abrir(chave)` troca de tela (cria uma instância nova). `DefinirTelaAposLogin(chave)` escolhe a tela que abre depois do login. `VoltarParaLogin()` encerra a sessão ("Sair"). `Abrir` não recebe parâmetros: para passar dados entre telas da mesma funcionalidade, use um estado estático da funcionalidade.
- **Servidor:** `ClienteApi.Get<T>(rota)` e `ClienteApi.Post<T>(rota, corpo)` (`JogoDoGrupo.Client.Core.Api`) devolvem `RespostaApi<T>` com `Sucesso`, `Status`, `Corpo` e `CodigoErro`. Não lançam exceção por erro de rede ou HTTP. Um 401 numa rota protegida encerra a sessão e abre o login sozinho. Use as rotas e DTOs do shared.
- **Estilo:** as classes de `EstiloCore` (`Painel`, `Titulo`, `Campo`, `Botao`, `BotaoPrincipal`, `MensagemErro`...) já valem em qualquer tela. Estilo próprio vai em USS dentro de `Features/<X>/UI/Resources/`, carregado com `Resources.Load`. O visual é moldura de jogo (painel escuro, borda dourada, cantos retos, texto quente), no espírito das telas de World of Warcraft. Cor, tamanho e borda ficam no USS. No C# só entram a classe (`AddToClassList`) e o que é comportamento (`display` para mostrar ou esconder, `pickingMode`). Painel aberto por cima do mundo usa `TelaSobreJogo` junto com `Tela`, para o véu não tapar a cena com um fundo opaco. Uma fonte ornamental entra só quando houver asset CC0; até lá o contraste vem da cor e da moldura.
- **Sessão:** ~~`Sessao` guarda o token só em memória.~~ substituído em 2026-10-11 por: `Sessao` (`JogoDoGrupo.Client.Core.Contas`) guarda o token em memória e neste computador (sem a senha). A abertura seguinte pula o login até a pessoa usar "Sair". O token do servidor vale cerca de 100 anos (`Jwt:ValidadeHoras`): o formato pede uma data, e quem encerra a sessão é o "Sair". Sessão recusada pelo servidor também apaga o gravado. O `ClienteApi` envia o token sozinho.
- **Unity:** scripts novos geram `.meta` ao abrir o editor; commite os `.meta` no mesmo PR ou logo depois. O client chama o servidor por HTTP em desenvolvimento: em **Project Settings > Player > Other Settings**, **Allow downloads over HTTP** precisa estar em "Allowed in development builds".

### Entrada no mundo

Mundo e Personagem registram no Core quem monta o hub e quem cria o visual. O Core não referencia essas funcionalidades. Código em `JogoDoGrupo.Client.Core.Jogo` (`client/Assets/_Game/Core/Scripts/Jogo/`).

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
private static void Registrar()
{
    EntradaNoJogo.DefinirConstrutorDeMundo(ConstrutorHub.Construir);
    EntradaNoJogo.DefinirVisualDoJogador(_ =>
        Resources.Load<GameObject>("Personagem/Jogador_Humano"));
}
```

- **Construtor de mundo:** `DefinirConstrutorDeMundo(Func<PosicaoInicial>)`. Cria os objetos do hub e devolve onde o jogador nasce (`PosicaoInicial`: posição dos pés e rotação; `PosicaoInicial.Origem` é a origem olhando para +Z). `null` tira o registro e o Core monta um chão.
- **Visual:** `DefinirVisualDoJogador(Func<long, GameObject>)`. Recebe o id do personagem e devolve uma instância ou o próprio prefab (o Core instancia). Pés em Y=0, frente em +Z. O Core desliga os colisores do visual e o root motion: quem colide é o `CharacterController` (altura `AlturaJogador` 1,8 m, raio `RaioJogador` 0,35 m). `null` tira o registro e o Core usa uma cápsula. O prefab humano e os parâmetros do Animator (`Velocidade`, `NoChao`) estão em [personagem.md](features/personagem.md#visual-do-jogador-arte).
- **Entrar e sair:** `Entrar(long personagemId)` esconde a interface, cria uma cena em código, monta o mundo, cria o jogador local (controle em 3ª pessoa, câmera que orbita com o mouse e menu do Esc) e o posiciona. Tudo criado em `Entrar`, inclusive pelo construtor e pelo visual, vai para essa cena. `Sair()` descarrega a cena e abre a tela após o login. `NoJogo`, `PersonagemId` e `Jogador` descrevem o estado. Se o construtor ou o visual falhar, o Core desmonta e volta para a seleção.
- **Menu:** Esc abre "Continuar", "Voltar à seleção" (`Sair`) e "Sair da conta" (desmonta o mundo, apaga a sessão gravada e abre o login).
- **Sessão:** se a sessão expirar no mundo, o Core desmonta o mundo antes de abrir o login.

~~O botão **Entrar** da seleção ainda não chama `Entrar`~~ substituído em 2026-10-11 por: o botão **Entrar** chama `EntradaNoJogo.Entrar` com o id selecionado (fluxo em [personagem.md](features/personagem.md#fluxo-de-entrada-no-jogo)).

## Faixas de IDs de mensagem

Cada funcionalidade recebe uma faixa de 1000 IDs, declarada em `shared/Package/Runtime/Features/<X>/<X>MessageIds.cs` com um `MessageIdRange`. A tabela com todas as faixas fica no [README da raiz](../README.md#funcionalidades). Uma funcionalidade nova pega a próxima faixa livre.

## Decisões em aberto

- Biblioteca de rede para o **tempo real** (ex.: LiteNetLib). Conta e personagens já usam HTTP ([ADR 0009](decisoes/0009-api-http-para-conta-e-personagens.md)).
- ~~Banco de dados e persistência~~ decidido: EF Core com SQLite no desenvolvimento e PostgreSQL depois ([ADR 0010](decisoes/0010-banco-de-dados.md)).
- ~~Como hospedar as sessões de grupo pequeno: um processo de servidor por sessão ou várias sessões por processo? Onde fica o hub social?~~ Substituído em 2026-10-11: o formato voltou a ser mundo persistente (D1 opção A da [validação](brainstorm/validacao.md)).
- ~~Como dividir o mundo aberto entre servidores (por região?)~~ ~~não se aplica mais: o jogo usa sessões de grupo pequeno~~ substituído em 2026-10-11: volta a valer. Como dividir o mundo persistente entre servidores (por região?) e onde a cidade roda.
- Sincronização do combate de ação: como o servidor autoritativo valida acertos com latência (D4).
- ~~Testes automatizados~~ decidido: xUnit no shared e no servidor, com testes de integração da API ([ADR 0013](decisoes/0013-testes-automatizados.md)).

Cada uma vira um ADR em [decisoes/](decisoes/) quando for decidida.
