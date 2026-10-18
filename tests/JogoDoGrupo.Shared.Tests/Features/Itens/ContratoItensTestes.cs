using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Shared.Tests.Features.Itens;

public class ContratoItensTestes
{
    [Fact]
    public void Catalogo_tem_os_ids_combinados()
    {
        Assert.Equal(21, CatalogoItens.Todos.Length);
        Assert.Equal(CatalogoItens.Todos.Length, CatalogoItens.Todos.Distinct().Count());
        Assert.Equal(1, CatalogoItens.EspadaDeTeste);
        Assert.Equal(10, CatalogoItens.RedeasDoCorcelNoturno);
        Assert.Equal(11, CatalogoItens.SinoDoCervoJovem);
        Assert.Equal(26, CatalogoItens.RedeasDaMulaDeCarga);
    }

    [Theory]
    [InlineData(1, "Espada de teste")]
    [InlineData(2, "Escudo de teste")]
    [InlineData(3, "Poção de teste")]
    [InlineData(4, "Moeda de teste")]
    [InlineData(10, "Rédeas do Corcel Noturno")]
    [InlineData(11, "Sino do Cervo Jovem")]
    [InlineData(21, "Rédeas do Lobo de Presa")]
    [InlineData(26, "Rédeas da Mula de Carga")]
    public void ObterNome_devolve_o_nome_exibido(int itemId, string esperado) =>
        Assert.Equal(esperado, CatalogoItens.ObterNome(itemId));

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(999)]
    [InlineData(-1)]
    public void ObterNome_devolve_null_fora_do_catalogo(int itemId) =>
        Assert.Null(CatalogoItens.ObterNome(itemId));

    [Fact]
    public void Limites_sao_os_do_recorte_de_teste()
    {
        Assert.Equal(40, RegrasItens.EspacosInventario);
        Assert.Equal(1, RegrasItens.MinimoItensPorBau);
        Assert.Equal(3, RegrasItens.MaximoItensPorBau);
    }

    [Fact]
    public void Rotas_sao_os_textos_combinados()
    {
        Assert.Equal("/api/personagens/{personagemId}/inventario", RotasItens.Inventario);
        Assert.Equal("/api/personagens/{personagemId}/bau", RotasItens.Bau);
        Assert.Equal("/api/personagens/42/inventario", RotasItens.InventarioDe(42));
        Assert.Equal("/api/personagens/42/bau", RotasItens.BauDe(42));
        Assert.Equal("/api/personagens/42/inventario/ordenar", RotasItens.OrdenarDe(42));
        Assert.Equal(
            "/api/personagens/42/inventario/descartar-lote",
            RotasItens.DescartarLoteDe(42)
        );
        Assert.Equal("/api/personagens/42/inventario/lixo", RotasItens.LixoDe(42));
        Assert.Equal("/api/personagens/42/inventario/favorito", RotasItens.FavoritoDe(42));
    }

    [Fact]
    public void Codigos_de_erro_sao_os_textos_combinados()
    {
        Assert.Equal("InventarioCheio", CodigosErroItens.InventarioCheio);
        Assert.Equal("PersonagemNaoEncontrado", CodigosErroItens.PersonagemNaoEncontrado);
        Assert.Equal("ItemNaoEncontrado", CodigosErroItens.ItemNaoEncontrado);
        Assert.Equal("ItemEquipado", CodigosErroItens.ItemEquipado);
        Assert.Equal("ItemFavorito", CodigosErroItens.ItemFavorito);
    }

    [Fact]
    public void Codigos_de_equipar_sao_os_textos_combinados()
    {
        Assert.Equal("NivelInsuficiente", CodigosErroItens.NivelInsuficiente);
        Assert.Equal("ArmaDeDuasMaos", CodigosErroItens.ArmaDeDuasMaos);
        Assert.Equal("ItemNaoEquipavel", CodigosErroItens.ItemNaoEquipavel);
        Assert.Equal("PosicaoInvalida", CodigosErroItens.PosicaoInvalida);
        Assert.Equal("ConjuntoInvalido", CodigosErroItens.ConjuntoInvalido);
    }

    [Fact]
    public void Regra_de_equipar_recusa_por_posicao_nivel_e_duas_maos_nessa_ordem()
    {
        var martelo = CatalogoEquipamentos.Obter(100)!;
        var escudo = CatalogoEquipamentos.Obter(2)!;
        Assert.True(martelo.DuasMaos);
        martelo.NivelPersonagemMinimo = 10;
        escudo.NivelPersonagemMinimo = 1;
        var principal = CatalogoEquipamentos.Obter(100)!;

        Assert.Null(RegrasEquipar.Recusa(martelo, PosicaoEquipamento.Arma1, 10, null));
        Assert.Equal(
            CodigosErroItens.PosicaoInvalida,
            RegrasEquipar.Recusa(martelo, PosicaoEquipamento.Capacete, 1, null)
        );
        Assert.Equal(
            CodigosErroItens.NivelInsuficiente,
            RegrasEquipar.Recusa(martelo, PosicaoEquipamento.Arma1, 9, null)
        );
        Assert.Null(RegrasEquipar.Recusa(martelo, PosicaoEquipamento.Arma1, 0, null));
        Assert.Equal(
            CodigosErroItens.ArmaDeDuasMaos,
            RegrasEquipar.Recusa(escudo, PosicaoEquipamento.Secundaria1, 5, principal)
        );
        Assert.Null(RegrasEquipar.Recusa(escudo, PosicaoEquipamento.Secundaria2, 5, null));
        Assert.Equal(
            CodigosErroItens.ItemNaoEquipavel,
            RegrasEquipar.Recusa(null!, PosicaoEquipamento.Arma1, 5, null)
        );
    }

    [Fact]
    public void Codigos_de_erro_nao_repetem_os_do_core()
    {
        Assert.NotEqual(CodigosErro.NaoAutenticado, CodigosErroItens.InventarioCheio);
        Assert.NotEqual(CodigosErro.NaoAutenticado, CodigosErroItens.PersonagemNaoEncontrado);
        Assert.NotEqual(CodigosErro.NaoAutenticado, CodigosErroItens.ItemNaoEncontrado);
        Assert.NotEqual(CodigosErro.NaoAutenticado, CodigosErroItens.ItemEquipado);
    }

    [Fact]
    public void Respostas_comecam_com_arrays_vazios_e_nao_nulos()
    {
        Assert.Empty(new InventarioResponse().Entradas);
        Assert.Empty(new AbrirBauResponse().Ganhos);
        Assert.Empty(new AbrirBauResponse().Inventario);
        Assert.Equal("", new EntradaInventario().Nome);
    }
}
