using JogoDoGrupo.Server.Contratos.Social;
using static JogoDoGrupo.Server.Tests.Features.Social.AmbienteDeGrupo;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class ConsultaDeGrupoTestes
{
    private readonly AmbienteDeGrupo _ambiente = new();

    [Fact]
    public void Personagem_solo_nao_tem_grupo()
    {
        IConsultaDeGrupo consulta = _ambiente.Grupos;

        Assert.Null(consulta.Consultar(A));
    }

    [Fact]
    public void Consulta_retorna_lider_e_membros_na_ordem_atual_e_atualiza_apos_saida()
    {
        _ambiente.FormarGrupo(A, B, C);
        IConsultaDeGrupo consulta = _ambiente.Grupos;
        var inicial = consulta.Consultar(C)!;

        Assert.Equal(A, inicial.LiderId);
        Assert.Equal([A, B, C], inicial.Membros);

        _ambiente.Grupos.Sair(B);

        var atualizado = consulta.Consultar(A)!;
        Assert.Equal(inicial.GrupoId, atualizado.GrupoId);
        Assert.Equal(A, atualizado.LiderId);
        Assert.Equal([A, C], atualizado.Membros);
        Assert.Null(consulta.Consultar(B));
    }

    [Fact]
    public void Consulta_reflete_imediatamente_a_troca_de_lider()
    {
        _ambiente.FormarGrupo(A, B, C);
        IConsultaDeGrupo consulta = _ambiente.Grupos;

        _ambiente.Grupos.PassarLideranca(A, C);

        var atualizado = consulta.Consultar(B)!;
        Assert.Equal(C, atualizado.LiderId);
        Assert.Equal([A, B, C], atualizado.Membros);
    }
}
