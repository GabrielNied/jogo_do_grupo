using System;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class RegrasProjetilTestes
{
    [Fact]
    public void Velocidades_e_alcance_da_flecha_estao_de_acordo_com_o_balanceamento()
    {
        Assert.Equal(35.0f, RegrasProjetil.Velocidade(TipoDeProjetil.Flecha, TipoDeAtaque.Leve));
        Assert.Equal(50.0f, RegrasProjetil.Velocidade(TipoDeProjetil.Flecha, TipoDeAtaque.Pesado));

        Assert.Equal(35.0f, RegrasProjetil.AlcanceMaximo(TipoDeProjetil.Flecha, TipoDeAtaque.Leve));
        Assert.Equal(
            40.0f,
            RegrasProjetil.AlcanceMaximo(TipoDeProjetil.Flecha, TipoDeAtaque.Pesado)
        );

        Assert.Equal(0.25f, RegrasProjetil.Raio(TipoDeProjetil.Flecha));
        Assert.True(
            RegrasProjetil.TempoDeVidaMaximoMs(TipoDeProjetil.Flecha, TipoDeAtaque.Leve) <= 1000
        );
        Assert.True(
            RegrasProjetil.TempoDeVidaMaximoMs(TipoDeProjetil.Flecha, TipoDeAtaque.Pesado) <= 1000
        );
    }

    [Fact]
    public void DirecaoVetorial_converte_graus_corretamente()
    {
        // 0 graus = +Z
        RegrasProjetil.DirecaoVetorial(0f, out var dirX0, out var dirY0, out var dirZ0);
        Assert.Equal(0f, dirX0, 0.001f);
        Assert.Equal(0f, dirY0, 0.001f);
        Assert.Equal(1f, dirZ0, 0.001f);

        // 90 graus = +X
        RegrasProjetil.DirecaoVetorial(90f, out var dirX90, out var dirY90, out var dirZ90);
        Assert.Equal(1f, dirX90, 0.001f);
        Assert.Equal(0f, dirY90, 0.001f);
        Assert.Equal(0f, dirZ90, 0.001f);

        // 180 graus = -Z
        RegrasProjetil.DirecaoVetorial(180f, out var dirX180, out var dirY180, out var dirZ180);
        Assert.Equal(0f, dirX180, 0.001f);
        Assert.Equal(0f, dirY180, 0.001f);
        Assert.Equal(-1f, dirZ180, 0.001f);
    }

    [Fact]
    public void AvancarPosicao_desloca_de_acordo_com_velocidade_e_tempo()
    {
        RegrasProjetil.AvancarPosicao(
            0f,
            1f,
            0f,
            0f,
            0f,
            1f, // apontando para +Z
            35f,
            0.1f, // 35 m/s por 100 ms = 3.5 m
            out var novoX,
            out var novoY,
            out var novoZ
        );

        Assert.Equal(0f, novoX, 0.001f);
        Assert.Equal(1f, novoY, 0.001f);
        Assert.Equal(3.5f, novoZ, 0.001f);
    }

    [Fact]
    public void SegmentoColideComCirculo_detecta_acerto_direto()
    {
        // Disparo vai de (0, 0) a (0, 10). Alvo em (0, 5) com raio 0.5.
        bool colidiu = RegrasProjetil.SegmentoColideComCirculo(0, 0, 0, 10, 0, 5, 0.5);
        Assert.True(colidiu);
    }

    [Fact]
    public void SegmentoColideComCirculo_detecta_raspao_na_borda()
    {
        // Disparo vai de (0, 0) a (0, 10) pelo eixo Z.
        // Alvo em (0.8, 5). Raio alvo 0.5 + raio projetil 0.35 = 0.85 total (colide).
        bool colidiu = RegrasProjetil.SegmentoColideComCirculo(0, 0, 0, 10, 0.8, 5, 0.85);
        Assert.True(colidiu);

        // Alvo mais longe em X (1.5, 5) não colide.
        bool errou = RegrasProjetil.SegmentoColideComCirculo(0, 0, 0, 10, 1.5, 5, 0.85);
        Assert.False(errou);
    }

    [Fact]
    public void SegmentoColideComCirculo_nao_detecta_alvo_atras_da_origem()
    {
        // Disparo vai de (0, 2) a (0, 10). Alvo em (0, 0) com raio 0.5 (ficou para trás).
        bool colidiu = RegrasProjetil.SegmentoColideComCirculo(0, 2, 0, 10, 0, 0, 0.5);
        Assert.False(colidiu);
    }

    [Fact]
    public void ColideComAlvo_considera_faixa_de_altura()
    {
        // Alvo no chão (Y=0, altura=2.0). Projétil a 1.2m de altura.
        bool acertou = RegrasProjetil.ColideComAlvo(0, 1.2, 0, 0, 1.2, 5, 0.25, 0, 0, 3, 0.5, 2.0);
        Assert.True(acertou);

        // Projétil passando muito alto (Y=10m) não acerta o alvo no chão.
        bool altoDemais = RegrasProjetil.ColideComAlvo(
            0,
            10.0,
            0,
            0,
            10.0,
            5,
            0.25,
            0,
            0,
            3,
            0.5,
            2.0
        );
        Assert.False(altoDemais);
    }
}
