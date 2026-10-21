using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>O chefe raro (Presa-Velha) no campo: aparição por sorteio, morte e reaparição.</summary>
public class ChefeRaroNoCampoTestes
{
    private const long Jogador = 9;

    private static readonly DefinicaoDeCriatura Presa = CatalogoCriaturas.Obter(
        CatalogoCriaturas.PresaVelha
    )!;

    private static readonly long IntervaloMs = Presa.IntervaloDeAparicaoSegundos * 1000L;

    /// <summary>Sorteio que sempre dá o resultado combinado e conta as tentativas.</summary>
    private sealed class SorteadorDoChefe(bool aparece) : ISorteadorDeCriaturas
    {
        public bool Aparece { get; set; } = aparece;
        public int Tentativas { get; private set; }

        public CriaturaSorteada Sortear(DefinicaoDeCriatura d) =>
            new(d, VarianteDeCriatura.Normal, d.VidaBase, d.DanoBase, d.ExperienciaBase, 1.0, 1.0f);

        bool ISorteadorDeCriaturas.Aparece(DefinicaoDeCriatura definicao)
        {
            Tentativas++;
            return Aparece;
        }
    }

    private sealed class Campo
    {
        public Campo(bool aparece)
        {
            Sorteador = new SorteadorDoChefe(aparece);
            Grupo = new GrupoDeInimigos(
                Presa,
                Sorteador,
                1,
                () => AgoraMs,
                pontoX: ConfigInimigosDoCampo.PontoDoChefeRaroX,
                pontoZ: ConfigInimigosDoCampo.PontoDoChefeRaroZ,
                primeiroId: ConfigInimigosDoCampo.PrimeiroIdDoChefeRaro
            );
        }

        public long AgoraMs { get; set; } = 1_000_000;
        public SorteadorDoChefe Sorteador { get; }
        public GrupoDeInimigos Grupo { get; }

        public PassoDosInimigos Passo(long avancoMs = 100)
        {
            AgoraMs += avancoMs;
            return Grupo.Passo([]);
        }

        public void Matar(long id)
        {
            Assert.True(Grupo.TentarAplicarDano(id, 999_999, Jogador, out _));
        }
    }

    [Fact]
    public void Chance_cheia_traz_o_chefe_no_primeiro_passo_como_um_inimigo_normal()
    {
        var campo = new Campo(aparece: true);
        Assert.Empty(campo.Grupo.Vivos());

        var passo = campo.Passo();

        var aparicao = Assert.Single(passo.Aparicoes).Inimigo;
        Assert.Equal(CatalogoCriaturas.PresaVelha, aparicao.CriaturaId);
        Assert.Equal(VarianteDeCriatura.Normal, aparicao.Variante);
        Assert.Equal(Presa.VidaBase, aparicao.Vida);
        Assert.Equal(Presa.VidaBase, aparicao.VidaMaxima);
        Assert.Equal(EstadoDeInimigo.Repouso, aparicao.Estado);
        Assert.Equal((float)ConfigInimigosDoCampo.PontoDoChefeRaroX, aparicao.X);
        Assert.Equal(
            (float)(
                ConfigInimigosDoCampo.PontoDoChefeRaroZ
                + ConfigInimigosDoCampo.RaioDoEspalhamentoMetros
            ),
            aparicao.Z
        );
        Assert.Equal(aparicao.InimigoId, Assert.Single(campo.Grupo.Vivos()).InimigoId);
    }

