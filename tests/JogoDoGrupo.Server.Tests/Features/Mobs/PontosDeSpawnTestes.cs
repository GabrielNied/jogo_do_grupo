using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>Pontos de spawn, faixa de nível, tamanho do grupo, escala por nível e teto de vivos.</summary>
public class PontosDeSpawnTestes
{
    private const long Jogador = 9;

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    /// <summary>Sempre a variante normal; os números de "Proximo" saem da fila do teste.</summary>
    private sealed class Sorteador : ISorteadorDeCriaturas
    {
        private readonly Queue<double> _fila = new();

        public double Padrao { get; set; }

        public void Enfileirar(params double[] valores)
        {
            foreach (var valor in valores)
            {
                _fila.Enqueue(valor);
            }
        }

        public double Proximo() => _fila.Count > 0 ? _fila.Dequeue() : Padrao;

        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                definicao.VidaBase,
                definicao.DanoBase,
                definicao.ExperienciaBase,
                1.0,
                1f,
                definicao.Nivel
            );

        public bool Aparece(DefinicaoDeCriatura definicao) => true;
    }

    private sealed class Relogio
    {
        public long Ms { get; set; } = 10_000;
    }

    private static GrupoDeInimigos Grupo(
        Sorteador sorteador,
        Relogio relogio,
        int tamanhoMaximo,
        int? tamanhoMinimo = null,
        int nivelMinimo = 0,
        int nivelMaximo = 0,
        LimiteDeInimigosVivos? limite = null,
        long primeiroId = ConfigInimigosDoCampo.PrimeiroInimigoId,
        DefinicaoDeCriatura? ficha = null,
        double x = 0,
        double z = 0
    ) =>
        new(
            ficha ?? Javali,
            sorteador,
            tamanhoMaximo,
            () => relogio.Ms,
            pontoX: x,
            pontoZ: z,
            primeiroId: primeiroId,
            tamanhoMinimo: tamanhoMinimo,
            nivelMinimo: nivelMinimo,
            nivelMaximo: nivelMaximo,
            limite: limite
        );

    private static void MatarTodos(GrupoDeInimigos grupo)
    {
        foreach (var vivo in grupo.Vivos())
        {
            Assert.True(grupo.TentarAplicarDano(vivo.InimigoId, 1_000_000, Jogador, out _));
        }
    }

    // ── Dados dos pontos ─────────────────────────────────────────────────────

    [Fact]
    public void Ha_varios_pontos_com_ids_e_nomes_unicos_e_faixas_validas()
    {
        var pontos = PontosDeSpawnDoCampo.Todos;

        Assert.True(pontos.Count >= 3);
        Assert.Equal(pontos.Count, pontos.Select(p => p.Nome).Distinct().Count());
        Assert.Equal(pontos.Count, pontos.Select(p => p.PrimeiroId).Distinct().Count());
        Assert.All(
            pontos,
            p =>
            {
                Assert.NotNull(CatalogoCriaturas.Obter(p.CriaturaId));
                Assert.Equal(
                    CategoriaDeCriatura.Comum,
                    CatalogoCriaturas.Obter(p.CriaturaId).Categoria
                );
                Assert.InRange(p.NivelMinimo, 1, p.NivelMaximo);
                Assert.InRange(p.TamanhoMinimo, 1, p.TamanhoMaximo);
                Assert.True(
                    p.PrimeiroId + PontosDeSpawnDoCampo.EspacoDeIdsPorPonto
                        <= ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro
                        || p.PrimeiroId > ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro,
                    "ids do ponto invadem os do chefe raro"
                );
            }
        );
    }

    [Fact]
    public void Quanto_mais_longe_da_cidade_mais_forte_e_o_ponto()
    {
        var porDistancia = PontosDeSpawnDoCampo
            .Todos.OrderBy(p => RegrasInimigo.Distancia(0, 0, p.X, p.Z))
            .ToArray();

        for (var i = 1; i < porDistancia.Length; i++)
        {
            Assert.True(
                porDistancia[i].NivelMinimo >= porDistancia[i - 1].NivelMinimo,
                $"{porDistancia[i].Nome} é mais longe e não pode ser mais fraco que {porDistancia[i - 1].Nome}"
            );
        }

        Assert.True(porDistancia[^1].NivelMinimo > porDistancia[0].NivelMaximo);
    }

    [Fact]
    public void Os_pontos_ficam_em_terra_firme_longe_do_chefe_raro()
    {
        Assert.All(
            PontosDeSpawnDoCampo.Todos,
            p =>
            {
                var altura = MapaDoMundo.AlturaEm((float)p.X, (float)p.Z);
                Assert.True(altura > MapaDoMundo.NivelDaAgua, $"{p.Nome} cai na água ou na lava");
                var doChefe = RegrasInimigo.Distancia(
                    p.X,
                    p.Z,
                    ConfigInimigosDoCampo.PontoDoChefeRaroX,
                    ConfigInimigosDoCampo.PontoDoChefeRaroZ
                );
                Assert.True(doChefe > 2 * RegrasInimigo.DistanciaDeVisaoMetros);
            }
        );
    }

    [Fact]
    public void O_primeiro_ponto_e_o_campo_original_ao_norte_do_portao()
    {
        var primeiro = PontosDeSpawnDoCampo.Todos[0];

        Assert.Equal(ConfigInimigosDoCampo.PontoX, primeiro.X);
        Assert.Equal(ConfigInimigosDoCampo.PontoZ, primeiro.Z);
        Assert.Equal(ConfigInimigosDoCampo.PrimeiroInimigoId, primeiro.PrimeiroId);
    }

    // ── Espécies ─────────────────────────────────────────────────────────────

    [Fact]
    public void As_tres_especies_comuns_tem_comportamento_distinto()
    {
        var javali = ComportamentoDeCriatura.Obter(CatalogoCriaturas.JavaliDoCampo);
        var lobo = ComportamentoDeCriatura.Obter(CatalogoCriaturas.LoboDoCampo);
        var tatu = ComportamentoDeCriatura.Obter(CatalogoCriaturas.TatuRochoso);

        Assert.Equal(ComportamentoDeCriatura.Padrao, javali);
        Assert.Equal(3, new[] { javali, lobo, tatu }.Distinct().Count());
        Assert.True(lobo.VisaoMetros > javali.VisaoMetros && javali.VisaoMetros > tatu.VisaoMetros);
        Assert.True(
            lobo.VelocidadeDePerseguicaoMetrosPorSegundo
                > javali.VelocidadeDePerseguicaoMetrosPorSegundo
        );
        Assert.True(
            javali.VelocidadeDePerseguicaoMetrosPorSegundo
                > tatu.VelocidadeDePerseguicaoMetrosPorSegundo
        );
        Assert.True(lobo.PreparoSegundos < tatu.PreparoSegundos);
    }

    [Fact]
    public void Lobo_ve_o_jogador_a_14_m_e_o_javali_e_o_tatu_nao()
    {
        foreach (
            var (criatura, via) in new[]
            {
                (CatalogoCriaturas.LoboDoCampo, true),
                (CatalogoCriaturas.JavaliDoCampo, false),
                (CatalogoCriaturas.TatuRochoso, false),
            }
        )
        {
            var relogio = new Relogio();
            var grupo = Grupo(
                new Sorteador(),
                relogio,
                1,
                ficha: CatalogoCriaturas.Obter(criatura)
            );
            // O inimigo único nasce 4 m ao norte do centro: (0, 4).
            relogio.Ms += 100;
            var passo = grupo.Passo([new JogadorNoCampo(Jogador, 0, 18)]);

            Assert.Equal(via, passo.Mudancas.Count > 0);
        }
    }

    [Fact]
    public void O_aviso_do_golpe_segue_o_ritmo_da_especie()
    {
        foreach (
            var criatura in new[] { CatalogoCriaturas.LoboDoCampo, CatalogoCriaturas.TatuRochoso }
        )
        {
            var relogio = new Relogio();
            var grupo = Grupo(
                new Sorteador(),
                relogio,
                1,
                ficha: CatalogoCriaturas.Obter(criatura)
            );
            var jogador = new JogadorNoCampo(Jogador, 0, 6);
            InimigoPreparaAtaque? preparo = null;
            for (var i = 0; i < 200 && preparo == null; i++)
            {
                relogio.Ms += 100;
                preparo = grupo.Passo([jogador]).Preparos.FirstOrDefault();
            }

            Assert.NotNull(preparo);
            Assert.Equal(ComportamentoDeCriatura.Obter(criatura).PreparoMs, preparo.PreparoMs);
        }
    }

    // ── Nível ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(3, 1.0)]
    [InlineData(4, 1.15)]
    [InlineData(8, 1.75)]
    [InlineData(1, 0.7)]
    [InlineData(-50, ConfigNivelDeMobs.MultiplicadorMinimo)]
    public void Vida_pelo_nivel_cresce_por_nivel_acima_da_ficha_e_tem_piso(
        int nivel,
        double multiplicador
    )
    {
        var esperado = (int)Math.Round(180 * multiplicador, MidpointRounding.AwayFromZero);

        Assert.Equal(esperado, EscalaPorNivel.Vida(180, nivel, 3));
    }

    [Fact]
    public void Vida_dano_e_experiencia_crescem_com_o_nivel_e_na_ficha_sao_os_da_ficha()
    {
        Assert.Equal(40, EscalaPorNivel.Dano(40, 3, 3));
        Assert.Equal(20, EscalaPorNivel.Experiencia(20, 3, 3));
        Assert.Equal(60, EscalaPorNivel.Dano(40, 8, 3)); // +10% por nível, 5 níveis
        Assert.Equal(40, EscalaPorNivel.Experiencia(20, 8, 3)); // +20% por nível
        Assert.True(EscalaPorNivel.Dano(40, 9, 3) > EscalaPorNivel.Dano(40, 8, 3));
        Assert.Equal(1, EscalaPorNivel.Dano(1, 1, 3));
    }

    [Fact]
    public void Chefe_e_chefe_raro_nao_escalam_com_o_nivel()
    {
        var chefe = CatalogoCriaturas.Obter(CatalogoCriaturas.GuardiaoDoReflexo);
        var criatura = new Sorteador().Sortear(chefe);

        var escalada = EscalaPorNivel.Aplicar(criatura, 40);

        Assert.Equal(chefe.VidaBase, escalada.VidaMaxima);
        Assert.Equal(chefe.DanoBase, escalada.Dano);
        Assert.Equal(chefe.ExperienciaBase, escalada.Experiencia);
        Assert.Equal(chefe.Nivel, escalada.Nivel);
    }

    [Theory]
    [InlineData(0.0, 2)]
    [InlineData(0.33, 2)]
    [InlineData(0.34, 3)]
    [InlineData(0.99, 4)]
    [InlineData(1.0, 4)]
    [InlineData(-1.0, 2)]
    public void Sortear_na_faixa_distribui_igual_e_limita(double sorteio, int esperado) =>
        Assert.Equal(esperado, EscalaPorNivel.Sortear(2, 4, sorteio));

    [Fact]
    public void Faixa_de_um_so_valor_nao_gasta_sorteio()
    {
        Assert.Equal(5, EscalaPorNivel.Sortear(5, 5, 0.7));
    }

    [Fact]
    public void O_mob_nasce_com_nivel_da_faixa_e_vida_dano_e_xp_escalados()
    {
        var sorteador = new Sorteador();
        sorteador.Enfileirar(0.99); // o nível: o maior da faixa
        var relogio = new Relogio();
        var grupo = Grupo(sorteador, relogio, 1, nivelMinimo: 5, nivelMaximo: 8);

        var inimigo = Assert.Single(grupo.Vivos());

        Assert.Equal(8, inimigo.Nivel);
        Assert.Equal(EscalaPorNivel.Vida(Javali.VidaBase, 8, Javali.Nivel), inimigo.VidaMaxima);
        Assert.True(inimigo.VidaMaxima > Javali.VidaBase);
    }

    [Fact]
    public void Sem_faixa_o_mob_usa_o_nivel_da_ficha_e_os_valores_dela()
    {
        var grupo = Grupo(new Sorteador(), new Relogio(), 1);

        var inimigo = Assert.Single(grupo.Vivos());

        Assert.Equal(Javali.Nivel, inimigo.Nivel);
        Assert.Equal(Javali.VidaBase, inimigo.VidaMaxima);
    }

    [Fact]
    public void Dano_do_golpe_e_experiencia_da_morte_contam_o_nivel()
    {
        var sorteador = new Sorteador { Padrao = 0.99 };
        var relogio = new Relogio();
        var grupo = Grupo(sorteador, relogio, 1, nivelMinimo: 8, nivelMaximo: 8);
        var inimigo = Assert.Single(grupo.Vivos());

        // Um jogador perto: o inimigo ataca e o golpe pega quem fica parado na área.
        var golpes = new List<Server.Contratos.Mobs.GolpeDeInimigoAcertou>();
        var jogador = new JogadorNoCampo(Jogador, inimigo.X, inimigo.Z + 1.5);
        for (var i = 0; i < 60 && golpes.Count == 0; i++)
        {
            relogio.Ms += 100;
            golpes.AddRange(grupo.Passo([jogador]).Golpes);
        }

        var golpe = Assert.Single(golpes);
        Assert.Equal(EscalaPorNivel.Dano(Javali.DanoBase, 8, Javali.Nivel), golpe.Dano);

        Assert.True(grupo.TentarAplicarDano(inimigo.InimigoId, 1_000_000, Jogador, out _));
        var derrota = Assert.Single(grupo.Passo([jogador]).Derrotas);
        Assert.Equal(8, derrota.Nivel);
        Assert.Equal(
            EscalaPorNivel.Experiencia(Javali.ExperienciaBase, 8, Javali.Nivel),
            derrota.Experiencia
        );
    }

    [Fact]
    public void Cada_reaparicao_sorteia_o_nivel_de_novo()
    {
        var sorteador = new Sorteador();
        var relogio = new Relogio();
        sorteador.Enfileirar(0.0); // 1º nível: o menor
        var grupo = Grupo(sorteador, relogio, 1, nivelMinimo: 5, nivelMaximo: 8);
        Assert.Equal(5, Assert.Single(grupo.Vivos()).Nivel);
        MatarTodos(grupo);
        grupo.Passo([]);

        sorteador.Enfileirar(0.99); // 2º nível: o maior
        relogio.Ms += Javali.IntervaloDeAparicaoSegundos * 1000L;
        grupo.Passo([]);

        Assert.Equal(8, Assert.Single(grupo.Vivos()).Nivel);
    }

    // ── Tamanho do grupo ─────────────────────────────────────────────────────

    [Fact]
    public void O_tamanho_do_grupo_e_sorteado_na_faixa_do_ponto()
    {
        foreach (var (sorteio, esperado) in new[] { (0.0, 3), (0.5, 4), (0.99, 5) })
        {
            var sorteador = new Sorteador();
            sorteador.Enfileirar(sorteio);

            var grupo = Grupo(sorteador, new Relogio(), 5, tamanhoMinimo: 3);

            Assert.Equal(esperado, grupo.Vivos().Length);
            Assert.Equal(5, grupo.Capacidade);
        }
    }

    [Fact]
    public void O_tamanho_e_sorteado_de_novo_a_cada_reaparicao_do_grupo()
    {
        var sorteador = new Sorteador();
        var relogio = new Relogio();
        sorteador.Enfileirar(0.0); // 3 inimigos
        var grupo = Grupo(sorteador, relogio, 5, tamanhoMinimo: 3);
        Assert.Equal(3, grupo.Vivos().Length);
        var idsAntes = grupo.Vivos().Select(i => i.InimigoId).ToArray();

        MatarTodos(grupo);
        Assert.Equal(3, grupo.Passo([]).Mortes.Count);
        Assert.Empty(grupo.Vivos());

        sorteador.Enfileirar(0.99); // 5 inimigos
        relogio.Ms += Javali.IntervaloDeAparicaoSegundos * 1000L;
        var volta = grupo.Passo([]);

        Assert.Equal(5, volta.Aparicoes.Count);
        Assert.Equal(5, grupo.Vivos().Length);
        Assert.Empty(idsAntes.Intersect(grupo.Vivos().Select(i => i.InimigoId)));
    }

    [Fact]
    public void Sem_sobrevivente_o_grupo_so_volta_inteiro_depois_do_intervalo_da_ultima_morte()
    {
        var sorteador = new Sorteador();
        var relogio = new Relogio();
        var grupo = Grupo(sorteador, relogio, 3);
        var ids = grupo.Vivos().Select(i => i.InimigoId).ToArray();
        var intervalo = Javali.IntervaloDeAparicaoSegundos * 1000L;

        Assert.True(grupo.TentarAplicarDano(ids[0], 1_000_000, Jogador, out _));
        Assert.True(grupo.TentarAplicarDano(ids[1], 1_000_000, Jogador, out _));
        Assert.True(grupo.TentarAplicarDano(ids[2], 1_000_000, Jogador, out _));
        relogio.Ms += intervalo - 1;
        Assert.Empty(grupo.Passo([]).Aparicoes);
        relogio.Ms += 1;
        Assert.Equal(3, grupo.Passo([]).Aparicoes.Count);
    }

    [Fact]
    public void Tamanho_fixo_nao_gasta_sorteio_de_tamanho()
    {
        var sorteador = new Sorteador();
        sorteador.Enfileirar(0.99);

        var grupo = Grupo(sorteador, new Relogio(), 4);

        Assert.Equal(4, grupo.Vivos().Length);
        Assert.Equal(0.99, sorteador.Proximo()); // continua na fila
    }

    [Fact]
    public void Tamanho_minimo_maior_que_o_maximo_e_recusado()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Grupo(new Sorteador(), new Relogio(), 3, tamanhoMinimo: 4)
        );
    }

    // ── Teto de inimigos vivos ───────────────────────────────────────────────

    [Fact]
    public void O_teto_corta_o_grupo_e_a_vaga_volta_quando_um_morre()
    {
        var limite = new LimiteDeInimigosVivos(2);
        var relogio = new Relogio();

        var grupo = Grupo(new Sorteador(), relogio, 4, limite: limite);

        Assert.Equal(2, grupo.Vivos().Length);
        Assert.Equal(2, limite.Vivos);

        MatarTodos(grupo);
        Assert.Equal(0, limite.Vivos);
    }

    [Fact]
    public void Grupos_dividem_o_teto_e_o_que_ficou_sem_vaga_tenta_de_novo()
    {
        var limite = new LimiteDeInimigosVivos(5);
        var relogio = new Relogio();
        var sorteador = new Sorteador();
        var primeiro = Grupo(sorteador, relogio, 3, limite: limite);
        var segundo = Grupo(
            sorteador,
            relogio,
            3,
            limite: limite,
            primeiroId: ConfigInimigosDoCampo.PrimeiroInimigoId + 1000
        );
        var terceiro = Grupo(
            sorteador,
            relogio,
            3,
            limite: limite,
            primeiroId: ConfigInimigosDoCampo.PrimeiroInimigoId + 2000
        );

        Assert.Equal(3, primeiro.Vivos().Length);
        Assert.Equal(2, segundo.Vivos().Length);
        Assert.Empty(terceiro.Vivos()); // sem vaga
        Assert.Equal(5, limite.Vivos);

        // O terceiro tenta de novo: ainda não há vaga.
        relogio.Ms += (long)(ConfigInimigosDoCampo.TentativaDeVagaSegundos * 1000);
        Assert.Empty(terceiro.Passo([]).Aparicoes);

        MatarTodos(primeiro);
        primeiro.Passo([]);
        relogio.Ms += (long)(ConfigInimigosDoCampo.TentativaDeVagaSegundos * 1000);
        var volta = terceiro.Passo([]);

        Assert.Equal(3, volta.Aparicoes.Count);
        Assert.Equal(3, terceiro.Vivos().Length);
        Assert.True(limite.Vivos <= limite.Maximo);
    }

    [Fact]
    public void Inimigo_de_teste_e_chefe_raro_nao_ocupam_o_teto()
    {
        var limite = new LimiteDeInimigosVivos(1);
        var grupo = Grupo(new Sorteador(), new Relogio(), 1, limite: limite);

        Assert.NotNull(grupo.ForcarAparicao(10, 10));

        Assert.Equal(1, limite.Vivos);
        Assert.Equal(2, grupo.Vivos().Length);
    }

    [Fact]
    public void O_teto_nunca_e_passado_com_pedidos_simultaneos()
    {
        var limite = new LimiteDeInimigosVivos(10);
        var concedidas = 0;

        Parallel.For(
            0,
            200,
            _ =>
            {
                Interlocked.Add(ref concedidas, limite.Reservar(3));
            }
        );

        Assert.Equal(10, concedidas);
        Assert.Equal(10, limite.Vivos);
    }

    [Fact]
    public void Reservar_pede_zero_ou_negativo_nao_concede_nada()
    {
        var limite = new LimiteDeInimigosVivos(10);

        Assert.Equal(0, limite.Reservar(0));
        Assert.Equal(0, limite.Reservar(-3));
        Assert.Equal(0, limite.Vivos);
    }

    [Fact]
    public void No_conjunto_do_campo_o_teto_cobre_todos_os_pontos_juntos()
    {
        var limite = new LimiteDeInimigosVivos(PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
        var sorteador = new Sorteador { Padrao = 0.99 }; // sempre o maior grupo e o maior nível
        var grupos = PontosDeSpawnDoCampo
            .Todos.Select(p => GrupoDeInimigos.DoPonto(p, sorteador, limite))
            .ToArray();

        var vivos = grupos.Sum(g => g.Vivos().Length);

        Assert.True(PontosDeSpawnDoCampo.Todos.Sum(p => p.TamanhoMaximo) > limite.Maximo);
        Assert.Equal(limite.Maximo, vivos); // o teto age: os últimos da fila esperam vaga
        Assert.Equal(vivos, limite.Vivos);
        // Cada grupo nasce no próprio ponto, com a espécie e o nível dele.
        for (var i = 0; i < grupos.Length; i++)
        {
            var ponto = PontosDeSpawnDoCampo.Todos[i];
            Assert.All(
                grupos[i].Vivos(),
                v =>
                {
                    Assert.Equal(ponto.CriaturaId, v.CriaturaId);
                    Assert.Equal(ponto.NivelMaximo, v.Nivel);
                    Assert.Equal(
                        ponto.X,
                        v.X,
                        ConfigInimigosDoCampo.RaioDoEspalhamentoMetros + 0.01
                    );
                }
            );
        }
    }

    // ── Checklist de experiência ─────────────────────────────────────────────

    [Fact]
    public void Muitos_jogadores_no_mesmo_grupo_nao_duplicam_golpes_nem_derrotas()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador(), relogio, 3);
        var jogadores = Enumerable
            .Range(1, 8)
            .Select(i => new JogadorNoCampo(i, 0, 8 + i * 0.1))
            .ToArray();

        for (var i = 0; i < 100; i++)
        {
            relogio.Ms += 100;
            grupo.Passo(jogadores);
        }

        foreach (var vivo in grupo.Vivos())
        {
            for (var j = 1; j <= 8; j++)
            {
                grupo.TentarAplicarDano(vivo.InimigoId, 20, j, out _);
            }
        }

        var derrotas = grupo.Passo(jogadores).Derrotas;
        Assert.All(
            derrotas,
            d => Assert.Equal(d.Participantes.Distinct().Count(), d.Participantes.Count)
        );
        Assert.Equal(derrotas.Count, derrotas.Select(d => d.InimigoId).Distinct().Count());
    }

    [Fact]
    public void Quem_perde_o_alvo_por_queda_volta_ao_ponto_sem_perseguir_ninguem()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador(), relogio, 2);
        var jogador = new JogadorNoCampo(Jogador, 0, 12);
        for (var i = 0; i < 10; i++)
        {
            relogio.Ms += 100;
            grupo.Passo([jogador]);
        }

        // O jogador cai da área (desconexão): o grupo esquece e volta ao repouso.
        for (var i = 0; i < 400; i++)
        {
            relogio.Ms += 100;
            grupo.Passo([]);
        }

        Assert.All(
            grupo.Vivos(),
            v =>
            {
                Assert.Equal(EstadoDeInimigo.Repouso, v.Estado);
                Assert.Equal(RegrasInimigo.SemAlvo, v.AlvoId);
            }
        );
    }

    [Fact]
    public void Muitos_grupos_avancam_um_passo_dentro_do_orcamento_do_laco()
    {
        // O laço roda 10 vezes por segundo: com todos os pontos e jogadores por perto, um passo
        // tem que caber folgado nos 100 ms.
        var relogio = new Relogio();
        var sorteador = new Sorteador { Padrao = 0.99 };
        var limite = new LimiteDeInimigosVivos(PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
        var grupos = PontosDeSpawnDoCampo
            .Todos.Select(p => GrupoDeInimigos.DoPonto(p, sorteador, limite, () => relogio.Ms))
            .ToArray();
        var jogadores = Enumerable
            .Range(1, 20)
            .Select(i => new JogadorNoCampo(i, i * 3, 190))
            .ToArray();

        var inicio = System.Diagnostics.Stopwatch.StartNew();
        for (var passo = 0; passo < 50; passo++)
        {
            relogio.Ms += 100;
            foreach (var grupo in grupos)
            {
                grupo.Passo(jogadores);
            }
        }

        Assert.True(inicio.ElapsedMilliseconds / 50.0 < 50, "um passo do laço passou de 50 ms");
    }

    private sealed class RelogioDoMundoFalso : IRelogioDoMundo
    {
        public float HoraAtual { get; set; } = 12f;
        public bool Noite { get; set; } = false;

        public float Hora() => HoraAtual;

        public bool EhNoite() => Noite;
    }

    [Fact]
    public void Ao_virar_a_noite_os_pontos_noturnos_passam_a_repor_so_especies_da_noite_e_os_de_dia_param_de_repor()
    {
        var relogio = new Relogio { Ms = 10_000 };
        var relogioMundo = new RelogioDoMundoFalso { Noite = false, HoraAtual = 12f };
        var sorteador = new Sorteador { Padrao = 0.5 };

        var pontoDia = new PontoDeSpawn(
            "ponto-teste-dia",
            AreasDoMundo.Cidade,
            0,
            190,
            CatalogoCriaturas.JavaliDoCampo,
            2,
            4,
            2,
            2,
            1_000_000_000_000,
            Periodo: PeriodoDeAparicao.Dia
        );

        var pontoNoite = new PontoDeSpawn(
            "ponto-teste-noite",
            AreasDoMundo.Cidade,
            50,
            190,
            CatalogoCriaturas.LoboDoCampo,
            4,
            6,
            2,
            2,
            1_000_000_001_000,
            Periodo: PeriodoDeAparicao.Noite
        );

        var grupoDia = GrupoDeInimigos.DoPonto(
            pontoDia,
            sorteador,
            relogioMs: () => relogio.Ms,
            relogioDoMundo: relogioMundo
        );

        var grupoNoite = GrupoDeInimigos.DoPonto(
            pontoNoite,
            sorteador,
            relogioMs: () => relogio.Ms,
            relogioDoMundo: relogioMundo
        );

        // De dia: o ponto diurno monta e tem vivos; o noturno está vazio aguardando a noite
        Assert.Equal(2, grupoDia.Vivos().Length);
        Assert.All(
            grupoDia.Vivos(),
            v => Assert.Equal(CatalogoCriaturas.JavaliDoCampo, v.CriaturaId)
        );
        Assert.Empty(grupoNoite.Vivos());

        // Mata 1 membro do grupo diurno de dia e confere reposição normal de dia
        var javaliMorto = grupoDia.Vivos()[0];
        grupoDia.TentarAplicarDano(javaliMorto.InimigoId, 9999, 1, out _);
        Assert.Single(grupoDia.Vivos());

        // Passa o tempo do respawn de dia: repõe
        relogio.Ms += 200_000;
        grupoDia.Passo([]);
        Assert.Equal(2, grupoDia.Vivos().Length);

        // ── Virada para a Noite ──
        relogioMundo.Noite = true;
        relogioMundo.HoraAtual = 23f;

        // Ao virar a noite, o ponto noturno monta e repõe só espécies da noite
        relogio.Ms += 10_000;
        grupoNoite.Passo([]);
        Assert.Equal(2, grupoNoite.Vivos().Length);
        Assert.All(
            grupoNoite.Vivos(),
            v => Assert.Equal(CatalogoCriaturas.LoboDoCampo, v.CriaturaId)
        );

        // Os vivos do ponto de dia continuam vivos!
        Assert.Equal(2, grupoDia.Vivos().Length);

        // Mata 1 membro do grupo de dia durante a noite
        var javaliMortoDeNoite = grupoDia.Vivos()[0];
        grupoDia.TentarAplicarDano(javaliMortoDeNoite.InimigoId, 9999, 1, out _);
        Assert.Single(grupoDia.Vivos());

        // Passa o tempo do respawn durante a noite: o de dia NÃO repõe (os vivos ficam)
        relogio.Ms += 200_000;
        grupoDia.Passo([]);
        Assert.Single(grupoDia.Vivos()); // Não repôs! Permanece 1 vivo

        // Já o ponto noturno repõe mortos normalmente durante a noite
        var loboMorto = grupoNoite.Vivos()[0];
        grupoNoite.TentarAplicarDano(loboMorto.InimigoId, 9999, 1, out _);
        Assert.Single(grupoNoite.Vivos());
        relogio.Ms += 200_000;
        grupoNoite.Passo([]);
        Assert.Equal(2, grupoNoite.Vivos().Length); // Ponto noturno repôs lobo da noite

        // ── Amanhecer ──
        relogioMundo.Noite = false;
        relogioMundo.HoraAtual = 8f;

        // Ao voltar o dia, o ponto de dia volta a repor o javali morto
        relogio.Ms += 10_000;
        grupoDia.Passo([]);
        Assert.Equal(2, grupoDia.Vivos().Length);

        // E no ponto noturno, os lobos vivos continuam vivos
        Assert.Equal(2, grupoNoite.Vivos().Length);

        // Mas se um lobo morrer de dia, o ponto noturno para de repor
        var loboMortoDeDia = grupoNoite.Vivos()[0];
        grupoNoite.TentarAplicarDano(loboMortoDeDia.InimigoId, 9999, 1, out _);
        Assert.Single(grupoNoite.Vivos());
        relogio.Ms += 200_000;
        grupoNoite.Passo([]);
        Assert.Single(grupoNoite.Vivos()); // Não repôs durante o dia
    }
}
