using System.Collections.Generic;

namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// Regras puras para efeitos de áudio (docs/features/interface.md, Áudio): limite de 16 efeitos
    /// simultâneos cortando os mais distantes primeiro, e corte de sons de outros jogadores além de 25 m.
    /// </summary>
    public static class RegrasEfeitosAudio
    {
        /// <summary>
        /// Sons de outros jogadores (golpes e acertos) só tocam se estiverem a até 25 m de distância.
        /// </summary>
        public static bool PodeOuvirOutroJogador(float distanciaMetros)
        {
            if (distanciaMetros < 0f)
            {
                return false;
            }

            return distanciaMetros <= ConfigAudio.DistanciaMaximaOutrosJogadoresMetros;
        }
    }

    /// <summary>
    /// Gerenciador de vagas para efeitos sonoros simultâneos. No máximo 16 efeitos ao mesmo tempo;
    /// passando disso, os mais distantes são cortados primeiro.
    /// </summary>
    public sealed class ControleDeVagasDeEfeitos
    {
        public readonly struct VagaDeEfeito
        {
            public int Id { get; }
            public float Distancia { get; }

            public VagaDeEfeito(int id, float distancia)
            {
                Id = id;
                Distancia = distancia;
            }
        }

        private readonly int _capacidade;
        private readonly List<VagaDeEfeito> _ativas;
        private int _proximoId = 1;

        public ControleDeVagasDeEfeitos(int capacidade = ConfigAudio.MaximoDeEfeitosSimultaneos)
        {
            _capacidade = capacidade > 0 ? capacidade : ConfigAudio.MaximoDeEfeitosSimultaneos;
            _ativas = new List<VagaDeEfeito>(_capacidade);
        }

        public int AtivasCount => _ativas.Count;
        public IReadOnlyList<VagaDeEfeito> Ativas => _ativas;

        /// <summary>
        /// Tenta alocar uma vaga para tocar um efeito na distância indicada.
        /// Se houver vaga livre, aloca e devolve o novo Id.
        /// Se a capacidade estiver cheia e a nova distância for menor que a do mais distante,
        /// desaloja o mais distante (preenchendo <paramref name="desalocadoId"/>) e aloca o novo.
        /// Se for maior ou igual à do mais distante, recusa (retorna null).
        /// </summary>
        public int? TentarAlocar(float distancia, out int? desalocadoId)
        {
            desalocadoId = null;

            if (_ativas.Count < _capacidade)
            {
                var id = _proximoId++;
                _ativas.Add(new VagaDeEfeito(id, distancia));
                return id;
            }

            var indiceMaisDistante = 0;
            var maiorDistancia = _ativas[0].Distancia;
            for (var i = 1; i < _ativas.Count; i++)
            {
                if (_ativas[i].Distancia > maiorDistancia)
                {
                    maiorDistancia = _ativas[i].Distancia;
                    indiceMaisDistante = i;
                }
            }

            if (distancia < maiorDistancia)
            {
                desalocadoId = _ativas[indiceMaisDistante].Id;
                _ativas.RemoveAt(indiceMaisDistante);
                var id = _proximoId++;
                _ativas.Add(new VagaDeEfeito(id, distancia));
                return id;
            }

            return null;
        }

        /// <summary>Libera a vaga de um efeito que encerrou.</summary>
        public bool Liberar(int id)
        {
            for (var i = 0; i < _ativas.Count; i++)
            {
                if (_ativas[i].Id == id)
                {
                    _ativas.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public void Limpar() => _ativas.Clear();
    }
}
