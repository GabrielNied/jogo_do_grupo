using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public class LimitesDoChatTestes
{
    private const long Ana = 1;
    private const long Bia = 2;

    private readonly RelogioFalso _relogio = new();
    private readonly LimitesDoChat _limites;

    public LimitesDoChatTestes() => _limites = new LimitesDoChat(_relogio);

    /// <summary>Confere e, se passou, registra: o que o chat faz com uma mensagem entregue.</summary>
    private ResultadoDosLimites Mandar(
        long quem,
        string texto,
        CanalDeChat canal = CanalDeChat.Cidade
    )
    {
        var resultado = _limites.Conferir(quem, canal, texto);
        if (resultado.Passou)
        {
            _limites.Registrar(quem, canal, resultado.Texto);
        }

        return resultado;
    }

    /// <summary>Manda cinco mensagens diferentes, enchendo a janela de frequência.</summary>
    private void EncherAJanela(long quem, string prefixo)
    {
        for (var i = 0; i < RegrasChat.MensagensPorJanela; i++)
        {
            Assert.True(Mandar(quem, $"{prefixo} {i}").Passou);
        }
    }

    // ── Texto ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Mensagem_normal_passa_e_devolve_o_texto_limpo()
    {
        var resultado = Mandar(Ana, "  oi\ngente  ");
        Assert.True(resultado.Passou);
        Assert.Null(resultado.Codigo);
        Assert.Equal("oi gente", resultado.Texto);
        Assert.Equal(0, resultado.SegundosRestantes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    [InlineData("\n\t")]
    public void So_espacos_e_mensagem_vazia(string texto) =>
        Assert.Equal(CodigosErroSocial.MensagemVazia, Mandar(Ana, texto).Codigo);

    [Fact]
    public void Texto_nulo_e_mensagem_vazia() =>
        Assert.Equal(
            CodigosErroSocial.MensagemVazia,
            _limites.Conferir(Ana, CanalDeChat.Cidade, null).Codigo
        );

    [Fact]
    public void Duzentos_caracteres_passam_e_duzentos_e_um_nao()
    {
        Assert.True(Mandar(Ana, new string('a', 200)).Passou);
        Assert.Equal(
            CodigosErroSocial.MensagemLongaDemais,
            Mandar(Ana, new string('b', 201)).Codigo
        );
    }

    // ── Frequência ────────────────────────────────────────────────────────────

    [Fact]
    public void Quinta_mensagem_em_10_s_passa_e_a_sexta_e_recusada()
    {
        EncherAJanela(Ana, "msg");
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "sexta").Codigo);
    }

    [Fact]
    public void Passados_os_10_s_volta_a_aceitar()
    {
        EncherAJanela(Ana, "msg");
        _relogio.Avancar(9.9);
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "cedo").Codigo);
        _relogio.Avancar(0.2);
        Assert.True(Mandar(Ana, "agora sim").Passou);
    }

    [Fact]
    public void Frequencia_soma_os_canais()
    {
        Assert.True(Mandar(Ana, "a", CanalDeChat.Cidade).Passou);
        Assert.True(Mandar(Ana, "b", CanalDeChat.Grupo).Passou);
        Assert.True(Mandar(Ana, "c", CanalDeChat.Sussurro).Passou);
        Assert.True(Mandar(Ana, "d", CanalDeChat.Cidade).Passou);
        Assert.True(Mandar(Ana, "e", CanalDeChat.Grupo).Passou);
        Assert.Equal(
            CodigosErroSocial.MensagensRapidasDemais,
            Mandar(Ana, "f", CanalDeChat.Sussurro).Codigo
        );
    }

    [Fact]
    public void Frequencia_e_por_personagem()
    {
        EncherAJanela(Ana, "msg");
        Assert.True(Mandar(Bia, "oi").Passou);
    }

    [Fact]
    public void Conferir_sem_registrar_nao_conta_na_frequencia()
    {
        for (var i = 0; i < 20; i++)
        {
            Assert.True(_limites.Conferir(Ana, CanalDeChat.Cidade, $"tentativa {i}").Passou);
        }
    }

    [Fact]
    public void Mensagem_recusada_pelo_texto_nao_conta_na_frequencia()
    {
        for (var i = 0; i < 20; i++)
        {
            Mandar(Ana, "");
        }

        Assert.True(Mandar(Ana, "oi").Passou);
    }

    // ── Repetição ─────────────────────────────────────────────────────────────

    [Fact]
    public void Mesma_mensagem_no_mesmo_canal_com_29_s_e_recusada_e_com_30_s_passa()
    {
        Assert.True(Mandar(Ana, "vendo espada").Passou);
        _relogio.Avancar(29);
        Assert.Equal(CodigosErroSocial.MensagemRepetida, Mandar(Ana, "vendo espada").Codigo);
        _relogio.Avancar(1);
        Assert.True(Mandar(Ana, "vendo espada").Passou);
    }

    [Fact]
    public void Repeticao_ignora_maiusculas_e_espacos()
    {
        Assert.True(Mandar(Ana, "Vendo Espada").Passou);
        Assert.Equal(CodigosErroSocial.MensagemRepetida, Mandar(Ana, "vendo  ESPADA ").Codigo);
    }

    [Fact]
    public void Mesma_mensagem_em_outro_canal_ou_de_outro_personagem_nao_e_repeticao()
    {
        Assert.True(Mandar(Ana, "oi", CanalDeChat.Cidade).Passou);
        Assert.True(Mandar(Ana, "oi", CanalDeChat.Sussurro).Passou);
        Assert.True(Mandar(Bia, "oi", CanalDeChat.Cidade).Passou);
    }

    [Fact]
    public void Mensagem_diferente_entre_duas_iguais_libera_a_repeticao()
    {
        Assert.True(Mandar(Ana, "oi").Passou);
        Assert.True(Mandar(Ana, "tudo bem?").Passou);
        Assert.True(Mandar(Ana, "oi").Passou);
    }

    // ── Silêncio automático ───────────────────────────────────────────────────

    [Fact]
    public void Terceiro_estouro_silencia_por_60_s_em_todos_os_canais_e_depois_libera()
    {
        EncherAJanela(Ana, "msg");
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "um").Codigo);
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "dois").Codigo);

        var terceiro = Mandar(Ana, "tres");
        Assert.Equal(CodigosErroSocial.Silenciado, terceiro.Codigo);
        Assert.Equal(RegrasChat.SilencioSegundos, terceiro.SegundosRestantes);

        _relogio.Avancar(20);
        var durante = Mandar(Ana, "oi", CanalDeChat.Sussurro);
        Assert.Equal(CodigosErroSocial.Silenciado, durante.Codigo);
        Assert.Equal(40, durante.SegundosRestantes);

        _relogio.Avancar(39.5);
        Assert.Equal(1, Mandar(Ana, "quase").SegundosRestantes);

        _relogio.Avancar(0.5);
        Assert.True(Mandar(Ana, "voltei").Passou);
    }

    [Fact]
    public void Estouros_com_mais_de_5_minutos_nao_contam_para_o_silencio()
    {
        EncherAJanela(Ana, "a");
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "um").Codigo);
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "dois").Codigo);

        _relogio.Avancar(RegrasChat.JanelaDeEstourosSegundos);
        EncherAJanela(Ana, "b");
        Assert.Equal(CodigosErroSocial.MensagensRapidasDemais, Mandar(Ana, "tres").Codigo);
    }

    [Fact]
    public void Silencio_de_um_personagem_nao_afeta_outro()
    {
        EncherAJanela(Ana, "msg");
        Mandar(Ana, "um");
        Mandar(Ana, "dois");
        Assert.Equal(CodigosErroSocial.Silenciado, Mandar(Ana, "tres").Codigo);
        Assert.True(Mandar(Bia, "oi").Passou);
    }

    // ── Memória ───────────────────────────────────────────────────────────────

    [Fact]
    public void Esquecer_solta_quem_nao_tem_nada_pendente()
    {
        Mandar(Ana, "oi");
        _limites.Esquecer(Ana);
        Assert.Equal(1, _limites.PersonagensLembrados);

        _relogio.Avancar(RegrasChat.JanelaDeFrequenciaSegundos);
        _limites.Esquecer(Ana);
        Assert.Equal(0, _limites.PersonagensLembrados);
    }

    [Fact]
    public void Sair_e_voltar_nao_zera_o_silencio()
    {
        EncherAJanela(Ana, "msg");
        Mandar(Ana, "um");
        Mandar(Ana, "dois");
        Mandar(Ana, "tres");

        _relogio.Avancar(15);
        _limites.Esquecer(Ana);

        Assert.Equal(CodigosErroSocial.Silenciado, Mandar(Ana, "voltei").Codigo);
    }

    [Fact]
    public void Esquecer_quem_nunca_falou_nao_faz_nada()
    {
        _limites.Esquecer(Ana);
        Assert.Equal(0, _limites.PersonagensLembrados);
    }

    // ── Concorrência ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Mensagens_simultaneas_do_mesmo_personagem_contam_todas()
    {
        var tarefas = Enumerable
            .Range(0, 40)
            .Select(i =>
                Task.Run(() =>
                {
                    lock (_limites)
                    {
                        return Mandar(Ana, $"mensagem {i}").Passou;
                    }
                })
            );
        var passaram = (await Task.WhenAll(tarefas)).Count(passou => passou);
        Assert.Equal(RegrasChat.MensagensPorJanela, passaram);
    }
}
