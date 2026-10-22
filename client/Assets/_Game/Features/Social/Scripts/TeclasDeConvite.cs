using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// As teclas de responder convite por cima do jogo (grupo, troca e amizade). Y e N são
    /// atalhos das janelas de Conquistas e Talentos, então ficam F1 e F2, que nenhuma janela
    /// nem ação do mundo usa (#1198).
    /// </summary>
    internal static class TeclasDeConvite
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
