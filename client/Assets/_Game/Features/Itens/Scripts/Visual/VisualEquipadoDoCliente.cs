using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>
    /// O visual equipado de cada personagem da área, como o servidor mandou (sempre o estado
    /// inteiro). Guarda por personagem, não por avatar: a mensagem pode chegar antes de o avatar
    /// existir, e o <see cref="ControleDoVisualEquipado"/> aplica quando ele aparece.
    /// </summary>
    public static class VisualEquipadoDoCliente
    {
        private static IDisposable _assinatura;
        private static readonly Dictionary<long, Entrada> _estados =
            new Dictionary<long, Entrada>();
        private static int _versao;

        public sealed class Entrada
        {
            public VisualEquipado Visual;
            public int Versao;
        }

        public static IReadOnlyDictionary<long, Entrada> Estados => _estados;

        /// <summary>Assina a mensagem uma vez só (desde o início, antes de o mundo existir).</summary>
        public static void Iniciar()
        {
            if (_assinatura == null)
            {
                _assinatura = ConexaoTempoReal.Assinar<VisualEquipado>(
                    ItensMessageIds.VisualEquipado,
                    Receber
                );
            }
        }

        public static void Reiniciar()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            _estados.Clear();
        }

        /// <summary>Troca o que havia do personagem pelo estado novo.</summary>
        public static void Receber(VisualEquipado visual)
        {
            if (visual == null || visual.PersonagemId <= 0)
            {
                return;
            }

            _estados[visual.PersonagemId] = new Entrada { Visual = visual, Versao = ++_versao };
        }
    }
}
