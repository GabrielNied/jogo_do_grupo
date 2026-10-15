using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Instancias;

/// <summary>Vitória na dungeon credita a essência pelo serviço real com EF (MAT-003).</summary>
public sealed class VitoriaCreditaEssenciaTestes
{
    [Fact]
    public async Task Vitoria_credita_essencia_de_dungeon_uma_vez_no_saldo_real()
    {
        using var f = new InstanciasFixture();
        using var jogador = await f.EntrarAsync();
        jogador.Cliente.Enviar(InstanciasMessageIds.EntrarNaDungeon, new EntrarNaDungeon());
        var inicio = await jogador.Cliente.ReceberAsync<DungeonIniciada>(
            InstanciasMessageIds.DungeonIniciada
        );

        var derrota = new InimigoDerrotado(
            1,
            CatalogoCriaturas.GuardiaoDoReflexo,
            VarianteDeCriatura.Normal,
            500,
            1,
            inicio.Area,
            LayoutDaMinaDoReflexo.BossCentroX,
            LayoutDaMinaDoReflexo.BossCentroZ,
            [jogador.PersonagemId],
            1,
            5
        );
        f.Instancias.AoInimigoDerrotado(derrota);

        // O crédito roda em segundo plano: espera o saldo persistido aparecer.
        Assert.Equal((1, 0), await EsperarSaldoAsync(f, jogador.PersonagemId, 1));

        // Vitória repetida no mesmo run (outro golpe final ou o gancho) não credita de novo.
        f.Instancias.AoInimigoDerrotado(derrota with { Sequencia = 2 });
        f.Instancias.AgendarVitoria(inicio.IdInstancia);
        await Task.Delay(500);
        Assert.Equal((1, 0), await LerSaldoAsync(f, jogador.PersonagemId));

        // É o mesmo saldo que a fusão debita: gasta 1, a segunda tentativa falha e o banco zera.
        await using (var escopo = f.Fabrica.Services.CreateAsyncScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var materiais = escopo.ServiceProvider.GetRequiredService<ISaldosMateriaisFusao>();
            await using var transacao = await db.Database.BeginTransactionAsync();
            Assert.True(await materiais.DebitarAsync(jogador.PersonagemId, 1, 0, default));
            Assert.False(await materiais.DebitarAsync(jogador.PersonagemId, 1, 0, default));
            await transacao.CommitAsync();
        }
        Assert.Equal((0, 0), await LerSaldoAsync(f, jogador.PersonagemId));
    }

    private static async Task<(int Dungeon, int Raid)> LerSaldoAsync(
        InstanciasFixture f,
        long personagemId
    )
    {
        await using var escopo = f.Fabrica.Services.CreateAsyncScope();
        return await escopo
            .ServiceProvider.GetRequiredService<ISaldosMateriaisFusao>()
            .SaldoAsync(personagemId, default);
    }

    private static async Task<(int Dungeon, int Raid)> EsperarSaldoAsync(
        InstanciasFixture f,
        long personagemId,
        int dungeonEsperado
    )
    {
        var limite = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var saldo = await LerSaldoAsync(f, personagemId);
            if (saldo.Dungeon >= dungeonEsperado || DateTime.UtcNow > limite)
                return saldo;
            await Task.Delay(50);
        }
    }
}
