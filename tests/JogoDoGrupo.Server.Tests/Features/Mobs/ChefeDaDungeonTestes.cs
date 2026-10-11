using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// O boss da dungeon sem rede: força fixa, três ataques telegrafados com 3 s de aviso, janela
/// exposta, morte uma vez só e sem volta sozinho. O boss fica na origem (0, 0).
/// </summary>
public class ChefeDaDungeonTestes
{
    private const string AreaDoBoss = "dungeon-1";
    private const int AvisoMs = 3000;

    private static readonly JogadorNoCampo Perto = new(1, 10, 0);

    private sealed class Luta
    {
        private long _ultimoId = 500;

        public Luta()
        {
            Boss = new ChefeDaDungeon(AreaDoBoss, 0, 0, () => ++_ultimoId, () => AgoraMs);
        }

        public long AgoraMs { get; set; } = 1_000_000;
        public ChefeDaDungeon Boss { get; }

        public PassoDosInimigos Passo(long avancoMs, params JogadorNoCampo[] jogadores)
        {
            AgoraMs += avancoMs;
            return Boss.Passo(jogadores);
        }

        /// <summary>Avança de 100 em 100 ms até o próximo aviso do tipo pedido.</summary>
        public InimigoPreparaAtaque AteOAviso(
            TipoDeAtaqueDeInimigo tipo,
            params JogadorNoCampo[] jogadores
        )
        {
            for (var i = 0; i < 2000; i++)
            {
                var passo = Passo(100, jogadores);
                foreach (var preparo in passo.Preparos)
                {
                    if (preparo.Tipo == tipo)
                    {
                        return preparo;
                    }
                }
            }

            throw new InvalidOperationException($"O aviso {tipo} nunca veio.");
        }

        /// <summary>Faz o aviso passar sem acertar (todos longe) e deixa a recuperação acabar.</summary>
        public PassoDosInimigos AteOAcerto(params JogadorNoCampo[] jogadores) =>
            Passo(AvisoMs, jogadores);

        public long Id => Boss.InimigoId;

        public ResultadoDeDanoTeste Bater(int dano, long atacante)
        {
            var ok = Boss.TentarAplicarDano(Id, dano, atacante, out var resultado);
            return new ResultadoDeDanoTeste(ok, resultado.Dano, resultado.VidaRestante);
        }
    }

