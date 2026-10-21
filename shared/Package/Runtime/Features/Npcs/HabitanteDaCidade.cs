namespace JogoDoGrupo.Shared.Features.Npcs
{
    /// <summary>
    /// NPC ambiente da cidade (guarda ou morador): não conversa em menu, não luta e não pode ser
    /// atacado. Fica só nos dados: o client o desenha e o move pela <see cref="Rota"/> (#1069).
    /// </summary>
    public sealed class HabitanteDaCidade
    {
        public readonly int Id;
        public readonly string Nome;
        public readonly PapelNpc Papel;

        /// <summary>Semente da aparência (cabelo, barba, cor, altura) para moradores.</summary>
        public readonly int Semente;

        public readonly RotaDeNpc Rota;

        /// <summary>Deslocamento no ciclo da rota, em segundos, para os NPCs não andarem em fila.</summary>
        public readonly float Fase;

        public readonly string[] Falas;

        /// <summary>0 se anda sozinho; senão o id do par com quem conversa.</summary>
        public readonly int ParceiroId;

        /// <summary>Quem fala nos turnos pares (0) ou ímpares (1) da conversa.</summary>
        public readonly int Lado;

        public HabitanteDaCidade(
            int id,
            string nome,
            PapelNpc papel,
            int semente,
            RotaDeNpc rota,
            string[] falas,
            float fase = 0f,
            int parceiroId = 0,
            int lado = 0
        )
        {
            Id = id;
            Nome = nome;
            Papel = papel;
            Semente = semente;
            Rota = rota;
            Falas = falas;
            Fase = fase;
            ParceiroId = parceiroId;
            Lado = lado;
        }
    }
}
