using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Onde fica o grupo de inimigos comuns do campo e como o servidor o monta (proposta, a
/// calibrar). Os números do inimigo em si (visão, velocidade, golpe) são de
/// <c>RegrasInimigo</c>, no shared; vida, dano e tempo de volta são da ficha da criatura.
/// </summary>
public static class ConfigInimigosDoCampo
{
    /// <summary>Área do mundo em que o grupo vive (o campo faz parte da área da cidade).</summary>
    public const string Area = AreasDoMundo.Cidade;

    /// <summary>
    /// Centro do grupo (m, plano do chão): 150 m ao norte do portão norte, que fica em
    /// (0, 40). Decisão da issue #63.
    /// </summary>
    public const double PontoX = 0.0;

    public const double PontoZ = 190.0;

    /// <summary>
    /// Raio, em metros, do círculo em volta do centro em que ficam os pontos de origem, a
    /// distâncias iguais um do outro.
    /// </summary>
    public const double RaioDoEspalhamentoMetros = 4.0;

    /// <summary>Para onde o inimigo olha em repouso, em graus (180 = para a cidade, ao sul).</summary>
    public const double DirecaoDeRepousoGraus = 180.0;

    /// <summary>
    /// Id do primeiro inimigo. Os seguintes somam 1, e cada volta depois de morto ganha um id
    /// novo. Alto assim para nunca coincidir com id de personagem nem com o boneco de treino
    /// (negativo), porque o client recebe todos como <c>AlvoId</c>.
    /// </summary>
    public const long PrimeiroInimigoId = 1_000_000_000_000;

    /// <summary>
    /// Maior tempo, em segundos, que um passo da IA conta para o movimento. Se o laço atrasar,
    /// o inimigo não salta a distância toda de uma vez.
    /// </summary>
    public const double PassoMaximoSegundos = 0.5;

    /// <summary>
    /// Quanto antes do alcance do ataque, em metros, o inimigo para de andar. Sem a folga, o
    /// arredondamento dos números podia deixá-lo um fio fora do alcance, sem nunca atacar.
    /// </summary>
    public const double FolgaDoAlcanceMetros = 0.1;

    /// <summary>A esta distância do ponto de origem, em metros, a volta já terminou.</summary>
    public const double ToleranciaDeChegadaMetros = 0.05;
}
