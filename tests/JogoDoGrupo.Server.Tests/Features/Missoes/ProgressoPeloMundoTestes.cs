using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Npcs;
using JogoDoGrupo.Server.Contratos.Profissoes;
using JogoDoGrupo.Server.Core.Eventos;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// Os objetivos que dependem do mundo contam pelo servidor, nunca por aviso do client: a
/// conversa aceita pelos NPCs (Falar), a posição dentro do raio (IrA) e a coleta nos pontos
/// das Profissões (Coletar). Client UDP de verdade e banco SQLite, como no loot.
/// </summary>
public sealed class ProgressoPeloMundoTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private sealed class Jogador(HttpClient http, ClienteTempoRealDeTeste cliente, long id)
        : IDisposable
    {
        public ClienteTempoRealDeTeste Cliente { get; } = cliente;
        public long Id { get; } = id;

        public void Dispose()
        {
            Cliente.Dispose();
            http.Dispose();
        }
    }

    private async Task<Jogador> NovoJogadorAsync()
    {
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var personagem = await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta);
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            servidor.Services.GetRequiredService<ServidorTempoReal>().Porta,
            http.DefaultRequestHeaders.Authorization!.Parameter!,
            personagem.Id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);
        // A primeira etapa da história chega já aceita ao autenticar.
        await cliente.ReceberAsync<EstadoDasMissoes>(MissoesMessageIds.EstadoDasMissoes);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return new Jogador(http, cliente, personagem.Id);
    }

    private void Teleportar(Jogador jogador, float x, float z)
    {
        var (aceito, texto) = servidor
            .Services.GetRequiredService<ITeleporteDeTeste>()
            .Teleportar(jogador.Id, x, MapaDoMundo.AlturaEm(x, z), z);
        Assert.True(aceito, texto);
    }

    // Põe a missão em andamento direto no banco (as secundárias pedem a história concluída) e
    // pede o estado, como o client faz ao abrir o registro.
    private async Task ComMissaoEmAndamentoAsync(Jogador jogador, int missaoId)
    {
        var definicao = CatalogoDeMissoes.Obter(missaoId)!;
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            db.Set<MissaoPersonagemEntidade>()
                .Add(
                    new MissaoPersonagemEntidade
                    {
                        PersonagemId = jogador.Id,
                        MissaoId = missaoId,
                        Estado = EstadoDaMissao.EmAndamento,
                        ObjetivosJson = System.Text.Json.JsonSerializer.Serialize(
                            definicao
                                .Objetivos.Select(o => new
                                {
                                    ObjetivoId = o.Id,
                                    QuantidadeAtual = 0,
                                    Concluido = false,
                                })
                                .ToArray()
                        ),
                        AceitaEm = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    }
                );
            await db.SaveChangesAsync();
        }

        jogador.Cliente.Enviar(MissoesMessageIds.ConsultarMissoes, new ConsultarMissoes());
        await jogador.Cliente.ReceberAsync<EstadoDasMissoes>(MissoesMessageIds.EstadoDasMissoes);
    }

    private async Task<ProgressoDeMissaoDto> AtualizacaoDeAsync(Jogador jogador, int missaoId)
    {
        while (true)
        {
            var atualizada = await jogador.Cliente.ReceberAsync<MissaoAtualizada>(
                MissoesMessageIds.MissaoAtualizada
            );
            if (atualizada.Missao.MissaoId == missaoId)
                return atualizada.Missao;
        }
    }

    private async Task<ProgressoDeMissaoDto?> NoBancoAsync(Jogador jogador, int missaoId)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        var estado = await escopo
            .ServiceProvider.GetRequiredService<ServicoDeMissoes>()
            .ObterEstadoDasMissoesAsync(jogador.Id);
        return estado.Ativas.FirstOrDefault(m => m.MissaoId == missaoId);
    }

    [Fact]
    public async Task ConversaPertoDoNpc_PublicaEDeixaAPrimeiraEtapaProntaParaEntregar()
    {
        using var jogador = await NovoJogadorAsync();
        var conversas = new List<JogadorFalouComNpc>();
        servidor
            .Services.GetRequiredService<EventosDoServidor>()
            .Assinar<JogadorFalouComNpc>(evento =>
            {
                lock (conversas)
                    conversas.Add(evento);
            });
        var alden = CatalogoNpcs.Obter(CatalogoNpcs.IdVendedor)!;
        Teleportar(jogador, alden.X + 1f, alden.Z);

        jogador.Cliente.Enviar(
            NpcsMessageIds.FalarComNpc,
            new FalarComNpc(CatalogoNpcs.IdVendedor)
        );

        await jogador.Cliente.ReceberAsync<DialogoDoNpc>(NpcsMessageIds.DialogoDoNpc);
        var missao = await AtualizacaoDeAsync(
            jogador,
            CatalogoDeMissoes.Missao1PortoesDaBrasaMansa
        );
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, missao.Estado);
        lock (conversas)
            Assert.Contains(conversas, c => c.PersonagemId == jogador.Id && c.NpcId == alden.Id);
    }

    [Fact]
    public async Task ConversaDeLonge_ERecusadaENaoConta()
    {
        using var jogador = await NovoJogadorAsync();
        var alden = CatalogoNpcs.Obter(CatalogoNpcs.IdVendedor)!;
        Teleportar(jogador, alden.X + 15f, alden.Z);

        jogador.Cliente.Enviar(
            NpcsMessageIds.FalarComNpc,
            new FalarComNpc(CatalogoNpcs.IdVendedor)
        );

        var recusa = await jogador.Cliente.ReceberAsync<DialogoRecusado>(
            NpcsMessageIds.DialogoRecusado
        );
        Assert.Equal(MotivoRecusaDialogo.LongeDemais, recusa.Motivo);
        var missao = await NoBancoAsync(jogador, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa);
        Assert.NotNull(missao);
        Assert.Equal(EstadoDaMissao.EmAndamento, missao.Estado);
        Assert.False(missao.Objetivos[0].Concluido);
    }

    [Fact]
    public async Task ChegarNoRaioDoObjetivoIrA_ConcluiPelaPosicaoDoServidor()
    {
        using var jogador = await NovoJogadorAsync();
        const int missaoId = CatalogoDeMissoes.Missao104CorrespondenciaExtraviada;
        await ComMissaoEmAndamentoAsync(jogador, missaoId);
        var objetivo = CatalogoDeMissoes.Obter(missaoId)!.Objetivos[0];
        Assert.Equal(TipoDeObjetivo.IrA, objetivo.Tipo);

        // Fora do raio, o laço não conta nada.
        Teleportar(
            jogador,
            objetivo.CoordenadaX + objetivo.RaioAreaMetros + 10f,
            objetivo.CoordenadaZ
        );
        await Task.Delay(MissoesModule.IntervaloDasChegadas * 4);
        Assert.Equal(EstadoDaMissao.EmAndamento, (await NoBancoAsync(jogador, missaoId))!.Estado);

        Teleportar(jogador, objetivo.CoordenadaX + 1f, objetivo.CoordenadaZ);

        var missao = await AtualizacaoDeAsync(jogador, missaoId);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, missao.Estado);
        Assert.True(missao.Objetivos[0].Concluido);
    }

    [Fact]
    public async Task MaterialColetado_ContaNoObjetivoColetarPeloMaterial()
    {
        using var jogador = await NovoJogadorAsync();
        const int missaoId = CatalogoDeMissoes.Missao101CaldeiraoDaLareira;
        await ComMissaoEmAndamentoAsync(jogador, missaoId);
        var eventos = servidor.Services.GetRequiredService<EventosDoServidor>();

        // Outro material não conta.
        eventos.Publicar(new MaterialColetado(jogador.Id, ConfigProfissoes.MaterialMadeira, 2));
        eventos.Publicar(new MaterialColetado(jogador.Id, ConfigProfissoes.MaterialCarneDeCaca, 2));
        var parcial = await AtualizacaoDeAsync(jogador, missaoId);
        Assert.Equal(2, parcial.Objetivos[0].QuantidadeAtual);
        Assert.Equal(EstadoDaMissao.EmAndamento, parcial.Estado);

        eventos.Publicar(new MaterialColetado(jogador.Id, ConfigProfissoes.MaterialCarneDeCaca, 2));
        var completa = await AtualizacaoDeAsync(jogador, missaoId);
        Assert.Equal(4, completa.Objetivos[0].QuantidadeAtual);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, completa.Estado);
    }
}
