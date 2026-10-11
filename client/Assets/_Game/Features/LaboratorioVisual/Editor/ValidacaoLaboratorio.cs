using System.Text;
using JogoDoGrupo.Client.Core.Visual;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.LaboratorioVisual.Editor
{
    public static class ValidacaoLaboratorio
    {
        public static string Relatar(VisualElement raiz)
        {
            if (raiz == null)
                return "FALHA: raiz ausente.";
            if (raiz.panel == null || raiz.resolvedStyle.width <= 0)
                return "PENDENTE: interface sem layout renderizado.";
            var texto = new StringBuilder();
            var alertas = 0;
            raiz.Query<Button>()
                .ForEach(botao =>
                {
                    if (
                        botao.resolvedStyle.display == DisplayStyle.None
                        || botao.worldBound.width <= 0
                    )
                        return;
                    if (botao.resolvedStyle.height < ConfigVisual.Dimensoes.AlvoMinimo)
                    {
                        texto.AppendLine("- ALERTA: alvo menor que 44 px lógicos: " + botao.text);
                        alertas++;
                    }
                });
            raiz.Query<Label>(className: "lab-leitura")
                .ForEach(label =>
                {
                    if (label.resolvedStyle.fontSize < ConfigVisual.Tipografia.Leitura)
                    {
                        texto.AppendLine("- ALERTA: leitura menor que 16 px: " + label.text);
                        alertas++;
                    }
                });
            texto.Insert(
                0,
                "Alertas estruturais: "
                    + alertas
                    + ". Esta verificação não aprova fidelidade visual, contraste ou ausência de cortes.\n"
            );
            return texto.ToString();
        }
    }
}
