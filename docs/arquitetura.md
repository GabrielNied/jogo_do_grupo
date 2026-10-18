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
3. Contratos entre funcionalidades (eventos, interfaces) são definidos por quem publica. ~~Ficam no `Shared/Features/<dono>/`.~~ Substituído em 2026-10-11 ([ADR 0016](decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)): o que o **client usa** (mensagens de rede, DTOs, regras puras) fica em `shared/Package/Runtime/Features/<dono>/`; o que **só o servidor usa** (eventos e consultas entre funcionalidades) fica em `server/src/JogoDoGrupo.Server/Contratos/<Dono>/`. Uma funcionalidade pode referenciar `Contratos/` de outra, nunca `Features/` de outra.

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
| **API HTTP** (JSON, token JWT) | Conta, login, seleção e criação de personagem | DTOs e rotas em `shared/Package/Runtime/{Core,Features/<X>}/` ([ADR 0009](decisoes/0009-api-http-para-conta-e-personagens.md), [ADR 0049](decisoes/0049-rate-limiting-e-tokens-revogaveis.md)) |
| ~~**Tempo real** (UDP, a decidir)~~ **Tempo real** (UDP, LiteNetLib, [ADR 0015](decisoes/0015-litenetlib-para-o-tempo-real.md)) | Movimento, combate, presença no mundo | Mensagens com IDs na faixa de cada funcionalidade ([Servidor: tempo real](#servidor-tempo-real-udp)) |

Telas do client: UI Toolkit montada em código ([ADR 0011](decisoes/0011-telas-com-ui-toolkit-em-codigo.md)).

### Threading do servidor

O **laço principal do tempo real** é o único lugar em que as funcionalidades devem mexer em redes e estado quente do jogo: ele recebe UDP, executa a fila de pendências e dispara os ticks agendados por `ServerContext.AoPasso`. Funcionalidades que terminam trabalho assíncrono (banco, autenticação, processamento em segundo plano) devolvem a execução com `ServidorTempoReal.NoLaco(...)` e não ficam com `PeriodicTimer` privados no módulo.

Esse modelo evita `lock` espalhado por `Combate`, `Mobs` e `Itens`, reduz a chance de deadlock e mantém a ordem de eventos estável. O restante de tarefas fora do laço continua sendo dívida documentada até a próxima refatoração, enquanto `ServicoDeInimigos` já usa o tick do core como exemplo.

**Compensação de lag:** o Core expõe `IHistoricoDePosicoesDoMundo` (amostras podadas a 250 ms). Mundo e Mobs registram posição; Combate consulta no acerto e na esquiva ([ADR 0039](decisoes/0039-compensacao-de-lag-no-servidor.md)).

### Canais do mundo persistente (ADR 0045)

Para suportar capacidade escalável sem sharding geográfico complexo, cada processo de servidor roda múltiplos **canais** lógicos do mundo (`ConfigCanais.QuantidadeCanaisPadrao = 3`, configurável via `Canais:Quantidade`; capacidade de 80 jogadores por canal, `ConfigCanais.CapacidadePorCanal` / `Canais:Capacidade`).

- **Instanciação:** áreas da cidade são particionadas por canal (`cidade`, `cidade:2`, `cidade:3`...) com grades de interesse (`GradeDeInteresse`) independentes e chat de cidade isolado por canal.
- **Entrada:** membros do mesmo grupo são direcionados para o canal dos aliados se houver vaga; jogadores avulsos são alocados no canal menos cheio.
- **Troca de canal:** o client pode consultar (`ConsultarCanais`, `/canais`) e solicitar troca (`PedirTrocaDeCanal`, `/canal <N>`). A troca é permitida fora de combate e recusada em combate (`EmCombate`), se o canal for inválido (`CanalInvalido`), se for o mesmo canal (`MesmoCanal`) ou se o canal estiver lotado (`CanalLotado`). Ao trocar, as coordenadas são mantidas na nova instância e os observadores antigos/novos são atualizados de forma atômica.

## Servidor: como uma funcionalidade se pluga

### Observabilidade

O Core grava logs estruturados no caminho `Observabilidade:CaminhoLog` (padrão
`logs/servidor-.ndjson`, relativo à raiz de conteúdo do servidor), com rotação diária e retenção de 14
arquivos. Propriedades sensíveis são mascaradas, inclusive dentro de objetos e coleções,
sem descartar o evento. `GET /saude` confirma que o processo
está respondendo; `GET /admin/metricas` exige a claim `permissao=administrar` e retorna jogadores
online, erros no último minuto ([ADR 0033](decisoes/0033-mascaramento-de-logs.md)) e o tick do
laço do tempo real em `Passos` (passos por segundo e duração p50/p95/máxima dos últimos 512
passos, de `ServidorTempoReal.Passos`, e `MsDesdeOUltimoPasso`, que cresce se o laço travar).

#### Carga do tempo real

**Meta (#150):** **80 jogadores na mesma área**, todos à vista uns dos outros (a lotação de canal proposta no [#188](https://github.com/GabrielNied/jogo_do_grupo/issues/188)), com zero erros, laço a **50 passos/s ou mais** com passo **p95 até 10 ms**, e ataque de ida e volta **p95 até 100 ms** na rede local. Mede-se com os [robôs de carga](../tools/JogoDoGrupo.Robos/README.md); o `dotnet test` roda uma versão curta só para pegar regressão de protocolo.

Medido em 2026-10-15 (servidor Release e robôs no mesmo PC de desenvolvimento, SQLite): 80 robôs dão 63 passos/s, passo p95 4,8 ms e ataque p95 65 ms; 150 ainda cabem na meta (52 passos/s, p95 9,9 ms); perto de 190 o laço cai para 23 passos/s com passo p95 50 ms. Antes da CPU, o gargalo é a **banda**: cada jogador que vê outros 79 andando recebe ~800 pacotes/s (~95 KB/s), cerca de 60 Mbit/s de upload para 80 jogadores, acima do upload de um PC de casa ([hospedagem](hospedagem.md)). A área de interesse (#500) só ajuda quando as pessoas se espalham.

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
- **Erros:** toda resposta 4xx/5xx sai como `ErroApi`. Use `RespostasApi.Erro(StatusCodes.Status409Conflict, CodigosErroX.Algo)` (`using JogoDoGrupo.Server.Core.Api;`). Corpo ausente ou JSON malformado (400 `RequisicaoInvalida`) banco ocupado (503 `ServidorOcupado`) e exceções (500 `ErroInterno`) o Core já trata.
- **JSON:** campos públicos e nomes como no C# (compatível com o `JsonUtility` do Unity), enums como número. Os DTOs do shared funcionam direto como parâmetro e retorno (`Results.Ok(dto)`).
- **Entidades:** crie a classe e uma `IEntityTypeConfiguration<T>` na pasta da funcionalidade; o `JogoDbContext` (`JogoDoGrupo.Server.Core.Persistencia`) as descobre sozinho. Acesse com `db.Set<T>()`. Para ligar à conta, guarde o `ContaId` (`long`) com índice.
- **Unicidade:** cheque antes de salvar e, para a corrida entre duas requisições, capture `catch (DbUpdateException e) when (e.EhViolacaoDeUnicidade())` e responda 409 (reconhece SQLite e SQLSTATE `23505`).
- **Banco ocupado:** `ErrosBanco.EhBancoOcupado()` reconhece SQLite ocupado/bloqueado (5/6) e falha de serialização SQLSTATE `40001`, inclusive em exceções internas. Ao sair da transação sem commit, ela é desfeita; deixe a exceção subir e o `TratamentoErrosMiddleware` responde 503 `CodigosErro.ServidorOcupado` para permitir nova tentativa (não precisa de catch na funcionalidade). Outras falhas continuam como 500 `ErroInterno`. O reconhecimento de SQLSTATE prepara a classificação de erros para PostgreSQL; o provedor e a migração ainda não foram implementados.
- **Migrações:** mudou entidade, gere a migração no mesmo PR. Ela vai para `Core/Persistencia/Migrations/` sozinha. Da raiz do repo:

  ```
  dotnet build JogoDoGrupo.slnx
  dotnet dotnet-ef migrations add <NomeEmPascalCase> --project server/src/JogoDoGrupo.Server
  dotnet csharpier format .
  ```

  O servidor aplica as migrações ao iniciar quando `Banco:MigrarAoIniciar` é `true`; sem a chave, só em Development (o banco de desenvolvimento é o banco é o arquivo `server/src/JogoDoGrupo.Server/jogodogrupo.db`, fora do git; para zerar, pare o servidor e apague o arquivo). Se outro PR com migração entrar antes, atualize a branch, rode `migrations remove` e gere de novo.

### Servidor: eventos entre funcionalidades

Uma funcionalidade avisa as outras por `contexto.Eventos` (`EventosDoServidor`, em `Core/Eventos/`), sem referenciá-las ([ADR 0016](decisoes/0016-eventos-entre-funcionalidades-do-servidor.md)). Os tipos de evento e as interfaces de consulta ficam em `server/src/JogoDoGrupo.Server/Contratos/<Dono>/`, definidos por quem publica.

```csharp
public void Inicializar(ServerContext contexto)
{
    _eventos = contexto.Eventos;
    _eventos.Assinar<JogadorSaiuDaArea>(AoSairDaArea); // tipo definido por quem publica
}

private void AoEntrar(SessaoTempoReal sessao, EntrarNaCidade _) =>
    _eventos.Publicar(new JogadorEntrouNaArea { /* ... */ });
```

- **Entrega:** síncrona, na thread de quem publica, na ordem de inscrição. Publique do laço de rede e não bloqueie no manipulador.
- **Tipo exato:** quem assina `T` não recebe tipos derivados de `T`.
- **Evento dentro de evento:** o que um manipulador publica só é entregue depois que o evento atual chegou a todos.
- **Falha:** a exceção de um assinante não impede os outros; no fim, quem publicou recebe uma `AggregateException`.
- **Perguntas** (ler o estado de outra funcionalidade) usam uma interface de `Contratos/<Dono>/` registrada na DI pelo dono, e não eventos. Peça a interface pelo `IServiceProvider` (rotas, serviços) ou guarde-a no módulo.

~~Os eventos do exemplo ainda não existem; o primeiro par de verdade vem com o Mundo (issue #119).~~ Substituído em 2026-10-11: os eventos do exemplo existem. O Mundo publica `JogadorEntrouNaArea` e `JogadorSaiuDaArea` e oferece a consulta `IPosicoesDoMundo` (`Contratos/Mundo/`, veja [mundo.md](features/mundo.md#expõe)).

### Servidor: tempo real (UDP)

O Core sobe um host LiteNetLib junto com o app (código em `Core/TempoReal/`). Cada pacote é o ID da mensagem em 2 bytes (`ushort`, little-endian) seguido do DTO do shared em JSON UTF-8, no mesmo formato da API HTTP. O client conecta com a chave `TempoReal:ChaveConexao` e manda `AutenticarTempoReal` (JWT do login e personagem). Só depois de `ResultadoAutenticacaoTempoReal` aceito a conexão vira sessão e as mensagens chegam às funcionalidades. Recusada, a conexão pode tentar de novo até `TempoReal:TempoParaAutenticarMs`; depois disso, ou sem tentar, é desconectada. Uma conta tem uma sessão só: autenticar de novo (ex.: reconexão) encerra a anterior.

#### Reconexão sem perder o lugar ([#210](https://github.com/GabrielNied/jogo_do_grupo/issues/210))

**Hoje:** o client tenta reconectar sozinho depois de autenticado (`ClienteTempoReal`, com **3** tentativas com backoff). Reautenticar encerra a sessão UDP anterior. O grupo guarda a vaga do membro offline por **60 s** ([social.md](features/social.md), `RegrasGrupo.VagaGuardadaSegundos`). O mundo não reposiciona na reconexão ([mundo.md](features/mundo.md)) e concede orçamento extra de movimento.

**Alvo (#210, proposta IA):** por **60 s** após a queda o personagem permanece no mundo (parado, ainda alvo dos inimigos). Reconectar nesse prazo devolve o mesmo canal, instância e grupo; depois disso o personagem sai do mundo e do grupo como se tivesse saído do jogo. Duas sessões da mesma conta continuam proibidas.

**Decisão de design (2026-10-13):** na entrega de #210, o prazo do Social fica unificado com os **60 s** do Core (`RegrasGrupo`, [social.md](features/social.md) e testes junto com servidor e client).

**Testes esperados (#210):** reconectar antes de 60 s mantém grupo e instância; após 60 s o grupo segue sem o membro; segunda sessão da mesma conta continua recusada.

```csharp
public void RegistrarMensagens(MensagensTempoReal mensagens) =>
    mensagens.Faixa(MundoMessageIds.Faixa)
        .Ao<EntrarNaCidade>(MundoMessageIds.EntrarNaCidade, AoEntrar);

public void Inicializar(ServerContext contexto)
{
    _tempoReal = contexto.TempoReal;
    _tempoReal.SessaoEncerrada += AoSair;
}

private void AoEntrar(SessaoTempoReal sessao, EntrarNaCidade _) =>
    _tempoReal.Enviar(sessao, MundoMessageIds.EstadoDaCidade, new EstadoDaCidade { /* ... */ });
```

- **Faixa:** `RegistrarMensagens` (opcional em `IFeatureModule`, roda antes de `Inicializar`) reserva a faixa e registra um manipulador por ID. Faixa sobreposta, faixa do Core, ID fora da faixa ou ID repetido impedem o servidor de subir.
- **Recusa:** mensagem com ID fora de qualquer faixa registrada, sem manipulador, com JSON inválido ou de conexão não autenticada é descartada (a conexão continua até o limite de recusas) e dispara `MensagemRecusada`.
- **Limite por conexão:** cada conexão, autenticada ou não, pode mandar em média `TempoReal:MensagensPorSegundo` pacotes (60) com rajada de `TempoReal:RajadaMensagens` (120). Acima disso o servidor registra um aviso no log com a conta, dispara `MensagemRecusada` com `LimiteExcedido` e derruba a conexão (proteção contra trapaça, #211).
- **Proteções de entrada:** limites de tamanho, profundidade JSON, recusas/exceções, tentativas de autenticação e conexões pendentes por IP ficam em `TempoReal`. Padrões e fronteira da remontagem de fragmentos estão no [ADR 0036](decisoes/0036-limites-do-protocolo-udp.md). Acima do máximo, a sessão encerra pelo caminho comum.
- **Sessão:** `SessaoTempoReal` traz `Id`, `ContaId` e `PersonagemId`. `contexto.TempoReal.Sessoes` lista as autenticadas.
- **Envio:** `Enviar(sessao, id, dto, entrega)` e `EnviarParaTodos(id, dto, entrega, exceto)`. `EntregaTempoReal.Confiavel` (padrão) chega sempre e na ordem; `Sequenciada` pode se perder e descarta pacote velho (use para posição). Com `Sequenciada`, informe `entidadeId`: o pacote vai no canal `entidadeId % TransporteTempoReal.Canais` (shared), para posições de entidades diferentes não se descartarem entre si. `NetManager.ChannelsCount` no servidor **e** no `TransporteLiteNetLib` do Unity tem de ser `TransporteTempoReal.Canais`; a LiteNetLib descarta em silêncio canal acima do configurado (#826). `npm run conferir-contratos-rede` (pre-push e CI) barra `NetManager` sem a constante. Sem `entidadeId`, a sequenciada vai no canal 0 e divide com as outras. Teste EditMode: `TransporteLiteNetLibTestes.Sequenciada_NoUltimoCanalDoServidor_Chega`. `Desconectar(sessao)` encerra.
- **Eventos:** `SessaoAutenticada` e `SessaoEncerrada` (saiu, caiu depois de `TempoReal:TempoQuedaMs`, foi substituída ou desconectada).
- **Laço único:** manipuladores e eventos rodam um de cada vez no laço de rede; não bloqueie nele. Trabalho com banco vai para uma `Task`, que responde com `Enviar` (pode ser chamado de qualquer thread).
- **Personagem da conta:** o Core pergunta a um `IVerificadorPersonagem` registrado na DI. Sem ele, toda autenticação é recusada com `AcessoNegado`.

**Configuração** (`server/src/JogoDoGrupo.Server/appsettings.json`): `ConnectionStrings:Jogo`, a seção `TempoReal` (`Endereco`, `Porta` 5081, `ChaveConexao`, `TempoParaAutenticarMs`, `TempoQuedaMs`, `MensagensPorSegundo`, `RajadaMensagens`) a seção `Jwt` (`Chave`, `Emissor`, `Audiencia`, `ValidadeHoras`) e `Banco:MigrarAoIniciar` (opcional; veja Migrações). A URL `http://localhost:5080` e a chave de desenvolvimento estão em `appsettings.Development.json`. Em produção, defina as variáveis de ambiente `ASPNETCORE_ENVIRONMENT=Production`, `ASPNETCORE_URLS` (a URL em que o servidor escuta) e `Jwt__Chave` (pelo menos 32 bytes). O servidor não sobe com chave curta, com `Jwt:Emissor` ou `Jwt:Audiencia` vazios, nem com a chave de desenvolvimento fora do ambiente Development.

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
- **Estilo:** as classes de `EstiloCore` continuam disponíveis e agora consomem o padrão [Lumora UI](../design/interface/GUIA-DE-ESTILO.md). `EstiloCore.Aplicar(raizDocumento)` instala os tokens e componentes também nos documentos independentes de HUD; `EstiloCore.CriarPainel()` cria um painel rolável. A fonte única de valores é a configuração indicada no guia. A moldura do grupo pode solicitar a variante social por `JanelaDoJogo.MolduraSocial`. USS da funcionalidade define o layout com os mesmos tokens.
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
        Resources.Load<GameObject>("Personagem/Jogador_Valdren"));
}
```

- **Construtor de mundo:** `DefinirConstrutorDeMundo(Func<PosicaoInicial>)`. Cria os objetos do hub e devolve onde o jogador nasce (`PosicaoInicial`: posição dos pés e rotação; `PosicaoInicial.Origem` é a origem olhando para +Z). `null` tira o registro e o Core monta um chão.
- **Visual:** `DefinirVisualDoJogador(Func<long, GameObject>)`. Recebe o id do personagem e devolve uma instância ou o próprio prefab (o Core instancia). Pés em Y=0, frente em +Z. O Core desliga os colisores do visual e o root motion: quem colide é o `CharacterController` (altura `AlturaJogador` 1,8 m, raio `RaioJogador` 0,35 m). `null` tira o registro e o Core usa uma cápsula. O prefab humano e os parâmetros do Animator (`Velocidade`, `NoChao`) estão em [personagem.md](features/personagem.md#visual-do-jogador-arte).
- **Entrar e sair:** `Entrar(long personagemId)` esconde a interface, cria uma cena em código, monta o mundo, cria o jogador local (controle em 3ª pessoa, câmera que orbita com o mouse e menu do Esc) e o posiciona. Tudo criado em `Entrar`, inclusive pelo construtor e pelo visual, vai para essa cena. `Sair()` descarrega a cena e abre a tela após o login. `NoJogo`, `PersonagemId` e `Jogador` descrevem o estado. Se o construtor ou o visual falhar, o Core desmonta e volta para a seleção.
- **Menu:** Esc abre "Continuar", "Voltar à seleção" (`Sair`) e "Sair da conta" (desmonta o mundo, apaga a sessão gravada e abre o login).
- **Janelas do jogo:** personagem, inventário, missões e mapa abrem por atalho pelo `JanelasDoJogo` (`JogoDoGrupo.Client.Core.Jogo.Janelas`). A funcionalidade herda de `JanelaDoJogo` (como `Tela`: `Montar`, `AoAbrir`, `AoFechar`, confira `Aberta` depois de cada `await`) e registra em `BeforeSceneLoad` com `JanelasDoJogo.Registrar(new DefinicaoDeJanela(chave, titulo, tecla, ordem, () => new MinhaJanela()))`. O Core cria a ação de input, a moldura, a barra de atalhos e cuida do Esc, do cursor e da câmera (várias janelas abertas, personagem andando); a funcionalidade **não** cria `InputAction` para abrir janela nem mexe em `Cursor` ou `InputAtivo`. `JanelasDoJogo.Abrir(chave)` abre por código (ex.: o baú abre o inventário), `EstaAberta(chave)` e `Fechar(chave)` cuidam da sua janela (`Fechar()` sem chave fecha a de cima) e `JanelasDoJogo.AlgoAberto` diz se há janela ou menu na tela (para não agir por baixo). Sem conteúdo ainda? Use `JanelaEmConstrucao`. Regras em [interface.md](features/interface.md#janelas-do-jogo).
- **Sessão:** se a sessão expirar no mundo, o Core desmonta o mundo antes de abrir o login.

~~O botão **Entrar** da seleção ainda não chama `Entrar`~~ substituído em 2026-10-11 por: o botão **Entrar** chama `EntradaNoJogo.Entrar` com o id selecionado (fluxo em [personagem.md](features/personagem.md#fluxo-de-entrada-no-jogo)).

## Faixas de IDs de mensagem

Cada funcionalidade recebe uma faixa de 1000 IDs, declarada em `shared/Package/Runtime/Features/<X>/<X>MessageIds.cs` com um `MessageIdRange`. A tabela com todas as faixas fica no [README da raiz](../README.md#funcionalidades). Uma funcionalidade nova pega a próxima faixa livre.

## Decisões em aberto

- ~~Biblioteca de rede para o **tempo real** (ex.: LiteNetLib).~~ Decidido em 2026-10-11: LiteNetLib por UDP ([ADR 0015](decisoes/0015-litenetlib-para-o-tempo-real.md)). Conta e personagens já usam HTTP ([ADR 0009](decisoes/0009-api-http-para-conta-e-personagens.md)).
- ~~Banco de dados e persistência~~ decidido: EF Core com SQLite no desenvolvimento e PostgreSQL depois ([ADR 0010](decisoes/0010-banco-de-dados.md)).
- ~~Como hospedar as sessões de grupo pequeno: um processo de servidor por sessão ou várias sessões por processo? Onde fica o hub social?~~ Substituído em 2026-10-11: o formato voltou a ser mundo persistente (D1 opção A da [validação](brainstorm/validacao.md)).
- ~~Como dividir o mundo persistente entre servidores~~ Decidido em 2026-10-16: **canais** no mesmo processo (80 jogadores/canal, entrada pelo grupo ou canal menos cheio, troca fora de combate). Detalhe em [ADR 0045](decisoes/0045-canais-do-mundo-persistente.md); implementação [#188](https://github.com/GabrielNied/jogo_do_grupo/issues/188). Sharding geográfico em várias máquinas fica para depois.
- Onde hospedar o servidor sem custo: proposta no [ADR 0021](decisoes/0021-hospedagem-gratuita.md) (PC do grupo agora, Oracle Cloud em São Paulo depois).
- Sincronização do combate de ação: como o servidor autoritativo valida acertos com latência (D4).
- ~~Testes automatizados~~ decidido: xUnit no shared e no servidor, com testes de integração da API ([ADR 0013](decisoes/0013-testes-automatizados.md)).

Cada uma vira um ADR em [decisoes/](decisoes/) quando for decidida.
