using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Shared.Features.Mobs;

namespace JogoDoGrupo.Server.Features.Mobs;

/// <summary>
/// Os números do boss da primeira dungeon (issue #66, docs/features/mobs.md, "Boss da dungeon").
/// Tudo é <b>proposta a calibrar</b>. A força é fixa: a vida e o dano não mudam com o número de
/// jogadores. Vida, XP e nível vêm da ficha do Guardião do Reflexo no <c>CatalogoCriaturas</c>.
/// </summary>
public static class ConfigChefeDaDungeon
{
    // ── Quem e onde ──────────────────────────────────────────────────────────────

    /// <summary>Criatura do boss no <c>CatalogoCriaturas</c>.</summary>
    public const int CriaturaId = CatalogoCriaturas.GuardiaoDoReflexo;

    /// <summary>Vida máxima, a mesma para 1 a 5 jogadores (da ficha da criatura).</summary>
    public static readonly int VidaMaxima = CatalogoCriaturas.Obter(CriaturaId).VidaBase;

    /// <summary>
    /// Se o servidor já sobe com um boss de teste no mundo, enquanto a instância da dungeon não
    /// o cria sozinha. Quando ela existir, desligue aqui.
    /// </summary>
    public const bool CriarChefeDeTesteAoSubir = true;

    /// <summary>Área e ponto do boss de teste: nas colinas a oeste, longe da cidade.</summary>
    public const string AreaDeTeste = AreasDoMundo.Cidade;

    public const double PontoDeTesteX = -200.0;
    public const double PontoDeTesteZ = 0.0;

    /// <summary>Id do primeiro boss; cada boss novo soma 1 (nunca coincide com personagem).</summary>
    public const long PrimeiroIdDoChefe = 1_000_000_200_000;

    /// <summary>Raio do corpo do boss como alvo dos golpes dos jogadores (é grande).</summary>
    public const double RaioDoCorpoMetros = 2.0;

    // ── Ritmo ────────────────────────────────────────────────────────────────────

    /// <summary>Longe assim da origem, em metros, o jogador não conta para a luta.</summary>
    public const double DistanciaDeDespertarMetros = 30.0;

    /// <summary>Segundos entre ver um jogador e o primeiro ataque começar.</summary>
    public const double AlertaSegundos = 0.5;

    /// <summary>Aviso de todo ataque telegrafado: o tempo entre o aviso e o acerto.</summary>
    public const double AvisoDoAtaqueSegundos = 3.0;

    /// <summary>Segundos de pausa entre o fim de um ataque e o aviso do próximo.</summary>
    public const double PausaEntreAtaquesSegundos = 2.0;

    /// <summary>Velocidade da volta à origem quando a sala esvazia (m/s).</summary>
    public const double VelocidadeDeRetornoMetrosPorSegundo = 8.0;

    /// <summary>Ordem dos ataques; depois do último recomeça do primeiro.</summary>
    public static readonly TipoDeAtaqueDeInimigo[] OrdemDosAtaques =
    [
        TipoDeAtaqueDeInimigo.Investida,
        TipoDeAtaqueDeInimigo.Raios,
        TipoDeAtaqueDeInimigo.Pisada,
    ];

    // ── Ataques ──────────────────────────────────────────────────────────────────

    /// <summary>Investida: dano, raio da área no ponto marcado, alcance máximo e recuperação.</summary>
    public const int DanoDaInvestida = 150;

    public const double RaioDaInvestidaMetros = 3.0;
    public const double AlcanceDaInvestidaMetros = 25.0;
    public const double RecuperacaoDaInvestidaSegundos = 2.0;

    /// <summary>Pisada: dano e raio da área em volta do boss.</summary>
    public const int DanoDaPisada = 100;

    public const double RaioDaPisadaMetros = 9.0;
    public const double RecuperacaoDaPisadaSegundos = 2.5;

    /// <summary>Raios: dano de cada raio, tamanho, quantos caem e a recuperação.</summary>
    public const int DanoDoRaio = 80;

    public const double RaioDoRaioMetros = 2.5;
    public const int RaiosPorAtaque = 4;

    /// <summary>
    /// Os raios vão para jogadores diferentes, em rodízio. Se há menos jogadores que raios, os
    /// que sobram caem assim de longe (m) do jogador, em ângulos espalhados.
    /// </summary>
    public const double DesvioDosRaiosExtrasMetros = 4.0;

    public const double RecuperacaoDosRaiosSegundos = 1.5;

    // ── Janela exposta ───────────────────────────────────────────────────────────

    /// <summary>Depois destes ataques o boss fica exposto (a janela abre no acerto).</summary>
    public static readonly TipoDeAtaqueDeInimigo[] AtaquesQueExpoem =
    [
        TipoDeAtaqueDeInimigo.Investida,
        TipoDeAtaqueDeInimigo.Pisada,
    ];

    /// <summary>Quanto dura a janela exposta.</summary>
    public const double DuracaoDaJanelaExpostaSegundos = 5.0;

    /// <summary>Por quanto o dano recebido é multiplicado enquanto ele está exposto.</summary>
    public const double MultiplicadorDeDanoExposto = 2.0;

    // ── Consultas ────────────────────────────────────────────────────────────────

    /// <summary>Dano do ataque (sem defesa, antes de qualquer cálculo do Combate).</summary>
    public static int DanoDe(TipoDeAtaqueDeInimigo tipo) =>
        tipo switch
        {
            TipoDeAtaqueDeInimigo.Investida => DanoDaInvestida,
            TipoDeAtaqueDeInimigo.Pisada => DanoDaPisada,
            TipoDeAtaqueDeInimigo.Raios => DanoDoRaio,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "Ataque de boss inválido."
            ),
        };

    /// <summary>Segundos parado depois do acerto do ataque.</summary>
    public static double RecuperacaoDe(TipoDeAtaqueDeInimigo tipo) =>
        tipo switch
        {
            TipoDeAtaqueDeInimigo.Investida => RecuperacaoDaInvestidaSegundos,
            TipoDeAtaqueDeInimigo.Pisada => RecuperacaoDaPisadaSegundos,
            TipoDeAtaqueDeInimigo.Raios => RecuperacaoDosRaiosSegundos,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tipo),
                tipo,
                "Ataque de boss inválido."
            ),
        };
}
