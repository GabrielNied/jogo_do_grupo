using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasCercoTestes
{
    [Fact]
    public void Numeros_sao_os_da_proposta()
    {
        Assert.Equal(2, RegrasCerco.LimiteDeAtacantesPorAlvo);
        Assert.Equal(2.0 * RegrasInimigo.RaioDoCorpoMetros, RegrasCerco.SeparacaoMinimaMetros);
        Assert.True(RegrasCerco.RaioDoAnelMetros < RegrasInimigo.AlcanceDeAtaqueMetros);
        Assert.True(RegrasCerco.RaioDaRondaMetros > RegrasInimigo.AlcanceDeAtaqueMetros);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(5, false)]
    public void Pode_atacar_so_enquanto_ha_vaga(int atacantes, bool esperado) =>
        Assert.Equal(esperado, RegrasCerco.PodeAtacar(atacantes));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Postos_de_um_grupo_inteiro_ficam_separados_e_no_raio_do_anel(bool ronda)
    {
        var raio = ronda ? RegrasCerco.RaioDaRondaMetros : RegrasCerco.RaioDoAnelMetros;
        var xs = new double[12];
        var zs = new double[12];
        for (var i = 0; i < xs.Length; i++)
        {
            RegrasCerco.Posto(3.0, -2.0, i, ronda, out xs[i], out zs[i]);
            var distanciaAoAlvo = RegrasInimigo.Distancia(xs[i], zs[i], 3.0, -2.0);
            Assert.True(distanciaAoAlvo >= raio - 1e-9);
        }

        // O primeiro anel (o maior grupo comum) fica a pelo menos a separação mínima.
        for (var a = 0; a < RegrasCerco.PostosPorAnel; a++)
        {
            for (var b = a + 1; b < RegrasCerco.PostosPorAnel; b++)
            {
                Assert.True(
                    RegrasInimigo.Distancia(xs[a], zs[a], xs[b], zs[b])
                        >= RegrasCerco.SeparacaoMinimaMetros
                );
            }
        }
    }

    [Fact]
    public void Posto_do_atacante_fica_no_alcance_e_o_da_ronda_fora_dele()
    {
        RegrasCerco.Posto(0, 0, 0, false, out var ax, out var az);
        RegrasCerco.Posto(0, 0, 0, true, out var rx, out var rz);

        Assert.True(RegrasInimigo.NoAlcanceDeAtaque(RegrasInimigo.Distancia(0, 0, ax, az)));
        Assert.False(RegrasInimigo.NoAlcanceDeAtaque(RegrasInimigo.Distancia(0, 0, rx, rz)));
    }

    [Fact]
    public void Afastar_empurra_so_quem_esta_perto_demais()
    {
        RegrasCerco.Afastar(0, 0, new[] { 0.5, 5.0 }, new[] { 0.0, 0.0 }, out var x, out var z);

        Assert.Equal(-RegrasCerco.SeparacaoMinimaMetros + 0.5, x, 9);
        Assert.Equal(0.0, z, 9);
    }

    [Fact]
    public void Afastar_no_mesmo_ponto_vai_ao_longo_de_x_e_sem_vizinhos_nao_mexe()
    {
        RegrasCerco.Afastar(1, 1, new[] { 1.0 }, new[] { 1.0 }, out var x, out var z);
        Assert.Equal(1 + RegrasCerco.SeparacaoMinimaMetros, x, 9);
        Assert.Equal(1.0, z, 9);

        RegrasCerco.Afastar(1, 1, null, null, out x, out z);
        Assert.Equal((1.0, 1.0), (x, z));
    }

    [Fact]
    public void Afastar_respeita_maxDeslocamentoMetros()
    {
        // Ponto sobreposto com vizinho tenta empurrar SeparacaoMinimaMetros (1.2 m),
        // mas com maxDeslocamentoMetros = 0.3 m, desloca no maximo 0.3 m.
        RegrasCerco.Afastar(
            1,
            1,
            new[] { 1.0 },
            new[] { 1.0 },
            out var x,
            out var z,
            maxDeslocamentoMetros: 0.3
        );
        Assert.Equal(1.3, x, 9);
        Assert.Equal(1.0, z, 9);
    }

    [Fact]
    public void Afastar_com_spans_produz_mesmo_resultado()
    {
        ReadOnlySpan<double> vx = stackalloc double[] { 0.5, 5.0 };
        ReadOnlySpan<double> vz = stackalloc double[] { 0.0, 0.0 };
        RegrasCerco.Afastar(0, 0, vx, vz, out var x, out var z);

        Assert.Equal(-RegrasCerco.SeparacaoMinimaMetros + 0.5, x, 9);
        Assert.Equal(0.0, z, 9);
    }

    [Fact]
    public void CalcularMelhorPosto_escolhe_slot_mais_proximo_angularmente()
    {
        // Alvo em (0,0). Inimigo em (0, 5) -> angulo ~ 0 graus (+Z)
        Span<bool> ocupados = stackalloc bool[RegrasCerco.PostosPorAnel];
        ocupados.Clear();

        RegrasCerco.CalcularMelhorPosto(
            0,
            0,
            0,
            5,
            ocupados,
            false,
            out var px,
            out var pz,
            out var slot
        );

        // Slot 0 tem angulo 0 graus (+Z)
        Assert.Equal(0, slot);
        Assert.Equal(0.0, px, 3);
        Assert.True(pz > 0);

        // Se slot 0 estiver ocupado, deve escolher slot adjacente (1 ou PostosPorAnel - 1)
        ocupados[0] = true;
        RegrasCerco.CalcularMelhorPosto(0, 0, 0, 5, ocupados, false, out px, out pz, out slot);
        Assert.True(slot == 1 || slot == RegrasCerco.PostosPorAnel - 1);
    }

    [Fact]
    public void PassoOrbital_mantem_raio_de_ronda_ao_redor_do_alvo()
    {
        double alvoX = 10,
            alvoZ = 20;
        double iniX = alvoX + RegrasCerco.RaioDaRondaMetros,
            iniZ = alvoZ;

        RegrasCerco.PassoOrbital(
            alvoX,
            alvoZ,
            iniX,
            iniZ,
            RegrasCerco.VelocidadeStrafeRondaMetrosPorSegundo,
            1.0,
            true,
            out var nx,
            out var nz
        );

        double dist = RegrasInimigo.Distancia(nx, nz, alvoX, alvoZ);
        Assert.True(Math.Abs(dist - RegrasCerco.RaioDaRondaMetros) < 1e-4);
    }

    [Fact]
    public void CalcularRecuoPosGolpe_afasta_em_direcao_ao_raio_de_ronda()
    {
        double alvoX = 0,
            alvoZ = 0;
        double iniX = 0,
            iniZ = RegrasCerco.RaioDoAnelMetros;

        RegrasCerco.CalcularRecuoPosGolpe(
            alvoX,
            alvoZ,
            iniX,
            iniZ,
            RegrasCerco.DistanciaRecuoPosGolpeMetros,
            out var nx,
            out var nz
        );

        double dist = RegrasInimigo.Distancia(nx, nz, alvoX, alvoZ);
        Assert.True(dist > RegrasCerco.RaioDoAnelMetros);
        Assert.True(dist <= RegrasCerco.RaioDaRondaMetros + 1e-4);
    }
}
