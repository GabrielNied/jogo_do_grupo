using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// As barras de vida, stamina e mana do personagem logado. Uma consulta ao entrar no mundo
    /// traz o retrato inicial; dali em diante o servidor avisa cada mudança por
    /// <see cref="RecursosMudaram"/> (<see cref="AplicarDoServidor"/>) e a barra de stamina sobe
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
        private (int vida, int stamina, int mana)? _mostrado;

        /// <summary>
        /// O servidor mandou os recursos por <c>RecursosMudaram</c>: guarda o retrato, corrige a
        /// previsão da stamina e uma consulta que ainda estava a caminho, mais antiga, não o sobrescreve.
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
                recursos.StaminaAtual,
                recursos.StaminaMaxima,
                recursos.ManaAtual,
                recursos.ManaMaxima
            );
            _prontidao?.RegistrarRegeneracaoDeStamina(
                recursos.RegeneracaoStamina,
                Time.unscaledTimeAsDouble
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
            int stamina,
            int staminaMaxima,
            int mana,
            int manaMaxima
        )
        {
            _ultimo = new RecursosMudaram
            {
                VidaAtual = vida,
                VidaMaxima = vidaMaxima,
                StaminaAtual = stamina,
                StaminaMaxima = staminaMaxima,
                ManaAtual = mana,
                ManaMaxima = manaMaxima,
            };
            _temRetrato = true;
            _prontidao?.RegistrarStamina(stamina, Time.unscaledTimeAsDouble, staminaMaxima);
            _mostrado = null;
            Desenhar(Time.unscaledTimeAsDouble);
        }

        // Todo quadro: a stamina é o previsto, e a barra só é redesenhada quando algum número muda.
        private void Desenhar(double agora)
        {
            var stamina = _prontidao?.StaminaEm(agora) ?? _ultimo.StaminaAtual;
            var atual = (_ultimo.VidaAtual, stamina, _ultimo.ManaAtual);
            if (_mostrado == atual)
            {
                return;
            }

            _mostrado = atual;
            if (
                ApresentacaoCombate.TentarMontar(
                    _ultimo,
                    out var barraVida,
                    out var barraStamina,
                    out var barraMana
                )
            )
            {
                barraStamina = new BarraRecurso(
                    ApresentacaoCombate.RotuloStamina,
                    stamina,
                    _ultimo.StaminaMaxima
                );
                _painel.Mostrar(barraVida, barraStamina, barraMana);
            }
        }

        // A regeneração de stamina é o secundário calculado pelo servidor (com o equipamento), não
        // a base fixa.
        private void RegistrarRegeneracaoDoServidor(RecursosResponse corpo)
        {
            if (corpo.Secundarios == null)
            {
                return;
            }

            foreach (var calculado in corpo.Secundarios)
            {
                if (
                    calculado != null
                    && calculado.Atributo == AtributoSecundario.RegeneracaoStamina
                )
                {
                    _prontidao?.RegistrarRegeneracaoDeStamina(
                        calculado.Valor,
                        Time.unscaledTimeAsDouble
                    );
                }
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
                        corpo.StaminaAtual,
                        corpo.StaminaMaxima,
                        corpo.ManaAtual,
                        corpo.ManaMaxima
                    );
                    RegistrarRegeneracaoDoServidor(corpo);
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
