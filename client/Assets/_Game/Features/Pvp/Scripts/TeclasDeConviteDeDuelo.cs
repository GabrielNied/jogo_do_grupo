using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Pvp
{
    /// <summary>
    /// As teclas de responder o convite de duelo por cima do jogo. Y e N são atalhos das janelas
    /// de Conquistas e Talentos, então ficam F1 e F2, como nos convites de Social (#1198). A
    /// constante é própria da feature: funcionalidades não se referenciam (regra 2).
    /// </summary>
    internal static class TeclasDeConviteDeDuelo
    {
        public const string RotuloAceitar = "F1";
        public const string RotuloRecusar = "F2";
        public const string TextoAceitar = "Aceitar (" + RotuloAceitar + ")";
        public const string TextoRecusar = "Recusar (" + RotuloRecusar + ")";

        /// <summary>1 se aceitar foi apertada neste quadro, -1 se recusar, 0 se nenhuma.</summary>
        public static int Resposta(Keyboard teclado)
        {
            if (teclado == null)
                return 0;
            if (teclado.f1Key.wasPressedThisFrame)
                return 1;
            return teclado.f2Key.wasPressedThisFrame ? -1 : 0;
        }
    }
}
