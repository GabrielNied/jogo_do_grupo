using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// A troca de arma reserva o personagem por 0,4 s no <see cref="ControleDeAcoes"/>: golpe,
/// esquiva e guarda são recusados nesse tempo e voltam a valer no fim, sem janela sem dano.
/// </summary>
public class TrocaDeArmaTestes
{
    private const long Ana = 1;
    private static readonly long Troca = RegrasTrocaDeArma.DuracaoMs;

    private static PedidoDeAtaque Leve() => new(TipoDeAtaque.Leve, 0f, 1);

    private static ControleDeAcoes ControleTrocando(long agoraMs = 1_000)
    {
        var controle = new ControleDeAcoes();
        Assert.True(controle.TentarOcupar(Ana, agoraMs, Troca, out _, invulneravel: false));
        return controle;
    }

    [Fact]
    public void Duracao_da_troca_e_de_400_ms()
    {
        Assert.Equal(400, RegrasTrocaDeArma.DuracaoMs);
    }

    [Fact]
    public void Durante_a_troca_o_golpe_e_recusado_e_depois_vale()
    {
        var controle = ControleTrocando();

        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 1_000, out _));
        Assert.Equal(ResultadoDoPedido.Recusado, controle.Pedir(Ana, Leve(), 1_399, out _));
        Assert.Equal(ResultadoDoPedido.Iniciado, controle.Pedir(Ana, Leve(), 1_400, out _));
    }

    [Fact]
    public void Durante_a_troca_a_esquiva_e_recusada_e_depois_vale()
    {
        var controle = ControleTrocando();

        Assert.False(controle.TentarOcupar(Ana, 1_399, 600, out _));
        Assert.True(controle.TentarOcupar(Ana, 1_400, 600, out _));
    }

    [Fact]
    public void Durante_a_troca_a_guarda_e_recusada_e_depois_vale()
    {
        var controle = ControleTrocando();

        Assert.False(controle.TentarGuardar(Ana, 1_399));
        Assert.True(controle.TentarGuardar(Ana, 1_400));
    }

    [Fact]
    public void A_troca_nao_da_janela_sem_dano()
    {
        var controle = ControleTrocando();

        Assert.False(controle.EstaInvulneravel(Ana, 1_100));
    }

    [Fact]
    public void Nao_troca_no_preparo_do_golpe_nem_com_a_guarda_segurada_nem_na_esquiva()
    {
        var golpe = new ControleDeAcoes();
        golpe.Pedir(Ana, Leve(), 1_000, out _);
        Assert.False(golpe.TentarOcupar(Ana, 1_000, Troca, out _, invulneravel: false));

        var guarda = new ControleDeAcoes();
        guarda.TentarGuardar(Ana, 1_000);
        Assert.False(guarda.TentarOcupar(Ana, 1_000, Troca, out _, invulneravel: false));

        var esquiva = new ControleDeAcoes();
        esquiva.TentarOcupar(Ana, 1_000, 600, out _);
        Assert.False(esquiva.TentarOcupar(Ana, 1_100, Troca, out _, invulneravel: false));
    }

    [Fact]
    public void Troca_nao_tem_recarga_propria_e_pode_repetir_ao_fim_dos_400_ms()
    {
        var controle = ControleTrocando();

        Assert.False(controle.TentarOcupar(Ana, 1_200, Troca, out _, invulneravel: false));
        Assert.True(controle.TentarOcupar(Ana, 1_400, Troca, out _, invulneravel: false));
    }
}
