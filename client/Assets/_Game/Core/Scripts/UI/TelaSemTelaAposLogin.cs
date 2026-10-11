using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Aberta depois do login quando nenhuma funcionalidade registrou a tela seguinte
    /// (<see cref="Navegacao.DefinirTelaAposLogin"/>). Só permite sair para o login.
    /// </summary>
    internal sealed class TelaSemTelaAposLogin : Tela
    {
        protected override void Montar(VisualElement raiz)
        {
            var painel = EstiloCore.CriarPainel();

            var titulo = new Label("Jogo do Grupo");
            titulo.AddToClassList(EstiloCore.Titulo);

            var texto = new Label("Nenhuma tela registrada após o login.");
            texto.AddToClassList(EstiloCore.Mensagem);

            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            var sair = new Button(() => Navegacao.VoltarParaLogin()) { text = "Sair" };
            sair.AddToClassList(EstiloCore.Botao);
            linha.Add(sair);

            painel.Add(titulo);
            painel.Add(texto);
            painel.Add(linha);
            raiz.Add(painel);
        }
    }
}
