using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// As barras de vida, vigor e energia do personagem logado. Uma consulta ao entrar no mundo
    /// traz o retrato inicial; dali em diante o servidor avisa cada mudança por
    /// <see cref="RecursosMudaram"/> (<see cref="AplicarDoServidor"/>) e a barra de vigor sobe
    /// suave pela regeneração prevista da <see cref="ProntidaoLocal"/>, que o servidor corrige a
    /// cada mensagem. Nada é decidido aqui. Sem resposta na consulta inicial, tenta de novo a cada
    /// <see cref="IntervaloDaTentativaSegundos"/> até conseguir.
    /// </summary>
    internal sealed class ControleRecursos : MonoBehaviour
    {
        public const float IntervaloDaTentativaSegundos = 3f;

        private PainelRecursos _painel;
        private ProntidaoLocal _prontidao;
        private long _personagemId;
        private float _proximaConsulta;
        private bool _consultando;
        private bool _temRetrato;
        private int _versaoDoServidor;

        private RecursosMudaram _ultimo = new RecursosMudaram();
        private (int vida, int vigor, int energia)? _mostrado;

        /// <summary>
        /// O servidor mandou os recursos por <c>RecursosMudaram</c>: guarda o retrato, corrige a
        /// previsão do vigor e uma consulta que ainda estava a caminho, mais antiga, não o sobrescreve.
        /// </summary>
        public void AplicarDoServidor(RecursosMudaram recursos)
        {
            if (recursos == null)
            {
                return;
            }

            _versaoDoServidor++;
            Guardar(
                recursos.VidaAtual,
                recursos.VidaMaxima,
                recursos.VigorAtual,
                recursos.VigorMaximo,
                recursos.EnergiaAtual,
                recursos.EnergiaMaxima
            );
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
            if (_painel == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            if (!_temRetrato && !_consultando && Time.unscaledTime >= _proximaConsulta)
            {
                _proximaConsulta = Time.unscaledTime + IntervaloDaTentativaSegundos;
                Consultar();
            }

            if (_temRetrato)
            {
                Desenhar(Time.unscaledTimeAsDouble);
            }
        }

        private void Guardar(
            int vida,
            int vidaMaxima,
            int vigor,
            int vigorMaximo,
            int energia,
            int energiaMaxima
        )
        {
            _ultimo = new RecursosMudaram
            {
                VidaAtual = vida,
                VidaMaxima = vidaMaxima,
                VigorAtual = vigor,
                VigorMaximo = vigorMaximo,
                EnergiaAtual = energia,
                EnergiaMaxima = energiaMaxima,
            };
            _temRetrato = true;
            _prontidao?.RegistrarVigor(vigor, Time.unscaledTimeAsDouble, vigorMaximo);
            _mostrado = null;
            Desenhar(Time.unscaledTimeAsDouble);
        }

        // Todo quadro: o vigor é o previsto, e a barra só é redesenhada quando algum número muda.
        private void Desenhar(double agora)
        {
            var vigor = _prontidao?.VigorEm(agora) ?? _ultimo.VigorAtual;
            var atual = (_ultimo.VidaAtual, vigor, _ultimo.EnergiaAtual);
            if (_mostrado == atual)
            {
                return;
            }

            _mostrado = atual;
            if (
                ApresentacaoCombate.TentarMontar(
                    _ultimo,
                    out var barraVida,
                    out var barraVigor,
                    out var barraEnergia
                )
            )
            {
                barraVigor = new BarraRecurso(
                    ApresentacaoCombate.RotuloVigor,
                    vigor,
                    _ultimo.VigorMaximo
                );
                _painel.Mostrar(barraVida, barraVigor, barraEnergia);
            }
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

                if (resposta.Sucesso && resposta.Corpo != null)
                {
                    var corpo = resposta.Corpo;
                    Guardar(
                        corpo.VidaAtual,
                        corpo.VidaMaxima,
                        corpo.VigorAtual,
                        corpo.VigorMaximo,
                        corpo.EnergiaAtual,
                        corpo.EnergiaMaxima
                    );
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
