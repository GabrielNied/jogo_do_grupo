using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Valores do boneco de treino da cidade. Moram no shared (<see cref="RegrasBonecoDeTreino"/>),
/// porque o client põe o modelo na mesma posição; aqui só os nomes que o servidor já usava.
/// </summary>
public static class ConfigBonecoDeTreino
{
    public const long AlvoId = RegrasBonecoDeTreino.AlvoId;
    public const int Vida = RegrasBonecoDeTreino.Vida;
    public const double SegundosParaRecuperar = RegrasBonecoDeTreino.SegundosParaRecuperar;
    public const double X = RegrasBonecoDeTreino.X;
    public const double Z = RegrasBonecoDeTreino.Z;
    public const double Raio = RegrasBonecoDeTreino.Raio;
}
