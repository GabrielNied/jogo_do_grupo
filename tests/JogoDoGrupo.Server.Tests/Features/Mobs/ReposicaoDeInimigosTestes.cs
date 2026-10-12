using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Densidade e reposição do campo (issue #489): reposição individual do morto com o grupo vivo,
/// teto de vivos, variação dos tempos e a meta de distância até um grupo.
/// </summary>
public class ReposicaoDeInimigosTestes
{
    private const long Jogador = 9;
    private const long Passo100Ms = 100;

    private static readonly DefinicaoDeCriatura Javali = CatalogoCriaturas.Obter(
        CatalogoCriaturas.JavaliDoCampo
    );

    private static readonly long IntervaloMs = Javali.IntervaloDeAparicaoSegundos * 1000L;

    private static readonly long IntervaloMaximoMs = (long)(
        IntervaloMs * (1.0 + ConfigReposicaoDeInimigos.VariacaoDoIntervalo)
    );

    /// <summary>Sempre a variante normal; "Proximo" devolve o valor de <see cref="Sorteio"/>.</summary>
    private sealed class Sorteador : ISorteadorDeCriaturas
    {
        public double Sorteio { get; set; }

        public double Proximo() => Sorteio;

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
        int tamanho,
        LimiteDeInimigosVivos? limite = null
    ) => new(Javali, sorteador, tamanho, () => relogio.Ms, limite: limite);

    private static void Matar(GrupoDeInimigos grupo, long id) =>
        Assert.True(grupo.TentarAplicarDano(id, 1_000_000, Jogador, out _));

    // ── Reposição individual ─────────────────────────────────────────────────

    [Fact]
    public void Sobrevivente_sozinho_fora_de_luta_tem_o_grupo_reposto_depois_do_intervalo()
    {
        var relogio = new Relogio();
        var limite = new LimiteDeInimigosVivos(10);
        var grupo = Grupo(new Sorteador(), relogio, 3, limite);
        var ids = grupo.Vivos().Select(i => i.InimigoId).ToArray();

        Matar(grupo, ids[0]);
        Matar(grupo, ids[1]);
        grupo.Passo([]);
        Assert.Single(grupo.Vivos());
        Assert.Equal(1, limite.Vivos);

        relogio.Ms += IntervaloMs - 1;
        Assert.Empty(grupo.Passo([]).Aparicoes);
        Assert.Single(grupo.Vivos());

        relogio.Ms += 1;
        var passo = grupo.Passo([]);

        Assert.Equal(2, passo.Aparicoes.Count);
        Assert.Equal(3, grupo.Vivos().Length);
        Assert.Equal(3, limite.Vivos);
        // O sobrevivente continua o mesmo; os repostos são inimigos novos, com id novo.
        Assert.Contains(ids[2], grupo.Vivos().Select(i => i.InimigoId));
        Assert.Empty(new[] { ids[0], ids[1] }.Intersect(grupo.Vivos().Select(i => i.InimigoId)));
    }

    [Fact]
    public void O_reposto_volta_no_lugar_do_morto()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador(), relogio, 3);
        var antes = grupo.Vivos();
        var morto = antes[0];

        Matar(grupo, morto.InimigoId);
        relogio.Ms += IntervaloMs;
        var passo = grupo.Passo([]);

        var novo = Assert.Single(passo.Aparicoes).Inimigo;
        Assert.Equal(morto.X, novo.X, 0.001);
        Assert.Equal(morto.Z, novo.Z, 0.001);
        Assert.Equal(3, grupo.Vivos().Length);
    }

    [Fact]
    public void Em_luta_a_reposicao_espera_e_sai_quando_o_grupo_fica_fora_de_luta()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador(), relogio, 2);
        var ids = grupo.Vivos().Select(i => i.InimigoId).ToArray();
        var vivo = grupo.Vivos().Single(i => i.InimigoId == ids[1]);
        Matar(grupo, ids[0]);

        // O jogador perto do sobrevivente: o grupo entra em alerta e passa do intervalo em luta.
        var jogador = new JogadorNoCampo(Jogador, vivo.X, vivo.Z + 3);
        var voltou = false;
        for (var i = 0; i < (IntervaloMaximoMs / Passo100Ms) + 50; i++)
        {
            relogio.Ms += Passo100Ms;
            var passo = grupo.Passo([jogador]);
            voltou |= passo.Aparicoes.Count > 0;
        }

        Assert.False(voltou, "repôs com o grupo em luta");
        Assert.Single(grupo.Vivos());

        // O jogador sai da área: o grupo esquece, volta ao repouso e então repõe.
        for (var i = 0; i < 600 && !voltou; i++)
        {
            relogio.Ms += Passo100Ms;
            voltou |= grupo.Passo([]).Aparicoes.Count > 0;
        }

        Assert.True(voltou);
        Assert.Equal(2, grupo.Vivos().Length);
    }

    [Fact]
    public void A_reposicao_nao_passa_do_teto_e_tenta_de_novo_quando_ha_vaga()
    {
        var relogio = new Relogio();
        var limite = new LimiteDeInimigosVivos(4);
        var sorteador = new Sorteador();
        var grupo = Grupo(sorteador, relogio, 3, limite);
        var ids = grupo.Vivos().Select(i => i.InimigoId).ToArray();
        Matar(grupo, ids[0]);
        // Outro grupo ocupa a vaga que sobrou.
        var vizinho = Grupo(sorteador, relogio, 2, limite);
        Assert.Equal(4, limite.Vivos);

        relogio.Ms += IntervaloMaximoMs;
        Assert.Empty(grupo.Passo([]).Aparicoes);
        Assert.Equal(2, grupo.Vivos().Length);
        Assert.True(limite.Vivos <= limite.Maximo);

        Matar(vizinho, vizinho.Vivos()[0].InimigoId);
        relogio.Ms += (long)(ConfigInimigosDoCampo.TentativaDeVagaSegundos * 1000);
        var passo = grupo.Passo([]);

        Assert.Single(passo.Aparicoes);
        Assert.Equal(3, grupo.Vivos().Length);
        Assert.Equal(limite.Maximo, limite.Vivos);
    }

    [Fact]
    public void Inimigo_de_teste_morto_nao_volta()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador(), relogio, 1);
        var extra = grupo.ForcarAparicao(20, 20);
        Assert.NotNull(extra);
        Matar(grupo, extra.InimigoId);

        relogio.Ms += IntervaloMaximoMs * 2;
        grupo.Passo([]);

        Assert.Single(grupo.Vivos());
    }

    [Fact]
    public void O_intervalo_varia_para_o_membro_nao_voltar_sempre_no_mesmo_instante()
    {
        foreach (var (sorteio, voltaEmMs) in new[] { (0.0, IntervaloMs), (1.0, IntervaloMaximoMs) })
        {
            var relogio = new Relogio();
            var sorteador = new Sorteador { Sorteio = sorteio };
            var grupo = Grupo(sorteador, relogio, 2);
            Matar(grupo, grupo.Vivos()[0].InimigoId);

            relogio.Ms += voltaEmMs - 1;
            Assert.Empty(grupo.Passo([]).Aparicoes);
            relogio.Ms += 1;
            Assert.Single(grupo.Passo([]).Aparicoes);
        }

        Assert.True(IntervaloMaximoMs > IntervaloMs);
    }

    [Fact]
    public void A_variacao_tambem_vale_para_o_grupo_inteiro()
    {
        var relogio = new Relogio();
        var grupo = Grupo(new Sorteador { Sorteio = 1.0 }, relogio, 2);
        foreach (var vivo in grupo.Vivos())
        {
            Matar(grupo, vivo.InimigoId);
        }

        relogio.Ms += IntervaloMs;
        Assert.Empty(grupo.Passo([]).Aparicoes);
        relogio.Ms += IntervaloMaximoMs - IntervaloMs;
        Assert.Equal(2, grupo.Passo([]).Aparicoes.Count);
    }

    // ── Campo todo ───────────────────────────────────────────────────────────

    private static (GrupoDeInimigos[] Grupos, LimiteDeInimigosVivos Limite) Campo(
        Relogio relogio,
        double sorteio = 0.99
    )
    {
        var limite = new LimiteDeInimigosVivos(PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
        var sorteador = new Sorteador { Sorteio = sorteio };
        return (
            [
                .. PontosDeSpawnDoCampo.Todos.Select(p =>
                    GrupoDeInimigos.DoPonto(p, sorteador, limite, () => relogio.Ms)
                ),
            ],
            limite
        );
    }

    private static void Avancar(GrupoDeInimigos[] grupos, Relogio relogio, long ms)
    {
        for (var feito = 0L; feito < ms; feito += Passo100Ms)
        {
            relogio.Ms += Passo100Ms;
            foreach (var grupo in grupos)
            {
                grupo.Passo([]);
            }
        }
    }

    [Fact]
    public void O_teto_age_porque_os_pontos_somados_passam_dele()
    {
        var relogio = new Relogio();
        var (grupos, limite) = Campo(relogio);

        Assert.True(
            PontosDeSpawnDoCampo.Todos.Sum(p => p.TamanhoMaximo) > limite.Maximo,
            "a soma dos máximos tem que passar do teto, senão ele nunca age"
        );
        Assert.Equal(limite.Maximo, limite.Vivos);
        Assert.Equal(limite.Maximo, grupos.Sum(g => g.Vivos().Length));
        Assert.Contains(grupos, g => g.Vivos().Length < g.Capacidade);
    }

    [Fact]
    public void Campo_limpo_em_sequencia_volta_a_ter_pelo_menos_30_vivos_e_nunca_passa_do_teto()
    {
        const int minimoDepois = 30;
        const long limpezaMs = 60_000;
        var relogio = new Relogio();
        var (grupos, limite) = Campo(relogio, sorteio: 0.5);

        // Limpa o campo todo, um inimigo por vez, sem esperar.
        foreach (var grupo in grupos)
        {
            foreach (var vivo in grupo.Vivos())
            {
                Matar(grupo, vivo.InimigoId);
            }

            relogio.Ms += 100;
            grupo.Passo([]);
        }

        Assert.Equal(0, limite.Vivos);

        // Depois do intervalo máximo (mais uma volta de tentativa de vaga) o campo está cheio.
        var espera =
            IntervaloMaximoMs
            + (long)(ConfigInimigosDoCampo.TentativaDeVagaSegundos * 1000)
            + (Tatu().IntervaloDeAparicaoSegundos * 1000L);
        for (var t = 0L; t < espera + limpezaMs; t += Passo100Ms)
        {
            Avancar(grupos, relogio, Passo100Ms);
            var vivos = grupos.Sum(g => g.Vivos().Length);
            Assert.True(vivos <= limite.Maximo, $"passou do teto: {vivos}");
            Assert.Equal(vivos, limite.Vivos);
        }

        Assert.True(
            grupos.Sum(g => g.Vivos().Length) >= minimoDepois,
            $"só {grupos.Sum(g => g.Vivos().Length)} vivos depois de {(espera + limpezaMs) / 1000} s"
        );
    }

    [Fact]
    public void Grupos_pela_metade_do_campo_voltam_a_ficar_completos()
    {
        var relogio = new Relogio();
        var (grupos, limite) = Campo(relogio, sorteio: 0.99);
        var cheio = limite.Vivos;

        // Deixa só um sobrevivente em cada grupo.
        foreach (var grupo in grupos)
        {
            foreach (var vivo in grupo.Vivos().Skip(1))
            {
                Matar(grupo, vivo.InimigoId);
            }
        }

        Avancar(grupos, relogio, 1_000);
        Assert.True(limite.Vivos < cheio);

        Avancar(grupos, relogio, IntervaloMaximoMs * 2 + 60_000);

        Assert.Equal(limite.Maximo, limite.Vivos);
        Assert.Equal(limite.Maximo, grupos.Sum(g => g.Vivos().Length));
    }

    private static DefinicaoDeCriatura Tatu() =>
        CatalogoCriaturas.Obter(CatalogoCriaturas.TatuRochoso);

    // ── Pontos ───────────────────────────────────────────────────────────────

    [Fact]
    public void Ha_de_12_a_15_pontos_de_spawn_no_campo()
    {
        Assert.InRange(PontosDeSpawnDoCampo.Todos.Count, 12, 15);
    }

    [Fact]
    public void De_qualquer_ponto_do_campo_proximo_ha_um_grupo_a_menos_de_30_s_correndo()
    {
        Assert.True(
            PontosDeSpawnDoCampo.DistanciaMaximaAteUmGrupoMetros
                <= 30 * RegrasVelocidade.CorrerPadrao
        );

        var pior = 0.0;
        for (
            var x = -PontosDeSpawnDoCampo.LarguraDoCampoProximoMetros;
            x <= PontosDeSpawnDoCampo.LarguraDoCampoProximoMetros;
            x += 10
        )
        {
            for (var z = 40.0; z <= PontosDeSpawnDoCampo.FimDoCampoProximoZ; z += 10)
            {
                var perto = PontosDeSpawnDoCampo.Todos.Min(p =>
                    RegrasInimigo.Distancia(x, z, p.X, p.Z)
                );
                pior = Math.Max(pior, perto);
            }
        }

        Assert.True(
            pior <= PontosDeSpawnDoCampo.DistanciaMaximaAteUmGrupoMetros,
            $"há um lugar do campo a {pior:F0} m do grupo mais próximo"
        );
    }

    [Fact]
    public void Saindo_do_portao_norte_um_grupo_fica_a_menos_de_30_s_correndo()
    {
        var doPortao = PontosDeSpawnDoCampo.Todos.Min(p =>
            RegrasInimigo.Distancia(0, 40, p.X, p.Z)
        );

        Assert.True(
            doPortao <= 30 * RegrasVelocidade.CorrerPadrao - RegrasInimigo.DistanciaDeVisaoMetros
        );
    }
}
