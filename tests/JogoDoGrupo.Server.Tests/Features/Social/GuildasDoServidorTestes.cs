using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class GuildasDoServidorTestes
{
    private const long Ana = 1;
    private const long Bia = 2;
    private const long Caio = 3;
    private const long Davi = 4;

    private readonly DiretorioDoChatFalso _diretorio = new();
    private readonly SaidaDaGuildaFalsa _saida = new();
    private readonly List<long> _coroasMovimentadas = [];
    private readonly Dictionary<long, long> _saldos = [];
    private readonly GuildasDoServidor _guildas;

    public GuildasDoServidorTestes()
    {
        _diretorio.Adicionar(Ana, "Ana");
        _diretorio.Adicionar(Bia, "Bia");
        _diretorio.Adicionar(Caio, "Caio");
        _diretorio.Adicionar(Davi, "Davi");

        _saldos[Ana] = 5000;
        _saldos[Bia] = 2000;
        _saldos[Caio] = 500;
        _saldos[Davi] = 1000;

        _guildas = new GuildasDoServidor(
            _diretorio,
            _saida,
            (personagemId, valor) =>
            {
                if (_saldos.TryGetValue(personagemId, out var saldo) && saldo >= valor)
                {
                    _saldos[personagemId] = saldo - valor;
                    return Task.FromResult(true);
                }
                return Task.FromResult(false);
            },
            id => _coroasMovimentadas.Add(id)
        );

        _guildas.AoConectar(Ana);
        _guildas.AoConectar(Bia);
        _guildas.AoConectar(Caio);
        _guildas.AoConectar(Davi);
    }

    [Fact]
    public async Task Criar_guilda_valida_debita_coroas_e_define_lider()
    {
        await _guildas.CriarAsync(Ana, "Os Guardioes");

        Assert.Equal(4000, _saldos[Ana]);
        Assert.Contains(Ana, _coroasMovimentadas);
        Assert.Empty(_saida.RecusasEnviadas);

        var dados = Assert.Single(_saida.DadosEnviados, d => d.PersonagemId == Ana).Dados;
        Assert.Equal("Os Guardioes", dados.Nome);
        Assert.Equal(CargoDaGuilda.Lider, dados.MeuCargo);
        var membro = Assert.Single(dados.Membros);
        Assert.Equal(Ana, membro.PersonagemId);
        Assert.Equal(CargoDaGuilda.Lider, membro.Cargo);
    }

    [Fact]
    public async Task Criar_guilda_com_nome_invalido_recusa()
    {
        await _guildas.CriarAsync(Ana, "X");

        var recusa = Assert.Single(_saida.RecusasEnviadas);
        Assert.Equal(Ana, recusa.PersonagemId);
        Assert.Equal(CodigosErroSocial.NomeDeGuildaInvalido, recusa.Codigo);
        Assert.Equal(5000, _saldos[Ana]);
    }

    [Fact]
    public async Task Criar_guilda_sem_saldo_recusa()
    {
        await _guildas.CriarAsync(Caio, "Pioneiros");

        var recusa = Assert.Single(_saida.RecusasEnviadas);
        Assert.Equal(Caio, recusa.PersonagemId);
        Assert.Equal(CodigosErroSocial.CoroasInsuficientes, recusa.Codigo);
    }

    [Fact]
    public async Task Criar_guilda_com_nome_duplicado_ignora_maiusculas_e_recusa()
    {
        await _guildas.CriarAsync(Ana, "Lendas do Sul");
        await _guildas.CriarAsync(Bia, "lendas do sul");

        var recusa = Assert.Single(_saida.RecusasEnviadas);
        Assert.Equal(Bia, recusa.PersonagemId);
        Assert.Equal(CodigosErroSocial.NomeDeGuildaEmUso, recusa.Codigo);
    }

    [Fact]
    public async Task Fluxo_de_convite_e_aceite_adiciona_recruta()
    {
        await _guildas.CriarAsync(Ana, "Valentes");
        _saida.DadosEnviados.Clear();

        _guildas.Convidar(Ana, "Bia");

        var convite = Assert.Single(_saida.ConvitesEnviados);
        Assert.Equal(Bia, convite.PersonagemId);
        Assert.Equal("Valentes", convite.Convite.NomeDaGuilda);

        _guildas.ResponderConvite(Bia, convite.Convite.GuildaId, aceitar: true);

        // Ambas recebem a atualização dos dados da guilda
        var dadosAna = _saida.DadosEnviados.Last(d => d.PersonagemId == Ana).Dados;
        var dadosBia = _saida.DadosEnviados.Last(d => d.PersonagemId == Bia).Dados;

        Assert.Equal(2, dadosAna.Membros.Length);
        Assert.Equal(CargoDaGuilda.Lider, dadosAna.MeuCargo);
        Assert.Equal(CargoDaGuilda.Recruta, dadosBia.MeuCargo);
    }

    [Fact]
    public async Task Convidar_sem_permissao_recusa()
    {
        await _guildas.CriarAsync(Ana, "Guerreiros");
        _guildas.Convidar(Ana, "Bia");
        var guildaId = _saida.ConvitesEnviados.Single().Convite.GuildaId;
        _guildas.ResponderConvite(Bia, guildaId, aceitar: true);
        _saida.RecusasEnviadas.Clear();

        // Bia é Recruta e não pode convidar
        _guildas.Convidar(Bia, "Caio");

        var recusa = Assert.Single(_saida.RecusasEnviadas);
        Assert.Equal(Bia, recusa.PersonagemId);
        Assert.Equal(CodigosErroSocial.SemPermissaoNaGuilda, recusa.Codigo);
    }

    [Fact]
    public async Task Expulsar_membro_remove_da_guilda()
    {
        await _guildas.CriarAsync(Ana, "Exploradores");
        _guildas.Convidar(Ana, "Bia");
        var guildaId = _saida.ConvitesEnviados.Single().Convite.GuildaId;
        _guildas.ResponderConvite(Bia, guildaId, aceitar: true);

        _guildas.Expulsar(Ana, Bia);

        var dadosBia = _saida.DadosEnviados.Last(d => d.PersonagemId == Bia).Dados;
        Assert.Equal(0, dadosBia.GuildaId);
        Assert.Empty(dadosBia.Membros);

        var dadosAna = _saida.DadosEnviados.Last(d => d.PersonagemId == Ana).Dados;
        Assert.Single(dadosAna.Membros);
    }

    [Fact]
    public async Task Alterar_cargo_e_passar_lideranca()
    {
        await _guildas.CriarAsync(Ana, "Ordem Solar");
        _guildas.Convidar(Ana, "Bia");
        var guildaId = _saida.ConvitesEnviados.Single().Convite.GuildaId;
        _guildas.ResponderConvite(Bia, guildaId, aceitar: true);

        // Promover Bia a Oficial
        _guildas.AlterarCargo(Ana, Bia, CargoDaGuilda.Oficial);
        var dadosBia = _saida.DadosEnviados.Last(d => d.PersonagemId == Bia).Dados;
        Assert.Equal(CargoDaGuilda.Oficial, dadosBia.MeuCargo);

        // Passar liderança para Bia
        _guildas.PassarLideranca(Ana, Bia);
        dadosBia = _saida.DadosEnviados.Last(d => d.PersonagemId == Bia).Dados;
        var dadosAna = _saida.DadosEnviados.Last(d => d.PersonagemId == Ana).Dados;

        Assert.Equal(CargoDaGuilda.Lider, dadosBia.MeuCargo);
        Assert.Equal(CargoDaGuilda.Oficial, dadosAna.MeuCargo);
    }

    [Fact]
    public async Task Lider_nao_pode_sair_com_outros_membros_sem_passar_lideranca()
    {
        await _guildas.CriarAsync(Ana, "Sentinelas");
        _guildas.Convidar(Ana, "Bia");
        var guildaId = _saida.ConvitesEnviados.Single().Convite.GuildaId;
        _guildas.ResponderConvite(Bia, guildaId, aceitar: true);
        _saida.RecusasEnviadas.Clear();

        _guildas.Sair(Ana);

        var recusa = Assert.Single(_saida.RecusasEnviadas);
        Assert.Equal(Ana, recusa.PersonagemId);
        Assert.Equal(CodigosErroSocial.LiderNaoPodeSair, recusa.Codigo);
    }

    [Fact]
    public async Task Ultimo_membro_sair_dissolve_guilda()
    {
        await _guildas.CriarAsync(Ana, "Eremitas");
        _guildas.Sair(Ana);

        var dados = _saida.DadosEnviados.Last(d => d.PersonagemId == Ana).Dados;
        Assert.Equal(0, dados.GuildaId);
        Assert.Equal(0, _guildas.ObterGuildaId(Ana));
    }
}

public sealed class SaidaDaGuildaFalsa : ISaidaDaGuilda
{
    public readonly List<(long PersonagemId, DadosDaGuilda Dados)> DadosEnviados = [];
    public readonly List<(long PersonagemId, ConviteDeGuildaRecebido Convite)> ConvitesEnviados =
    [];
    public readonly List<(long PersonagemId, string Codigo)> RecusasEnviadas = [];

    public void Dados(long personagemId, DadosDaGuilda dados) =>
        DadosEnviados.Add((personagemId, dados));

    public void Convite(long personagemId, ConviteDeGuildaRecebido convite) =>
        ConvitesEnviados.Add((personagemId, convite));

    public void Recusada(long personagemId, string codigo) =>
        RecusasEnviadas.Add((personagemId, codigo));
}
