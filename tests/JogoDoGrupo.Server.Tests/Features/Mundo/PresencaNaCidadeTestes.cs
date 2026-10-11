using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

/// <summary>Integração: client UDP de verdade contra o servidor com o MundoModule.</summary>
public class PresencaNaCidadeTestes(TempoRealFixture f) : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task Dois_jogadores_entram_e_se_veem()
    {
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estadoA = await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        Assert.Equal(idA, estadoA.Eu.PersonagemId);
        Assert.Equal(ValidadorDeMovimento.Nascimento.Z, estadoA.Eu.Z);
        Assert.DoesNotContain(estadoA.Outros, o => o.PersonagemId == idA);

        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estadoB = await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Contains(estadoB.Outros, o => o.PersonagemId == idA);
        var entrou = await a.ReceberAsync<JogadorEntrou>(MundoMessageIds.JogadorEntrou);
        Assert.Equal(idB, entrou.Jogador.PersonagemId);
    }

    [Fact]
    public async Task Movimento_valido_e_replicado_e_alem_da_velocidade_e_recusado()
    {
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, _) = await f.EntrarAsync();
        using var _b = b;
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        b.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await b.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        var z = ValidadorDeMovimento.Nascimento.Z;
        a.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 0.3f,
                Z = z,
                Sequencia = 7,
            }
        );
        var propria = await a.ReceberAsync<PosicaoJogador>(MundoMessageIds.PosicaoJogador);
        Assert.Equal(7, propria.Sequencia);
        Assert.Equal(0.3f, propria.X);
        var replica = await b.ReceberAsync<PosicaoJogador>(MundoMessageIds.PosicaoJogador);
        Assert.Equal(idA, replica.PersonagemId);
        Assert.Equal(0.3f, replica.X);
        Assert.Equal(0, replica.Sequencia);

        a.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = 40f,
                Z = z,
                Sequencia = 8,
            }
        );
        var corrigida = await a.ReceberAsync<PosicaoJogador>(MundoMessageIds.PosicaoJogador);
        Assert.Equal(8, corrigida.Sequencia);
        Assert.Equal(0.3f, corrigida.X);
    }

    [Fact]
    public async Task Sair_ou_cair_remove_o_avatar_e_avisa_os_outros()
    {
        var (a, _, idA) = await f.EntrarAsync();
        using var _a = a;
        var (b, _, idB) = await f.EntrarAsync();
        using var _b = b;
        var (c, _, idC) = await f.EntrarAsync();
        using var _c = c;
        foreach (var cliente in new[] { a, b, c })
        {
            cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
            await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        }

        b.Sair();
        var saiu = await a.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);
        Assert.Equal(idB, saiu.PersonagemId);

        c.Cair();
        var caiu = await a.ReceberAsync<JogadorSaiu>(MundoMessageIds.JogadorSaiu);
        Assert.Equal(idC, caiu.PersonagemId);
        Assert.NotEqual(idA, caiu.PersonagemId);
    }

    [Fact]
    public async Task Movimento_sem_entrar_na_cidade_e_ignorado()
    {
        var (a, _, _) = await f.EntrarAsync();
        using var _a = a;

        a.Enviar(MundoMessageIds.IntencaoDeMovimento, new IntencaoDeMovimento { Sequencia = 1 });
        a.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());

        var estado = await a.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);
        Assert.Equal(ValidadorDeMovimento.Nascimento.Z, estado.Eu.Z);
    }
}
