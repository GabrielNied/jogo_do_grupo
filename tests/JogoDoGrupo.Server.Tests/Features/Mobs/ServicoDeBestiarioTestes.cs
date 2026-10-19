using JogoDoGrupo.Server.Features.Mobs;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Mobs;

/// <summary>
/// Testes de integração do serviço autoritativo de bestiário (#922).
/// </summary>
public sealed class ServicoDeBestiarioTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeBestiario, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeBestiario>());
    }

    [Fact]
    public async Task ObterBestiario_PersonagemNovo_RetornaTodasCriaturasComZeroDerrotasEDesconhecido()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var estado = await ComServicoAsync(s => s.ObterBestiarioAsync(id));

        Assert.NotNull(estado);
        Assert.Equal(CatalogoCriaturas.Todas.Length, estado.Entradas.Length);
        foreach (var entrada in estado.Entradas)
        {
            Assert.Equal(0, entrada.Derrotas);
            Assert.Equal(NivelDeConhecimentoBestiario.Desconhecido, entrada.Nivel);
        }
    }

    [Fact]
    public async Task RegistrarDerrota_PrimeiraDerrota_AvancaParaAvistadoComUmaDerrota()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var entrada = await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.JavaliDoCampo)
        );

        Assert.NotNull(entrada);
        Assert.Equal(CatalogoCriaturas.JavaliDoCampo, entrada.CriaturaId);
        Assert.Equal(1, entrada.Derrotas);
        Assert.Equal(NivelDeConhecimentoBestiario.Avistado, entrada.Nivel);

        var estado = await ComServicoAsync(s => s.ObterBestiarioAsync(id));
        var registroJavali = Assert.Single(
            estado.Entradas,
            e => e.CriaturaId == CatalogoCriaturas.JavaliDoCampo
        );
        Assert.Equal(1, registroJavali.Derrotas);
        Assert.Equal(NivelDeConhecimentoBestiario.Avistado, registroJavali.Nivel);
    }

    [Fact]
    public async Task RegistrarDerrota_MultiplasDerrotas_AvancaNivelDeConhecimento()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        for (int i = 1; i <= 5; i++)
        {
            var res = await ComServicoAsync(s =>
                s.RegistrarDerrotaAsync(id, CatalogoCriaturas.LoboDoCampo)
            );
            Assert.Equal(i, res?.Derrotas);
        }

        var estadoApos5 = await ComServicoAsync(s => s.ObterBestiarioAsync(id));
        var lobo = Assert.Single(
            estadoApos5.Entradas,
            e => e.CriaturaId == CatalogoCriaturas.LoboDoCampo
        );
        Assert.Equal(5, lobo.Derrotas);
        Assert.Equal(NivelDeConhecimentoBestiario.Estudado, lobo.Nivel);

        // Mais 15 derrotas para chegar a 20 (Mestre)
        for (int i = 6; i <= 20; i++)
        {
            await ComServicoAsync(s => s.RegistrarDerrotaAsync(id, CatalogoCriaturas.LoboDoCampo));
        }

        var estadoApos20 = await ComServicoAsync(s => s.ObterBestiarioAsync(id));
        var loboMestre = Assert.Single(
            estadoApos20.Entradas,
            e => e.CriaturaId == CatalogoCriaturas.LoboDoCampo
        );
        Assert.Equal(20, loboMestre.Derrotas);
        Assert.Equal(NivelDeConhecimentoBestiario.Mestre, loboMestre.Nivel);
    }

    [Fact]
    public async Task RegistrarDerrota_Chefe_UsaThresholdsReduzidos()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // 1 derrota -> Avistado
        var r1 = await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.GuardiaoDoReflexo)
        );
        Assert.Equal(NivelDeConhecimentoBestiario.Avistado, r1?.Nivel);

        // 3 derrotas -> Estudado
        await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.GuardiaoDoReflexo)
        );
        var r3 = await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.GuardiaoDoReflexo)
        );
        Assert.Equal(NivelDeConhecimentoBestiario.Estudado, r3?.Nivel);

        // 5 derrotas -> Mestre
        await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.GuardiaoDoReflexo)
        );
        var r5 = await ComServicoAsync(s =>
            s.RegistrarDerrotaAsync(id, CatalogoCriaturas.GuardiaoDoReflexo)
        );
        Assert.Equal(NivelDeConhecimentoBestiario.Mestre, r5?.Nivel);
    }

    [Fact]
    public async Task RegistrarDerrota_CriaturaInexistente_RetornaNullENaoAlteraBanco()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var resultado = await ComServicoAsync(s => s.RegistrarDerrotaAsync(id, 999999));

        Assert.Null(resultado);
    }

    [Fact]
    public async Task Isolamento_ProgressoNaoVazaEntrePersonagens()
    {
        var id1 = await _ambiente.NovoPersonagemAsync();
        var id2 = await _ambiente.NovoPersonagemAsync();

        await ComServicoAsync(s => s.RegistrarDerrotaAsync(id1, CatalogoCriaturas.TatuRochoso));
        await ComServicoAsync(s => s.RegistrarDerrotaAsync(id1, CatalogoCriaturas.TatuRochoso));

        var estado1 = await ComServicoAsync(s => s.ObterBestiarioAsync(id1));
        var tatu1 = Assert.Single(
            estado1.Entradas,
            e => e.CriaturaId == CatalogoCriaturas.TatuRochoso
        );
        Assert.Equal(2, tatu1.Derrotas);

        var estado2 = await ComServicoAsync(s => s.ObterBestiarioAsync(id2));
        var tatu2 = Assert.Single(
            estado2.Entradas,
            e => e.CriaturaId == CatalogoCriaturas.TatuRochoso
        );
        Assert.Equal(0, tatu2.Derrotas);
        Assert.Equal(NivelDeConhecimentoBestiario.Desconhecido, tatu2.Nivel);
    }
}
