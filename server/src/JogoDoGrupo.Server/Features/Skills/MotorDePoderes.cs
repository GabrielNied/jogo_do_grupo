using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Features.Skills;

/// <summary>Quanto de recarga tirar das skills que o seletor escolhe.</summary>
public sealed record ReducaoDeRecarga(SeletorDeSkill Alvo, int Ms);

/// <summary>
/// O que os efeitos fizeram num evento: o multiplicador do dano da skill (1 sem efeito), as
/// recargas a reduzir e os avisos para o dono.
/// </summary>
public sealed record ResultadoDosPoderes(
    double MultiplicadorDoDano,
    IReadOnlyList<ReducaoDeRecarga> Reducoes,
    IReadOnlyList<PoderDisparou> Avisos
)
{
    public static readonly ResultadoDosPoderes Nenhum = new(1.0, [], []);
}

/// <summary>
/// O motor dos efeitos de equipamento no servidor (poderes lendários e bônus de set, #629 e
/// #630): interpreta as regras em dados (<see cref="RegraDePoder"/>) que Itens entrega por
/// <see cref="IEfeitosDoEquipamento"/> e guarda o estado de execução (cargas do personagem,
/// marcas nos alvos, folga entre ativações). Sem ciclo: a ação de uma regra nunca dispara outro
/// gatilho e as condições olham o estado de <b>antes</b> do evento. A lista de efeitos fica em
/// cache até <see cref="EquipamentoMudou"/>; ao recalcular, o estado que veio de uma origem que
/// saiu (peça tirada, marco perdido) é apagado.
/// </summary>
public sealed class MotorDePoderes
{
    private readonly IEfeitosDoEquipamento _efeitos;
    private readonly Func<long> _agoraMs;
    private readonly ILogger<MotorDePoderes>? _log;
    private readonly ConcurrentDictionary<long, EstadoDoPersonagem> _estados = new();

    public MotorDePoderes(
        IEfeitosDoEquipamento efeitos,
        Func<long> agoraMs,
        ILogger<MotorDePoderes>? log = null
    )
    {
        _efeitos = efeitos;
        _agoraMs = agoraMs;
        _log = log;
    }

    private sealed class EstadoDoPersonagem
    {
        public readonly object Trava = new();
        public int Versao;
        public IReadOnlyList<EfeitoAtivo>? Efeitos;
        public readonly Dictionary<string, Carga> Cargas = [];
        public readonly Dictionary<(long Alvo, string Estado), Marca> Marcas = [];
        public readonly Dictionary<string, long> UltimaAtivacao = [];
    }

    private readonly record struct Carga(int Quantidade, long ExpiraMs, string Origem);

    private readonly record struct Marca(long ExpiraMs, string Origem);

    /// <summary>Os efeitos mudaram (equipar, desequipar, trocar de conjunto): relê na próxima vez.</summary>
    public void Invalidar(long personagemId)
    {
        var estado = _estados.GetOrAdd(personagemId, _ => new EstadoDoPersonagem());
        lock (estado.Trava)
        {
            estado.Versao++;
            estado.Efeitos = null;
        }
    }

    /// <summary>O personagem saiu: o estado de execução (cargas e marcas) não sobrevive.</summary>
    public void Esquecer(long personagemId) => _estados.TryRemove(personagemId, out _);

    /// <summary>
    /// Uma skill foi usada (custo cobrado, dano ainda não aplicado): gasta as cargas que os
    /// poderes amplificam, devolve o multiplicador do dano e dá as cargas de quem usa skill.
    /// </summary>
    public Task<ResultadoDosPoderes> AoUsarSkillAsync(
        long personagemId,
        DefinicaoDeSkill skill,
        CancellationToken c
    ) => AvaliarAsync(personagemId, GatilhoDePoder.AoUsarSkill, skill, [], c);

    /// <summary>
    /// A skill acertou os <paramref name="alvos"/>: marca alvos, dá cargas e devolve as recargas
    /// a reduzir. Condição de alvo marcado olha as marcas de antes deste acerto.
    /// </summary>
    public Task<ResultadoDosPoderes> AoAcertarComSkillAsync(
        long personagemId,
        DefinicaoDeSkill skill,
        IReadOnlyCollection<long> alvos,
        CancellationToken c
    ) => AvaliarAsync(personagemId, GatilhoDePoder.AoAcertarComSkill, skill, alvos, c);

