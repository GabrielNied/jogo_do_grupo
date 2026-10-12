using System;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// O tooltip do inventário: aparece depois de <see cref="AtrasoMs"/> com o mouse parado sobre
    /// um item e some ao sair, ao arrastar ou ao clicar. Vive na raiz da interface para não ser
    /// cortado pela rolagem. O conteúdo vem pronto de <see cref="TextosTooltip"/>.
    /// </summary>
    public sealed class TooltipDoInventario
    {
        public const long AtrasoMs = 300;
        private const float Deslocamento = 16f;

        private readonly VisualElement _raiz;
        private readonly Func<bool> _bloqueado;
        private readonly Func<VisualElement> _evitar;
        private readonly VisualElement _caixa;
        private IVisualElementScheduledItem _agendado;
        private Vector2 _posicao;

        /// <param name="raiz">Raiz da interface.</param>
        /// <param name="bloqueado">Verdadeiro enquanto não pode mostrar (arrastando).</param>
        public TooltipDoInventario(
            VisualElement raiz,
            Func<bool> bloqueado,
            Func<VisualElement> evitar = null
        )
        {
            _raiz = raiz;
            _bloqueado = bloqueado;
            _evitar = evitar;
            _caixa = new VisualElement { name = "itens.tooltip" };
            _caixa.AddToClassList(EstiloItens.Tooltip);
            _caixa.style.display = DisplayStyle.None;
            _caixa.pickingMode = PickingMode.Ignore;
            _caixa.RegisterCallback<GeometryChangedEvent>(_ => Ajustar());
            _raiz.Add(_caixa);
        }

        public bool Visivel => _caixa.style.display == DisplayStyle.Flex;

        /// <summary>Mostra o tooltip de <paramref name="elemento"/> quando o mouse parar nele.</summary>
        public void Registrar(VisualElement elemento, Func<DadosTooltip> dados)
        {
            elemento.RegisterCallback<PointerEnterEvent>(e =>
            {
                _posicao = e.position;
                Esconder();
                _agendado = elemento
                    .schedule.Execute(() =>
                    {
                        if (_bloqueado != null && _bloqueado())
                            return;
                        var conteudo = dados();
                        if (conteudo != null)
                            Mostrar(conteudo);
                    })
                    .StartingIn(AtrasoMs);
            });
            elemento.RegisterCallback<PointerMoveEvent>(e =>
            {
                _posicao = e.position;
                if (_bloqueado != null && _bloqueado())
                    Esconder();
                else if (Visivel)
                    Ajustar();
            });
            elemento.RegisterCallback<PointerLeaveEvent>(_ => Esconder());
            elemento.RegisterCallback<PointerDownEvent>(_ => Esconder());
        }

        /// <summary>Tira o tooltip da raiz da interface (a janela fechou).</summary>
        public void Remover()
        {
            Esconder();
            _caixa.RemoveFromHierarchy();
        }

        public void Esconder()
        {
            _agendado?.Pause();
            _agendado = null;
            _caixa.style.display = DisplayStyle.None;
        }

        public void Mostrar(DadosTooltip dados)
        {
            _caixa.Clear();
            var nome = new Label(dados.Nome);
            nome.AddToClassList(EstiloCore.Texto);
            nome.AddToClassList(EstiloItens.TooltipNome);
            if (dados.Raridade != null)
                nome.AddToClassList(EstiloItens.NomeRaridade(dados.Raridade.Value));
            _caixa.Add(nome);
            foreach (var linha in dados.Linhas)
                _caixa.Add(Linha(linha));
            if (dados.TituloComparacao != null)
            {
                var comparacao = new VisualElement();
                comparacao.AddToClassList(EstiloItens.TooltipComparacao);
                comparacao.Add(
                    Linha(
                        new LinhaTooltip
                        {
                            Texto = dados.TituloComparacao,
                            Estilo = EstiloLinha.Suave,
                        }
                    )
                );
                foreach (var linha in dados.Comparacao)
                    comparacao.Add(Linha(linha));
                _caixa.Add(comparacao);
            }

            _caixa.style.display = DisplayStyle.Flex;
            _caixa.BringToFront();
            Ajustar();
        }

        private static Label Linha(LinhaTooltip linha)
        {
            var label = new Label(linha.Texto);
            label.AddToClassList(EstiloCore.Texto);
            label.AddToClassList(EstiloItens.TooltipLinha);
            var classe = EstiloItens.DoEstilo(linha.Estilo);
            if (classe != null)
                label.AddToClassList(classe);
            return label;
        }

        // Ao lado do ponteiro, sem sair da tela e sem cobrir o elemento a evitar (o painel de
        // bônus): testa direita/esquerda, embaixo/em cima, e fica com o primeiro que serve.
        private void Ajustar()
        {
            var local = _raiz.WorldToLocal(_posicao);
            var largura = _caixa.resolvedStyle.width;
            var altura = _caixa.resolvedStyle.height;
            if (float.IsNaN(largura))
                largura = 0;
            if (float.IsNaN(altura))
                altura = 0;
            var limite = _raiz.layout;
            var evitar = _evitar?.Invoke();
            var evitando = evitar != null && !float.IsNaN(evitar.worldBound.width);
            var proibido = evitando
                ? new Rect(_raiz.WorldToLocal(evitar.worldBound.position), evitar.worldBound.size)
                : default;

            var direita = local.x + Deslocamento;
            var esquerda = local.x - Deslocamento - largura;
            var embaixo = local.y + Deslocamento;
            var emCima = local.y - Deslocamento - altura;
            var candidatos = new[]
            {
                new Vector2(direita, embaixo),
                new Vector2(esquerda, embaixo),
                new Vector2(direita, emCima),
                new Vector2(esquerda, emCima),
            };
            var escolhido = Limitar(candidatos[0], largura, altura, limite);
            foreach (var candidato in candidatos)
            {
                var posicao = Limitar(candidato, largura, altura, limite);
                var cabe = posicao == candidato;
                var cobre =
                    evitando && proibido.Overlaps(new Rect(posicao, new Vector2(largura, altura)));
                if (cabe && !cobre)
                {
                    escolhido = posicao;
                    break;
                }
            }

            _caixa.style.left = escolhido.x;
            _caixa.style.top = escolhido.y;
        }

        private static Vector2 Limitar(Vector2 posicao, float largura, float altura, Rect limite)
        {
            var x = Mathf.Clamp(posicao.x, 0, Mathf.Max(0, limite.width - largura));
            var y = Mathf.Clamp(posicao.y, 0, Mathf.Max(0, limite.height - altura));
            return new Vector2(x, y);
        }
    }
}
