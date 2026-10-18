using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Detecta aliados caídos por perto no mundo e permite levantar o aliado segurando a tecla
    /// de interagir (E) por 3 segundos (RegrasQueda.TempoParaLevantarSegundos).
    /// </summary>
    internal sealed class ControleSocorro : MonoBehaviour
    {
        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly Dictionary<long, EstadoDeQueda> _caidos =
            new Dictionary<long, EstadoDeQueda>();
        private long _meuPersonagemId;
        private InputAction _interagir;
        private Label _dica;
        private VisualElement _trilho;
        private VisualElement _preenchimento;
        private long _alvoAtualId;
        private float _tempoCanalizando;
        private bool _canalizando;

        public void Configurar(long meuPersonagemId, VisualElement container)
        {
            _meuPersonagemId = meuPersonagemId;

            _dica = new Label();
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            _dica.style.display = DisplayStyle.None;
            container.Add(_dica);

            _trilho = new VisualElement { name = "TrilhoSocorro" };
            _trilho.AddToClassList(EstiloCore.BarraCarregamento);
            _trilho.style.width = 180;
            _trilho.style.alignSelf = Align.Center;
            _trilho.style.marginTop = 6;
            _trilho.style.display = DisplayStyle.None;

            _preenchimento = new VisualElement { name = "PreenchimentoSocorro" };
            _preenchimento.AddToClassList(EstiloCore.BarraCarregamentoPreenchimento);
            _preenchimento.style.width = Length.Percent(0);
            _trilho.Add(_preenchimento);
            container.Add(_trilho);

            _interagir = new InputAction(
                "SocorroInteragir",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _interagir.AddBinding("<Gamepad>/buttonWest");
            if (isActiveAndEnabled)
            {
                _interagir.Enable();
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDeQueda>(
                    CombateMessageIds.EstadoDeQueda,
                    AoMudarEstadoDeQueda
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AcaoRecusada>(
                    CombateMessageIds.AcaoRecusada,
                    AoRecusarAcao
                )
            );
        }

        private void OnEnable() => _interagir?.Enable();

        private void OnDisable() => _interagir?.Disable();

        private void OnDestroy()
        {
            if (_canalizando)
            {
                Cancelar();
            }

            foreach (var a in _assinaturas)
            {
                a.Dispose();
            }
            _assinaturas.Clear();

            TeclasDoJogo.Mudou -= AtualizarTecla;
            _interagir?.Dispose();
            _interagir = null;
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private void Update()
        {
            if (_dica == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            var minhaPosicao = jogador.transform.position;
            long alvoMaisProximo = 0;
            float menorDistancia = float.MaxValue;

            foreach (var kvp in _caidos)
            {
                var id = kvp.Key;
                var avatar = GameObject.Find("Jogador_" + id);
                if (avatar == null)
                {
                    continue;
                }

                var dist = Vector3.Distance(minhaPosicao, avatar.transform.position);
                if (dist <= (float)RegrasQueda.AlcanceParaLevantarMetros && dist < menorDistancia)
                {
                    menorDistancia = dist;
                    alvoMaisProximo = id;
                }
            }

            if (alvoMaisProximo == 0)
            {
                if (_canalizando)
                {
                    Cancelar();
                }
                _dica.style.display = DisplayStyle.None;
                _trilho.style.display = DisplayStyle.None;
                return;
            }

            _dica.style.display = DisplayStyle.Flex;
            _dica.text =
                "Segure " + TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir) + ": levantar aliado";

            if (!BloqueioDeEntradaPorTexto.EntradaDeTextoAtiva() && _interagir.IsPressed())
            {
                if (!_canalizando)
                {
                    _canalizando = true;
                    _alvoAtualId = alvoMaisProximo;
                    _tempoCanalizando = 0f;
                    ConexaoTempoReal.Enviar(
                        CombateMessageIds.ComecarALevantar,
                        new ComecarALevantar { AlvoId = _alvoAtualId },
                        EntregaTempoReal.Confiavel
                    );
                }

                _tempoCanalizando += Time.deltaTime;
                var progresso = Mathf.Clamp01(
                    _tempoCanalizando / (float)RegrasQueda.TempoParaLevantarSegundos
                );
                _trilho.style.display = DisplayStyle.Flex;
                _preenchimento.style.width = Length.Percent(progresso * 100f);
            }
            else
            {
                if (_canalizando)
                {
                    Cancelar();
                }
                _trilho.style.display = DisplayStyle.None;
            }
        }

        private void Cancelar()
        {
            if (_canalizando)
            {
                _canalizando = false;
                _alvoAtualId = 0;
                _tempoCanalizando = 0f;
                ConexaoTempoReal.Enviar(
                    CombateMessageIds.CancelarLevantar,
                    new CancelarLevantar(),
                    EntregaTempoReal.Confiavel
                );
            }

            if (_trilho != null)
            {
                _trilho.style.display = DisplayStyle.None;
            }
        }

        private void AoMudarEstadoDeQueda(EstadoDeQueda estado)
        {
            if (estado == null)
            {
                return;
            }

            if (estado.PersonagemId == _meuPersonagemId)
            {
                if (estado.Estado == FaseDeQueda.Caido && _canalizando)
                {
                    Cancelar();
                }
                return;
            }

            if (estado.Estado == FaseDeQueda.Caido || estado.Estado == FaseDeQueda.Levantando)
            {
                _caidos[estado.PersonagemId] = estado;
            }
            else
            {
                _caidos.Remove(estado.PersonagemId);
                if (_alvoAtualId == estado.PersonagemId && _canalizando)
                {
                    Cancelar();
                }
            }
        }

        private void AoRecusarAcao(AcaoRecusada recusa)
        {
            if (_canalizando)
            {
                _canalizando = false;
                _alvoAtualId = 0;
                _tempoCanalizando = 0f;
                if (_trilho != null)
                {
                    _trilho.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
