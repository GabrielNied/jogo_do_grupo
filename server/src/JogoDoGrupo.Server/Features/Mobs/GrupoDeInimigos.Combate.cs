using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Integração de combate, dano, ameaça e reação (parte de <see cref="GrupoDeInimigos"/>).</summary>
public sealed partial class GrupoDeInimigos
{
    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        if (area != Area)
        {
            return [];
        }

        lock (_trava)
        {
            var alvos = new List<AlvoDeCombate>();
            foreach (var inimigo in _inimigos)
            {
                var alvoX = inimigo.X;
                var alvoZ = inimigo.Z;
                if (
                    instanteReferenciaMs > 0
                    && _historico?.TentarObterEm(
                        inimigo.Id,
                        instanteReferenciaMs,
                        out var posicaoHistorica
                    ) == true
                )
                {
                    alvoX = posicaoHistorica.X;
                    alvoZ = posicaoHistorica.Z;
                }

                if (
                    RegrasInimigo.EstaVivo(inimigo.Estado)
                    && RegrasAtaque.EstaNoVolume(
                        origemX,
                        origemZ,
                        direcaoYGraus,
                        alvoX,
                        alvoZ,
                        RegrasInimigo.RaioDoCorpoMetros
                    )
                )
                {
                    alvos.Add(
                        new AlvoDeCombate(
                            inimigo.Id,
                            alvoX,
                            alvoZ,
                            RegrasInimigo.RaioDoCorpoMetros,
                            inimigo.Vida,
                            inimigo.Criatura.VidaMaxima,
                            Y: Altura(inimigo)
                        )
                    );
                }
            }

            return alvos;
        }
    }

    public IReadOnlyList<AlvoDeCombate> AlvosProximos(
        string area,
        double origemX,
        double origemZ,
        double alcanceMetros,
        long instanteReferenciaMs = 0
    )
    {
        if (area != Area)
        {
            return [];
        }

        lock (_trava)
        {
            var alvos = new List<AlvoDeCombate>();
            foreach (var inimigo in _inimigos)
            {
                var alvoX = inimigo.X;
                var alvoZ = inimigo.Z;
                if (
                    instanteReferenciaMs > 0
                    && _historico?.TentarObterEm(
                        inimigo.Id,
                        instanteReferenciaMs,
                        out var posicaoHistorica
                    ) == true
                )
                {
                    alvoX = posicaoHistorica.X;
                    alvoZ = posicaoHistorica.Z;
                }

                var dx = alvoX - origemX;
                var dz = alvoZ - origemZ;
                if (
                    RegrasInimigo.EstaVivo(inimigo.Estado)
                    && Math.Sqrt(dx * dx + dz * dz) - RegrasInimigo.RaioDoCorpoMetros
                        <= alcanceMetros
                )
                {
                    alvos.Add(
                        new AlvoDeCombate(
                            inimigo.Id,
                            alvoX,
                            alvoZ,
                            RegrasInimigo.RaioDoCorpoMetros,
                            inimigo.Vida,
                            inimigo.Criatura.VidaMaxima,
                            Y: Altura(inimigo)
                        )
                    );
                }
            }

            return alvos;
        }
    }

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        if (dano <= 0)
        {
            return false;
        }

        ResultadoDeDano aplicadoAgora;
        PontoAtacado ponto;
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            // Na volta ele fica imune (Evade): sem isso o jogador bate de graça num inimigo
            // que não revida e que depois sobe de vida cheia (issue #488).
            if (
                inimigo == null
                || !RegrasInimigo.EstaVivo(inimigo.Estado)
                || inimigo.Estado == EstadoDeInimigo.Retorno
            )
            {
                return false;
            }

            var aplicado = Math.Min(dano, inimigo.Vida);
            inimigo.Vida -= aplicado;
            inimigo.Participantes.Add(atacanteId);
            inimigo.Ameacas.AdicionarDano(atacanteId, aplicado);
            inimigo.VidaMudou = true;
            aplicadoAgora = new ResultadoDeDano(
                inimigo.Id,
                aplicado,
                inimigo.Vida,
                inimigo.Criatura.VidaMaxima
            );
            if (inimigo.Vida <= 0)
            {
                Matar(inimigo);
            }

            ponto = new PontoAtacado(inimigo.X, inimigo.Z);
        }

        resultado = aplicadoAgora;
        // Fora da trava: a cadeia trava os outros grupos, e dois grupos não podem se esperar.
        AoSerAtacado?.Invoke(this, atacanteId, ponto.X, ponto.Z);
        return true;
    }

    private readonly record struct PontoAtacado(double X, double Z);

    /// <summary>
    /// Avisa que um inimigo deste grupo foi atacado (grupo, atacante, posição do atacado). Quem
    /// liga a puxada em cadeia assina (veja <see cref="PuxadaEmCadeia"/>); roda fora da trava.
    /// </summary>
    public Action<GrupoDeInimigos, long, double, double>? AoSerAtacado { get; set; }

    /// <summary>
    /// Puxada em cadeia (#915): um vizinho da mesma espécie foi atacado em (x, z). Se algum
    /// inimigo vivo deste grupo está a até <paramref name="raioMetros"/> dali, o atacante passa
    /// a ser visto por ele. Devolve se o grupo foi puxado.
    /// </summary>
    public bool Puxar(long atacanteId, int criaturaId, double x, double z, double raioMetros)
    {
        if (_definicao.Id != criaturaId)
        {
            return false;
        }

        lock (_trava)
        {
            if (
                !_inimigos.Any(i =>
                    RegrasInimigo.EstaVivo(i.Estado)
                    && RegrasInimigo.Distancia(i.X, i.Z, x, z) <= raioMetros
                )
            )
            {
                return false;
            }

            _puxados.Add(atacanteId);
            return true;
        }
    }

    /// <summary>Espécie do grupo (para a puxada em cadeia).</summary>
    public int CriaturaId => _definicao.Id;

    /// <summary>
    /// Reação ao acerto (Combate): um acerto no preparo cancela o golpe, que não chega ao
    /// contato, e o inimigo fica sem atacar por <paramref name="duracaoMs"/> (a recuperação
    /// dele passa a ser essa pausa). A recuperação de um golpe que já encostou continua.
    /// </summary>
    public bool Interromper(long alvoId, int duracaoMs)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            if (inimigo == null || inimigo.Estado != EstadoDeInimigo.Ataque || !inimigo.Preparando)
            {
                return false;
            }

            inimigo.Preparando = false;
            inimigo.FaseAteMs = _relogioMs() + duracaoMs;
            return true;
        }
    }

    /// <summary>
    /// Força o alvo a focar no provocador na hora e iguala sua ameaça ao maior da tabela por 4 s.
    /// </summary>
    public bool Provocar(long alvoId, long provocadorId)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            if (
                inimigo == null
                || !RegrasInimigo.EstaVivo(inimigo.Estado)
                || inimigo.Estado == EstadoDeInimigo.Retorno
            )
            {
                return false;
            }

            inimigo.Ameacas.Provocar(provocadorId, _relogioMs());
            inimigo.Alvo = provocadorId;
            inimigo.Participantes.Add(provocadorId);
            return true;
        }
    }

    /// <summary>
    /// Quem curou um aliado que já está na luta com o inimigo passa a participar dela (#909): loot
    /// e experiência também são dele. Inimigo que voltou para casa (imune) ou morto não conta.
    /// </summary>
    public void RegistrarCura(long curadorId, long curadoId)
    {
        if (curadorId == curadoId || curadorId == RegrasInimigo.SemAlvo)
        {
            return;
        }

        lock (_trava)
        {
            foreach (var inimigo in _inimigos)
            {
                if (
                    RegrasInimigo.EstaVivo(inimigo.Estado)
                    && inimigo.Estado != EstadoDeInimigo.Retorno
                    && inimigo.Participantes.Contains(curadoId)
                )
                {
                    inimigo.Participantes.Add(curadorId);
                }
            }
        }
    }

    /// <summary>Retorna a ameaça de um personagem sobre um inimigo (para consultas e testes).</summary>
    public double ObterAmeaca(long alvoId, long personagemId)
    {
        lock (_trava)
        {
            var inimigo = Array.Find(_inimigos, i => i.Id == alvoId);
            return inimigo?.Ameacas.ObterAmeaca(personagemId) ?? 0.0;
        }
    }
}
