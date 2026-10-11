namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Valores do boneco de treino da cidade (proposta, a calibrar). Só o servidor usa; o client
/// coloca o modelo na mesma posição enquanto o contrato não tem mensagem para isso.
/// </summary>
public static class ConfigBonecoDeTreino
{
    /// <summary>
    /// Id do alvo que o client vê em <c>AcertoDeAtaque.AlvoId</c>. Negativo: nunca colide com id
    /// de personagem nem de inimigo.
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
}
