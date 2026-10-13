using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Quais inimigos cada jogador já recebeu no raio de interesse (histerese em
/// <see cref="RegrasInteresseDeInimigos"/>).
/// </summary>
internal sealed class ControleInteresseDeInimigos
{
    private readonly Dictionary<long, HashSet<long>> _recebendo = new();

    public void Reiniciar(long personagemId, IEnumerable<long> inimigosIniciais)
    {
        _recebendo[personagemId] = inimigosIniciais.ToHashSet();
    }

    public void RemoverJogador(long personagemId) => _recebendo.Remove(personagemId);

    /// <summary>Se o jogador já recebia mensagens desse inimigo (estava no raio).</summary>
    public bool JaRecebia(long personagemId, long inimigoId) =>
        _recebendo.TryGetValue(personagemId, out var set) && set.Contains(inimigoId);

    /// <summary>Para de mandar mensagens do inimigo (morte ou saída do raio).</summary>
    public void EsquecerInimigo(long personagemId, long inimigoId) =>
        ObterSet(personagemId).Remove(inimigoId);

    /// <summary>Filtra o retrato ao entrar na área.</summary>
    public InimigoNoMundo[] FiltrarEntrada(
        long personagemId,
        double jogadorX,
        double jogadorZ,
        IEnumerable<InimigoNoMundo> vivos
    )
    {
        var ids = new HashSet<long>();
        var lista = new List<InimigoNoMundo>();
        foreach (var inimigo in vivos)
        {
            var dist = RegrasInteresseDeInimigos.DistanciaAoJogador(
                jogadorX,
                jogadorZ,
                inimigo.X,
                inimigo.Z
            );
            if (RegrasInteresseDeInimigos.DeveEnviar(dist, jaRecebia: false))
            {
                ids.Add(inimigo.InimigoId);
                lista.Add(inimigo);
            }
        }

        _recebendo[personagemId] = ids;
        return lista.ToArray();
    }

    /// <summary>
    /// Decide envio de uma mensagem ligada a um inimigo. Se saiu do raio, devolve
    /// <see cref="AcaoInteresse.EnviarSaida"/>. <see cref="ResultadoAvaliacaoDeInteresse.EntrouNoRaio"/>
    /// vale quando o jogador passa a enxergar o inimigo (precisa de <see cref="InimigoApareceu"/>).
    /// </summary>
    public ResultadoAvaliacaoDeInteresse Avaliar(
        long personagemId,
        long inimigoId,
        double inimigoX,
        double inimigoZ,
        double jogadorX,
        double jogadorZ
    )
    {
        var set = ObterSet(personagemId);
        var dist = RegrasInteresseDeInimigos.DistanciaAoJogador(
            jogadorX,
            jogadorZ,
            inimigoX,
            inimigoZ
        );
        var ja = set.Contains(inimigoId);
        if (RegrasInteresseDeInimigos.DeveEnviar(dist, ja))
        {
            set.Add(inimigoId);
            return new ResultadoAvaliacaoDeInteresse(AcaoInteresse.Enviar, EntrouNoRaio: !ja);
        }

        if (ja)
        {
            set.Remove(inimigoId);
            return new ResultadoAvaliacaoDeInteresse(
                AcaoInteresse.EnviarSaida,
                EntrouNoRaio: false
            );
        }

        return new ResultadoAvaliacaoDeInteresse(AcaoInteresse.Ignorar, EntrouNoRaio: false);
    }

    private HashSet<long> ObterSet(long personagemId)
    {
        if (!_recebendo.TryGetValue(personagemId, out var set))
        {
            set = new HashSet<long>();
            _recebendo[personagemId] = set;
        }

        return set;
    }

    internal enum AcaoInteresse
    {
        Ignorar,
        Enviar,
        EnviarSaida,
    }

    internal readonly record struct ResultadoAvaliacaoDeInteresse(
        AcaoInteresse Acao,
        bool EntrouNoRaio
    );
}
