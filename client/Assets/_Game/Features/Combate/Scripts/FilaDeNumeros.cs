using System.Collections.Generic;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Um número de dano subindo na tela.</summary>
    internal sealed class NumeroFlutuante
    {
        public NumeroFlutuante(
            string texto,
            bool pesado,
            float deslocamento,
            Vector3? origem = null
        )
        {
            Texto = texto;
            Pesado = pesado;
            Deslocamento = deslocamento;
            Origem = origem;
        }

        public string Texto { get; }

        /// <summary>Se veio de um ataque pesado (aparece maior).</summary>
        public bool Pesado { get; }

        /// <summary>Deslocamento horizontal, em pixels de referência, para não empilhar números.</summary>
        public float Deslocamento { get; }

        /// <summary>
        /// Onde o golpe pegou, no mundo (sobre o alvo). Sem origem, o número aparece em torno da
        /// mira, no centro da tela.
        /// </summary>
        public Vector3? Origem { get; }

        public float Idade { get; set; }

        /// <summary>De 0 (acabou de nascer) a 1 (some).</summary>
        public float Progresso =>
            Idade >= ConfigSensacaoCombate.NumeroSegundos
                ? 1f
                : Idade / ConfigSensacaoCombate.NumeroSegundos;
    }

    /// <summary>
    /// Os números de dano na tela: cada um vive 1 s e há no máximo 20; passando disso, os mais
    /// antigos somem primeiro (docs/features/interface.md, "Texto flutuante de dano").
    /// </summary>
    internal sealed class FilaDeNumeros
    {
        public const int Maximo = 20;

        private readonly List<NumeroFlutuante> _numeros = new List<NumeroFlutuante>();

        public IReadOnlyList<NumeroFlutuante> Itens => _numeros;

        public void Adicionar(string texto, bool pesado, float deslocamento, Vector3? origem = null)
        {
            if (_numeros.Count >= Maximo)
            {
                _numeros.RemoveAt(0);
            }

            _numeros.Add(new NumeroFlutuante(texto, pesado, deslocamento, origem));
        }

        public void Atualizar(float segundos)
        {
            for (var i = _numeros.Count - 1; i >= 0; i--)
            {
                _numeros[i].Idade += segundos;
                if (_numeros[i].Idade >= ConfigSensacaoCombate.NumeroSegundos)
                {
                    _numeros.RemoveAt(i);
                }
            }
        }

        public void Limpar() => _numeros.Clear();
    }
}
