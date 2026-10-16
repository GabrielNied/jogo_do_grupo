using JogoDoGrupo.Server.Features.Combate;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Tests.Features.Combate;

/// <summary>
/// As regras do estado caído sem rede nem banco, com os tempos reais (45 s, 3 s, 5 s) e o
/// relógio em ms passado à mão: limites exatos e concorrência.
/// </summary>
public class ControleDeQuedaTestes
{
    private const long Ana = 1; // a caída
    private const long Beto = 2; // ajudante
    private const long Caio = 3; // outro ajudante
    private const long Inicio = 10_000;

    private static readonly Func<bool> Perto = () => true;
    private static readonly Func<bool> Longe = () => false;

    private readonly ControleDeQueda _controle = new();

    private void AnaCai(long agora = Inicio) => Assert.True(_controle.Cair(Ana, agora, out _));

    private ResultadoDeComecar Levantar(long ajudante, long alvo, long agora, bool perto = true) =>
        _controle.Comecar(ajudante, alvo, agora, perto ? Perto : Longe, out _);

    // ── Cair ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Cair_marca_caido_com_45_segundos()
    {
        AnaCai();

        Assert.True(_controle.EstaCaido(Ana));
        var s = _controle.Situacao(Ana, Inicio);
        Assert.Equal(FaseDeQueda.Caido, s.Fase);
        Assert.Equal(RegrasQueda.DuracaoCaidoMs, s.RestanteMs);
        Assert.Equal(0, s.AjudanteId);
    }

    [Fact]
    public void Cair_de_novo_nao_reinicia_a_contagem()
    {
        AnaCai();

        Assert.False(_controle.Cair(Ana, Inicio + 20_000, out _));

        Assert.Equal(25_000, _controle.Situacao(Ana, Inicio + 20_000).RestanteMs);
    }

    [Fact]
    public void Quem_nunca_caiu_esta_vivo()
    {
        Assert.False(_controle.EstaCaido(Ana));
        Assert.Equal(FaseDeQueda.Vivo, _controle.Situacao(Ana, Inicio).Fase);
    }

