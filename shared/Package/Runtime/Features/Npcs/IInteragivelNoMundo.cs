#nullable enable
namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Objeto no mundo com o qual o jogador pode interagir (NPC, baú, barraca etc.).
    /// Permite ao client escolher apenas o interagível mais próximo ao alcance (#1195).
    /// </summary>
    public interface IInteragivelNoMundo
    {
        /// <summary>Posição horizontal X no mundo.</summary>
        float PosicaoX { get; }

        /// <summary>Posição horizontal Z no mundo.</summary>
        float PosicaoZ { get; }

        /// <summary>Raio máximo de alcance em metros para interagir.</summary>
        float RaioInteracao { get; }

        /// <summary>Se o objeto está em condições de responder à interação no momento.</summary>
        bool PodeInteragir();

        /// <summary>Dispara a ação de interação.</summary>
        void Interagir();
    }
}
