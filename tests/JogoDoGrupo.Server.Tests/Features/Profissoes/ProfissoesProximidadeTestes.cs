using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Features.Profissoes;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Profissoes;

public sealed class ServidorDeProfissoesComProximidade : IDisposable
{
    public ServidorDeTeste Base { get; } = new();
    public PosicoesFalsas Posicoes { get; } = new();
    public WebApplicationFactory<Program> Servidor { get; }

    public ServidorDeProfissoesComProximidade()
    {
        Servidor = Base.WithWebHostBuilder(b =>
        {
            b.UseSetting("Profissoes:ExigirProximidade", "true");
            b.ConfigureServices(s => s.AddSingleton<IPosicoesDoMundo>(Posicoes));
        });
    }

    public void Dispose()
    {
        Servidor.Dispose();
        Base.Dispose();
    }
}

/// <summary>O servidor confere a distância pela posição que ele mesmo aceitou (regra 3).</summary>
public sealed class ProfissoesProximidadeTestes(ServidorDeProfissoesComProximidade fixture)
    : IClassFixture<ServidorDeProfissoesComProximidade>
{
    private async Task<long> NovoAprendizAsync(params Profissao[] profissoes)
    {
        var id = await new AmbienteDeNegociacao(fixture.Base).NovoPersonagemAsync();
        foreach (var profissao in profissoes)
        {
            Assert.Null(await ComServicoAsync(s => s.AprenderAsync(id, profissao, default)));
        }

        return id;
    }

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeProfissoes, Task<T>> acao)
    {
        await using var escopo = fixture.Servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeProfissoes>());
    }

    [Fact]
    public async Task Longe_do_ponto_a_coleta_e_recusada_e_perto_funciona()
    {
        var id = await NovoAprendizAsync(Profissao.Lenhador);
        var ponto = ConfigProfissoes.ObterPonto(ConfigProfissoes.NoArvore2)!;

        fixture.Posicoes.Colocar(id, AreasDoMundo.Cidade, ponto.X + 20f, ponto.Z);
        var (longe, _, _) = await ComServicoAsync(s => s.ColetarAsync(id, ponto.Id, default));
        fixture.Posicoes.Colocar(id, AreasDoMundo.Cidade, ponto.X + 1f, ponto.Z);
        var (perto, _, _) = await ComServicoAsync(s => s.ColetarAsync(id, ponto.Id, default));

        Assert.Equal(CodigosErroProfissoes.Longe, longe);
        Assert.Null(perto);
    }

    [Fact]
    public async Task Sem_posicao_ou_em_outra_area_a_coleta_e_recusada()
    {
        var semPosicao = await NovoAprendizAsync(Profissao.Curtidor);
        var emOutraArea = await NovoAprendizAsync(Profissao.Curtidor);
        var ponto = ConfigProfissoes.ObterPonto(ConfigProfissoes.NoCarcaca2)!;
        fixture.Posicoes.Colocar(emOutraArea, "instancia:1", ponto.X, ponto.Z);

        var (a, _, _) = await ComServicoAsync(s => s.ColetarAsync(semPosicao, ponto.Id, default));
        var (b, _, _) = await ComServicoAsync(s => s.ColetarAsync(emOutraArea, ponto.Id, default));

        Assert.Equal(CodigosErroProfissoes.Longe, a);
        Assert.Equal(CodigosErroProfissoes.Longe, b);
    }

    [Fact]
    public async Task Longe_da_forja_a_producao_e_recusada()
    {
        var id = await NovoAprendizAsync(Profissao.Ferreiro);
        fixture.Posicoes.Colocar(
            id,
            AreasDoMundo.Cidade,
            ConfigProfissoes.ForjaX + 30f,
            ConfigProfissoes.ForjaZ
        );

        var (erro, _) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaEspada, default)
        );

        Assert.Equal(CodigosErroProfissoes.Longe, erro);
    }
}
