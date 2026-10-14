using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mundo;

/// <summary>
/// Teleporte do comando de teste. Usa o estado da área atual e avisa quem está nela. Fora do
/// mundo, ou com a pessoa ainda sem entrar, não move ninguém.
/// </summary>
internal sealed class TeleporteDeTeste(
    EstadoDasAreasDoMundo<SessaoTempoReal> areas,
    ServidorTempoReal tempoReal
) : ITeleporteDeTeste
{
    /// <summary>Abaixo disto o comando recusa, para um número solto não enterrar o personagem.</summary>
    public const float AlturaMinima = -50f;

    /// <summary>Acima disto o comando recusa.</summary>
    public const float AlturaMaxima = 300f;

    public (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z)
    {
        var recusa = Recusa(x, y, z);
        if (recusa != null)
        {
            return (false, recusa);
        }

        var localizado = areas.Obter(personagemId);
        if (localizado == null)
        {
            return (false, "Você não está no mundo.");
        }

        var (area, avatar) = localizado.Value;
        var estadoArea = areas.ObterOuCriar(area);

        var sequencia = estadoArea.ForcarPosicao(
            personagemId,
            new PosicaoNaCidade(x, y, z, avatar.Posicao.RotacaoY)
        );
        if (sequencia == null)
        {
            return (false, "Você não está no mundo.");
        }

        tempoReal.Enviar(
            avatar.Sessao,
            MundoMessageIds.PosicaoForcada,
            new PosicaoForcada
            {
                X = x,
                Y = y,
                Z = z,
                RotacaoY = avatar.Posicao.RotacaoY,
                Sequencia = sequencia.Value,
            }
        );

        var replica = new PosicaoJogador
        {
            PersonagemId = personagemId,
            X = x,
            Y = y,
            Z = z,
            RotacaoY = avatar.Posicao.RotacaoY,
        };
        foreach (var outro in estadoArea.Presentes())
        {
            if (!ReferenceEquals(outro.Sessao, avatar.Sessao))
            {
                tempoReal.Enviar(
                    outro.Sessao,
                    MundoMessageIds.PosicaoJogador,
                    replica,
                    EntregaTempoReal.Sequenciada,
                    personagemId
                );
            }
        }

        return (true, "Teleportado.");
    }

    /// <summary>Nulo quando a posição pode ser aplicada; senão, a frase de recusa.</summary>
    internal static string? Recusa(float x, float y, float z)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(z))
        {
            return "Posição inválida.";
        }

        if (
            MathF.Abs(x) > ValidadorDeMovimento.LimiteHorizontal
            || MathF.Abs(z) > ValidadorDeMovimento.LimiteHorizontal
            || y < AlturaMinima
            || y > AlturaMaxima
        )
        {
            return "Posição fora do mundo.";
        }

        return null;
    }
}
