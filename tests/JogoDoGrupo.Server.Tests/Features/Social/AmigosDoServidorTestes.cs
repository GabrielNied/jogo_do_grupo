using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Social;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Social;

public sealed class AmigosDoServidorTestes
{
    private readonly AmbienteDeAmigos _a = new();

    [Fact]
    public void Pedido_duplicado_recusa_PedidoJaPendente()
    {
        _a.Amigos.PedirAmizade(AmbienteDeAmigos.A, "Bia");
        _a.Saida.Limpar();
        _a.Amigos.PedirAmizade(AmbienteDeAmigos.A, "Bia");
        Assert.Equal(
            CodigosErroSocial.PedidoJaPendente,
            _a.Saida.UltimaRecusaAmizade(AmbienteDeAmigos.A)
        );
    }

    [Fact]
    public void Aceite_cria_amizade_nos_dois_lados()
    {
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Saida.Limpar();
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, true);
        Assert.Single(_a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos);
        Assert.Single(_a.Saida.ListaAmigos(AmbienteDeAmigos.B).Amigos);
    }

    [Fact]
    public void Recusa_de_pedido_nao_avisa_quem_pediu()
    {
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Saida.Limpar();
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, false);
        Assert.Empty(_a.Saida.Todas.Where(e => e.Para == AmbienteDeAmigos.A));
    }

    [Fact]
    public void Pedidos_cruzados_viram_uma_amizade()
    {
        _a.Amigos.PedirAmizade(AmbienteDeAmigos.A, "Bia");
        _a.Saida.Limpar();
        _a.Amigos.PedirAmizade(AmbienteDeAmigos.B, "Ana");
        Assert.Single(_a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos);
        Assert.Single(_a.Saida.ListaAmigos(AmbienteDeAmigos.B).Amigos);
    }

    [Fact]
    public void Ignorado_recebe_AlvoNaoEncontrado_ao_pedir_ou_sussurrar()
    {
        _a.Amigos.Ignorar(AmbienteDeAmigos.B, "Ana");
        _a.Saida.Limpar();
        _a.Amigos.PedirAmizade(AmbienteDeAmigos.A, "Bia");
        Assert.Equal(
            CodigosErroSocial.AlvoNaoEncontrado,
            _a.Saida.UltimaRecusaAmizade(AmbienteDeAmigos.A)
        );

        var chat = new ChatDoServidor(
            new LimitesDoChat(TimeProvider.System),
            _a.Diretorio,
            new PosicoesFalsas(),
            TimeProvider.System
        );
        var sussurro = chat.Processar(
            AmbienteDeAmigos.A,
            new EnviarMensagemDeChat
            {
                Canal = CanalDeChat.Sussurro,
                NomeDoAlvo = "Bia",
                Texto = "oi",
            },
            _ => true,
            contatoBarradoEntrePersonagens: _a.Amigos.ContatoBarradoEntrePersonagens
        );
        Assert.Equal(CodigosErroSocial.AlvoNaoEncontrado, sussurro.Recusa!.Codigo);
    }

    [Fact]
    public void Ignorar_desfaz_amizade_e_pedidos()
    {
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, true);
        _a.Amigos.Ignorar(AmbienteDeAmigos.B, "Ana");
        Assert.Empty(_a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos);
        Assert.Empty(_a.Saida.ListaAmigos(AmbienteDeAmigos.B).Amigos);
    }

    [Fact]
    public void Online_muda_ao_trocar_personagem_da_conta()
    {
        const long biaAlt = 9;
        _a.Diretorio.Adicionar(biaAlt, "BiaAlt", contaId: 2);
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, true);
        _a.Saida.Limpar();
        _a.Amigos.RemoverOnline(2, AmbienteDeAmigos.B);
        _a.Amigos.RegistrarOnline(2, biaAlt);
        var mudanca = _a.Saida.De<AmigoMudouDeEstado>(AmbienteDeAmigos.A).Last();
        Assert.True(mudanca.Online);
        Assert.Equal("BiaAlt", mudanca.PersonagemOnline);
    }

    [Fact]
    public void Listas_voltam_apos_reiniciar_servico()
    {
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, true);
        var amigosAntes = _a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos.Length;
        var recarregado = _a.RecarregarAmigos();
        recarregado.RegistrarOnline(1, AmbienteDeAmigos.A);
        Assert.Equal(amigosAntes, _a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos.Length);
    }

    [Fact]
    public void Sem_carregar_antes_o_primeiro_uso_le_do_banco()
    {
        var pedidoId = _a.Pedir(AmbienteDeAmigos.A, AmbienteDeAmigos.B);
        _a.Amigos.ResponderPedido(AmbienteDeAmigos.B, pedidoId, true);
        _a.Saida.Limpar();

        var novo = _a.RecriarSemCarregar();
        novo.RegistrarOnline(1, AmbienteDeAmigos.A);

        Assert.Single(_a.Saida.ListaAmigos(AmbienteDeAmigos.A).Amigos);
    }
}

