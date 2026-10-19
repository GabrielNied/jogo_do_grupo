using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Shared.Tests.Features.Social;

public class RegrasChatTestes
{
    // ── Contrato: IDs, canais, códigos e campos padrão ────────────────────────

    [Fact]
    public void Ids_do_chat_tem_os_numeros_do_contrato_e_estao_na_faixa()
    {
        Assert.Equal(13100, SocialMessageIds.EnviarMensagemDeChat);
        Assert.Equal(13101, SocialMessageIds.MensagemDeChat);
        Assert.Equal(13102, SocialMessageIds.MensagemDeChatRecusada);
        Assert.Equal(13103, SocialMessageIds.HistoricoDaCidade);
        Assert.True(SocialMessageIds.Faixa.Contem(SocialMessageIds.EnviarMensagemDeChat));
        Assert.True(SocialMessageIds.Faixa.Contem(SocialMessageIds.HistoricoDaCidade));
    }

    [Fact]
    public void Ids_do_chat_ficam_depois_dos_do_grupo() =>
        Assert.True(
            SocialMessageIds.EnviarMensagemDeChat > SocialMessageIds.OperacaoDoGrupoRecusada
        );

    [Fact]
    public void CanalDeChat_tem_valores_estaveis()
    {
        Assert.Equal(0, (int)CanalDeChat.Cidade);
        Assert.Equal(1, (int)CanalDeChat.Grupo);
        Assert.Equal(2, (int)CanalDeChat.Sussurro);
        Assert.Equal(3, (int)CanalDeChat.ProcurarGrupo);
        Assert.Equal(4, (int)CanalDeChat.Guilda);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(-1, false)]
    public void CanalValido_so_aceita_canais_conhecidos(int valor, bool esperado) =>
        Assert.Equal(esperado, RegrasChat.CanalValido((CanalDeChat)valor));

    [Fact]
    public void Codigos_do_chat_tem_o_texto_do_contrato_e_sao_distintos()
    {
        var codigos = new[]
        {
            CodigosErroSocial.MensagemVazia,
            CodigosErroSocial.MensagemLongaDemais,
            CodigosErroSocial.MensagensRapidasDemais,
            CodigosErroSocial.MensagemRepetida,
            CodigosErroSocial.Silenciado,
            CodigosErroSocial.CanalIndisponivel,
            CodigosErroSocial.SussurroParaSiMesmo,
            CodigosErroSocial.NaoEstaEmGrupo,
            CodigosErroSocial.AlvoNaoEncontrado,
        };
        Assert.Equal(codigos.Length, codigos.Distinct().Count());
        Assert.Equal("MensagemVazia", CodigosErroSocial.MensagemVazia);
        Assert.Equal("Silenciado", CodigosErroSocial.Silenciado);
        Assert.Equal("SussurroParaSiMesmo", CodigosErroSocial.SussurroParaSiMesmo);
    }

    [Fact]
    public void Mensagens_do_chat_campos_padrao_nao_nulos()
    {
        var enviar = new EnviarMensagemDeChat();
        Assert.Equal(CanalDeChat.Cidade, enviar.Canal);
        Assert.Equal("", enviar.Texto);
        Assert.Equal("", enviar.NomeDoAlvo);

        var mensagem = new MensagemDeChat();
        Assert.Equal(0L, mensagem.RemetenteId);
        Assert.Equal("", mensagem.NomeDoRemetente);
        Assert.Equal("", mensagem.NomeDoAlvo);
        Assert.Equal("", mensagem.Texto);
        Assert.Equal(0L, mensagem.HoraUnixMs);

        var recusada = new MensagemDeChatRecusada();
        Assert.Equal("", recusada.Codigo);
        Assert.Equal(0, recusada.SegundosRestantes);

        var historico = new HistoricoDaCidade();
        Assert.NotNull(historico.Mensagens);
        Assert.Empty(historico.Mensagens);
    }

    // ── Valores padrão (tabela de limites de social.md) ───────────────────────

    [Fact]
    public void Valores_padrao_do_chat()
    {
        Assert.Equal(200, RegrasChat.TamanhoMaximo);
        Assert.Equal(5, RegrasChat.MensagensPorJanela);
        Assert.Equal(10, RegrasChat.JanelaDeFrequenciaSegundos);
        Assert.Equal(30, RegrasChat.IntervaloDeRepeticaoSegundos);
        Assert.Equal(3, RegrasChat.EstourosParaSilenciar);
        Assert.Equal(300, RegrasChat.JanelaDeEstourosSegundos);
        Assert.Equal(60, RegrasChat.SilencioSegundos);
        Assert.Equal(20, RegrasChat.HistoricoDaCidadeMensagens);
        Assert.Equal(600, RegrasChat.HistoricoDaCidadeSegundos);
    }

