using System.Text.Json;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>O poder no exemplar: sorteado na geração, fixo, fora do tier (#629).</summary>
public class PoderesDoExemplarTestes
{
    private static readonly GeradorEquipamentos Gerador = new(new ConfigEquipamentos());

    private static Equipamento Gerar(int modelo, int semente) =>
        Gerador.Gerar(modelo, new Random(semente))!;

    [Theory]
    [InlineData(111, CatalogoDePoderes.PoderPeito)]
    [InlineData(112, CatalogoDePoderes.PoderLuvas)]
    [InlineData(114, CatalogoDePoderes.PoderBotas)]
    [InlineData(116, CatalogoDePoderes.PoderAnel)]
    public void Modelo_que_declara_categorias_recebe_o_poder_do_seu_encaixe(
        int modelo,
        string esperado
    )
    {
        for (var semente = 0; semente < 10; semente++)
            Assert.Equal(esperado, Gerar(modelo, semente).PoderId);
    }

    [Fact]
    public void Modelo_sem_categorias_ou_sem_poder_no_catalogo_nao_recebe_poder()
    {
        // Variante A nunca declara; capacete B declara, mas o catálogo ainda não tem poder dele.
        foreach (
            var modelo in Enumerable
                .Range(100, 73)
                .Where(m => m != 111 && m != 112 && m != 114 && m != 116)
        )
        {
            // Os modelos B de peito, luvas, botas e anel de outras famílias também recebem.
            var dados = CatalogoEquipamentos.Obter(modelo)!;
            var esperado = CatalogoDePoderes.Candidatos(dados).Count > 0;
            Assert.Equal(esperado, Gerar(modelo, 1).PoderId != "");
        }

        Assert.Equal("", Gerar(101, 1).PoderId);
        Assert.Equal("", Gerar(110, 1).PoderId);
    }

    [Fact]
    public void Poder_nao_muda_o_tier_nem_os_valores_sorteados()
    {
        var item = Gerar(111, 5);
        var copia = JsonSerializer.Deserialize<Equipamento>(
            JsonSerializer.Serialize(item, Json),
            Json
        )!;
        copia.PoderId = "";

        Assert.NotEqual("", item.PoderId);
        Assert.Equal(RegrasTier.TierDe(copia), RegrasTier.TierDe(item));
        Assert.Equal(copia.TierOrigem, item.TierOrigem);
    }

    [Fact]
    public void Exemplar_antigo_sem_o_campo_le_sem_poder_e_o_poder_sobrevive_a_gravar_e_ler()
    {
        var item = Gerar(111, 3);
        var json = JsonSerializer.Serialize(item, Json);
        var legado = json.Replace("\"PoderId\":\"" + item.PoderId + "\",", "");
        Assert.DoesNotContain("PoderId", legado);

        Assert.Equal("", JsonSerializer.Deserialize<Equipamento>(legado, Json)!.PoderId);
        Assert.Equal(item.PoderId, JsonSerializer.Deserialize<Equipamento>(json, Json)!.PoderId);
    }

    private static readonly JsonSerializerOptions Json = new() { IncludeFields = true };
}
