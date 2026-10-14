using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Features.Mundo;
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
    public async Task No_nascimento_retrato_respeita_raio_de_interesse()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var todos = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);
        var comuns = todos
            .Inimigos.Where(i =>
                CatalogoCriaturas.Obter(i.CriaturaId).Categoria == CategoriaDeCriatura.Comum
            )
            .ToArray();

        var nascimento = ValidadorDeMovimento.Nascimento;
        Assert.All(
            comuns,
            i =>
            {
                var dist = RegrasInteresseDeInimigos.DistanciaAoJogador(
                    nascimento.X,
                    nascimento.Z,
                    i.X,
                    i.Z
                );
                Assert.True(dist <= RegrasInteresseDeInimigos.EntradaMetros);
            }
        );
        Assert.Empty(comuns);
    }

    [Fact]
    public async Task Perto_do_campo_recebe_retrato_de_inimigos()
    {
        var (cliente, _, personagemId) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await Task.WhenAll(
            cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade),
            cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea)
        );

        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        var (aceito, _) = teleporte.Teleportar(personagemId, 0f, 0f, 175f);
        Assert.True(aceito);

        var servico = f.Fabrica.Services.GetRequiredService<ServicoDeInimigos>();
        servico.AoEntrarNaArea(new JogadorEntrouNaArea(personagemId, ConfigInimigosDoCampo.Area));

        var todos = await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);
        var comuns = todos
            .Inimigos.Where(i =>
                CatalogoCriaturas.Obter(i.CriaturaId).Categoria == CategoriaDeCriatura.Comum
            )
            .ToArray();

        Assert.InRange(comuns.Length, 1, PontosDeSpawnDoCampo.LimiteDeInimigosVivos);
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

            Assert.InRange(doPonto.Length, 0, ponto.TamanhoMaximo);
            var (centroX, centroZ) =
                ponto.Rota == null
                    ? (ponto.X, ponto.Z)
                    : (doPonto.Average(i => i.X), doPonto.Average(i => i.Z));
            var confereRaio = ponto.Rota == null;
            Assert.All(
                doPonto,
                i =>
                {
                    Assert.Equal(ponto.CriaturaId, i.CriaturaId);
                    Assert.InRange(i.Nivel, ponto.NivelMinimo, ponto.NivelMaximo);
                    Assert.Contains(
                        i.Estado,
                        new[]
                        {
                            EstadoDeInimigo.Repouso,
                            EstadoDeInimigo.Retorno,
                            EstadoDeInimigo.Alerta,
                            EstadoDeInimigo.Perseguicao,
                            EstadoDeInimigo.Ataque,
                        }
                    );
                    Assert.Equal(i.VidaMaxima, i.Vida);
                    Assert.Equal(
                        EscalaPorNivel.Vida(
                            RegrasCriaturas.VidaMaxima(ficha.VidaBase, i.Variante),
                            i.Nivel,
                            ficha.Nivel
                        ),
                        i.VidaMaxima
                    );
                    if (confereRaio)
                    {
                        var doCentro = RegrasInimigo.Distancia(centroX, centroZ, i.X, i.Z);
                        Assert.Equal(ConfigInimigosDoCampo.RaioDoEspalhamentoMetros, doCentro, 5);
                    }
                }
            );
        }
    }

    [Fact]
    public async Task Aproximar_do_campo_sem_reentrada_recebe_inimigo_apareceu()
    {
        var (cliente, _, personagemId) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);

        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        Assert.True(teleporte.Teleportar(personagemId, 0f, 0f, 175f).Aceito);

        var servico = f.Fabrica.Services.GetRequiredService<ServicoDeInimigos>();
        servico.Avancar();

        var apareceu = await cliente.ReceberAsync<InimigoApareceu>(MobsMessageIds.InimigoApareceu);
        Assert.NotNull(apareceu.Inimigo);
        Assert.Equal(
            CategoriaDeCriatura.Comum,
            CatalogoCriaturas.Obter(apareceu.Inimigo.CriaturaId).Categoria
        );
    }

    [Fact]
    public async Task Matar_inimigo_no_campo_manda_inimigo_morreu()
    {
        var (cliente, _, personagemId) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<InimigosDaArea>(MobsMessageIds.InimigosDaArea);

        var teleporte = f.Fabrica.Services.GetRequiredService<ITeleporteDeTeste>();
        Assert.True(teleporte.Teleportar(personagemId, 130f, 0f, 170f).Aceito);

        var servico = f.Fabrica.Services.GetRequiredService<ServicoDeInimigos>();
        servico.Avancar();
        var apareceu = await cliente.ReceberAsync<InimigoApareceu>(MobsMessageIds.InimigoApareceu);
        var alvoId = apareceu.Inimigo.InimigoId;

        var grupos = f.Fabrica.Services.GetRequiredService<GruposDoCampo>().Grupos;
        var grupo = grupos.First(g => g.Vivos().Any(i => i.InimigoId == alvoId));
        Assert.True(grupo.TentarAplicarDano(alvoId, 1_000_000, personagemId, out _));

        servico.Avancar();

        var morreu = await cliente.ReceberAsync<InimigoMorreu>(MobsMessageIds.InimigoMorreu);
        Assert.Equal(alvoId, morreu.InimigoId);
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
