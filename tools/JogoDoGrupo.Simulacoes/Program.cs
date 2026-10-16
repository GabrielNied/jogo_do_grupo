using System.Globalization;
using System.Text;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Personagem;
using JogoDoGrupo.Shared.Features.Talentos;
using TipoDeEfeito = JogoDoGrupo.Shared.Features.Talentos.TipoDeEfeito;

// Simulação dos números da árvore e dos atributos (#627, ADR 0043). Chama as regras do shared;
// mudar uma constante lá muda o resultado aqui. Uso no README.md ao lado.
var saida = args.Length > 0 ? args[0] : Path.Combine("tools", "JogoDoGrupo.Simulacoes", "saida");
Directory.CreateDirectory(saida);
var cultura = CultureInfo.InvariantCulture;

var arvore = ArvoreDoJogo.Criar();
var problemas = ValidadorDaArvore.Validar(arvore);
if (problemas.Count > 0)
{
    Console.Error.WriteLine("Árvore inválida: " + string.Join(" ", problemas));
    return 1;
}

var builds = Builds.Todas();
var relatorio = new StringBuilder();
void Linha(string texto = "")
{
    relatorio.AppendLine(texto);
    Console.WriteLine(texto);
}

// --- Orçamento --------------------------------------------------------------------------
var pontosNoMaximo = ConfigExperiencia.PontosPorNivel * (ConfigExperiencia.NivelMaximo - 1);
var custoTotal = ArvoreDoJogo.CustoTotal(arvore);
Linha("## Orçamento de pontos");
Linha();
Linha("| Item | Valor |");
Linha("|---|---|");
Linha("| Pontos no nível 100 | " + pontosNoMaximo + " |");
Linha("| Nós na árvore | " + arvore.Nos.Length + " |");
Linha("| Custo para comprar tudo | " + custoTotal + " |");
Linha(
    "| Parte da árvore que cabe nos pontos | "
        + (100.0 * pontosNoMaximo / custoTotal).ToString("0", cultura)
        + "% |"
);
foreach (AtributoPrincipal caminho in Enum.GetValues(typeof(AtributoPrincipal)))
{
    var custo = 0;
    foreach (var no in arvore.Nos)
    {
        if (no.Caminhos.Length == 1 && no.Caminhos[0] == caminho)
        {
            custo += RegrasDaArvore.CustoPago(no, no.RankMaximo);
        }
    }

    Linha("| Caminho " + caminho + " completo (sem híbridos) | " + custo + " pontos |");
}

Linha();

// --- Builds no nível 100 ----------------------------------------------------------------
var equipamentos = new[]
{
    ("Sem equipamento", Equip.Nenhum, 0),
    ("Equip. físico qualidade 60", Equip.Fisico, 60),
    ("Equip. físico qualidade 100", Equip.Fisico, 100),
    ("Equip. mágico qualidade 100", Equip.Magico, 100),
    ("Equip. tanque qualidade 100", Equip.Tanque, 100),
};

var csv = new StringBuilder();
csv.AppendLine(
    "build;equipamento;pontos;forca;vigor;agilidade;intelecto;espirito;vida;stamina;mana;"
        + "dano_leve;dano_pesado;dano_esperado_leve;chance_critica;dano_critico;vel_ataque_fisico;"
        + "vel_ataque_magico;pen_fisica;pen_magica;reg_vida;reg_stamina;reg_mana;defesa_fisica;"
        + "reducao_fisica"
);

