using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Features.Economia.UI;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Economia
{
    internal sealed class PontoInteracaoEconomia : IInteragivelNoMundo
    {
        public Transform Transform { get; }
        public string Rotulo { get; }
        public string ChaveJanela { get; }

        public float PosicaoX => Transform != null ? Transform.position.x : 0f;
        public float PosicaoZ => Transform != null ? Transform.position.z : 0f;
        public float RaioInteracao => MarcadorNpcEconomia.DistanciaParaInteragir;

        public bool PodeInteragir() =>
            Transform != null
            && Transform.gameObject.activeInHierarchy
            && !JanelasDoJogo.MenuAberto;

        public void Interagir()
        {
            JanelasDoJogo.Abrir(ChaveJanela);
        }

        public PontoInteracaoEconomia(Transform transform, string rotulo, string chaveJanela)
        {
            Transform = transform;
            Rotulo = rotulo;
            ChaveJanela = chaveJanela;
        }
    }

    /// <summary>
    /// Dicas e tecla E nos marcadores dos comerciantes e ferreiro; abre as janelas correspondentes (#173, #1066).
    /// </summary>
    internal sealed class ControleInteracaoEconomia : MonoBehaviour
    {
        private InputAction _interagir;
        private Label _dica;
        private GameObject _jogador;
        private readonly List<PontoInteracaoEconomia> _pontos = new List<PontoInteracaoEconomia>();

        public void Configurar(
            GameObject jogador,
            VisualElement raizDaInterface,
            params PontoInteracaoEconomia[] pontos
        )
        {
            _jogador = jogador;
            DesregistrarTodos();
            _pontos.Clear();
            if (pontos != null)
            {
                _pontos.AddRange(pontos);
                RegistrarTodos();
            }

            _dica = new Label("");
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _interagir = new InputAction(
                "InteragirEconomia",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _interagir.AddBinding("<Gamepad>/buttonWest");

            if (isActiveAndEnabled)
            {
                _interagir.Enable();
            }
        }

        public void Configurar(
            GameObject jogador,
            Transform vendedor,
            Transform ferreiro,
            VisualElement raizDaInterface
        )
        {
            var pontos = new List<PontoInteracaoEconomia>();
            if (vendedor != null)
            {
                pontos.Add(
                    new PontoInteracaoEconomia(
                        vendedor,
                        "falar com o comerciante",
                        JanelaVendedor.Chave
                    )
                );
            }
            if (ferreiro != null)
            {
                pontos.Add(
                    new PontoInteracaoEconomia(
                        ferreiro,
                        "falar com o ferreiro",
                        JanelaFerreiro.Chave
                    )
                );
            }

            Configurar(jogador, raizDaInterface, pontos.ToArray());
        }

        private void OnEnable()
        {
            _interagir?.Enable();
            RegistrarTodos();
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private void OnDisable()
        {
            _interagir?.Disable();
            DesregistrarTodos();
        }

        private void OnDestroy()
        {
            TeclasDoJogo.Mudou -= AtualizarTecla;
            DesregistrarTodos();
            _interagir?.Dispose();
            _interagir = null;
        }

        private void RegistrarTodos()
        {
            for (var i = 0; i < _pontos.Count; i++)
            {
                RegistroDeInteracao.Registrar(_pontos[i]);
            }
        }

        private void DesregistrarTodos()
        {
            for (var i = 0; i < _pontos.Count; i++)
            {
                RegistroDeInteracao.Desregistrar(_pontos[i]);
            }
        }

        private void Update()
        {
            if (_dica == null || _jogador == null || _pontos.Count == 0)
            {
                return;
            }

            if (!EntradaNoJogo.NoJogo || JanelasDoJogo.MenuAberto)
            {
                MostrarDica(false);
                return;
            }

            var pos = _jogador.transform.position;
            PontoInteracaoEconomia maisProximo = null;
            float menorDistanciaSq = float.MaxValue;

            for (var i = 0; i < _pontos.Count; i++)
            {
                var ponto = _pontos[i];
                if (ponto.Transform == null)
                {
                    continue;
                }

                if (MarcadorNpcEconomia.EstaPerto(pos, ponto.Transform.position))
                {
                    var dSq = DistanciaHorizontal(pos, ponto.Transform.position);
                    if (dSq < menorDistanciaSq)
                    {
                        menorDistanciaSq = dSq;
                        maisProximo = ponto;
                    }
                }
            }

            if (
                maisProximo == null
                || !RegistroDeInteracao.EhOMaisProximo(maisProximo, pos.x, pos.z)
            )
            {
                MostrarDica(false);
                return;
            }

            var rotuloTecla = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir);
            _dica.text = $"{rotuloTecla}: {maisProximo.Rotulo}";
            MostrarDica(true);

            if (_interagir != null && _interagir.WasPressedThisFrame())
            {
                maisProximo.Interagir();
            }
        }

        private static float DistanciaHorizontal(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private void MostrarDica(bool mostrar)
        {
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
