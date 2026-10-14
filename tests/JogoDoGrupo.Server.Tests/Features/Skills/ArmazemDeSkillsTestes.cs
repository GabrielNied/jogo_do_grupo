using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Server.Features.Skills;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Skills;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Skills;

/// <summary>Sorteador que o teste controla.</summary>
public sealed class SorteadorFixo(int valor) : ISorteadorDeSkills
{
    public int Proximo(int limite) => Math.Min(valor, limite - 1);
}

/// <summary>
/// Com o banco e o servidor de verdade: o sorteio fica gravado no exemplar, e a cobrança de
/// recursos de Combate desconta uma vez só.
/// </summary>
public sealed class ArmazemDeSkillsTestes : IClassFixture<ServidorDeTeste>, IDisposable
{
    private readonly WebApplicationFactory<Program> _fabrica;

    public ArmazemDeSkillsTestes(ServidorDeTeste servidor) =>
        _fabrica = servidor.WithWebHostBuilder(_ => { });

    public void Dispose() => _fabrica.Dispose();

    private IServiceScopeFactory Escopos =>
        _fabrica.Services.GetRequiredService<IServiceScopeFactory>();

    private static long _proximo = 1_000;

    private static ArmaEquipada NovaArma(TipoEquipamento tipo, int modelo = 1) =>
        new(Interlocked.Increment(ref _proximo), modelo, tipo, 1);

    [Theory]
    [InlineData(TipoEquipamento.Espada)]
    [InlineData(TipoEquipamento.Cajado)]
    [InlineData(TipoEquipamento.Arco)]
    public async Task Sorteio_so_usa_o_pool_do_modelo_base_e_nao_repete(TipoEquipamento tipo)
    {
        var armazem = new ArmazemDeSkills(Escopos, new SorteadorDeSkillsAleatorio());

        var ids = await armazem.ObterOuSortearAsync(NovaArma(tipo), CancellationToken.None);

        var pool = CatalogoDeSkills.Pool(tipo).Select(s => s.Id).ToArray();
        Assert.Equal(ConfigSkills.SkillsPorArma, ids.Count);
        Assert.All(ids, id => Assert.Contains(id, pool));
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public async Task Skills_do_exemplar_sao_estaveis_inclusive_depois_de_reiniciar()
    {
        var arma = NovaArma(TipoEquipamento.Espada);
        var primeiro = new ArmazemDeSkills(Escopos, new SorteadorFixo(0));
        var ids = await primeiro.ObterOuSortearAsync(arma, CancellationToken.None);
        Assert.Equal(ids, await primeiro.ObterOuSortearAsync(arma, CancellationToken.None));

        // Outro armazém (servidor reiniciado), outro sorteio: lê o que está gravado.
        var depois = new ArmazemDeSkills(Escopos, new SorteadorFixo(3));
        Assert.Equal(ids, await depois.ObterOuSortearAsync(arma, CancellationToken.None));

        // A fusão mantém o exemplar principal (mesmo id): as skills continuam.
        Assert.Equal(
            ids,
            await depois.ObterOuSortearAsync(arma with { Conjunto = 2 }, CancellationToken.None)
        );
    }

    [Fact]
    public async Task Pedidos_simultaneos_da_arma_nova_sorteiam_uma_vez_so()
    {
        var arma = NovaArma(TipoEquipamento.Martelo);
        var armazem = new ArmazemDeSkills(Escopos, new SorteadorDeSkillsAleatorio());

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 20)
                .Select(_ =>
                    Task.Run(() => armazem.ObterOuSortearAsync(arma, CancellationToken.None))
                )
        );

        Assert.Single(resultados.Select(r => string.Join(',', r)).Distinct());
        using var escopo = Escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        Assert.Equal(
            1,
            await db.Set<SkillsDoExemplar>().CountAsync(s => s.InstanciaId == arma.InstanciaId)
        );
    }

    [Fact]
    public async Task Trocar_o_modelo_do_exemplar_sorteia_de_novo()
    {
        var arma = NovaArma(TipoEquipamento.Espada, modelo: 1);
        var armazem = new ArmazemDeSkills(Escopos, new SorteadorFixo(0));
        await armazem.ObterOuSortearAsync(arma, CancellationToken.None);

        var ids = await armazem.ObterOuSortearAsync(
            arma with
            {
                ModeloId = 2,
                Tipo = TipoEquipamento.Cajado,
            },
            CancellationToken.None
        );

        Assert.All(ids, id => Assert.StartsWith("cajado.", id));
    }

    [Fact]
    public async Task Cobranca_real_desconta_uma_vez_so_com_pedidos_simultaneos()
    {
        using var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(_fabrica);
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;
        var recursos = _fabrica.Services.GetRequiredService<RecursosEmMemoria>();
        const int custo = 40;
        await recursos.AtualizarAsync(id, (r, _) => r.Mana = custo, CancellationToken.None);
        var cobranca = _fabrica.Services.GetRequiredService<ICobrancaDeRecursos>();

        var resultados = await Task.WhenAll(
            Enumerable
                .Range(0, 30)
                .Select(_ =>
                    Task.Run(() =>
                        cobranca.TentarCobrarAsync(
                            id,
                            RecursoCobravel.Mana,
                            custo,
                            CancellationToken.None
                        )
                    )
                )
        );

        Assert.Single(resultados, r => r.Resultado == ResultadoDaCobranca.Cobrado);
        Assert.All(
            resultados.Where(r => r.Resultado != ResultadoDaCobranca.Cobrado),
            r => Assert.Equal(ResultadoDaCobranca.Insuficiente, r.Resultado)
        );
        Assert.Equal(0, resultados.Single(r => r.Recursos != null).Recursos!.ManaAtual);
    }
}

internal sealed class SorteadorDeSkillsAleatorio : ISorteadorDeSkills
{
    public int Proximo(int limite) => Random.Shared.Next(limite);
}
