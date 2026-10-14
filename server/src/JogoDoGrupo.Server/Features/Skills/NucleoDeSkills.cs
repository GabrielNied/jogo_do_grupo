using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Skills;

namespace JogoDoGrupo.Server.Features.Skills;

/// <summary>
/// O que um pedido de uso virou. <see cref="Recusa"/> é um código de
/// <see cref="CodigosErroSkills"/> (nada foi gasto) ou nulo se a skill foi usada; aí
/// <see cref="Usada"/>, <see cref="Recursos"/> e <see cref="Acertos"/> dizem o que aconteceu.
/// </summary>
public sealed record ResultadoDoUso(
    string? Recusa,
    SkillUsada? Usada = null,
    RecursosMudaram? Recursos = null,
    SkillAcertou? Acertos = null,
    string Area = ""
);

/// <summary>
/// As regras de Skills no servidor, sem rede: qual é a barra do personagem e o que acontece
/// quando ele usa uma skill. A arma vem de Itens, o custo é cobrado por Combate (uma operação
/// atômica) e o dano vai nos alvos por <see cref="IAlvosDeCombate"/>. A recarga é da arma
/// (exemplar + espaço): trocar de arma não mexe na da outra. Os pedidos de um personagem rodam um
/// depois do outro, então o custo nunca é cobrado duas vezes pela mesma recarga.
/// </summary>
public sealed class NucleoDeSkills
{
    private readonly IArmaEquipada _armas;
    private readonly IArmazemDeSkills _armazem;
    private readonly ICobrancaDeRecursos _cobranca;
    private readonly IAtributosDosEquipamentos _equipamentos;
    private readonly IPosicoesDoMundo _posicoes;
    private readonly IReadOnlyList<IAlvosDeCombate> _alvos;
    private readonly Func<long> _agoraMs;

    // Fim da recarga (ms) por (exemplar da arma, espaço).
    private readonly ConcurrentDictionary<(long Arma, int Espaco), long> _recargas = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _travas = new();

    public NucleoDeSkills(
        IArmaEquipada armas,
        IArmazemDeSkills armazem,
        ICobrancaDeRecursos cobranca,
        IAtributosDosEquipamentos equipamentos,
        IPosicoesDoMundo posicoes,
        IEnumerable<IAlvosDeCombate> alvos,
        Func<long> agoraMs
    )
    {
        _armas = armas;
        _armazem = armazem;
        _cobranca = cobranca;
        _equipamentos = equipamentos;
        _posicoes = posicoes;
        _alvos = alvos.ToArray();
        _agoraMs = agoraMs;
    }

    /// <summary>A barra da arma equipada com o que falta de cada recarga; vazia sem arma.</summary>
    public async Task<BarraDeSkillsMudou> BarraAsync(long personagemId, CancellationToken c)
    {
        var arma = await _armas.ObterAsync(personagemId, c);
        if (arma is not { } equipada)
        {
            return new BarraDeSkillsMudou();
        }

        var ids = await _armazem.ObterOuSortearAsync(equipada, c);
        var agora = _agoraMs();
        var skills = new List<SkillDaBarra>();
        for (int espaco = 0; espaco < ids.Count; espaco++)
        {
            var definicao = CatalogoDeSkills.Obter(ids[espaco]);
            if (definicao == null)
            {
                continue;
            }

            skills.Add(
                new SkillDaBarra
                {
                    Espaco = espaco,
                    Id = definicao.Id,
                    Nome = definicao.Nome,
                    Recurso = definicao.Recurso,
                    Custo = definicao.Custo,
                    RecargaMs = definicao.RecargaMs,
                    RestanteMs = RestanteMs(equipada.InstanciaId, espaco, agora),
                }
            );
        }

        return new BarraDeSkillsMudou { ArmaId = equipada.InstanciaId, Skills = skills.ToArray() };
    }

