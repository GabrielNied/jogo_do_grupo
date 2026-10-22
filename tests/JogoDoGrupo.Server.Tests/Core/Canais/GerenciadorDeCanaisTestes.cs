using JogoDoGrupo.Server.Core.Canais;
using JogoDoGrupo.Shared.Features.Mundo;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Core.Canais;

public sealed class GerenciadorDeCanaisTestes
{
    [Fact]
    public void Jogadores_avulsos_entram_no_primeiro_canal_com_vaga()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );

        var primeiro = gerenciador.EscolherCanalDeEntrada(1, membrosDoGrupo: null);
        Assert.Equal(1, primeiro);
        gerenciador.RegistrarEntrada(1, primeiro!.Value);

        // Mesmo com o canal 2 e 3 vazios, o segundo avulso encontra o primeiro no canal 1
        var segundo = gerenciador.EscolherCanalDeEntrada(2, membrosDoGrupo: null);
        Assert.Equal(1, segundo);
    }

    [Fact]
    public void Avulso_vai_ao_proximo_canal_quando_o_primeiro_esta_cheio()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 1)
        );

        gerenciador.RegistrarEntrada(1, 1);

        Assert.Equal(2, gerenciador.EscolherCanalDeEntrada(2, membrosDoGrupo: null));
    }

    [Fact]
    public void Canal_que_volta_a_ter_vaga_e_o_proximo_escolhido()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 1)
        );

        gerenciador.RegistrarEntrada(1, 1);
        gerenciador.RegistrarEntrada(2, 2);
        gerenciador.RegistrarSaida(1);

        Assert.Equal(1, gerenciador.EscolherCanalDeEntrada(3, membrosDoGrupo: null));
    }

    [Fact]
    public void Prefere_canal_do_membro_do_grupo_se_houver_vaga()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );

        // Jogador 1 (membro do grupo) está no canal 2
        gerenciador.RegistrarEntrada(1, 2);
        // Canal 3 está vazio (0 jogadores)

        // Jogador 2 avulso iria para o canal 1, mas tem o jogador 1 no grupo
        var canalEscolhido = gerenciador.EscolherCanalDeEntrada(2, membrosDoGrupo: [1]);
        Assert.Equal(2, canalEscolhido);
    }

    [Fact]
    public void Recusa_entrada_quando_todos_os_canais_estao_lotados()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 2, CapacidadePorCanal: 1)
        );

        gerenciador.RegistrarEntrada(1, 1);
        gerenciador.RegistrarEntrada(2, 2);

        var canalEscolhido = gerenciador.EscolherCanalDeEntrada(3, membrosDoGrupo: null);
        Assert.Null(canalEscolhido);
    }

    [Fact]
    public void Troca_de_canal_recusada_se_em_combate()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );
        gerenciador.RegistrarEntrada(1, 1);

        var resultado = gerenciador.TentarTrocarCanal(1, canalDestino: 2, emCombate: true);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CodigosErroMundo.EmCombate, resultado.CodigoErro);
        Assert.Equal(1, resultado.CanalAtual);
    }

    [Fact]
    public void Troca_de_canal_recusada_para_o_mesmo_canal()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );
        gerenciador.RegistrarEntrada(1, 1);

        var resultado = gerenciador.TentarTrocarCanal(1, canalDestino: 1, emCombate: false);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CodigosErroMundo.MesmoCanal, resultado.CodigoErro);
    }

    [Fact]
    public void Troca_de_canal_recusada_para_canal_invalido()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );
        gerenciador.RegistrarEntrada(1, 1);

        var resultado0 = gerenciador.TentarTrocarCanal(1, canalDestino: 0, emCombate: false);
        var resultado4 = gerenciador.TentarTrocarCanal(1, canalDestino: 4, emCombate: false);

        Assert.False(resultado0.Sucesso);
        Assert.Equal(CodigosErroMundo.CanalInvalido, resultado0.CodigoErro);
        Assert.False(resultado4.Sucesso);
        Assert.Equal(CodigosErroMundo.CanalInvalido, resultado4.CodigoErro);
    }

    [Fact]
    public void Troca_de_canal_recusada_se_destino_lotado()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 2, CapacidadePorCanal: 1)
        );
        gerenciador.RegistrarEntrada(1, 1);
        gerenciador.RegistrarEntrada(2, 2);

        var resultado = gerenciador.TentarTrocarCanal(1, canalDestino: 2, emCombate: false);

        Assert.False(resultado.Sucesso);
        Assert.Equal(CodigosErroMundo.CanalLotado, resultado.CodigoErro);
    }

    [Fact]
    public void Troca_de_canal_com_sucesso_atualiza_ocupacao()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 10)
        );
        gerenciador.RegistrarEntrada(1, 1);

        var resultado = gerenciador.TentarTrocarCanal(1, canalDestino: 2, emCombate: false);

        Assert.True(resultado.Sucesso);
        Assert.Equal(1, resultado.CanalAtual);
        Assert.Equal(2, resultado.CanalDestino);
        Assert.Equal(0, gerenciador.OcupacaoDoCanal(1));
        Assert.Equal(1, gerenciador.OcupacaoDoCanal(2));
        Assert.Equal(2, gerenciador.ObterCanalDoPersonagem(1));
    }

    [Fact]
    public void Relatorio_de_status_reflete_ocupacao_de_todos_os_canais()
    {
        var gerenciador = new GerenciadorDeCanais(
            new ConfiguracaoCanais(QuantidadeCanais: 3, CapacidadePorCanal: 80)
        );
        gerenciador.RegistrarEntrada(1, 1);
        gerenciador.RegistrarEntrada(2, 1);
        gerenciador.RegistrarEntrada(3, 2);

        var status = gerenciador.ObterStatusDosCanais();

        Assert.Equal(3, status.Count);
        Assert.Equal(2, status[0].Jogadores);
        Assert.Equal(80, status[0].Capacidade);
        Assert.Equal(1, status[1].Jogadores);
        Assert.Equal(0, status[2].Jogadores);
    }
}
