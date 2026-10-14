using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Mobs
{
    /// <summary>
    /// Guarda e gerencia a pontuação de ameaça por personagem para um inimigo ou chefe individual (docs/features/combate.md#ameaça-agro-169).
    /// </summary>
    public sealed class TabelaDeAmeaca
    {
        private readonly Dictionary<long, double> _pontos = new();
        private long _provocadorId = RegrasInimigo.SemAlvo;
        private long _provocacaoAteMs;

        public bool TemAmeaca => _pontos.Count > 0;

        public IReadOnlyCollection<long> Personagens => _pontos.Keys;

        public double ObterAmeaca(long personagemId)
        {
            return _pontos.TryGetValue(personagemId, out var val) ? val : 0.0;
        }

        public double MaiorAmeaca()
        {
            double max = 0.0;
            foreach (var kv in _pontos)
            {
                if (kv.Value > max)
                {
                    max = kv.Value;
                }
            }
            return max;
        }

        public void AdicionarDano(long personagemId, int danoReal, bool eTank = false)
        {
            if (personagemId == RegrasInimigo.SemAlvo || danoReal <= 0)
            {
                return;
            }

            double gerada = RegrasAmeaca.CalcularAmeacaDeDano(danoReal, eTank);
            _pontos[personagemId] = ObterAmeaca(personagemId) + gerada;
        }

        public void AdicionarCura(
            long personagemId,
            int curaReal,
            int inimigosEmCombate = 1,
            bool eTank = false
        )
        {
            if (personagemId == RegrasInimigo.SemAlvo || curaReal <= 0)
            {
                return;
            }

            double gerada = RegrasAmeaca.CalcularAmeacaDeCura(curaReal, inimigosEmCombate, eTank);
            _pontos[personagemId] = ObterAmeaca(personagemId) + gerada;
        }

        public void Provocar(
            long provocadorId,
            long agoraMs,
            long duracaoMs = RegrasAmeaca.DuracaoDaProvocacaoMs
        )
        {
            if (provocadorId == RegrasInimigo.SemAlvo)
            {
                return;
            }

            double topo = MaiorAmeaca();
            double atual = ObterAmeaca(provocadorId);
            _pontos[provocadorId] = Math.Max(atual, topo);

            _provocadorId = provocadorId;
            _provocacaoAteMs = agoraMs + Math.Max(0, duracaoMs);
        }

        public bool EstaProvocado(long agoraMs, out long provocadorId)
        {
            if (_provocadorId != RegrasInimigo.SemAlvo && agoraMs < _provocacaoAteMs)
            {
                provocadorId = _provocadorId;
                return true;
            }

            provocadorId = RegrasInimigo.SemAlvo;
            return false;
        }

        public void Remover(long personagemId)
        {
            _pontos.Remove(personagemId);
            if (_provocadorId == personagemId)
            {
                _provocadorId = RegrasInimigo.SemAlvo;
                _provocacaoAteMs = 0;
            }
        }

        public void Limpar()
        {
            _pontos.Clear();
            _provocadorId = RegrasInimigo.SemAlvo;
            _provocacaoAteMs = 0;
        }

        public long EscolherAlvo(
            long alvoAtual,
            long[] candidatos,
            double[] distanciasMetros,
            double alcanceDeAtaqueMetros,
            long agoraMs
        )
        {
            if (candidatos == null || candidatos.Length == 0)
            {
                return RegrasInimigo.SemAlvo;
            }

            double[] ameacas = new double[candidatos.Length];
            for (int i = 0; i < candidatos.Length; i++)
            {
                ameacas[i] = ObterAmeaca(candidatos[i]);
            }

            return RegrasAmeaca.EscolherAlvo(
                alvoAtual,
                candidatos,
                distanciasMetros,
                ameacas,
                alcanceDeAtaqueMetros,
                _provocadorId,
                agoraMs,
                _provocacaoAteMs
            );
        }
    }
}
