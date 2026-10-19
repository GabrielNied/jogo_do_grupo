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
    string Area = "",
    IReadOnlyList<PoderDisparou>? Poderes = null,
    bool BarraMudou = false,
    SkillPreparoIniciado? Preparo = null,
    SkillCancelada? Cancelada = null,
    int Espaco = 0,
    int Sequencia = 0
);

/// <summary>O preparo de skill forte em curso de um personagem (nada cobrado ainda).</summary>
internal sealed record PreparoEmCurso(
    long ArmaId,
    int Espaco,
    string SkillId,
    int Sequencia,
    float DirecaoY
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
    private readonly ITrocaDeArma? _troca;
    private readonly MotorDePoderes? _poderes;

    // Fim da recarga (ms) por (exemplar da arma, espaço).
    private readonly ConcurrentDictionary<(long Arma, int Espaco), long> _recargas = new();
    private readonly ConcurrentDictionary<long, SemaphoreSlim> _travas = new();

    // Um preparo por personagem; só muda sob a trava dele, então concluir e cancelar nunca valem os dois.
    private readonly ConcurrentDictionary<long, PreparoEmCurso> _preparos = new();

    public NucleoDeSkills(
        IArmaEquipada armas,
        IArmazemDeSkills armazem,
        ICobrancaDeRecursos cobranca,
        IAtributosDosEquipamentos equipamentos,
        IPosicoesDoMundo posicoes,
        IEnumerable<IAlvosDeCombate> alvos,
        Func<long> agoraMs,
        ITrocaDeArma? troca = null,
        MotorDePoderes? poderes = null
    )
    {
        _troca = troca;
        _poderes = poderes;
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
                    PreparoMs = definicao.PreparoMs,
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
    public void Esquecer(long personagemId)
    {
        _travas.TryRemove(personagemId, out _);
        _preparos.TryRemove(personagemId, out _);
    }

    /// <summary>Se o personagem está no preparo de uma skill forte.</summary>
    public bool EmPreparo(long personagemId) => _preparos.ContainsKey(personagemId);

    /// <summary>
    /// O fim do preparo (o temporizador do serviço chama): cobra o custo inteiro uma vez e aplica a
    /// skill. Devolve <c>null</c> se o preparo já não existe (foi cancelado) ou é de outro pedido.
    /// Com a arma trocada, vira cancelamento sem cobrança.
    /// </summary>
    public async Task<ResultadoDoUso?> ConcluirPreparoAsync(
        long personagemId,
        int sequencia,
        CancellationToken c
    )
    {
        var trava = _travas.GetOrAdd(personagemId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(c);
        try
        {
            if (
                !_preparos.TryGetValue(personagemId, out var preparo)
                || preparo.Sequencia != sequencia
            )
            {
                return null;
            }

            _preparos.TryRemove(personagemId, out _);
            var skill = CatalogoDeSkills.Obter(preparo.SkillId);
            var arma = await _armas.ObterAsync(personagemId, c);
            if (
                skill == null
                || arma is not { } equipada
                || equipada.InstanciaId != preparo.ArmaId
                || !_posicoes.TentarObter(personagemId, out var posicao)
            )
            {
                return Cancelado(preparo, 0, null);
            }

            return await ExecutarAsync(
                personagemId,
                equipada,
                await _armazem.ObterOuSortearAsync(equipada, c),
                skill,
                preparo.Espaco,
                preparo.Sequencia,
                preparo.DirecaoY,
                posicao,
                c
            );
        }
        finally
        {
            trava.Release();
        }
    }

    /// <summary>
    /// Cancela o preparo em curso e cobra <see cref="RegrasSkills.CustoAoCancelar"/> (50% do custo),
    /// sem dano e sem recarga. Sem recurso para o cancelamento, cancela sem cobrar. Devolve
    /// <c>null</c> se não há preparo (já concluiu ou nunca houve).
    /// </summary>
    public async Task<ResultadoDoUso?> CancelarPreparoAsync(long personagemId, CancellationToken c)
    {
        var trava = _travas.GetOrAdd(personagemId, _ => new SemaphoreSlim(1, 1));
        await trava.WaitAsync(c);
        try
        {
            if (!_preparos.TryRemove(personagemId, out var preparo))
            {
                return null;
            }

            var skill = CatalogoDeSkills.Obter(preparo.SkillId);
            if (skill == null)
            {
                return Cancelado(preparo, 0, null);
            }

            var custo = RegrasSkills.CustoAoCancelar(skill.Custo);
            var (resultado, recursos) = await _cobranca.TentarCobrarAsync(
                personagemId,
                RecursoDa(skill),
                custo,
                c
            );
            return resultado == ResultadoDaCobranca.Cobrado
                ? Cancelado(preparo, custo, recursos)
                : Cancelado(preparo, 0, null);
        }
        finally
        {
            trava.Release();
        }
    }

    private static RecursoCobravel RecursoDa(DefinicaoDeSkill skill) =>
        skill.Recurso == RecursoDeSkill.Mana ? RecursoCobravel.Mana : RecursoCobravel.Stamina;

    private static ResultadoDoUso Cancelado(
        PreparoEmCurso preparo,
        int custo,
        RecursosMudaram? recursos
    ) =>
        new(
            null,
            Recursos: recursos,
            Cancelada: new SkillCancelada
            {
                Espaco = preparo.Espaco,
                Id = preparo.SkillId,
                Sequencia = preparo.Sequencia,
                CustoCobrado = custo,
            },
            Espaco: preparo.Espaco,
            Sequencia: preparo.Sequencia
        );

    private static string CodigoDaCobranca(ResultadoDaCobranca resultado) =>
        resultado == ResultadoDaCobranca.Caido
            ? CodigosErroSkills.VoceEstaCaido
            : CodigosErroSkills.RecursoInsuficiente;

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

        // Nos 0,4 s da troca de arma não sai skill, como golpe, esquiva e guarda.
        if (_troca?.EmTroca(personagemId) == true)
        {
            return Recusar(CodigosErroSkills.AcaoEmAndamento);
        }

        // Um preparo por vez: até acabar ou ser cancelado, nenhuma outra skill sai.
        if (_preparos.ContainsKey(personagemId))
        {
            return Recusar(CodigosErroSkills.AcaoEmAndamento);
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

        if (skill.PreparoMs > 0)
        {
            // Skill forte: só confere o recurso agora; a cobrança vem ao concluir ou cancelar.
            var cabe = await _cobranca.PodeCobrarAsync(
                personagemId,
                RecursoDa(skill),
                skill.Custo,
                c
            );
            if (cabe != ResultadoDaCobranca.Cobrado)
            {
                return Recusar(CodigoDaCobranca(cabe));
            }

            _preparos[personagemId] = new PreparoEmCurso(
                equipada.InstanciaId,
                pedido.Espaco,
                skill.Id,
                pedido.Sequencia,
                pedido.DirecaoY
            );
            return new ResultadoDoUso(
                null,
                Preparo: new SkillPreparoIniciado
                {
                    Espaco = pedido.Espaco,
                    Id = skill.Id,
                    Sequencia = pedido.Sequencia,
                    PreparoMs = skill.PreparoMs,
                },
                Espaco: pedido.Espaco,
                Sequencia: pedido.Sequencia
            );
        }

        return await ExecutarAsync(
            personagemId,
            equipada,
            ids,
            skill,
            pedido.Espaco,
            pedido.Sequencia,
            pedido.DirecaoY,
            posicao,
            c
        );

        ResultadoDoUso Recusar(string codigo) =>
            new(codigo, Espaco: pedido.Espaco, Sequencia: pedido.Sequencia);
    }

    // Cobra o custo inteiro, começa a recarga e aplica a skill (uso instantâneo ou fim do preparo).
    private async Task<ResultadoDoUso> ExecutarAsync(
        long personagemId,
        ArmaEquipada equipada,
        IReadOnlyList<string> ids,
        DefinicaoDeSkill skill,
        int espaco,
        int sequencia,
        float direcaoY,
        PosicaoNoMundo posicao,
        CancellationToken c
    )
    {
        var (resultado, recursos) = await _cobranca.TentarCobrarAsync(
            personagemId,
            RecursoDa(skill),
            skill.Custo,
            c
        );
        if (resultado != ResultadoDaCobranca.Cobrado)
        {
            return new ResultadoDoUso(
                CodigoDaCobranca(resultado),
                Espaco: espaco,
                Sequencia: sequencia
            );
        }

        _recargas[(equipada.InstanciaId, espaco)] = _agoraMs() + skill.RecargaMs;

        // Os poderes de equipamento mexem na skill depois do custo cobrado: o dano extra vem das
        // cargas gastas agora e as marcas e reduções vêm do que a skill acertou.
        var avisos = new List<PoderDisparou>();
        var uso =
            _poderes == null
                ? ResultadoDosPoderes.Nenhum
                : await _poderes.AoUsarSkillAsync(personagemId, skill, c);
        avisos.AddRange(uso.Avisos);

        var acertos = await AplicarDanoAsync(
            personagemId,
            skill,
            direcaoY,
            posicao,
            uso.MultiplicadorDoDano,
            c
        );
        var barraMudou = false;
        if (_poderes != null && acertos.Length > 0)
        {
            var alvos = acertos.Select(a => a.AlvoId).ToArray();
            var acerto = await _poderes.AoAcertarComSkillAsync(personagemId, skill, alvos, c);
            avisos.AddRange(acerto.Avisos);
            barraMudou = ReduzirRecargas(equipada.InstanciaId, ids, espaco, acerto.Reducoes);
        }

        return new ResultadoDoUso(
            null,
            new SkillUsada
            {
                Espaco = espaco,
                Id = skill.Id,
                Sequencia = sequencia,
                RecargaMs = skill.RecargaMs,
            },
            recursos,
            new SkillAcertou
            {
                AtacanteId = personagemId,
                Id = skill.Id,
                Nome = skill.Nome,
                Sequencia = sequencia,
                Acertos = acertos,
            },
            posicao.Area,
            avisos,
            barraMudou,
            Espaco: espaco,
            Sequencia: sequencia
        );
    }

    // O volume é o cone do golpe básico (cada dono de alvo o mede); o mesmo alvo leva a skill
    // uma vez só e jogador nunca é alvo (sem PvP nesta fatia).
    private async Task<AcertoDeAtaque[]> AplicarDanoAsync(
        long personagemId,
        DefinicaoDeSkill skill,
        float direcaoY,
        PosicaoNoMundo posicao,
        double multiplicadorDosPoderes,
        CancellationToken c
    )
    {
        var itens = await _equipamentos.ObterAsync(personagemId, c);
        var dano = RegrasSkills.ComPoderes(
            RegrasSkills.Dano(
                skill,
                itens.DanoDaArma,
                itens.BonusDanoFisico,
                itens.BonusDanoMagico
            ),
            multiplicadorDosPoderes
        );
        var vistos = new HashSet<long>();
        var acertos = new List<AcertoDeAtaque>();
        foreach (var fonte in _alvos)
        {
            foreach (var alvo in fonte.AlvosNoVolume(posicao.Area, posicao.X, posicao.Z, direcaoY))
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

    // Tira ms da recarga das outras skills da barra que o seletor escolhe (a que acabou de ser
    // usada fica de fora). Devolve se mexeu em alguma, para o dono receber a barra atualizada.
    private bool ReduzirRecargas(
        long armaId,
        IReadOnlyList<string> ids,
        int espacoUsado,
        IReadOnlyList<ReducaoDeRecarga> reducoes
    )
    {
        var mudou = false;
        foreach (var reducao in reducoes)
        {
            for (int espaco = 0; espaco < ids.Count; espaco++)
            {
                var definicao = CatalogoDeSkills.Obter(ids[espaco]);
                if (
                    espaco == espacoUsado
                    || definicao == null
                    || !reducao.Alvo.Corresponde(definicao.Id, definicao.Etiquetas)
                    || !_recargas.TryGetValue((armaId, espaco), out var fim)
                )
                {
                    continue;
                }

                var novo = fim - reducao.Ms;
                if (novo <= _agoraMs())
                {
                    _recargas.TryRemove(new KeyValuePair<(long, int), long>((armaId, espaco), fim));
                }
                else
                {
                    _recargas[(armaId, espaco)] = novo;
                }

                mudou = true;
            }
        }

        return mudou;
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