    // ── Limpar ────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("oi", "oi")]
    [InlineData("  oi gente  ", "oi gente")]
    [InlineData("linha um\nlinha dois", "linha um linha dois")]
    [InlineData("a\r\nb", "a  b")]
    [InlineData("a\tb", "a b")]
    [InlineData("\n\n oi \n", "oi")]
    [InlineData("a\u2028b\u2029c", "a b c")]
    [InlineData("a\u0000b", "a b")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Limpar_troca_quebras_e_controles_por_espaco(string texto, string esperado) =>
        Assert.Equal(esperado, RegrasChat.Limpar(texto));

    [Fact]
    public void Limpar_nulo_vira_vazio() => Assert.Equal("", RegrasChat.Limpar(null));

    [Fact]
    public void Limpar_mantem_acentos_numeros_pontuacao_e_marcas_de_formatacao()
    {
        const string texto = "Olá, São João! 123 <b>negrito</b> ção?";
        Assert.Equal(texto, RegrasChat.Limpar(texto));
    }

    // ── Validar ───────────────────────────────────────────────────────────────

    [Fact]
    public void Texto_de_1_a_200_caracteres_e_aceito()
    {
        Assert.Null(RegrasChat.Validar("a"));
        Assert.Null(RegrasChat.Validar(new string('a', 200)));
    }

    [Fact]
    public void Texto_com_201_caracteres_e_longo_demais() =>
        Assert.Equal(
            CodigosErroSocial.MensagemLongaDemais,
            RegrasChat.Validar(new string('a', 201))
        );

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\n\t\r")]
    public void So_espacos_e_mensagem_vazia(string texto) =>
        Assert.Equal(CodigosErroSocial.MensagemVazia, RegrasChat.Validar(texto));

    [Fact]
    public void Nulo_e_mensagem_vazia() =>
        Assert.Equal(CodigosErroSocial.MensagemVazia, RegrasChat.Validar(null));

    [Fact]
    public void Tamanho_e_medido_depois_de_aparar() =>
        Assert.Null(RegrasChat.Validar("   " + new string('a', 200) + "   "));

    // ── Repetição ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Oi  gente", "oi gente")]
    [InlineData("OIGENTE", "oi gente")]
    [InlineData("  oi\ngente ", "oigente")]
    [InlineData("ÁGUA", "água")]
    public void ChaveDeRepeticao_ignora_maiusculas_e_espacos(string a, string b) =>
        Assert.Equal(RegrasChat.ChaveDeRepeticao(a), RegrasChat.ChaveDeRepeticao(b));

    [Fact]
    public void ChaveDeRepeticao_diferencia_textos_diferentes() =>
        Assert.NotEqual(RegrasChat.ChaveDeRepeticao("oi"), RegrasChat.ChaveDeRepeticao("oi!"));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(29.0, true)]
    [InlineData(29.9, true)]
    [InlineData(30.0, false)]
    public void Repeticao_com_29_s_e_recusada_e_com_30_s_passa(double segundos, bool repetida) =>
        Assert.Equal(repetida, RegrasChat.Repetida(segundos));

    // ── Frequência e silêncio ─────────────────────────────────────────────────

    [Theory]
    [InlineData(0, false)]
    [InlineData(4, false)]
    [InlineData(5, true)]
    [InlineData(9, true)]
    public void Quinta_mensagem_passa_e_a_sexta_estoura(int aceitasNaJanela, bool estourou) =>
        Assert.Equal(estourou, RegrasChat.FrequenciaEstourou(aceitasNaJanela));

    [Theory]
    [InlineData(0.0, true)]
    [InlineData(9.9, true)]
    [InlineData(10.0, false)]
    public void Passados_10_s_a_mensagem_sai_da_janela(double segundos, bool conta) =>
        Assert.Equal(conta, RegrasChat.ContaNaFrequencia(segundos));

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    public void Terceiro_estouro_silencia(int estouros, bool silencia) =>
        Assert.Equal(silencia, RegrasChat.DeveSilenciar(estouros));

    [Theory]
    [InlineData(299.0, true)]
    [InlineData(300.0, false)]
    public void Estouro_conta_por_5_minutos(double segundos, bool conta) =>
        Assert.Equal(conta, RegrasChat.ContaComoEstouro(segundos));

    [Theory]
    [InlineData(0.0, 60)]
    [InlineData(0.2, 60)]
    [InlineData(1.0, 59)]
    [InlineData(59.5, 1)]
    [InlineData(60.0, 0)]
    [InlineData(600.0, 0)]
    [InlineData(-5.0, 60)]
    public void Silencio_dura_60_s_e_depois_libera(double segundos, int restante) =>
        Assert.Equal(restante, RegrasChat.SilencioRestanteSegundos(segundos));

    // ── Histórico da Cidade ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0.0, false)]
    [InlineData(600.0, false)]
    [InlineData(600.1, true)]
    public void Historico_nao_entrega_mensagem_de_mais_de_10_min(double segundos, bool velha) =>
        Assert.Equal(velha, RegrasChat.VelhaDemaisParaOHistorico(segundos));
}
