using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Shared.Tests.Features.Mobs;

public class RegrasInimigoTestes
{
    private const double Tolerancia = 1e-9;

    [Fact]
    public void Numeros_sao_os_da_proposta()
    {
        Assert.Equal(CatalogoCriaturas.JavaliDoCampo, RegrasInimigo.CriaturaDaFatia);
        Assert.Equal(3, RegrasInimigo.TamanhoMinimoDoGrupo);
        Assert.Equal(5, RegrasInimigo.TamanhoMaximoDoGrupo);
        Assert.Equal(12.0, RegrasInimigo.DistanciaDeVisaoMetros);
        Assert.Equal(4.0, RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo);
        Assert.Equal(2.0, RegrasInimigo.AlcanceDeAtaqueMetros);
        Assert.Equal(1.0, RegrasInimigo.PreparoDoAtaqueSegundos);
        Assert.Equal(1.5, RegrasInimigo.RecuperacaoDoAtaqueSegundos);
        Assert.Equal(30.0, RegrasInimigo.DistanciaDeDesistenciaMetros);
        Assert.Equal(10, RegrasInimigo.PassosPorSegundo);
        Assert.Equal(1000, RegrasInimigo.PreparoDoAtaqueMs);
        Assert.Equal(0.1, RegrasInimigo.PassoSegundos, Tolerancia);
    }

    [Fact]
    public void Criatura_da_fatia_tem_a_vida_fixa_e_o_respawn_da_proposta()
    {
        var ficha = CatalogoCriaturas.Obter(RegrasInimigo.CriaturaDaFatia);
        Assert.NotNull(ficha);
        Assert.Equal(CategoriaDeCriatura.Comum, ficha.Categoria);
        Assert.Equal(180, ficha.VidaBase);
        Assert.Equal(40, ficha.DanoBase);
        Assert.Equal(120, ficha.IntervaloDeAparicaoSegundos);
    }

    [Fact]
    public void Golpe_alcanca_o_alvo_que_esta_no_alcance_de_ataque()
    {
        // O inimigo ataca a até 2 m: o passo do golpe mais o cone têm que cobrir isso.
        Assert.True(
            RegrasInimigo.PassoDoGolpeMetros + RegrasInimigo.AlcanceDoGolpeMetros
                >= RegrasInimigo.AlcanceDeAtaqueMetros
        );
    }

    [Fact]
    public void Persegue_mais_devagar_do_que_volta()
    {
        Assert.True(
            RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo
                < RegrasInimigo.VelocidadeDeRetornoMetrosPorSegundo
        );
    }

    [Theory]
    [InlineData(EstadoDeInimigo.Repouso, true)]
    [InlineData(EstadoDeInimigo.Morto, true)]
    [InlineData((EstadoDeInimigo)(-1), false)]
    [InlineData((EstadoDeInimigo)6, false)]
    public void EstadoValido_so_aceita_os_estados_do_enum(EstadoDeInimigo estado, bool esperado) =>
        Assert.Equal(esperado, RegrasInimigo.EstadoValido(estado));

    [Theory]
    [InlineData(EstadoDeInimigo.Repouso, true, true)]
    [InlineData(EstadoDeInimigo.Alerta, true, true)]
    [InlineData(EstadoDeInimigo.Perseguicao, true, true)]
    [InlineData(EstadoDeInimigo.Ataque, true, true)]
    [InlineData(EstadoDeInimigo.Retorno, true, false)]
    [InlineData(EstadoDeInimigo.Morto, false, false)]
    [InlineData((EstadoDeInimigo)99, false, false)]
    public void Morto_nao_age_e_quem_volta_nao_procura_alvo(
        EstadoDeInimigo estado,
        bool vivo,
        bool procuraAlvo
    )
    {
        Assert.Equal(vivo, RegrasInimigo.EstaVivo(estado));
        Assert.Equal(procuraAlvo, RegrasInimigo.ProcuraAlvo(estado));
    }

