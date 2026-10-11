using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Extensions.Configuration;

namespace JogoDoGrupo.Server.Tests.Core.TempoReal;

public class MensagensTempoRealTestes
{
    // ── Registro de faixas ───────────────────────────────────────────────────

    [Fact]
    public void Faixa_de_funcionalidade_e_registrada()
    {
        var faixa = new MensagensTempoReal().Faixa(MundoMessageIds.Faixa);

        Assert.Equal(MundoMessageIds.Faixa.Inicio, faixa.Faixa.Inicio);
        Assert.Equal(MundoMessageIds.Faixa.Fim, faixa.Faixa.Fim);
    }

    [Theory]
    [InlineData(0, 999)]
    [InlineData(500, 1500)]
    [InlineData(999, 999)]
    public void Faixa_que_invade_o_core_e_recusada(int inicio, int fim) =>
        Assert.Throws<InvalidOperationException>(() =>
            new MensagensTempoReal().Faixa(new MessageIdRange((ushort)inicio, (ushort)fim))
        );

    [Theory]
    [InlineData(1000, 1999)]
    [InlineData(1999, 2500)]
    [InlineData(500 + 1000, 1600)]
    public void Faixa_que_sobrepoe_outra_e_recusada(int inicio, int fim)
    {
        var mensagens = new MensagensTempoReal();
        mensagens.Faixa(MundoMessageIds.Faixa);

        Assert.Throws<InvalidOperationException>(() =>
            mensagens.Faixa(new MessageIdRange((ushort)inicio, (ushort)fim))
        );
    }

    [Fact]
    public void Faixas_vizinhas_convivem()
    {
        var mensagens = new MensagensTempoReal();
        mensagens.Faixa(new MessageIdRange(1000, 1999));
        mensagens.Faixa(new MessageIdRange(2000, 2999));
    }

    [Fact]
    public void Faixa_invertida_e_recusada() =>
        Assert.Throws<ArgumentException>(() =>
            new MensagensTempoReal().Faixa(new MessageIdRange(2000, 1000))
        );

    [Theory]
    [InlineData(999)]
    [InlineData(2000)]
    public void Manipulador_fora_da_faixa_e_recusado(int id) =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MensagensTempoReal()
                .Faixa(MundoMessageIds.Faixa)
                .Ao<EntrarNaCidade>((ushort)id, (_, _) => { })
        );

    [Fact]
    public void Manipulador_repetido_e_recusado()
    {
        var faixa = new MensagensTempoReal().Faixa(MundoMessageIds.Faixa);
        faixa.Ao<EntrarNaCidade>(MundoMessageIds.EntrarNaCidade, (_, _) => { });

        Assert.Throws<InvalidOperationException>(() =>
            faixa.Ao<EntrarNaCidade>(MundoMessageIds.EntrarNaCidade, (_, _) => { })
        );
    }

    [Fact]
    public void Registros_simultaneos_de_faixas_diferentes_nao_se_perdem()
    {
        var mensagens = new MensagensTempoReal();
        var faixas = Enumerable
            .Range(1, 50)
            .Select(i => new MessageIdRange((ushort)(i * 1000), (ushort)(i * 1000 + 999)))
            .ToArray();

        Parallel.ForEach(
            faixas,
            faixa => mensagens.Faixa(faixa).Ao<EntrarNaCidade>(faixa.Inicio, (_, _) => { })
        );

        foreach (var faixa in faixas)
        {
            Assert.Throws<InvalidOperationException>(() => mensagens.Faixa(faixa));
        }
    }

    // ── Protocolo ────────────────────────────────────────────────────────────

    [Fact]
    public void Pacote_tem_id_little_endian_e_corpo_json()
    {
        var pacote = ProtocoloTempoReal.Codificar(
            0x1234,
            new AutenticarTempoReal { Token = "abc", PersonagemId = 9 }
        );

        Assert.Equal(0x34, pacote[0]);
        Assert.Equal(0x12, pacote[1]);
        Assert.Equal(
            "{\"Token\":\"abc\",\"PersonagemId\":9}",
            System.Text.Encoding.UTF8.GetString(pacote, 2, pacote.Length - 2)
        );
        Assert.True(ProtocoloTempoReal.TentarLerId(pacote, out var id));
        Assert.Equal(0x1234, id);
        var lido = ProtocoloTempoReal.LerCorpo<AutenticarTempoReal>(pacote)!;
        Assert.Equal("abc", lido.Token);
        Assert.Equal(9, lido.PersonagemId);
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 1 })]
    public void Pacote_sem_cabecalho_nao_tem_id(byte[] pacote) =>
        Assert.False(ProtocoloTempoReal.TentarLerId(pacote, out _));

    [Theory]
    [InlineData("")]
    [InlineData("{quebrado")]
    [InlineData("null")]
    public void Corpo_invalido_vira_nulo(string corpo)
    {
        var pacote = new byte[] { 1, 0 }.Concat(System.Text.Encoding.UTF8.GetBytes(corpo));

        Assert.Null(ProtocoloTempoReal.LerCorpo<AutenticarTempoReal>(pacote.ToArray()));
    }

    // ── Configuração ─────────────────────────────────────────────────────────

    [Fact]
    public void Configuracao_vazia_usa_os_padroes()
    {
        var tempoReal = ConfiguracaoTempoReal.Ler(new ConfigurationBuilder().Build());

        Assert.Equal(5081, tempoReal.Porta);
        Assert.Equal("0.0.0.0", tempoReal.Endereco);
    }

    [Theory]
    [InlineData("Porta", "-1")]
    [InlineData("Porta", "65536")]
    [InlineData("Endereco", "nao-e-ip")]
    [InlineData("Endereco", "::1")]
    [InlineData("ChaveConexao", "")]
    [InlineData("TempoParaAutenticarMs", "0")]
    [InlineData("TempoQuedaMs", "-5")]
    public void Configuracao_invalida_impede_o_servidor_de_subir(string campo, string valor)
    {
        var configuracao = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { [$"TempoReal:{campo}"] = valor }
            )
            .Build();

        Assert.Throws<InvalidOperationException>(() => ConfiguracaoTempoReal.Ler(configuracao));
    }
}
