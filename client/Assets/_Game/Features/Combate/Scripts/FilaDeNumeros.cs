using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Um número de dano subindo na tela.</summary>
    internal sealed class NumeroFlutuante
    {
        public NumeroFlutuante(string texto, bool pesado, float deslocamento)
        {
            Texto = texto;
            Pesado = pesado;
            Deslocamento = deslocamento;
        }

        public string Texto { get; }

        /// <summary>Se veio de um ataque pesado (aparece maior).</summary>
        public bool Pesado { get; }

        /// <summary>Deslocamento horizontal, em pixels de referência, para não empilhar números.</summary>
        public float Deslocamento { get; }

        public float Idade { get; set; }

        /// <summary>De 0 (acabou de nascer) a 1 (some).</summary>
        public float Progresso =>
            Idade >= FilaDeNumeros.DuracaoSegundos ? 1f : Idade / FilaDeNumeros.DuracaoSegundos;
    }

    /// <summary>
    /// Os números de dano na tela: cada um vive 1 s e há no máximo 20; passando disso, os mais
    /// antigos somem primeiro (docs/features/interface.md, "Texto flutuante de dano").
    /// </summary>
    internal sealed class FilaDeNumeros
    {
        public const float DuracaoSegundos = 1f;
        public const int Maximo = 20;

        private readonly List<NumeroFlutuante> _numeros = new List<NumeroFlutuante>();

        public IReadOnlyList<NumeroFlutuante> Itens => _numeros;

        public void Adicionar(string texto, bool pesado, float deslocamento)
        {
            if (_numeros.Count >= Maximo)
            {
                _numeros.RemoveAt(0);
            }

            _numeros.Add(new NumeroFlutuante(texto, pesado, deslocamento));
        }

        public void Atualizar(float segundos)
        {
            for (var i = _numeros.Count - 1; i >= 0; i--)
            {
                _numeros[i].Idade += segundos;
                if (_numeros[i].Idade >= DuracaoSegundos)
                {
                    _numeros.RemoveAt(i);
                }
            }
        }

        public void Limpar() => _numeros.Clear();
    }
}
