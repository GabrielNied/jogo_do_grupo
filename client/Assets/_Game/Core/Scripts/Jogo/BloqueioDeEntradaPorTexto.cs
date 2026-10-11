using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Outras partes do client (ex.: chat) registram quando atalhos ou o Esc do menu devem
    /// ceder à caixa de texto, sem o Core referenciar funcionalidades.
    /// </summary>
    internal static class BloqueioDeEntradaPorTexto
    {
        private static readonly List<Func<bool>> _bloqueiosDeAtalho = new List<Func<bool>>();

        /// <summary>
        /// Quando <c>true</c>, o <see cref="Jogo.SessaoNoMundo"/> não trata o Esc como menu
        /// neste frame (o campo ou o chat já consumiu).
        /// </summary>
        public static bool EscConsumidoNesteFrame { get; set; }

        public static void RegistrarBloqueioDeAtalhos(Func<bool> condicao)
        {
            if (condicao == null)
            {
                throw new ArgumentNullException(nameof(condicao));
            }

            _bloqueiosDeAtalho.Add(condicao);
        }

        public static bool AtalhosDoJogoBloqueados()
        {
            foreach (var condicao in _bloqueiosDeAtalho)
            {
                if (condicao())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Só para testes EditMode.</summary>
        internal static void LimparRegistros()
        {
            _bloqueiosDeAtalho.Clear();
            EscConsumidoNesteFrame = false;
        }
    }
}
