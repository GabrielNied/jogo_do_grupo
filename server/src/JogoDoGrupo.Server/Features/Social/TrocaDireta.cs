using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Server.Features.Social;

/// <summary>Leva as mensagens da troca direta ao client dos dois jogadores.</summary>
public interface ISaidaDeTroca
{
    void Convite(long paraPersonagemId, ConviteDeTroca convite);
    void Estado(long paraPersonagemId, EstadoDaTroca estado);
    void Bolsa(long paraPersonagemId, BolsaParaTroca bolsa);
    void Encerrada(long paraPersonagemId, TrocaEncerrada encerrada);
    void Recusada(long paraPersonagemId, TrocaRecusada recusa);
}

/// <summary>O que um jogador pôs na oferta (coroas e itens da bolsa, pelo id do exemplar).</summary>
public sealed record OfertaPedida(long Coroas, long[] InstanciaIds)
{
    public static readonly OfertaPedida Vazia = new(0, []);
}

/// <summary>Resultado de executar a troca: nulo em <see cref="Erro"/> é sucesso.</summary>
public sealed record ResultadoDaTroca(string? Erro, long PersonagemDoErro)
{
    public static readonly ResultadoDaTroca Ok = new(null, 0);
}

/// <summary>
/// Parte da troca que mexe no banco. Separada das regras (<see cref="TrocaDiretaDoServidor"/>)
/// para as regras serem testadas sem banco e a atomicidade, com banco de verdade.
/// </summary>
public interface IExecutorDaTroca
{
    /// <summary>
    /// Confere a oferta de um jogador (saldo, itens negociáveis dele) e devolve como ela aparece
    /// para os dois. O primeiro item de <c>erro</c> não nulo recusa a oferta.
    /// </summary>
    Task<(string? Erro, OfertaDeTroca? Oferta)> ValidarOfertaAsync(
        long personagemId,
        OfertaPedida oferta,
        CancellationToken c
    );

    Task<BolsaParaTroca> BolsaAsync(long personagemId, long trocaId, CancellationToken c);

    /// <summary>
    /// Troca tudo de uma vez, numa transação só: coroas e itens dos dois lados. Qualquer falha
    /// desfaz tudo e nada muda de dono.
    /// </summary>
    Task<ResultadoDaTroca> ExecutarAsync(
        long personagemA,
        OfertaPedida ofertaA,
        long personagemB,
        OfertaPedida ofertaB,
        CancellationToken c
    );
}