    /// <summary>Usa a skill do espaço pedido, ou recusa sem gastar nada.</summary>
    public async Task<ResultadoDoUso> UsarAsync(
        long personagemId,
        UsarSkill pedido,
        CancellationToken c
    )
    {
        var trava = _travas.GetOrAdd(personagemId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(c);
        try
        {
            return await UsarSobTravaAsync(personagemId, pedido, c);
        }
        finally
        {
            trava.Release();
        }
    }

    /// <summary>Esquece o que é só do personagem na memória (a recarga é da arma e fica).</summary>
    public void Esquecer(long personagemId) => _travas.TryRemove(personagemId, out _);

    private async Task<ResultadoDoUso> UsarSobTravaAsync(
        long personagemId,
        UsarSkill pedido,
        CancellationToken c
    )
    {
        if (!float.IsFinite(pedido.DirecaoY))
        {
            return Recusar(CodigosErroSkills.SemSkill);
        }

        var arma = await _armas.ObterAsync(personagemId, c);
        if (arma is not { } equipada)
        {
            return Recusar(CodigosErroSkills.SemSkill);
        }

        var ids = await _armazem.ObterOuSortearAsync(equipada, c);
        if (pedido.Espaco < 0 || pedido.Espaco >= ids.Count)
        {
            return Recusar(CodigosErroSkills.SemSkill);
        }

        var skill = CatalogoDeSkills.Obter(ids[pedido.Espaco]);
        if (skill == null || !_posicoes.TentarObter(personagemId, out var posicao))
        {
            return Recusar(CodigosErroSkills.SemSkill);
        }

        var agora = _agoraMs();
        if (RestanteMs(equipada.InstanciaId, pedido.Espaco, agora) > 0)
        {
            return Recusar(CodigosErroSkills.EmRecarga);
        }

        var (resultado, recursos) = await _cobranca.TentarCobrarAsync(
            personagemId,
            skill.Recurso == RecursoDeSkill.Mana ? RecursoCobravel.Mana : RecursoCobravel.Stamina,
            skill.Custo,
            c
        );
        if (resultado != ResultadoDaCobranca.Cobrado)
        {
            return Recusar(
                resultado == ResultadoDaCobranca.Caido
                    ? CodigosErroSkills.VoceEstaCaido
                    : CodigosErroSkills.RecursoInsuficiente
            );
        }

        _recargas[(equipada.InstanciaId, pedido.Espaco)] = _agoraMs() + skill.RecargaMs;

        var acertos = await AplicarDanoAsync(personagemId, skill, pedido, posicao, c);
        return new ResultadoDoUso(
            null,
            new SkillUsada
            {
                Espaco = pedido.Espaco,
                Id = skill.Id,
                Sequencia = pedido.Sequencia,
                RecargaMs = skill.RecargaMs,
            },
            recursos,
            new SkillAcertou
            {
                AtacanteId = personagemId,
                Id = skill.Id,
                Nome = skill.Nome,
                Sequencia = pedido.Sequencia,
                Acertos = acertos,
            },
            posicao.Area
        );

        ResultadoDoUso Recusar(string codigo) => new(codigo);
    }

    // O volume é o cone do golpe básico (cada dono de alvo o mede); o mesmo alvo leva a skill
    // uma vez só e jogador nunca é alvo (sem PvP nesta fatia).
    private async Task<AcertoDeAtaque[]> AplicarDanoAsync(
        long personagemId,
        DefinicaoDeSkill skill,
        UsarSkill pedido,
        PosicaoNoMundo posicao,
        CancellationToken c
    )
    {
        var itens = await _equipamentos.ObterAsync(personagemId, c);
        var dano = RegrasSkills.Dano(
            skill,
            itens.DanoDaArma,
            itens.BonusDanoFisico,
            itens.BonusDanoMagico
        );
        var vistos = new HashSet<long>();
        var acertos = new List<AcertoDeAtaque>();
        foreach (var fonte in _alvos)
        {
            foreach (var alvo in fonte.AlvosNoVolume(posicao.Area, posicao.X, posicao.Z, pedido.DirecaoY))
            {
                if (
                    alvo.PersonagemId == 0
                    && vistos.Add(alvo.Id)
                    && fonte.TentarAplicarDano(alvo.Id, dano, personagemId, out var r)
                )
                {
                    acertos.Add(
                        new AcertoDeAtaque
                        {
                            AlvoId = r.AlvoId,
                            Dano = r.Dano,
                            VidaRestante = r.VidaRestante,
                            VidaMaxima = r.VidaMaxima,
                        }
                    );
                }
            }
        }

        return acertos.ToArray();
    }

    private int RestanteMs(long armaId, int espaco, long agora)
    {
        if (!_recargas.TryGetValue((armaId, espaco), out var fim))
        {
            return 0;
        }

        var restante = fim - agora;
        if (restante <= 0)
        {
            _recargas.TryRemove(new KeyValuePair<(long, int), long>((armaId, espaco), fim));
            return 0;
        }

        return (int)Math.Min(restante, int.MaxValue);
    }
}
