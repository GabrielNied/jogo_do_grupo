using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>Diretório de mentira: os personagens que o teste cadastrar.</summary>
public sealed class DiretorioDoChatFalso : IDiretorioDePersonagens
{
    private readonly Dictionary<long, string> _nomes = [];

    public void Adicionar(long id, string nome) => _nomes[id] = nome;

    public void Remover(long id) => _nomes.Remove(id);

    public bool TentarObterPorNome(string nome, out long personagemId)
    {
        foreach (var (id, existente) in _nomes)
        {
            if (string.Equals(existente, nome, StringComparison.OrdinalIgnoreCase))
            {
                personagemId = id;
                return true;
            }
        }

        personagemId = 0;
        return false;
    }

    public bool TentarObterNome(long personagemId, out string nome)
    {
        if (_nomes.TryGetValue(personagemId, out var achado))
        {
            nome = achado;
            return true;
        }

        nome = "";
        return false;
    }

    public bool TentarObterRaca(long personagemId, out int raca)
    {
        raca = 1;
        return true;
    }

    public bool TentarObterContaDoPersonagem(long personagemId, out long contaId)
    {
        contaId = personagemId;
        return _nomes.ContainsKey(personagemId);
    }
}

/// <summary>Posições de mentira: quem o teste puser na cidade.</summary>
public sealed class PosicoesFalsas : IPosicoesDoMundo
{
    public List<long> NaCidade { get; } = [];

    /// <summary>Quem está na cidade mas fora dos muros (no terreno em volta).</summary>
    public HashSet<long> ForaDosMuros { get; } = [];

    public bool TentarObter(long personagemId, out PosicaoNoMundo posicao)
    {
        posicao = new PosicaoNoMundo(
            AreasDoMundo.Cidade,
            ForaDosMuros.Contains(personagemId) ? 500 : 0,
            0,
            0,
            0
        );
        return NaCidade.Contains(personagemId);
    }

    public IReadOnlyList<long> PersonagensNaArea(string area) =>
        area == AreasDoMundo.Cidade ? NaCidade.ToArray() : [];
}

public class ChatDoServidorTestes
{
    private const long Ana = 1;
    private const long Bia = 2;
    private const long Caio = 3;

    private readonly RelogioFalso _relogio = new();
    private readonly DiretorioDoChatFalso _diretorio = new();
    private readonly PosicoesFalsas _posicoes = new();
    private readonly HashSet<long> _online = [Ana, Bia, Caio];
    private readonly ChatDoServidor _chat;

    public ChatDoServidorTestes()
    {
        _diretorio.Adicionar(Ana, "Ana");
        _diretorio.Adicionar(Bia, "Bia");
        _diretorio.Adicionar(Caio, "Caio");
        _chat = new ChatDoServidor(new LimitesDoChat(_relogio), _diretorio, _posicoes, _relogio);
    }

    private ResultadoDoChat Enviar(
        long quem,
        CanalDeChat canal,
        string texto,
        string nomeDoAlvo = ""
    ) =>
        _chat.Processar(
            quem,
            new EnviarMensagemDeChat
            {
                Canal = canal,
                Texto = texto,
                NomeDoAlvo = nomeDoAlvo,
            },
            _online.Contains
        );

    private static void AssertRecusada(string codigo, ResultadoDoChat resultado)
    {
        Assert.NotNull(resultado.Recusa);
        Assert.Equal(codigo, resultado.Recusa.Codigo);
        Assert.Null(resultado.Mensagem);
        Assert.Empty(resultado.Destinatarios);
    }

    // ── Cidade ────────────────────────────────────────────────────────────────

    [Fact]
    public void Mensagem_na_cidade_chega_a_todos_que_estao_nela_e_ao_remetente()
    {
        _posicoes.NaCidade.AddRange([Ana, Bia]);

        var resultado = Enviar(Ana, CanalDeChat.Cidade, "  bom dia  ");

        Assert.Null(resultado.Recusa);
        Assert.Equal(new[] { Ana, Bia }, resultado.Destinatarios.Order());
        var mensagem = resultado.Mensagem!;
        Assert.Equal(CanalDeChat.Cidade, mensagem.Canal);
        Assert.Equal(Ana, mensagem.RemetenteId);
        Assert.Equal("Ana", mensagem.NomeDoRemetente);
        Assert.Equal("", mensagem.NomeDoAlvo);
        Assert.Equal("bom dia", mensagem.Texto);
        Assert.Equal(_relogio.GetUtcNow().ToUnixTimeMilliseconds(), mensagem.HoraUnixMs);
    }