    // ── Levantar ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Aliado_completa_3_segundos_e_o_caido_levanta()
    {
        AnaCai();

        Assert.Equal(ResultadoDeComecar.Iniciado, Levantar(Beto, Ana, Inicio + 1_000));
        var durante = _controle.Situacao(Ana, Inicio + 2_000);
        Assert.Equal(FaseDeQueda.Levantando, durante.Fase);
        Assert.Equal(Beto, durante.AjudanteId);
        Assert.Equal(1_000, durante.ProgressoMs);

        var saida = _controle.Resolver(Ana, Inicio + 4_000);

        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Levantou, Ana, Beto), saida);
        Assert.False(_controle.EstaCaido(Ana));
    }

    [Fact]
    public void Faltando_1_ms_para_os_3_segundos_ainda_nao_levantou()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 1_000);

        Assert.False(_controle.Resolver(Ana, Inicio + 3_999).Houve);
        Assert.True(_controle.EstaCaido(Ana));
        Assert.True(_controle.Resolver(Ana, Inicio + 4_000).Houve);
    }

    [Fact]
    public void A_contagem_pausa_enquanto_o_aliado_levanta_e_retoma_se_interrompido()
    {
        AnaCai();
        // Aos 10 s (35 s restantes), Beto começa a levantar
        Levantar(Beto, Ana, Inicio + 10_000);

        // Enquanto Beto levanta, o tempo restante permanece pausado em 35 s
        Assert.Equal(35_000, _controle.Situacao(Ana, Inicio + 12_000).RestanteMs);

        // Beto é interrompido aos 12 s (após 2 s de interação)
        _controle.Interromper(Beto, Inicio + 12_000, out _);

        // A contagem retoma: aos 12 s ainda restam 35 s
        Assert.Equal(35_000, _controle.Situacao(Ana, Inicio + 12_000).RestanteMs);

        // 10 s após a interrupção (aos 22 s), restam 25 s
        Assert.Equal(25_000, _controle.Situacao(Ana, Inicio + 22_000).RestanteMs);
    }

    [Fact]
    public void Resolver_sem_queda_ou_sem_vencer_nao_faz_nada()
    {
        Assert.False(_controle.Resolver(Ana, Inicio).Houve);
        AnaCai();
        Assert.False(_controle.Resolver(Ana, Inicio + 44_999).Houve);
    }

    // ── Recusas ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Alvo_vivo_recusa_AlvoNaoEstaCaido()
    {
        Assert.Equal(ResultadoDeComecar.AlvoNaoEstaCaido, Levantar(Beto, Ana, Inicio));
    }

    [Fact]
    public void Alvo_cujo_tempo_acabou_recusa_AlvoNaoEstaCaido_e_entrega_o_retorno()
    {
        AnaCai();

        var r = _controle.Comecar(Beto, Ana, Inicio + 45_000, Perto, out var efeito);

        Assert.Equal(ResultadoDeComecar.AlvoNaoEstaCaido, r);
        var saida = Assert.Single(efeito.Saidas);
        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Retorno, Ana, 0), saida);
    }

    [Fact]
    public void Ajudante_caido_recusa_VoceEstaCaido()
    {
        AnaCai();
        Assert.True(_controle.Cair(Beto, Inicio, out _));

        Assert.Equal(ResultadoDeComecar.VoceEstaCaido, Levantar(Beto, Ana, Inicio + 100));
        Assert.Equal(FaseDeQueda.Caido, _controle.Situacao(Ana, Inicio + 100).Fase);
    }

    [Fact]
    public void Longe_demais_recusa_e_nao_muda_nada()
    {
        AnaCai();

        Assert.Equal(
            ResultadoDeComecar.LongeDemais,
            Levantar(Beto, Ana, Inicio + 100, perto: false)
        );

        var s = _controle.Situacao(Ana, Inicio + 100);
        Assert.Equal(FaseDeQueda.Caido, s.Fase);
        Assert.Equal(0, s.AjudanteId);
    }

    [Fact]
    public void Segundo_ajudante_recebe_JaEstaSendoLevantado_e_o_primeiro_continua()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);

        Assert.Equal(ResultadoDeComecar.JaEstaSendoLevantado, Levantar(Caio, Ana, Inicio + 200));

        var s = _controle.Situacao(Ana, Inicio + 1_100);
        Assert.Equal(Beto, s.AjudanteId);
        Assert.Equal(1_000, s.ProgressoMs);
        Assert.Equal(
            new SaidaDaQueda(TipoDeSaida.Levantou, Ana, Beto),
            _controle.Resolver(Ana, Inicio + 3_100)
        );
    }

    [Fact]
    public void O_mesmo_ajudante_pedindo_de_novo_nao_reinicia_o_progresso()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);

        Assert.Equal(ResultadoDeComecar.JaEstaSendoLevantado, Levantar(Beto, Ana, Inicio + 1_000));
        Assert.Equal(1_900, _controle.Situacao(Ana, Inicio + 2_000).ProgressoMs);
    }

    [Fact]
    public void Voltar_sem_estar_caido_recusa()
    {
        Assert.Equal(ResultadoDeVoltar.NaoEstaCaido, _controle.Voltar(Ana, Inicio, out var saida));
        Assert.False(saida.Houve);
    }

    // ── Interromper ───────────────────────────────────────────────────────────────

    [Fact]
    public void Interromper_zera_o_progresso_e_libera_o_caido()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);

        var alvo = _controle.Interromper(Beto, Inicio + 2_900, out var saida);

        Assert.Equal(Ana, alvo);
        Assert.False(saida.Houve);
        var s = _controle.Situacao(Ana, Inicio + 2_900);
        Assert.Equal(FaseDeQueda.Caido, s.Fase);
        Assert.Equal(0, s.ProgressoMs);
        Assert.False(_controle.Resolver(Ana, Inicio + 5_000).Houve);
    }

    [Fact]
    public void Depois_de_interromper_um_novo_ajudante_comeca_do_zero()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);
        _controle.Interromper(Beto, Inicio + 2_900, out _);

        Assert.Equal(ResultadoDeComecar.Iniciado, Levantar(Caio, Ana, Inicio + 3_000));

        Assert.False(_controle.Resolver(Ana, Inicio + 5_999).Houve);
        Assert.Equal(TipoDeSaida.Levantou, _controle.Resolver(Ana, Inicio + 6_000).Tipo);
    }

    [Fact]
    public void Interromper_quem_nao_levanta_ninguem_nao_faz_nada()
    {
        Assert.Equal(0, _controle.Interromper(Beto, Inicio, out var saida));
        Assert.False(saida.Houve);
    }

    [Fact]
    public void Ajudante_que_cai_interrompe_a_propria_interacao()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);

        Assert.True(_controle.Cair(Beto, Inicio + 1_000, out var efeito));

        Assert.Equal(Ana, efeito.AlvoInterrompido);
        Assert.Equal(FaseDeQueda.Caido, _controle.Situacao(Ana, Inicio + 1_000).Fase);
    }

    [Fact]
    public void Interrupcao_depois_do_fim_dos_3_segundos_perde_para_o_levantar()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);

        var alvo = _controle.Interromper(Beto, Inicio + 3_100, out var saida);

        Assert.Equal(0, alvo);
        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Levantou, Ana, Beto), saida);
    }

    [Fact]
    public void Ajudante_que_comeca_outro_caido_interrompe_o_primeiro()
    {
        AnaCai();
        Assert.True(_controle.Cair(Caio, Inicio, out _));
        Levantar(Beto, Ana, Inicio + 100);

        var r = _controle.Comecar(Beto, Caio, Inicio + 1_000, Perto, out var efeito);

        Assert.Equal(ResultadoDeComecar.Iniciado, r);
        Assert.Equal(Ana, efeito.AlvoInterrompido);
        Assert.Equal(0, _controle.AjudanteDe(Ana));
        Assert.Equal(Beto, _controle.AjudanteDe(Caio));
    }

    // ── Voltar à cidade ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(4_900)]
    [InlineData(4_999)]
    public void Voltar_antes_de_5_segundos_e_recusado_e_nada_muda(long depoisDeCair)
    {
        AnaCai();

        var r = _controle.Voltar(Ana, Inicio + depoisDeCair, out var saida);

        Assert.Equal(ResultadoDeVoltar.AindaNaoPodeVoltar, r);
        Assert.False(saida.Houve);
        Assert.True(_controle.EstaCaido(Ana));
    }

    [Fact]
    public void Voltar_com_exatamente_5_segundos_vale()
    {
        AnaCai();

        var r = _controle.Voltar(Ana, Inicio + 5_000, out var saida);

        Assert.Equal(ResultadoDeVoltar.Voltou, r);
        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Retorno, Ana, 0), saida);
        Assert.False(_controle.EstaCaido(Ana));
    }

    [Fact]
    public void Voltar_durante_a_interacao_libera_o_ajudante()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 5_000);

        _controle.Voltar(Ana, Inicio + 6_000, out var saida);

        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Retorno, Ana, Beto), saida);
        Assert.Equal(0, _controle.Interromper(Beto, Inicio + 6_100, out _));
    }

    [Fact]
    public void Voltar_depois_da_interacao_completa_perde_para_o_levantar()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 5_000);

        var r = _controle.Voltar(Ana, Inicio + 8_000, out var saida);

        Assert.Equal(ResultadoDeVoltar.NaoEstaCaido, r);
        Assert.Equal(TipoDeSaida.Levantou, saida.Tipo);
    }

    // ── Fim do tempo ──────────────────────────────────────────────────────────────

    [Fact]
    public void Fim_dos_45_segundos_volta_sozinho()
    {
        AnaCai();

        Assert.False(_controle.Resolver(Ana, Inicio + 44_999).Houve);
        var saida = _controle.Resolver(Ana, Inicio + 45_000);

        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Retorno, Ana, 0), saida);
        Assert.False(_controle.EstaCaido(Ana));
    }

    [Fact]
    public void Depois_da_saida_resolver_de_novo_nao_devolve_outra()
    {
        AnaCai();
        Assert.True(_controle.Resolver(Ana, Inicio + 45_000).Houve);

        Assert.False(_controle.Resolver(Ana, Inicio + 45_001).Houve);
        Assert.Equal(ResultadoDeVoltar.NaoEstaCaido, _controle.Voltar(Ana, Inicio + 46_000, out _));
    }

    [Fact]
    public void Interacao_que_comeca_perto_do_fim_do_tempo_pausa_a_contagem_e_completa()
    {
        AnaCai();
        // Aos 43 s faltavam 2 s para acabar a contagem
        Levantar(Beto, Ana, Inicio + 43_000);

        // Aos 45 s a contagem estaria em 0 se não pausasse, mas com a pausa Beto continua levantando
        Assert.False(_controle.Resolver(Ana, Inicio + 45_000).Houve);
        Assert.True(_controle.EstaCaido(Ana));

        // Aos 46 s (3 s de ajuda completados), Ana levanta com sucesso
        var saida = _controle.Resolver(Ana, Inicio + 46_000);
        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Levantou, Ana, Beto), saida);
        Assert.False(_controle.EstaCaido(Ana));
    }

    [Fact]
    public void Ao_levantar_personagem_fica_imune_a_dano_por_2_segundos()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 1_000);
        var saida = _controle.Resolver(Ana, Inicio + 4_000);
        Assert.Equal(TipoDeSaida.Levantou, saida.Tipo);

        // 1,999 s após levantar ainda está imune
        Assert.True(_controle.EstaImuneAposLevantar(Ana, Inicio + 4_000 + 1_999));

        // Exatamente 2,0 s a imunidade acaba
        Assert.False(_controle.EstaImuneAposLevantar(Ana, Inicio + 4_000 + 2_000));
    }

    [Fact]
    public void Fim_do_tempo_e_levantar_no_mesmo_instante_dao_uma_saida_so_e_levantar_vence()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 42_000); // completa exatamente em 45 s

        var saida = _controle.Resolver(Ana, Inicio + 45_000);

        Assert.Equal(new SaidaDaQueda(TipoDeSaida.Levantou, Ana, Beto), saida);
        Assert.False(_controle.Resolver(Ana, Inicio + 45_000).Houve);
    }

    [Fact]
    public void Aviso_de_quando_acordar_aponta_para_o_proximo_evento()
    {
        Assert.Equal(-1, _controle.MsAteOProximoEvento(Ana, Inicio));
        AnaCai();
        Assert.Equal(45_000, _controle.MsAteOProximoEvento(Ana, Inicio));

        Levantar(Beto, Ana, Inicio + 1_000);
        Assert.Equal(2_500, _controle.MsAteOProximoEvento(Ana, Inicio + 1_500));
    }

    // ── Concorrência ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Dois_ajudantes_ao_mesmo_tempo_so_um_vale()
    {
        for (var rodada = 0; rodada < 200; rodada++)
        {
            var controle = new ControleDeQueda();
            controle.Cair(Ana, Inicio, out _);
            using var largada = new ManualResetEventSlim();

            ResultadoDeComecar Tentar(long ajudante)
            {
                largada.Wait();
                return controle.Comecar(ajudante, Ana, Inicio + 100, Perto, out _);
            }

            var a = Task.Run(() => Tentar(Beto));
            var b = Task.Run(() => Tentar(Caio));
            largada.Set();
            var resultados = await Task.WhenAll(a, b);

            Assert.Single(resultados, ResultadoDeComecar.Iniciado);
            Assert.Single(resultados, ResultadoDeComecar.JaEstaSendoLevantado);
        }
    }

    [Fact]
    public async Task Fim_do_tempo_voltar_e_levantar_simultaneos_dao_uma_saida_so()
    {
        for (var rodada = 0; rodada < 300; rodada++)
        {
            var controle = new ControleDeQueda();
            controle.Cair(Ana, Inicio, out _);
            controle.Comecar(Beto, Ana, Inicio + 42_000, Perto, out _); // completa em 45 s
            var agora = Inicio + 45_000;
            using var largada = new ManualResetEventSlim();
            var saidas = new List<SaidaDaQueda>();
            var trava = new object();

            void Guardar(SaidaDaQueda s)
            {
                if (s.Houve)
                {
                    lock (trava)
                    {
                        saidas.Add(s);
                    }
                }
            }

            var tarefas = new[]
            {
                Task.Run(() =>
                {
                    largada.Wait();
                    Guardar(controle.Resolver(Ana, agora));
                }),
                Task.Run(() =>
                {
                    largada.Wait();
                    controle.Voltar(Ana, agora, out var s);
                    Guardar(s);
                }),
                Task.Run(() =>
                {
                    largada.Wait();
                    controle.Interromper(Beto, agora, out var s);
                    Guardar(s);
                }),
                Task.Run(() =>
                {
                    largada.Wait();
                    controle.Comecar(Caio, Ana, agora, Perto, out var e);
                    foreach (var s in e.Saidas)
                    {
                        Guardar(s);
                    }
                }),
            };
            largada.Set();
            await Task.WhenAll(tarefas);

            Assert.Single(saidas);
            Assert.False(controle.EstaCaido(Ana));
        }
    }

    [Fact]
    public void Aviso_de_saida_acontece_dentro_da_trava_uma_vez_por_queda()
    {
        var avisados = new List<long>();
        var controle = new ControleDeQueda(aoSair: avisados.Add);
        controle.Cair(Ana, Inicio, out _);

        controle.Resolver(Ana, Inicio + 45_000);
        controle.Resolver(Ana, Inicio + 45_001);
        controle.Voltar(Ana, Inicio + 46_000, out _);

        Assert.Equal(new[] { Ana }, avisados);
    }

    [Fact]
    public void Pode_cair_de_novo_depois_de_levantar()
    {
        AnaCai();
        Levantar(Beto, Ana, Inicio + 100);
        Assert.True(_controle.Resolver(Ana, Inicio + 3_100).Houve);

        Assert.True(_controle.Cair(Ana, Inicio + 10_000, out _));
        Assert.Equal(45_000, _controle.Situacao(Ana, Inicio + 10_000).RestanteMs);
    }
}
