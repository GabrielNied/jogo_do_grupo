using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>
/// Integração: clients UDP de verdade, com personagens de verdade no banco (o chat busca os
/// nomes no diretório de personagens), contra o servidor com o SocialModule.
/// </summary>
public class ChatTempoRealTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private async Task<(ClienteTempoRealDeTeste Cliente, long Id, string Nome)> EntrarAsync(
        bool naCidade
    )
    {
        var http = await servidor.CriarClienteLogadoAsync();
        var nome = ServidorDeTeste.NomeUnico("C");
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

        if (naCidade)
        {
            cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
            await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        }

        return (cliente, id, nome);
    }

    private static void Falar(
        ClienteTempoRealDeTeste cliente,
        CanalDeChat canal,
        string texto,
        string nomeDoAlvo = ""
    ) =>
        cliente.Enviar(
            SocialMessageIds.EnviarMensagemDeChat,
            new EnviarMensagemDeChat
            {
                Canal = canal,
                Texto = texto,
                NomeDoAlvo = nomeDoAlvo,
            }
        );

    private static Task<MensagemDeChat> ReceberMensagemAsync(ClienteTempoRealDeTeste cliente) =>
        cliente.ReceberAsync<MensagemDeChat>(SocialMessageIds.MensagemDeChat);

    [Fact]
    public async Task Mensagem_na_cidade_chega_ao_remetente_e_a_quem_esta_na_cidade()
    {
        var (a, idA, nomeA) = await EntrarAsync(naCidade: true);
        using var _a = a;
        var (b, _, _) = await EntrarAsync(naCidade: true);
        using var _b = b;

        Falar(a, CanalDeChat.Cidade, "  alguém para a dungeon?  ");

        foreach (var cliente in new[] { a, b })
        {
            var mensagem = await ReceberMensagemAsync(cliente);
            Assert.Equal(CanalDeChat.Cidade, mensagem.Canal);
            Assert.Equal(idA, mensagem.RemetenteId);
            Assert.Equal(nomeA, mensagem.NomeDoRemetente);
            Assert.Equal("alguém para a dungeon?", mensagem.Texto);
            Assert.True(mensagem.HoraUnixMs > 0);
        }
    }

    [Fact]
    public async Task Sussurro_chega_ao_alvo_fora_da_cidade_com_o_nome_em_outra_caixa()
    {
        var (a, idA, nomeA) = await EntrarAsync(naCidade: true);
        using var _a = a;
        var (b, _, nomeB) = await EntrarAsync(naCidade: false);
        using var _b = b;

        Falar(a, CanalDeChat.Sussurro, "oi, tudo bem?", nomeB.ToUpperInvariant());

        foreach (var cliente in new[] { a, b })
        {
            var mensagem = await ReceberMensagemAsync(cliente);
            Assert.Equal(CanalDeChat.Sussurro, mensagem.Canal);
            Assert.Equal(idA, mensagem.RemetenteId);
            Assert.Equal(nomeA, mensagem.NomeDoRemetente);
            Assert.Equal(nomeB, mensagem.NomeDoAlvo);
            Assert.Equal("oi, tudo bem?", mensagem.Texto);
        }
    }

    [Fact]
    public async Task Recusa_volta_so_para_quem_enviou_e_a_proxima_mensagem_segue_normal()
    {
        var (a, _, _) = await EntrarAsync(naCidade: false);
        using var _a = a;
        var (b, _, nomeB) = await EntrarAsync(naCidade: false);
        using var _b = b;

        // Fora da cidade, o canal Cidade não existe.
        Falar(a, CanalDeChat.Cidade, "oi");
        var recusa = await a.ReceberAsync<MensagemDeChatRecusada>(
            SocialMessageIds.MensagemDeChatRecusada
        );
        Assert.Equal(CodigosErroSocial.CanalIndisponivel, recusa.Codigo);

        // A primeira mensagem de chat que B recebe é o sussurro: a recusada não chegou a ele.
        Falar(a, CanalDeChat.Sussurro, "agora sim", nomeB);
        Assert.Equal("agora sim", (await ReceberMensagemAsync(b)).Texto);
    }
}
