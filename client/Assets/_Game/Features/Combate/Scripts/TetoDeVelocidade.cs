using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// O teto de velocidade do próprio corpo (40% da corrida no golpe e na guarda). Mais de uma
    /// fonte pode prender ao mesmo tempo (a guarda segue segurada enquanto o golpe começa): o
    /// valor original é guardado uma vez e só volta quando a última fonte solta. Só mexe no
    /// <c>Movimento</c> do próprio jogador (ADR 0024); se outra velocidade chegou no meio, vale a
    /// dela.
    /// </summary>
    internal sealed class TetoDeVelocidade
    {
        private readonly HashSet<string> _fontes = new HashSet<string>();
        private ControleTerceiraPessoa _controle;
        private float _andarOriginal;
        private float _correrOriginal;
        private float _andarLimitado;
        private float _correrLimitado;

        public bool Ativo => _fontes.Count > 0;

        /// <summary>A fonte pede o teto de <paramref name="fator"/> (0,4 = 40%) da velocidade.</summary>
        public void Prender(string fonte, GameObject jogador, float fator)
        {
            if (_fontes.Contains(fonte))
            {
                return;
            }

            if (_fontes.Count == 0)
            {
                _controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
                if (_controle == null)
                {
                    return;
                }

                var movimento = _controle.Movimento;
                _andarOriginal = movimento.VelocidadeAndar;
                _correrOriginal = movimento.VelocidadeCorrer;
                _andarLimitado = _andarOriginal * fator;
                _correrLimitado = _correrOriginal * fator;
                movimento.VelocidadeAndar = _andarLimitado;
                movimento.VelocidadeCorrer = _correrLimitado;
            }

            _fontes.Add(fonte);
        }

        /// <summary>A fonte larga o teto; com a última fonte, a velocidade original volta.</summary>
        public void Soltar(string fonte)
        {
            if (!_fontes.Remove(fonte) || _fontes.Count > 0 || _controle == null)
            {
                return;
            }

            var movimento = _controle.Movimento;
            if (Mathf.Approximately(movimento.VelocidadeAndar, _andarLimitado))
            {
                movimento.VelocidadeAndar = _andarOriginal;
            }

            if (Mathf.Approximately(movimento.VelocidadeCorrer, _correrLimitado))
            {
                movimento.VelocidadeCorrer = _correrOriginal;
            }

            _controle = null;
        }
    }
}
