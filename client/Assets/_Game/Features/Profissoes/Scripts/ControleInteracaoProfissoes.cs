using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Client.Features.Profissoes.UI;
using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>Um ponto do mundo com que se interage: um ponto de coleta (NoId) ou a forja.</summary>
    internal struct PontoDeInteracaoProfissoes
    {
        public Transform Transform;
        public PontoDeColeta Coleta;

        public bool EhForja => Coleta == null;
    }

    /// <summary>
    /// Dica e tecla E nos pontos de coleta e na forja (#192). Esconde o ponto esgotado, pede a
    /// coleta ao servidor e abre a janela de Profissões na forja.
    /// </summary>
    internal sealed class ControleInteracaoProfissoes : MonoBehaviour
    {
        private const float PausaEntreColetas = 0.5f;

        private readonly List<PontoDeInteracaoProfissoes> _pontos =
            new List<PontoDeInteracaoProfissoes>();
        private InputAction _interagir;
        private Label _dica;
        private GameObject _jogador;
        private float _liberaEm;

        public void Configurar(
            GameObject jogador,
            VisualElement raizDaInterface,
            IEnumerable<PontoDeInteracaoProfissoes> pontos
        )
        {
            _jogador = jogador;
            _pontos.Clear();
            _pontos.AddRange(pontos);

            _dica = new Label("");
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _interagir = new InputAction(
                "InteragirProfissoes",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            _interagir.AddBinding("<Gamepad>/buttonWest");
            TeclasDoJogo.Mudou += AtualizarTecla;
            if (isActiveAndEnabled)
                _interagir.Enable();
        }

        private void OnEnable() => _interagir?.Enable();

        private void OnDisable() => _interagir?.Disable();

        private void OnDestroy()
        {
            TeclasDoJogo.Mudou -= AtualizarTecla;
            _interagir?.Dispose();
            _interagir = null;
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private void Update()
        {
            if (_dica == null || _jogador == null)
                return;

            // O ponto esgotado some para todos até voltar.
            foreach (var ponto in _pontos)
            {
                if (ponto.Transform != null && ponto.Coleta != null)
                    ponto.Transform.gameObject.SetActive(
                        ControleDeProfissoes.NoDisponivel(ponto.Coleta.Id)
                    );
            }

            if (!EntradaNoJogo.NoJogo || JanelasDoJogo.MenuAberto)
            {
                MostrarDica(false);
                return;
            }

            var maisProximo = BuscarMaisProximo(_jogador.transform.position);
            if (maisProximo == null)
            {
                MostrarDica(false);
                return;
            }

            var ponto2 = maisProximo.Value;
            var tecla = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir);
            _dica.text =
                tecla
                + ": "
                + (
                    ponto2.EhForja
                        ? TextosProfissoes.RotuloDaForja
                        : TextosProfissoes.DicaDoPonto(
                            ponto2.Coleta,
                            ControleDeProfissoes.Aprendeu(ponto2.Coleta.Profissao)
                        )
                );
            MostrarDica(true);

            if (_interagir == null || !_interagir.WasPressedThisFrame())
                return;

            if (ponto2.EhForja)
            {
                JanelasDoJogo.Abrir(JanelaProfissoes.Chave);
                return;
            }

            if (Time.realtimeSinceStartup < _liberaEm)
                return;
            _liberaEm = Time.realtimeSinceStartup + PausaEntreColetas;
            ControleDeProfissoes.Coletar(ponto2.Coleta.Id);
        }

        private PontoDeInteracaoProfissoes? BuscarMaisProximo(Vector3 posicao)
        {
            PontoDeInteracaoProfissoes? melhor = null;
            var menor = float.MaxValue;
            foreach (var ponto in _pontos)
            {
                if (ponto.Transform == null || !ponto.Transform.gameObject.activeSelf)
                    continue;
                if (!MarcadorDeProfissoes.EstaPerto(posicao, ponto.Transform.position))
                    continue;

                var dx = posicao.x - ponto.Transform.position.x;
                var dz = posicao.z - ponto.Transform.position.z;
                var d = dx * dx + dz * dz;
                if (d < menor)
                {
                    menor = d;
                    melhor = ponto;
                }
            }

            return melhor;
        }

        private void MostrarDica(bool mostrar) =>
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
