using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public class TracadoDoMundoTestes
{
    [Fact]
    public void Estrada_acomoda_o_eixo_e_funde_a_borda_sem_alterar_o_exterior()
    {
        var tracado = new TracadoDoMundo(
            new[] { new TracadoDoMundo.Ponto(-50f, 0f), new TracadoDoMundo.Ponto(50f, 0f) },
            8f,
            8f,
            0f
        );
        const float alturaOriginal = 20f;
        float AlturaBase(float _, float __) => 10f;

        Assert.Equal(10f, tracado.AplicarEstrada(0f, 0f, alturaOriginal, AlturaBase));
        Assert.InRange(tracado.AplicarEstrada(0f, 8f, alturaOriginal, AlturaBase), 10f, 20f);
        Assert.Equal(20f, tracado.AplicarEstrada(0f, 12f, alturaOriginal, AlturaBase));
    }

    [Fact]
    public void Altura_oficial_da_grade_aplica_o_mesmo_tracado_salvo()
    {
        const float x = -200f;
        const float z = 150f;
        var baseY = MapaDoMundo.AlturaBaseDaAutoriaEm(x, z);
        var esperado = EstradaDoPasso.Tracado.AplicarEstrada(
            x,
            z,
            baseY,
            MapaDoMundo.AlturaBaseDaAutoriaEm
        );

        Assert.Equal(esperado, MapaDoMundo.AlturaEm(x, z), 0.001f);
        Assert.Equal(
            MapaDoMundo.AlturaBaseDaAutoriaEm(1000f, 0f),
            MapaDoMundo.AlturaEm(1000f, 0f),
            0.001f
        );
    }
}
