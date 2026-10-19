using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>Regras da troca direta (#175) sem banco: executor e mundo são falsos.</summary>
public sealed class TrocaDiretaDoServidorTestes
{
    private const long Ana = 1;
    private const long Beto = 2;

    private sealed class Saida : ISaidaDeTroca
    {
        public readonly List<(long Para, ConviteDeTroca Msg)> Convites = [];
        public readonly List<(long Para, EstadoDaTroca Msg)> Estados = [];
        public readonly List<(long Para, BolsaParaTroca Msg)> Bolsas = [];
        public readonly List<(long Para, TrocaEncerrada Msg)> Encerradas = [];
        public readonly List<(long Para, TrocaRecusada Msg)> Recusas = [];

        public void Convite(long p, ConviteDeTroca m) => Convites.Add((p, m));

        public void Estado(long p, EstadoDaTroca m) => Estados.Add((p, m));

        public void Bolsa(long p, BolsaParaTroca m) => Bolsas.Add((p, m));

        public void Encerrada(long p, TrocaEncerrada m) => Encerradas.Add((p, m));

        public void Recusada(long p, TrocaRecusada m) => Recusas.Add((p, m));
    }

    private sealed class ExecutorFalso : IExecutorDaTroca
    {
        public string? ErroDaOferta;
        public ResultadoDaTroca Resultado = ResultadoDaTroca.Ok;
        public int Execucoes;
        public (long A, OfertaPedida OfertaA, long B, OfertaPedida OfertaB)? Ultima;

        public Task<(string? Erro, OfertaDeTroca? Oferta)> ValidarOfertaAsync(
            long personagemId,
            OfertaPedida oferta,
            CancellationToken c
        ) =>
            Task.FromResult<(string?, OfertaDeTroca?)>(
                ErroDaOferta != null
                    ? (ErroDaOferta, null)
                    : (
                        null,
                        new OfertaDeTroca
                        {
                            Coroas = oferta.Coroas,
                            Itens = oferta
                                .InstanciaIds.Select(i => new EntradaInventario
                                {
                                    InstanciaId = i,
                                    Nome = $"item{i}",
                                })
                                .ToArray(),
                        }
                    )
            );

        public Task<BolsaParaTroca> BolsaAsync(
            long personagemId,
            long trocaId,
            CancellationToken c
        ) => Task.FromResult(new BolsaParaTroca { TrocaId = trocaId });

        public Task<ResultadoDaTroca> ExecutarAsync(
            long personagemA,
            OfertaPedida ofertaA,
            long personagemB,
            OfertaPedida ofertaB,
            CancellationToken c
        )
        {
            Execucoes++;
            Ultima = (personagemA, ofertaA, personagemB, ofertaB);
            return Task.FromResult(Resultado);
        }
    }

    private sealed class DiretorioFalso : IDiretorioDePersonagens
    {
        public bool TentarObterPorNome(string nome, out long id)
        {
            id = nome.ToLowerInvariant() switch
            {
                "ana" => Ana,
                "beto" => Beto,
                _ => 0,
            };
            return id != 0;
        }

        public bool TentarObterNome(long id, out string nome)
        {
            nome =
                id == Ana ? "Ana"
                : id == Beto ? "Beto"
                : "";
            return nome != "";
        }

        public bool TentarObterContaDoPersonagem(long id, out long conta)
        {
            conta = id;
            return true;
        }

        public bool TentarObterRaca(long id, out int raca)
        {
            raca = 0;
            return true;
        }
    }

    private sealed class PosicoesFalsas : IPosicoesDoMundo
    {
        public readonly Dictionary<long, PosicaoNoMundo> Posicoes = new()
        {
            [Ana] = new PosicaoNoMundo("cidade", 0, 0, 0, 0),
            [Beto] = new PosicaoNoMundo("cidade", 3, 0, 4, 0),
        };

        public bool TentarObter(long id, out PosicaoNoMundo posicao) =>
            Posicoes.TryGetValue(id, out posicao);

        public IReadOnlyList<long> PersonagensNaArea(string area) => [];
    }

    private sealed class CombateFalso : IEstadoDeCombate
    {
        public readonly HashSet<long> EmCombateAgora = [];

        public bool EmCombate(long id) => EmCombateAgora.Contains(id);
    }

    private sealed class Ambiente
    {
        public readonly Saida Saida = new();
        public readonly ExecutorFalso Executor = new();
        public readonly PosicoesFalsas Posicoes = new();
        public readonly CombateFalso Combate = new();
        public readonly HashSet<long> Online = [Ana, Beto];
        public readonly List<long> CoroasMovimentadas = [];
        public long AgoraMs = 1000;
        public readonly TrocaDiretaDoServidor Servico;

        public Ambiente()
        {
            Servico = new TrocaDiretaDoServidor(
                new DiretorioFalso(),
                Saida,
                Executor,
                id => Online.Contains(id),
                () => Posicoes,
                () => Combate,
                (a, b) => false,
                () => AgoraMs,
                id => CoroasMovimentadas.Add(id)
            );
        }

        /// <summary>Ana convida Beto, ele aceita: a troca fica aberta. Devolve o id.</summary>
        public async Task<long> AbrirAsync()
        {
            await Servico.PedirAsync(Ana, "Beto");
            var id = Saida.Convites.Single().Msg.TrocaId;
            await Servico.ResponderAsync(Beto, id, aceitar: true);
            return id;
        }
    }

    [Fact]
    public async Task Pedir_AlvoOffline_Recusa()
    {
        var a = new Ambiente();
        a.Online.Remove(Beto);

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(CodigosErroTroca.AlvoNaoEncontrado, a.Saida.Recusas.Single().Msg.Codigo);
        Assert.False(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task Pedir_ParaSiMesmo_Recusa()
    {
        var a = new Ambiente();

        await a.Servico.PedirAsync(Ana, "ana");

        Assert.Equal(CodigosErroTroca.TrocaComSiMesmo, a.Saida.Recusas.Single().Msg.Codigo);
    }

    [Fact]
    public async Task Pedir_AMaisDe10Metros_Recusa()
    {
        var a = new Ambiente();
        a.Posicoes.Posicoes[Beto] = new PosicaoNoMundo("cidade", 10.5f, 0, 0, 0);

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(CodigosErroTroca.LongeDemais, a.Saida.Recusas.Single().Msg.Codigo);
        Assert.Empty(a.Saida.Convites);
    }

    [Fact]
    public async Task Pedir_ExatamenteA10Metros_Aceita()
    {
        var a = new Ambiente();
        a.Posicoes.Posicoes[Beto] = new PosicaoNoMundo("cidade", 10f, 0, 0, 0);

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Single(a.Saida.Convites);
    }

    [Fact]
    public async Task Pedir_EmAreasDiferentes_Recusa()
    {
        var a = new Ambiente();
        a.Posicoes.Posicoes[Beto] = new PosicaoNoMundo("dungeon", 1, 0, 0, 0);

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(CodigosErroTroca.LongeDemais, a.Saida.Recusas.Single().Msg.Codigo);
    }

    [Theory]
    [InlineData(Ana)]
    [InlineData(Beto)]
    public async Task Pedir_ComUmDosDoisEmCombate_Recusa(long emCombate)
    {
        var a = new Ambiente();
        a.Combate.EmCombateAgora.Add(emCombate);

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(CodigosErroTroca.EmCombate, a.Saida.Recusas.Single().Msg.Codigo);
    }

    [Fact]
    public async Task Pedir_QuemJaEstaEmTroca_Recusa()
    {
        var a = new Ambiente();
        await a.AbrirAsync();
        a.Saida.Recusas.Clear();

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(CodigosErroTroca.VoceJaEstaEmTroca, a.Saida.Recusas.Single().Msg.Codigo);
    }

    [Fact]
    public async Task Convite_AceitoAbreATrocaParaOsDois()
    {
        var a = new Ambiente();

        var id = await a.AbrirAsync();

        Assert.Equal("Ana", a.Saida.Convites.Single().Msg.NomeDe);
        Assert.Equal(2, a.Saida.Estados.Count);
        Assert.Equal("Beto", a.Saida.Estados.Single(e => e.Para == Ana).Msg.NomeDoOutro);
        Assert.Equal("Ana", a.Saida.Estados.Single(e => e.Para == Beto).Msg.NomeDoOutro);
        Assert.Equal(2, a.Saida.Bolsas.Count);
        Assert.All(a.Saida.Bolsas, b => Assert.Equal(id, b.Msg.TrocaId));
    }

    [Fact]
    public async Task Convite_Recusado_AvisaQuemConvidouELibera()
    {
        var a = new Ambiente();
        await a.Servico.PedirAsync(Ana, "Beto");
        var id = a.Saida.Convites.Single().Msg.TrocaId;

        await a.Servico.ResponderAsync(Beto, id, aceitar: false);

        var encerrada = a.Saida.Encerradas.Single();
        Assert.Equal(Ana, encerrada.Para);
        Assert.Equal(CodigosErroTroca.ConviteRecusado, encerrada.Msg.Motivo);
        Assert.False(a.Servico.EstaEmTroca(Ana));
        Assert.False(a.Servico.EstaEmTroca(Beto));
    }

    [Fact]
    public async Task Convite_Vencido_NaoAbreELibera()
    {
        var a = new Ambiente();
        await a.Servico.PedirAsync(Ana, "Beto");
        var id = a.Saida.Convites.Single().Msg.TrocaId;
        a.AgoraMs += (RegrasTroca.ValidadeDoConviteSegundos + 1) * 1000L;

        await a.Servico.ResponderAsync(Beto, id, aceitar: true);

        Assert.Equal(CodigosErroTroca.ConviteExpirado, a.Saida.Recusas.Single().Msg.Codigo);
        Assert.False(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task Convite_Vencido_NaoPrendeNovoPedido()
    {
        var a = new Ambiente();
        await a.Servico.PedirAsync(Ana, "Beto");
        a.AgoraMs += (RegrasTroca.ValidadeDoConviteSegundos + 1) * 1000L;

        await a.Servico.PedirAsync(Ana, "Beto");

        Assert.Equal(2, a.Saida.Convites.Count);
    }

    [Fact]
    public async Task MudancaDeOferta_DesfazOAceiteDosDoisLados()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 10, [100]);
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.ConfirmarAsync(Ana, id, versao);
        Assert.True(a.Saida.Estados.Last(e => e.Para == Ana).Msg.MeuAceite);
        Assert.True(a.Saida.Estados.Last(e => e.Para == Beto).Msg.AceiteDele);

        await a.Servico.AtualizarOfertaAsync(Beto, id, 0, [200]);

        var ultimaAna = a.Saida.Estados.Last(e => e.Para == Ana).Msg;
        var ultimaBeto = a.Saida.Estados.Last(e => e.Para == Beto).Msg;
        Assert.False(ultimaAna.MeuAceite);
        Assert.False(ultimaAna.AceiteDele);
        Assert.False(ultimaBeto.MeuAceite);
        Assert.False(ultimaBeto.AceiteDele);
        Assert.Equal(0, a.Executor.Execucoes);
    }

    [Fact]
    public async Task Aceite_DeUmaVersaoAntiga_EhIgnorado()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 10, []);
        var versaoVelha = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.AtualizarOfertaAsync(Ana, id, 20, []);
        await a.Servico.ConfirmarAsync(Ana, id, versaoVelha);
        await a.Servico.ConfirmarAsync(Beto, id, versaoVelha);

        Assert.Equal(0, a.Executor.Execucoes);
        Assert.False(a.Saida.Estados.Last(e => e.Para == Ana).Msg.AceiteDele);
        Assert.True(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task DoisAceites_ExecutamUmaVezCom_AsOfertasCertas_EEncerram()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 30, [100, 101]);
        await a.Servico.AtualizarOfertaAsync(Beto, id, 5, [200]);
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;

        await a.Servico.ConfirmarAsync(Ana, id, versao);
        Assert.Equal(0, a.Executor.Execucoes);
        await a.Servico.ConfirmarAsync(Beto, id, versao);

        Assert.Equal(1, a.Executor.Execucoes);
        var (pa, ofertaA, pb, ofertaB) = a.Executor.Ultima!.Value;
        Assert.Equal((Ana, Beto), (pa, pb));
        Assert.Equal(30, ofertaA.Coroas);
        Assert.Equal([100L, 101L], ofertaA.InstanciaIds);
        Assert.Equal(5, ofertaB.Coroas);
        Assert.Equal([200L], ofertaB.InstanciaIds);
        Assert.All(a.Saida.Encerradas, e => Assert.Equal(CodigosErroTroca.Concluida, e.Msg.Motivo));
        Assert.Equal(2, a.Saida.Encerradas.Count);
        Assert.Equal([Ana, Beto], a.CoroasMovimentadas.OrderBy(x => x));
        Assert.False(a.Servico.EstaEmTroca(Ana));
        Assert.False(a.Servico.EstaEmTroca(Beto));
    }

    [Fact]
    public async Task FalhaNaExecucao_ReabreATrocaSemAceites_ENaoAvisaConclusao()
    {
        var a = new Ambiente();
        a.Executor.Resultado = new ResultadoDaTroca(CodigosErroTroca.BolsaCheia, Beto);
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 30, [100]);
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.ConfirmarAsync(Ana, id, versao);
        await a.Servico.ConfirmarAsync(Beto, id, versao);

        Assert.Equal(1, a.Executor.Execucoes);
        Assert.Empty(a.Saida.Encerradas);
        Assert.Empty(a.CoroasMovimentadas);
        Assert.Equal(2, a.Saida.Recusas.Count(r => r.Msg.Codigo == CodigosErroTroca.BolsaCheia));
        var estado = a.Saida.Estados.Last(e => e.Para == Ana).Msg;
        Assert.False(estado.MeuAceite);
        Assert.False(estado.AceiteDele);
        Assert.True(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task OfertaInvalidaOuSemSaldo_EhRecusadaESemMudarAOfertaAtual()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 10, []);
        var estadosAntes = a.Saida.Estados.Count;
        a.Executor.ErroDaOferta = CodigosErroTroca.SaldoInsuficiente;

        await a.Servico.AtualizarOfertaAsync(Ana, id, 999, []);
        await a.Servico.AtualizarOfertaAsync(Ana, id, -1, []);
        a.Executor.ErroDaOferta = null;
        await a.Servico.AtualizarOfertaAsync(Ana, id, 0, [5, 5]);

        Assert.Equal(
            [
                CodigosErroTroca.SaldoInsuficiente,
                CodigosErroTroca.OfertaInvalida,
                CodigosErroTroca.OfertaInvalida,
            ],
            a.Saida.Recusas.Select(r => r.Msg.Codigo)
        );
        Assert.Equal(estadosAntes, a.Saida.Estados.Count);
    }

    [Fact]
    public async Task OfertaComMaisDe8Itens_EhInvalida()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();

        await a.Servico.AtualizarOfertaAsync(
            Ana,
            id,
            0,
            Enumerable.Range(1, RegrasTroca.ItensMaximosPorLado + 1).Select(i => (long)i).ToArray()
        );

        Assert.Equal(CodigosErroTroca.OfertaInvalida, a.Saida.Recusas.Single().Msg.Codigo);
    }

    [Fact]
    public async Task Desconectar_CancelaSemExecutarEAvisaOOutro()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 30, [100]);
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.ConfirmarAsync(Ana, id, versao);

        await a.Servico.AoDesconectarAsync(Ana);

        var encerrada = Assert.Single(a.Saida.Encerradas);
        Assert.Equal(Beto, encerrada.Para);
        Assert.Equal(CodigosErroTroca.Desconectou, encerrada.Msg.Motivo);
        Assert.Equal(0, a.Executor.Execucoes);
        Assert.Empty(a.CoroasMovimentadas);
        Assert.False(a.Servico.EstaEmTroca(Beto));
    }

    [Fact]
    public async Task Desconectar_ComConviteNaoRespondido_LiberaOConvidado()
    {
        var a = new Ambiente();
        await a.Servico.PedirAsync(Ana, "Beto");

        await a.Servico.AoDesconectarAsync(Ana);

        Assert.False(a.Servico.EstaEmTroca(Beto));
    }

    [Fact]
    public async Task Cancelar_AvisaOsDoisComMotivosDiferentes()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();

        await a.Servico.CancelarAsync(Beto, id);

        Assert.Equal(
            CodigosErroTroca.CanceladaPorVoce,
            a.Saida.Encerradas.Single(e => e.Para == Beto).Msg.Motivo
        );
        Assert.Equal(
            CodigosErroTroca.CanceladaPeloOutro,
            a.Saida.Encerradas.Single(e => e.Para == Ana).Msg.Motivo
        );
        Assert.False(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task Cancelar_ComIdZero_CancelaOConviteQueEuMandei()
    {
        var a = new Ambiente();
        await a.Servico.PedirAsync(Ana, "Beto");

        await a.Servico.CancelarAsync(Ana, 0);

        Assert.False(a.Servico.EstaEmTroca(Beto));
    }

    [Fact]
    public async Task EntrarEmCombateAntesDoUltimoAceite_CancelaSemExecutar()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        await a.Servico.AtualizarOfertaAsync(Ana, id, 30, []);
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.ConfirmarAsync(Ana, id, versao);
        a.Combate.EmCombateAgora.Add(Beto);

        await a.Servico.ConfirmarAsync(Beto, id, versao);

        Assert.Equal(0, a.Executor.Execucoes);
        Assert.All(a.Saida.Encerradas, e => Assert.Equal(CodigosErroTroca.EmCombate, e.Msg.Motivo));
        Assert.Equal(2, a.Saida.Encerradas.Count);
        Assert.False(a.Servico.EstaEmTroca(Ana));
    }

    [Fact]
    public async Task AfastarseAntesDoUltimoAceite_CancelaSemExecutar()
    {
        var a = new Ambiente();
        var id = await a.AbrirAsync();
        var versao = a.Saida.Estados.Last().Msg.VersaoDaOferta;
        await a.Servico.ConfirmarAsync(Ana, id, versao);
        a.Posicoes.Posicoes[Beto] = new PosicaoNoMundo("cidade", 50, 0, 0, 0);

        await a.Servico.ConfirmarAsync(Beto, id, versao);

        Assert.Equal(0, a.Executor.Execucoes);
        Assert.All(
            a.Saida.Encerradas,
            e => Assert.Equal(CodigosErroTroca.LongeDemais, e.Msg.Motivo)
        );
    }

    [Fact]
    public void Regras_Oferta_ValidaLimitesERepeticao()
    {
        Assert.True(RegrasTroca.OfertaValida(0, []));
        Assert.True(RegrasTroca.OfertaValida(5, [1, 2, 3]));
        Assert.False(RegrasTroca.OfertaValida(-1, []));
        Assert.False(RegrasTroca.OfertaValida(0, [1, 1]));
        Assert.False(RegrasTroca.OfertaValida(0, null!));
    }
}
