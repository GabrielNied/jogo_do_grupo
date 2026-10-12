using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Arrastar e soltar por eventos de ponteiro (UI Toolkit). O ponteiro fica capturado no
    /// elemento onde o botão esquerdo desceu; passando de <see cref="LimiarPx"/> começa o arraste
    /// (um "fantasma" segue o mouse na raiz da interface) e, ao soltar, devolve o alvo achado por
    /// <c>alvoEm</c>. Só mexe na interface: quem decide o que a soltura vira é a
    /// <see cref="LogicaInventario"/> e, no fim, o servidor. Fica em Itens até haver segundo uso
    /// (então vai para Core/UI).
    /// </summary>
    public sealed class ControleDeArrasto
    {
        /// <summary>Distância em pixels antes de um clique virar arraste.</summary>
        public const float LimiarPx = 6f;

        private readonly VisualElement _raiz;
        private readonly Func<Vector2, AlvoSoltura> _alvoEm;
        private readonly Action<OrigemArrasto, AlvoSoltura> _aoSoltar;
        private readonly Action<OrigemArrasto, bool> _aoMudar;
        private readonly Action<OrigemArrasto, Vector2> _aoMover;

        private VisualElement _fantasma;
        private VisualElement _elemento;
        private OrigemArrasto _origem;
        private Func<VisualElement> _criarFantasma;
        private Vector2 _inicio;
        private int _pointerId;
        private bool _pressionado;

        /// <param name="raiz">Elemento da interface que recebe o fantasma.</param>
        /// <param name="alvoEm">Acha o alvo debaixo da posição (em coordenadas do painel).</param>
        /// <param name="aoSoltar">Chamado ao soltar um arraste.</param>
        /// <param name="aoMudar">Chamado com <c>true</c> ao começar e <c>false</c> ao terminar.</param>
        /// <param name="aoMover">Chamado a cada movimento do arraste.</param>
        public ControleDeArrasto(
            VisualElement raiz,
            Func<Vector2, AlvoSoltura> alvoEm,
            Action<OrigemArrasto, AlvoSoltura> aoSoltar,
            Action<OrigemArrasto, bool> aoMudar,
            Action<OrigemArrasto, Vector2> aoMover
        )
        {
            _raiz = raiz;
            _alvoEm = alvoEm;
            _aoSoltar = aoSoltar;
            _aoMudar = aoMudar;
            _aoMover = aoMover;
        }

        public bool Arrastando => _fantasma != null;

        /// <summary>Faz o <paramref name="elemento"/> arrastável como <paramref name="origem"/>.</summary>
        public void Registrar(
            VisualElement elemento,
            OrigemArrasto origem,
            Func<VisualElement> criarFantasma
        )
        {
            elemento.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || _pressionado)
                    return;
                _pressionado = true;
                _elemento = elemento;
                _origem = origem;
                _criarFantasma = criarFantasma;
                _inicio = e.position;
                _pointerId = e.pointerId;
                elemento.CapturePointer(e.pointerId);
            });
            elemento.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!_pressionado || _elemento != elemento || e.pointerId != _pointerId)
                    return;
                if (!Arrastando)
                {
                    if (((Vector2)e.position - _inicio).magnitude < LimiarPx)
                        return;
                    Iniciar();
                }

                PosicionarFantasma(e.position);
                _aoMover?.Invoke(_origem, e.position);
            });
            elemento.RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button != 0 || !_pressionado || _elemento != elemento)
                    return;
                var estavaArrastando = Arrastando;
                var origemAtual = _origem;
                Encerrar();
                if (estavaArrastando)
                    _aoSoltar?.Invoke(origemAtual, _alvoEm(e.position));
            });
            elemento.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                if (_pressionado && _elemento == elemento)
                    Encerrar();
            });
        }

        /// <summary>Cancela o arraste em andamento sem soltar (janela fechou, por exemplo).</summary>
        public void Cancelar()
        {
            if (_pressionado)
                Encerrar();
        }

        private void Iniciar()
        {
            _fantasma = _criarFantasma != null ? _criarFantasma() : new VisualElement();
            _fantasma.AddToClassList(EstiloItens.Fantasma);
            _fantasma.pickingMode = PickingMode.Ignore;
            _raiz.Add(_fantasma);
            _aoMudar?.Invoke(_origem, true);
        }

        private void Encerrar()
        {
            var estavaArrastando = Arrastando;
            var origem = _origem;
            var elemento = _elemento;
            _pressionado = false;
            _elemento = null;
            if (_fantasma != null)
            {
                _fantasma.RemoveFromHierarchy();
                _fantasma = null;
            }

            if (elemento != null && elemento.HasPointerCapture(_pointerId))
                elemento.ReleasePointer(_pointerId);
            if (estavaArrastando)
                _aoMudar?.Invoke(origem, false);
        }

        private void PosicionarFantasma(Vector3 posicaoNoPainel)
        {
            var local = _raiz.WorldToLocal(posicaoNoPainel);
            // Centralizado no ponteiro.
            _fantasma.style.left = local.x - _fantasma.resolvedStyle.width / 2f;
            _fantasma.style.top = local.y - _fantasma.resolvedStyle.height / 2f;
        }
    }
}
