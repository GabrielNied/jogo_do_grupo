namespace JogoDoGrupo.Server.Contratos.Combate;

/// <summary>
/// Algo que pode levar um golpe: o boneco de treino da cidade hoje, inimigos e bosses quando
/// Mobs existir. <see cref="Id"/> é o <c>AlvoId</c> que o client vê em <c>AcertoDeAtaque</c>;
/// os ids são únicos entre todas as implementações de <see cref="IAlvosDeCombate"/> (o boneco de
/// treino usa um id negativo fixo). <see cref="PersonagemId"/> é o personagem que o alvo
/// representa quando é um jogador (0 para boneco, inimigo e chefe); é por ele que o golpe poupa
/// o próprio atacante e os aliados. A posição é no plano do chão (a altura não conta).
/// </summary>
public readonly record struct AlvoDeCombate(
    long Id,
    double X,
    double Z,
    double Raio,
    int VidaAtual,
    int VidaMaxima,
    long PersonagemId = 0
);

/// <summary>O que um golpe fez num alvo: o dano que ele realmente levou e a vida que sobrou.</summary>
public readonly record struct ResultadoDeDano(
    long AlvoId,
    int Dano,
    int VidaRestante,
    int VidaMaxima
);

/// <summary>
/// Consulta e dano de alvos de combate, oferecidos por quem é dono do alvo às outras
/// funcionalidades (ADR 0016). Combate pede por <c>IEnumerable&lt;IAlvosDeCombate&gt;</c>: cada
/// funcionalidade com alvos registra a sua implementação (<c>AddSingleton</c>). Pode ser chamada
/// de qualquer thread.
/// </summary>
public interface IAlvosDeCombate
{
    /// <summary>
    /// Os alvos <b>vivos</b> da área que estão no volume do golpe (cone à frente do atacante, de
    /// <c>RegrasAtaque.EstaNoVolume</c> no shared; <paramref name="direcaoYGraus"/> segue a
    /// convenção de <c>IntencaoDeAtaque.DirecaoY</c>). Vazio se a área não é desta implementação.
    /// </summary>
    IReadOnlyList<AlvoDeCombate> AlvosNoVolume(
        string area,
        double origemX,
        double origemZ,
        double direcaoYGraus
    );

    /// <summary>
    /// Tira vida do alvo. <paramref name="atacanteId"/> é o personagem que deu o golpe (quem
    /// recebe o dano pode usá-lo para saber quem participou da morte). Falso se ele não é desta implementação ou já não está vivo (por
    /// exemplo, outro golpe o derrubou antes). O dano de <paramref name="resultado"/> é o que
    /// de fato foi tirado, nunca mais que a vida que ele tinha.
    /// </summary>
    bool TentarAplicarDano(long alvoId, int dano, long atacanteId, out ResultadoDeDano resultado);

    /// <summary>
    /// Um acerto confirmado de Combate pede que o alvo pare o golpe comum que prepara (reação ao
    /// acerto). O alvo cancela o preparo (o golpe não chega ao contato), não ataca de novo por
    /// <paramref name="duracaoMs"/> e devolve verdadeiro. Falso, sem mudar nada, se não é desta
    /// implementação, não está preparando um golpe ou não pode ser interrompido (o chefe de
    /// dungeon nunca é). Quem não tem preparo a interromper usa o padrão, que sempre devolve
    /// falso. Combate controla a folga entre reações e só chama quando uma vale.
    /// </summary>
    bool Interromper(long alvoId, int duracaoMs) => false;
}
