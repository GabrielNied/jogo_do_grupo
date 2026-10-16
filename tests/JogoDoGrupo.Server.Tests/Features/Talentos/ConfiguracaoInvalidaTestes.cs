using JogoDoGrupo.Server.Features.Talentos;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Server.Tests.Features.Talentos;

public class ConfiguracaoInvalidaTestes
{
    [Fact]
    public void Configuracao_valida_passa()
    {
        var arvore = ArvoreDoJogo.Criar();

        Assert.Same(arvore, TalentosModule.ValidarOuFalhar(arvore));
    }

    [Fact]
    public void Configuracao_invalida_nao_sobe_e_diz_o_motivo()
    {
        var arvore = ArvoreDoJogo.Criar();
        arvore.Nos[1].Id = arvore.Nos[0].Id;
        arvore.Nos[2].Requisitos = new[] { new RequisitoDeNo { NoId = "fantasma" } };

        var erro = Assert.Throws<InvalidOperationException>(() =>
            TalentosModule.ValidarOuFalhar(arvore)
        );

        Assert.Contains("repetido", erro.Message);
        Assert.Contains("fantasma", erro.Message);
    }
}
