using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
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
    /// Zero usado por <see cref="AtributosFinais(AtributosCombate)"/>. A rota, o ataque e o dano
    /// leem os pontos de <c>IPontosDistribuidos</c>.
    /// </summary>
    public const int PontosDistribuidos = 0;

    public static AtributosCombate AtributosFinais(AtributosCombate efeitos) =>
        AtributosFinais(
            efeitos,
            new PontosDeAtributos
            {
                Forca = PontosDistribuidos,
                Agilidade = PontosDistribuidos,
                Intelecto = PontosDistribuidos,
                Vigor = PontosDistribuidos,
                Espirito = PontosDistribuidos,
            }
        );

    /// <summary>
    /// Os cinco atributos do personagem: base + pontos distribuídos + efeitos. O equipamento não
    /// entra aqui, nunca (DEC-001): ele é uma fonte à parte (<see cref="Maximos"/>,
    /// <see cref="ParcelasDeDano"/> e os secundários da pipeline).
    /// </summary>
    public static AtributosCombate AtributosFinais(
        AtributosCombate efeitos,
        PontosDeAtributos pontos
    ) =>
        new()
        {
            Forca = Final(pontos.Forca, efeitos.Forca),
            Agilidade = Final(pontos.Agilidade, efeitos.Agilidade),
            Intelecto = Final(pontos.Intelecto, efeitos.Intelecto),
            Vigor = Final(pontos.Vigor, efeitos.Vigor),
            Espirito = Final(pontos.Espirito, efeitos.Espirito),
        };

    private static int Final(int pontos, int efeitos) =>
        Math.Max(0, RegrasRecursos.ValorFinal(AtributoBase, pontos, efeitos));

    /// <summary>
    /// Vida, stamina e mana máximas: o que o personagem tem pelos atributos dele mais o que as
    /// peças dão direto (Vigor, Agilidade e Espírito dos itens, já convertidos).
    /// </summary>
    public static MaximosDeRecursos Maximos(
        AtributosCombate personagem,
        AtributosDosEquipamentos equipamento
    ) =>
        new(
            RegrasRecursos.VidaMaxima(personagem.Vigor, equipamento.VidaMaxima),
            RegrasRecursos.StaminaMaxima(personagem.Agilidade, equipamento.StaminaMaxima),
            RegrasRecursos.ManaMaxima(personagem.Espirito, equipamento.ManaMaxima)
        );

    /// <summary>
    /// As parcelas do dano do golpe básico (combate.md): o dano-base é o da arma equipada (ou
    /// dos punhos); a Força (arma física) ou o Intelecto (arma mágica) do personagem e a do item
    /// aumentam em %, cada uma na sua parcela.
    /// </summary>
    public static ParcelasDeDano ParcelasDoDano(
        AtributosCombate personagem,
        AtributosDosEquipamentos equipamento
    )
    {
        var magico = equipamento.TemArma && equipamento.ArmaMagica;
        var pontos = magico ? personagem.Intelecto : personagem.Forca;
        return new ParcelasDeDano
        {
            DanoBase = equipamento.TemArma ? equipamento.DanoDaArma : ParcelasDeDano.DanoSemArma,
            Magico = magico,
            PercentualPersonagem = RegrasAtaque.PercentualDeDanoPorPonto * Math.Max(0, pontos),
            PercentualEquipamento = magico
                ? equipamento.PercentualIntelecto
                : equipamento.PercentualForca,
        };
    }

    /// <summary>Cria os recursos cheios para um personagem novo.</summary>
    public static RecursosPersonagem Novo(
        long personagemId,
        MaximosDeRecursos maximos,
        DateTime agora
    ) =>
        new()
        {
            PersonagemId = personagemId,
            Vida = maximos.Vida,
            Stamina = maximos.Stamina,
            Mana = maximos.Mana,
            StaminaContadaAte = agora,
            ManaContadaAte = agora,
        };

    /// <summary>
    /// Limita ao novo máximo e regenera stamina e mana até <paramref name="agora"/>. Muda
    /// <paramref name="r"/> no lugar.
    /// </summary>
    public static void Atualizar(
        RecursosPersonagem r,
        MaximosDeRecursos maximos,
        AtributosSecundarios s,
        ref DateTime vidaContadaAte,
        DateTime agora
    )
    {
        (r.Vida, vidaContadaAte) = RegenerarVida(
            r.Vida,
            maximos.Vida,
            s.RegeneracaoVida,
            vidaContadaAte,
            agora
        );

        (r.Stamina, r.StaminaContadaAte) = Regenerar(
            r.Stamina,
            maximos.Stamina,
            s.RegeneracaoStamina,
            r.UltimoGastoStaminaEm,
            r.StaminaContadaAte,
            agora
        );

        (r.Mana, r.ManaContadaAte) = Regenerar(
            r.Mana,
            maximos.Mana,
            s.RegeneracaoMana,
            r.UltimoGastoManaEm,
            r.ManaContadaAte,
            agora
        );
    }

    /// <summary>
    /// Regeneração de vida: contínua (sem atraso), só com vida acima de 0 (quem caiu não volta
    /// sozinho) e com taxa acima de 0. Paga só os pontos inteiros; a fração fica para a próxima.
    /// </summary>
    internal static (int Atual, DateTime ContadoAte) RegenerarVida(
        int atual,
        int maximo,
        double taxa,
        DateTime contadoAte,
        DateTime agora
    )
    {
        var limitado = RegrasRecursos.Limitar(atual, maximo);
        if (limitado <= 0 || taxa <= 0 || limitado >= maximo)
        {
            return (limitado, agora > contadoAte ? agora : contadoAte);
        }

        if (agora <= contadoAte)
        {
            return (limitado, contadoAte);
        }

        var ganho = (int)Math.Floor((agora - contadoAte).TotalSeconds * taxa);
        if (ganho <= 0)
        {
            return (limitado, contadoAte);
        }

        var novo = Math.Min(maximo, limitado + ganho);
        return (novo, novo >= maximo ? agora : contadoAte.AddSeconds(ganho / taxa));
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

/// <summary>Os três máximos de recurso do personagem agora (atributos dele mais o equipamento).</summary>
public readonly record struct MaximosDeRecursos(int Vida, int Stamina, int Mana);
