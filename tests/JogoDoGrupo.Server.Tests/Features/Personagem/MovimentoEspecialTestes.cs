using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Personagem;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

/// <summary>Natação, dano de queda e escalada no servidor (#349), com posições aceitas falsas.</summary>
public sealed class MovimentoEspecialTestes
{
    private const long Id = 7;

    // Piscina marcada: fundo em -1, superfície em 5 (6 m de água).
    private static readonly VolumeMarcado PiscinaFunda = new(
        "piscina-funda",
        TipoDeVolume.Agua,
        0f,
        2f,
        0f,
        20f,
        6f,
        20f
    );

    // Poça rasa para nadar (1,5 m), mas que não amortece queda: fundo em -1, superfície em 0,5.
    private static readonly VolumeMarcado PocaRasa = new(
        "poca-rasa",
        TipoDeVolume.Agua,
        0f,
        -0.25f,
        0f,
        20f,
        1.5f,
        20f
    );

    // Via de escalada: parede em +Z (frente do volume), de y = 0 a 10.
    private static readonly VolumeMarcado Via = new(
        "via-1",
        TipoDeVolume.ViaDeEscalada,
        30f,
        5f,
        0f,
        4f,
        10f,
        2f
    );

    // Via que atravessa a piscina: dá para estar nela e nadando ao mesmo tempo.
    private static readonly VolumeMarcado ViaNaAgua = new(
        "via-na-agua",
        TipoDeVolume.ViaDeEscalada,
        0f,
        8f,
        0f,
        4f,
        10f,
        2f
    );

    private sealed class PosicoesFalsas : IPosicoesDoMundo
    {
        public Dictionary<long, PosicaoNoMundo> Posicoes { get; } = [];

        public bool TentarObter(long personagemId, out PosicaoNoMundo posicao) =>
            Posicoes.TryGetValue(personagemId, out posicao);

        public IReadOnlyList<long> PersonagensNaArea(string area) =>
            Posicoes.Where(p => p.Value.Area == area).Select(p => p.Key).ToList();
    }

    private sealed class CobrancaFalsa : ICobrancaDeRecursos
    {
        public ResultadoDaCobranca Resultado { get; set; } = ResultadoDaCobranca.Cobrado;
        public List<int> Custos { get; } = [];

        public Task<(ResultadoDaCobranca, RecursosMudaram?)> TentarCobrarAsync(
            long personagemId,
            RecursoCobravel recurso,
            int custo,
            CancellationToken cancelamento
        )
        {
            if (Resultado == ResultadoDaCobranca.Cobrado)
            {
                Custos.Add(custo);
            }

            return Task.FromResult<(ResultadoDaCobranca, RecursosMudaram?)>((Resultado, null));
        }

        public Task<ResultadoDaCobranca> PodeCobrarAsync(
            long personagemId,
            RecursoCobravel recurso,
            int custo,
            CancellationToken cancelamento
        ) => Task.FromResult(Resultado);
    }

    private sealed class DanoFalso : IDanoAoPersonagem
    {
        public List<(long Id, int Dano, string Origem, string Area)> Chamadas { get; } = [];

        public void AplicarDanoDeAmbiente(
            long personagemId,
            int dano,
            string origem,
            string area
        ) => Chamadas.Add((personagemId, dano, origem, area));
    }

    private sealed class LimiteFalso : ILimiteDeVelocidadeDoMundo
    {
        public List<(double Fator, double Duracao)> Chamadas { get; } = [];

        public bool LimitarVelocidade(long personagemId, double fator, double duracaoSegundos)
        {
            Chamadas.Add((fator, duracaoSegundos));
            return true;
        }
    }

    private sealed class QuedaFalsa : IEstadoDeQueda
    {
        public bool Caido { get; set; }

        public bool EstaCaido(long personagemId) => Caido;
    }

