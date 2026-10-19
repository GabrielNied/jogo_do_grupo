using System.Collections.Concurrent;
using JogoDoGrupo.Server.Core.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Economia;

/// <summary>
/// Mantém as barracas presenciais em memória e consulta vendedores offline ativos para
/// resolver se um jogador possui barraca na praça (#905). Desacoplado do ServicoDoMercado.
/// </summary>
public sealed class ControleDeBarracasAtivas(JogoDbContext db, RelogioDoMercado relogio)
    : IResolvedorDeBarracas
{
    private static readonly ConcurrentDictionary<long, BarracaPresencialInfo> _presenciais = new();

    public ICollection<BarracaPresencialInfo> BarracasPresenciais => _presenciais.Values;

    public void RegistrarPresencial(BarracaPresencialInfo info)
    {
        _presenciais[info.PersonagemId] = info;
    }

    public void RemoverPresencial(long personagemId)
    {
        _presenciais.TryRemove(personagemId, out _);
    }

    public bool TentarObterPresencial(long personagemId, out BarracaPresencialInfo? info)
    {
        if (_presenciais.TryGetValue(personagemId, out var presencial))
        {
            info = presencial;
            return true;
        }

        info = null;
        return false;
    }

    public bool TentarObterNomeBarraca(long personagemId, out string nomeOuTitulo)
    {
        if (_presenciais.TryGetValue(personagemId, out var presencial))
        {
            nomeOuTitulo = !string.IsNullOrWhiteSpace(presencial.Titulo)
                ? presencial.Titulo
                : presencial.NomeDono;
            return true;
        }

        var agora = relogio.AgoraSegundos;
        var offline = db.Set<VendedorOfflineEntidade>()
            .AsNoTracking()
            .FirstOrDefault(v => v.PersonagemId == personagemId && v.Ativo && v.ExpiraEm > agora);

        if (offline != null)
        {
            nomeOuTitulo = !string.IsNullOrWhiteSpace(offline.Titulo)
                ? offline.Titulo
                : offline.NomeDono;
            return true;
        }

        nomeOuTitulo = "";
        return false;
    }
}