    /// <summary>Um golpe comum do personagem acertou (Combate publica; veja <c>GolpeComumAcertou</c>).</summary>
    public Task<ResultadoDosPoderes> AoGolpeComumAsync(
        long personagemId,
        IReadOnlyCollection<long> alvos,
        CancellationToken c
    ) => AvaliarAsync(personagemId, GatilhoDePoder.AoAcertarAtaqueBasico, null, alvos, c);

    /// <summary>O personagem esquivou (Combate publica; veja <c>PersonagemEsquivou</c>).</summary>
    public Task<ResultadoDosPoderes> AoEsquivarAsync(long personagemId, CancellationToken c) =>
        AvaliarAsync(personagemId, GatilhoDePoder.AoEsquivar, null, [], c);

    private async Task<ResultadoDosPoderes> AvaliarAsync(
        long personagemId,
        GatilhoDePoder gatilho,
        DefinicaoDeSkill? skill,
        IReadOnlyCollection<long> alvos,
        CancellationToken c
    )
    {
        var estado = _estados.GetOrAdd(personagemId, _ => new EstadoDoPersonagem());
        var efeitos = await EfeitosAsync(personagemId, estado, c);
        if (efeitos.Count == 0)
            return ResultadoDosPoderes.Nenhum;
        lock (estado.Trava)
        {
            return Avaliar(estado, efeitos, gatilho, skill, alvos, _agoraMs());
        }
    }

    private async Task<IReadOnlyList<EfeitoAtivo>> EfeitosAsync(
        long personagemId,
        EstadoDoPersonagem estado,
        CancellationToken c
    )
    {
        int versao;
        lock (estado.Trava)
        {
            if (estado.Efeitos != null)
                return estado.Efeitos;
            versao = estado.Versao;
        }

        IReadOnlyList<EfeitoAtivo> lidos;
        try
        {
            lidos = await _efeitos.ObterAsync(personagemId, c);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // Sem os efeitos a skill sai sem poder, mas sai: o custo já foi cobrado.
            _log?.LogError(e, "Erro ao ler os efeitos do personagem {Personagem}.", personagemId);
            return [];
        }

        lock (estado.Trava)
        {
            // Mudou enquanto lia: usa o lido agora, mas não guarda (a próxima vez relê).
            if (estado.Versao == versao)
            {
                estado.Efeitos = lidos;
                Podar(estado, lidos);
            }
        }

        return lidos;
    }

    // Apaga o estado que veio de uma origem que não está mais ativa.
    private static void Podar(EstadoDoPersonagem estado, IReadOnlyList<EfeitoAtivo> efeitos)
    {
        var origens = new HashSet<string>(efeitos.Select(e => e.Origem));
        var chaves = new HashSet<string>(efeitos.Select(e => e.Chave));
        foreach (
            var chave in estado
                .Cargas.Where(p => !origens.Contains(p.Value.Origem))
                .Select(p => p.Key)
                .ToList()
        )
            estado.Cargas.Remove(chave);
        foreach (
            var chave in estado
                .Marcas.Where(p => !origens.Contains(p.Value.Origem))
                .Select(p => p.Key)
                .ToList()
        )
            estado.Marcas.Remove(chave);
        foreach (var chave in estado.UltimaAtivacao.Keys.Where(k => !chaves.Contains(k)).ToList())
            estado.UltimaAtivacao.Remove(chave);
    }

