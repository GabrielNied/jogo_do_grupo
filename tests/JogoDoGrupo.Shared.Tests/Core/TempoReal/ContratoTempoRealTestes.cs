using JogoDoGrupo.Shared.Core;
using JogoDoGrupo.Shared.Core.Api;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Shared.Tests.Core.TempoReal;

public class ContratoTempoRealTestes
{
    // ── IDs dentro da faixa do Core (0–999) ──────────────────────────────────

    [Fact]
    public void AutenticarTempoReal_id_esta_dentro_da_faixa_do_core() =>
        Assert.True(
            CoreMessageIds.Faixa.Contem(TempoRealIds.AutenticarTempoReal),
            $"ID {TempoRealIds.AutenticarTempoReal} fora de CoreMessageIds.Faixa ({CoreMessageIds.Faixa.Inicio}–{CoreMessageIds.Faixa.Fim})"
        );

    [Fact]
    public void ResultadoAutenticacaoTempoReal_id_esta_dentro_da_faixa_do_core() =>
        Assert.True(
            CoreMessageIds.Faixa.Contem(TempoRealIds.ResultadoAutenticacaoTempoReal),
            $"ID {TempoRealIds.ResultadoAutenticacaoTempoReal} fora de CoreMessageIds.Faixa"
        );

    // ── IDs distintos ─────────────────────────────────────────────────────────

    [Fact]
    public void Ids_tempo_real_sao_distintos() =>
        Assert.NotEqual(
            TempoRealIds.AutenticarTempoReal,
            TempoRealIds.ResultadoAutenticacaoTempoReal
        );

    // ── Códigos de recusa conhecidos ──────────────────────────────────────────

    [Fact]
    public void ResultadoAutenticacaoTempoReal_codigo_de_recusa_nao_autenticado_bate_com_CodigosErro() =>
        Assert.Equal(CodigosErro.NaoAutenticado, "NaoAutenticado");

    [Fact]
    public void ResultadoAutenticacaoTempoReal_codigo_de_recusa_acesso_negado_bate_com_CodigosErro() =>
        Assert.Equal(CodigosErro.AcessoNegado, "AcessoNegado");

    // ── Valores padrão do contrato ────────────────────────────────────────────

    [Fact]
    public void AutenticarTempoReal_campos_padrao_nao_nulos()
    {
        var msg = new AutenticarTempoReal();
        Assert.Equal("", msg.Token);
        Assert.Equal(0L, msg.PersonagemId);
    }

    [Fact]
    public void ResultadoAutenticacaoTempoReal_campos_padrao_nao_nulos()
    {
        var msg = new ResultadoAutenticacaoTempoReal();
        Assert.False(msg.Aceita);
        Assert.Equal("", msg.Codigo);
    }
}
