using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Manadas com rota (issue #321): seguem pontos, perseguem até um limite, voltam ao perder o
/// alvo e nunca entram na zona urbana (a cidade mais a margem). Relógio e jogadores à mão.
/// </summary>
public class ManadaComRotaTestes
{
    private const long Jogador = 9;
    private const double Borda = 50.0; // 40 m da cidade + 10 m de margem

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    private sealed class SorteadorFixo : ISorteadorDeCriaturas
    {
        public CriaturaSorteada Sortear(DefinicaoDeCriatura definicao) =>
            new(
                definicao,
                VarianteDeCriatura.Normal,
                definicao.VidaBase,
                definicao.DanoBase,
                definicao.ExperienciaBase,
                1,
                1f
            );

        public bool Aparece(DefinicaoDeCriatura definicao) => true;
    }

    private static RotaDaManada Rota(double limite = 80.0, double z = 60.0) =>
        new(
            [
                new PontoDeRota(TipoDePontoDeRota.Pasto, 0, z, 5),
                new PontoDeRota(TipoDePontoDeRota.Rio, 30, 60, 5),
                new PontoDeRota(TipoDePontoDeRota.Repouso, 30, 90, 5),
                new PontoDeRota(TipoDePontoDeRota.Retorno, 0, 90, 5),
            ],
            limite
        );

    /// <summary>Uma manada de um só, com o ponto de origem em (0, pontoZ + 4), no pasto da rota.</summary>
    private sealed class Campo
    {
        public Campo(RotaDaManada? rota = null, double pontoZ = 60)
        {
            Grupo = new GrupoDeInimigos(
                Javali,
                new SorteadorFixo(),
                1,
                () => AgoraMs,
                pontoX: 0,
                pontoZ: pontoZ,
                rota: rota ?? Rota()
            );
        }

        public long AgoraMs { get; set; } = 10_000;
        public GrupoDeInimigos Grupo { get; }
        public List<JogadorNoCampo> Jogadores { get; } = [];
        public List<PassoDosInimigos> Passos { get; } = [];

        public InimigoNoMundo Unico => Assert.Single(Grupo.Vivos());

        public void Por(double x, double z)
        {
            Jogadores.RemoveAll(j => j.PersonagemId == Jogador);
            Jogadores.Add(new JogadorNoCampo(Jogador, x, z));
        }

        public PassoDosInimigos Passo()
        {
            AgoraMs += 100;
            var passo = Grupo.Passo(Jogadores);
            Passos.Add(passo);
            return passo;
        }

        public void Avancar(double segundos)
        {
            for (var i = 0; i < Math.Round(segundos * 10); i++)
            {
                Passo();
            }
        }

        /// <summary>Avança até o estado, no máximo pelo tempo dado; falha se não chegou.</summary>
        public void AteEstado(EstadoDeInimigo estado, double segundosNoMaximo)
        {
            for (var i = 0; i < segundosNoMaximo * 10; i++)
            {
                Passo();
                if (Unico.Estado == estado)
                {
                    return;
                }
            }

            Assert.Fail($"Não chegou a {estado}; estado: {Unico.Estado}.");
        }
    }

    // ── A rota ────────────────────────────────────────────────────────────────

    [Fact]
    public void A_rota_do_campo_e_valida_e_passa_por_pasto_rio_repouso_e_retorno()
    {
        var rota = PontosDeSpawnDoCampo.RotaDaManadaDoPasto;

        Assert.Equal(
            [
                TipoDePontoDeRota.Pasto,
                TipoDePontoDeRota.Rio,
                TipoDePontoDeRota.Repouso,
                TipoDePontoDeRota.Retorno,
            ],
            rota.Pontos.Select(p => p.Tipo)
        );
        Assert.All(rota.Pontos, p => Assert.False(RegrasManada.EstaNaZonaUrbana(p.X, p.Z)));
        Assert.Same(rota, PontosDeSpawnDoCampo.Todos.Single(p => p.Rota != null).Rota);
    }

    [Fact]
    public void Rota_com_ponto_na_zona_urbana_ou_sem_pontos_suficientes_e_recusada()
    {
        var naCidade = new RotaDaManada([
            new PontoDeRota(TipoDePontoDeRota.Pasto, 0, 100, 1),
            new PontoDeRota(TipoDePontoDeRota.Rio, 10, 45, 1), // 45 < 50
        ]);
        var curta = new RotaDaManada([new PontoDeRota(TipoDePontoDeRota.Pasto, 0, 100, 1)]);

        Assert.Throws<ArgumentException>(() => naCidade.Validada());
        Assert.Throws<ArgumentException>(() => curta.Validada());
    }

    [Fact]
    public void Em_repouso_a_manada_anda_de_ponto_em_ponto_e_recomeca_a_volta()
    {
        var campo = new Campo();
        Assert.Equal(64.0, campo.Unico.Z, 0.01); // nasce no pasto, no lugar dele no bando

        campo.Avancar(4.9); // espera no pasto
        Assert.Equal(64.0, campo.Unico.Z, 0.01);

        campo.Avancar(1.0); // parte para o rio: 30 m a 1,5 m/s
        Assert.True(campo.Unico.X > 0.5);

        campo.Avancar(20.5);
        Assert.Equal(30.0, campo.Unico.X, 0.01);
        Assert.Equal(64.0, campo.Unico.Z, 0.01);
        Assert.Equal(EstadoDeInimigo.Repouso, campo.Unico.Estado);

        // Repouso (30, 94) e retorno (0, 94): chega a cada um e espera.
        campo.Avancar(5 + 20 + 5 + 20 + 1);
        Assert.Equal(94.0, campo.Unico.Z, 0.01);
        Assert.Equal(0.0, campo.Unico.X, 0.01);

        // E recomeça pelo pasto.
        campo.Avancar(5 + 20 + 1);
        Assert.Equal(64.0, campo.Unico.Z, 0.01);
    }

    [Fact]
    public void A_manada_anda_em_repouso_sem_ninguem_por_perto_e_avisa_a_posicao()
    {
        var campo = new Campo();
        campo.Avancar(6.0);

        Assert.Contains(campo.Passos, p => p.Mudancas.Any(m => m.SoPosicao));
    }

    // ── Limite urbano ─────────────────────────────────────────────────────────

    [Fact]
    public void Quem_esta_na_zona_urbana_nao_e_visto_mesmo_a_poucos_metros()
    {
        // O ponto do bando fica logo depois da borda: quem está dentro dela está na visão.
        var campo = new Campo(Rota(z: 52), 52);
        campo.Por(0, 49.9);

        campo.Avancar(1.0);
        Assert.Equal(EstadoDeInimigo.Repouso, campo.Unico.Estado);

        campo.Por(0, 50.5); // um passo fora da zona: é visto
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);
    }

    [Fact]
    public void A_perseguicao_acaba_na_borda_da_cidade_e_ela_nunca_e_cruzada()
    {
        var campo = new Campo();
        campo.Por(0, 56);
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);
        campo.Avancar(1.0);

        campo.Por(0, 40); // o jogador entra na cidade
        campo.Avancar(0.2);
        Assert.Equal(EstadoDeInimigo.Retorno, campo.Unico.Estado);

        campo.Avancar(10.0);
        Assert.All(
            campo.Passos,
            p => Assert.All(p.Mudancas, m => Assert.True(m.Mensagem.Z > Borda))
        );
        Assert.Equal(EstadoDeInimigo.Repouso, campo.Unico.Estado);
    }

    [Fact]
    public void A_manada_nao_passa_da_borda_seguindo_um_jogador_colado_nela()
    {
        var campo = new Campo();
        campo.Por(0, Borda + 2.0);
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);
        campo.Por(0, Borda + 0.5); // fora da zona, junto da borda

        campo.Avancar(15.0);

        Assert.All(
            campo.Passos,
            p => Assert.All(p.Mudancas, m => Assert.True(m.Mensagem.Z > Borda))
        );
    }

    [Fact]
    public void O_golpe_pega_quem_fica_fora_da_zona_e_poupa_quem_entrou_nela()
    {
        var fora = new Campo();
        fora.Por(0, Borda + 2.0);
        fora.AteEstado(EstadoDeInimigo.Ataque, 6);
        fora.Avancar(3.0);
        Assert.Contains(fora.Passos, p => p.Golpes.Count > 0);

        var dentro = new Campo();
        dentro.Por(0, Borda + 2.0);
        dentro.AteEstado(EstadoDeInimigo.Ataque, 6);
        dentro.Por(0, Borda - 0.5); // sai para dentro da zona durante o aviso
        dentro.Avancar(3.0);
        Assert.DoesNotContain(dentro.Passos, p => p.Golpes.Count > 0);
    }

    // ── Limite de perseguição e volta ao ponto ───────────────────────────────

    [Fact]
    public void Ao_passar_do_limite_de_perseguicao_a_manada_desiste_e_volta_ao_ponto_de_origem()
    {
        var campo = new Campo(Rota(limite: 10.0));
        campo.Por(0, 70);
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);

        // O jogador foge para o norte, sempre à frente da manada, para longe do ponto.
        for (var i = 0; i < 100 && campo.Unico.Estado != EstadoDeInimigo.Retorno; i++)
        {
            campo.Por(0, campo.Unico.Z + 8);
            campo.Passo();
        }

        Assert.Equal(EstadoDeInimigo.Retorno, campo.Unico.Estado);
        Assert.InRange(campo.Unico.Z, 64.0 + 10.0, 64.0 + 10.0 + 1.0); // parou no limite
        Assert.Equal(RegrasInimigo.SemAlvo, campo.Unico.AlvoId);

        campo.AteEstado(EstadoDeInimigo.Repouso, 10);
        Assert.Equal(64.0, campo.Unico.Z, 0.01);
        Assert.Equal(0.0, campo.Unico.X, 0.01);
    }

    [Fact]
    public void Ao_perder_o_alvo_a_manada_volta_ao_ponto_de_origem_com_a_vida_cheia()
    {
        var campo = new Campo();
        campo.Por(0, 56);
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);
        campo.Avancar(1.0);
        Assert.True(
            campo.Grupo.TentarAplicarDano(campo.Unico.InimigoId, 20, Jogador, out var resultado)
        );
        Assert.True(resultado.VidaRestante < Javali.VidaBase);

        campo.Jogadores.Clear(); // o alvo some (saiu ou desconectou)
        campo.Avancar(0.2);
        Assert.Equal(EstadoDeInimigo.Retorno, campo.Unico.Estado);

        campo.AteEstado(EstadoDeInimigo.Repouso, 15);
        Assert.Equal(64.0, campo.Unico.Z, 0.01);
        Assert.Equal(Javali.VidaBase, campo.Unico.Vida);
    }

    [Fact]
    public void Depois_de_voltar_a_manada_retoma_a_rota()
    {
        var campo = new Campo();
        campo.Por(0, 56);
        campo.AteEstado(EstadoDeInimigo.Perseguicao, 3);
        campo.Avancar(1.0);
        campo.Jogadores.Clear();
        campo.AteEstado(EstadoDeInimigo.Repouso, 15);

        campo.Avancar(15);

        Assert.True(campo.Unico.X > 0.5, "voltou a seguir a rota rumo ao rio");
    }

    // ── Regras puras ──────────────────────────────────────────────────────────

    [Fact]
    public void A_zona_urbana_e_a_cidade_mais_a_margem()
    {
        Assert.True(RegrasManada.EstaNaZonaUrbana(0, 0));
        Assert.True(RegrasManada.EstaNaZonaUrbana(-50, 50));
        Assert.False(RegrasManada.EstaNaZonaUrbana(0, 50.01));
        Assert.False(RegrasManada.EstaNaZonaUrbana(-50.01, 0));
        Assert.True(RegrasManada.DeveDesistir(80.01, 80));
        Assert.False(RegrasManada.DeveDesistir(80, 80));
    }
}
