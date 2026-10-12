using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public class MovimentoNaCavernaTestes
{
    private const float PassoDoTeste = 3f;

    private static PosicaoNaCidade NoPercurso(float distancia)
    {
        PrimeiraCaverna.PontoDoPercurso(distancia, out var x, out var z);
        return new PosicaoNaCidade(x, PrimeiraCaverna.PisoNoPlano(x, z)!.Value, z, 0f);
    }

    [Fact]
    public void Andar_da_boca_a_camara_final_e_aceito_passo_a_passo()
    {
        var atual = NoPercurso(0f);
        var comprimento = PrimeiraCaverna.ComprimentoDoPercurso;
        for (var d = PassoDoTeste; d < comprimento + PassoDoTeste; d += PassoDoTeste)
        {
            var desejada = NoPercurso(d);

            Assert.True(ValidadorDeMovimento.Permitido(atual, desejada, 1000), $"recusado a {d} m");
            Assert.Equal(
                JulgamentoDoPasso.Normal,
                ValidadorDeMovimento.Julgar(
                    atual,
                    desejada,
                    ValidadorDeMovimento.TetoDoOrcamento(ValidadorDeMovimento.VelocidadeMaxima)
                )
            );
            atual = desejada;
        }
    }

    [Fact]
    public void A_ponta_do_beco_e_aceita()
    {
        var ponta = new PosicaoNaCidade(
            PrimeiraCaverna.PontaDoBecoX,
            PrimeiraCaverna
                .PisoNoPlano(PrimeiraCaverna.PontaDoBecoX, PrimeiraCaverna.PontaDoBecoZ)!
                .Value,
            PrimeiraCaverna.PontaDoBecoZ,
            0f
        );

        Assert.True(ValidadorDeMovimento.Permitido(ponta, ponta, 0));
    }

    [Fact]
    public void Atravessar_a_parede_para_a_rocha_e_recusado()
    {
        var no = NoPercurso(40f);
        var naRocha = no with { X = no.X + 12f };

        Assert.Null(PrimeiraCaverna.PisoEm(naRocha.X, naRocha.Z, naRocha.Y));
        Assert.False(ValidadorDeMovimento.Permitido(no, naRocha, 1000));
    }

    [Fact]
    public void Subir_pelo_teto_para_dentro_da_rocha_e_recusado()
    {
        var no = NoPercurso(40f);
        var teto = PrimeiraCaverna.TetoEm(no.X, no.Z)!.Value;
        var naRocha = no with { Y = teto + 2f };

        // O ponto está abaixo do terreno (o morro cobre o túnel), então só o teto o separa.
        Assert.True(naRocha.Y < MapaDoMundo.AlturaEm(naRocha.X, naRocha.Z) - 3f);
        Assert.False(ValidadorDeMovimento.Permitido(no, naRocha, 1000));
    }

    [Fact]
    public void Afundar_sob_o_piso_e_recusado()
    {
        var no = NoPercurso(40f);

        Assert.False(ValidadorDeMovimento.Permitido(no, no with { Y = no.Y - 6f }, 1000));
    }

    [Fact]
    public void Em_cima_do_morro_vale_a_regra_do_terreno()
    {
        var no = NoPercurso(40f);
        var chao = MapaDoMundo.AlturaEm(no.X, no.Z);
        var emCima = no with { Y = chao };

        Assert.True(ValidadorDeMovimento.Permitido(emCima, emCima, 0));
    }
}