    [Theory]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Alerta)]
    [InlineData(EstadoDeInimigo.Alerta, EstadoDeInimigo.Perseguicao)]
    [InlineData(EstadoDeInimigo.Alerta, EstadoDeInimigo.Retorno)]
    [InlineData(EstadoDeInimigo.Perseguicao, EstadoDeInimigo.Ataque)]
    [InlineData(EstadoDeInimigo.Perseguicao, EstadoDeInimigo.Retorno)]
    [InlineData(EstadoDeInimigo.Ataque, EstadoDeInimigo.Perseguicao)]
    [InlineData(EstadoDeInimigo.Ataque, EstadoDeInimigo.Retorno)]
    [InlineData(EstadoDeInimigo.Retorno, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Morto)]
    [InlineData(EstadoDeInimigo.Alerta, EstadoDeInimigo.Morto)]
    [InlineData(EstadoDeInimigo.Perseguicao, EstadoDeInimigo.Morto)]
    [InlineData(EstadoDeInimigo.Ataque, EstadoDeInimigo.Morto)]
    [InlineData(EstadoDeInimigo.Retorno, EstadoDeInimigo.Morto)]
    public void Transicoes_permitidas(EstadoDeInimigo de, EstadoDeInimigo para) =>
        Assert.True(RegrasInimigo.TransicaoValida(de, para));

    [Theory]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Perseguicao)]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Ataque)]
    [InlineData(EstadoDeInimigo.Repouso, EstadoDeInimigo.Retorno)]
    [InlineData(EstadoDeInimigo.Alerta, EstadoDeInimigo.Ataque)]
    [InlineData(EstadoDeInimigo.Alerta, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Perseguicao, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Perseguicao, EstadoDeInimigo.Alerta)]
    [InlineData(EstadoDeInimigo.Ataque, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Retorno, EstadoDeInimigo.Alerta)]
    [InlineData(EstadoDeInimigo.Retorno, EstadoDeInimigo.Perseguicao)]
    [InlineData(EstadoDeInimigo.Retorno, EstadoDeInimigo.Ataque)]
    [InlineData(EstadoDeInimigo.Morto, EstadoDeInimigo.Repouso)]
    [InlineData(EstadoDeInimigo.Morto, EstadoDeInimigo.Alerta)]
    [InlineData(EstadoDeInimigo.Morto, EstadoDeInimigo.Morto)]
    [InlineData(EstadoDeInimigo.Repouso, (EstadoDeInimigo)99)]
    [InlineData((EstadoDeInimigo)99, EstadoDeInimigo.Morto)]
    public void Transicoes_proibidas(EstadoDeInimigo de, EstadoDeInimigo para) =>
        Assert.False(RegrasInimigo.TransicaoValida(de, para));

    [Theory]
    [InlineData(0.0, 3)]
    [InlineData(0.33, 3)]
    [InlineData(0.34, 4)]
    [InlineData(0.66, 4)]
    [InlineData(0.67, 5)]
    [InlineData(0.999, 5)]
    [InlineData(1.0, 5)]
    [InlineData(7.0, 5)]
    [InlineData(-1.0, 3)]
    public void TamanhoDoGrupo_fica_de_3_a_5(double sorteio, int esperado) =>
        Assert.Equal(esperado, RegrasInimigo.TamanhoDoGrupo(sorteio));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(12.0, true)]
    [InlineData(12.01, false)]
    [InlineData(-1.0, false)]
    public void Ve_ate_12_metros(double distancia, bool esperado) =>
        Assert.Equal(esperado, RegrasInimigo.Ve(distancia));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(2.0, true)]
    [InlineData(2.01, false)]
    [InlineData(-1.0, false)]
    public void Ataca_ate_2_metros(double distancia, bool esperado) =>
        Assert.Equal(esperado, RegrasInimigo.NoAlcanceDeAtaque(distancia));

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(30.0, false)]
    [InlineData(30.01, true)]
    public void Desiste_a_mais_de_30_metros_do_ponto_de_origem(double distancia, bool esperado) =>
        Assert.Equal(esperado, RegrasInimigo.DeveDesistir(distancia));

    [Fact]
    public void Distancia_e_medida_no_plano_do_chao()
    {
        Assert.Equal(5.0, RegrasInimigo.Distancia(0, 0, 3, 4), Tolerancia);
        Assert.Equal(0.0, RegrasInimigo.Distancia(2, -7, 2, -7), Tolerancia);
    }

    [Fact]
    public void EscolherAlvo_pega_o_mais_proximo()
    {
        long alvo = RegrasInimigo.EscolherAlvo(
            RegrasInimigo.SemAlvo,
            new long[] { 10, 20, 30 },
            new[] { 8.0, 3.0, 5.0 }
        );
        Assert.Equal(20, alvo);
    }

    [Fact]
    public void EscolherAlvo_mantem_o_alvo_atual_mesmo_com_outro_mais_perto()
    {
        long alvo = RegrasInimigo.EscolherAlvo(10, new long[] { 20, 10 }, new[] { 1.0, 9.0 });
        Assert.Equal(10, alvo);
    }

    [Fact]
    public void EscolherAlvo_troca_quando_o_alvo_atual_saiu_dos_candidatos()
    {
        long alvo = RegrasInimigo.EscolherAlvo(10, new long[] { 20, 30 }, new[] { 6.0, 4.0 });
        Assert.Equal(30, alvo);
    }

    [Fact]
    public void EscolherAlvo_no_empate_fica_com_o_menor_id_em_qualquer_ordem()
    {
        long semAlvo = RegrasInimigo.SemAlvo;
        Assert.Equal(
            7,
            RegrasInimigo.EscolherAlvo(semAlvo, new long[] { 9, 7 }, new[] { 4.0, 4.0 })
        );
        Assert.Equal(
            7,
            RegrasInimigo.EscolherAlvo(semAlvo, new long[] { 7, 9 }, new[] { 4.0, 4.0 })
        );
    }

    [Fact]
    public void EscolherAlvo_sem_candidatos_fica_sem_alvo()
    {
        long semAlvo = RegrasInimigo.SemAlvo;
        long[] ninguem = Array.Empty<long>();
        double[] nenhuma = Array.Empty<double>();
        Assert.Equal(semAlvo, RegrasInimigo.EscolherAlvo(10, ninguem, nenhuma));
        Assert.Equal(semAlvo, RegrasInimigo.EscolherAlvo(10, null!, nenhuma));
        Assert.Equal(semAlvo, RegrasInimigo.EscolherAlvo(10, new long[] { 5 }, null!));
    }

    [Fact]
    public void EscolherAlvo_ignora_candidato_sem_distancia_e_id_vazio()
    {
        long semAlvo = RegrasInimigo.SemAlvo;
        // O segundo candidato não tem distância: só o primeiro conta.
        Assert.Equal(5, RegrasInimigo.EscolherAlvo(semAlvo, new long[] { 5, 6 }, new[] { 9.0 }));
        // Id vazio nunca vira alvo, nem quando o alvo atual também é vazio.
        Assert.Equal(
            8,
            RegrasInimigo.EscolherAlvo(semAlvo, new long[] { semAlvo, 8 }, new[] { 1.0, 9.0 })
        );
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, 5.0, 0.0)]
    [InlineData(0.0, 0.0, 5.0, 0.0, 90.0)]
    [InlineData(0.0, 0.0, 0.0, -5.0, 180.0)]
    [InlineData(0.0, 0.0, -5.0, 0.0, -90.0)]
    [InlineData(1.0, 1.0, 4.0, 4.0, 45.0)]
    public void DirecaoPara_segue_a_convencao_do_jogo(
        double x,
        double z,
        double destinoX,
        double destinoZ,
        double esperado
    ) => Assert.Equal(esperado, RegrasInimigo.DirecaoPara(x, z, destinoX, destinoZ), Tolerancia);

    [Fact]
    public void DirecaoPara_no_mesmo_ponto_mantem_a_direcao_atual() =>
        Assert.Equal(137.0, RegrasInimigo.DirecaoPara(3, 3, 3, 3, 137.0));

    [Fact]
    public void Avancar_anda_a_velocidade_vezes_o_tempo_na_direcao_do_destino()
    {
        RegrasInimigo.Avancar(0, 0, 0, 20, 4.0, 0.5, 0.0, out double x, out double z);
        Assert.Equal(0.0, x, Tolerancia);
        Assert.Equal(2.0, z, Tolerancia);

        RegrasInimigo.Avancar(0, 0, 30, 40, 5.0, 1.0, 0.0, out x, out z);
        Assert.Equal(3.0, x, Tolerancia);
        Assert.Equal(4.0, z, Tolerancia);
    }

    [Fact]
    public void Avancar_nao_passa_do_destino()
    {
        RegrasInimigo.Avancar(0, 0, 1, 0, 4.0, 10.0, 0.0, out double x, out double z);
        Assert.Equal(1.0, x, Tolerancia);
        Assert.Equal(0.0, z, Tolerancia);
    }

    [Fact]
    public void Avancar_para_na_distancia_de_parada()
    {
        RegrasInimigo.Avancar(0, 0, 10, 0, 4.0, 10.0, 2.0, out double x, out double z);
        Assert.Equal(8.0, x, Tolerancia);
        Assert.Equal(0.0, z, Tolerancia);
    }

    [Theory]
    [InlineData(1.5, 0.0, 3.0, 0.0, 4.0, 1.0, 2.0)] // já está dentro da distância de parada
    [InlineData(5.0, 5.0, 5.0, 5.0, 4.0, 1.0, 0.0)] // já está no destino
    [InlineData(0.0, 0.0, 3.0, 0.0, -4.0, 1.0, 0.0)] // velocidade negativa
    [InlineData(0.0, 0.0, 3.0, 0.0, 4.0, -1.0, 0.0)] // tempo negativo
    [InlineData(0.0, 0.0, 3.0, 0.0, 4.0, 0.0, 0.0)] // tempo zero
    public void Avancar_fica_parado(
        double x,
        double z,
        double destinoX,
        double destinoZ,
        double velocidade,
        double segundos,
        double parada
    )
    {
        RegrasInimigo.Avancar(
            x,
            z,
            destinoX,
            destinoZ,
            velocidade,
            segundos,
            parada,
            out double novoX,
            out double novoZ
        );
        Assert.Equal(x, novoX, Tolerancia);
        Assert.Equal(z, novoZ, Tolerancia);
    }

    [Fact]
    public void Perseguicao_por_passos_fixos_chega_ao_alcance_de_ataque_e_para()
    {
        double x = 0.0;
        double z = 0.0;
        // 12 m de distância a 4 m/s, parando a 2 m: 10 m em 2,5 s, ou 25 passos.
        for (int passo = 0; passo < 25; passo++)
        {
            Assert.False(RegrasInimigo.NoAlcanceDeAtaque(RegrasInimigo.Distancia(x, z, 0, 12)));
            RegrasInimigo.Avancar(
                x,
                z,
                0,
                12,
                RegrasInimigo.VelocidadeDePerseguicaoMetrosPorSegundo,
                RegrasInimigo.PassoSegundos,
                RegrasInimigo.AlcanceDeAtaqueMetros,
                out x,
                out z
            );
        }
        Assert.Equal(10.0, z, 1e-6);
        double falta = RegrasInimigo.Distancia(x, z, 0, 12);
        Assert.Equal(RegrasInimigo.AlcanceDeAtaqueMetros, falta, 1e-6);
    }

    [Theory]
    [InlineData(0.0, 1.0, true)] // à frente
    [InlineData(0.0, 1.2, true)] // na borda do alcance
    [InlineData(0.0, 1.3, false)] // além do alcance
    [InlineData(0.0, -1.0, false)] // atrás do corpo
    [InlineData(0.8, 0.8, true)] // 45 graus, dentro do cone de 90
    [InlineData(1.0, 0.5, false)] // mais de 45 graus para o lado
    [InlineData(0.0, 0.0, true)] // em cima do inimigo
    public void EstaNoGolpe_e_um_cone_a_frente_do_corpo(
        double alvoX,
        double alvoZ,
        bool esperado
    ) => Assert.Equal(esperado, RegrasInimigo.EstaNoGolpe(0, 0, 0.0, alvoX, alvoZ));

    [Fact]
    public void EstaNoGolpe_segue_a_direcao_travada_e_o_corpo_do_alvo()
    {
        // Olhando para +X (90 graus): quem está a +X leva, quem está a +Z não.
        Assert.True(RegrasInimigo.EstaNoGolpe(0, 0, 90.0, 1.0, 0));
        Assert.False(RegrasInimigo.EstaNoGolpe(0, 0, 90.0, 0, 1.0));
        // O raio do alvo desconta da distância.
        Assert.False(RegrasInimigo.EstaNoGolpe(0, 0, 0.0, 0, 1.5));
        Assert.True(RegrasInimigo.EstaNoGolpe(0, 0, 0.0, 0, 1.5, 0.5));
    }

    [Fact]
    public void EstaNoGolpe_confere_com_a_conta_do_cone_de_combate()
    {
        // Mesma conta de RegrasAtaque.EstaNoVolume, só com o alcance e a abertura do inimigo.
        Assert.Equal(
            JogoDoGrupo.Shared.Features.Combate.RegrasAtaque.EstaNoVolume(0, 0, 0.0, 1.0, 0.5),
            RegrasInimigo.EstaNoGolpe(0, 0, 0.0, 1.0, 0.5)
        );
        Assert.Equal(
            JogoDoGrupo.Shared.Features.Combate.RegrasAtaque.AberturaDoConeGraus,
            RegrasInimigo.AberturaDoGolpeGraus
        );
    }
}
