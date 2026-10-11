using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Consulta os recursos do personagem logado a cada poucos segundos enquanto ele está no
    /// mundo e repassa ao painel. Só mostra o que o servidor responde; em falha mantém a última
    /// leitura e tenta de novo no próximo ciclo.
    /// </summary>
    internal sealed class ControleRecursos : MonoBehaviour
    {
        public const float IntervaloSegundos = 3f;

        private PainelRecursos _painel;
        private long _personagemId;
        private float _proximaConsulta;
        private bool _consultando;

        public void Configurar(long personagemId, PainelRecursos painel)
        {
            _personagemId = personagemId;
            _painel = painel;
            _proximaConsulta = 0f;
        }

        private void Update()
        {
            if (_painel == null || _consultando || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            if (Time.unscaledTime < _proximaConsulta)
            {
                return;
            }

            _proximaConsulta = Time.unscaledTime + IntervaloSegundos;
            Consultar();
        }

        private async void Consultar()
        {
            _consultando = true;
            try
            {
                var resposta = await ClienteApi.Get<RecursosResponse>(
                    RotasCombate.RecursosDe(_personagemId)
                );
                if (this == null || !EntradaNoJogo.NoJogo)
                {
                    return;
                }

                if (
                    ApresentacaoCombate.TentarMontar(
                        resposta.Sucesso,
                        resposta.Corpo,
                        out var vida,
                        out var vigor,
                        out var energia
                    )
                )
                {
                    _painel.Mostrar(vida, vigor, energia);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                _consultando = false;
            }
        }
    }
}
