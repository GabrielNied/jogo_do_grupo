using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasAtaqueTestes
{
    // ── Valores padrão de balanceamento (fatia-jogavel.md, seção 2) ───────────

    [Fact]
    public void Valores_padrao_do_ataque_leve()
    {
        Assert.Equal(0.5, RegrasAtaque.PreparoSegundos(TipoDeAtaque.Leve));
        Assert.Equal(1.0, RegrasAtaque.Multiplicador(TipoDeAtaque.Leve));
        Assert.Equal(0, RegrasAtaque.CustoDeVigor(TipoDeAtaque.Leve));
        Assert.Equal(500, RegrasAtaque.PreparoMs(TipoDeAtaque.Leve));
    }

    [Fact]
    public void Valores_padrao_do_ataque_pesado()
    {
        Assert.Equal(1.0, RegrasAtaque.PreparoSegundos(TipoDeAtaque.Pesado));
        Assert.Equal(2.2, RegrasAtaque.Multiplicador(TipoDeAtaque.Pesado));
        Assert.Equal(20, RegrasAtaque.CustoDeVigor(TipoDeAtaque.Pesado));
        Assert.Equal(1000, RegrasAtaque.PreparoMs(TipoDeAtaque.Pesado));
    }

    [Fact]
    public void Valores_padrao_do_volume_e_da_fila()
    {
        Assert.Equal(2.5, RegrasAtaque.AlcanceMetros);
        Assert.Equal(90.0, RegrasAtaque.AberturaDoConeGraus);
        Assert.Equal(0.3, RegrasAtaque.JanelaDaFilaSegundos);
    }

    [Fact]
    public void Pesado_tem_recuperacao_maior_que_o_leve() =>
        Assert.True(
            RegrasAtaque.RecuperacaoSegundos(TipoDeAtaque.Pesado)
                > RegrasAtaque.RecuperacaoSegundos(TipoDeAtaque.Leve)
        );

    [Theory]
    [InlineData(TipoDeAtaque.Leve)]
    [InlineData(TipoDeAtaque.Pesado)]
    public void Duracao_e_preparo_mais_recuperacao(TipoDeAtaque tipo) =>
        Assert.Equal(
            RegrasAtaque.PreparoSegundos(tipo) + RegrasAtaque.RecuperacaoSegundos(tipo),
            RegrasAtaque.DuracaoSegundos(tipo),
            9
        );

    // ── Tipo recebido da rede ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(-1, false)]
    public void TipoValido_so_aceita_leve_e_pesado(int valor, bool esperado) =>
        Assert.Equal(esperado, RegrasAtaque.TipoValido((TipoDeAtaque)valor));

    // ── Vigor ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Leve_sempre_pode(int vigorAtual) =>
        Assert.True(RegrasAtaque.TemVigor(TipoDeAtaque.Leve, vigorAtual));

    [Theory]
    [InlineData(20, true)]
    [InlineData(100, true)]
    [InlineData(19, false)]
    [InlineData(0, false)]
    public void Pesado_precisa_de_20_de_vigor(int vigorAtual, bool esperado) =>
        Assert.Equal(esperado, RegrasAtaque.TemVigor(TipoDeAtaque.Pesado, vigorAtual));

    // ── Dano ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(TipoDeAtaque.Leve, 0, 20)]
    [InlineData(TipoDeAtaque.Leve, 10, 40)]
    [InlineData(TipoDeAtaque.Pesado, 0, 44)]
    [InlineData(TipoDeAtaque.Pesado, 10, 88)]
    public void Dano_e_arma_mais_forca_vezes_o_multiplicador(
        TipoDeAtaque tipo,
        int forca,
        int esperado
    ) => Assert.Equal(esperado, RegrasAtaque.Dano(tipo, forca));

    [Fact]
    public void Dano_com_forca_negativa_conta_como_forca_zero() =>
        Assert.Equal(
            RegrasAtaque.Dano(TipoDeAtaque.Leve, 0),
            RegrasAtaque.Dano(TipoDeAtaque.Leve, -50)
        );

    [Fact]
    public void Dano_com_forca_enorme_nao_estoura() =>
        Assert.Equal(int.MaxValue, RegrasAtaque.Dano(TipoDeAtaque.Pesado, int.MaxValue));

    [Fact]
    public void Pesado_causa_mais_dano_que_o_leve() =>
        Assert.True(
            RegrasAtaque.Dano(TipoDeAtaque.Pesado, 5) > RegrasAtaque.Dano(TipoDeAtaque.Leve, 5)
        );

    // ── Fila de um pedido ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(0.3, true)]
    [InlineData(0.1, true)]
    [InlineData(0.31, false)]
    [InlineData(1.0, false)]
    [InlineData(0.0, false)]
    [InlineData(-0.2, false)]
    public void Pedido_entra_na_fila_so_nos_ultimos_300_ms_da_acao(
        double segundosAteOFim,
        bool esperado
    ) => Assert.Equal(esperado, RegrasAtaque.EntraNaFila(segundosAteOFim));

    // ── Volume do golpe: cone de 90 graus e 2,5 m à frente ───────────────────

    [Fact]
    public void Alvo_a_frente_dentro_do_alcance_esta_no_volume() =>
        Assert.True(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, 2.0));

    [Theory]
    [InlineData(2.5, true)]
    [InlineData(2.51, false)]
    [InlineData(10.0, false)]
    public void Alcance_e_de_2_metros_e_meio(double distancia, bool esperado) =>
        Assert.Equal(esperado, RegrasAtaque.EstaNoVolume(0, 0, 0, 0, distancia));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(44.0, true)]
    [InlineData(-44.0, true)]
    [InlineData(46.0, false)]
    [InlineData(-46.0, false)]
    [InlineData(90.0, false)]
    [InlineData(180.0, false)]
    public void Cone_abre_45_graus_para_cada_lado(double anguloDoAlvo, bool esperado)
    {
        double radianos = anguloDoAlvo * Math.PI / 180.0;
        double x = Math.Sin(radianos) * 2.0;
        double z = Math.Cos(radianos) * 2.0;
        Assert.Equal(esperado, RegrasAtaque.EstaNoVolume(0, 0, 0, x, z));
    }

    [Fact]
    public void Direcao_zero_aponta_para_z_e_noventa_para_x()
    {
        Assert.True(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, 2));
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, 2, 0));
        Assert.True(RegrasAtaque.EstaNoVolume(0, 0, 90, 2, 0));
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 90, 0, 2));
    }

    [Fact]
    public void Alvo_atras_do_atacante_nao_esta_no_volume() =>
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, -1.0));

    [Fact]
    public void Cone_vale_para_atacante_fora_da_origem() =>
        Assert.True(RegrasAtaque.EstaNoVolume(100, -50, 180, 100, -52));

    [Theory]
    [InlineData(350.0, 10.0, true)]
    [InlineData(-170.0, 170.0, true)]
    [InlineData(720.0, 0.0, true)]
    [InlineData(350.0, 60.0, false)]
    public void Cone_atravessa_a_volta_de_360_graus(
        double direcao,
        double anguloDoAlvo,
        bool esperado
    )
    {
        double radianos = anguloDoAlvo * Math.PI / 180.0;
        double x = Math.Sin(radianos) * 2.0;
        double z = Math.Cos(radianos) * 2.0;
        Assert.Equal(esperado, RegrasAtaque.EstaNoVolume(0, 0, direcao, x, z));
    }

    [Fact]
    public void Alvo_na_mesma_posicao_do_atacante_esta_no_volume() =>
        Assert.True(RegrasAtaque.EstaNoVolume(3, 4, 123, 3, 4));

    [Fact]
    public void Raio_do_alvo_estende_o_alcance_mas_nao_o_angulo()
    {
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, 2.9));
        Assert.True(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, 2.9, 0.5));
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, 2.0, 0, 0.5));
    }

    [Fact]
    public void Raio_negativo_conta_como_zero() =>
        Assert.False(RegrasAtaque.EstaNoVolume(0, 0, 0, 0, 2.6, -5.0));

    [Theory]
    [InlineData(10.0, 350.0, 20.0)]
    [InlineData(350.0, 10.0, -20.0)]
    [InlineData(180.0, -180.0, 0.0)]
    [InlineData(90.0, 0.0, 90.0)]
    public void DiferencaDeAngulo_devolve_o_menor_caminho(double a, double b, double esperado) =>
        Assert.Equal(esperado, RegrasAtaque.DiferencaDeAngulo(a, b), 9);

    // ── Cadeia de leves, giro e compromisso (fatia-jogavel.md, seção 2) ───────

    [Fact]
    public void Valores_padrao_da_cadeia_do_giro_e_do_compromisso()
    {
        Assert.Equal(3, RegrasAtaque.PassosDaCadeiaLeve);
        Assert.Equal(1.2, RegrasAtaque.MultiplicadorDoTerceiroLeve);
        Assert.Equal(0.8, RegrasAtaque.JanelaDaCadeiaSegundos);
        Assert.Equal(240.0, RegrasAtaque.GiroMaximoLeveGrausPorSegundo);
        Assert.Equal(60.0, RegrasAtaque.GiroMaximoPesadoGrausPorSegundo);
        Assert.Equal(0.4, RegrasAtaque.VelocidadeDuranteGolpe);
    }

    [Fact]
    public void Terceiro_leve_usa_1_2x_e_o_pesado_ignora_o_passo()
    {
        // Força 0: dano da espada 20.
        Assert.Equal(20, RegrasAtaque.Dano(TipoDeAtaque.Leve, 0, 0));
        Assert.Equal(20, RegrasAtaque.Dano(TipoDeAtaque.Leve, 0, 1));
        Assert.Equal(24, RegrasAtaque.Dano(TipoDeAtaque.Leve, 0, 2));
        Assert.Equal(44, RegrasAtaque.Dano(TipoDeAtaque.Pesado, 0, 2));
        Assert.Equal(
            RegrasAtaque.Dano(TipoDeAtaque.Leve, 5),
            RegrasAtaque.Dano(TipoDeAtaque.Leve, 5, 0)
        );
    }

    [Theory]
    [InlineData(TipoDeAtaque.Leve, 0, 0, 1)]
    [InlineData(TipoDeAtaque.Leve, 1, 1, 2)]
    [InlineData(TipoDeAtaque.Leve, 2, 2, 0)] // o 4º leve volta ao 1º
    [InlineData(TipoDeAtaque.Pesado, 1, 0, 0)] // o pesado reinicia a cadeia
    public void ProximoPasso_sobe_e_o_pesado_reinicia(
        TipoDeAtaque tipo,
        int proximoPasso,
        int passoDoGolpe,
        int esperado
    )
    {
        Assert.Equal(passoDoGolpe, RegrasAtaque.PassoDoGolpe(tipo, proximoPasso, 0.0));
        Assert.Equal(esperado, RegrasAtaque.ProximoPasso(tipo, passoDoGolpe));
    }

    [Fact]
    public void Cadeia_expira_so_depois_de_0_8_s()
    {
        Assert.Equal(1, RegrasAtaque.PassoDoGolpe(TipoDeAtaque.Leve, 1, 0.8));
        Assert.Equal(0, RegrasAtaque.PassoDoGolpe(TipoDeAtaque.Leve, 1, 0.81));
    }

    [Theory]
    [InlineData(TipoDeAtaque.Pesado, 1.0, 180.0, 60.0)] // o pesado gira 60° no 1 s de preparo
    [InlineData(TipoDeAtaque.Pesado, 1.0, -170.0, -60.0)]
    [InlineData(TipoDeAtaque.Pesado, 1.0, 30.0, 30.0)] // dentro do teto, vai até onde olha
    [InlineData(TipoDeAtaque.Leve, 0.5, 170.0, 120.0)] // o leve gira até 240°/s
    [InlineData(TipoDeAtaque.Leve, 0.5, 90.0, 90.0)]
    public void Direcao_do_golpe_nao_gira_mais_que_o_teto_do_preparo(
        TipoDeAtaque tipo,
        double segundos,
        double desejada,
        double esperado
    ) => Assert.Equal(esperado, RegrasAtaque.DirecaoDoGolpe(tipo, 0.0, desejada, segundos), 9);

    [Fact]
    public void Direcao_do_golpe_gira_pelo_menor_caminho_a_partir_da_direcao_inicial()
    {
        // De 350° para 10° são 20° no sentido positivo, não 340° no negativo.
        var direcao = RegrasAtaque.DirecaoDoGolpe(TipoDeAtaque.Pesado, 350.0, 10.0, 1.0);

        Assert.Equal(20.0, RegrasAtaque.DiferencaDeAngulo(direcao, 350.0), 9);
    }
}
