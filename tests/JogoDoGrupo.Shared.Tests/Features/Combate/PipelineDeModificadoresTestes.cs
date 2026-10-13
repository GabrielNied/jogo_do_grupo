using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Shared.Tests.Features.Combate;

public class PipelineDeModificadoresTestes
{
    private const AtributoSecundario Atributo = AtributoSecundario.ChanceCritica;

    private static Modificador M(
        OperacaoDeModificador operacao,
        double valor,
        int ordem = 0,
        string grupo = "g",
        FonteDeModificador fonte = FonteDeModificador.Arvore,
        string origem = "o"
    ) =>
        new()
        {
            Atributo = Atributo,
            Fonte = fonte,
            Origem = origem,
            Operacao = operacao,
            Ordem = ordem,
            Grupo = grupo,
            Valor = valor,
        };

    [Fact]
    public void Somas_do_mesmo_passo_somam()
    {
        var r = PipelineDeModificadores.Calcular(
            Atributo,
            [M(OperacaoDeModificador.Soma, 10), M(OperacaoDeModificador.Soma, 5)]
        );

        Assert.Equal(15, r.Valor);
    }

    [Fact]
    public void A_multiplicacao_so_alcanca_o_que_entrou_antes_dela()
    {
        var multiplicaDepois = PipelineDeModificadores.Calcular(
            Atributo,
            [
                M(OperacaoDeModificador.Soma, 10, ordem: 0),
                M(OperacaoDeModificador.Multiplica, 0.5, ordem: 1),
            ]
        );
        var multiplicaAntes = PipelineDeModificadores.Calcular(
            Atributo,
            [
                M(OperacaoDeModificador.Soma, 10, ordem: 1),
                M(OperacaoDeModificador.Multiplica, 0.5, ordem: 0),
            ]
        );

        Assert.Equal(15, multiplicaDepois.Valor);
        Assert.Equal(10, multiplicaAntes.Valor); // multiplicou o 0 que havia, depois somou
    }

    [Fact]
    public void No_mesmo_passo_a_soma_entra_antes_da_multiplicacao()
    {
        var r = PipelineDeModificadores.Calcular(
            Atributo,
            [M(OperacaoDeModificador.Multiplica, 0.5), M(OperacaoDeModificador.Soma, 10)]
        );

        Assert.Equal(15, r.Valor);
    }

    [Fact]
    public void Mesmo_grupo_soma_os_bonus_e_grupos_diferentes_se_multiplicam()
    {
        var mesmo = PipelineDeModificadores.Calcular(
            Atributo,
            [
                M(OperacaoDeModificador.Soma, 100),
                M(OperacaoDeModificador.Multiplica, 0.1, ordem: 1, grupo: "a"),
                M(OperacaoDeModificador.Multiplica, 0.1, ordem: 1, grupo: "a"),
            ]
        );
        var diferentes = PipelineDeModificadores.Calcular(
            Atributo,
            [
                M(OperacaoDeModificador.Soma, 100),
                M(OperacaoDeModificador.Multiplica, 0.1, ordem: 1, grupo: "a"),
                M(OperacaoDeModificador.Multiplica, 0.1, ordem: 1, grupo: "b"),
            ]
        );

        Assert.Equal(120, mesmo.Valor, 9);
        Assert.Equal(121, diferentes.Valor, 9);
    }

    [Fact]
    public void A_ordem_da_lista_nao_muda_o_resultado_nem_as_parcelas()
    {
        var lista = new[]
        {
            M(OperacaoDeModificador.Soma, 3, origem: "a", fonte: FonteDeModificador.Base),
            M(
                OperacaoDeModificador.Soma,
                4,
                ordem: 30,
                origem: "b",
                fonte: FonteDeModificador.Equipamento
            ),
            M(OperacaoDeModificador.Multiplica, 0.2, ordem: 20, grupo: "x", origem: "c"),
            M(OperacaoDeModificador.Multiplica, 0.3, ordem: 20, grupo: "y", origem: "d"),
            M(OperacaoDeModificador.Multiplica, 0.1, ordem: 20, grupo: "x", origem: "e"),
        };

        var a = PipelineDeModificadores.Calcular(Atributo, lista);
        var b = PipelineDeModificadores.Calcular(Atributo, lista.Reverse().ToArray());

        Assert.Equal(a.Valor, b.Valor);
        Assert.Equal(a.Parcelas.Select(p => p.Origem), b.Parcelas.Select(p => p.Origem));
    }

    [Fact]
    public void Sem_grupo_de_acumulo_ou_de_outro_atributo_nao_entra()
    {
        var outro = M(OperacaoDeModificador.Soma, 99);
        outro.Atributo = AtributoSecundario.DanoCritico;

        var r = PipelineDeModificadores.Calcular(
            Atributo,
            [M(OperacaoDeModificador.Soma, 7, grupo: ""), outro, M(OperacaoDeModificador.Soma, 2)]
        );

        Assert.Equal(2, r.Valor);
        Assert.Single(r.Parcelas);
    }

    [Fact]
    public void Os_nove_guardam_a_origem_de_cada_parcela_e_limitam_o_resultado()
    {
        var arvore = new Modificador
        {
            Atributo = AtributoSecundario.ChanceCritica,
            Fonte = FonteDeModificador.Arvore,
            Origem = "Nó de teste",
            Operacao = OperacaoDeModificador.Soma,
            Ordem = ConfigAtributosSecundarios.OrdemArvore,
            Grupo = "arvore",
            Valor = 5, // acima do teto
        };
        var falsaBase = new Modificador
        {
            Atributo = AtributoSecundario.DanoCritico,
            Fonte = FonteDeModificador.Base,
            Origem = "Tentativa de duplicar a base",
            Grupo = "base",
            Valor = 100,
        };

        var r = RegrasSecundarios.Calcular([arvore, falsaBase]);

        Assert.Equal(9, r.Length);
        var chance = r.Single(c => c.Atributo == AtributoSecundario.ChanceCritica);
        Assert.Equal(ConfigAtributosSecundarios.TetoChanceCritica, chance.Valor);
        Assert.Equal(
            [FonteDeModificador.Base, FonteDeModificador.Arvore],
            chance.Parcelas.Select(p => p.Fonte)
        );
        var dano = r.Single(c => c.Atributo == AtributoSecundario.DanoCritico);
        Assert.Equal(ConfigAtributosSecundarios.BaseDanoCritico, dano.Valor);
        Assert.Equal(0, RegrasSecundarios.Valores(r).RegeneracaoVida);
    }
}
