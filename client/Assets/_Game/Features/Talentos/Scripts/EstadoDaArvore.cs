using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Talentos;

namespace JogoDoGrupo.Client.Features.Talentos
{
    /// <summary>
    /// A árvore como o servidor mandou, mais o nó escolhido e a compra em andamento. O client
    /// não decide nada: pinta os estados e avalia o pedido com as mesmas regras do servidor
    /// (<see cref="RegrasDaArvore"/>) só para avisar antes; quem compra é o servidor.
    /// </summary>
    internal sealed class EstadoDaArvore
    {
        private Dictionary<string, int> _ranks = new Dictionary<string, int>();

        public ConfiguracaoDaArvore Configuracao { get; private set; } = new ConfiguracaoDaArvore();
        public int PontosLivres { get; private set; }
        public int PontosGastos { get; private set; }
        public AtributosDaArvore Atributos { get; private set; } = new AtributosDaArvore();
        public bool Carregada { get; private set; }
        public string NoSelecionadoId { get; private set; }

        /// <summary>Verdadeiro entre enviar a compra e a resposta: só um pedido por vez.</summary>
        public bool Comprando { get; private set; }

        public event Action Mudou;

        public IReadOnlyDictionary<string, int> Ranks => _ranks;

        public NoDaArvore NoSelecionado =>
            NoSelecionadoId == null ? null : RegrasDaArvore.Procurar(Configuracao, NoSelecionadoId);

        public void Aplicar(ArvoreResponse arvore)
        {
            if (arvore == null)
            {
                return;
            }

            Configuracao = arvore.Configuracao ?? new ConfiguracaoDaArvore();
            PontosLivres = arvore.PontosLivres;
            PontosGastos = arvore.PontosGastos;
            Atributos = arvore.Atributos ?? new AtributosDaArvore();
            _ranks = RegrasDaArvore.ParaDicionario(arvore.Ranks);
            Carregada = true;
            if (NoSelecionado == null)
            {
                NoSelecionadoId = null;
            }

            Mudou?.Invoke();
        }

        public void Selecionar(string noId)
        {
            NoSelecionadoId = noId;
            Mudou?.Invoke();
        }

        public void DefinirComprando(bool comprando)
        {
            Comprando = comprando;
            Mudou?.Invoke();
        }

        public int RankDe(NoDaArvore no) => RegrasDaArvore.RankDe(_ranks, no.Id);

        public EstadoDoNo EstadoDe(NoDaArvore no) => RegrasDaArvore.EstadoDe(_ranks, no);

        /// <summary>Se o servidor aceitaria comprar o próximo rank agora (mesma regra dele).</summary>
        public AvaliacaoDeCompra Avaliar(NoDaArvore no) =>
            RegrasDaArvore.AvaliarCompra(Configuracao, _ranks, PontosLivres, no.Id, RankDe(no) + 1);

        /// <summary>O pedido de compra do próximo rank. Repetir o pedido nunca cobra duas vezes.</summary>
        public ComprarNoRequest MontarPedido(NoDaArvore no) =>
            new ComprarNoRequest
            {
                NoId = no.Id,
                RankAlvo = RankDe(no) + 1,
                VersaoConfiguracao = Configuracao.Versao,
            };

        /// <summary>
        /// Atributos da árvore se o próximo rank do nó fosse comprado, para a prévia
        /// antes e depois.
        /// </summary>
        public AtributosDaArvore AtributosDepois(NoDaArvore no)
        {
            var depois = new Dictionary<string, int>(_ranks);
            depois[no.Id] = RankDe(no) + 1;
            return RegrasDaArvore.SomarAtributos(Configuracao, depois);
        }

        /// <summary>Raio máximo dos nós (nas unidades da configuração), com folga para o nó.</summary>
        public float Extensao()
        {
            var maior = 1f;
            foreach (var no in Configuracao.Nos)
            {
                maior = Math.Max(maior, Math.Max(Math.Abs(no.X), Math.Abs(no.Y)));
            }

            return maior + ExtensaoFolga;
        }

        /// <summary>Espaço extra além do nó mais distante, na mesma unidade das posições.</summary>
        public const float ExtensaoFolga = 1.1f;
    }
}