    private static ResultadoDosPoderes Avaliar(
        EstadoDoPersonagem estado,
        IReadOnlyList<EfeitoAtivo> efeitos,
        GatilhoDePoder gatilho,
        DefinicaoDeSkill? skill,
        IReadOnlyCollection<long> alvos,
        long agora
    )
    {
        Expirar(estado, agora);
        var usaSkill =
            gatilho == GatilhoDePoder.AoUsarSkill || gatilho == GatilhoDePoder.AoAcertarComSkill;

        // 1. Decide tudo pelo estado de antes do evento: nenhuma ação influencia outra condição.
        var decididos = new List<(EfeitoAtivo Efeito, int CargasAntes)>();
        foreach (var efeito in efeitos)
        {
            var regra = efeito.Regra;
            if (regra.Gatilho != gatilho)
                continue;
            if (usaSkill && (skill == null || !regra.Skill.Corresponde(skill.Id, skill.Etiquetas)))
                continue;
            if (
                regra.IntervaloMinimoMs > 0
                && estado.UltimaAtivacao.TryGetValue(efeito.Chave, out var ultima)
                && agora - ultima < regra.IntervaloMinimoMs
            )
                continue;
            var cargas = estado.Cargas.TryGetValue(regra.Estado, out var carga)
                ? carga.Quantidade
                : 0;
            if (!CondicaoVale(estado, regra, cargas, alvos))
                continue;
            decididos.Add((efeito, cargas));
        }

        var avisos = new List<PoderDisparou>();
        var reducoes = new List<ReducaoDeRecarga>();
        var bonus = 0.0;

        // 2. Gasta as cargas antes de dar novas: a carga que esta mesma skill dá fica para a próxima.
        foreach (var (efeito, cargasAntes) in decididos)
        {
            var regra = efeito.Regra;
            if (regra.Acao != AcaoDePoder.AmplificarDano)
                continue;
            var n = 1;
            if (regra.Condicao == CondicaoDePoder.TemCarga)
            {
                n = Math.Min(
                    cargasAntes,
                    estado.Cargas.TryGetValue(regra.Estado, out var atual) ? atual.Quantidade : 0
                );
                if (n <= 0)
                    continue; // outra regra já gastou essas cargas neste evento
                Gastar(estado, regra.Estado, n);
            }

            bonus += regra.Valor * n;
            estado.UltimaAtivacao[efeito.Chave] = agora;
            avisos.Add(
                Aviso(
                    efeito,
                    AcaoDePoder.AmplificarDano,
                    (int)Math.Round(regra.Valor * n * 100, MidpointRounding.AwayFromZero)
                )
            );
        }

        // 3. O resto das ações.
        foreach (var (efeito, _) in decididos)
        {
            var regra = efeito.Regra;
            switch (regra.Acao)
            {
                case AcaoDePoder.GanharCarga:
                    var quantidade = estado.Cargas.TryGetValue(regra.Estado, out var existente)
                        ? existente.Quantidade
                        : 0;
                    quantidade = Math.Min(regra.MaximoDeAcumulos, quantidade + 1);
                    estado.Cargas[regra.Estado] = new Carga(
                        quantidade,
                        agora + regra.DuracaoMs,
                        efeito.Origem
                    );
                    avisos.Add(Aviso(efeito, AcaoDePoder.GanharCarga, quantidade));
                    break;
                case AcaoDePoder.MarcarAlvos:
                    foreach (var alvo in alvos)
                        estado.Marcas[(alvo, regra.Estado)] = new Marca(
                            agora + regra.DuracaoMs,
                            efeito.Origem
                        );
                    avisos.Add(Aviso(efeito, AcaoDePoder.MarcarAlvos, alvos.Count));
                    break;
                case AcaoDePoder.ReduzirRecarga:
                    reducoes.Add(new ReducaoDeRecarga(regra.AlvoDaAcao, (int)regra.Valor));
                    avisos.Add(Aviso(efeito, AcaoDePoder.ReduzirRecarga, (int)regra.Valor));
                    break;
                default:
                    continue;
            }

            estado.UltimaAtivacao[efeito.Chave] = agora;
        }

        return new ResultadoDosPoderes(1.0 + bonus, reducoes, avisos);
    }

    private static bool CondicaoVale(
        EstadoDoPersonagem estado,
        RegraDePoder regra,
        int cargas,
        IReadOnlyCollection<long> alvos
    )
    {
        switch (regra.Condicao)
        {
            case CondicaoDePoder.Nenhuma:
                return true;
            case CondicaoDePoder.TemCarga:
                return cargas > 0;
            case CondicaoDePoder.AlvoMarcado:
                return alvos.Any(a => estado.Marcas.ContainsKey((a, regra.Estado)));
            default:
                return false;
        }
    }

    private static void Gastar(EstadoDoPersonagem estado, string nome, int quantidade)
    {
        var carga = estado.Cargas[nome];
        var resto = carga.Quantidade - quantidade;
        if (resto <= 0)
            estado.Cargas.Remove(nome);
        else
            estado.Cargas[nome] = carga with { Quantidade = resto };
    }

    private static void Expirar(EstadoDoPersonagem estado, long agora)
    {
        foreach (
            var chave in estado
                .Cargas.Where(p => p.Value.ExpiraMs <= agora)
                .Select(p => p.Key)
                .ToList()
        )
            estado.Cargas.Remove(chave);
        foreach (
            var chave in estado
                .Marcas.Where(p => p.Value.ExpiraMs <= agora)
                .Select(p => p.Key)
                .ToList()
        )
            estado.Marcas.Remove(chave);
    }

    private static PoderDisparou Aviso(EfeitoAtivo efeito, AcaoDePoder acao, int valor) =>
        new()
        {
            Nome = efeito.Nome,
            Acao = acao,
            Valor = valor,
        };
}
