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

    /// <summary>Experiência acumulada dentro do nível atual. No teto, passa do custo do nível.</summary>
    public long XpAtual { get; set; }

    /// <summary>Pontos de atributo ganhos ao subir de nível e ainda não gastos.</summary>
    public int PontosLivres { get; set; }

    /// <summary>Pontos que o jogador já distribuiu. Sem base, equipamento nem efeito.</summary>
    public int Forca { get; set; }

    public int Agilidade { get; set; }

    public int Intelecto { get; set; }

    public int Vigor { get; set; }

    public int Espirito { get; set; }

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

    public ProgressoResponse ParaProgresso() =>
        new()
        {
            Nivel = Nivel,
            XpAtual = XpAtual,
            XpDoNivel = RegrasExperiencia.XpParaOProximoNivel(Nivel),
            PontosLivres = PontosLivres,
            Distribuidos = ParaPontos(),
        };

    public PontosDeAtributos ParaPontos() =>
        new()
        {
            Forca = Forca,
            Agilidade = Agilidade,
            Intelecto = Intelecto,
            Vigor = Vigor,
            Espirito = Espirito,
        };
}
