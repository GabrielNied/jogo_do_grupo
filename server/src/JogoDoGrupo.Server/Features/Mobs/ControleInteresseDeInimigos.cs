using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Quais inimigos cada jogador já recebeu no raio de interesse (histerese em
/// <see cref="RegrasInteresseDeInimigos"/>).
/// </summary>
public sealed class ControleInteresseDeInimigos
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

    /// <summary>Quem já recebia mensagens desse inimigo.</summary>
    public IEnumerable<long> QuemJaRecebia(long inimigoId)
    {
        foreach (var (personagemId, set) in _recebendo)
        {
            if (set.Contains(inimigoId))
            {
                yield return personagemId;
            }
        }
    }

    /// <summary>Para de mandar mensagens do inimigo (morte ou saída do raio).</summary>
    public void EsquecerInimigo(long personagemId, long inimigoId) =>
        ObterSet(personagemId).Remove(inimigoId);

    /// <summary>Filtra o retrato ao entrar na área (dungeons replicam tudo; cidade usa interesse).</summary>
    public InimigoNoMundo[] FiltrarEntrada(
        long personagemId,
        string area,
        double jogadorX,
        double jogadorZ,
        IEnumerable<InimigoNoMundo> vivos
    )
    {
        var ids = new HashSet<long>();
        var lista = new List<InimigoNoMundo>();
        var replicaTudo = area != JogoDoGrupo.Server.Contratos.Mundo.AreasDoMundo.Cidade;
        foreach (var inimigo in vivos)
        {
            var dist = RegrasInteresseDeInimigos.DistanciaAoJogador(
                jogadorX,
                jogadorZ,
                inimigo.X,
                inimigo.Z
            );
            if (replicaTudo || RegrasInteresseDeInimigos.DeveEnviar(dist, jaRecebia: false))
            {
                ids.Add(inimigo.InimigoId);
                lista.Add(inimigo);
            }
        }

        _recebendo[personagemId] = ids;
        return lista.ToArray();
    }

    /// <summary>Filtra o retrato ao entrar na cidade.</summary>
    public InimigoNoMundo[] FiltrarEntrada(
        long personagemId,
        double jogadorX,
        double jogadorZ,
        IEnumerable<InimigoNoMundo> vivos
    ) =>
        FiltrarEntrada(
            personagemId,
            JogoDoGrupo.Server.Contratos.Mundo.AreasDoMundo.Cidade,
            jogadorX,
            jogadorZ,
            vivos
        );

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

    public enum AcaoInteresse
    {
        Ignorar,
        Enviar,
        EnviarSaida,
    }

    public readonly record struct ResultadoAvaliacaoDeInteresse(
        AcaoInteresse Acao,
        bool EntrouNoRaio
    );
}
