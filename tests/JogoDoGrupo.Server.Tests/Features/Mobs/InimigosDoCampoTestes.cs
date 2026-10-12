using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Integração: o servidor de verdade sobe com os grupos de inimigos do campo (um por ponto de
/// spawn) ligados ao Mundo e ao Combate. A IA em si é testada sem rede em
/// <see cref="GrupoDeInimigosTestes"/>.
/// </summary>
public class InimigosDoCampoTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task Quem_entra_na_area_recebe_os_inimigos_vivos_de_cada_ponto_de_spawn()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var todos = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);
        // O chefe raro pode estar no campo (sorteio); aqui só valem os grupos comuns.
        var comuns = todos
            .Inimigos.Where(i => i.CriaturaId != CatalogoCriaturas.PresaVelha)
            .ToArray();

        Assert.Equal(comuns.Length, comuns.Select(i => i.InimigoId).Distinct().Count());
        foreach (var ponto in PontosDeSpawnDoCampo.Todos)
        {
            var doPonto = comuns
                .Where(i =>
                    i.InimigoId >= ponto.PrimeiroId
                    && i.InimigoId < ponto.PrimeiroId + PontosDeSpawnDoCampo.EspacoDeIdsPorPonto
                )
                .ToArray();
            var ficha = CatalogoCriaturas.Obter(ponto.CriaturaId);

            Assert.InRange(doPonto.Length, ponto.TamanhoMinimo, ponto.TamanhoMaximo);
            Assert.All(
                doPonto,
                i =>
                {
                    Assert.Equal(ponto.CriaturaId, i.CriaturaId);
                    Assert.InRange(i.Nivel, ponto.NivelMinimo, ponto.NivelMaximo);
                    Assert.Equal(EstadoDeInimigo.Repouso, i.Estado);
                    Assert.Equal(i.VidaMaxima, i.Vida);
                    Assert.Equal(
                        EscalaPorNivel.Vida(
                            RegrasCriaturas.VidaMaxima(ficha.VidaBase, i.Variante),
                            i.Nivel,
                            ficha.Nivel
                        ),
                        i.VidaMaxima
                    );
                    var doCentro = RegrasInimigo.Distancia(ponto.X, ponto.Z, i.X, i.Z);
                    Assert.Equal(ConfigInimigosDoCampo.RaioDoEspalhamentoMetros, doCentro, 2);
                }
            );
        }
    }

    [Fact]
    public void Os_grupos_do_campo_sao_alvos_do_combate()
    {
        var grupos = f.Fabrica.Services.GetRequiredService<GruposDoCampo>().Grupos;
        var alvos = f.Fabrica.Services.GetServices<IAlvosDeCombate>();

        Assert.Equal(PontosDeSpawnDoCampo.Todos.Count, grupos.Count);
        Assert.All(
            grupos,
            grupo =>
            {
                Assert.Contains(alvos, alvo => ReferenceEquals(alvo, grupo));
                Assert.Equal(ConfigInimigosDoCampo.Area, grupo.Area);
            }
        );
    }
}
