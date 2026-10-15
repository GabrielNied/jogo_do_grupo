using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>
/// Aliados do grupo no mapa a qualquer distância (#789): quem é do grupo e está fora da área de
/// interesse chega por <see cref="PosicoesDosAliados"/>; quem não é do grupo não recebe.
/// </summary>
public class PosicaoDoGrupoTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(ClienteTempoRealDeTeste Cliente, long Id, string Nome)> ConectarAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("G");
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;

        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(porta, token, id);
        Assert.True(resultado.Aceita, resultado.Codigo);
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        return (cliente, id, nome);
    }

    private static void Mover(ClienteTempoRealDeTeste cliente, float x, int sequencia)
    {
        var z = ValidadorDeMovimento.Nascimento.Z;
        cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = x,
                Y = MapaDoMundo.AlturaEm(x, z),
                Z = z,
                Sequencia = sequencia,
            }
        );
    }

    private static async Task FormarGrupoAsync(
        ClienteTempoRealDeTeste lider,
        ClienteTempoRealDeTeste convidado,
        long idConvidado,
        string nomeConvidado
    )
    {
        lider.Enviar(
            SocialMessageIds.ConvidarParaGrupo,
            new ConvidarParaGrupo { Nome = nomeConvidado }
        );
        var convite = await convidado.ReceberAsync<ConviteRecebido>(
            SocialMessageIds.ConviteRecebido
        );
        convidado.Enviar(
            SocialMessageIds.ResponderConvite,
            new ResponderConvite { ConviteId = convite.ConviteId, Aceitar = true }
        );
        ListaDoGrupo lista;
        do
        {
            lista = await lider.ReceberAsync<ListaDoGrupo>(SocialMessageIds.ListaDoGrupo);
        } while (lista.Membros.All(m => m.PersonagemId != idConvidado));
    }

    private static async Task<PosicoesDosAliados> UltimaAsync(
        ClienteTempoRealDeTeste cliente,
        Func<PosicoesDosAliados, bool> serve
    )
    {
        using var limite = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var mensagem = await cliente
                .ReceberAsync<PosicoesDosAliados>(MundoMessageIds.PosicoesDosAliados)
                .WaitAsync(limite.Token);
            if (serve(mensagem))
            {
                return mensagem;
            }
        }
    }

    [Fact]
    public async Task Aliado_longe_chega_ao_membro_e_quem_nao_e_do_grupo_nao_recebe()
    {
        var (a, idA, _) = await ConectarAsync();
        using var _a = a;
        var (b, idB, nomeB) = await ConectarAsync();
        using var _b = b;
        var (c, _, _) = await ConectarAsync();
        using var _c = c;
        await FormarGrupoAsync(a, b, idB, nomeB);

        // B se afasta para muito além do raio de saída: A e C deixam de vê-lo.
        Mover(b, 800, 1);
        await Task.Delay(300);
        servidor.Services.GetRequiredService<ServicoDePosicaoDoGrupo>().Sincronizar();

        var paraA = await UltimaAsync(a, m => m.Aliados.Length == 1);
        var aliado = Assert.Single(paraA.Aliados);
        Assert.Equal(idB, aliado.PersonagemId);
        Assert.InRange(aliado.X, 799f, 801f);

        // Quem não é do grupo nunca recebe a posição de ninguém.
        Assert.DoesNotContain(MundoMessageIds.PosicoesDosAliados, c.IdsRecebidos);
        Assert.NotEqual(idA, idB);
    }

    [Fact]
    public async Task Aliado_perto_nao_vem_por_aqui_porque_ja_chega_pela_presenca()
    {
        var (a, _, _) = await ConectarAsync();
        using var _a = a;
        var (b, idB, nomeB) = await ConectarAsync();
        using var _b = b;
        await FormarGrupoAsync(a, b, idB, nomeB);

        servidor.Services.GetRequiredService<ServicoDePosicaoDoGrupo>().Sincronizar();

        var paraA = await UltimaAsync(a, _ => true);
        Assert.Empty(paraA.Aliados);
    }
}
