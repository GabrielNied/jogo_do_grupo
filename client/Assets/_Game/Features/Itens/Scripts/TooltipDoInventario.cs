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
        private const float EspacoEntre = 8f;

        private readonly VisualElement _raiz;
        private readonly Func<bool> _bloqueado;
        private readonly Func<VisualElement> _evitar;
        private readonly VisualElement _caixa;
        private readonly VisualElement _caixaEquipado;
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
            _caixaEquipado = new VisualElement { name = "itens.tooltip-equipado" };
            _caixaEquipado.AddToClassList(EstiloItens.Tooltip);
            _caixaEquipado.style.display = DisplayStyle.None;
            _caixaEquipado.pickingMode = PickingMode.Ignore;
            _caixaEquipado.RegisterCallback<GeometryChangedEvent>(_ => Ajustar());
            _raiz.Add(_caixa);
            _raiz.Add(_caixaEquipado);
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
            _caixaEquipado.RemoveFromHierarchy();
        }

        public void Esconder()
        {
            _agendado?.Pause();
            _agendado = null;
            _caixa.style.display = DisplayStyle.None;
            _caixaEquipado.style.display = DisplayStyle.None;
        }

        public void Mostrar(DadosTooltip dados)
        {
            Preencher(_caixa, dados, true);
            var lado = dados.Equipado != null;
            if (lado)
                Preencher(_caixaEquipado, dados.Equipado, false);
            _caixa.style.display = DisplayStyle.Flex;
            _caixaEquipado.style.display = lado ? DisplayStyle.Flex : DisplayStyle.None;
            _caixaEquipado.BringToFront();
            _caixa.BringToFront();
            Ajustar();
        }

        private static void Preencher(VisualElement caixa, DadosTooltip dados, bool comComparacao)
        {
            caixa.Clear();
            if (dados.Cabecalho != null)
            {
                caixa.Add(
                    Linha(new LinhaTooltip { Texto = dados.Cabecalho, Estilo = EstiloLinha.Suave })
                );
            }
            var nome = new Label(dados.Nome);
            nome.AddToClassList(EstiloCore.Texto);
            nome.AddToClassList(EstiloItens.TooltipNome);
            if (dados.Raridade != null)
                nome.AddToClassList(EstiloItens.NomeRaridade(dados.Raridade.Value));
            caixa.Add(nome);
            foreach (var linha in dados.Linhas)
                caixa.Add(Linha(linha));
            if (comComparacao && dados.TituloComparacao != null)
            {
                var comparacao = new VisualElement();
                comparacao.AddToClassList(EstiloItens.TooltipComparacao);
                // Com a peça equipada ao lado, o título seria repetição.
                if (dados.Equipado == null)
                {
                    comparacao.Add(
                        Linha(
                            new LinhaTooltip
                            {
                                Texto = dados.TituloComparacao,
                                Estilo = EstiloLinha.Suave,
                            }
                        )
                    );
                }
                foreach (var linha in dados.Comparacao)
                    comparacao.Add(Linha(linha));
                caixa.Add(comparacao);
            }
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
            var largura1 = Medida(_caixa.resolvedStyle.width);
            var altura1 = Medida(_caixa.resolvedStyle.height);
            var lado = _caixaEquipado.style.display == DisplayStyle.Flex;
            var largura2 = lado ? Medida(_caixaEquipado.resolvedStyle.width) : 0;
            var altura2 = lado ? Medida(_caixaEquipado.resolvedStyle.height) : 0;
            // Os dois tooltips andam juntos como um bloco (item e, ao lado, a peça equipada).
            var largura = largura1 + (lado ? EspacoEntre + largura2 : 0);
            var altura = Mathf.Max(altura1, altura2);
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
            _caixaEquipado.style.left = escolhido.x + largura1 + EspacoEntre;
            _caixaEquipado.style.top = escolhido.y;
        }

        private static float Medida(float valor)
        {
            return float.IsNaN(valor) ? 0 : valor;
        }

        private static Vector2 Limitar(Vector2 posicao, float largura, float altura, Rect limite)
        {
            var x = Mathf.Clamp(posicao.x, 0, Mathf.Max(0, limite.width - largura));
            var y = Mathf.Clamp(posicao.y, 0, Mathf.Max(0, limite.height - altura));
            return new Vector2(x, y);
        }
    }
}
