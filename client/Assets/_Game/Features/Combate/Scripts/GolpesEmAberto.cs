using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Os golpes do próprio jogador que ainda esperam o resultado do servidor. Um golpe sem
    /// <see cref="AtaqueAcertou"/> até o contato mais a <see cref="ToleranciaDeRedeSegundos"/>
    /// errou: o client dá o retorno do golpe no vazio. Liga o acerto ao golpe pela
    /// <c>Sequencia</c>, não pelo último ataque iniciado.
    /// </summary>
    internal sealed class GolpesEmAberto
    {
        /// <summary>Quanto o resultado pode demorar a chegar depois do contato, em segundos.</summary>
        public const double ToleranciaDeRedeSegundos = 0.25;

        /// <summary>Quantos golpes sem resposta o client guarda (os mais antigos saem).</summary>
        public const int Maximo = 8;

        public readonly struct Golpe
        {
            public Golpe(int sequencia, TipoDeAtaque tipo, float direcaoY, double prazo)
            {
                Sequencia = sequencia;
                Tipo = tipo;
                DirecaoY = direcaoY;
                Prazo = prazo;
            }

            public int Sequencia { get; }
            public TipoDeAtaque Tipo { get; }
            public float DirecaoY { get; }

            /// <summary>Quando o golpe passa a contar como errado, no relógio do client.</summary>
            public double Prazo { get; }
        }

        private readonly List<Golpe> _golpes = new List<Golpe>();

        public int Quantidade => _golpes.Count;

        /// <summary>Anota um golpe que começou em <paramref name="agora"/>.</summary>
        public void Registrar(int sequencia, TipoDeAtaque tipo, float direcaoY, double agora)
        {
            if (_golpes.Count >= Maximo)
            {
                _golpes.RemoveAt(0);
            }

            _golpes.Add(
                new Golpe(
                    sequencia,
                    tipo,
                    direcaoY,
                    agora + RegrasAtaque.PreparoSegundos(tipo) + ToleranciaDeRedeSegundos
                )
            );
        }

        /// <summary>
        /// O servidor confirmou o golpe de <paramref name="sequencia"/>. Devolve o golpe anotado
        /// (<c>false</c> se não era um golpe em aberto, por exemplo o de outro dispositivo).
        /// </summary>
        public bool Confirmar(int sequencia, out Golpe golpe)
        {
            for (var i = 0; i < _golpes.Count; i++)
            {
                if (_golpes[i].Sequencia == sequencia)
                {
                    golpe = _golpes[i];
                    _golpes.RemoveAt(i);
                    return true;
                }
            }

            golpe = default;
            return false;
        }

        /// <summary>Tira os golpes cujo prazo passou sem resposta e devolve o primeiro, se houver.</summary>
        public bool TirarVencido(double agora, out Golpe golpe)
        {
            for (var i = 0; i < _golpes.Count; i++)
            {
                if (agora >= _golpes[i].Prazo)
                {
                    golpe = _golpes[i];
                    _golpes.RemoveAt(i);
                    return true;
                }
            }

            golpe = default;
            return false;
        }

        public void Limpar() => _golpes.Clear();
    }
}
