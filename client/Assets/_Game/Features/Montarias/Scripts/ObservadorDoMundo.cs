using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Montarias.UI;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Montarias
{
    /// <summary>
    /// Observa a entrada do jogador no mundo e inicializa o controle de montaria
    /// e o botão correspondente no HUD.
    /// </summary>
    internal sealed class ObservadorDoMundo : MonoBehaviour
    {
        private const string NomeObservador = "Montarias.Observador";

        private static ObservadorDoMundo _instancia;
        private GameObject _jogadorAtendido;
        private BotaoMontariaHud _botaoHud;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDoMundo>();
            objeto.AddComponent<ControleDeMontaria>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null)
            {
                if (_botaoHud != null)
                {
                    _botaoHud.Dispose();
                    _botaoHud = null;
                }

                _jogadorAtendido = null;
                return;
            }

            if (jogador == _jogadorAtendido)
            {
                return;
            }

            _jogadorAtendido = jogador;
            try
            {
                MontarHud();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void MontarHud()
        {
            var regiao =
                Hud.Regiao(RegiaoDoHud.BarraDeAcao) ?? Hud.Regiao(RegiaoDoHud.InferiorCentro);
            if (regiao == null)
            {
                return;
            }

            _botaoHud?.Dispose();
            _botaoHud = new BotaoMontariaHud(regiao, Hud.Regiao(RegiaoDoHud.InferiorCentro));
        }

        private void OnDestroy()
        {
            _botaoHud?.Dispose();
            _botaoHud = null;
        }
    }
}
