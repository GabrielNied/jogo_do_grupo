using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class BonecoDeTreinoTestes
{
    private const double X = ConfigBonecoDeTreino.X;
    private const double Z = ConfigBonecoDeTreino.Z;
    private long _agoraMs = 1_000;

    private BonecoDeTreino Novo() => new(() => Interlocked.Read(ref _agoraMs));

    private void Passar(double segundos) =>
        Interlocked.Add(ref _agoraMs, (long)(segundos * 1000.0));

    // 2 m atrás do boneco, olhando para ele (+Z).
    private static IReadOnlyList<AlvoDeCombate> NoVolume(IAlvosDeCombate b) =>
        b.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 2, 0);

    [Fact]
    public void Comeca_com_180_de_vida_e_aparece_no_volume_do_golpe()
    {
        var alvo = Assert.Single(NoVolume(Novo()));

        Assert.Equal(ConfigBonecoDeTreino.AlvoId, alvo.Id);
        Assert.Equal(180, alvo.VidaAtual);
        Assert.Equal(180, alvo.VidaMaxima);
        Assert.Equal(ConfigBonecoDeTreino.X, alvo.X);
        Assert.Equal(Z, alvo.Z);
    }

    [Fact]
    public void Nascimento_do_hub_nao_fica_em_cima_do_boneco_nem_na_linha_da_capita()
    {
        var nascimento = ValidadorDeMovimento.Nascimento;

        // Nasce fora do alcance do golpe: o boneco fica ao lado da praça (#1186).
        Assert.Empty(
            Novo()
                .AlvosNoVolume(AreasDoMundo.Cidade, nascimento.X, nascimento.Z, nascimento.RotacaoY)
        );
        Assert.True(
            Math.Abs(ConfigBonecoDeTreino.X - nascimento.X) >= 3.0,
            "O boneco deve ficar ao lado do nascimento, não à frente dele."
        );
        // A linha reta do nascimento até a Capitã Valéria (x 0, z -32) fica livre.
        var valeria = JogoDoGrupo.Shared.Features.Npcs.CatalogoNpcs.Valeria;
        Assert.True(Math.Abs(ConfigBonecoDeTreino.X - valeria.X) > ConfigBonecoDeTreino.Raio + 1.0);
    }

    [Fact]
    public void Dano_tira_vida_e_devolve_o_que_sobrou()
    {
        var boneco = Novo();

        Assert.True(boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 20, 1, out var r));

        Assert.Equal(new ResultadoDeDano(ConfigBonecoDeTreino.AlvoId, 20, 160, 180), r);
        Assert.Equal(160, boneco.VidaAtual);
        Assert.Equal(160, Assert.Single(NoVolume(boneco)).VidaAtual);
    }

    [Fact]
    public void Dano_maior_que_a_vida_so_tira_o_que_tem_e_o_boneco_sai_de_combate()
    {
        var boneco = Novo();
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 170, 1, out _);

        Assert.True(boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 44, 1, out var r));

        Assert.Equal(10, r.Dano);
        Assert.Equal(0, r.VidaRestante);
        Assert.Empty(NoVolume(boneco));
        Assert.False(boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 20, 1, out _));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Dano_zero_ou_negativo_e_recusado_e_nao_cura(int dano)
    {
        var boneco = Novo();
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 50, 1, out _);

        Assert.False(boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, dano, 1, out _));
        Assert.Equal(130, boneco.VidaAtual);
    }

    [Fact]
    public void Id_que_nao_e_do_boneco_e_recusado()
    {
        Assert.False(Novo().TentarAplicarDano(12345, 20, 1, out _));
    }

    [Fact]
    public void Volta_a_vida_cheia_5_segundos_depois_do_ultimo_golpe()
    {
        var boneco = Novo();
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 100, 1, out _);

        Passar(4.9);
        Assert.Equal(80, boneco.VidaAtual);

        Passar(0.1);
        Assert.Equal(180, boneco.VidaAtual);
        Assert.Equal(180, Assert.Single(NoVolume(boneco)).VidaAtual);
    }

    [Fact]
    public void Cada_golpe_reinicia_a_contagem_dos_5_segundos()
    {
        var boneco = Novo();
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 20, 1, out _);
        Passar(3);
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 20, 1, out _);

        Passar(3); // 6 s do primeiro golpe, 3 s do último
        Assert.Equal(140, boneco.VidaAtual);

        Passar(2); // 5 s do último
        Assert.Equal(180, boneco.VidaAtual);
    }

    [Fact]
    public void Boneco_derrotado_volta_a_ser_alvo_depois_de_5_segundos()
    {
        var boneco = Novo();
        boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 500, 1, out _);
        Assert.Empty(NoVolume(boneco));

        Passar(5);

        Assert.Equal(180, Assert.Single(NoVolume(boneco)).VidaAtual);
    }

    [Fact]
    public void Fora_do_alcance_ou_do_cone_ou_de_outra_area_nao_e_alvo()
    {
        var boneco = Novo();

        // Alcance 2,5 m mais o raio do corpo (0,5 m): 3 m ainda pega, 3,1 m não.
        Assert.Single(boneco.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 3.0, 0));
        Assert.Empty(boneco.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 3.1, 0));
        // De costas para o boneco.
        Assert.Empty(boneco.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 2, 180));
        // Cone de 90 graus: 45 graus ainda pega, 50 não.
        Assert.Single(boneco.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 2, 45));
        Assert.Empty(boneco.AlvosNoVolume(AreasDoMundo.Cidade, X, Z - 2, 50));
        Assert.Empty(boneco.AlvosNoVolume("dungeon", X, Z - 2, 0));
    }

    [Fact]
    public async Task Golpes_simultaneos_nunca_tiram_mais_que_a_vida()
    {
        var boneco = Novo();
        var total = 0;

        await Task.WhenAll(
            Enumerable
                .Range(0, 50)
                .Select(_ =>
                    Task.Run(() =>
                    {
                        if (boneco.TentarAplicarDano(ConfigBonecoDeTreino.AlvoId, 20, 1, out var r))
                        {
                            Interlocked.Add(ref total, r.Dano);
                        }
                    })
                )
        );

        Assert.Equal(ConfigBonecoDeTreino.Vida, total);
        Assert.Equal(0, boneco.VidaAtual);
    }

    [Fact]
    public void Valores_de_balanceamento_do_boneco()
    {
        Assert.Equal(180, ConfigBonecoDeTreino.Vida);
        Assert.Equal(5.0, ConfigBonecoDeTreino.SegundosParaRecuperar);
    }
}
