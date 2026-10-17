using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Server.Tests.Features.Itens;

/// <summary>
/// Simulação do ritmo de upgrade (issue #493): jogador solo matando o mob de referência a 4 por
/// minuto, com o sorteio e o gerador de verdade, equipando toda peça que for melhor que a pior
/// das posições onde ela cabe. Falha se o loot sair da meta de <see cref="ConfigLoot"/>.
/// </summary>
public class RitmoDeLootTestes
{
    private static readonly PosicaoEquipamento[] Posicoes = Enum.GetValues<PosicaoEquipamento>()
        .Where(p => p != PosicaoEquipamento.Inventario && RegrasEquipamentos.Conjunto(p) == 0)
        .ToArray();

    private sealed record Resultado(List<double> MinutosDosUpgrades, double MinutosTotais);

    private static Resultado Simular(int semente)
    {
        var aleatorio = new Random(semente);
        var sorteador = new SorteadorDeLoot(aleatorio);
        var gerador = new GeradorEquipamentos(new ConfigEquipamentos());
        var mob = CatalogoCriaturas.Obter(CatalogoCriaturas.JavaliDoCampo);
        var equipado = new Dictionary<PosicaoEquipamento, Equipamento>();
        var upgrades = new List<double>();
        var nivel = 1;
        long xp = 0;
        var mobs = 0;

        while (nivel < ConfigExperiencia.NivelDaMetaDeRitmo)
        {
            mobs++;
            var minuto = mobs / ConfigExperiencia.MobsPorMinutoDeReferencia;
            var variante = RegrasCriaturas.SortearVariante(mob.Categoria, aleatorio.NextDouble());
            var origem = TabelaDeLoot.OrigemDe(mob.Categoria, variante);
            foreach (var queda in sorteador.Sortear(origem, mob.Nivel))
            {
                var peca = gerador.Gerar(queda.ModeloId, aleatorio, queda.Origem, queda.Grau);
                if (
                    peca != null
                    && peca.NivelPersonagemMinimo <= nivel
                    && TentarEquipar(equipado, peca)
                )
                {
                    upgrades.Add(minuto);
                }
            }

            var ganho = RegrasExperiencia.AplicarGanho(
                nivel,
                xp,
                RegrasCriaturas.Experiencia(mob.ExperienciaBase, variante)
            );
            nivel = ganho.Nivel;
            xp = ganho.Xp;
        }

        return new Resultado(upgrades, mobs / ConfigExperiencia.MobsPorMinutoDeReferencia);
    }

    // Equipa na posição onde a peça cabe e é a que perde mais para ela; falso se nenhuma é pior.
    private static bool TentarEquipar(
        Dictionary<PosicaoEquipamento, Equipamento> equipado,
        Equipamento peca
    )
    {
        PosicaoEquipamento? alvo = null;
        var pior = TabelaDeLoot.Pontuacao(peca);
        foreach (var posicao in Posicoes.Where(p => RegrasEquipamentos.Aceita(peca.Tipo, p)))
        {
            var atual = equipado.TryGetValue(posicao, out var e) ? TabelaDeLoot.Pontuacao(e) : -1;
            if (atual < pior)
            {
                pior = atual;
                alvo = posicao;
            }
        }

        if (alvo == null)
        {
            return false;
        }

        equipado[alvo.Value] = peca;
        return true;
    }

    private static IReadOnlyList<Resultado> Varias() =>
        Enumerable.Range(1, 200).Select(Simular).ToArray();

    [Fact]
    public void Jogador_solo_ganha_uma_peca_melhor_a_cada_X_minutos_nos_niveis_1_a_10()
    {
        var execucoes = Varias();

        var minutos = execucoes.Average(r => r.MinutosTotais);
        var upgrades = execucoes.Average(r => r.MinutosDosUpgrades.Count);
        Assert.True(upgrades > 0);
        Assert.True(
            minutos / upgrades <= ConfigLoot.MinutosPorPecaMelhorNaMeta,
            $"uma peça melhor a cada {minutos / upgrades:0.0} min; a meta é {ConfigLoot.MinutosPorPecaMelhorNaMeta}"
        );
    }

    [Fact]
    public void Nenhuma_corrida_fica_muito_tempo_sem_peca_melhor()
    {
        foreach (var r in Varias())
        {
            var anterior = 0.0;
            foreach (var minuto in r.MinutosDosUpgrades.Append(r.MinutosTotais))
            {
                Assert.True(
                    minuto - anterior <= ConfigLoot.MaiorEsperaSemPecaMelhorMinutos,
                    $"{minuto - anterior:0.0} min sem peça melhor"
                );
                anterior = minuto;
            }
        }
    }

    [Fact]
    public void Primeira_peca_melhor_sai_nos_primeiros_minutos()
    {
        Assert.All(
            Varias(),
            r => Assert.True(r.MinutosDosUpgrades[0] <= ConfigLoot.MinutosPorPecaMelhorNaMeta)
        );
    }
}
