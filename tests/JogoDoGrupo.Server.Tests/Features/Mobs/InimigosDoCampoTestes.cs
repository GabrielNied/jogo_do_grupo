using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Integração: o servidor de verdade sobe com o grupo de inimigos do campo ligado ao Mundo e
/// ao Combate. A IA em si é testada sem rede em <see cref="GrupoDeInimigosTestes"/>.
/// </summary>
public class InimigosDoCampoTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task Quem_entra_na_area_recebe_os_inimigos_vivos_do_campo()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var todos = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);
        // O chefe raro pode estar no campo (sorteio); aqui só vale o grupo de javalis.
        var area = new InimigosDaArea
        {
            Inimigos = todos
                .Inimigos.Where(i => i.CriaturaId == RegrasInimigo.CriaturaDaFatia)
                .ToArray(),
        };

        var javali = CatalogoCriaturas.Obter(RegrasInimigo.CriaturaDaFatia);
        Assert.InRange(
            area.Inimigos.Length,
            RegrasInimigo.TamanhoMinimoDoGrupo,
            RegrasInimigo.TamanhoMaximoDoGrupo
        );
        Assert.Equal(
            area.Inimigos.Length,
            area.Inimigos.Select(i => i.InimigoId).Distinct().Count()
        );
        Assert.All(
            area.Inimigos,
            i =>
            {
                Assert.True(i.InimigoId >= ConfigInimigosDoCampo.PrimeiroInimigoId);
                Assert.Equal(javali.Id, i.CriaturaId);
                Assert.Equal(EstadoDeInimigo.Repouso, i.Estado);
                Assert.Equal(i.VidaMaxima, i.Vida);
                Assert.Equal(RegrasCriaturas.VidaMaxima(javali.VidaBase, i.Variante), i.VidaMaxima);
                var doCentro = RegrasInimigo.Distancia(
                    ConfigInimigosDoCampo.PontoX,
                    ConfigInimigosDoCampo.PontoZ,
                    i.X,
                    i.Z
                );
                Assert.Equal(ConfigInimigosDoCampo.RaioDoEspalhamentoMetros, doCentro, 2);
            }
        );
    }

    [Fact]
    public void O_grupo_do_campo_e_um_dos_alvos_do_combate()
    {
        var grupo = f.Fabrica.Services.GetRequiredService<GrupoDeInimigos>();
        var alvos = f.Fabrica.Services.GetServices<IAlvosDeCombate>();

        Assert.Contains(alvos, alvo => ReferenceEquals(alvo, grupo));
        Assert.Equal(ConfigInimigosDoCampo.Area, grupo.Area);
    }
}
