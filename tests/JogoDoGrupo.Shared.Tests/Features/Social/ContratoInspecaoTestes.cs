using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public sealed class ContratoInspecaoTestes
{
    private static readonly ushort[] _ids =
    [
        SocialMessageIds.PedirInspecao,
        SocialMessageIds.RetratoInspecaoResponse,
        SocialMessageIds.InspecaoRecusada,
    ];

    [Fact]
    public void Ids_de_inspecao_estao_na_faixa_de_social()
    {
        Assert.All(
            _ids,
            id => Assert.True(SocialMessageIds.Faixa.Contem(id), $"ID {id} fora da faixa de Social")
        );
    }

    [Fact]
    public void Ids_de_inspecao_sao_distintos()
    {
        Assert.Equal(_ids.Length, _ids.Distinct().Count());
    }

    [Fact]
    public void Ids_de_inspecao_tem_os_valores_esperados()
    {
        Assert.Equal(13300, SocialMessageIds.PedirInspecao);
        Assert.Equal(13310, SocialMessageIds.RetratoInspecaoResponse);
        Assert.Equal(13311, SocialMessageIds.InspecaoRecusada);
    }

    [Fact]
    public void RetratoInspecaoResponse_instancia_com_valores_padrao()
    {
        var retrato = new RetratoInspecaoResponse
        {
            PersonagemId = 42,
            Nome = "Aventureiro",
            Raca = 1,
            Nivel = 10,
            ConjuntoAtivo = 1,
            Forca = 20,
            Vigor = 15,
            Agilidade = 12,
            Intelecto = 10,
            Espirito = 8,
            Equipados =
            [
                new EntradaInventario
                {
                    InstanciaId = 101,
                    Posicao = PosicaoEquipamento.Peito,
                    Nome = "Cota de Malha",
                    Equipamento = new Equipamento
                    {
                        ModeloId = 5,
                        Nome = "Cota de Malha",
                        Tipo = TipoEquipamento.Peito,
                        Raridade = RaridadeItem.Raro,
                        TierOrigem = TierItem.A,
                        Nivel = 10,
                    },
                },
            ],
            Atributos = new AtributosDosEquipamentos { DefesaFisica = 50, VidaMaxima = 200 },
        };

        Assert.Equal(42, retrato.PersonagemId);
        Assert.Equal("Aventureiro", retrato.Nome);
        Assert.Equal(10, retrato.Nivel);
        Assert.Single(retrato.Equipados);
        Assert.Equal(TierItem.A, retrato.Equipados[0].Equipamento!.TierOrigem);
        Assert.Equal(50, retrato.Atributos.DefesaFisica);
    }

    [Fact]
    public void InspecaoRecusada_possui_codigos_de_erro_claros()
    {
        var recusa = new InspecaoRecusada
        {
            PersonagemId = 99,
            Motivo = CodigoErroInspecao.AlvoForaDoAlcance,
        };

        Assert.Equal(99, recusa.PersonagemId);
        Assert.Equal(CodigoErroInspecao.AlvoForaDoAlcance, recusa.Motivo);
    }
}
