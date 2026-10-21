using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>O chefe de mundo (#191): hora e lugar sorteados, força fixa, sem sumiço por tempo.</summary>
public class ChefeDeMundoTestes
{
    private const long Jogador = 9;

    private static readonly DefinicaoDeCriatura Anciao = CatalogoCriaturas.Obter(
        CatalogoCriaturas.AnciaoTrovejante
    )!;

    private static readonly long IntervaloMs = Anciao.IntervaloDeAparicaoSegundos * 1000L;

    /// <summary>Sorteio combinado: aparece ou não, e o "próximo" devolve o valor dado.</summary>
    private sealed class SorteadorDoMundo(bool aparece, double proximo = 0.0)
        : ISorteadorDeCriaturas
    {
        public bool Aparece { get; set; } = aparece;
        public double Proximo { get; set; } = proximo;
        public int Tentativas { get; private set; }

        public CriaturaSorteada Sortear(DefinicaoDeCriatura d) =>
            new(d, VarianteDeCriatura.Normal, d.VidaBase, d.DanoBase, d.ExperienciaBase, 1.0, 1.0f);

        bool ISorteadorDeCriaturas.Aparece(DefinicaoDeCriatura definicao)
        {
            Tentativas++;
            return Aparece;
        }

        double ISorteadorDeCriaturas.Proximo() => Proximo;
    }

    private sealed class Mundo
    {
        public Mundo(bool aparece, double proximo = 0.0)
        {
            Sorteador = new SorteadorDoMundo(aparece, proximo);
            Grupo = new GrupoDeInimigos(
                Anciao,
                Sorteador,
                1,
                () => AgoraMs,
                primeiroId: ConfigChefeDeMundo.PrimeiroId,
                lugares: ConfigChefeDeMundo.Lugares
            );
        }

        public long AgoraMs { get; set; } = 1_000_000;
        public SorteadorDoMundo Sorteador { get; }
        public GrupoDeInimigos Grupo { get; }

        public PassoDosInimigos Passo(long avancoMs = 100)
        {
            AgoraMs += avancoMs;
            return Grupo.Passo([]);
        }
    }

    [Fact]
    public void Ao_subir_o_servidor_nao_ha_chefe_e_o_primeiro_sorteio_so_vem_apos_um_intervalo()
    {
        var mundo = new Mundo(aparece: true);

        Assert.Empty(mundo.Grupo.Vivos());
        Assert.Empty(mundo.Passo().Aparicoes);
        Assert.Empty(mundo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Equal(0, mundo.Sorteador.Tentativas);

        var aparicao = Assert.Single(mundo.Passo(1000).Aparicoes).Inimigo;
        Assert.Equal(1, mundo.Sorteador.Tentativas);
        Assert.Equal(CatalogoCriaturas.AnciaoTrovejante, aparicao.CriaturaId);
        Assert.Equal(VarianteDeCriatura.Normal, aparicao.Variante);
    }

    [Fact]
    public void Chance_zero_so_tenta_de_novo_no_intervalo_seguinte()
    {
        var mundo = new Mundo(aparece: false);
        mundo.Passo(IntervaloMs);
        Assert.Equal(1, mundo.Sorteador.Tentativas);

        Assert.Empty(mundo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Equal(1, mundo.Sorteador.Tentativas);

        Assert.Empty(mundo.Passo(1000).Aparicoes);
        Assert.Equal(2, mundo.Sorteador.Tentativas);
        Assert.Empty(mundo.Grupo.Vivos());
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.4, 1)]
    [InlineData(0.99, 2)]
    public void O_lugar_e_sorteado_entre_os_lugares_da_config(double sorteio, int indice)
    {
        var mundo = new Mundo(aparece: true, proximo: sorteio);

        var aparicao = Assert.Single(mundo.Passo(IntervaloMs).Aparicoes).Inimigo;

        var lugar = ConfigChefeDeMundo.Lugares[indice];
        Assert.Equal((float)lugar.X, aparicao.X);
        Assert.Equal((float)lugar.Z, aparicao.Z);
    }

    [Fact]
    public void A_forca_e_fixa_a_da_ficha()
    {
        var mundo = new Mundo(aparece: true);

        var aparicao = Assert.Single(mundo.Passo(IntervaloMs).Aparicoes).Inimigo;

        Assert.Equal(Anciao.VidaBase, aparicao.VidaMaxima);
        Assert.Equal(Anciao.VidaBase, aparicao.Vida);
        Assert.Equal(Anciao.Nivel, aparicao.Nivel);
    }

    [Fact]
    public void O_chefe_nao_some_por_tempo_nem_sorteia_de_novo_enquanto_vive()
    {
        var mundo = new Mundo(aparece: true);
        mundo.Passo(IntervaloMs);
        var id = Assert.Single(mundo.Grupo.Vivos()).InimigoId;

        // Horas depois (bem além dos 30 min do chefe raro), ele continua lá, sem novo sorteio.
        for (var i = 0; i < 10; i++)
        {
            var passo = mundo.Passo(IntervaloMs);
            Assert.Empty(passo.Aparicoes);
            Assert.Empty(passo.Mortes);
        }

        Assert.Equal(id, Assert.Single(mundo.Grupo.Vivos()).InimigoId);
        Assert.Equal(1, mundo.Sorteador.Tentativas);
    }

    [Fact]
    public void A_morte_publica_os_participantes_e_a_espera_recomeca_com_novo_lugar()
    {
        var mundo = new Mundo(aparece: true, proximo: 0.0);
        var id = Assert.Single(mundo.Passo(IntervaloMs).Aparicoes).Inimigo.InimigoId;

        Assert.True(mundo.Grupo.TentarAplicarDano(id, 999_999_999, Jogador, out _));
        var passo = mundo.Passo();

        var derrota = Assert.Single(passo.Derrotas);
        Assert.Equal(CatalogoCriaturas.AnciaoTrovejante, derrota.CriaturaId);
        Assert.Contains(Jogador, derrota.Participantes);
        Assert.Empty(mundo.Grupo.Vivos());

        // Só volta no intervalo seguinte, e em outro sorteio de lugar.
        mundo.Sorteador.Proximo = 0.99;
        Assert.Empty(mundo.Passo(IntervaloMs - 1000).Aparicoes);
        var volta = Assert.Single(mundo.Passo(1500).Aparicoes).Inimigo;
        Assert.NotEqual(id, volta.InimigoId);
        Assert.Equal((float)ConfigChefeDeMundo.Lugares[^1].X, volta.X);
    }

    [Fact]
    public void O_loot_do_chefe_de_mundo_e_o_de_chefe_com_o_grau_do_nivel()
    {
        Assert.Equal(
            OrigemDoLoot.Chefe,
            TabelaDeLoot.OrigemDe(CategoriaDeCriatura.ChefeDeMundo, VarianteDeCriatura.Normal)
        );
        Assert.Equal(GrauEquipamento.Forte, TabelaDeLoot.GrauDoNivel(Anciao.Nivel));
    }

    [Fact]
    public void Os_lugares_ficam_em_terra_firme_fora_da_cidade_e_do_campo()
    {
        Assert.NotEmpty(ConfigChefeDeMundo.Lugares);
        Assert.All(
            ConfigChefeDeMundo.Lugares,
            lugar =>
            {
                var x = (float)lugar.X;
                var z = (float)lugar.Z;
                Assert.True(MapaDoMundo.DentroDoMundo(x, z), lugar.Nome);
                Assert.NotEqual(Bioma.Cidade, MapaDoMundo.BiomaEm(x, z));
                Assert.True(
                    MapaDoMundo.AlturaEm(x, z) > MapaDoMundo.NivelDaAgua,
                    $"{lugar.Nome} cai na água"
                );
                Assert.True(
                    RegrasInimigo.Distancia(0, 0, lugar.X, lugar.Z) > 500,
                    $"{lugar.Nome} é perto demais da cidade"
                );
                Assert.All(
                    PontosDeSpawnDoCampo.Todos,
                    p =>
                        Assert.True(
                            RegrasInimigo.Distancia(p.X, p.Z, lugar.X, lugar.Z)
                                > 2 * RegrasInimigo.DistanciaDeVisaoMetros,
                            $"{lugar.Nome} está na visão do ponto {p.Nome}"
                        )
                );
            }
        );
    }

    [Fact]
    public void Chefe_de_mundo_exige_pelo_menos_um_lugar()
    {
        Assert.Throws<ArgumentException>(() =>
            new GrupoDeInimigos(Anciao, new SorteadorDoMundo(true), 1, lugares: [])
        );
    }

    [Fact]
    public void Ids_dos_chefes_de_mundo_nao_invadem_os_do_campo_nem_os_do_chefe_raro()
    {
        Assert.True(ConfigChefeDeMundo.PrimeiroId > ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro);
    }
}