    [Fact]
    public void Canal_cidade_nao_chega_a_quem_esta_fora_dos_muros()
    {
        _posicoes.NaCidade.AddRange([Ana, Bia, Caio]);
        _posicoes.ForaDosMuros.Add(Caio);

        var resultado = Enviar(Ana, CanalDeChat.Cidade, "bom dia");

        Assert.Null(resultado.Recusa);
        Assert.Equal(new[] { Ana, Bia }, resultado.Destinatarios.Order());
    }

    [Fact]
    public void Quem_fala_de_fora_dos_muros_recebe_canal_indisponivel()
    {
        _posicoes.NaCidade.AddRange([Ana, Bia]);
        _posicoes.ForaDosMuros.Add(Ana);

        AssertRecusada(CodigosErroSocial.CanalIndisponivel, Enviar(Ana, CanalDeChat.Cidade, "oi"));
    }

    [Fact]
    public void Quem_nao_esta_na_cidade_nao_recebe_o_canal_cidade()
    {
        _posicoes.NaCidade.AddRange([Ana, Bia]);
        var resultado = Enviar(Ana, CanalDeChat.Cidade, "oi");
        Assert.DoesNotContain(Caio, resultado.Destinatarios);
    }

    [Fact]
    public void Falar_na_cidade_fora_dela_e_canal_indisponivel()
    {
        _posicoes.NaCidade.Add(Bia);
        AssertRecusada(CodigosErroSocial.CanalIndisponivel, Enviar(Ana, CanalDeChat.Cidade, "oi"));
    }

    [Fact]
    public void Canal_que_nao_existe_e_canal_indisponivel()
    {
        _posicoes.NaCidade.Add(Ana);
        AssertRecusada(CodigosErroSocial.CanalIndisponivel, Enviar(Ana, (CanalDeChat)99, "oi"));
    }

    // ── Grupo ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Falar_no_grupo_sem_grupo_e_nao_esta_em_grupo() =>
        AssertRecusada(CodigosErroSocial.NaoEstaEmGrupo, Enviar(Ana, CanalDeChat.Grupo, "oi"));

    // ── Sussurro ──────────────────────────────────────────────────────────────

    [Fact]
    public void Sussurro_chega_so_ao_alvo_e_ao_remetente()
    {
        var resultado = Enviar(Ana, CanalDeChat.Sussurro, "segredo", "Bia");

        Assert.Null(resultado.Recusa);
        Assert.Equal(new[] { Ana, Bia }, resultado.Destinatarios.Order());
        Assert.Equal("Ana", resultado.Mensagem!.NomeDoRemetente);
        Assert.Equal("Bia", resultado.Mensagem.NomeDoAlvo);
        Assert.Equal("segredo", resultado.Mensagem.Texto);
    }

    [Fact]
    public void Sussurro_acha_o_nome_com_maiusculas_diferentes_e_devolve_o_nome_certo()
    {
        var resultado = Enviar(Ana, CanalDeChat.Sussurro, "oi", "bIA");
        Assert.Null(resultado.Recusa);
        Assert.Equal("Bia", resultado.Mensagem!.NomeDoAlvo);
    }

    [Fact]
    public void Sussurro_funciona_entre_areas()
    {
        _posicoes.NaCidade.Add(Ana);
        Assert.Null(Enviar(Ana, CanalDeChat.Sussurro, "oi", "Bia").Recusa);
    }

    [Fact]
    public void Sussurro_para_nome_inexistente_e_alvo_nao_encontrado() =>
        AssertRecusada(
            CodigosErroSocial.AlvoNaoEncontrado,
            Enviar(Ana, CanalDeChat.Sussurro, "oi", "Ninguem")
        );

    [Fact]
    public void Sussurro_para_personagem_offline_responde_igual_a_nome_inexistente()
    {
        _online.Remove(Bia);
        AssertRecusada(
            CodigosErroSocial.AlvoNaoEncontrado,
            Enviar(Ana, CanalDeChat.Sussurro, "oi", "Bia")
        );
    }

    [Fact]
    public void Sussurro_sem_nome_de_alvo_e_alvo_nao_encontrado() =>
        AssertRecusada(
            CodigosErroSocial.AlvoNaoEncontrado,
            Enviar(Ana, CanalDeChat.Sussurro, "oi")
        );

    [Fact]
    public void Sussurro_para_si_mesmo_e_recusado() =>
        AssertRecusada(
            CodigosErroSocial.SussurroParaSiMesmo,
            Enviar(Ana, CanalDeChat.Sussurro, "oi", "ana")
        );

    // ── Identidade ────────────────────────────────────────────────────────────

    [Fact]
    public void Nome_do_remetente_vem_do_servidor_mesmo_que_o_client_mande_outro()
    {
        // O pedido não tem campo de remetente; o "nome" só pode vir do diretório.
        var resultado = Enviar(Caio, CanalDeChat.Sussurro, "sou a Ana, confia", "Bia");
        Assert.Equal(Caio, resultado.Mensagem!.RemetenteId);
        Assert.Equal("Caio", resultado.Mensagem.NomeDoRemetente);
    }

