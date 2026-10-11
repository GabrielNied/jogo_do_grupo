using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Personagem;

/// <summary>Personagem de uma conta (regras em docs/features/personagem.md).</summary>
public sealed class Personagem
{
    public const int NivelInicial = 1;

    public long Id { get; set; }

    /// <summary>Conta dona do personagem (sempre a do token, nunca enviada pelo client).</summary>
    public long ContaId { get; set; }

    /// <summary>Nome como foi digitado (mantém as maiúsculas).</summary>
    public string Nome { get; set; } = "";

    /// <summary>Nome em minúsculas, com índice único: "Ana" e "ana" são o mesmo nome.</summary>
    public string NomeNormalizado { get; set; } = "";

    public Raca Raca { get; set; }

    public int Nivel { get; set; } = NivelInicial;

    public DateTime CriadoEm { get; set; }

    public static string Normalizar(string nome) => nome.ToLowerInvariant();

    public PersonagemResumo ParaResumo() =>
        new()
        {
            Id = Id,
            Nome = Nome,
            Raca = Raca,
            Nivel = Nivel,
        };
}