internal sealed class SaidaAmigosGravada : ISaidaAmigos, ISaidaDoGrupo
{
    private readonly List<(long Para, object Mensagem)> _enviadas = [];

    public IReadOnlyList<(long Para, object Mensagem)> Todas => [.. _enviadas];

    public void ListaDeAmigos(long personagemId, ListaDeAmigos lista) =>
        Gravar(personagemId, lista);

    public void ListaDeIgnorados(long personagemId, ListaDeIgnorados lista) =>
        Gravar(personagemId, lista);

    public void AmigoMudouDeEstado(long personagemId, AmigoMudouDeEstado mudanca) =>
        Gravar(personagemId, mudanca);

    public void RecusarAmizade(long personagemId, string codigo) => Gravar(personagemId, codigo);

    public void Lista(long personagemId, ListaDoGrupo lista) => Gravar(personagemId, lista);

    public void Aviso(long personagemId, AvisoDoGrupo aviso) => Gravar(personagemId, aviso);

    public void Convite(long personagemId, ConviteRecebido convite) =>
        Gravar(personagemId, convite);

    public void Recusada(long personagemId, string codigo) => Gravar(personagemId, codigo);

    public void Limpar() => _enviadas.Clear();

    public List<T> De<T>(long para) =>
        _enviadas.Where(e => e.Para == para && e.Mensagem is T).Select(e => (T)e.Mensagem).ToList();

    public ListaDeAmigos ListaAmigos(long para) => De<ListaDeAmigos>(para).Last();

    public string? UltimaRecusaAmizade(long para) => De<string>(para).LastOrDefault();

    private void Gravar(long para, object mensagem) => _enviadas.Add((para, mensagem));
}

internal sealed class AmbienteDeAmigos
{
    public const long A = 1,
        B = 2;

    private readonly ServiceProvider _provedor;
    private readonly SqliteConnection _conexao;
    private double _agora;

    public AmbienteDeAmigos()
    {
        _conexao = new SqliteConnection("Data Source=:memory:");
        _conexao.Open();
        var servicos = new ServiceCollection();
        servicos.AddDbContext<JogoDbContext>(opcoes => opcoes.UseSqlite(_conexao));
        _provedor = servicos.BuildServiceProvider();
        using (var escopo = _provedor.CreateScope())
        {
            escopo.ServiceProvider.GetRequiredService<JogoDbContext>().Database.EnsureCreated();
        }

        Diretorio = new DiretorioFalso();
        Diretorio.Adicionar(A, "Ana", 1);
        Diretorio.Adicionar(B, "Bia", 2);
        Saida = new SaidaAmigosGravada();
        Amigos = CriarAmigos();
        Amigos.Carregar();
        Amigos.RegistrarOnline(1, A);
        Amigos.RegistrarOnline(2, B);
        _agora = 10;
    }

    public DiretorioFalso Diretorio { get; }
    public SaidaAmigosGravada Saida { get; }
    public AmigosDoServidor Amigos { get; private set; }

    public long Pedir(long de, long para)
    {
        Avancar(RegrasAmigos.IntervaloEntrePedidosSegundos);
        Saida.Limpar();
        Amigos.PedirAmizade(de, AmbienteDeGrupo.Nome(para));
        var lista = Saida.ListaAmigos(de);
        var pedido = lista.PedidosEnviados.Single();
        return pedido.PedidoId;
    }

    public AmigosDoServidor RecarregarAmigos()
    {
        Amigos = CriarAmigos();
        Amigos.Carregar();
        return Amigos;
    }

    public AmigosDoServidor RecriarSemCarregar()
    {
        Amigos = CriarAmigos();
        return Amigos;
    }

    private AmigosDoServidor CriarAmigos() =>
        new(Diretorio, Saida, _provedor.GetRequiredService<IServiceScopeFactory>(), () => _agora);

    private void Avancar(double segundos) => _agora += segundos;
}
