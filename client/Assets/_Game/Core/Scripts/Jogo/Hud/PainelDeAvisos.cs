using JogoDoGrupo.Client.Core.UI;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Desenha a <see cref="ListaDeAvisos"/> na região <see cref="RegiaoDoHud.InferiorCentro"/>.
    /// Só mostra: não recebe clique. A cor vem da classe do tipo, no <c>EstiloCore.uss</c>.
    /// </summary>
    internal sealed class PainelDeAvisos
    {
        internal const string NomeElemento = "core.avisos";

        private readonly ListaDeAvisos _lista;
        private readonly VisualElement _raiz;

        public PainelDeAvisos(VisualElement regiao, ListaDeAvisos lista)
        {
            _lista = lista;
            _raiz = new VisualElement { name = NomeElemento };
            _raiz.AddToClassList(EstiloCore.Avisos);
            _raiz.pickingMode = PickingMode.Ignore;
            regiao?.Add(_raiz);
        }

        /// <summary>Elemento com as linhas.</summary>
        public VisualElement Raiz => _raiz;

        public void Mostrar(
            string texto,
            TipoDeAviso tipo,
            CategoriaDeAviso categoria = CategoriaDeAviso.Sistema
        )
        {
            if (_lista.Mostrar(texto, tipo, categoria))
            {
                Redesenhar();
            }
        }

        public void AtualizarVisibilidade() => Redesenhar();

        /// <summary>Tira da tela as linhas vencidas.</summary>
        public void Atualizar()
        {
            if (_lista.Atualizar())
            {
                Redesenhar();
            }
        }

        internal static string ClasseDoTipo(TipoDeAviso tipo)
        {
            switch (tipo)
            {
                case TipoDeAviso.Aviso:
                    return EstiloCore.AvisoAviso;
                case TipoDeAviso.Erro:
                    return EstiloCore.AvisoErro;
                default:
                    return EstiloCore.AvisoInformacao;
            }
        }

        private void Redesenhar()
        {
            _raiz.Clear();
            foreach (var linha in _lista.Linhas)
            {
                if (!PreferenciasDeAvisos.PodeMostrar(linha.Categoria, linha.Tipo))
                    continue;
                var rotulo = new Label(linha.TextoNaTela);
                rotulo.AddToClassList(EstiloCore.Aviso);
                rotulo.AddToClassList(ClasseDoTipo(linha.Tipo));
                rotulo.pickingMode = PickingMode.Ignore;
                _raiz.Add(rotulo);
            }
        }
    }
}
