using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// O boneco de treino fixo da cidade (<see cref="ConfigBonecoDeTreino"/>). Leva golpes de
/// qualquer um, chega a 0 de vida e então fica fora de combate até voltar ao máximo, o que
/// acontece <see cref="ConfigBonecoDeTreino.SegundosParaRecuperar"/> segundos depois do último
/// golpe que ele levou. A volta é calculada na hora da consulta (não há relógio de fundo).
/// Seguro para várias threads.
/// </summary>
public sealed class BonecoDeTreino(Func<long> relogioMs) : IAlvosDeCombate
{
    private readonly Lock _trava = new();
    private int _vida = ConfigBonecoDeTreino.Vida;
    private long _ultimoGolpeMs;

    public BonecoDeTreino()
        : this(() => Environment.TickCount64) { }

    /// <summary>A vida do boneco agora, já com a recuperação.</summary>
    public int VidaAtual
    {
        get
        {
            lock (_trava)
            {
                return Recuperar();
            }
        }
    }

    public IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus,
        long instanteReferenciaMs = 0
    )
    {
        if (area != AreasDoMundo.Cidade)
        {
            return [];
        }

        int vida;
        lock (_trava)
        {
            vida = Recuperar();
        }

        if (
            vida <= 0
            || !RegrasAtaque.EstaNoVolume(
                origemX,
                origemZ,
                direcaoYGraus,
                ConfigBonecoDeTreino.X,
                ConfigBonecoDeTreino.Z,
                ConfigBonecoDeTreino.Raio
            )
        )
        {
            return [];
        }

        return
        [
            new AlvoDeCombate(
                ConfigBonecoDeTreino.AlvoId,
                ConfigBonecoDeTreino.X,
                ConfigBonecoDeTreino.Z,
                ConfigBonecoDeTreino.Raio,
                vida,
                ConfigBonecoDeTreino.Vida
            ),
        ];
    }

    public bool TentarAplicarDano(
        long alvoId,
        int dano,
        long atacanteId,
        out ResultadoDeDano resultado
    )
    {
        resultado = default;
        if (alvoId != ConfigBonecoDeTreino.AlvoId || dano <= 0)
        {
            return false;
        }

        lock (_trava)
        {
            var vida = Recuperar();
            if (vida <= 0)
            {
                return false;
            }

            var aplicado = Math.Min(dano, vida);
            _vida = vida - aplicado;
            _ultimoGolpeMs = relogioMs();
            resultado = new ResultadoDeDano(
                ConfigBonecoDeTreino.AlvoId,
                aplicado,
                _vida,
                ConfigBonecoDeTreino.Vida
            );
            return true;
        }
    }

    // Chamar com a trava.
    private int Recuperar()
    {
        if (
            _vida < ConfigBonecoDeTreino.Vida
            && relogioMs() - _ultimoGolpeMs >= ConfigBonecoDeTreino.SegundosParaRecuperar * 1000.0
        )
        {
            _vida = ConfigBonecoDeTreino.Vida;
        }

        return _vida;
    }
}
