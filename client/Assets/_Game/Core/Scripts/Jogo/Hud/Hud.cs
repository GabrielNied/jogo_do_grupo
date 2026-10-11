using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Moldura do HUD: o Core cria uma região por parte da tela ao entrar no mundo
    /// (<see cref="EntradaNoJogo.Entrar"/>) e remove ao sair. Cada funcionalidade põe o seu
    /// elemento na sua região, sem conhecer as outras. Regras em
    /// <c>docs/features/interface.md</c>, seção "HUD do jogo".
    /// </summary>
    /// <remarks>
    /// O HUD só mostra o que o servidor já enviou e nada nele recebe clique: as regiões
    /// ignoram o mouse, e um elemento que precise de clique (ex.: aceitar convite) liga o seu
    /// próprio <c>pickingMode</c>. O HUD fica atrás das janelas e do menu do Esc e continua
    /// na tela com eles abertos.
    /// <code>
    /// var regiao = Hud.Regiao(RegiaoDoHud.InferiorEsquerda);
    /// regiao?.Add(meuPainel);
    /// </code>
    /// </remarks>
    public static class Hud
    {
        internal const string NomeElemento = "core.hud";

        // Frequência com que as linhas vencidas das mensagens de sistema saem da tela.
        private const long IntervaloAtualizacaoMs = 100;

        private static readonly Dictionary<RegiaoDoHud, VisualElement> _regioes =
            new Dictionary<RegiaoDoHud, VisualElement>();

        private static VisualElement _raiz;
        private static PainelDeAvisos _avisos;

        /// <summary>Se o HUD está na tela (entre a entrada e a saída do mundo).</summary>
        public static bool Montado => _raiz != null;

        /// <summary>
        /// Contêiner da região, ou <c>null</c> fora do mundo. O elemento devolvido ignora o
        /// mouse; o que a funcionalidade puser dentro também deveria ignorar, salvo pedido.
        /// </summary>
        public static VisualElement Regiao(RegiaoDoHud regiao)
        {
            return _regioes.TryGetValue(regiao, out var elemento) ? elemento : null;
        }

        /// <summary>Elemento raiz do HUD, ou <c>null</c> fora do mundo.</summary>
        internal static VisualElement Raiz => _raiz;

        /// <summary>Mensagens de sistema na tela, ou <c>null</c> fora do mundo.</summary>
        internal static PainelDeAvisos Avisos => _avisos;

        /// <summary>
        /// Cria o HUD atrás de tudo o que já está em <paramref name="container"/>. Se já
        /// existir, remove antes.
        /// </summary>
        /// <param name="relogio">
        /// Hora atual em segundos para as mensagens de sistema; <c>null</c> usa o tempo real
        /// do Unity (o jogo é online e não pausa).
        /// </param>
        internal static void Montar(VisualElement container, Func<double> relogio = null)
        {
            Remover();
            if (container == null)
            {
                Debug.LogWarning("[Hud] A interface ainda não foi criada; o HUD não foi montado.");
                return;
            }

            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.Hud);
            _raiz.pickingMode = PickingMode.Ignore;

            foreach (RegiaoDoHud regiao in Enum.GetValues(typeof(RegiaoDoHud)))
            {
                var elemento = new VisualElement { name = NomeDaRegiao(regiao) };
                elemento.AddToClassList(EstiloCore.HudRegiao);
                elemento.AddToClassList(ClasseDaRegiao(regiao));
                elemento.pickingMode = PickingMode.Ignore;
                _regioes[regiao] = elemento;
                _raiz.Add(elemento);
            }

            var lista = new ListaDeAvisos(relogio ?? (() => Time.realtimeSinceStartupAsDouble));
            _avisos = new PainelDeAvisos(_regioes[RegiaoDoHud.InferiorCentro], lista);
            _raiz.schedule.Execute(_avisos.Atualizar).Every(IntervaloAtualizacaoMs);

            // Atrás das telas, janelas e menu que já estiverem (e dos que vierem depois).
            container.Insert(0, _raiz);
        }

        /// <summary>Tira o HUD da tela, com tudo o que as funcionalidades puseram nas regiões.</summary>
        internal static void Remover()
        {
            // Solta cada região: quem guardou a referência não continua preso ao HUD antigo.
            foreach (var regiao in _regioes.Values)
            {
                regiao.RemoveFromHierarchy();
            }

            _raiz?.RemoveFromHierarchy();
            _raiz = null;
            _avisos = null;
            _regioes.Clear();
        }

        internal static string NomeDaRegiao(RegiaoDoHud regiao)
        {
            return $"core.hud.{regiao}";
        }

        internal static string ClasseDaRegiao(RegiaoDoHud regiao)
        {
            switch (regiao)
            {
                case RegiaoDoHud.InferiorEsquerda:
                    return EstiloCore.HudInferiorEsquerda;
                case RegiaoDoHud.LateralEsquerda:
                    return EstiloCore.HudLateralEsquerda;
                case RegiaoDoHud.TopoCentro:
                    return EstiloCore.HudTopoCentro;
                case RegiaoDoHud.CentroAvisos:
                    return EstiloCore.HudCentroAvisos;
                case RegiaoDoHud.InferiorCentro:
                    return EstiloCore.HudInferiorCentro;
                case RegiaoDoHud.SuperiorDireita:
                    return EstiloCore.HudSuperiorDireita;
                default:
                    throw new ArgumentOutOfRangeException(nameof(regiao), regiao, null);
            }
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa antes de qualquer registro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _raiz = null;
            _avisos = null;
            _regioes.Clear();
        }
    }
}