    private readonly record struct ResultadoDeDanoTeste(bool Aplicou, int Dano, int VidaRestante);

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public void Tem_a_mesma_vida_com_1_ou_5_jogadores(int jogadores)
    {
        var luta = new Luta();
        var grupo = Enumerable
            .Range(1, jogadores)
            .Select(i => new JogadorNoCampo(i, 10 + i, 0))
            .ToArray();

        luta.Passo(100, grupo);
        luta.Passo(600, grupo);
        var retrato = Assert.Single(luta.Boss.Vivos());

        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, retrato.VidaMaxima);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, retrato.Vida);
        Assert.Equal(
            CatalogoCriaturas.Obter(ConfigChefeDaDungeon.CriaturaId).VidaBase,
            retrato.Vida
        );
        Assert.Equal(ConfigChefeDaDungeon.CriaturaId, retrato.CriaturaId);
    }

    [Fact]
    public void Sem_jogadores_fica_em_repouso_e_nao_ataca()
    {
        var luta = new Luta();

        for (var i = 0; i < 100; i++)
        {
            Assert.True(luta.Passo(100).Vazio);
        }

        Assert.Equal(EstadoDeInimigo.Repouso, Assert.Single(luta.Boss.Vivos()).Estado);
    }

    [Fact]
    public void Jogador_longe_demais_nao_desperta()
    {
        var luta = new Luta();
        var longe = new JogadorNoCampo(1, ConfigChefeDaDungeon.DistanciaDeDespertarMetros + 5, 0);

        for (var i = 0; i < 50; i++)
        {
            Assert.True(luta.Passo(100, longe).Vazio);
        }
    }

    [Fact]
    public void Os_tres_ataques_vem_na_ordem_com_3_segundos_de_aviso()
    {
        var luta = new Luta();

        var investida = luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        Assert.Equal(AvisoMs, investida.PreparoMs);
        luta.AteOAcerto(Perto);
        var raios = luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, Perto);
        Assert.Equal(AvisoMs, raios.PreparoMs);
        luta.AteOAcerto(Perto);
        var pisada = luta.AteOAviso(TipoDeAtaqueDeInimigo.Pisada, Perto);
        Assert.Equal(AvisoMs, pisada.PreparoMs);
        luta.AteOAcerto(Perto);

        // Depois do último recomeça do primeiro.
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
    }

    [Fact]
    public void Cada_ataque_so_acerta_depois_dos_3_segundos_de_aviso()
    {
        foreach (var tipo in ConfigChefeDaDungeon.OrdemDosAtaques)
        {
            var luta = new Luta();
            // Quem está no ponto marcado: todos ficam em cima do jogador, que o boss mira.
            var parado = new[] { Perto };
            for (
                var anterior = 0;
                anterior < Array.IndexOf(ConfigChefeDaDungeon.OrdemDosAtaques, tipo);
                anterior++
            )
            {
                luta.AteOAviso(ConfigChefeDaDungeon.OrdemDosAtaques[anterior], parado);
                luta.AteOAcerto(parado);
            }

            var aviso = luta.AteOAviso(tipo, parado);
            var golpes = 0;
            for (var ms = 100; ms < AvisoMs; ms += 100)
            {
                golpes += luta.Passo(100, parado).Golpes.Count;
            }

            Assert.Equal(0, golpes);
            var acerto = luta.Passo(100, parado);
            Assert.Single(acerto.Golpes);
            Assert.Equal(tipo, aviso.Tipo);
        }
    }

    [Fact]
    public void Investida_acerta_so_quem_esta_no_ponto_marcado_no_momento_do_acerto()
    {
        var luta = new Luta();
        var alvo = new JogadorNoCampo(1, 10, 0);
        var longe = new JogadorNoCampo(2, 10, 20);

        var aviso = luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, alvo, longe);

        // A área é a do ponto onde o alvo estava, fixa desde o aviso.
        var area = Assert.Single(aviso.Areas);
        Assert.Equal(10f, area.CentroX, 1);
        Assert.Equal(0f, area.CentroZ, 1);
        Assert.Equal((float)ConfigChefeDaDungeon.RaioDaInvestidaMetros, area.Raio);
        Assert.Equal(area.CentroX, aviso.CentroX);

        var acerto = luta.Passo(AvisoMs, alvo, longe);

        var golpe = Assert.Single(acerto.Golpes);
        Assert.Equal(alvo.PersonagemId, golpe.PersonagemId);
        Assert.Equal(ConfigChefeDaDungeon.DanoDaInvestida, golpe.Dano);
        Assert.Equal(luta.Id, golpe.InimigoId);
        Assert.Equal(ConfigChefeDaDungeon.CriaturaId, golpe.CriaturaId);
        Assert.Equal(AreaDoBoss, golpe.Area);
    }

    [Fact]
    public void Esquivar_saindo_da_area_antes_do_acerto_evita_o_dano()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);

        luta.Passo(2000, Perto);
        var esquivou = new JogadorNoCampo(Perto.PersonagemId, 20, 0);
        var acerto = luta.Passo(1000, esquivou);

        Assert.Empty(acerto.Golpes);
    }

    [Fact]
    public void Quem_entra_na_area_so_no_momento_do_acerto_leva_o_dano()
    {
        var luta = new Luta();
        var fora = new JogadorNoCampo(2, 10, 20);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto, fora);
        luta.Passo(2000, Perto, fora);

        var entrou = new JogadorNoCampo(2, 11, 1);
        var acerto = luta.Passo(1000, Perto, entrou);

        Assert.Equal([1L, 2L], acerto.Golpes.Select(g => g.PersonagemId).Order().ToArray());
    }

    [Fact]
    public void Investida_leva_o_boss_ate_o_ponto_marcado()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);

        var acerto = luta.Passo(AvisoMs, Perto);

        var mudanca = acerto.Mudancas.Last().Mensagem;
        Assert.Equal(10f, mudanca.X, 1);
        Assert.Equal(0f, mudanca.Z, 1);
    }

    [Fact]
    public void Investida_respeita_o_alcance_maximo()
    {
        var luta = new Luta();
        // Dentro do despertar (30 m), mas além do alcance da investida (25 m)? Aumenta com o 2º.
        var alvo = new JogadorNoCampo(1, ConfigChefeDaDungeon.DistanciaDeDespertarMetros, 0);

        var aviso = luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, alvo);

        var area = Assert.Single(aviso.Areas);
        Assert.True(area.CentroX <= (float)ConfigChefeDaDungeon.AlcanceDaInvestidaMetros + 0.01f);
    }

    [Fact]
    public void Pisada_acerta_em_volta_do_boss_e_so_quem_esta_dentro()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.AteOAcerto(Perto);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, Perto);
        luta.AteOAcerto(Perto);

        // Depois da investida o boss está onde estava o jogador 1: (10, 0).
        var colado = new JogadorNoCampo(1, 10, 0);
        var perto = new JogadorNoCampo(2, 14, 0);
        var longe = new JogadorNoCampo(3, 25, 0);
        var aviso = luta.AteOAviso(TipoDeAtaqueDeInimigo.Pisada, colado, perto, longe);

        var area = Assert.Single(aviso.Areas);
        Assert.Equal((float)ConfigChefeDaDungeon.RaioDaPisadaMetros, area.Raio);
        Assert.Equal(10f, area.CentroX, 1);

        var acerto = luta.Passo(AvisoMs, colado, perto, longe);

        Assert.Equal([1L, 2L], acerto.Golpes.Select(g => g.PersonagemId).ToArray());
        Assert.All(acerto.Golpes, g => Assert.Equal(ConfigChefeDaDungeon.DanoDaPisada, g.Dano));
    }

    [Fact]
    public void Raios_caem_em_jogadores_diferentes_e_acertam_cada_um_uma_vez()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.AteOAcerto(Perto);

        var a = new JogadorNoCampo(1, 10, 0);
        var b = new JogadorNoCampo(2, -20, 0);
        var aviso = luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, a, b);

        Assert.Equal(ConfigChefeDaDungeon.RaiosPorAtaque, aviso.Areas.Length);
        Assert.All(
            aviso.Areas,
            r => Assert.Equal((float)ConfigChefeDaDungeon.RaioDoRaioMetros, r.Raio)
        );
        Assert.Contains(
            aviso.Areas,
            r => Math.Abs(r.CentroX - 10f) < 0.1f && Math.Abs(r.CentroZ) < 0.1f
        );
        Assert.Contains(
            aviso.Areas,
            r => Math.Abs(r.CentroX + 20f) < 0.1f && Math.Abs(r.CentroZ) < 0.1f
        );

        var acerto = luta.Passo(AvisoMs, a, b);

        Assert.Equal([1L, 2L], acerto.Golpes.Select(g => g.PersonagemId).ToArray());
        Assert.All(acerto.Golpes, g => Assert.Equal(ConfigChefeDaDungeon.DanoDoRaio, g.Dano));
    }

    [Fact]
    public void Raio_nao_pega_quem_saiu_do_ponto_marcado()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.AteOAcerto(Perto);
        var a = new JogadorNoCampo(1, 10, 0);
        var b = new JogadorNoCampo(2, -20, 0);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, a, b);

        var acerto = luta.Passo(AvisoMs, new JogadorNoCampo(1, 10, 10), b);

        Assert.Equal([2L], acerto.Golpes.Select(g => g.PersonagemId).ToArray());
    }

    [Fact]
    public void Janela_exposta_multiplica_o_dano_so_enquanto_dura()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);

        // Durante o aviso ainda não está exposto.
        Assert.False(luta.Boss.Exposto);
        Assert.Equal(100, luta.Bater(100, 1).Dano);

        var acerto = luta.Passo(AvisoMs, Perto);

        var exposto = Assert.Single(acerto.Expostos);
        Assert.Equal(luta.Id, exposto.InimigoId);
        Assert.Equal(
            (int)(ConfigChefeDaDungeon.DuracaoDaJanelaExpostaSegundos * 1000),
            exposto.DuracaoMs
        );
        Assert.Equal((float)ConfigChefeDaDungeon.MultiplicadorDeDanoExposto, exposto.Multiplicador);
        Assert.True(luta.Boss.Exposto);
        Assert.Equal(
            (int)(100 * ConfigChefeDaDungeon.MultiplicadorDeDanoExposto),
            luta.Bater(100, 1).Dano
        );

        luta.Passo(exposto.DuracaoMs, Perto);

        Assert.False(luta.Boss.Exposto);
        Assert.Equal(100, luta.Bater(100, 1).Dano);
    }

    [Fact]
    public void Raios_nao_abrem_a_janela_exposta()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.AteOAcerto(Perto);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, Perto);

        var acerto = luta.Passo(AvisoMs, Perto);

        Assert.Empty(acerto.Expostos);
        Assert.False(luta.Boss.Exposto);
    }

    [Fact]
    public void Pisada_abre_a_janela_exposta()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.AteOAcerto(Perto);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Raios, Perto);
        luta.AteOAcerto(Perto);
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Pisada, Perto);

        var acerto = luta.Passo(AvisoMs, Perto);

        Assert.Single(acerto.Expostos);
        Assert.True(luta.Boss.Exposto);
    }

    [Fact]
    public void Dano_nunca_passa_da_vida_que_sobra()
    {
        var luta = new Luta();

        var resultado = luta.Bater(ConfigChefeDaDungeon.VidaMaxima + 999, 1);

        Assert.True(resultado.Aplicou);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, resultado.Dano);
        Assert.Equal(0, resultado.VidaRestante);
    }

    [Fact]
    public void Dano_zero_ou_negativo_e_recusado()
    {
        var luta = new Luta();

        Assert.False(luta.Bater(0, 1).Aplicou);
        Assert.False(luta.Bater(-5, 1).Aplicou);
        Assert.False(luta.Boss.TentarAplicarDano(luta.Id + 1, 10, 1, out _));
    }

    [Fact]
    public void Morte_publica_InimigoDerrotado_uma_vez_so_com_quem_causou_dano()
    {
        var luta = new Luta();
        var jogadores = new[]
        {
            new JogadorNoCampo(7, 10, 0),
            new JogadorNoCampo(3, 12, 0),
            new JogadorNoCampo(5, 14, 0),
        };
        luta.Passo(100, jogadores);

        luta.Bater(1000, 7);
        luta.Bater(1000, 3);
        luta.Bater(1000, 7);
        Assert.False(luta.Boss.Morto);
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 7);

        Assert.True(luta.Boss.Morto);
        var passo = luta.Passo(100, jogadores);
        var derrota = Assert.Single(passo.Derrotas);
        Assert.Equal([3L, 7L], derrota.Participantes.ToArray());
        Assert.DoesNotContain(5L, derrota.Participantes);
        Assert.Equal(luta.Id, derrota.InimigoId);
        Assert.Equal(ConfigChefeDaDungeon.CriaturaId, derrota.CriaturaId);
        Assert.Equal(AreaDoBoss, derrota.Area);
        Assert.Equal(VarianteDeCriatura.Normal, derrota.Variante);
        Assert.Equal(
            CatalogoCriaturas.Obter(ConfigChefeDaDungeon.CriaturaId).ExperienciaBase,
            derrota.Experiencia
        );
        Assert.Equal(1.0, derrota.MultiplicadorDeRecompensa);
        Assert.Equal(1L, derrota.Sequencia);
        Assert.Single(passo.Mortes);

        // Nenhum passo seguinte repete a morte, e golpes novos não acham alvo.
        for (var i = 0; i < 20; i++)
        {
            var depois = luta.Passo(100, jogadores);
            Assert.Empty(depois.Derrotas);
            Assert.Empty(depois.Mortes);
        }

        Assert.False(luta.Boss.TentarAplicarDano(luta.Id, 10, 5, out _));
    }

    [Fact]
    public void Morte_no_meio_do_aviso_cancela_o_ataque()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 1);

        var passo = luta.Passo(AvisoMs, Perto);

        Assert.Empty(passo.Golpes);
        Assert.Single(passo.Derrotas);
    }

    [Fact]
    public void Boss_morto_nao_volta_sozinho()
    {
        var luta = new Luta();
        luta.Passo(100, Perto);
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 1);
        luta.Passo(100, Perto);
        var idDaMorte = luta.Id;

        // Uma hora depois, ainda morto.
        var passo = luta.Passo(3_600_000, Perto);

        Assert.True(passo.Vazio);
        Assert.Empty(luta.Boss.Vivos());
        Assert.True(luta.Boss.Morto);
        Assert.Equal(idDaMorte, luta.Id);
    }

    [Fact]
    public void Reiniciar_traz_o_boss_de_volta_com_id_novo_e_vida_cheia()
    {
        var luta = new Luta();
        luta.Passo(100, Perto);
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 1);
        var passoDaMorte = luta.Passo(100, Perto);
        var idAntigo = luta.Id;

        var idNovo = luta.Boss.Reiniciar();

        Assert.NotEqual(idAntigo, idNovo);
        Assert.Equal(idNovo, luta.Id);
        var retrato = Assert.Single(luta.Boss.Vivos());
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, retrato.Vida);
        Assert.Equal(EstadoDeInimigo.Repouso, retrato.Estado);
        var passo = luta.Passo(100);
        var aparicao = Assert.Single(passo.Aparicoes);
        Assert.Equal(idNovo, aparicao.Inimigo.InimigoId);
        Assert.False(luta.Boss.TentarAplicarDano(idAntigo, 10, 1, out _));

        // A segunda morte é outra, com a lista de participantes do zero.
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 9);
        var segunda = Assert.Single(luta.Passo(100, Perto).Derrotas);
        Assert.Equal([9L], segunda.Participantes.ToArray());
        Assert.Equal(2L, segunda.Sequencia);
        Assert.Single(passoDaMorte.Derrotas);
    }

    [Fact]
    public void Sala_vazia_faz_o_boss_voltar_curar_e_esquecer_os_participantes()
    {
        var luta = new Luta();
        luta.Passo(100, Perto);
        luta.Bater(2000, 1);

        // Ninguém na sala: ele larga a luta, volta à origem e recupera a vida.
        for (var i = 0; i < 100; i++)
        {
            luta.Passo(100);
        }

        var retrato = Assert.Single(luta.Boss.Vivos());
        Assert.Equal(EstadoDeInimigo.Repouso, retrato.Estado);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, retrato.Vida);

        var outro = new JogadorNoCampo(4, 10, 0);
        luta.Passo(100, outro);
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 4);
        var derrota = Assert.Single(luta.Passo(100, outro).Derrotas);
        Assert.Equal([4L], derrota.Participantes.ToArray());
    }

    [Fact]
    public void Ataque_comecado_vai_ate_o_fim_mesmo_se_a_sala_esvaziar()
    {
        var luta = new Luta();
        luta.AteOAviso(TipoDeAtaqueDeInimigo.Investida, Perto);

        // Todos saem: o golpe acontece (sem ninguém para pegar) e só depois ele volta.
        var passo = luta.Passo(AvisoMs);

        Assert.Empty(passo.Golpes);
        Assert.Single(passo.Expostos);
    }

    [Fact]
    public void Alvo_dos_golpes_so_na_area_do_boss_e_no_cone()
    {
        var luta = new Luta();

        var certo = luta.Boss.AlvosNoVolume(AreaDoBoss, -2, 0, 90);
        var outraArea = luta.Boss.AlvosNoVolume("cidade", -2, 0, 90);
        var costas = luta.Boss.AlvosNoVolume(AreaDoBoss, -2, 0, 270);

        var alvo = Assert.Single(certo);
        Assert.Equal(luta.Id, alvo.Id);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, alvo.VidaAtual);
        Assert.Equal(ConfigChefeDaDungeon.VidaMaxima, alvo.VidaMaxima);
        Assert.Empty(outraArea);
        Assert.Empty(costas);
    }

    [Fact]
    public void Boss_morto_nao_e_alvo()
    {
        var luta = new Luta();
        luta.Bater(ConfigChefeDaDungeon.VidaMaxima, 1);

        Assert.Empty(luta.Boss.AlvosNoVolume(AreaDoBoss, -2, 0, 90));
    }

    [Fact]
    public void Sequencia_das_mudancas_so_cresce()
    {
        var luta = new Luta();
        var sequencias = new List<int>();

        for (var i = 0; i < 80; i++)
        {
            sequencias.AddRange(luta.Passo(100, Perto).Mudancas.Select(m => m.Mensagem.Sequencia));
        }

        Assert.NotEmpty(sequencias);
        Assert.Equal(sequencias.Order().ToArray(), sequencias.ToArray());
        Assert.Equal(sequencias.Count, sequencias.Distinct().Count());
    }

    [Fact]
    public void Construtor_recusa_argumentos_invalidos()
    {
        Assert.Throws<ArgumentException>(() => new ChefeDaDungeon("", 0, 0, () => 1));
        Assert.Throws<ArgumentNullException>(() => new ChefeDaDungeon("a", 0, 0, null!));
    }

    [Fact]
    public void Config_tem_aviso_de_3_segundos_e_numeros_validos()
    {
        Assert.Equal(3.0, ConfigChefeDaDungeon.AvisoDoAtaqueSegundos);
        Assert.True(ConfigChefeDaDungeon.MultiplicadorDeDanoExposto > 1.0);
        Assert.All(
            ConfigChefeDaDungeon.OrdemDosAtaques,
            t =>
            {
                Assert.True(ConfigChefeDaDungeon.DanoDe(t) > 0);
                Assert.True(ConfigChefeDaDungeon.RecuperacaoDe(t) > 0);
            }
        );
        Assert.Contains(TipoDeAtaqueDeInimigo.Investida, ConfigChefeDaDungeon.OrdemDosAtaques);
        Assert.Contains(TipoDeAtaqueDeInimigo.Pisada, ConfigChefeDaDungeon.OrdemDosAtaques);
        Assert.Contains(TipoDeAtaqueDeInimigo.Raios, ConfigChefeDaDungeon.OrdemDosAtaques);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConfigChefeDaDungeon.DanoDe(TipoDeAtaqueDeInimigo.Golpe)
        );
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ConfigChefeDaDungeon.RecuperacaoDe(TipoDeAtaqueDeInimigo.Golpe)
        );
    }
}