    private readonly PosicoesFalsas _posicoes = new();
    private readonly CobrancaFalsa _cobranca = new();
    private readonly DanoFalso _dano = new();
    private readonly LimiteFalso _limite = new();
    private readonly QuedaFalsa _queda = new();
    private readonly List<EstadoDoMovimento> _estados = [];
    private readonly List<DanoDeQueda> _avisosDeDano = [];
    private ServicoDeMovimentoEspecial _servico;
    private long _agora = 1_000_000;

    public MovimentoEspecialTestes()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = true;
        _servico = Criar([PiscinaFunda, Via, ViaNaAgua]);
    }

    private ServicoDeMovimentoEspecial Criar(IReadOnlyList<VolumeMarcado> volumes)
    {
        return new ServicoDeMovimentoEspecial(
            _posicoes,
            () => _posicoes.Posicoes.Keys.ToList(),
            (_, estado) => _estados.Add(estado),
            (_, aviso) => _avisosDeDano.Add(aviso),
            (_, _) => { },
            _cobranca,
            _dano,
            _limite,
            _queda,
            volumes: volumes,
            relogioMs: () => _agora
        );
    }

    private static PosicaoNoMundo Em(
        float x,
        float y,
        float z,
        string area = AreasDoMundo.Cidade
    ) => new(area, x, y, z, 0f);

    private async Task IrParaAsync(PosicaoNoMundo posicao, long avancaMs = 100)
    {
        _agora += avancaMs;
        _posicoes.Posicoes[Id] = posicao;
        await _servico.PassoAsync();
    }

    // Um ponto de terra firme (chão bem acima da água) para as quedas, e o chão dele.
    private static readonly (float X, float Z, float Chao) Terra = AcharTerra();

    private static (float X, float Z, float Chao) AcharTerra()
    {
        DadosDoMapaFeitoAMao.DesativadoEmTestes = true;
        for (var x = -200f; x <= 200f; x += 10f)
        {
            for (var z = -200f; z <= 200f; z += 10f)
            {
                var chao = MapaDoMundo.AlturaEm(x, z);
                if (
                    chao > MapaDoMundo.NivelDaAgua + 2f
                    && !RegrasMovimentoEspecial.EstaNadando(x, chao, z, null)
                    && Math.Abs(x) > 15f
                )
                {
                    return (x, z, chao);
                }
            }
        }

        throw new InvalidOperationException("Sem terra firme perto da origem.");
    }

    private static float ChaoDoCentro => Terra.Chao;

    [Fact]
    public async Task Queda_de_10_metros_tira_vida_pelo_servidor_e_avisa_o_dono()
    {
        Assert.True(ChaoDoCentro > MapaDoMundo.NivelDaAgua + 1f);
        var chao = ChaoDoCentro;

        foreach (var altura in new[] { 10f, 10f, 9.5f, 7.5f, 4.5f, 0f, 0f })
        {
            await IrParaAsync(Em(Terra.X, chao + altura, Terra.Z));
        }

        var chamada = Assert.Single(_dano.Chamadas);
        Assert.Equal(Id, chamada.Id);
        Assert.Equal(RegrasMovimentoEspecial.DanoDaQueda(10f), chamada.Dano);
        Assert.Equal("Queda", chamada.Origem);
        Assert.Equal(AreasDoMundo.Cidade, chamada.Area);
        var aviso = Assert.Single(_avisosDeDano);
        Assert.Equal(chamada.Dano, aviso.Dano);
        Assert.InRange(aviso.Altura, 9.99f, 10.01f);
    }

    [Fact]
    public async Task Queda_dentro_da_altura_segura_e_pulo_nao_tiram_vida()
    {
        var chao = ChaoDoCentro;

        // Pulo de 1,25 m e queda de 3 m: nenhum dos dois passa da altura segura.
        foreach (var altura in new[] { 0f, 0f, 1.25f, 0.9f, 0.3f, 0f, 0f, 3f, 2f, 0.5f, 0f, 0f })
        {
            await IrParaAsync(Em(Terra.X, chao + altura, Terra.Z));
        }

        Assert.Empty(_dano.Chamadas);
    }

    [Fact]
    public async Task Descer_uma_rampa_a_pe_nao_conta_como_queda()
    {
        var chao = ChaoDoCentro;

        // 40 passos de 0,5 m descendo 0,5 m (45 graus): 20 m de descida, nenhuma queda.
        for (var i = 0; i < 40; i++)
        {
            await IrParaAsync(Em(Terra.X + (i * 0.5f), chao + 30f - (i * 0.5f), Terra.Z));
        }

        await IrParaAsync(Em(Terra.X + 20f, chao + 10f, Terra.Z));
        Assert.Empty(_dano.Chamadas);
    }

    [Fact]
    public async Task Cair_em_agua_funda_nao_machuca()
    {
        // Cai de 25 m sobre a piscina de 6 m de água.
        foreach (var y in new[] { 25f, 25f, 24.5f, 22.5f, 19.5f, 15.5f, 10.5f, 4f, 3.5f })
        {
            await IrParaAsync(Em(0f, y, 0f));
        }

        Assert.Empty(_dano.Chamadas);
        Assert.True(_servico.EstaNadando(Id));
    }

    [Fact]
    public async Task Cair_em_agua_rasa_machuca()
    {
        _servico = Criar([PocaRasa]);
        // Poça de 1,5 m: dá para nadar, mas não amortece. Cai de 20 m até -0,2 (dentro da poça).
        foreach (var y in new[] { 20f, 20f, 19.5f, 17.5f, 14.5f, 10.5f, 5.5f, 0f, -0.2f })
        {
            await IrParaAsync(Em(0f, y, 0f));
        }

        var chamada = Assert.Single(_dano.Chamadas);
        Assert.True(chamada.Dano > 0);
    }

    [Fact]
    public async Task Teleporte_ou_volta_a_cidade_nao_vira_queda()
    {
        var chao = ChaoDoCentro;
        await IrParaAsync(Em(Terra.X, chao + 80f, Terra.Z));
        await IrParaAsync(Em(Terra.X, chao + 80f, Terra.Z));

        // Reaparece no chão a centenas de metros: salto horizontal reinicia a medição.
        await IrParaAsync(Em(Terra.X + 100f, chao, Terra.Z));
        await IrParaAsync(Em(Terra.X + 100f, chao, Terra.Z));

        Assert.Empty(_dano.Chamadas);
    }

    [Fact]
    public async Task Fora_da_cidade_e_do_mundo_aberto_nao_ha_queda()
    {
        foreach (var altura in new[] { 30f, 30f, 29.5f, 27.5f, 24.5f, 20.5f, 15.5f, 0f, 0f })
        {
            await IrParaAsync(Em(10f, altura, 10f, AreasDoMundo.Instancia("mina")));
        }

        Assert.Empty(_dano.Chamadas);
        Assert.False(_servico.EstaNadando(Id));
    }

    [Fact]
    public async Task Quem_esta_caido_nao_leva_dano_de_queda()
    {
        _queda.Caido = true;
        var chao = ChaoDoCentro;
        foreach (var altura in new[] { 10f, 10f, 9.5f, 7.5f, 4.5f, 0f, 0f })
        {
            await IrParaAsync(Em(Terra.X, chao + altura, Terra.Z));
        }

        Assert.Empty(_dano.Chamadas);
    }

    [Fact]
    public async Task Nadar_so_vale_em_agua_funda_com_os_pes_abaixo_da_superficie()
    {
        // Pés acima da superfície (5): não nada.
        await IrParaAsync(Em(0f, 5.5f, 0f));
        Assert.False(_servico.EstaNadando(Id));

        await IrParaAsync(Em(0f, 4f, 0f));
        Assert.True(_servico.EstaNadando(Id));
        Assert.Contains(_estados, e => e.Nadando);

        // Fora do volume, na terra firme: não nada.
        await IrParaAsync(Em(Terra.X, ChaoDoCentro, Terra.Z));
        Assert.False(_servico.EstaNadando(Id));
    }

    [Fact]
    public async Task Nadar_gasta_stamina_a_cada_segundo_e_sem_stamina_fica_lento()
    {
        await IrParaAsync(Em(0f, 4f, 0f));
        Assert.Empty(_cobranca.Custos);

        for (var i = 0; i < 10; i++)
        {
            await IrParaAsync(Em(0f, 4f, 0f));
        }

        Assert.NotEmpty(_cobranca.Custos);
        Assert.All(
            _cobranca.Custos,
            c => Assert.Equal(ConfigMovimentoEspecial.StaminaDoNadoPorSegundo, c)
        );

        _cobranca.Resultado = ResultadoDaCobranca.Insuficiente;
        for (var i = 0; i < 10; i++)
        {
            await IrParaAsync(Em(0f, 4f, 0f));
        }

        Assert.Contains(_estados, e => e.Nadando && e.SemStamina);
        Assert.Contains(_limite.Chamadas, c => c.Fator == ConfigMovimentoEspecial.FatorSemStamina);
        Assert.True(_servico.EstaNadando(Id));
    }

    [Fact]
    public async Task Escalar_fora_de_via_marcada_e_recusado()
    {
        await IrParaAsync(Em(Terra.X, ChaoDoCentro, Terra.Z));
        _estados.Clear();

        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });

        Assert.False(_servico.EstaEscalando(Id));
        var resposta = Assert.Single(_estados);
        Assert.False(resposta.Escalando);
    }

    [Fact]
    public async Task Escalar_dentro_da_via_marcada_vale_e_gasta_stamina()
    {
        await IrParaAsync(Em(30f, 2f, -0.5f));
        _estados.Clear();

        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.True(_servico.EstaEscalando(Id));
        Assert.Contains(_estados, e => e.Escalando);

        for (var i = 0; i < 11; i++)
        {
            await IrParaAsync(Em(30f, 2f + (i * 0.2f), -0.5f));
        }

        Assert.True(_servico.EstaEscalando(Id));
        Assert.Contains(ConfigMovimentoEspecial.StaminaDaEscaladaPorSegundo, _cobranca.Custos);
        // Escalar não é cair: subir e descer pela via não dá dano.
        Assert.Empty(_dano.Chamadas);
    }

    [Fact]
    public async Task Sair_da_via_ou_ficar_sem_stamina_termina_a_escalada()
    {
        await IrParaAsync(Em(30f, 2f, -0.5f));
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.True(_servico.EstaEscalando(Id));

        // Anda para o lado até fora do volume (largura 4, margem 0,5).
        await IrParaAsync(Em(35f, 2f, -0.5f));
        Assert.False(_servico.EstaEscalando(Id));

        await IrParaAsync(Em(30f, 2f, -0.5f));
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.True(_servico.EstaEscalando(Id));

        _cobranca.Resultado = ResultadoDaCobranca.Insuficiente;
        for (var i = 0; i < 11; i++)
        {
            await IrParaAsync(Em(30f, 2f, -0.5f));
        }

        Assert.False(_servico.EstaEscalando(Id));
        Assert.Contains(_estados, e => e.SemStamina);
    }

    [Fact]
    public async Task Escalar_sem_stamina_e_recusado_e_nadando_tambem()
    {
        await IrParaAsync(Em(30f, 2f, -0.5f));
        _cobranca.Resultado = ResultadoDaCobranca.Insuficiente;
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.False(_servico.EstaEscalando(Id));

        _cobranca.Resultado = ResultadoDaCobranca.Cobrado;
        // Nadando na piscina (y = 4) não escala, mesmo que a via alcance.
        await IrParaAsync(Em(0f, 4f, -0.5f));
        Assert.True(_servico.EstaNadando(Id));
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.False(_servico.EstaEscalando(Id));
    }

    [Fact]
    public async Task Pedido_de_parar_solta_a_via_e_quem_caiu_nao_escala()
    {
        await IrParaAsync(Em(30f, 2f, -0.5f));
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = false });
        Assert.False(_servico.EstaEscalando(Id));

        _queda.Caido = true;
        await _servico.AoEscalarAsync(Id, new IntencaoDeEscalada { Escalar = true });
        Assert.False(_servico.EstaEscalando(Id));
    }
}
