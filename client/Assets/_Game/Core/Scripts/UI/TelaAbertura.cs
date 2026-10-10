using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.UI
{
    /// <summary>
    /// Abertura/carregamento: nome do jogo e uma barra curta de carregamento; ao terminar,
    /// abre o login.
    /// </summary>
    internal sealed class TelaAbertura : Tela
    {
        private const float DuracaoSegundos = 1.5f;
        private const long IntervaloMs = 16;

        private VisualElement _preenchimento;
        private IVisualElementScheduledItem _atualizacao;
        private float _inicio;

        protected override void Montar(VisualElement raiz)
        {
            var titulo = new Label("Jogo do Grupo");
            titulo.AddToClassList(EstiloCore.Titulo);

            var subtitulo = new Label("Carregando...");
            subtitulo.AddToClassList(EstiloCore.Subtitulo);

            var barra = new VisualElement();
            barra.AddToClassList(EstiloCore.BarraCarregamento);
            _preenchimento = new VisualElement();
            _preenchimento.AddToClassList(EstiloCore.BarraCarregamentoPreenchimento);
            barra.Add(_preenchimento);

            raiz.Add(titulo);
            raiz.Add(subtitulo);
            raiz.Add(barra);
        }

        protected override void AoAbrir()
        {
            _inicio = Time.realtimeSinceStartup;
            _atualizacao = Raiz.schedule.Execute(Atualizar).Every(IntervaloMs);
        }

        protected override void AoFechar()
        {
            _atualizacao?.Pause();
        }

        private void Atualizar()
        {
            if (!Aberta)
            {
                return;
            }

            var progresso = Mathf.Clamp01((Time.realtimeSinceStartup - _inicio) / DuracaoSegundos);
            _preenchimento.style.width = Length.Percent(progresso * 100f);

            if (progresso >= 1f)
            {
                _atualizacao.Pause();
                Navegacao.Abrir(TelasCore.Login);
            }
        }
    }
}
