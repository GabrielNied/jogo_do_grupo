using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Features.Combate;

/// <summary>
/// Contas de Combate sem banco nem relógio de parede: atributos finais, máximos e
/// regeneração. Regras e números em <see cref="RegrasRecursos"/>.
/// </summary>
internal static class CalculadoraRecursos
{
    /// <summary>Valor base de cada atributo (proposta; Personagem ainda não define).</summary>
    public const int AtributoBase = 0;

    /// <summary>
    /// Zero usado por <see cref="AtributosFinais(AtributosCombate, AtributosCombate)"/>.
    /// A rota, o ataque e o dano leem os pontos de <c>IPontosDistribuidos</c>.
    /// </summary>
    public const int PontosDistribuidos = 0;

    public static AtributosCombate AtributosFinais(
        AtributosCombate equipamentos,
        AtributosCombate efeitos
    ) =>
        AtributosFinais(
            equipamentos,
            efeitos,
            new PontosDeAtributos
            {
                Forca = PontosDistribuidos,
                Destreza = PontosDistribuidos,
                Intelecto = PontosDistribuidos,
                Vitalidade = PontosDistribuidos,
                Espirito = PontosDistribuidos,
            }
        );

    public static AtributosCombate AtributosFinais(
        AtributosCombate equipamentos,
        AtributosCombate efeitos,
        PontosDeAtributos pontos
    ) =>
        new()
        {
            Forca = Final(pontos.Forca, equipamentos.Forca, efeitos.Forca),
            Destreza = Final(pontos.Destreza, equipamentos.Destreza, efeitos.Destreza),
            Intelecto = Final(pontos.Intelecto, equipamentos.Intelecto, efeitos.Intelecto),
            Vitalidade = Final(pontos.Vitalidade, equipamentos.Vitalidade, efeitos.Vitalidade),
            Espirito = Final(pontos.Espirito, equipamentos.Espirito, efeitos.Espirito),
        };

    private static int Final(int pontos, int equipamentos, int efeitos) =>
        Math.Max(0, RegrasRecursos.ValorFinal(AtributoBase, pontos, equipamentos, efeitos));

    /// <summary>Cria os recursos cheios para um personagem novo.</summary>
    public static RecursosPersonagem Novo(long personagemId, AtributosCombate a, DateTime agora) =>
        new()
        {
            PersonagemId = personagemId,
            Vida = RegrasRecursos.VidaMaxima(a.Vitalidade),
            Vigor = RegrasRecursos.VigorMaximo(),
            Energia = RegrasRecursos.EnergiaMaxima(a.Espirito),
            VigorContadoAte = agora,
            EnergiaContadaAte = agora,
        };

    /// <summary>
    /// Limita ao novo máximo e regenera vigor e energia até <paramref name="agora"/>. Muda
    /// <paramref name="r"/> no lugar.
    /// </summary>
    public static void Atualizar(RecursosPersonagem r, AtributosCombate a, DateTime agora)
    {
        r.Vida = RegrasRecursos.Limitar(r.Vida, RegrasRecursos.VidaMaxima(a.Vitalidade));

        (r.Vigor, r.VigorContadoAte) = Regenerar(
            r.Vigor,
            RegrasRecursos.VigorMaximo(),
            RegrasRecursos.RegeneracaoVigorPorSegundo,
            r.UltimoGastoVigorEm,
            r.VigorContadoAte,
            agora
        );

        (r.Energia, r.EnergiaContadaAte) = Regenerar(
            r.Energia,
            RegrasRecursos.EnergiaMaxima(a.Espirito),
            RegrasRecursos.RegeneracaoEnergiaPorSegundo,
            r.UltimoGastoEnergiaEm,
            r.EnergiaContadaAte,
            agora
        );
    }

    private static (int Atual, DateTime ContadoAte) Regenerar(
        int atual,
        int maximo,
        double taxa,
        DateTime ultimoGasto,
        DateTime contadoAte,
        DateTime agora
    )
    {
        var limitado = RegrasRecursos.Limitar(atual, maximo);
        if (agora <= contadoAte)
        {
            // Relógio andou para trás ou consulta no mesmo instante: nada a regenerar.
            return (limitado, contadoAte);
        }

        var novo = RegrasRecursos.Regenerar(
            limitado,
            maximo,
            taxa,
            (agora - ultimoGasto).TotalSeconds,
            (agora - contadoAte).TotalSeconds
        );
        if (novo >= maximo)
        {
            return (novo, agora);
        }

        var ganho = novo - limitado;
        if (ganho <= 0)
        {
            // Ainda dentro do atraso (ou menos de 1 ponto): o tempo não se perde.
            return (novo, contadoAte);
        }

        // Paga só o tempo que virou pontos inteiros; a fração fica para a próxima consulta.
        var fimDoAtraso = ultimoGasto.AddSeconds(RegrasRecursos.AtrasoRegeneracaoSegundos);
        var inicio = contadoAte > fimDoAtraso ? contadoAte : fimDoAtraso;
        return (novo, inicio.AddSeconds(ganho / taxa));
    }
}
