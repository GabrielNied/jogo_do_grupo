using System;
using JogoDoGrupo.Shared.Features.Npcs;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Npcs;

public class RegistroDeInteracaoTestes : IDisposable
{
    public RegistroDeInteracaoTestes()
    {
        RegistroDeInteracao.Limpar();
    }

    public void Dispose()
    {
        RegistroDeInteracao.Limpar();
    }

    private sealed class InteragivelFalso : IInteragivelNoMundo
    {
        public float PosicaoX { get; set; }
        public float PosicaoZ { get; set; }
        public float RaioInteracao { get; set; } = 3f;
        public bool Disponivel { get; set; } = true;
        public int InteracoesRealizadas { get; private set; }

        public bool PodeInteragir() => Disponivel;

        public void Interagir()
        {
            InteracoesRealizadas++;
        }
    }

    [Fact]
    public void ObterMaisProximo_QuandoVazio_RetornaNull()
    {
        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);
        Assert.Null(resultado);
    }

    [Fact]
    public void ObterMaisProximo_QuandoApenasUmNoAlcance_RetornaEle()
    {
        var npc = new InteragivelFalso
        {
            PosicaoX = 1f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
        };
        RegistroDeInteracao.Registrar(npc);

        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);

        Assert.Same(npc, resultado);
        Assert.True(RegistroDeInteracao.EhOMaisProximo(npc, 0f, 0f));
    }

    [Fact]
    public void ObterMaisProximo_QuandoForaDeAlcance_RetornaNull()
    {
        var bau = new InteragivelFalso
        {
            PosicaoX = 5f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
        };
        RegistroDeInteracao.Registrar(bau);

        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);

        Assert.Null(resultado);
        Assert.False(RegistroDeInteracao.EhOMaisProximo(bau, 0f, 0f));
    }

    [Fact]
    public void ObterMaisProximo_EntreNpcEBauNoAlcance_RetornaOMaisProximo()
    {
        // Jogador em (0, 0). Baú a 1 m, NPC a 2 m.
        var bau = new InteragivelFalso
        {
            PosicaoX = 1f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
        };
        var npc = new InteragivelFalso
        {
            PosicaoX = 2f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
        };

        RegistroDeInteracao.Registrar(bau);
        RegistroDeInteracao.Registrar(npc);

        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);

        Assert.Same(bau, resultado);
        Assert.True(RegistroDeInteracao.EhOMaisProximo(bau, 0f, 0f));
        Assert.False(RegistroDeInteracao.EhOMaisProximo(npc, 0f, 0f));

        // Se o jogador andar para (2, 0), o NPC passa a ser o mais próximo.
        var resultadoEm2 = RegistroDeInteracao.ObterMaisProximo(2f, 0f);
        Assert.Same(npc, resultadoEm2);
        Assert.True(RegistroDeInteracao.EhOMaisProximo(npc, 2f, 0f));
        Assert.False(RegistroDeInteracao.EhOMaisProximo(bau, 2f, 0f));
    }

    [Fact]
    public void ObterMaisProximo_QuandoMaisProximoNaoPodeInteragir_RetornaOProximoValido()
    {
        var bauRecarregando = new InteragivelFalso
        {
            PosicaoX = 1f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
            Disponivel = false,
        };
        var npc = new InteragivelFalso
        {
            PosicaoX = 2f,
            PosicaoZ = 0f,
            RaioInteracao = 3f,
            Disponivel = true,
        };

        RegistroDeInteracao.Registrar(bauRecarregando);
        RegistroDeInteracao.Registrar(npc);

        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);

        Assert.Same(npc, resultado);
        Assert.True(RegistroDeInteracao.EhOMaisProximo(npc, 0f, 0f));
        Assert.False(RegistroDeInteracao.EhOMaisProximo(bauRecarregando, 0f, 0f));
    }

    [Fact]
    public void Desregistrar_RemoveDoCalculo()
    {
        var npc = new InteragivelFalso { PosicaoX = 1f, PosicaoZ = 0f };
        RegistroDeInteracao.Registrar(npc);
        RegistroDeInteracao.Desregistrar(npc);

        var resultado = RegistroDeInteracao.ObterMaisProximo(0f, 0f);
        Assert.Null(resultado);
    }
}
