using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

public class EquipamentosParaCombateTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    [Theory]
    [InlineData(CatalogoEquipamentos.CajadoDeFogoDeTeste, ElementoDaArma.Fogo)]
    [InlineData(CatalogoEquipamentos.CajadoDeGeloDeTeste, ElementoDaArma.Gelo)]
    [InlineData(CatalogoEquipamentos.CajadoDeCuraDeTeste, ElementoDaArma.Cura)]
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma + 2, ElementoDaArma.Nenhum)] // Cajado Neutro
    [InlineData(CatalogoEquipamentos.PrimeiroModeloDeArma, ElementoDaArma.Nenhum)] // Espada
    public async Task ObterArmaEquipada_RetornaElementoCorretoDaArmaAtiva(
        int modeloId,
        ElementoDaArma elementoEsperado
    )
    {
        var (cliente, id) = await Personagem();
        var itemInstanciaId = await InserirItem(id, modeloId, PosicaoEquipamento.Arma1);

        using var escopo = servidor.Services.CreateScope();
        var servicoArma = escopo.ServiceProvider.GetRequiredService<IArmaEquipada>();

        var arma = await servicoArma.ObterAsync(id, CancellationToken.None);

        Assert.NotNull(arma);
        Assert.Equal(itemInstanciaId, arma.Value.InstanciaId);
        Assert.Equal(modeloId, arma.Value.ModeloId);
        Assert.Equal(elementoEsperado, arma.Value.Elemento);
        Assert.Equal(1, arma.Value.Conjunto);
    }

    [Fact]
    public async Task ObterArmaEquipada_IgnoraArmaQuebrada()
    {
        var (cliente, id) = await Personagem();
        await InserirItem(
            id,
            CatalogoEquipamentos.CajadoDeFogoDeTeste,
            PosicaoEquipamento.Arma1,
            durabilidadeAtual: 0
        );

        using var escopo = servidor.Services.CreateScope();
        var servicoArma = escopo.ServiceProvider.GetRequiredService<IArmaEquipada>();

        var arma = await servicoArma.ObterAsync(id, CancellationToken.None);

        Assert.Null(arma);
    }

    [Fact]
    public async Task ObterArmaEquipada_SemArma_RetornaNull()
    {
        var (cliente, id) = await Personagem();

        using var escopo = servidor.Services.CreateScope();
        var servicoArma = escopo.ServiceProvider.GetRequiredService<IArmaEquipada>();

        var arma = await servicoArma.ObterAsync(id, CancellationToken.None);

        Assert.Null(arma);
    }

    private async Task<(HttpClient Cliente, long Id)> Personagem()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();
        var resposta = await ServidorDeTeste.PostJsonAsync(
            cliente,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = ServidorDeTeste.NomeUnico(), Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        return (cliente, (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id);
    }

    private async Task<long> InserirItem(
        long personagemId,
        int modeloId,
        PosicaoEquipamento posicao,
        int durabilidadeAtual = 100
    )
    {
        using var escopo = servidor.Services.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        var item = new ItemInventario
        {
            PersonagemId = personagemId,
            ItemId = modeloId,
            Posicao = posicao,
        };
        var dados = CatalogoEquipamentos.Obter(modeloId);
        if (dados != null)
        {
            dados.DurabilidadeAtual = durabilidadeAtual;
            item.GravarEquipamento(dados);
        }
        db.Add(item);
        await db.SaveChangesAsync();
        return item.Id;
    }
}
