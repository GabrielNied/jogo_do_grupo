using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public sealed class ServidorComMultiplosCanais : IDisposable
{
    public ServidorDeTeste Real { get; } = new();
    public WebApplicationFactory<Program> Falso { get; }
    public int Porta { get; }

    public ServidorComMultiplosCanais()
    {
        Falso = Real.WithWebHostBuilder(b =>
        {
            b.UseSetting("Canais:Quantidade", "3");
            b.UseSetting("Canais:Capacidade", "80");
        });
        Porta = Falso.Services.GetRequiredService<ServidorTempoReal>().Porta;
    }

    public void Dispose()
    {
        Falso.Dispose();
        Real.Dispose();
    }
}

public class CanaisDoMundoTestes(ServidorComMultiplosCanais servidor)
    : IClassFixture<ServidorComMultiplosCanais>
{
    private async Task<(ClienteTempoRealDeTeste Cliente, long Id, string Nome)> NovoJogadorAsync()
    {
        var http = await ServidorDeTeste.CriarClienteLogadoEmAsync(servidor.Falso);
        var nome = ServidorDeTeste.NomeUnico("C");
        var resposta = await ServidorDeTeste.PostJsonAsync(
            http,
            RotasPersonagem.Personagens,
            new CriarPersonagemRequest { Nome = nome, Raca = Raca.Valdren }
        );
        resposta.EnsureSuccessStatusCode();
        var id = (await ServidorDeTeste.LerJsonAsync<PersonagemResumo>(resposta)).Id;

        var token = http.DefaultRequestHeaders.Authorization!.Parameter!;
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            servidor.Porta,
            token,
            id
        );
        Assert.True(resultado.Aceita, resultado.Codigo);

        return (cliente, id, nome);
    }

    [Fact]
    public async Task Jogador_recebe_CanalAlterado_ao_entrar_na_cidade()
    {
        var (cliente, _, _) = await NovoJogadorAsync();
        using var _ = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());

        var canalAlterado = await cliente.ReceberAsync<CanalAlterado>(
            MundoMessageIds.CanalAlterado
        );
        Assert.NotNull(canalAlterado);
        Assert.InRange(canalAlterado.Canal, 1, 3);
        Assert.Equal(3, canalAlterado.TotalCanais);
    }

    [Fact]
    public async Task Troca_de_canal_rejeitada_para_mesmo_canal_ou_canal_invalido()
    {
        var (cliente, _, _) = await NovoJogadorAsync();
        using var _ = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var inicial = await cliente.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        // Troca para o mesmo canal
        cliente.Enviar(
            MundoMessageIds.PedirTrocaDeCanal,
            new PedirTrocaDeCanal { CanalDestino = inicial.Canal }
        );
        var recusaMesmo = await cliente.ReceberAsync<RecusaTrocaDeCanal>(
            MundoMessageIds.RecusaTrocaDeCanal
        );
        Assert.Equal(CodigosErroMundo.MesmoCanal, recusaMesmo.Motivo);

        // Troca para canal inválido (ex.: 99)
        cliente.Enviar(
            MundoMessageIds.PedirTrocaDeCanal,
            new PedirTrocaDeCanal { CanalDestino = 99 }
        );
        var recusaInvalido = await cliente.ReceberAsync<RecusaTrocaDeCanal>(
            MundoMessageIds.RecusaTrocaDeCanal
        );
        Assert.Equal(CodigosErroMundo.CanalInvalido, recusaInvalido.Motivo);
    }

    [Fact]
    public async Task Consultar_canais_devolve_lista_e_canal_atual()
    {
        var (cliente, _, _) = await NovoJogadorAsync();
        using var _ = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var inicial = await cliente.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        cliente.Enviar(MundoMessageIds.ConsultarCanais, new ConsultarCanais());
        var resposta = await cliente.ReceberAsync<RespostaCanais>(MundoMessageIds.RespostaCanais);

        Assert.Equal(inicial.Canal, resposta.CanalAtual);
        Assert.NotNull(resposta.Canais);
        Assert.Equal(3, resposta.Canais.Length);
        Assert.Contains(resposta.Canais, c => c.Canal == inicial.Canal && c.Jogadores >= 1);
    }

    [Fact]
    public async Task Troca_de_canal_move_jogador_e_atualiza_visibilidade()
    {
        var (clienteA, idA, _) = await NovoJogadorAsync();
        using var _a = clienteA;
        var (clienteB, idB, _) = await NovoJogadorAsync();
        using var _b = clienteB;

        clienteA.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteA.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        var canalA = await clienteA.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        clienteB.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        var canalB = await clienteB.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        // Se ambos caíram no mesmo canal, movemos B para outro canal primeiro
        var canalDiferente = canalA.Canal == 1 ? 2 : 1;
        if (canalB.Canal == canalA.Canal)
        {
            clienteB.Enviar(
                MundoMessageIds.PedirTrocaDeCanal,
                new PedirTrocaDeCanal { CanalDestino = canalDiferente }
            );
            await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
            await clienteB.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);
        }

        // Agora B pede troca para o canal de A
        clienteB.Enviar(
            MundoMessageIds.PedirTrocaDeCanal,
            new PedirTrocaDeCanal { CanalDestino = canalA.Canal }
        );

        var estadoB = await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        var mudou = await clienteB.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);
        Assert.Equal(canalA.Canal, mudou.Canal);
        Assert.Contains(estadoB.Outros, o => o.PersonagemId == idA);

        // Cliente A deve receber JogadorEntrou referente a B
        var entrou = await clienteA.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);
        Assert.Equal(idB, entrou.Jogador.PersonagemId);
    }

    [Fact]
    public async Task Chat_da_cidade_isola_mensagens_entre_canais()
    {
        var (clienteA, idA, nomeA) = await NovoJogadorAsync();
        using var _a = clienteA;
        var (clienteB, _, _) = await NovoJogadorAsync();
        using var _b = clienteB;

        clienteA.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteA.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        var canalA = await clienteA.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        clienteB.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        var canalB = await clienteB.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);

        // Garante que B está num canal diferente de A
        var canalDiferente = canalA.Canal == 1 ? 2 : 1;
        if (canalB.Canal == canalA.Canal)
        {
            clienteB.Enviar(
                MundoMessageIds.PedirTrocaDeCanal,
                new PedirTrocaDeCanal { CanalDestino = canalDiferente }
            );
            await clienteB.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
            await clienteB.ReceberAsync<CanalAlterado>(MundoMessageIds.CanalAlterado);
        }

        // A fala no chat de cidade
        clienteA.Enviar(
            SocialMessageIds.EnviarMensagemDeChat,
            new EnviarMensagemDeChat
            {
                Canal = CanalDeChat.Cidade,
                Texto = "Mensagem no canal de A",
            }
        );

        // A recebe a própria mensagem
        var msgA = await clienteA.ReceberAsync<MensagemDeChat>(SocialMessageIds.MensagemDeChat);
        Assert.Equal(idA, msgA.RemetenteId);
        Assert.Equal("Mensagem no canal de A", msgA.Texto);

        // B não deve receber a mensagem pois está em outro canal da cidade
        // B envia algo para si mesmo no sussurro para confirmar que a conexão está viva
        clienteB.Enviar(
            SocialMessageIds.EnviarMensagemDeChat,
            new EnviarMensagemDeChat
            {
                Canal = CanalDeChat.Sussurro,
                NomeDoAlvo = nomeA,
                Texto = "Sussurro direto",
            }
        );

        // B recebe seu sussurro enviado sem nunca ter recebido a mensagem da cidade de A
        var msgB = await clienteB.ReceberAsync<MensagemDeChat>(SocialMessageIds.MensagemDeChat);
        Assert.Equal(CanalDeChat.Sussurro, msgB.Canal);
        Assert.Equal("Sussurro direto", msgB.Texto);
    }
}
