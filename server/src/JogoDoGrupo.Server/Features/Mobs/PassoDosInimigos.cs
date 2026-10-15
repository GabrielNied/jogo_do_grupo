using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Mundo;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>Um jogador que está na área do grupo, com a posição que o servidor aceitou.</summary>
public readonly record struct JogadorNoCampo(long PersonagemId, double X, double Z);

/// <summary>
/// Um <see cref="InimigoMudou"/> a enviar. <paramref name="SoPosicao"/> diz que só a posição
/// ou a direção mudaram (pode ir pela entrega que aceita perda).
/// </summary>
public readonly record struct MudancaDeInimigo(InimigoMudou Mensagem, bool SoPosicao);

/// <summary>
/// O que aconteceu com o grupo desde o passo anterior: as mensagens para quem está na área e
/// os eventos para as outras funcionalidades.
/// </summary>
public sealed class PassoDosInimigos
{
    public List<MudancaDeInimigo> Mudancas { get; } = [];
    public List<InimigoPreparaAtaque> Preparos { get; } = [];
    public List<InimigoAcertou> Acertos { get; } = [];
    public List<InimigoMorreu> Mortes { get; } = [];
    public List<InimigoApareceu> Aparicoes { get; } = [];
    public List<InimigoExposto> Expostos { get; } = [];
    public List<GolpeDeInimigoAcertou> Golpes { get; } = [];
    public List<InimigoDerrotado> Derrotas { get; } = [];

    /// <summary>Se não há nada a enviar nem a publicar.</summary>
    public bool Vazio =>
        Mudancas.Count == 0
        && Preparos.Count == 0
        && Acertos.Count == 0
        && Mortes.Count == 0
        && Aparicoes.Count == 0
        && Expostos.Count == 0
        && Golpes.Count == 0
        && Derrotas.Count == 0;
}
