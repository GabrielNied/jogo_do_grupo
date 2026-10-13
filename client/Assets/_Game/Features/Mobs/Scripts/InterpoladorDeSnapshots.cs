using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Mobs
{
    /// <summary>Uma pose do servidor com o instante (local) em que valeu.</summary>
    internal readonly struct SnapshotDePose
    {
        public SnapshotDePose(double tempo, float x, float y, float z, float rotacaoY, int etiqueta)
        {
            Tempo = tempo;
            X = x;
            Y = y;
            Z = z;
            RotacaoY = rotacaoY;
            Etiqueta = etiqueta;
        }

        public double Tempo { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RotacaoY { get; }

        /// <summary>Valor opaco que acompanha a pose (ex.: o estado); vale o do pacote mais antigo do trecho.</summary>
        public int Etiqueta { get; }
    }

    /// <summary>A pose calculada para um instante de render.</summary>
    internal readonly struct PoseAmostrada
    {
        public PoseAmostrada(
            float x,
            float y,
            float z,
            float rotacaoY,
            float velocidade,
            int etiqueta,
            bool extrapolando
        )
        {
            X = x;
            Y = y;
            Z = z;
            RotacaoY = rotacaoY;
            Velocidade = velocidade;
            Etiqueta = etiqueta;
            Extrapolando = extrapolando;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RotacaoY { get; }

        /// <summary>Velocidade horizontal (m/s) vinda dos pacotes, não da pose mostrada.</summary>
        public float Velocidade { get; }

        public int Etiqueta { get; }
        public bool Extrapolando { get; }
    }

    /// <summary>
    /// Buffer de snapshots com interpolação entre os dois pacotes que cercam o instante pedido
    /// (o chamador pede um instante atrasado, <see cref="ConfigMovimentoDoInimigo.AtrasoDeRenderSegundos"/>)
    /// e extrapolação curta quando falta o pacote seguinte. Lógica pura, sem Unity: serve a
    /// qualquer entidade que chega por pacotes (os outros jogadores, #499, podem copiar o uso).
    /// </summary>
    internal sealed class InterpoladorDeSnapshots
    {
        private readonly List<SnapshotDePose> _buffer = new List<SnapshotDePose>();
        private readonly float _extrapolacaoMaxima;
        private readonly float _salto;
        private readonly int _capacidade;

        // Velocidade do último trecho (m/s), para extrapolar depois do pacote mais novo.
        private float _vx;
        private float _vy;
        private float _vz;

        public InterpoladorDeSnapshots(
            float extrapolacaoMaximaSegundos = ConfigMovimentoDoInimigo.ExtrapolacaoMaximaSegundos,
            float saltoDeTeletransporteMetros =
                ConfigMovimentoDoInimigo.SaltoDeTeletransporteMetros,
            int capacidade = ConfigMovimentoDoInimigo.PacotesNoBuffer
        )
        {
            _extrapolacaoMaxima = extrapolacaoMaximaSegundos;
            _salto = saltoDeTeletransporteMetros;
            _capacidade = Math.Max(2, capacidade);
        }

        public int Quantidade => _buffer.Count;

        public bool Vazio => _buffer.Count == 0;

        public void Limpar()
        {
            _buffer.Clear();
            _vx = _vy = _vz = 0f;
        }

        /// <summary>
        /// Guarda o pacote. Devolve falso (e descarta) se o instante não é maior que o do pacote
        /// mais novo: fora de ordem ou repetido.
        /// </summary>
        public bool Adicionar(SnapshotDePose snapshot)
        {
            if (_buffer.Count > 0)
            {
                var ultimo = _buffer[_buffer.Count - 1];
                if (snapshot.Tempo <= ultimo.Tempo)
                {
                    return false;
                }

                var dx = snapshot.X - ultimo.X;
                var dz = snapshot.Z - ultimo.Z;
                if (Distancia(dx, dz) > _salto)
                {
                    _vx = _vy = _vz = 0f;
                }
                else
                {
                    var dt = (float)(snapshot.Tempo - ultimo.Tempo);
                    _vx = dx / dt;
                    _vy = (snapshot.Y - ultimo.Y) / dt;
                    _vz = dz / dt;
                }
            }

            _buffer.Add(snapshot);
            if (_buffer.Count > _capacidade)
            {
                _buffer.RemoveAt(0);
            }

            return true;
        }

        /// <summary>
        /// A pose no instante pedido (o buffer não pode estar vazio). Antes do primeiro pacote,
        /// fica nele; entre dois, interpola; depois do último, segue a velocidade dele até a
        /// extrapolação máxima e então para. Descarta o que ficou para trás: os instantes
        /// pedidos só crescem.
        /// </summary>
        public PoseAmostrada Amostrar(double tempo)
        {
            while (_buffer.Count >= 2 && _buffer[1].Tempo <= tempo)
            {
                _buffer.RemoveAt(0);
            }

            var a = _buffer[0];
            if (_buffer.Count == 1)
            {
                return tempo > a.Tempo
                    ? Extrapolar(a, tempo)
                    : new PoseAmostrada(a.X, a.Y, a.Z, a.RotacaoY, 0f, a.Etiqueta, false);
            }

            var b = _buffer[1];
            var dx = b.X - a.X;
            var dz = b.Z - a.Z;
            if (tempo <= a.Tempo || Distancia(dx, dz) > _salto)
            {
                // Antes do primeiro pacote, ou teletransporte: fica no ponto antigo até o
                // instante do novo, então pula.
                return new PoseAmostrada(a.X, a.Y, a.Z, a.RotacaoY, 0f, a.Etiqueta, false);
            }

            var dt = b.Tempo - a.Tempo;
            var t = (float)((tempo - a.Tempo) / dt);
            return new PoseAmostrada(
                a.X + dx * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + dz * t,
                AnguloEntre(a.RotacaoY, b.RotacaoY, t),
                Distancia(dx, dz) / (float)dt,
                a.Etiqueta,
                false
            );
        }

        private PoseAmostrada Extrapolar(SnapshotDePose ultimo, double tempo)
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
                ultimo.Etiqueta,
                true
            );
        }

        private static float Distancia(float dx, float dz) => (float)Math.Sqrt(dx * dx + dz * dz);

        // Pelo caminho curto (359 -> 1 passa por 0).
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
