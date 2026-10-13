using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasEsquivaTestes
{
    // ── Contrato: IDs e campos padrão ─────────────────────────────────────────

    private static readonly ushort[] _ids =
    {
        CombateMessageIds.IntencaoDeEsquiva,
        CombateMessageIds.EsquivaRecusada,
        CombateMessageIds.EsquivaIniciada,
    };

    [Fact]
    public void Ids_da_esquiva_tem_os_numeros_do_contrato_e_estao_na_faixa()
    {
        Assert.Equal(6010, CombateMessageIds.IntencaoDeEsquiva);
        Assert.Equal(6011, CombateMessageIds.EsquivaRecusada);
        Assert.Equal(6012, CombateMessageIds.EsquivaIniciada);
        Assert.All(_ids, id => Assert.True(CombateMessageIds.Faixa.Contem(id)));
    }

    [Fact]
    public void Ids_da_esquiva_nao_repetem_os_do_ataque()
    {
        var doAtaque = new[]
        {
            CombateMessageIds.IntencaoDeAtaque,
            CombateMessageIds.AtaqueIniciado,
            CombateMessageIds.AtaqueAcertou,
            CombateMessageIds.AtaqueRecusado,
            CombateMessageIds.RecursosMudaram,
        };
        var todos = _ids.Concat(doAtaque).ToArray();
        Assert.Equal(todos.Length, todos.Distinct().Count());
    }

    [Fact]
    public void Mensagens_da_esquiva_campos_padrao()
    {
        var intencao = new IntencaoDeEsquiva();
        Assert.Equal(0f, intencao.DirecaoY);
        Assert.Equal(0, intencao.Sequencia);

        var recusada = new EsquivaRecusada();
        Assert.Equal(0, recusada.Sequencia);
        Assert.Equal("", recusada.Codigo);

        var iniciada = new EsquivaIniciada();
        Assert.Equal(0L, iniciada.PersonagemId);
        Assert.Equal(0f, iniciada.DirecaoY);
        Assert.Equal(0, iniciada.DuracaoMs);
    }

    // ── Valores padrão (fatia-jogavel.md, seção 3) ────────────────────────────

    [Fact]
    public void Valores_padrao_da_esquiva()
    {
        Assert.Equal(5.0, RegrasEsquiva.DistanciaMetros);
        Assert.Equal(0.6, RegrasEsquiva.DuracaoSegundos);
        Assert.Equal(15, RegrasEsquiva.CustoDeStamina);
        Assert.Equal(600, RegrasEsquiva.DuracaoMs);
        Assert.Equal(5.0 / 0.6, RegrasEsquiva.VelocidadeMetrosPorSegundo, 9);
    }

    [Fact]
    public void Esquiva_e_mais_rapida_que_a_corrida() =>
        Assert.True(RegrasEsquiva.VelocidadeMetrosPorSegundo > 5.5);

    // ── Stamina ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(15, true)]
    [InlineData(100, true)]
    [InlineData(14, false)]
    [InlineData(0, false)]
    [InlineData(-5, false)]
    public void Esquiva_precisa_de_15_de_stamina(int staminaAtual, bool esperado) =>
        Assert.Equal(esperado, RegrasEsquiva.TemStamina(staminaAtual));

    // ── Janela e deslocamento ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(0.3, true)]
    [InlineData(0.6, true)]
    [InlineData(0.61, false)]
    [InlineData(-0.1, false)]
    public void Esquiva_dura_600_ms(double segundos, bool emAndamento) =>
        Assert.Equal(emAndamento, RegrasEsquiva.EmAndamento(segundos));

    [Theory]
    [InlineData(5.0, true)]
    [InlineData(0.0, true)]
    [InlineData(2.5, true)]
    [InlineData(5.1, false)]
    [InlineData(-1.0, false)]
    public void Cinco_metros_cabem_e_cinco_e_dez_nao(double distancia, bool cabe) =>
        Assert.Equal(cabe, RegrasEsquiva.DeslocamentoCabe(distancia));

    [Theory]
    [InlineData(-1.0, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(0.3, 2.5)]
    [InlineData(0.6, 5.0)]
    [InlineData(2.0, 5.0)]
    public void Distancia_permitida_cresce_ate_5_m_e_para(double segundos, double esperado) =>
        Assert.Equal(esperado, RegrasEsquiva.DistanciaPermitidaAte(segundos), 9);

    // ── Destino ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, 0.0, 5.0)]
    [InlineData(90.0, 5.0, 0.0)]
    [InlineData(180.0, 0.0, -5.0)]
    [InlineData(270.0, -5.0, 0.0)]
    [InlineData(-90.0, -5.0, 0.0)]
    [InlineData(360.0, 0.0, 5.0)]
    public void Destino_fica_a_5_m_na_direcao_pedida(double direcao, double x, double z)
    {
        RegrasEsquiva.Destino(0, 0, direcao, out var destinoX, out var destinoZ);
        Assert.Equal(x, destinoX, 9);
        Assert.Equal(z, destinoZ, 9);
    }

    [Fact]
    public void Destino_parte_da_posicao_do_personagem()
    {
        RegrasEsquiva.Destino(10, -20, 0, out var destinoX, out var destinoZ);
        Assert.Equal(10.0, destinoX, 9);
        Assert.Equal(-15.0, destinoZ, 9);
    }

    [Fact]
    public void Destino_em_diagonal_continua_a_5_m()
    {
        RegrasEsquiva.Destino(0, 0, 45, out var destinoX, out var destinoZ);
        Assert.Equal(5.0, Math.Sqrt(destinoX * destinoX + destinoZ * destinoZ), 9);
        Assert.True(RegrasEsquiva.DeslocamentoCabe(5.0));
    }

    [Theory]
    [InlineData(true, 90.0, 30.0, 90.0)] // andando: a direção do movimento
    [InlineData(false, 90.0, 30.0, 210.0)] // parado: para trás da mira
    [InlineData(false, 0.0, 200.0, 20.0)]
    [InlineData(true, -90.0, 0.0, 270.0)]
    public void DirecaoDaEsquiva_anda_com_o_movimento_e_parado_sai_para_tras_da_mira(
        bool andando,
        double movimento,
        double mira,
        double esperado
    ) => Assert.Equal(esperado, RegrasEsquiva.DirecaoDaEsquiva(andando, movimento, mira), 9);

    [Fact]
    public void Dano_some_nos_primeiros_0_3_s_e_o_instante_final_ja_leva_dano()
    {
        Assert.Equal(0.3, RegrasEsquiva.JanelaDeInvulnerabilidadeSegundos);
        Assert.True(RegrasEsquiva.EstaInvulneravel(0.0));
        Assert.True(RegrasEsquiva.EstaInvulneravel(0.29));
        Assert.False(RegrasEsquiva.EstaInvulneravel(0.3));
        Assert.False(RegrasEsquiva.EstaInvulneravel(0.5));
        Assert.False(RegrasEsquiva.EstaInvulneravel(-0.1));
    }
}