Linha("## Builds no nível 100 (" + pontosNoMaximo + " pontos)");
Linha();
var resultados = new List<(Build Build, string Equip, Metricas M)>();
foreach (var build in builds)
{
    var compra = Comprador.Comprar(arvore, pontosNoMaximo, build.Compras);
    foreach (var (nomeEquip, equip, qualidade) in equipamentos)
    {
        var m = Calculadora.Calcular(arvore, compra.Ranks, equip, qualidade);
        resultados.Add((build, nomeEquip, m));
        csv.AppendLine(
            string.Join(
                ";",
                build.Nome,
                nomeEquip,
                compra.Gastos,
                m.Atributos.Forca,
                m.Atributos.Vigor,
                m.Atributos.Agilidade,
                m.Atributos.Intelecto,
                m.Atributos.Espirito,
                m.Vida,
                m.Stamina,
                m.Mana,
                m.DanoLeve,
                m.DanoPesado,
                m.DanoEsperadoLeve.ToString("0.0", cultura),
                m.ChanceCritica.ToString("0.000", cultura),
                m.DanoCritico.ToString("0.00", cultura),
                m.VelFisica.ToString("0.00", cultura),
                m.VelMagica.ToString("0.00", cultura),
                m.PenFisica.ToString("0.000", cultura),
                m.PenMagica.ToString("0.000", cultura),
                m.RegVida.ToString("0.0", cultura),
                m.RegStamina.ToString("0.0", cultura),
                m.RegMana.ToString("0.0", cultura),
                m.DefesaFisica.ToString("0", cultura),
                m.ReducaoFisica.ToString("0.000", cultura)
            )
        );
    }
}

File.WriteAllText(Path.Combine(saida, "builds-nivel-100.csv"), csv.ToString());

Linha("### Pontos investidos (atributos da árvore)");
Linha();
Linha("| Build | Gasto | Força | Vigor | Agilidade | Intelecto | Espírito |");
Linha("|---|---|---|---|---|---|---|");
foreach (var build in builds)
{
    var compra = Comprador.Comprar(arvore, pontosNoMaximo, build.Compras);
    var a = RegrasDaArvore.SomarAtributos(arvore, compra.Ranks);
    Linha(
        $"| {build.Nome} | {compra.Gastos} | {a.Forca} | {a.Vigor} | {a.Agilidade} | {a.Intelecto} | {a.Espirito} |"
    );
}

