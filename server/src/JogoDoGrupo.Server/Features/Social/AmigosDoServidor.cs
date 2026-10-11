using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Shared.Features.Social;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>
/// Amigos, pedidos e ignorados por conta. Persiste no banco; online vem de
/// <see cref="RegistrarOnline"/> / <see cref="RemoverOnline"/>.
/// </summary>
public sealed class AmigosDoServidor(
    IDiretorioDePersonagens diretorio,
    ISaidaAmigos saida,
    IServiceScopeFactory escopos,
    Func<double> relogioSegundos
)
{
    private readonly Lock _trava = new();
    private readonly Lock _travaDaCarga = new();
    private volatile bool _carregado;

    private readonly Dictionary<long, Dictionary<long, long>> _amigos = [];
    private readonly Dictionary<long, Dictionary<long, string>> _nomesAmizade = [];
    private readonly Dictionary<long, PedidoAmizadeSalvo> _pedidos = [];
    private readonly Dictionary<long, IgnoradoSalvo> _ignoradosPorId = [];
    private readonly Dictionary<long, Dictionary<long, long>> _ignorados = [];
    private readonly Dictionary<long, (long PersonagemId, string Nome)> _onlinePorConta = [];
    private readonly Dictionary<long, double> _ultimoPedidoSegundos = [];
    private long _proximoAmigoId = 1;
    private long _proximoPedidoId = 1;
    private long _proximoIgnoradoId = 1;

    /// <summary>
    /// Lê do banco amigos, pedidos e ignorados. Não precisa ser chamado na subida: o primeiro
    /// uso carrega sozinho, depois que o banco já foi migrado.
    /// </summary>
    public void Carregar()
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        lock (_trava)
        {
            _amigos.Clear();
            _nomesAmizade.Clear();
            _pedidos.Clear();
            _ignorados.Clear();
            _ignoradosPorId.Clear();
            foreach (var linha in db.Set<AmizadeSalva>().AsNoTracking())
            {
                Mapa(_amigos, linha.ContaId)[linha.AmigoContaId] = linha.Id;
                Mapa(_nomesAmizade, linha.ContaId)[linha.AmigoContaId] = linha.NomeReferencia;
                _proximoAmigoId = Math.Max(_proximoAmigoId, linha.Id + 1);
            }

            foreach (var pedido in db.Set<PedidoAmizadeSalvo>().AsNoTracking())
            {
                _pedidos[pedido.PedidoId] = pedido;
                _proximoPedidoId = Math.Max(_proximoPedidoId, pedido.PedidoId + 1);
            }

            foreach (var ign in db.Set<IgnoradoSalvo>().AsNoTracking())
            {
                _ignoradosPorId[ign.IgnoradoId] = ign;
                Mapa(_ignorados, ign.ContaId)[ign.IgnoradoContaId] = ign.IgnoradoId;
                _proximoIgnoradoId = Math.Max(_proximoIgnoradoId, ign.IgnoradoId + 1);
            }
        }

        _carregado = true;
    }

    private void GarantirCarregado()
    {
        if (_carregado)
        {
            return;
        }

        lock (_travaDaCarga)
        {
            if (!_carregado)
            {
                Carregar();
            }
        }
    }

    public void RegistrarOnline(long contaId, long personagemId)
    {
        GarantirCarregado();
        if (!diretorio.TentarObterNome(personagemId, out var nome))
        {
            return;
        }

        List<long> amigosConta;
        lock (_trava)
        {
            var anterior = _onlinePorConta.GetValueOrDefault(contaId);
            _onlinePorConta[contaId] = (personagemId, nome);
            amigosConta = AmigosDe(contaId).ToList();
        }

        NotificarAmigosOnline(contaId, personagemId, nome, amigosConta);
        EnviarListasDaConta(contaId, personagemId);
    }

    public void RemoverOnline(long contaId, long personagemId)
    {
        GarantirCarregado();
        List<long> amigosConta;
        lock (_trava)
        {
            if (
                !_onlinePorConta.TryGetValue(contaId, out var atual)
                || atual.PersonagemId != personagemId
            )
            {
                return;
            }

            _onlinePorConta.Remove(contaId);
            amigosConta = AmigosDe(contaId).ToList();
        }

        foreach (var amigoConta in amigosConta)
        {
            if (_onlinePorConta.TryGetValue(amigoConta, out var sessaoAmigo))
            {
                if (!TryAmigoId(amigoConta, contaId, out var amigoId))
                {
                    continue;
                }

                saida.AmigoMudouDeEstado(
                    sessaoAmigo.PersonagemId,
                    new AmigoMudouDeEstado
                    {
                        AmigoId = amigoId,
                        Online = false,
                        PersonagemOnline = "",
                    }
                );
            }
        }
    }

    public void PedirAmizade(long dePersonagemId, string? nome)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var deConta))
        {
            return;
        }

        var achou = diretorio.TentarObterPorNome(nome ?? "", out var alvoPersonagemId);
        long alvoConta = 0;
        if (achou && !diretorio.TentarObterContaDoPersonagem(alvoPersonagemId, out alvoConta))
        {
            achou = false;
        }

        lock (_trava)
        {
            if (!_onlinePorConta.ContainsKey(deConta))
            {
                return;
            }

            var relacao = achou ? Relacao(deConta, alvoConta) : RelacaoDeAmizade.Nenhuma;
            var recusa = RegrasAmigos.RecusaDoPedido(
                achou && alvoConta == deConta,
                relacao,
                achou && Ignora(deConta, alvoConta),
                achou && Ignora(alvoConta, deConta),
                ContarAmigos(deConta),
                ContarPedidosEnviados(deConta),
                achou ? ContarPedidosRecebidos(alvoConta) : 0,
                relogioSegundos() - _ultimoPedidoSegundos.GetValueOrDefault(deConta)
            );
            if (recusa != RegrasAmigos.SemRecusa)
            {
                saida.RecusarAmizade(dePersonagemId, recusa);
                return;
            }

            if (!achou)
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            if (RegrasAmigos.PedidoViraAceite(relacao))
            {
                AceitarPedidoInterno(
                    deConta,
                    dePersonagemId,
                    alvoConta,
                    PedidoRecebidoId(alvoConta, deConta)
                );
                return;
            }

            CriarPedido(deConta, dePersonagemId, alvoConta, nome!);
        }
    }

    public void ResponderPedido(long dePersonagemId, long pedidoId, bool aceitar)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var conta))
        {
            return;
        }

        lock (_trava)
        {
            if (!_pedidos.TryGetValue(pedidoId, out var pedido) || pedido.ParaContaId != conta)
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.PedidoNaoEncontrado);
                return;
            }

            if (!aceitar)
            {
                RemoverPedido(pedidoId);
                EnviarListas(conta, dePersonagemId);
                return;
            }

            var recusa = RegrasAmigos.RecusaDoAceite(
                ContarAmigos(conta),
                ContarPedidosEnviados(conta)
            );
            if (recusa != RegrasAmigos.SemRecusa)
            {
                saida.RecusarAmizade(dePersonagemId, recusa);
                return;
            }

            AceitarPedidoInterno(conta, dePersonagemId, pedido.DeContaId, pedidoId);
        }
    }

    public void CancelarPedido(long dePersonagemId, long pedidoId)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var conta))
        {
            return;
        }

        lock (_trava)
        {
            if (!_pedidos.TryGetValue(pedidoId, out var pedido) || pedido.DeContaId != conta)
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.PedidoNaoEncontrado);
                return;
            }

            RemoverPedido(pedidoId);
            EnviarListas(conta, dePersonagemId);
        }
    }

    public void RemoverAmigo(long dePersonagemId, long amigoId)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var conta))
        {
            return;
        }

        lock (_trava)
        {
            if (!TryParAmizade(conta, amigoId, out var amigoConta))
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.AmigoNaoEncontrado);
                return;
            }

            RemoverAmizadePar(conta, amigoConta);
            EnviarListas(conta, dePersonagemId);
            if (_onlinePorConta.TryGetValue(amigoConta, out var onlineAmigo))
            {
                EnviarListas(amigoConta, onlineAmigo.PersonagemId);
            }
        }
    }

    public void Ignorar(long dePersonagemId, string? nome)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var conta))
        {
            return;
        }

        var achou = diretorio.TentarObterPorNome(nome ?? "", out var alvoPersonagemId);
        long alvoConta = 0;
        var nomeAlvo = "";
        if (achou)
        {
            achou =
                diretorio.TentarObterContaDoPersonagem(alvoPersonagemId, out alvoConta)
                && diretorio.TentarObterNome(alvoPersonagemId, out nomeAlvo);
        }

        lock (_trava)
        {
            var recusa = RegrasAmigos.RecusaDeIgnorar(
                achou && alvoConta == conta,
                achou && Ignora(conta, alvoConta),
                ContarIgnorados(conta)
            );
            if (recusa != RegrasAmigos.SemRecusa)
            {
                saida.RecusarAmizade(dePersonagemId, recusa);
                return;
            }

            if (!achou)
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.AlvoNaoEncontrado);
                return;
            }

            RemoverAmizadePar(conta, alvoConta);
            RemoverPedidosEntre(conta, alvoConta);

            var ignoradoId = _proximoIgnoradoId++;
            var linha = new IgnoradoSalvo
            {
                IgnoradoId = ignoradoId,
                ContaId = conta,
                IgnoradoContaId = alvoConta,
                Nome = nomeAlvo,
            };
            _ignoradosPorId[ignoradoId] = linha;
            Mapa(_ignorados, conta)[alvoConta] = ignoradoId;
            PersistirIgnorado(linha);
            EnviarListas(conta, dePersonagemId);
            if (_onlinePorConta.TryGetValue(alvoConta, out var onlineAlvo))
            {
                EnviarListas(alvoConta, onlineAlvo.PersonagemId);
            }
        }
    }

    public void DeixarDeIgnorar(long dePersonagemId, long ignoradoId)
    {
        GarantirCarregado();
        if (!ContaDe(dePersonagemId, out var conta))
        {
            return;
        }

        lock (_trava)
        {
            if (!_ignoradosPorId.TryGetValue(ignoradoId, out var linha) || linha.ContaId != conta)
            {
                saida.RecusarAmizade(dePersonagemId, CodigosErroSocial.IgnoradoNaoEncontrado);
                return;
            }

            _ignoradosPorId.Remove(ignoradoId);
            Mapa(_ignorados, conta).Remove(linha.IgnoradoContaId);
            ApagarIgnorado(ignoradoId);
            EnviarListas(conta, dePersonagemId);
        }
    }

    public bool ContatoBarradoEntrePersonagens(long origemPersonagemId, long alvoPersonagemId)
    {
        GarantirCarregado();
        if (
            !ContaDe(origemPersonagemId, out var origemConta)
            || !ContaDe(alvoPersonagemId, out var alvoConta)
        )
        {
            return false;
        }

        lock (_trava)
        {
            return RegrasAmigos.ContatoBarrado(
                Ignora(origemConta, alvoConta),
                Ignora(alvoConta, origemConta)
            );
        }
    }

    private void CriarPedido(long deConta, long dePersonagemId, long alvoConta, string nomeAlvo)
    {
        var pedidoId = _proximoPedidoId++;
        var pedido = new PedidoAmizadeSalvo
        {
            PedidoId = pedidoId,
            DeContaId = deConta,
            ParaContaId = alvoConta,
            NomeAlvo = nomeAlvo,
        };
        _pedidos[pedidoId] = pedido;
        _ultimoPedidoSegundos[deConta] = relogioSegundos();
        PersistirPedido(pedido);
        EnviarListas(deConta, dePersonagemId);
    }

    private void AceitarPedidoInterno(
        long contaQueAceita,
        long personagemQueAceita,
        long contaQuemPediu,
        long pedidoId
    )
    {
        RemoverPedido(pedidoId);
        var nomeReferenciaLadoAceita = NomeReferencia(contaQuemPediu, contaQueAceita);
        var nomeReferenciaLadoPediu = NomeReferencia(contaQueAceita, contaQuemPediu);
        CriarAmizade(contaQueAceita, contaQuemPediu, nomeReferenciaLadoAceita);
        CriarAmizade(contaQuemPediu, contaQueAceita, nomeReferenciaLadoPediu);
        EnviarListas(contaQueAceita, personagemQueAceita);
        if (_onlinePorConta.TryGetValue(contaQuemPediu, out var onlinePediu))
        {
            EnviarListas(contaQuemPediu, onlinePediu.PersonagemId);
            NotificarAmigosOnline(
                contaQueAceita,
                personagemQueAceita,
                _onlinePorConta[contaQueAceita].Nome,
                [contaQuemPediu]
            );
        }
    }

    private string NomeReferencia(long deConta, long paraConta)
    {
        foreach (var pedido in _pedidos.Values)
        {
            if (pedido.DeContaId == deConta && pedido.ParaContaId == paraConta)
            {
                return pedido.NomeAlvo;
            }
        }

        if (_onlinePorConta.TryGetValue(deConta, out var online))
        {
            return online.Nome;
        }

        return "Amigo";
    }

    private void CriarAmizade(long conta, long amigoConta, string nomeReferencia)
    {
        var id = _proximoAmigoId++;
        Mapa(_amigos, conta)[amigoConta] = id;
        Mapa(_nomesAmizade, conta)[amigoConta] = nomeReferencia;
        PersistirAmizade(
            new AmizadeSalva
            {
                Id = id,
                ContaId = conta,
                AmigoContaId = amigoConta,
                NomeReferencia = nomeReferencia,
            }
        );
    }

    private void RemoverAmizadePar(long contaA, long contaB)
    {
        if (Mapa(_amigos, contaA).TryGetValue(contaB, out var idA))
        {
            Mapa(_amigos, contaA).Remove(contaB);
            Mapa(_nomesAmizade, contaA).Remove(contaB);
            ApagarAmizade(idA);
        }

        if (Mapa(_amigos, contaB).TryGetValue(contaA, out var idB))
        {
            Mapa(_amigos, contaB).Remove(contaA);
            Mapa(_nomesAmizade, contaB).Remove(contaA);
            ApagarAmizade(idB);
        }
    }

    private void RemoverPedidosEntre(long contaA, long contaB)
    {
        foreach (var pedidoId in _pedidos.Keys.ToList())
        {
            var pedido = _pedidos[pedidoId];
            if (
                (pedido.DeContaId == contaA && pedido.ParaContaId == contaB)
                || (pedido.DeContaId == contaB && pedido.ParaContaId == contaA)
            )
            {
                RemoverPedido(pedidoId);
            }
        }
    }

    private void RemoverPedido(long pedidoId)
    {
        if (_pedidos.Remove(pedidoId, out _))
        {
            ApagarPedido(pedidoId);
        }
    }

    private long PedidoRecebidoId(long deConta, long paraConta)
    {
        foreach (var pedido in _pedidos.Values)
        {
            if (pedido.DeContaId == deConta && pedido.ParaContaId == paraConta)
            {
                return pedido.PedidoId;
            }
        }

        return 0;
    }

    private RelacaoDeAmizade Relacao(long deConta, long alvoConta)
    {
        if (Mapa(_amigos, deConta).ContainsKey(alvoConta))
        {
            return RelacaoDeAmizade.Amigos;
        }

        if (_pedidos.Values.Any(p => p.DeContaId == deConta && p.ParaContaId == alvoConta))
        {
            return RelacaoDeAmizade.PedidoEnviado;
        }

        if (_pedidos.Values.Any(p => p.DeContaId == alvoConta && p.ParaContaId == deConta))
        {
            return RelacaoDeAmizade.PedidoRecebido;
        }

        return RelacaoDeAmizade.Nenhuma;
    }

    private bool Ignora(long conta, long alvoConta) =>
        Mapa(_ignorados, conta).ContainsKey(alvoConta);

    private int ContarAmigos(long conta) => Mapa(_amigos, conta).Count;

    private int ContarPedidosEnviados(long conta) =>
        _pedidos.Values.Count(p => p.DeContaId == conta);

    private int ContarPedidosRecebidos(long conta) =>
        _pedidos.Values.Count(p => p.ParaContaId == conta);

    private int ContarIgnorados(long conta) => Mapa(_ignorados, conta).Count;

    private IEnumerable<long> AmigosDe(long conta) => Mapa(_amigos, conta).Keys;

    private bool TryAmigoId(long conta, long amigoConta, out long amigoId) =>
        Mapa(_amigos, conta).TryGetValue(amigoConta, out amigoId);

    private bool TryParAmizade(long conta, long amigoId, out long amigoConta)
    {
        foreach (var par in Mapa(_amigos, conta))
        {
            if (par.Value == amigoId)
            {
                amigoConta = par.Key;
                return true;
            }
        }

        amigoConta = 0;
        return false;
    }

    private void EnviarListasDaConta(long contaId, long personagemId) =>
        EnviarListas(contaId, personagemId);

    private void EnviarListas(long contaId, long personagemId)
    {
        saida.ListaDeAmigos(personagemId, MontarListaAmigos(contaId));
        saida.ListaDeIgnorados(personagemId, MontarListaIgnorados(contaId));
    }

    private ListaDeAmigos MontarListaAmigos(long contaId)
    {
        var amigos = new List<AmigoDaLista>();
        foreach (var (amigoConta, amigoId) in Mapa(_amigos, contaId))
        {
            var nome = Mapa(_nomesAmizade, contaId).GetValueOrDefault(amigoConta, "Amigo");
            var online = _onlinePorConta.TryGetValue(amigoConta, out var sessao);
            amigos.Add(
                new AmigoDaLista
                {
                    AmigoId = amigoId,
                    Nome = nome,
                    Online = online,
                    PersonagemOnline = online ? sessao.Nome : "",
                }
            );
        }

        var recebidos = _pedidos
            .Values.Where(p => p.ParaContaId == contaId)
            .Select(p => new PedidoDeAmizade
            {
                PedidoId = p.PedidoId,
                Nome = NomeConta(p.DeContaId),
            })
            .ToArray();
        var enviados = _pedidos
            .Values.Where(p => p.DeContaId == contaId)
            .Select(p => new PedidoDeAmizade { PedidoId = p.PedidoId, Nome = p.NomeAlvo })
            .ToArray();

        return new ListaDeAmigos
        {
            Amigos = amigos.ToArray(),
            PedidosRecebidos = recebidos,
            PedidosEnviados = enviados,
        };
    }

    private ListaDeIgnorados MontarListaIgnorados(long contaId)
    {
        var lista = Mapa(_ignorados, contaId)
            .Select(par =>
            {
                var linha = _ignoradosPorId[par.Value];
                return new JogadorIgnorado { IgnoradoId = linha.IgnoradoId, Nome = linha.Nome };
            })
            .ToArray();
        return new ListaDeIgnorados { Ignorados = lista };
    }

    private string NomeConta(long contaId)
    {
        if (_onlinePorConta.TryGetValue(contaId, out var online))
        {
            return online.Nome;
        }

        return "Jogador";
    }

    private void NotificarAmigosOnline(
        long contaId,
        long personagemId,
        string nome,
        IReadOnlyList<long> amigosConta
    )
    {
        foreach (var amigoConta in amigosConta)
        {
            if (!_onlinePorConta.TryGetValue(amigoConta, out var sessaoAmigo))
            {
                continue;
            }

            if (!TryAmigoId(amigoConta, contaId, out var amigoId))
            {
                continue;
            }

            saida.AmigoMudouDeEstado(
                sessaoAmigo.PersonagemId,
                new AmigoMudouDeEstado
                {
                    AmigoId = amigoId,
                    Online = true,
                    PersonagemOnline = nome,
                }
            );
        }
    }

    private bool ContaDe(long personagemId, out long contaId) =>
        diretorio.TentarObterContaDoPersonagem(personagemId, out contaId);

    private static Dictionary<long, T> Mapa<T>(
        Dictionary<long, Dictionary<long, T>> raiz,
        long conta
    ) => raiz.TryGetValue(conta, out var mapa) ? mapa : raiz[conta] = [];

    private void PersistirAmizade(AmizadeSalva linha)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<AmizadeSalva>().Add(linha);
        db.SaveChanges();
    }

    private void ApagarAmizade(long id)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<AmizadeSalva>().Where(a => a.Id == id).ExecuteDelete();
    }

    private void PersistirPedido(PedidoAmizadeSalvo pedido)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<PedidoAmizadeSalvo>().Add(pedido);
        db.SaveChanges();
    }

    private void ApagarPedido(long pedidoId)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<PedidoAmizadeSalvo>().Where(p => p.PedidoId == pedidoId).ExecuteDelete();
    }

    private void PersistirIgnorado(IgnoradoSalvo linha)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<IgnoradoSalvo>().Add(linha);
        db.SaveChanges();
    }

    private void ApagarIgnorado(long ignoradoId)
    {
        using var escopo = escopos.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        db.Set<IgnoradoSalvo>().Where(i => i.IgnoradoId == ignoradoId).ExecuteDelete();
    }
}
