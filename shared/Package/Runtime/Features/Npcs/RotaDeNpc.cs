using System;

namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// Rota fixa de um NPC ambiente: pontos, pausas e velocidade. A posição é uma função pura do
    /// tempo (<see cref="Amostrar"/>), então todo client calcula o mesmo lugar sem rede (#1069).
    /// </summary>
    public sealed class RotaDeNpc
    {
        private readonly PontoDeRota[] _etapas;
        private readonly float[] _inicioDaPausa;
        private readonly float[] _inicioDoTrecho;

        public readonly ModoDeRota Modo;
        public readonly float Velocidade;

        /// <summary>Segundos de um ciclo completo (0 se o NPC nunca sai do lugar).</summary>
        public readonly float DuracaoDoCiclo;

        public RotaDeNpc(PontoDeRota[] pontos, ModoDeRota modo, float velocidade)
        {
            if (pontos == null || pontos.Length == 0)
            {
                throw new ArgumentException("A rota precisa de ao menos um ponto.", nameof(pontos));
            }

            if (velocidade <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(velocidade));
            }

            Modo = modo;
            Velocidade = velocidade;
            _etapas = Expandir(pontos, modo);
            _inicioDaPausa = new float[_etapas.Length];
            _inicioDoTrecho = new float[_etapas.Length];

            var tempo = 0f;
            for (var i = 0; i < _etapas.Length; i++)
            {
                _inicioDaPausa[i] = tempo;
                tempo += _etapas[i].PausaSegundos;
                _inicioDoTrecho[i] = tempo;
                if (_etapas.Length > 1)
                {
                    tempo += Distancia(_etapas[i], _etapas[(i + 1) % _etapas.Length]) / velocidade;
                }
            }

            DuracaoDoCiclo = _etapas.Length > 1 ? tempo : 0f;
        }

        public int QuantidadeDePontos => _etapas.Length;

        public PontoDeRota Ponto(int indice) => _etapas[indice];

        /// <summary>Posição no instante <paramref name="tempo"/> (segundos), com <paramref name="fase"/> de deslocamento.</summary>
        public AmostraDeRota Amostrar(double tempo, float fase = 0f)
        {
            if (_etapas.Length == 1 || DuracaoDoCiclo <= 0f)
            {
                return Parada(_etapas[0], 0f, 0f);
            }

            var t = (float)((((tempo + fase) % DuracaoDoCiclo) + DuracaoDoCiclo) % DuracaoDoCiclo);
            for (var i = 0; i < _etapas.Length; i++)
            {
                var fim = i + 1 < _etapas.Length ? _inicioDaPausa[i + 1] : DuracaoDoCiclo;
                if (t >= fim)
                {
                    continue;
                }

                var ponto = _etapas[i];
                if (t < _inicioDoTrecho[i])
                {
                    return Parada(ponto, t - _inicioDaPausa[i], DirecaoPara(i));
                }

                var proximo = _etapas[(i + 1) % _etapas.Length];
                var comprimento = Distancia(ponto, proximo);
                var fracao =
                    comprimento <= 0f ? 1f : (t - _inicioDoTrecho[i]) * Velocidade / comprimento;
                if (fracao > 1f)
                {
                    fracao = 1f;
                }

                return new AmostraDeRota(
                    ponto.X + (proximo.X - ponto.X) * fracao,
                    ponto.Z + (proximo.Z - ponto.Z) * fracao,
                    Graus(proximo.X - ponto.X, proximo.Z - ponto.Z, 0f),
                    false,
                    false,
                    0f
                );
            }

            return Parada(_etapas[0], 0f, 0f);
        }

        private static AmostraDeRota Parada(PontoDeRota ponto, float tempoNaPausa, float direcao)
        {
            var rotacao = ponto.TemOlhar
                ? Graus(ponto.OlharX - ponto.X, ponto.OlharZ - ponto.Z, direcao)
                : direcao;
            return new AmostraDeRota(ponto.X, ponto.Z, rotacao, true, ponto.Conversa, tempoNaPausa);
        }

        // Sem olhar definido, o NPC parado fica de frente para o próximo trecho.
        private float DirecaoPara(int indice)
        {
            if (_etapas.Length < 2)
            {
                return 0f;
            }

            var a = _etapas[indice];
            var b = _etapas[(indice + 1) % _etapas.Length];
            return Graus(b.X - a.X, b.Z - a.Z, 0f);
        }

        private static PontoDeRota[] Expandir(PontoDeRota[] pontos, ModoDeRota modo)
        {
            if (modo != ModoDeRota.VaiEVolta || pontos.Length < 3)
            {
                return (PontoDeRota[])pontos.Clone();
            }

            var lista = new PontoDeRota[pontos.Length * 2 - 2];
            Array.Copy(pontos, lista, pontos.Length);
            for (var i = 1; i < pontos.Length - 1; i++)
            {
                lista[pontos.Length - 1 + i] = pontos[pontos.Length - 1 - i];
            }

            return lista;
        }

        private static float Distancia(PontoDeRota a, PontoDeRota b)
        {
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }

        private static float Graus(float dx, float dz, float padrao)
        {
            if (Math.Abs(dx) < 0.0001f && Math.Abs(dz) < 0.0001f)
            {
                return padrao;
            }

            return (float)(Math.Atan2(dx, dz) * 180.0 / Math.PI);
        }
    }
}
