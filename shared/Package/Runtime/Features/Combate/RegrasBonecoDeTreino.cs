namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// O boneco de treino fixo da cidade: onde fica, quanta vida tem e quando se recupera. Um lugar
    /// só para o servidor (que decide o golpe) e o client (que põe o modelo e a barra de vida).
    /// Valores da proposta, a calibrar.
    /// </summary>
    public static class RegrasBonecoDeTreino
    {
        /// <summary>
        /// Id do alvo que o client vê em <see cref="AcertoDeAtaque.AlvoId"/>. Negativo: nunca
        /// colide com id de personagem nem de inimigo.
        /// </summary>
        public const long AlvoId = -1;

        /// <summary>Vida máxima do boneco (mesma de um inimigo comum da fatia, seção 4).</summary>
        public const int Vida = 180;

        /// <summary>Segundos depois do último golpe em que o boneco volta à vida máxima.</summary>
        public const double SegundosParaRecuperar = 5.0;

        /// <summary>
        /// Posição na cidade (m, plano do chão): 2 m à frente do ponto de nascimento (0, -24),
        /// olhando para +Z, para o jogador novo já alcançar o boneco com a espada.
        /// </summary>
        public const double X = 0.0;

        public const double Z = -22.0;

        /// <summary>Raio do corpo, em metros; desconta da distância do golpe.</summary>
        public const double Raio = 0.5;

        /// <summary>Altura do boneco, em metros (só o client usa, para o modelo e a barra).</summary>
        public const double Altura = 1.8;
    }
}