    [Fact]
    public void Sessao_de_personagem_que_nao_existe_mais_nao_fala()
    {
        _posicoes.NaCidade.Add(Ana);
        _diretorio.Remover(Ana);
        AssertRecusada(CodigosErroSocial.CanalIndisponivel, Enviar(Ana, CanalDeChat.Cidade, "oi"));
    }

    // ── Limites aplicados pelo chat ───────────────────────────────────────────

    [Theory]
    [InlineData("", CodigosErroSocial.MensagemVazia)]
    [InlineData("   ", CodigosErroSocial.MensagemVazia)]
    public void Texto_invalido_e_recusado_antes_de_olhar_o_canal(string texto, string codigo) =>
        AssertRecusada(codigo, Enviar(Ana, CanalDeChat.Sussurro, texto, "Ninguem"));

    [Fact]
    public void Texto_longo_demais_e_recusado() =>
        AssertRecusada(
            CodigosErroSocial.MensagemLongaDemais,
            Enviar(Ana, CanalDeChat.Sussurro, new string('a', 201), "Bia")
        );

    [Fact]
    public void Sexta_mensagem_em_10_s_e_recusada_e_ninguem_recebe()
    {
        _posicoes.NaCidade.AddRange([Ana, Bia]);
        for (var i = 0; i < RegrasChat.MensagensPorJanela; i++)
        {
            Assert.Null(Enviar(Ana, CanalDeChat.Cidade, $"mensagem {i}").Recusa);
        }

        AssertRecusada(
            CodigosErroSocial.MensagensRapidasDemais,
            Enviar(Ana, CanalDeChat.Cidade, "mais uma")
        );
    }

    [Fact]
    public void Mensagem_repetida_e_recusada()
    {
        _posicoes.NaCidade.Add(Ana);
        Assert.Null(Enviar(Ana, CanalDeChat.Cidade, "vendo espada").Recusa);
        AssertRecusada(
            CodigosErroSocial.MensagemRepetida,
            Enviar(Ana, CanalDeChat.Cidade, "VENDO espada")
        );
    }

    [Fact]
    public void Mensagem_recusada_pelo_canal_ou_pelo_alvo_nao_conta_na_frequencia()
    {
        for (var i = 0; i < 10; i++)
        {
            AssertRecusada(
                CodigosErroSocial.AlvoNaoEncontrado,
                Enviar(Ana, CanalDeChat.Sussurro, $"oi {i}", "Ninguem")
            );
            AssertRecusada(
                CodigosErroSocial.CanalIndisponivel,
                Enviar(Ana, CanalDeChat.Cidade, $"ola {i}")
            );
        }

        Assert.Null(Enviar(Ana, CanalDeChat.Sussurro, "oi", "Bia").Recusa);
    }

    [Fact]
    public void Silenciado_recebe_os_segundos_que_faltam()
    {
        _posicoes.NaCidade.Add(Ana);
        for (var i = 0; i < RegrasChat.MensagensPorJanela; i++)
        {
            Enviar(Ana, CanalDeChat.Cidade, $"mensagem {i}");
        }

        Enviar(Ana, CanalDeChat.Cidade, "estouro um");
        Enviar(Ana, CanalDeChat.Cidade, "estouro dois");
        Enviar(Ana, CanalDeChat.Cidade, "estouro tres");
        _relogio.Avancar(10);

        var resultado = Enviar(Ana, CanalDeChat.Sussurro, "oi", "Bia");
        AssertRecusada(CodigosErroSocial.Silenciado, resultado);
        Assert.Equal(50, resultado.Recusa!.SegundosRestantes);
    }

    [Fact]
    public void Mensagem_na_guilda_chega_aos_membros_online_e_remetente()
    {
        var online = new HashSet<long> { Ana, Bia };
        var membrosGuilda = new List<long> { Ana, Bia, Caio }; // Caio offline

        var resultado = _chat.Processar(
            Ana,
            new EnviarMensagemDeChat { Canal = CanalDeChat.Guilda, Texto = "Ola guilda" },
            online.Contains,
            guildaDe: id => membrosGuilda.Contains(id) ? membrosGuilda : []
        );

        Assert.Null(resultado.Recusa);
        Assert.Equal([Ana, Bia], resultado.Destinatarios.OrderBy(x => x));
    }

    [Fact]
    public void Mensagem_na_guilda_sem_pertencer_a_guilda_recusa()
    {
        var resultado = _chat.Processar(
            Ana,
            new EnviarMensagemDeChat { Canal = CanalDeChat.Guilda, Texto = "Ola guilda" },
            _ => true,
            guildaDe: _ => []
        );

        AssertRecusada(CodigosErroSocial.NaoEstaEmGuilda, resultado);
    }
}
