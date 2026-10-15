using JogoDoGrupo.Server.Features.Pets;
using JogoDoGrupo.Shared.Core.TempoReal;

namespace JogoDoGrupo.Server.Tests.Features.Pets;

/// <summary>
/// O <c>PetMudou</c> é sequenciado: no canal 0 compartilhado, o pacote de um pet descartava o de
/// outro; no canal do dono, descartava a <c>PosicaoJogador</c> dele (#852).
/// </summary>
public class CanalDoPetTestes
{
    private static long Canal(long entidadeId) => entidadeId % TransporteTempoReal.Canais;

    [Theory]
    [InlineData(1L)]
    [InlineData(5L)]
    [InlineData(16L)]
    [InlineData(31L)]
    [InlineData(1_000_000_007L)]
    public void Pet_nunca_cai_no_canal_do_dono(long donoId)
    {
        Assert.NotEqual(Canal(donoId), Canal(ServicoDePets.EntidadeDoPet(donoId)));
    }

    [Fact]
    public void Pets_de_donos_diferentes_vao_em_canais_diferentes()
    {
        Assert.NotEqual(
            Canal(ServicoDePets.EntidadeDoPet(5)),
            Canal(ServicoDePets.EntidadeDoPet(6))
        );
        Assert.NotEqual(0, Canal(ServicoDePets.EntidadeDoPet(32)));
    }
}
