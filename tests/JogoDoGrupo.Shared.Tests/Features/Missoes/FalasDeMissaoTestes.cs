using System.Linq;
using JogoDoGrupo.Shared.Features.Missoes;
using Xunit;

namespace JogoDoGrupo.Shared.Tests.Features.Missoes
{
    public class FalasDeMissaoTestes
    {
        [Fact]
        public void TodaMissaoPrincipal_TemFalaDeAceitarEDeEntregar()
        {
            var principais = CatalogoDeMissoes
                .Todas.Where(m => m.Tipo == TipoDeMissao.Principal)
                .ToList();
            Assert.NotEmpty(principais);
            foreach (var def in principais)
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(def.TextoAoAceitar),
                    $"missao {def.Id} sem TextoAoAceitar"
                );
                Assert.False(
                    string.IsNullOrWhiteSpace(def.TextoAoEntregar),
                    $"missao {def.Id} sem TextoAoEntregar"
                );
            }
        }
    }
}