Linha();
Linha("### Recursos e dano (golpe leve; pesado é 2,2x)");
Linha();
Linha(
    "| Build | Equipamento | Vida | Stamina | Mana | Leve | Pesado | Leve c/ crítico | Crít. | Dano crít. | Vel. fís. | Pen. fís. | Reg. vida | Reg. stam. | Reg. mana | Redução fís. |"
);
Linha("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
foreach (var (build, equip, m) in resultados)
{
    var relevante =
        equip == "Sem equipamento"
        || (equip.Contains("qualidade 60") && build.Equip != Equip.Tanque)
        || equip.StartsWith("Equip. " + Calculadora.NomeCurto(build.Equip));
    if (!relevante)
    {
        continue;
    }

    Linha(
        $"| {build.Nome} | {equip} | {m.Vida} | {m.Stamina} | {m.Mana} | {m.DanoLeve} | {m.DanoPesado} | "
            + $"{m.DanoEsperadoLeve.ToString("0", cultura)} | {(m.ChanceCritica * 100).ToString("0", cultura)}% | "
            + $"{m.DanoCritico.ToString("0.00", cultura)}x | {m.VelFisica.ToString("0.00", cultura)}x | "
            + $"{(m.PenFisica * 100).ToString("0", cultura)}% | {m.RegVida.ToString("0.0", cultura)} | "
            + $"{m.RegStamina.ToString("0.0", cultura)} | {m.RegMana.ToString("0.0", cultura)} | "
            + $"{(m.ReducaoFisica * 100).ToString("0", cultura)}% |"
    );
}

Linha();

// --- Por nível --------------------------------------------------------------------------
var porNivel = new StringBuilder();
porNivel.AppendLine(
    "nivel;pontos;build;gastos;forca;vigor;agilidade;intelecto;espirito;custo_redistribuir"
);
var series = new Dictionary<string, List<(int Nivel, double Valor)>>();
foreach (var build in builds)
{
    series[build.Nome] = new List<(int, double)>();
    for (var nivel = 1; nivel <= ConfigExperiencia.NivelMaximo; nivel++)
    {
        var pontos = ConfigExperiencia.PontosPorNivel * (nivel - 1);
        var compra = Comprador.Comprar(arvore, pontos, build.Compras);
        var a = RegrasDaArvore.SomarAtributos(arvore, compra.Ranks);
        var principal = Math.Max(
            Math.Max(Math.Max(a.Forca, a.Vigor), Math.Max(a.Agilidade, a.Intelecto)),
            a.Espirito
        );
        series[build.Nome].Add((nivel, principal));
        porNivel.AppendLine(
            string.Join(
                ";",
                nivel,
                pontos,
                build.Nome,
                compra.Gastos,
                a.Forca,
                a.Vigor,
                a.Agilidade,
                a.Intelecto,
                a.Espirito,
                ConfigRedistribuicao.CustoEmCoroas(nivel)
            )
        );
    }
}

File.WriteAllText(Path.Combine(saida, "por-nivel.csv"), porNivel.ToString());

Linha("## Custo de redistribuir por nível (#910)");
Linha();
Linha("| Nível | Pontos | Coroas |");
Linha("|---|---|---|");
foreach (var nivel in new[] { 1, 10, 20, 30, 50, 75, 100 })
{
    Linha(
        $"| {nivel} | {ConfigExperiencia.PontosPorNivel * (nivel - 1)} | {ConfigRedistribuicao.CustoEmCoroas(nivel)} |"
    );
}

Linha();

// --- Valores de combate dos itens (0 e 100) ---------------------------------------------
Linha("## Conversão dos atributos de item (valor de combate no 0 e no 100)");
Linha();
Linha("| Categoria | Atributo | No 0 | No 100 |");
Linha("|---|---|---|---|");
foreach (TipoEquipamento tipo in Enum.GetValues(typeof(TipoEquipamento)))
{
    foreach (var status in ConfigAtributosItem.Elegiveis(tipo))
    {
        var conversao = ConfigAtributosItem.De(tipo, status);
        if (conversao == null)
        {
            continue;
        }

        Linha(
            $"| {tipo} | {status} | {conversao.Value.No0.ToString("0.##", cultura)} | {conversao.Value.No100.ToString("0.##", cultura)} |"
        );
    }
}

File.WriteAllText(Path.Combine(saida, "relatorio.md"), relatorio.ToString());
File.WriteAllText(Path.Combine(saida, "relatorio.html"), Html.Pagina(relatorio.ToString(), series));
Console.WriteLine();
Console.WriteLine("Arquivos em " + Path.GetFullPath(saida));
return 0;

internal sealed record Build(
    string Nome,
    (string Id, int Rank)[] Compras,
    Equip Equip = Equip.Fisico
);

internal static class Builds
{
    // Ordem de compra de um caminho: os três principais, o grande, os dois secundários, o marco.
    private static List<(string, int)> Caminho(AtributoPrincipal a)
    {
        var lista = new List<(string, int)>();
        for (var i = 0; i < 3; i++)
        {
            lista.Add((ArvoreDoJogo.IdPrincipal(a, i), ArvoreDoJogo.RankMaximoPrincipal));
        }

        lista.Add((ArvoreDoJogo.IdGrande(a), ArvoreDoJogo.RankMaximoGrande));
        lista.Add((ArvoreDoJogo.IdSecundario(a, 0), ArvoreDoJogo.RankMaximoSecundario));
        lista.Add((ArvoreDoJogo.IdSecundario(a, 1), ArvoreDoJogo.RankMaximoSecundario));
        lista.Add((ArvoreDoJogo.IdMarco(a), 1));
        return lista;
    }

    private static Build De(string nome, Equip equip, params AtributoPrincipal[] caminhos)
    {
        var compras = new List<(string, int)>();
        foreach (var caminho in caminhos)
        {
            compras.AddRange(Caminho(caminho));
        }

        return new Build(nome, compras.ToArray(), equip);
    }

    public static Build[] Todas()
    {
        const AtributoPrincipal f = AtributoPrincipal.Forca;
        const AtributoPrincipal v = AtributoPrincipal.Vigor;
        const AtributoPrincipal a = AtributoPrincipal.Agilidade;
        const AtributoPrincipal i = AtributoPrincipal.Intelecto;
        const AtributoPrincipal e = AtributoPrincipal.Espirito;

        var hibrido = new List<(string, int)>
        {
            (ArvoreDoJogo.IdPrincipal(f, 0), 3),
            (ArvoreDoJogo.IdPrincipal(v, 0), 3),
            (ArvoreDoJogo.IdHibrido(f, v), ArvoreDoJogo.RankMaximoHibrido),
            (ArvoreDoJogo.IdPrincipal(f, 0), 5),
            (ArvoreDoJogo.IdPrincipal(v, 0), 5),
            (ArvoreDoJogo.IdPrincipal(f, 1), 5),
            (ArvoreDoJogo.IdPrincipal(v, 1), 5),
            (ArvoreDoJogo.IdSecundario(f, 0), 3),
            (ArvoreDoJogo.IdSecundario(v, 0), 3),
            (ArvoreDoJogo.IdPrincipal(f, 2), 5),
            (ArvoreDoJogo.IdPrincipal(v, 2), 5),
        };

        var espalhado = new List<(string, int)>();
        for (var posicao = 0; posicao < 2; posicao++)
        {
            foreach (AtributoPrincipal caminho in Enum.GetValues(typeof(AtributoPrincipal)))
            {
                espalhado.Add((ArvoreDoJogo.IdPrincipal(caminho, posicao), 5));
            }
        }

        return new[]
        {
            De("Guerreiro (Força, depois Vigor)", Equip.Fisico, f, v),
            De("Tanque (Vigor, depois Força)", Equip.Tanque, v, f),
            De("Ladino (Agilidade, depois Força)", Equip.Fisico, a, f),
            De("Mago (Intelecto, depois Espírito)", Equip.Magico, i, e),
            De("Místico (Espírito, depois Intelecto)", Equip.Magico, e, i),
            new Build("Híbrido Força e Vigor", hibrido.ToArray()),
            new Build("Espalhado (2 nós de cada caminho)", espalhado.ToArray()),
        };
    }
}

internal sealed record Compra(Dictionary<string, int> Ranks, int Gastos);

internal static class Comprador
{
    /// <summary>Compra na ordem dada, com as regras da árvore, até acabarem os pontos.</summary>
    public static Compra Comprar(
        ConfiguracaoDaArvore arvore,
        int pontos,
        (string Id, int Rank)[] compras
    )
    {
        var ranks = new Dictionary<string, int>();
        var livres = pontos;
        var gastos = 0;
        foreach (var (id, alvo) in compras)
        {
            for (var rank = RegrasDaArvore.RankDe(ranks, id) + 1; rank <= alvo; rank++)
            {
                var avaliacao = RegrasDaArvore.AvaliarCompra(arvore, ranks, livres, id, rank);
                if (avaliacao.Erro == CodigosErroTalentos.PontosInsuficientes)
                {
                    return new Compra(ranks, gastos);
                }

                if (avaliacao.Erro != null)
                {
                    break;
                }

                livres -= avaliacao.Custo;
                gastos += avaliacao.Custo;
                ranks[id] = rank;
            }
        }

        return new Compra(ranks, gastos);
    }
}

internal enum Equip
{
    Nenhum,
    Fisico,
    Magico,
    Tanque,
}

internal sealed record Metricas(
    AtributosDaArvore Atributos,
    int Vida,
    int Stamina,
    int Mana,
    int DanoLeve,
    int DanoPesado,
    double DanoEsperadoLeve,
    double ChanceCritica,
    double DanoCritico,
    double VelFisica,
    double VelMagica,
    double PenFisica,
    double PenMagica,
    double RegVida,
    double RegStamina,
    double RegMana,
    double DefesaFisica,
    double ReducaoFisica
);

internal static class Calculadora
{
    // Conjuntos de referência: o melhor que cada função pode vestir (uma peça por posição).
    private static readonly (TipoEquipamento Tipo, StatusItem[] Status)[] Fisico =
    {
        (TipoEquipamento.Espada, new[] { StatusItem.DanoDaArma, StatusItem.DanoFisico }),
        (TipoEquipamento.Capacete, new[] { StatusItem.DanoFisico, StatusItem.DefesaFisica }),
        (TipoEquipamento.Peito, new[] { StatusItem.DanoFisico, StatusItem.DefesaFisica }),
        (TipoEquipamento.Luvas, new[] { StatusItem.DanoFisico, StatusItem.ChanceCritica }),
        (TipoEquipamento.Calca, new[] { StatusItem.DanoFisico, StatusItem.DefesaFisica }),
        (TipoEquipamento.Botas, new[] { StatusItem.DanoFisico, StatusItem.RegeneracaoStamina }),
        (TipoEquipamento.Cinto, new[] { StatusItem.RegeneracaoStamina }),
        (TipoEquipamento.Anel, new[] { StatusItem.ChanceCritica, StatusItem.PenetracaoFisica }),
        (TipoEquipamento.Anel, new[] { StatusItem.ChanceCritica, StatusItem.PenetracaoFisica }),
        (
            TipoEquipamento.Brinco,
            new[] { StatusItem.DanoCritico, StatusItem.VelocidadeAtaqueFisico }
        ),
        (
            TipoEquipamento.Brinco,
            new[] { StatusItem.DanoCritico, StatusItem.VelocidadeAtaqueFisico }
        ),
        (TipoEquipamento.Adorno, new[] { StatusItem.DefesaMagica }),
    };

    private static readonly (TipoEquipamento Tipo, StatusItem[] Status)[] Magico =
    {
        (TipoEquipamento.Cajado, new[] { StatusItem.DanoDaArma, StatusItem.DanoMagico }),
        (TipoEquipamento.Foco, new[] { StatusItem.DanoMagico, StatusItem.RegeneracaoMana }),
        (TipoEquipamento.Capacete, new[] { StatusItem.DanoMagico, StatusItem.DefesaMagica }),
        (TipoEquipamento.Peito, new[] { StatusItem.DanoMagico, StatusItem.DefesaMagica }),
        (TipoEquipamento.Luvas, new[] { StatusItem.DanoMagico, StatusItem.ChanceCritica }),
        (TipoEquipamento.Calca, new[] { StatusItem.DanoMagico, StatusItem.RegeneracaoVida }),
        (TipoEquipamento.Botas, new[] { StatusItem.DanoMagico, StatusItem.RegeneracaoStamina }),
        (TipoEquipamento.Cinto, new[] { StatusItem.RegeneracaoMana }),
        (TipoEquipamento.Anel, new[] { StatusItem.ChanceCritica, StatusItem.PenetracaoMagica }),
        (TipoEquipamento.Anel, new[] { StatusItem.ChanceCritica, StatusItem.PenetracaoMagica }),
        (
            TipoEquipamento.Brinco,
            new[] { StatusItem.DanoCritico, StatusItem.VelocidadeAtaqueMagico }
        ),
        (
            TipoEquipamento.Brinco,
            new[] { StatusItem.DanoCritico, StatusItem.VelocidadeAtaqueMagico }
        ),
        (TipoEquipamento.Adorno, new[] { StatusItem.RegeneracaoMana }),
    };

    private static readonly (TipoEquipamento Tipo, StatusItem[] Status)[] Tanque =
    {
        (TipoEquipamento.Espada, new[] { StatusItem.DanoDaArma, StatusItem.DanoFisico }),
        (TipoEquipamento.Escudo, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Capacete, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Peito, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Luvas, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Calca, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Botas, new[] { StatusItem.VidaMaxima, StatusItem.DefesaFisica }),
        (TipoEquipamento.Cinto, new[] { StatusItem.VidaMaxima, StatusItem.RegeneracaoVida }),
        (TipoEquipamento.Anel, new[] { StatusItem.VidaMaxima }),
        (TipoEquipamento.Anel, new[] { StatusItem.VidaMaxima }),
        (TipoEquipamento.Brinco, new[] { StatusItem.VidaMaxima }),
        (TipoEquipamento.Brinco, new[] { StatusItem.VidaMaxima }),
        (TipoEquipamento.Adorno, new[] { StatusItem.DefesaMagica }),
    };

    public static string NomeCurto(Equip equip) =>
        equip switch
        {
            Equip.Magico => "mágico",
            Equip.Tanque => "tanque",
            _ => "físico",
        };

    public static Metricas Calcular(
        ConfiguracaoDaArvore arvore,
        Dictionary<string, int> ranks,
        Equip equip,
        int qualidade
    )
    {
        var atributos = RegrasDaArvore.SomarAtributos(arvore, ranks);

        double Soma(StatusItem status, (TipoEquipamento Tipo, StatusItem[] Status)[] conjunto)
        {
            double total = 0;
            foreach (var (tipo, lista) in conjunto)
            {
                if (Array.IndexOf(lista, status) >= 0)
                {
                    total += ConfigAtributosItem.Converter(tipo, status, qualidade);
                }
            }

            return total;
        }

        var conjunto = equip switch
        {
            Equip.Fisico => Fisico,
            Equip.Magico => Magico,
            Equip.Tanque => Tanque,
            _ => Array.Empty<(TipoEquipamento, StatusItem[])>(),
        };

        // Secundários da árvore.
        double ArvoreSec(string chave, bool multiplica = false)
        {
            double total = 0;
            foreach (var no in arvore.Nos)
            {
                var rank = RegrasDaArvore.RankDe(ranks, no.Id);
                if (rank <= 0)
                {
                    continue;
                }

                foreach (var efeito in no.Efeitos)
                {
                    if (
                        efeito.Tipo == TipoDeEfeito.AtributoSecundario
                        && efeito.Chave == chave
                        && efeito.Multiplica == multiplica
                    )
                    {
                        total += efeito.ValorPorRank * rank;
                    }
                }
            }

            return total;
        }

        var magico = equip == Equip.Magico;
        var temArma = equip != Equip.Nenhum;
        var danoArma = temArma ? Soma(StatusItem.DanoDaArma, conjunto) : ParcelasDeDano.DanoSemArma;
        var flat = magico
            ? Soma(StatusItem.DanoMagico, conjunto)
            : Soma(StatusItem.DanoFisico, conjunto);
        var pontos = magico ? atributos.Intelecto : atributos.Forca;
        var parcelas = new ParcelasDeDano
        {
            DanoBase = danoArma,
            Magico = magico,
            PercentualPersonagem = RegrasAtaque.PercentualDeDanoPorPonto * pontos,
            DanoFlatEquipamento = flat,
        };

        var leve = RegrasAtaque.Dano(TipoDeAtaque.Leve, parcelas);
        var pesado = RegrasAtaque.Dano(TipoDeAtaque.Pesado, parcelas);
        var chance = Math.Min(
            ConfigAtributosSecundarios.TetoChanceCritica,
            ConfigAtributosSecundarios.BaseChanceCritica
                + Soma(StatusItem.ChanceCritica, conjunto)
                + ArvoreSec("ChanceCritica")
        );
        var danoCritico =
            ConfigAtributosSecundarios.BaseDanoCritico
            + Soma(StatusItem.DanoCritico, conjunto)
            + ArvoreSec("DanoCritico");
        var defesa = Soma(StatusItem.DefesaFisica, conjunto);

        return new Metricas(
            atributos,
            RegrasRecursos.VidaMaxima(
                atributos.Vigor,
                (int)Math.Round(Soma(StatusItem.VidaMaxima, conjunto))
            ),
            RegrasRecursos.StaminaMaxima(atributos.Agilidade),
            RegrasRecursos.ManaMaxima(atributos.Espirito),
            leve,
            pesado,
            leve * (1 + chance * (danoCritico - 1)),
            chance,
            danoCritico,
            ConfigAtributosSecundarios.BaseVelocidadeAtaque
                + Soma(StatusItem.VelocidadeAtaqueFisico, conjunto)
                + ArvoreSec("VelocidadeAtaqueFisico"),
            ConfigAtributosSecundarios.BaseVelocidadeAtaque
                + Soma(StatusItem.VelocidadeAtaqueMagico, conjunto)
                + ArvoreSec("VelocidadeAtaqueMagico"),
            Soma(StatusItem.PenetracaoFisica, conjunto) + ArvoreSec("PenetracaoFisica"),
            Soma(StatusItem.PenetracaoMagica, conjunto) + ArvoreSec("PenetracaoMagica"),
            ConfigAtributosSecundarios.BaseRegeneracaoVida
                + Soma(StatusItem.RegeneracaoVida, conjunto)
                + ArvoreSec("RegeneracaoVida"),
            (
                ConfigAtributosSecundarios.BaseRegeneracaoStamina
                + Soma(StatusItem.RegeneracaoStamina, conjunto)
                + ArvoreSec("RegeneracaoStamina")
            ),
            (
                ConfigAtributosSecundarios.BaseRegeneracaoMana
                + Soma(StatusItem.RegeneracaoMana, conjunto)
                + ArvoreSec("RegeneracaoMana")
            ) * (1 + ArvoreSec("RegeneracaoMana", multiplica: true)),
            defesa,
            defesa / (ConfigAtributosSecundarios.ConstanteDeMitigacao + defesa)
        );
    }
}

internal static class Html
{
    private static readonly string[] Cores =
    {
        "#d62728",
        "#1f77b4",
        "#2ca02c",
        "#9467bd",
        "#ff7f0e",
        "#17becf",
        "#7f7f7f",
    };

    public static string Pagina(
        string markdown,
        Dictionary<string, List<(int Nivel, double Valor)>> series
    )
    {
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\">");
        html.AppendLine("<title>Simulação da árvore</title>");
        html.AppendLine(
            "<style>body{font-family:system-ui,sans-serif;max-width:1100px;margin:2rem auto;padding:0 1rem}"
                + "table{border-collapse:collapse;font-size:.85rem}td,th{border:1px solid #bbb;padding:.2rem .5rem}</style></head><body>"
        );
        html.AppendLine("<h1>Simulação da árvore de atributos (#627)</h1>");
        html.AppendLine("<h2>Maior atributo por nível, por build</h2>");
        html.AppendLine(Grafico(series));
        html.AppendLine(
            "<h2>Tabelas</h2><pre>" + System.Net.WebUtility.HtmlEncode(markdown) + "</pre>"
        );
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static string Grafico(Dictionary<string, List<(int Nivel, double Valor)>> series)
    {
        const int largura = 900;
        const int altura = 360;
        const int margem = 40;
        double maximo = 1;
        foreach (var serie in series.Values)
        {
            foreach (var (_, valor) in serie)
            {
                maximo = Math.Max(maximo, valor);
            }
        }

        var cultura = CultureInfo.InvariantCulture;
        var svg = new StringBuilder();
        svg.AppendLine(
            $"<svg viewBox=\"0 0 {largura} {altura + 90}\" width=\"100%\" role=\"img\">"
        );
        svg.AppendLine(
            $"<line x1=\"{margem}\" y1=\"{altura - margem}\" x2=\"{largura - 10}\" y2=\"{altura - margem}\" stroke=\"#888\"/>"
        );
        svg.AppendLine(
            $"<line x1=\"{margem}\" y1=\"10\" x2=\"{margem}\" y2=\"{altura - margem}\" stroke=\"#888\"/>"
        );
        svg.AppendLine($"<text x=\"{margem}\" y=\"{altura - 12}\" font-size=\"11\">nível 1</text>");
        svg.AppendLine(
            $"<text x=\"{largura - 60}\" y=\"{altura - 12}\" font-size=\"11\">nível 100</text>"
        );
        svg.AppendLine(
            $"<text x=\"4\" y=\"16\" font-size=\"11\">{maximo.ToString("0", cultura)}</text>"
        );
        var indice = 0;
        foreach (var (nome, serie) in series)
        {
            var cor = Cores[indice % Cores.Length];
            var pontos = string.Join(
                " ",
                serie.Select(p =>
                    (margem + (largura - margem - 10) * (p.Nivel - 1) / 99.0).ToString(
                        "0.#",
                        cultura
                    )
                    + ","
                    + (altura - margem - (altura - margem - 10) * p.Valor / maximo).ToString(
                        "0.#",
                        cultura
                    )
                )
            );
            svg.AppendLine(
                $"<polyline fill=\"none\" stroke=\"{cor}\" stroke-width=\"2\" points=\"{pontos}\"/>"
            );
            svg.AppendLine(
                $"<rect x=\"{margem}\" y=\"{altura + indice * 12}\" width=\"10\" height=\"10\" fill=\"{cor}\"/>"
                    + $"<text x=\"{margem + 16}\" y=\"{altura + indice * 12 + 9}\" font-size=\"11\">{System.Net.WebUtility.HtmlEncode(nome)}</text>"
            );
            indice++;
        }

        svg.AppendLine("</svg>");
        return svg.ToString();
    }
}
