using System.Collections.Concurrent;
using System.Text;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using LiteNetLib;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class ServidorTempoRealTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    // ── Autenticação ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Conecta_e_autentica_com_o_token_do_login()
    {
        var autenticadas = new ConcurrentQueue<SessaoTempoReal>();
        void Ao(SessaoTempoReal s) => autenticadas.Enqueue(s);
        f.TempoReal.SessaoAutenticada += Ao;
        try
        {
            var (token, contaId, personagemId) = await f.NovaContaAsync();

            var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
                f.Porta,
                token,
                personagemId
            );
            using var _ = cliente;

            Assert.True(resultado.Aceita);
            Assert.Equal("", resultado.Codigo);
            var sessao = f.SessaoDaConta(contaId);
            Assert.NotNull(sessao);
            Assert.True(sessao.Autenticada);
            Assert.Equal(personagemId, sessao.PersonagemId);
            await TempoRealFixture.EsperarAsync(
                () => autenticadas.Contains(sessao),
                "evento SessaoAutenticada"
            );
        }
        finally
        {
            f.TempoReal.SessaoAutenticada -= Ao;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("nao-e-um-jwt")]
    public async Task Token_ausente_ou_malformado_nao_entra(string token) =>
        await AssertRecusado(token, 1, CodigosErro.NaoAutenticado);

    [Fact]
    public async Task Token_expirado_nao_entra() =>
        await AssertRecusado(f.Token(DateTime.UtcNow.AddHours(-1)), 1, CodigosErro.NaoAutenticado);

    [Fact]
    public async Task Token_assinado_com_outra_chave_nao_entra() =>
        await AssertRecusado(
            f.Token(DateTime.UtcNow.AddHours(1), "outra-chave-com-pelo-menos-32-bytes-de-tamanho"),
            1,
            CodigosErro.NaoAutenticado
        );

    [Fact]
    public async Task Personagem_de_outra_conta_nao_entra()
    {
        var (token, _, _) = await f.NovaContaAsync();
        var (_, _, personagemDeOutro) = await f.NovaContaAsync();

        await AssertRecusado(token, personagemDeOutro, CodigosErro.AcessoNegado);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Personagem_invalido_nao_entra(long personagemId)
    {
        var (token, _, _) = await f.NovaContaAsync();

        await AssertRecusado(token, personagemId, CodigosErro.AcessoNegado);
    }

    [Fact]
    public async Task Corpo_de_autenticacao_invalido_e_recusado()
    {
        using var cliente = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);

        cliente.EnviarBruto(Pacote(TempoRealIds.AutenticarTempoReal, "{nao e json"));

        var resultado = await cliente.ReceberAsync<ResultadoAutenticacaoTempoReal>(
            TempoRealIds.ResultadoAutenticacaoTempoReal
        );
        Assert.False(resultado.Aceita);
        Assert.Equal(CodigosErro.NaoAutenticado, resultado.Codigo);
    }

    [Fact]
    public async Task Recusado_pode_tentar_de_novo_antes_do_prazo()
    {
        var (token, contaId, personagemId) = await f.NovaContaAsync();
        using var cliente = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);

        cliente.Enviar(TempoRealIds.AutenticarTempoReal, new AutenticarTempoReal { Token = "x" });
        var primeira = await cliente.ReceberAsync<ResultadoAutenticacaoTempoReal>(
            TempoRealIds.ResultadoAutenticacaoTempoReal
        );
        cliente.Enviar(
            TempoRealIds.AutenticarTempoReal,
            new AutenticarTempoReal { Token = token, PersonagemId = personagemId }
        );
        var segunda = await cliente.ReceberAsync<ResultadoAutenticacaoTempoReal>(
            TempoRealIds.ResultadoAutenticacaoTempoReal
        );

        Assert.False(primeira.Aceita);
        Assert.True(segunda.Aceita);
        Assert.NotNull(f.SessaoDaConta(contaId));
    }

    [Fact]
    public async Task Conexao_que_nao_autentica_e_desconectada_no_prazo()
    {
        using var cliente = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);

        var info = await cliente.Desconectado.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(DisconnectReason.RemoteConnectionClose, info.Reason);
    }

    [Fact]
    public async Task Chave_de_conexao_errada_e_recusada()
    {
        var erro = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ClienteTempoRealDeTeste.ConectarAsync(f.Porta, "outra-chave")
        );

        Assert.Contains(nameof(DisconnectReason.ConnectionRejected), erro.Message);
    }

    [Fact]
    public async Task Mesma_conta_autenticando_de_novo_substitui_a_sessao_anterior()
    {
        var (token, contaId, personagemId) = await f.NovaContaAsync();
        var (primeiro, _) = await ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagemId
        );
        using var _1 = primeiro;
        var anterior = f.SessaoDaConta(contaId)!;

        var (segundo, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagemId
        );
        using var _2 = segundo;

        Assert.True(resultado.Aceita);
        await primeiro.Desconectado.WaitAsync(ClienteTempoRealDeTeste.Espera);
        var atual = Assert.Single(f.TempoReal.Sessoes, s => s.ContaId == contaId);
        Assert.NotSame(anterior, atual);
    }

    [Fact]
    public async Task Varias_contas_autenticando_ao_mesmo_tempo_entram_todas()
    {
        var entradas = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => f.EntrarAsync()));
        try
        {
            foreach (var (_, contaId, personagemId) in entradas)
            {
                Assert.Equal(personagemId, f.SessaoDaConta(contaId)?.PersonagemId);
            }
        }
        finally
        {
            foreach (var (cliente, _, _) in entradas)
            {
                cliente.Dispose();
            }
        }
    }

    // ── Saída e queda ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Sair_remove_a_sessao_e_avisa()
    {
        var encerradas = new ConcurrentQueue<SessaoTempoReal>();
        void Ao(SessaoTempoReal s) => encerradas.Enqueue(s);
        f.TempoReal.SessaoEncerrada += Ao;
        try
        {
            var (cliente, contaId, personagemId) = await f.EntrarAsync();
            using var _ = cliente;
            var sessao = f.SessaoDaConta(contaId)!;

            cliente.Sair();

            await TempoRealFixture.EsperarAsync(
                () => f.SessaoDaConta(contaId) == null,
                "sessão removida"
            );
            await TempoRealFixture.EsperarAsync(
                () => encerradas.Contains(sessao),
                "evento SessaoEncerrada"
            );
            Assert.Equal(personagemId, sessao.PersonagemId);
        }
        finally
        {
            f.TempoReal.SessaoEncerrada -= Ao;
        }
    }

    [Fact]
    public async Task Queda_da_conexao_remove_a_sessao()
    {
        var (cliente, contaId, _) = await f.EntrarAsync();
        using var _ = cliente;

        cliente.Cair();

        await TempoRealFixture.EsperarAsync(
            () => f.SessaoDaConta(contaId) == null,
            "sessão removida após a queda"
        );
    }

    [Fact]
    public async Task Desconectar_pelo_servidor_remove_a_sessao_e_fecha_o_client()
    {
        var (cliente, contaId, _) = await f.EntrarAsync();
        using var _ = cliente;

        f.TempoReal.Desconectar(f.SessaoDaConta(contaId)!);

        await cliente.Desconectado.WaitAsync(ClienteTempoRealDeTeste.Espera);
        // O client pode saber da desconexão antes de o servidor terminar de remover a sessão.
        await TempoRealFixture.EsperarAsync(
            () => f.SessaoDaConta(contaId) == null,
            "sessão removida após desconectar pelo servidor"
        );
    }

    // ── Despacho por faixa ────────────────────────────────────────────────────

    [Fact]
    public async Task Mensagem_na_faixa_registrada_chega_ao_manipulador_com_a_sessao()
    {
        var (cliente, contaId, _) = await f.EntrarAsync();
        using var _ = cliente;
        var texto = Guid.NewGuid().ToString();

        cliente.Enviar(TempoRealFixture.IdEco, new MensagemTeste { Texto = texto, Numero = 7 });
        var eco = await cliente.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco);

        Assert.Equal(texto, eco.Texto);
        Assert.Equal(7, eco.Numero);
        var (sessao, mensagem) = Assert.Single(f.Recebidas, r => r.Mensagem.Texto == texto);
        Assert.Equal(contaId, sessao.ContaId);
    }

    [Theory]
    [InlineData(999)] // faixa do Core, sem uso
    [InlineData(2000)] // faixa de outra funcionalidade, ainda não registrada
    [InlineData(59999)]
    [InlineData(60100)]
    [InlineData(ushort.MaxValue)]
    public async Task Id_fora_de_faixa_registrada_e_recusado(ushort id)
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _ = cliente;
        var motivo = id == 999 ? MotivoRecusa.SemManipulador : MotivoRecusa.ForaDaFaixa;

        await AssertRecusadaSemDerrubar(cliente, Pacote(id, "{}"), id, motivo);
    }

    [Fact]
    public async Task Id_na_faixa_sem_manipulador_e_recusado()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _ = cliente;

        await AssertRecusadaSemDerrubar(
            cliente,
            Pacote(TempoRealFixture.IdSemManipulador, "{}"),
            TempoRealFixture.IdSemManipulador,
            MotivoRecusa.SemManipulador
        );
    }

    [Fact]
    public async Task Corpo_invalido_e_recusado()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _ = cliente;

        await AssertRecusadaSemDerrubar(
            cliente,
            Pacote(TempoRealFixture.IdEco, "{quebrado"),
            TempoRealFixture.IdEco,
            MotivoRecusa.CorpoInvalido
        );
    }

    [Fact]
    public async Task Pacote_sem_cabecalho_e_recusado()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _ = cliente;

        await AssertRecusadaSemDerrubar(cliente, [42], 0, MotivoRecusa.PacoteInvalido);
    }

    [Fact]
    public async Task Autenticar_de_novo_na_mesma_conexao_e_recusado()
    {
        var (cliente, _, _) = await f.EntrarAsync();
        using var _ = cliente;

        await AssertRecusadaSemDerrubar(
            cliente,
            Pacote(TempoRealIds.AutenticarTempoReal, "{}"),
            TempoRealIds.AutenticarTempoReal,
            MotivoRecusa.SemManipulador
        );
    }

    [Fact]
    public async Task Mensagem_antes_de_autenticar_nao_chega_ao_manipulador()
    {
        using var cliente = await ClienteTempoRealDeTeste.ConectarAsync(f.Porta);
        var texto = Guid.NewGuid().ToString();

        cliente.Enviar(TempoRealFixture.IdEco, new MensagemTeste { Texto = texto });

        await TempoRealFixture.EsperarAsync(
            () =>
                f.Recusadas.Any(r =>
                    r.Id == TempoRealFixture.IdEco
                    && r.Motivo == MotivoRecusa.NaoAutenticada
                    && !r.Sessao.Autenticada
                ),
            "recusa NaoAutenticada"
        );
        Assert.DoesNotContain(f.Recebidas, r => r.Mensagem.Texto == texto);
    }

    // ── Envio ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Enviar_para_todos_alcanca_as_sessoes_autenticadas_menos_a_excluida()
    {
        var (a, contaA, _) = await f.EntrarAsync();
        var (b, _, _) = await f.EntrarAsync();
        var (c, _, _) = await f.EntrarAsync();
        using var _a = a;
        using var _b = b;
        using var _c = c;
        var texto = Guid.NewGuid().ToString();
        var marcador = Guid.NewGuid().ToString();

        f.TempoReal.EnviarParaTodos(
            TempoRealFixture.IdEco,
            new MensagemTeste { Texto = texto },
            exceto: f.SessaoDaConta(contaA)
        );
        // Mensagem só para A, enviada depois: se A recebesse o broadcast, ele viria antes.
        f.TempoReal.Enviar(
            f.SessaoDaConta(contaA)!,
            TempoRealFixture.IdEco,
            new MensagemTeste { Texto = marcador }
        );

        Assert.Equal(texto, (await b.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco)).Texto);
        Assert.Equal(texto, (await c.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco)).Texto);
        Assert.Equal(marcador, (await a.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco)).Texto);
    }

    [Fact]
    public async Task Enviar_para_sessao_encerrada_devolve_falso()
    {
        var (cliente, contaId, _) = await f.EntrarAsync();
        var sessao = f.SessaoDaConta(contaId)!;
        cliente.Sair();
        cliente.Dispose();
        await TempoRealFixture.EsperarAsync(() => f.SessaoDaConta(contaId) == null, "saída");

        Assert.False(f.TempoReal.Enviar(sessao, TempoRealFixture.IdEco, new MensagemTeste()));
    }

    // ── Ajudantes ─────────────────────────────────────────────────────────────

    private async Task AssertRecusado(string token, long personagemId, string codigo)
    {
        var (cliente, resultado) = await ClienteTempoRealDeTeste.AutenticarAsync(
            f.Porta,
            token,
            personagemId
        );
        using var _ = cliente;

        Assert.False(resultado.Aceita);
        Assert.Equal(codigo, resultado.Codigo);
        Assert.DoesNotContain(
            f.TempoReal.Sessoes,
            s => s.PersonagemId == personagemId && personagemId > 0
        );
    }

    /// <summary>
    /// Manda o pacote inválido e depois um eco válido: o eco volta (a conexão continua) e o
    /// inválido foi recusado com o motivo esperado, sem chegar ao manipulador.
    /// </summary>
    private async Task AssertRecusadaSemDerrubar(
        ClienteTempoRealDeTeste cliente,
        byte[] pacote,
        ushort id,
        MotivoRecusa motivo
    )
    {
        var antes = f.Recebidas.Count;
        var texto = Guid.NewGuid().ToString();

        cliente.EnviarBruto(pacote);
        cliente.Enviar(TempoRealFixture.IdEco, new MensagemTeste { Texto = texto });
        await cliente.ReceberAsync<MensagemTeste>(TempoRealFixture.IdEco);

        Assert.Contains(f.Recusadas, r => r.Id == id && r.Motivo == motivo && r.Sessao.Autenticada);
        Assert.Single(f.Recebidas, r => r.Mensagem.Texto == texto);
        Assert.False(cliente.Desconectado.IsCompleted);
    }

    private static byte[] Pacote(ushort id, string corpo)
    {
        var json = Encoding.UTF8.GetBytes(corpo);
        var pacote = new byte[2 + json.Length];
        pacote[0] = (byte)(id & 0xFF);
        pacote[1] = (byte)(id >> 8);
        json.CopyTo(pacote, 2);
        return pacote;
    }
}
