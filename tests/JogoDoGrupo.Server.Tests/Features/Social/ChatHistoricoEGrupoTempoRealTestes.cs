using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Integração do histórico da Cidade e do canal Grupo: clients UDP de verdade, com personagens
/// no banco e o grupo de verdade (convite e aceite).
/// </summary>
public class ChatHistoricoEGrupoTempoRealTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private async Task<(ClienteTempoRealDeTeste Cliente, long Id, string Nome)> ConectarAsync()
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("H");
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = Raca.Humano }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;

        var porta = servidor.Services.GetRequiredService<ServidorTempoReal>().Porta;
        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(porta, token, id);
        Assert.True(resultado.Aceita, resultado.Codigo);
        return (cliente, id, nome);
    }

    private static async Task EntrarNaCidadeAsync(ClienteTempoRealDeTeste cliente)
    {
        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
    }

    private static void Falar(ClienteTempoRealDeTeste cliente, CanalDeChat canal, string texto) =>
        cliente.Enviar(
            SocialMessageIds.EnviarMensagemDeChat,
            new EnviarMensagemDeChat { Canal = canal, Texto = texto }
        );

    private static Task<MensagemDeChat> ReceberMensagemAsync(ClienteTempoRealDeTeste cliente) =>
        cliente.ReceberAsync<MensagemDeChat>(SocialMessageIds.MensagemDeChat);

    [Fact]
    public async Task Quem_entra_na_cidade_recebe_o_que_foi_dito_ha_pouco()
    {
        var (a, idA, nomeA) = await ConectarAsync();
        using var _a = a;
        await EntrarNaCidadeAsync(a);
        var texto = $"historico {ServidorDeTeste.NomeUnico()}";
        Falar(a, CanalDeChat.Cidade, texto);
        await ReceberMensagemAsync(a);

        var (b, _, _) = await ConectarAsync();
        using var _b = b;
        await EntrarNaCidadeAsync(b);

        var historico = await b.ReceberAsync<HistoricoDaCidade>(SocialMessageIds.HistoricoDaCidade);
        var mensagem = Assert.Single(historico.Mensagens, m => m.Texto == texto);
        Assert.Equal(CanalDeChat.Cidade, mensagem.Canal);
        Assert.Equal(idA, mensagem.RemetenteId);
        Assert.Equal(nomeA, mensagem.NomeDoRemetente);
        Assert.True(historico.Mensagens.Length <= RegrasChat.HistoricoDaCidadeMensagens);
    }

    [Fact]
    public async Task Mensagem_no_grupo_chega_aos_dois_membros_mesmo_fora_da_cidade()
    {
        var (a, idA, nomeA) = await ConectarAsync();
        using var _a = a;
        var (b, idB, nomeB) = await ConectarAsync();
        using var _b = b;

        // Sem grupo, o canal não existe para ela.
        Falar(a, CanalDeChat.Grupo, "tem alguém aí?");
        var recusa = await a.ReceberAsync<MensagemDeChatRecusada>(
            SocialMessageIds.MensagemDeChatRecusada
        );
        Assert.Equal(CodigosErroSocial.NaoEstaEmGrupo, recusa.Codigo);

        a.Enviar(SocialMessageIds.ConvidarParaGrupo, new ConvidarParaGrupo { Nome = nomeB });
        var convite = await b.ReceberAsync<ConviteRecebido>(SocialMessageIds.ConviteRecebido);
        b.Enviar(
            SocialMessageIds.ResponderConvite,
            new ResponderConvite { ConviteId = convite.ConviteId, Aceitar = true }
        );
        // A lista vazia de quando ela conectou pode vir antes: espera a que já tem o B.
        ListaDoGrupo lista;
        do
        {
            lista = await a.ReceberAsync<ListaDoGrupo>(SocialMessageIds.ListaDoGrupo);
        } while (lista.Membros.All(m => m.PersonagemId != idB));

        Falar(a, CanalDeChat.Grupo, "vamos para a dungeon");

        foreach (var cliente in new[] { a, b })
        {
            var mensagem = await ReceberMensagemAsync(cliente);
            Assert.Equal(CanalDeChat.Grupo, mensagem.Canal);
            Assert.Equal(idA, mensagem.RemetenteId);
            Assert.Equal(nomeA, mensagem.NomeDoRemetente);
            Assert.Equal("vamos para a dungeon", mensagem.Texto);
        }
    }
}
