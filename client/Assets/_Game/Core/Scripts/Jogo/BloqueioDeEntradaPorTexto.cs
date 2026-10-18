using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Centraliza a consulta e notificação de bloqueio de entrada e atalhos durante a digitação
    /// de texto no client (chat, janela de reporte F9, buscas de inventário/loja, diálogos, etc.).
    /// </summary>
    public static class BloqueioDeEntradaPorTexto
    {
        private static readonly List<Func<bool>> _bloqueiosDeAtalho = new List<Func<bool>>();
        private static bool _ultimoEstadoDigitacao;

        /// <summary>
        /// Quando <c>true</c>, o <see cref="Jogo.SessaoNoMundo"/> não trata o Esc como menu
        /// neste frame (o campo ou o chat já consumiu).
        /// </summary>
        public static bool EscConsumidoNesteFrame { get; set; }

        /// <summary>
        /// Notifica quando o estado de digitação ativa muda.
        /// </summary>
        public static event Action<bool> DigitacaoMudou;

        /// <summary>
        /// Registra uma condição personalizada que bloqueia atalhos e movimento enquanto for <c>true</c>.
        /// </summary>
        public static void RegistrarBloqueioDeAtalhos(Func<bool> condicao)
        {
            if (condicao == null)
            {
                throw new ArgumentNullException(nameof(condicao));
            }

            _bloqueiosDeAtalho.Add(condicao);
        }

        /// <summary>
        /// Verifica se alguma condição registrada exige o bloqueio de atalhos e movimento.
        /// </summary>
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

        /// <summary>
        /// Devolve <c>true</c> se há foco de digitação num campo de texto ou se
        /// algum bloqueio por texto (ex.: chat aberto, janela de reporte F9) está ativo.
        /// </summary>
        public static bool DigitacaoAtiva(VisualElement raizDoHud = null)
        {
            return FocoEmCampoDeTexto.TemCampoComFoco(raizDoHud) || AtalhosDoJogoBloqueados();
        }

        /// <summary>
        /// Alias para <see cref="DigitacaoAtiva(VisualElement)"/>.
        /// </summary>
        public static bool EntradaDeTextoAtiva(VisualElement raizDoHud = null) =>
            DigitacaoAtiva(raizDoHud);

        /// <summary>
        /// Atualiza o estado de digitação e dispara <see cref="DigitacaoMudou"/> se houver alteração.
        /// </summary>
        public static void AtualizarEstado(bool ativa)
        {
            if (_ultimoEstadoDigitacao != ativa)
            {
                _ultimoEstadoDigitacao = ativa;
                DigitacaoMudou?.Invoke(ativa);
            }
        }

        /// <summary>
        /// Registra uma raiz de interface adicional para monitoramento de foco em campos de texto.
        /// </summary>
        public static void RegistrarRaiz(VisualElement raiz) =>
            FocoEmCampoDeTexto.RegistrarRaiz(raiz);

        /// <summary>
        /// Remove uma raiz registrada anteriormente.
        /// </summary>
        public static void DesregistrarRaiz(VisualElement raiz) =>
            FocoEmCampoDeTexto.DesregistrarRaiz(raiz);

        /// <summary>
        /// Registra um <see cref="UIDocument"/> para monitoramento de foco em campos de texto.
        /// </summary>
        public static void RegistrarDocumento(UIDocument documento) =>
            FocoEmCampoDeTexto.RegistrarDocumento(documento);

        /// <summary>
        /// Remove o monitoramento de um <see cref="UIDocument"/>.
        /// </summary>
        public static void DesregistrarDocumento(UIDocument documento) =>
            FocoEmCampoDeTexto.DesregistrarDocumento(documento);

        /// <summary>Só para testes EditMode.</summary>
        internal static void LimparRegistros()
        {
            _bloqueiosDeAtalho.Clear();
            EscConsumidoNesteFrame = false;
            _ultimoEstadoDigitacao = false;
            FocoEmCampoDeTexto.LimparRegistros();
        }
    }
}
