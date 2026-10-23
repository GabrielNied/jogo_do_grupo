using System.Collections.Generic;

namespace JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto
{
    internal enum TipoDeMarca
    {
        Martelada,
        Fuligem,
    }

    /// <summary>
    /// Regra pura das marcas de impacto: teto de marcas ao mesmo tempo (a mais antiga sai
    /// primeiro), expiração e opacidade com fade. Não conhece a cena; quem usa remove o visual
    /// do que ela devolve.
    /// </summary>
    internal sealed class FilaDeMarcas<T>
    {
        private readonly struct Entrada
        {
            public Entrada(T item, double criadaEm)
            {
                Item = item;
                CriadaEm = criadaEm;
            }

            public T Item { get; }
            public double CriadaEm { get; }
        }

        private readonly LinkedList<Entrada> _marcas = new LinkedList<Entrada>();

        public int Quantidade => _marcas.Count;

        /// <summary>Entra uma marca. Devolve as que saíram para respeitar o <paramref name="teto"/>.</summary>
        public List<T> Adicionar(T item, double agora, int teto)
        {
            var saidas = new List<T>();
            _marcas.AddLast(new Entrada(item, agora));
            var limite = teto < 1 ? 1 : teto;
            while (_marcas.Count > limite)
            {
                saidas.Add(_marcas.First.Value.Item);
                _marcas.RemoveFirst();
            }

            return saidas;
        }

        /// <summary>Tira as marcas que já passaram da <paramref name="duracao"/>.</summary>
        public List<T> Expirar(double agora, float duracao)
        {
            var saidas = new List<T>();
            while (_marcas.Count > 0 && agora - _marcas.First.Value.CriadaEm >= duracao)
            {
                saidas.Add(_marcas.First.Value.Item);
                _marcas.RemoveFirst();
            }

            return saidas;
        }

        /// <summary>Esvazia a fila e devolve tudo o que estava nela.</summary>
        public List<T> Limpar()
        {
            var saidas = new List<T>();
            foreach (var entrada in _marcas)
            {
                saidas.Add(entrada.Item);
            }

            _marcas.Clear();
            return saidas;
        }

        /// <summary>Cada marca com a idade dela, da mais antiga para a mais nova.</summary>
        public IEnumerable<KeyValuePair<T, float>> ComIdade(double agora)
        {
            foreach (var entrada in _marcas)
            {
                yield return new KeyValuePair<T, float>(
                    entrada.Item,
                    (float)(agora - entrada.CriadaEm)
                );
            }
        }

        /// <summary>
        /// Opacidade de 0 a 1 da marca com a <paramref name="idade"/> dada: cheia até os últimos
        /// <paramref name="fade"/> segundos, depois cai em linha reta até zero na
        /// <paramref name="duracao"/>.
        /// </summary>
        public static float Opacidade(float idade, float duracao, float fade)
        {
            if (idade >= duracao)
            {
                return 0f;
            }

            var fadeUtil = fade < 0f ? 0f : (fade > duracao ? duracao : fade);
            var inicio = duracao - fadeUtil;
            if (idade <= inicio || fadeUtil <= 0f)
            {
                return 1f;
            }

            return 1f - (idade - inicio) / fadeUtil;
        }
    }
}
