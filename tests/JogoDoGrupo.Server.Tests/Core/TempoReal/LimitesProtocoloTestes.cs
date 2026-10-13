using System.Collections.Concurrent;
using System.Text;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.TempoReal;
using Microsoft.Extensions.Configuration;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class LimitesProtocoloTestes
{
    [Theory]
    [InlineData("TamanhoMaximoPacote", "1")]
    [InlineData("ProfundidadeMaximaJson", "0")]
    [InlineData("MaximoRecusasPorSessao", "0")]
    [InlineData("MaximoExcecoesPorSessao", "-1")]
    [InlineData("MaximoTentativasAutenticacao", "0")]
    [InlineData("MaximoConexoesPendentesPorIp", "0")]
    public void Configuracao_invalida_nao_sobe(string nome, string valor)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["TempoReal:" + nome] = valor }
            )
            .Build();
        Assert.Throws<InvalidOperationException>(() => ConfiguracaoTempoReal.Ler(config));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Limite_de_falhas_encerra_e_avisa_uma_vez(bool excecao)
    {
        using var f = new TempoRealFixture(
            new() { ["MaximoRecusasPorSessao"] = "2", ["MaximoExcecoesPorSessao"] = "2" }
        );
        var (cliente, conta, _) = await f.EntrarAsync();
        using var c = cliente;
        var sessao = f.SessaoDaConta(conta)!;
        var encerradas = new ConcurrentQueue<SessaoTempoReal>();
        f.TempoReal.SessaoEncerrada += encerradas.Enqueue;
        for (var i = 0; i < 2; i++)
        {
            cliente.Enviar(
                excecao ? TempoRealFixture.IdErro : TempoRealFixture.IdSemManipulador,
                new MensagemTeste()
            );
            cliente.Enviar(TempoRealFixture.IdEco, new MensagemTeste());
            await cliente.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco);
        }
        Assert.False(cliente.Desconectado.IsCompleted);
        cliente.Enviar(
            excecao ? TempoRealFixture.IdErro : TempoRealFixture.IdSemManipulador,
            new MensagemTeste()
        );
        await cliente.Desconectado.WaitAsync(ClienteTempoRealDeTeste.Espera);
        await TempoRealFixture.EsperarAsync(() => encerradas.Contains(sessao), "SessaoEncerrada");
        Assert.Single(encerradas, s => s == sessao);
        Assert.Null(f.SessaoDaConta(conta));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Consulta_suspensa_nao_evade_tentativas_nem_prazo(bool expirar)
    {
        using var f = new TempoRealFixture(new() { ["MaximoTentativasAutenticacao"] = "2" });
        var (token, conta, personagem) = await f.NovaContaAsync();
        var iniciou = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var consulta = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        var consultas = 0;
        f.Verificador.Consulta = () =>
        {
            Interlocked.Increment(ref consultas);
            iniciou.TrySetResult();
            return consulta.Task;
        };
        using var c = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
        var pedido = new AutenticarTempoReal { Token = token, PersonagemId = personagem };
        c.Enviar(TempoRealIds.AutenticarTempoReal, pedido);
        await iniciou.Task.WaitAsync(ClienteTempoRealDeTeste.Espera);
        if (!expirar)
        {
            c.Enviar(TempoRealIds.AutenticarTempoReal, pedido);
            c.Enviar(TempoRealIds.AutenticarTempoReal, pedido);
        }
        else
        {
            f.AvancarRelogio(TimeSpan.FromMilliseconds(TempoRealFixture.TempoParaAutenticarMs + 1));
        }

        await c.Desconectado.WaitAsync(ClienteTempoRealDeTeste.Espera);
        consulta.SetResult(true);
        Assert.Equal(1, consultas);
        // Uma nova autenticação funciona como barreira do laço após o resultado tardio.
        f.Verificador.Consulta = null;
        var (novo, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagem
        );
        using var n = novo;
        Assert.True(resultado.Aceita);
        Assert.Single(f.TempoReal.Sessoes, s => s.ContaId == conta);
        Assert.DoesNotContain(TempoRealIds.ResultadoAutenticacaoTempoReal, c.IdsRecebidos);
    }

    [Fact]
    public async Task Autenticar_libera_vaga_pendente_do_ip()
    {
        using var f = new TempoRealFixture(new() { ["MaximoConexoesPendentesPorIp"] = "1" });
        var (a, _, _) = await f.EntrarAsync();
        using var autenticado = a;
        using var pendente = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClienteTempoRealDeTeste.ConectarAsync(f.Porta)
        );
    }

    [Fact]
    public async Task Tentativa_N_mais_um_desconecta()
    {
        using var f = new TempoRealFixture(new() { ["MaximoTentativasAutenticacao"] = "2" });
        using var c = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
        for (var i = 0; i < 2; i++)
        {
            c.Enviar(
                TempoRealIds.AutenticarTempoReal,
                new AutenticarTempoReal { Token = "invalido" }
            );
            Assert.False(
                (
                    await c.ReceberAsync<ResultadoAutenticacaoTempoReal>(
                        TempoRealIds.ResultadoAutenticacaoTempoReal
                    )
                ).Aceita
            );
        }
        c.Enviar(TempoRealIds.AutenticarTempoReal, new AutenticarTempoReal());
        await c.Desconectado.WaitAsync(ClienteTempoRealDeTeste.Espera);
    }

    [Fact]
    public async Task Pendentes_por_ip_recusa_excedente_e_libera_vaga_ao_sair()
    {
        using var f = new TempoRealFixture(new() { ["MaximoConexoesPendentesPorIp"] = "1" });
        using var a = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClienteTempoRealDeTeste.ConectarAsync(f.Porta)
        );
        a.Sair();
        // O evento de recusa serve como barreira do la�o ap�s a sa�da confi�vel.
        await TempoRealFixture.EsperarAsync(() => a.Desconectado.IsCompleted, "sa�da do cliente");
        using var b = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
    }

    [Fact]
    public async Task Tamanho_e_profundidade_sao_recusados_antes_do_manipulador()
    {
        using var f = new TempoRealFixture(
            new() { ["TamanhoMaximoPacote"] = "2048", ["ProfundidadeMaximaJson"] = "2" }
        );
        var (cliente, conta, _) = await f.EntrarAsync();
        using var c = cliente;
        var sessao = f.SessaoDaConta(conta)!;
        var exato = ProtocoloTempoReal.Codificar(TempoRealFixture.IdEco, new MensagemTeste());
        var fronteira = new byte[2048];
        Array.Fill(fronteira, (byte)' ');
        exato.CopyTo(fronteira, 0);
        c.EnviarBruto(fronteira);
        await c.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco);
        var profundidadeExata = Encoding.UTF8.GetBytes("{\"Ignorado\":{}}");
        var valido = new byte[profundidadeExata.Length + 2];
        valido[0] = (byte)(TempoRealFixture.IdEco & 255);
        valido[1] = (byte)(TempoRealFixture.IdEco >> 8);
        profundidadeExata.CopyTo(valido, 2);
        c.EnviarBruto(valido);
        await c.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco);
        var grande = new byte[2049];
        Array.Fill(grande, (byte)' ');
        exato.CopyTo(grande, 0);
        c.EnviarBruto(grande);
        var profundo = Encoding.UTF8.GetBytes("{\"Ignorado\":{\"a\":{}}}");
        var pacote = new byte[profundo.Length + 2];
        pacote[0] = (byte)(TempoRealFixture.IdEco & 255);
        pacote[1] = (byte)(TempoRealFixture.IdEco >> 8);
        profundo.CopyTo(pacote, 2);
        c.EnviarBruto(pacote);
        c.Enviar(TempoRealFixture.IdEco, new MensagemTeste { Texto = "aceito" });
        Assert.Equal("aceito", (await c.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco)).Texto);
        Assert.Equal(2, f.Recusadas.Count(r => r.Sessao == sessao));
        Assert.Equal(3, f.Recebidas.Count(r => r.Sessao == sessao));
    }
}
