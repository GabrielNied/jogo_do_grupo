using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Personagem;

public class DiretorioDePersonagensTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private IDiretorioDePersonagens Diretorio =>
        servidor.Services.GetRequiredService<IDiretorioDePersonagens>();

    private async Task<(long Id, string Nome)> CriarAsync()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("D");
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = Raca.Humano }
        );
        resposta.EnsureSuccessStatusCode();
        return ((await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id, nome);
    }

    [Fact]
    public async Task Personagem_recem_criado_e_achado_pelo_nome()
    {
        var (id, nome) = await CriarAsync();
        Assert.True(Diretorio.TentarObterPorNome(nome, out var achado));
        Assert.Equal(id, achado);
    }

    [Fact]
    public async Task Acha_pelo_nome_com_maiusculas_diferentes()
    {
        var (id, nome) = await CriarAsync();
        Assert.True(Diretorio.TentarObterPorNome(nome.ToUpperInvariant(), out var maiusculo));
        Assert.True(Diretorio.TentarObterPorNome(nome.ToLowerInvariant(), out var minusculo));
        Assert.Equal(id, maiusculo);
        Assert.Equal(id, minusculo);
    }

    [Fact]
    public void Nome_que_nao_existe_devolve_falso()
    {
        Assert.False(Diretorio.TentarObterPorNome(ServidorDeTeste.NomeUnico("N"), out var id));
        Assert.Equal(0L, id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("nome com espaco")]
    [InlineData("Robert'; DROP TABLE Personagens;--")]
    [InlineData("nomelongodemaisparaserumnome")]
    public void Texto_que_nao_e_nome_valido_devolve_falso(string texto)
    {
        Assert.False(Diretorio.TentarObterPorNome(texto, out var id));
        Assert.Equal(0L, id);
    }

    [Fact]
    public void Nome_nulo_devolve_falso() =>
        Assert.False(Diretorio.TentarObterPorNome(null!, out _));

    [Fact]
    public async Task Devolve_o_nome_como_foi_digitado()
    {
        var (id, nome) = await CriarAsync();
        Assert.True(Diretorio.TentarObterNome(id, out var achado));
        Assert.Equal(nome, achado);
    }

    [Theory]
    [InlineData(long.MaxValue)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Id_que_nao_existe_devolve_falso_e_nome_vazio(long id)
    {
        Assert.False(Diretorio.TentarObterNome(id, out var nome));
        Assert.Equal("", nome);
    }

    [Fact]
    public async Task Personagem_apagado_some_das_duas_consultas()
    {
        var (id, nome) = await CriarAsync();
        using (var escopo = servidor.Services.CreateScope())
        {
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            await db.Set<Server.Features.Personagem.Personagem>()
                .Where(p => p.Id == id)
                .ExecuteDeleteAsync();
        }

        Assert.False(Diretorio.TentarObterPorNome(nome, out _));
        Assert.False(Diretorio.TentarObterNome(id, out _));
    }

    [Fact]
    public async Task Consultas_simultaneas_de_varias_threads_respondem_certo()
    {
        var (id, nome) = await CriarAsync();
        var diretorio = Diretorio;
        var consultas = Enumerable
            .Range(0, 32)
            .Select(_ =>
                Task.Run(() =>
                    diretorio.TentarObterPorNome(nome, out var achado)
                    && achado == id
                    && diretorio.TentarObterNome(id, out var achadoNome)
                    && achadoNome == nome
                )
            );
        Assert.All(await Task.WhenAll(consultas), certo => Assert.True(certo));
    }
}
