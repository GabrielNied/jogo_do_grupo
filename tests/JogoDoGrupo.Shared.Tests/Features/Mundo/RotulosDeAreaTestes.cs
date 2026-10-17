using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Mundo;

public sealed class RotulosDeAreaTestes
{
    [Fact]
    public void Descricao_distingue_cidade_campo_e_dungeon()
    {
        Assert.Equal(RotulosDeArea.Cidade, RotulosDeArea.Descricao(RotulosDeArea.IdCidade, 0f, 0f));
        Assert.Equal(
            RotulosDeArea.Campo,
            RotulosDeArea.Descricao(RotulosDeArea.IdCidade, 500f, 0f)
        );
        Assert.Equal(RotulosDeArea.Dungeon, RotulosDeArea.Descricao("instancia:mina", 0f, 0f));
    }
}
