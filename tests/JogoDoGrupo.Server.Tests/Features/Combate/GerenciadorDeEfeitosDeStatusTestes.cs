using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

public class GerenciadorDeEfeitosDeStatusTestes
{
    [Fact]
    public void Aplicar_lentidao_renova_duracao_sem_duplicar()
    {
        long tempoMs = 1000;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);

        var r1 = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Lentidao, duracaoSegundos: 4.0);
        Assert.Equal(ResultadoAplicacaoStatus.Aplicado, r1);
        Assert.True(gerenciador.TemLentidao(1));
        Assert.Equal(0.70, gerenciador.ModificadorVelocidade(1), precision: 2);

        // Avança 2 segundos e reaplica: renova
        tempoMs += 2000;
        var r2 = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Lentidao, duracaoSegundos: 4.0);
        Assert.Equal(ResultadoAplicacaoStatus.Renovado, r2);

        gerenciador.ObterRetrato(1, out var efeitos, out _);
        Assert.Single(efeitos);
        Assert.Equal(TipoEfeitoDeStatus.Lentidao, efeitos[0].Tipo);
        Assert.Equal(1, efeitos[0].Acumulos);
        Assert.Equal(4.0f, efeitos[0].DuracaoRestanteSegundos);
    }

    [Fact]
    public void Atordoado_impede_acoes_e_gera_6s_de_imunidade()
    {
        long tempoMs = 0;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);
        var controle = new ControleDeAcoes(gerenciador);

        var r = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 2.0);
        Assert.Equal(ResultadoAplicacaoStatus.Aplicado, r);
        Assert.True(gerenciador.EstaAtordoado(1));
        Assert.False(gerenciador.EstaImuneAAtordoado(1));

        // Controle de ações recusa enquanto atordoado
        var pedido = new PedidoDeAtaque(TipoDeAtaque.Leve, 0f, 1);
        var resPedido = controle.Pedir(1, pedido, tempoMs, out _);
        Assert.Equal(ResultadoDoPedido.Atordoado, resPedido);

        var resEsquiva = controle.TentarOcupar(1, tempoMs, 500, out _);
        Assert.False(resEsquiva);

        var resGuarda = controle.TentarGuardar(1, tempoMs);
        Assert.False(resGuarda);

        // Avança 2 segundos (atordoado expira)
        tempoMs += 2000;
        gerenciador.Atualizar(tempoMs, out _, out var mudancas);
        Assert.Contains(1L, mudancas);
        Assert.False(gerenciador.EstaAtordoado(1));
        Assert.True(gerenciador.EstaImuneAAtordoado(1));

        // Tentar atordoar durante a imunidade é ignorado
        var rImune = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 2.0);
        Assert.Equal(ResultadoAplicacaoStatus.Imune, rImune);
        Assert.False(gerenciador.EstaAtordoado(1));

        // Avança 6 segundos da imunidade
        tempoMs += 6000;
        Assert.False(gerenciador.EstaImuneAAtordoado(1));

        // Agora pode atordoar novamente
        var rNovo = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Atordoado, duracaoSegundos: 2.0);
        Assert.Equal(ResultadoAplicacaoStatus.Aplicado, rNovo);
        Assert.True(gerenciador.EstaAtordoado(1));
    }

    [Fact]
    public void Boss_imune_a_atordoado_acumula_quebra_e_ativa_vulnerabilidade()
    {
        long tempoMs = 0;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);

        // Boss recebe 4 atordoamentos (25 cada -> total 100)
        var r1 = gerenciador.Aplicar(100, TipoEfeitoDeStatus.Atordoado, ehBoss: true);
        Assert.Equal(ResultadoAplicacaoStatus.AvancoQuebraBoss, r1);
        Assert.Equal(25, gerenciador.ObterQuebraBoss(100));
        Assert.False(gerenciador.EstaAtordoado(100));

        var r2 = gerenciador.Aplicar(100, TipoEfeitoDeStatus.Atordoado, ehBoss: true);
        Assert.Equal(ResultadoAplicacaoStatus.AvancoQuebraBoss, r2);
        Assert.Equal(50, gerenciador.ObterQuebraBoss(100));

        var r3 = gerenciador.Aplicar(100, TipoEfeitoDeStatus.Atordoado, ehBoss: true);
        Assert.Equal(ResultadoAplicacaoStatus.AvancoQuebraBoss, r3);
        Assert.Equal(75, gerenciador.ObterQuebraBoss(100));

        var r4 = gerenciador.Aplicar(100, TipoEfeitoDeStatus.Atordoado, ehBoss: true);
        Assert.Equal(ResultadoAplicacaoStatus.QuebrouBoss, r4);
        Assert.Equal(0, gerenciador.ObterQuebraBoss(100)); // Reseta barra
        Assert.True(gerenciador.TemVulneravel(100)); // Abre janela de vulnerabilidade
        Assert.Equal(1.15, gerenciador.MultiplicadorDanoRecebido(100), precision: 2);
    }

    [Fact]
    public void DoT_sangramento_e_queimadura_acumulam_ate_3_e_geram_ticks()
    {
        long tempoMs = 0;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);

        gerenciador.Aplicar(1, TipoEfeitoDeStatus.Sangramento, duracaoSegundos: 5.0);
        gerenciador.Aplicar(1, TipoEfeitoDeStatus.Sangramento, duracaoSegundos: 5.0);
        gerenciador.Aplicar(1, TipoEfeitoDeStatus.Sangramento, duracaoSegundos: 5.0);
        // 4ª aplicação não passa de 3
        var r4 = gerenciador.Aplicar(1, TipoEfeitoDeStatus.Sangramento, duracaoSegundos: 5.0);
        Assert.Equal(ResultadoAplicacaoStatus.Renovado, r4);

        gerenciador.ObterRetrato(1, out var efeitos, out _);
        var sangramento = Assert.Single(efeitos);
        Assert.Equal(3, sangramento.Acumulos);

        // Avança 1 segundo: tick DoT acontece
        tempoMs += 1000;
        gerenciador.Atualizar(tempoMs, out var danos1, out _);
        var d1 = Assert.Single(danos1);
        Assert.Equal(1L, d1.EntidadeId);
        Assert.Equal(TipoEfeitoDeStatus.Sangramento, d1.Tipo);
        Assert.Equal(15, d1.Dano); // 5 * 3 acumulos = 15

        // Avança mais 500 ms: sem tick
        tempoMs += 500;
        gerenciador.Atualizar(tempoMs, out var danos2, out _);
        Assert.Empty(danos2);

        // Avança mais 500 ms (total 1000 ms desde o último): novo tick
        tempoMs += 500;
        gerenciador.Atualizar(tempoMs, out var danos3, out _);
        var d3 = Assert.Single(danos3);
        Assert.Equal(15, d3.Dano);
    }

    [Fact]
    public void Fortalecido_e_vulneravel_modificam_multiplicadores()
    {
        long tempoMs = 0;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);

        gerenciador.Aplicar(1, TipoEfeitoDeStatus.Fortalecido);
        gerenciador.Aplicar(2, TipoEfeitoDeStatus.Vulneravel);

        Assert.Equal(1.15, gerenciador.MultiplicadorDanoCausado(1), precision: 2);
        Assert.Equal(1.00, gerenciador.MultiplicadorDanoCausado(2), precision: 2);

        Assert.Equal(1.00, gerenciador.MultiplicadorDanoRecebido(1), precision: 2);
        Assert.Equal(1.15, gerenciador.MultiplicadorDanoRecebido(2), precision: 2);
    }

    [Fact]
    public void Remover_efeito_e_limpar_entidade()
    {
        long tempoMs = 0;
        var gerenciador = new GerenciadorDeEfeitosDeStatus(() => tempoMs);

        gerenciador.Aplicar(1, TipoEfeitoDeStatus.Atordoado);
        Assert.True(gerenciador.EstaAtordoado(1));

        gerenciador.Remover(1, TipoEfeitoDeStatus.Atordoado);
        Assert.False(gerenciador.EstaAtordoado(1));
        Assert.True(gerenciador.EstaImuneAAtordoado(1)); // Remoção manual de atordoado também inicia imunidade

        gerenciador.Limpar(1);
        Assert.False(gerenciador.EstaImuneAAtordoado(1));
    }
}
