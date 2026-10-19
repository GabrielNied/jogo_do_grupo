namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// O preparo de skill forte que o servidor confirmou, contado no relógio local. Só mostra: quem
    /// decide quando acaba, cobra e aplica é o servidor, que avisa o fim (usada, recusada ou
    /// cancelada). A folga evita a barra presa se a mensagem de fim se perder.
    /// </summary>
    internal sealed class EstadoDoPreparo
    {
        /// <summary>Quanto esperar pela mensagem de fim depois do tempo do preparo, em segundos.</summary>
        public const double FolgaDoFimSegundos = 1.5;

        private double _inicio;
        private double _duracao;

        public bool Ativo { get; private set; }

        public int Espaco { get; private set; }

        public int Sequencia { get; private set; }

        public string Nome { get; private set; } = "";

        public void Iniciar(int espaco, int sequencia, string nome, int preparoMs, double agora)
        {
            Ativo = preparoMs > 0;
            Espaco = espaco;
            Sequencia = sequencia;
            Nome = nome ?? "";
            _inicio = agora;
            _duracao = preparoMs / 1000.0;
        }

        public void Encerrar() => Ativo = false;

        /// <summary>Quanto do preparo já passou, de 0 a 1.</summary>
        public float Fracao(double agora)
        {
            if (!Ativo || _duracao <= 0)
            {
                return 0f;
            }

            var f = (agora - _inicio) / _duracao;
            return f < 0 ? 0f
                : f > 1 ? 1f
                : (float)f;
        }

        /// <summary>Se passou da hora do preparo mais a folga sem o servidor avisar o fim.</summary>
        public bool Expirou(double agora) =>
            Ativo && agora > _inicio + _duracao + FolgaDoFimSegundos;
    }
}
