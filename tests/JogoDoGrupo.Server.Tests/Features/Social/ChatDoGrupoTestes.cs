using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Tests.Features.Social;

/// <summary>O canal Grupo do chat: quem recebe é quem a lista do grupo diz que está online.</summary>
public class ChatDoGrupoTestes
{
    private const long Ana = 1;
    private const long Bia = 2;
    private const long Caio = 3;
    private const long Davi = 4;

    private readonly RelogioFalso _relogio = new();
    private readonly DiretorioDoChatFalso _diretorio = new();
    private readonly Dictionary<long, ListaDoGrupo> _grupos = [];
    private readonly ChatDoServidor _chat;

    public ChatDoGrupoTestes()
    {
        _diretorio.Adicionar(Ana, "Ana");
        _diretorio.Adicionar(Bia, "Bia");
        _diretorio.Adicionar(Caio, "Caio");
        _diretorio.Adicionar(Davi, "Davi");
        _chat = new ChatDoServidor(
            new LimitesDoChat(_relogio),
            _diretorio,
            new PosicoesFalsas(),
            _relogio
        );
    }

    /// <summary>Monta um grupo; cada membro passa a ver a mesma lista.</summary>
    private void Agrupar(params (long Id, bool Online)[] membros)
    {
        var lista = new ListaDoGrupo
        {
            GrupoId = 10,
            LiderId = membros[0].Id,
            Membros = membros
                .Select(m => new MembroDoGrupo { PersonagemId = m.Id, Online = m.Online })
                .ToArray(),
        };
        foreach (var (id, _) in membros)
        {
            _grupos[id] = lista;
        }
    }

    private ResultadoDoChat FalarNoGrupo(long quem, string texto = "oi, grupo") =>
        _chat.Processar(
            quem,
            new EnviarMensagemDeChat { Canal = CanalDeChat.Grupo, Texto = texto },
            _ => true,
            id => _grupos.GetValueOrDefault(id) ?? new ListaDoGrupo()
        );

    [Fact]
    public void Mensagem_no_grupo_chega_aos_membros_online_e_ao_remetente()
    {
        Agrupar((Ana, true), (Bia, true), (Caio, true));

        var resultado = FalarNoGrupo(Bia);

        Assert.Null(resultado.Recusa);
        Assert.Equal(new[] { Ana, Bia, Caio }, resultado.Destinatarios.Order());
        Assert.Equal(CanalDeChat.Grupo, resultado.Mensagem!.Canal);
        Assert.Equal(Bia, resultado.Mensagem.RemetenteId);
        Assert.Equal("Bia", resultado.Mensagem.NomeDoRemetente);
        Assert.Equal("", resultado.Mensagem.NomeDoAlvo);
        Assert.Equal("oi, grupo", resultado.Mensagem.Texto);
    }

    [Fact]
    public void Membro_offline_com_a_vaga_guardada_nao_recebe()
    {
        Agrupar((Ana, true), (Bia, false), (Caio, true));

        var resultado = FalarNoGrupo(Ana);

        Assert.Equal(new[] { Ana, Caio }, resultado.Destinatarios.Order());
    }

    [Fact]
    public void Quem_nao_e_do_grupo_nao_recebe()
    {
        Agrupar((Ana, true), (Bia, true));

        Assert.DoesNotContain(Davi, FalarNoGrupo(Ana).Destinatarios);
    }

    [Fact]
    public void Sem_grupo_e_nao_esta_em_grupo_e_ninguem_recebe()
    {
        Agrupar((Ana, true), (Bia, true));

        var resultado = FalarNoGrupo(Davi);

        Assert.Equal(CodigosErroSocial.NaoEstaEmGrupo, resultado.Recusa!.Codigo);
        Assert.Null(resultado.Mensagem);
        Assert.Empty(resultado.Destinatarios);
    }

    [Fact]
    public void Lista_de_outro_grupo_que_nao_tem_o_remetente_nao_serve()
    {
        Agrupar((Ana, true), (Bia, true));
        // Uma lista velha apontando para um grupo do qual ele já saiu.
        _grupos[Davi] = _grupos[Ana];

        Assert.Equal(CodigosErroSocial.NaoEstaEmGrupo, FalarNoGrupo(Davi).Recusa!.Codigo);
    }

    [Fact]
    public void Grupo_de_grupos_diferentes_nao_se_mistura()
    {
        Agrupar((Ana, true), (Bia, true));
        Agrupar((Caio, true), (Davi, true));

        Assert.Equal(new[] { Caio, Davi }, FalarNoGrupo(Caio).Destinatarios.Order());
    }

    [Fact]
    public void Limites_valem_no_canal_grupo()
    {
        Agrupar((Ana, true), (Bia, true));

        Assert.Equal(CodigosErroSocial.MensagemVazia, FalarNoGrupo(Ana, "   ").Recusa!.Codigo);
        Assert.Null(FalarNoGrupo(Ana, "bora").Recusa);
        Assert.Equal(CodigosErroSocial.MensagemRepetida, FalarNoGrupo(Ana, "BORA").Recusa!.Codigo);
    }
}
