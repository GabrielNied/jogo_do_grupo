using System;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Menu do Esc no mundo: sobreposto ao jogo (sem abrir uma <see cref="Tela"/>), com
    /// "Continuar" e "Voltar à seleção" (<see cref="EntradaNoJogo.Sair"/>).
    /// </summary>
    internal sealed class MenuJogo
    {
        private const string NomeElemento = "core.menu-jogo";

        // Fundo meio transparente: o mundo continua visível atrás do menu.
        private static readonly UnityEngine.Color CorFundo = new UnityEngine.Color(
            0f,
            0f,
            0f,
            0.55f
        );

        private readonly Action _aoContinuar;
        private VisualElement _raiz;

        public MenuJogo(Action aoContinuar)
        {
            _aoContinuar = aoContinuar;
        }

        public bool Aberto => _raiz != null;

        public void Abrir()
        {
            var container = Navegacao.Container;
            if (Aberto || container == null)
            {
                return;
            }

            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.Tela);
            _raiz.style.backgroundColor = CorFundo;

            var painel = new VisualElement();
            painel.AddToClassList(EstiloCore.Painel);

            var titulo = new Label("Menu");
            titulo.AddToClassList(EstiloCore.Titulo);

            var continuar = new Button(() => _aoContinuar?.Invoke()) { text = "Continuar" };
            continuar.AddToClassList(EstiloCore.Botao);
            continuar.AddToClassList(EstiloCore.BotaoPrincipal);

            var voltar = new Button(EntradaNoJogo.Sair) { text = "Voltar à seleção" };
            voltar.AddToClassList(EstiloCore.Botao);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            linha.Add(voltar);
            linha.Add(continuar);

            painel.Add(titulo);
            painel.Add(linha);
            _raiz.Add(painel);
            container.Add(_raiz);
        }

        public void Fechar()
        {
            _raiz?.RemoveFromHierarchy();
            _raiz = null;
        }
    }
}
