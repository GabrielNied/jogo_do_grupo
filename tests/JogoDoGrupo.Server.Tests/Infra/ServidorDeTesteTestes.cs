using System.Net;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Tests.Infra;

public class ServidorDeTesteTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    [Fact]
    public async Task Cliente_logado_acessa_rota_protegida()
    {
        var cliente = await servidor.CriarClienteLogadoAsync();

        var resposta = await cliente.GetAsync(RotasPersonagem.Personagens);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
    }
}
