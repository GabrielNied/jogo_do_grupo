using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>Uma amostra de pose do servidor com instante e sequência.</summary>
    public readonly struct SnapshotDeMovimento
    {
        public SnapshotDeMovimento(
            double tempo,
            float x,
            float y,
            float z,
            float rotacaoY,
            int sequencia = 0,
            long tempoServidorMs = 0
        )
        {
            Tempo = tempo;
            X = x;
            Y = y;
            Z = z;
            RotacaoY = rotacaoY;
            Sequencia = sequencia;
            TempoServidorMs = tempoServidorMs;
        }

        public double Tempo { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RotacaoY { get; }
        public int Sequencia { get; }
        public long TempoServidorMs { get; }
    }

    /// <summary>Pose calculada para um instante de render.</summary>
    public readonly struct PoseAmostrada
    {
        public PoseAmostrada(
            float x,
            float y,
            float z,
            float rotacaoY,
            float velocidade,
            bool extrapolando
        )
        {
            X = x;
            Y = y;
            Z = z;
            RotacaoY = rotacaoY;
            Velocidade = velocidade;
            Extrapolando = extrapolando;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RotacaoY { get; }
        public float Velocidade { get; }
        public bool Extrapolando { get; }
    }

    /// <summary>
    /// Buffer de snapshots com interpolação contínua entre dois pacotes e extrapolação curta
    /// quando pacotes atrasam ou são perdidos. Lógica pura para entidades replicadas.
    /// </summary>
    public sealed class InterpoladorDeMovimento
    {
        private readonly List<SnapshotDeMovimento> _buffer = new List<SnapshotDeMovimento>();
        private readonly float _extrapolacaoMaxima;
        private readonly float _salto;
        private readonly int _capacidade;

        private int _ultimaSequencia;
        private int _amostrasValidas;
        private float _vx;
        private float _vy;
        private float _vz;

        public InterpoladorDeMovimento(
            float extrapolacaoMaximaSegundos = ConfigInterpolacaoMundo.ExtrapolacaoMaximaSegundos,
            float saltoDeTeletransporteMetros = ConfigInterpolacaoMundo.DistanciaDeSalto,
            int capacidade = ConfigInterpolacaoMundo.CapacidadeBuffer
        )
        {
            _extrapolacaoMaxima = extrapolacaoMaximaSegundos;
            _salto = saltoDeTeletransporteMetros;
            _capacidade = Math.Max(2, capacidade);
        }

        public int Quantidade => _buffer.Count;
        public bool Vazio => _buffer.Count == 0;
        public int UltimaSequencia => _ultimaSequencia;

        public void Limpar()
        {
            _buffer.Clear();
            _ultimaSequencia = 0;
            _amostrasValidas = 0;
            _vx = _vy = _vz = 0f;
        }

        /// <summary>
        /// Adiciona uma amostra ao buffer. Descarta se a sequência for menor ou igual à última
        /// (pacote atrasado ou duplicado) ou se o tempo não avançar.
        /// </summary>
        public bool Adicionar(SnapshotDeMovimento snapshot)
        {
            if (snapshot.Sequencia > 0 && snapshot.Sequencia <= _ultimaSequencia)
            {
                return false;
            }

            if (_buffer.Count > 0)
            {
                var ultimo = _buffer[_buffer.Count - 1];
                if (snapshot.Tempo <= ultimo.Tempo)
                {
                    if (snapshot.Sequencia > 0 && snapshot.Sequencia > _ultimaSequencia)
                    {
                        snapshot = new SnapshotDeMovimento(
                            ultimo.Tempo + 0.0001,
                            snapshot.X,
                            snapshot.Y,
                            snapshot.Z,
                            snapshot.RotacaoY,
                            snapshot.Sequencia,
                            snapshot.TempoServidorMs
                        );
                    }
                    else
                    {
                        return false;
                    }
                }

                var dx = snapshot.X - ultimo.X;
                var dz = snapshot.Z - ultimo.Z;
                if (Distancia(dx, dz) > _salto)
                {
                    _amostrasValidas = 1;
                    _vx = _vy = _vz = 0f;
                }
                else
                {
                    _amostrasValidas++;
                    if (_amostrasValidas >= 2)
                    {
                        var dt = (float)(snapshot.Tempo - ultimo.Tempo);
                        if (dt > 0.0001f)
                        {
                            _vx = dx / dt;
                            _vy = (snapshot.Y - ultimo.Y) / dt;
                            _vz = dz / dt;
                        }
                    }
                    else
                    {
                        _vx = _vy = _vz = 0f;
                    }
                }
            }
            else
            {
                _amostrasValidas = 1;
            }

            if (snapshot.Sequencia > 0)
            {
                _ultimaSequencia = snapshot.Sequencia;
            }

            _buffer.Add(snapshot);
            if (_buffer.Count > _capacidade)
            {
                _buffer.RemoveAt(0);
            }

            return true;
        }

        /// <summary>
        /// Calcula a pose correspondente ao instante de render solicitado.
        /// Interpola entre dois pacotes ou extrapola na velocidade recente até o limite.
        /// </summary>
        public PoseAmostrada Amostrar(double tempo)
        {
            if (_buffer.Count == 0)
            {
                return new PoseAmostrada(0f, 0f, 0f, 0f, 0f, false);
            }

            while (_buffer.Count >= 2 && _buffer[1].Tempo <= tempo)
            {
                _buffer.RemoveAt(0);
            }

            var a = _buffer[0];
            if (_buffer.Count == 1)
            {
                return tempo > a.Tempo
                    ? Extrapolar(a, tempo)
                    : new PoseAmostrada(a.X, a.Y, a.Z, a.RotacaoY, 0f, false);
            }

            var b = _buffer[1];
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            if (tempo <= a.Tempo || Distancia(dx, dz) > _salto)
            {
                return new PoseAmostrada(a.X, a.Y, a.Z, a.RotacaoY, 0f, false);
            }

            var dt = b.Tempo - a.Tempo;
            var t = (float)((tempo - a.Tempo) / dt);
            var velocidade = (float)(Distancia(dx, dz) / dt);
            return new PoseAmostrada(
                a.X + dx * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + dz * t,
                AnguloEntre(a.RotacaoY, b.RotacaoY, t),
                velocidade,
                false
            );
        }

        /// <summary>
        /// Teleporta imediatamente para a posição indicada, limpando o buffer e reiniciando a pose.
        /// </summary>
        public void Teleportar(float x, float y, float z, float rotacaoY, double tempo)
        {
            _buffer.Clear();
            _amostrasValidas = 0;
            _vx = _vy = _vz = 0f;
            _buffer.Add(new SnapshotDeMovimento(tempo, x, y, z, rotacaoY));
        }

        private PoseAmostrada Extrapolar(SnapshotDeMovimento ultimo, double tempo)
        {
            var alem = (float)(tempo - ultimo.Tempo);
            var segue = Math.Min(alem, _extrapolacaoMaxima);
            var velocidade = alem > _extrapolacaoMaxima ? 0f : Distancia(_vx, _vz);
            return new PoseAmostrada(
                ultimo.X + _vx * segue,
                ultimo.Y + _vy * segue,
                ultimo.Z + _vz * segue,
                ultimo.RotacaoY,
                velocidade,
                true
            );
        }

        private static float Distancia(float dx, float dz) => (float)Math.Sqrt(dx * dx + dz * dz);

        private static float AnguloEntre(float de, float para, float t)
        {
            var delta = (para - de) % 360f;
            if (delta > 180f)
            {
                delta -= 360f;
            }
            else if (delta < -180f)
            {
                delta += 360f;
            }

            return de + delta * t;
        }
    }
}
