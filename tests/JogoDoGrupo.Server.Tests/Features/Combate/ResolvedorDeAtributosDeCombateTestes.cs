using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Talentos;
using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Talentos;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public sealed class ResolvedorDeAtributosDeCombateTestes
{
    [Fact]
    public void ExtrairLinhas_com_array_vazio_ou_nulo_retorna_array_vazio_sem_alocacao()
    {
        var vazias = ResolvedorDeAtributosDeCombate.ExtrairLinhas([]);
        var nulas = ResolvedorDeAtributosDeCombate.ExtrairLinhas(null);

        Assert.Same(Array.Empty<string>(), vazias);
        Assert.Same(Array.Empty<string>(), nulas);
    }

    [Fact]
    public void ExtrairLinhas_com_itens_extrai_textos_em_ordem()
    {
        LinhaDeEquipamento[] linhas =
        [
            new LinhaDeEquipamento { Texto = "Dano +10" },
            new LinhaDeEquipamento { Texto = "Vida +50" },
        ];

        var extraidas = ResolvedorDeAtributosDeCombate.ExtrairLinhas(linhas);

        Assert.Equal(["Dano +10", "Vida +50"], extraidas);
    }

    [Fact]
    public async Task ResolverAsync_consolida_atributos_maximos_e_secundarios()
    {
        var bonus = new BonusFalso();
        var equipamentos = new EquipamentosFalso();
        var pontos = new PontosFalso();
        var fontes = new IFonteDeModificadores[] { new FonteFalsa() };

        var resolvedor = new ResolvedorDeAtributosDeCombate(
            bonus,
            equipamentos,
            pontos,
            fontes,
            NullLogger.Instance
        );

        var snapshot = await resolvedor.ResolverAsync(101, CancellationToken.None);

        Assert.NotNull(snapshot.Atributos);
        Assert.NotNull(snapshot.Valores);
        Assert.NotNull(snapshot.Secundarios);
        Assert.Equal(["Linha 1"], snapshot.LinhasDoEquipamento);
        Assert.True(snapshot.Maximos.Vida > 0);
    }

    private sealed class BonusFalso : IBonusDeAtributos
    {
        public Task<AtributosCombate> EfeitosAsync(
            long personagemId,
            CancellationToken cancelamento
        ) => Task.FromResult(new AtributosCombate());
    }

    private sealed class EquipamentosFalso : IAtributosDosEquipamentos
    {
        public Task<AtributosDosEquipamentos> ObterAsync(
            long personagemId,
            CancellationToken cancelamento
        ) =>
            Task.FromResult(
                new AtributosDosEquipamentos
                {
                    VidaMaxima = 50,
                    Linhas = [new LinhaDeEquipamento { Texto = "Linha 1" }],
                }
            );
    }

    private sealed class PontosFalso : IAtributosDaArvore
    {
        public Task<AtributosDaArvore> ObterAsync(
            long personagemId,
            CancellationToken cancelamento
        ) => Task.FromResult(new AtributosDaArvore { Vigor = 10 });
    }

    private sealed class FonteFalsa : IFonteDeModificadores
    {
        public FonteDeModificador Fonte => FonteDeModificador.Equipamento;

        public Task<IReadOnlyList<Modificador>> ObterAsync(
            long personagemId,
            CancellationToken cancelamento
        ) =>
            Task.FromResult<IReadOnlyList<Modificador>>([
                new Modificador
                {
                    Atributo = AtributoSecundario.ChanceCritica,
                    Fonte = FonteDeModificador.Equipamento,
                    Operacao = OperacaoDeModificador.Soma,
                    Origem = "Teste",
                    Grupo = "Teste",
                    Valor = 0.05,
                },
            ]);
    }
}
