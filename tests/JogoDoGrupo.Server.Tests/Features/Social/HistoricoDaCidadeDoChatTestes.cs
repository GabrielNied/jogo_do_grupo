using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Server.Tests.Features.Combate;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public class HistoricoDaCidadeDoChatTestes
{
    private readonly RelogioFalso _relogio = new();
    private readonly HistoricoDaCidadeDoChat _historico;

    public HistoricoDaCidadeDoChatTestes() => _historico = new HistoricoDaCidadeDoChat(_relogio);

    /// <summary>Guarda uma mensagem carimbada com a hora atual do relógio do teste.</summary>
    private void Guardar(string texto) =>
        _historico.Guardar(
            new MensagemDeChat
            {
                Canal = CanalDeChat.Cidade,
                Texto = texto,
                HoraUnixMs = _relogio.GetUtcNow().ToUnixTimeMilliseconds(),
            }
        );

    private string[] Textos() => _historico.Atual().Select(m => m.Texto).ToArray();

    [Fact]
    public void Comeca_vazio() => Assert.Empty(_historico.Atual());

    [Fact]
    public void Entrega_da_mais_antiga_para_a_mais_nova()
    {
        Guardar("um");
        _relogio.Avancar(5);
        Guardar("dois");
        _relogio.Avancar(5);
        Guardar("tres");

        Assert.Equal(new[] { "um", "dois", "tres" }, Textos());
    }

    [Fact]
    public void Nunca_passa_de_20_e_fica_com_as_mais_novas()
    {
        for (var i = 1; i <= RegrasChat.HistoricoDaCidadeMensagens + 5; i++)
        {
            Guardar($"mensagem {i}");
        }

        var textos = Textos();
        Assert.Equal(RegrasChat.HistoricoDaCidadeMensagens, textos.Length);
        Assert.Equal("mensagem 6", textos[0]);
        Assert.Equal("mensagem 25", textos[^1]);
    }

    [Fact]
    public void Mensagem_com_exatamente_10_minutos_ainda_e_entregue_e_depois_nao()
    {
        Guardar("antiga");

        _relogio.Avancar(RegrasChat.HistoricoDaCidadeSegundos);
        Assert.Equal(new[] { "antiga" }, Textos());

        _relogio.Avancar(1);
        Assert.Empty(_historico.Atual());
    }

    [Fact]
    public void So_as_velhas_saem_e_as_recentes_ficam()
    {
        Guardar("velha");
        _relogio.Avancar(RegrasChat.HistoricoDaCidadeSegundos - 10);
        Guardar("recente");
        _relogio.Avancar(20);

        Assert.Equal(new[] { "recente" }, Textos());
    }

    [Fact]
    public void Quem_recebe_a_lista_nao_muda_o_historico()
    {
        Guardar("um");
        var entregue = _historico.Atual();
        entregue[0] = new MensagemDeChat { Texto = "trocada" };

        Assert.Equal(new[] { "um" }, Textos());
    }

    [Fact]
    public async Task Guardar_de_varias_threads_nao_perde_nem_passa_do_limite()
    {
        var tarefas = Enumerable
            .Range(0, 200)
            .Select(i => Task.Run(() => Guardar($"mensagem {i}")));
        await Task.WhenAll(tarefas);

        Assert.Equal(RegrasChat.HistoricoDaCidadeMensagens, _historico.Atual().Length);
    }
}
