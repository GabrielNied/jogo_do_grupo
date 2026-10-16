using JogoDoGrupo.Server.Contratos.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Onde fica o grupo de inimigos comuns do campo e como o servidor o monta (proposta, a
/// calibrar). Os números do inimigo em si (visão, velocidade, golpe) são de
/// <c>RegrasInimigo</c>, no shared; vida, dano e tempo de volta são da ficha da criatura.
/// </summary>
public static class ConfigInimigosDoCampo
{
    /// <summary>
    /// Segundos perseguindo sem conseguir começar um ataque até o inimigo desistir (contra
    /// kite na borda do alcance; proposta, a calibrar).
    /// </summary>
    public const double LimiteSemAtacarSegundos = 8.0;

    /// <summary>
    /// Raio, em metros, da puxada em cadeia: atacar um inimigo puxa os grupos da mesma espécie
    /// a até esta distância dele (#915; decidido no quiz de definições 2).
    /// </summary>
    public const double RaioDaPuxadaEmCadeiaMetros = 10.0;

    /// <summary>Área do mundo em que o grupo vive (o campo faz parte da área da cidade).</summary>
    public const string Area = AreasDoMundo.Cidade;

    /// <summary>
    /// Centro do grupo (m, plano do chão): 150 m ao norte do portão norte, que fica em
    /// (0, 40). Decisão da issue #63.
    /// </summary>
    public const double PontoX = 0.0;

    public const double PontoZ = 190.0;

    /// <summary>
    /// Ponto do chefe raro (Presa-Velha), no plano do chão: no campo, uns 60 m a oeste e 40 m
    /// além do grupo de javalis, fora da visão deles (proposta, a calibrar).
    /// </summary>
    public const double PontoDoChefeRaroX = -60.0;

    public const double PontoDoChefeRaroZ = 230.0;

    /// <summary>Id do primeiro (e único) chefe raro; cada aparição soma 1, longe dos do grupo comum.</summary>
    public const long PrimeiroIdDoChefeRaro = 1_000_000_100_000;

    /// <summary>
    /// Raio, em metros, do círculo em volta do centro em que ficam os pontos de origem, a
    /// distâncias iguais um do outro.
    /// </summary>
    public const double RaioDoEspalhamentoMetros = 4.0;

    /// <summary>
    /// De quanto em quanto tempo, em segundos, um grupo sem vaga no teto de vivos tenta
    /// reaparecer de novo.
    /// </summary>
    public const double TentativaDeVagaSegundos = 5.0;

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

    /// <summary>
    /// De quanto em quanto, em metros, o servidor confere o caminho do passo do golpe contra a
    /// borda da cidade e os outros inimigos.
    /// </summary>
    public const double SubpassoDoGolpeMetros = 0.1;
}
