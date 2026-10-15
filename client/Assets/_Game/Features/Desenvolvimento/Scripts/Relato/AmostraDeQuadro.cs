namespace JogoDoGrupo.Client.Features.Desenvolvimento.Relato
{
    /// <summary>Estado de um Animator num quadro.</summary>
    internal struct EstadoDoAnimator
    {
        /// <summary>Se havia Animator para olhar neste quadro.</summary>
        public bool Existe;

        /// <summary>Nome do objeto (o alvo pode trocar no meio da janela).</summary>
        public string Objeto;

        /// <summary>Clipe de maior peso na camada 0; <c>*</c> no fim se está em transição.</summary>
        public string Clipe;

        /// <summary>Tempo normalizado do estado (1 = uma volta do clipe).</summary>
        public float Normalizado;

        /// <summary><c>Animator.speed</c> (0 = pose congelada pelo hit-stop).</summary>
        public float Velocidade;

        /// <summary>Distância até o jogador, em metros (só do alvo).</summary>
        public float Distancia;
    }

    /// <summary>O que o gravador anota em cada quadro guardado.</summary>
    internal sealed class AmostraDeQuadro
    {
        /// <summary><c>Time.frameCount</c>.</summary>
        public int Frame;

        /// <summary>Relógio real, em segundos (não para no hit-stop).</summary>
        public double Tempo;

        public float TimeScale;
        public EstadoDoAnimator Jogador;
        public EstadoDoAnimator Alvo;

        /// <summary>Solavanco da câmera neste quadro, em metros (0 = movimento suave).</summary>
        public float Solavanco;

        /// <summary>Teclas e botões que mudaram no quadro, ex.: <c>↓Mouse 1 ↑W</c>. Vazio se nada.</summary>
        public string Entrada = "";
    }

    /// <summary>Mensagem do servidor recebida durante a janela.</summary>
    internal readonly struct EventoDeRede
    {
        public EventoDeRede(double tempo, ushort id, string nome, string json)
        {
            Tempo = tempo;
            Id = id;
            Nome = nome;
            Json = json;
        }

        public double Tempo { get; }
        public ushort Id { get; }
        public string Nome { get; }
        public string Json { get; }
    }

    /// <summary>Aviso ou erro do console do Unity.</summary>
    internal readonly struct LinhaDoConsole
    {
        public LinhaDoConsole(double tempo, UnityEngine.LogType tipo, string mensagem, string pilha)
        {
            Tempo = tempo;
            Tipo = tipo;
            Mensagem = mensagem ?? "";
            Pilha = pilha ?? "";
        }

        /// <summary>Relógio real, na escala de <see cref="AmostraDeQuadro.Tempo"/>.</summary>
        public double Tempo { get; }
        public UnityEngine.LogType Tipo { get; }
        public string Mensagem { get; }
        public string Pilha { get; }
    }
}
