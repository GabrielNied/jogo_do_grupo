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
        private ProntidaoLocal _prontidao;
        private long _personagemId;
        private float _proximaConsulta;
        private bool _consultando;
        private int _versaoDoServidor;

        /// <summary>
        /// O servidor mandou os recursos por <c>RecursosMudaram</c>: uma consulta que ainda
        /// estava a caminho é mais antiga e não deve sobrescrever o que acabou de chegar.
        /// </summary>
        public void RegistrarAtualizacaoDoServidor()
        {
            _versaoDoServidor++;
        }

        public void Configurar(long personagemId, PainelRecursos painel, ProntidaoLocal prontidao)
        {
            _personagemId = personagemId;
            _painel = painel;
            _prontidao = prontidao;
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
            var versao = _versaoDoServidor;
            try
            {
                var resposta = await ClienteApi.Get<RecursosResponse>(
                    RotasCombate.RecursosDe(_personagemId)
                );
                if (this == null || !EntradaNoJogo.NoJogo || versao != _versaoDoServidor)
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
                    _prontidao?.RegistrarVigor(vigor.Atual);
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
