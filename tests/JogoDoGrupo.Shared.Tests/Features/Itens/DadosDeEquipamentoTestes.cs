using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

/// <summary>
/// #585: o JsonUtility do client troca "Equipamento": null por um objeto vazio (tipo Capacete),
/// e o client mandava Capacete para qualquer peça sem dados guardados (400 PosicaoInvalida).
/// </summary>
public class DadosDeEquipamentoTestes
{
    [Fact]
    public void ObjetoVazioDoJsonUtilityNaoContaComoEquipamento()
    {
        var entrada = new EntradaInventario { Equipamento = new Equipamento() };
        Assert.Null(RegrasEquipamentos.DadosDe(entrada));
        Assert.Null(RegrasEquipamentos.DadosDe(new EntradaInventario { Equipamento = null! }));
        Assert.Null(RegrasEquipamentos.DadosDe(null!));
    }

    [Fact]
    public void ModeloRealContaComoEquipamento()
    {
        var dados = new Equipamento { ModeloId = 105, Tipo = TipoEquipamento.Botas };
        Assert.Same(
            dados,
            RegrasEquipamentos.DadosDe(new EntradaInventario { Equipamento = dados })
        );
    }

    [Fact]
    public void CatalogoDeTodoModeloDaUmEncaixeQueOServidorAceita()
    {
        var ultimo = CatalogoEquipamentos.PrimeiroModelo + CatalogoEquipamentos.QuantidadeModelos;
        for (var id = 100; id < ultimo; id++)
        {
            var dados = CatalogoEquipamentos.Obter(id);
            if (dados == null)
                continue;
            foreach (var conjunto in new[] { 1, 2 })
            {
                var encaixe = RegrasEquipamentos.EncaixeAutomatico(dados.Tipo, [], conjunto);
                Assert.True(RegrasEquipamentos.Aceita(dados.Tipo, encaixe), $"{id} {dados.Tipo}");
            }
        }
    }

    [Fact]
    public void TodoTipoTemEncaixeAutomaticoAceito()
    {
        foreach (var tipo in Enum.GetValues<TipoEquipamento>())
        {
            var encaixe = RegrasEquipamentos.EncaixeAutomatico(tipo, [], 1);
            Assert.True(RegrasEquipamentos.Aceita(tipo, encaixe), tipo.ToString());
        }
    }
}
