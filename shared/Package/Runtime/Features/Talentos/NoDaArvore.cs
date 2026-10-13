namespace JogoDoGrupo.Shared.Features.Talentos
{
    /// <summary>
    /// Um nó da árvore. As arestas são os <see cref="Requisitos"/> (do requisito para o nó).
    /// <see cref="X"/> e <see cref="Y"/> servem só à tela e não decidem regra.
    /// </summary>
    [System.Serializable]
    public class NoDaArvore
    {
        /// <summary>Id estável: o estado salvo do personagem aponta para ele.</summary>
        public string Id = "";

        public string Nome = "";
        public string Descricao = "";
        public TipoDeNo Tipo = TipoDeNo.Pequeno;

        /// <summary>Um caminho no nó pequeno; dois (vizinhos) no híbrido.</summary>
        public AtributoPrincipal[] Caminhos = new AtributoPrincipal[0];

        public int RankMaximo = 1;

        /// <summary>Custo em pontos da árvore de cada rank. Tem exatamente <see cref="RankMaximo"/> itens.</summary>
        public int[] CustoPorRank = new int[0];

        public RequisitoDeNo[] Requisitos = new RequisitoDeNo[0];
        public EfeitoDeNo[] Efeitos = new EfeitoDeNo[0];

        /// <summary>O efeito só vale para certas skills (a tela mostra o estado condicional).</summary>
        public bool Condicional = false;

        /// <summary>Posição na tela, em unidades do desenho radial (centro em 0,0).</summary>
        public float X = 0f;

        public float Y = 0f;
    }
}