    [Fact]
    public void Chance_zero_nunca_traz_o_chefe_e_so_tenta_a_cada_intervalo()
    {
        var campo = new Campo(aparece: false);

        Assert.Empty(campo.Passo().Aparicoes);
        Assert.Equal(1, campo.Sorteador.Tentativas);

        // Dentro do intervalo não há novo sorteio.
        Assert.Empty(campo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Equal(1, campo.Sorteador.Tentativas);

        // Passado o intervalo, mais um sorteio (e só um).
        Assert.Empty(campo.Passo(2000).Aparicoes);
        Assert.Equal(2, campo.Sorteador.Tentativas);
        Assert.Empty(campo.Grupo.Vivos());
    }

    [Fact]
    public void Sorteio_perdido_vale_no_intervalo_seguinte()
    {
        var campo = new Campo(aparece: false);
        campo.Passo();

        campo.Sorteador.Aparece = true;
        Assert.Empty(campo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Single(campo.Passo(1000).Aparicoes);
    }

    [Fact]
    public void So_ha_um_chefe_vivo_e_nao_ha_sorteio_enquanto_ele_esta_no_mundo()
    {
        var campo = new Campo(aparece: true);
        campo.Passo();

        for (var i = 0; i < 3; i++)
        {
            Assert.Empty(campo.Passo(IntervaloMs / 6).Aparicoes);
        }

        Assert.Equal(1, campo.Sorteador.Tentativas);
        Assert.Single(campo.Grupo.Vivos());
    }

    [Fact]
    public void Sem_combate_chefe_some_apos_30_min_e_recomeca_espera()
    {
        var campo = new Campo(aparece: true);
        var id = Assert.Single(campo.Passo().Aparicoes).Inimigo.InimigoId;

        // Com 29m59s continua vivo
        Assert.Empty(campo.Passo(IntervaloMs - 1000).Mortes);
        Assert.Single(campo.Grupo.Vivos());

        // Ao completar 30 minutos sem combate: some sem derrota e emite morte para os clientes
        var passo = campo.Passo(1000);
        Assert.Equal(id, Assert.Single(passo.Mortes).InimigoId);
        Assert.Empty(passo.Derrotas);
        Assert.Empty(campo.Grupo.Vivos());

        // Intervalo recomeça após o sumiço
        Assert.Empty(campo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Equal(1, campo.Sorteador.Tentativas);
        Assert.Single(campo.Passo(1000).Aparicoes);
        Assert.Equal(2, campo.Sorteador.Tentativas);
    }

    [Fact]
    public void Em_combate_timer_de_sumico_pausa_e_continua_vivo()
    {
        var campo = new Campo(aparece: true);
        var id = Assert.Single(campo.Passo().Aparicoes).Inimigo.InimigoId;

        var jogadorPerto = new JogadorNoCampo(
            Jogador,
            ConfigInimigosDoCampo.PontoDoChefeRaroX,
            ConfigInimigosDoCampo.PontoDoChefeRaroZ
                + ConfigInimigosDoCampo.RaioDoEspalhamentoMetros
                + 3
        );

        // Mesmo passando mais de 30 minutos em combate ativo, não some
        for (var i = 0; i < 35; i++)
        {
            campo.AgoraMs += 60_000;
            var passo = campo.Grupo.Passo([jogadorPerto]);
            Assert.Empty(passo.Mortes);
        }

        Assert.Single(campo.Grupo.Vivos());
    }

    [Fact]
    public void Morte_manda_as_mensagens_e_o_evento_com_a_variante_e_recomeca_a_espera()
    {
        var campo = new Campo(aparece: true);
        var id = Assert.Single(campo.Passo().Aparicoes).Inimigo.InimigoId;

        campo.Matar(id);
        var passo = campo.Passo();

        Assert.Equal(id, Assert.Single(passo.Mortes).InimigoId);
        var derrota = Assert.Single(passo.Derrotas);
        Assert.Equal(id, derrota.InimigoId);
        Assert.Equal(CatalogoCriaturas.PresaVelha, derrota.CriaturaId);
        Assert.Equal(VarianteDeCriatura.Normal, derrota.Variante);
        Assert.Equal(Presa.ExperienciaBase, derrota.Experiencia);
        Assert.Equal([Jogador], derrota.Participantes);
        Assert.Empty(campo.Grupo.Vivos());

        // Só volta no intervalo, a partir da morte, e por sorteio.
        Assert.Empty(campo.Passo(IntervaloMs - 1000).Aparicoes);
        Assert.Equal(1, campo.Sorteador.Tentativas);
        var volta = Assert.Single(campo.Passo(1000).Aparicoes).Inimigo;
        Assert.NotEqual(id, volta.InimigoId);
        Assert.Equal(2, campo.Sorteador.Tentativas);
    }

    [Fact]
    public void Chefe_morto_nao_leva_mais_dano()
    {
        var campo = new Campo(aparece: true);
        var id = Assert.Single(campo.Passo().Aparicoes).Inimigo.InimigoId;
        campo.Matar(id);

        Assert.False(campo.Grupo.TentarAplicarDano(id, 10, Jogador, out _));
        Assert.Empty(campo.Grupo.AlvosNoVolume(ConfigInimigosDoCampo.Area, -60, 230, 0));
    }

    [Fact]
    public void Chefe_usa_a_ia_comum_com_o_dano_da_propria_ficha()
    {
        var campo = new Campo(aparece: true);
        campo.Passo();
        var jogadorPerto = new JogadorNoCampo(
            Jogador,
            ConfigInimigosDoCampo.PontoDoChefeRaroX,
            ConfigInimigosDoCampo.PontoDoChefeRaroZ
                + ConfigInimigosDoCampo.RaioDoEspalhamentoMetros
                + 3
        );

        var golpes = new List<int>();
        for (var i = 0; i < 100 && golpes.Count == 0; i++)
        {
            campo.AgoraMs += 100;
            golpes.AddRange(campo.Grupo.Passo([jogadorPerto]).Golpes.Select(g => g.Dano));
        }

        Assert.Equal(new[] { Presa.DanoBase }, golpes);
    }

    [Fact]
    public void Chefe_raro_so_aceita_um_inimigo()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new GrupoDeInimigos(Presa, new SorteadorDoChefe(true), 2)
        );
    }
}
