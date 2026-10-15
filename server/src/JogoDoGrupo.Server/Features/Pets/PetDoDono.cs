using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Shared.Features.Pets;

namespace JogoDoGrupo.Server.Features.Pets;

/// <summary>O que o pet está fazendo agora.</summary>
public enum AtividadeDoPet
{
    /// <summary>Segue o dono e, de tempos em tempos, procura loot.</summary>
    Seguindo,

    /// <summary>Anda até um item do chão do dono.</summary>
    IndoAoLoot,

    /// <summary>Chegou ao item e espera o servidor de Itens responder; fica parado.</summary>
    Coletando,
}

/// <summary>
/// O pet de um dono, em memória (não persiste: some com o dono). Só o laço do tempo real mexe
/// nele, então não há trava. O id do pet é o do dono: há um por dono.
/// </summary>
public sealed class PetDoDono(
    long donoId,
    EspecieDePet especie,
    float x,
    float y,
    float z,
    float rotacaoY
)
{
    public long DonoId { get; } = donoId;
    public EspecieDePet Especie { get; } = especie;
    public float X { get; set; } = x;
    public float Y { get; set; } = y;
    public float Z { get; set; } = z;
    public float RotacaoY { get; set; } = rotacaoY;
    public bool Andando { get; set; }
    public int Sequencia { get; set; }

    public AtividadeDoPet Atividade { get; set; } = AtividadeDoPet.Seguindo;
    public LootAlcancavel Alvo { get; set; }

    /// <summary>Segundos até a próxima busca de loot.</summary>
    public float ProximaBuscaEm { get; set; }

    /// <summary>Segundos que faltam da pausa por inventário cheio.</summary>
    public float PausaDeLootEm { get; set; }

    /// <summary>Quem já recebeu <c>PetApareceu</c> e ainda não recebeu <c>PetSumiu</c>.</summary>
    public HashSet<long> Vistos { get; } = [];
}
