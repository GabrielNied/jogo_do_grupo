using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class GerenciadorDeEscudosTestes
{
    private long _agoraMs = 1000;
    private readonly GerenciadorDeEscudos _escudos;
    private readonly List<EscudoMudou> _avisos = [];

    public GerenciadorDeEscudosTestes()
    {
        _escudos = new GerenciadorDeEscudos(() => _agoraMs);
        _escudos.Mudou += (_, aviso) => _avisos.Add(aviso);
    }

    [Fact]
    public void Escudo_absorve_antes_da_vida_e_o_resto_passa()
    {
        _escudos.Aplicar(1, 30, 8);

        Assert.Equal(0, _escudos.Absorver(1, 20));
        Assert.Equal(10, _escudos.AbsorcaoAtual(1));
        Assert.Equal(15, _escudos.Absorver(1, 25));
        Assert.Equal(0, _escudos.AbsorcaoAtual(1));
        Assert.Equal(12, _escudos.Absorver(1, 12));
        Assert.Equal(0, _avisos[^1].Absorcao);
    }

    [Fact]
    public void Escudo_e_so_de_quem_recebeu()
    {
        _escudos.Aplicar(1, 30, 8);

        Assert.Equal(25, _escudos.Absorver(2, 25));
        Assert.Equal(30, _escudos.AbsorcaoAtual(1));
    }

    [Fact]
    public void Escudo_expira_no_tempo_e_avisa()
    {
        _escudos.Aplicar(1, 30, 8);

        _agoraMs += 7999;
        Assert.Equal(30, _escudos.AbsorcaoAtual(1));
        _agoraMs += 1;
        Assert.Equal(0, _escudos.AbsorcaoAtual(1));
        Assert.Equal(40, _escudos.Absorver(1, 40));

        _escudos.Atualizar();
        Assert.Equal(0, _avisos[^1].Absorcao);
    }

    [Fact]
    public void Mesmo_escudo_nao_acumula_vale_o_mais_forte_e_renova_a_duracao()
    {
        _escudos.Aplicar(1, 30, 8);
        _agoraMs += 5000;

        _escudos.Aplicar(1, 20, 8); // Mais fraco: a absorção fica, o tempo renova.
        Assert.Equal(30, _escudos.AbsorcaoAtual(1));
        _agoraMs += 5000;
        Assert.Equal(30, _escudos.AbsorcaoAtual(1));

        _escudos.Aplicar(1, 50, 8); // Mais forte: substitui, não soma.
        Assert.Equal(50, _escudos.AbsorcaoAtual(1));
    }

    [Fact]
    public async Task Absorcao_simultanea_nunca_passa_do_escudo()
    {
        _escudos.Aplicar(1, 100, 8);

        var passaram = await Task.WhenAll(
            Enumerable.Range(0, 50).Select(_ => Task.Run(() => _escudos.Absorver(1, 5)))
        );

        Assert.Equal(150, passaram.Sum());
        Assert.Equal(0, _escudos.AbsorcaoAtual(1));
    }

    [Fact]
    public void Buff_de_grupo_nao_acumula_so_renova()
    {
        long agora = 0;
        var status = new GerenciadorDeEfeitosDeStatus(() => agora);

        Assert.Equal(
            ResultadoAplicacaoStatus.Aplicado,
            status.Aplicar(1, TipoEfeitoDeStatus.Protegido, 12)
        );
        agora += 6000;
        Assert.Equal(
            ResultadoAplicacaoStatus.Renovado,
            status.Aplicar(1, TipoEfeitoDeStatus.Protegido, 12)
        );

        status.ObterRetrato(1, out var efeitos, out _);
        var protegido = Assert.Single(efeitos);
        Assert.Equal(1, protegido.Acumulos);
        Assert.Equal(12f, protegido.DuracaoRestanteSegundos);
        Assert.Equal(0.85, status.MultiplicadorDanoRecebido(1), 3);

        agora += 12001;
        Assert.Equal(1.0, status.MultiplicadorDanoRecebido(1), 3);
    }
}
