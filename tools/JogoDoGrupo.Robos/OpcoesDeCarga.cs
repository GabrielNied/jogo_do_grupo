using System.Globalization;

namespace JogoDoGrupo.Robos;

/// <summary>O que a carga faz: quantos robôs, por quanto tempo e contra qual servidor.</summary>
public sealed record OpcoesDeCarga
{
    public int Robos { get; init; } = 10;

    /// <summary>Tempo medido depois que todos entraram (a entrada não conta).</summary>
    public double DuracaoSegundos { get; init; } = 30;

    /// <summary>Robôs que começam a entrar por segundo (subida gradual, como gente chegando).</summary>
    public double EntradasPorSegundo { get; init; } = 20;

    public Uri Servidor { get; init; } = new("http://localhost:5080");

    public string HostTempoReal { get; init; } = "127.0.0.1";

    public int PortaTempoReal { get; init; } = 5081;

    public string ChaveConexao { get; init; } = "JogoDoGrupo";

    /// <summary>
    /// Início do usuário das contas (<c>robo_0001</c>...). As contas são reaproveitadas entre
    /// cargas; duas cargas ao mesmo tempo no mesmo servidor precisam de prefixos diferentes.
    /// </summary>
    public string Prefixo { get; init; } = "robo";

    /// <summary>Intenções de movimento por segundo, como o client (10 por segundo).</summary>
    public int MovimentosPorSegundo { get; init; } = 10;

    /// <summary>Intervalo entre ataques leves de cada robô.</summary>
    public int AtaqueACadaMs { get; init; } = 2000;

    /// <summary>
    /// Chave JWT do servidor (a de desenvolvimento está no appsettings.Development.json). Com ela,
    /// os robôs leem o tick em <c>/admin/metricas</c>; sem ela, o tick fica de fora.
    /// </summary>
    public string? ChaveJwt { get; init; }

    public string EmissorJwt { get; init; } = "JogoDoGrupo";

    public string AudienciaJwt { get; init; } = "JogoDoGrupo.Client";

    /// <summary>Arquivo do relatório em JSON; <c>-</c> escreve o JSON no console no lugar do texto.</summary>
    public string? Json { get; init; }

    public const string Ajuda = """
        Robôs de carga do tempo real (issue #150). Uso:
          dotnet run --project tools/JogoDoGrupo.Robos -- [opções]

          --robos N              robôs em paralelo (10)
          --duracao S            segundos medidos depois que todos entraram (30)
          --entradas-por-s N     robôs que começam a entrar por segundo (20)
          --servidor URL         API HTTP (http://localhost:5080)
          --tempo-real HOST:PORTA  UDP (host da API, porta 5081)
          --chave-conexao TEXTO  chave da LiteNetLib (JogoDoGrupo)
          --prefixo TEXTO        início do usuário das contas, reaproveitadas (robo)
          --movimentos-por-s N   intenções de movimento por segundo (10)
          --ataque-ms N          intervalo entre ataques leves (2000)
          --chave-jwt TEXTO      chave JWT para ler o tick; prefira a variável JOGO_CHAVE_JWT
          --json ARQUIVO         grava o relatório em JSON (- = JSON no console)
        Sai com código 1 se houve erro.
        """;

    /// <summary>Lê os argumentos. Devolve <c>null</c> e a mensagem quando algo está errado.</summary>
    public static (OpcoesDeCarga? Opcoes, string? Erro) Ler(string[] args)
    {
        var opcoes = new OpcoesDeCarga
        {
            ChaveJwt = Environment.GetEnvironmentVariable("JOGO_CHAVE_JWT"),
        };
        string? tempoReal = null;
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                var nome = args[i];
                if (nome is "-h" or "--help" or "--ajuda")
                {
                    return (null, Ajuda);
                }

                if (i + 1 >= args.Length)
                {
                    return (null, $"Falta o valor de {nome}.\n\n{Ajuda}");
                }

                var valor = args[++i];
                if (nome == "--tempo-real")
                {
                    tempoReal = valor;
                    continue;
                }

                opcoes = nome switch
                {
                    "--robos" => opcoes with { Robos = Inteiro(valor, 1) },
                    "--duracao" => opcoes with { DuracaoSegundos = Real(valor) },
                    "--entradas-por-s" => opcoes with { EntradasPorSegundo = Real(valor) },
                    "--servidor" => opcoes with { Servidor = new Uri(valor) },
                    "--chave-conexao" => opcoes with { ChaveConexao = valor },
                    "--prefixo" => opcoes with { Prefixo = valor },
                    "--movimentos-por-s" => opcoes with
                    {
                        MovimentosPorSegundo = Inteiro(valor, 1),
                    },
                    "--ataque-ms" => opcoes with { AtaqueACadaMs = Inteiro(valor, 100) },
                    "--chave-jwt" => opcoes with { ChaveJwt = valor },
                    "--json" => opcoes with { Json = valor },
                    _ => throw new FormatException($"Opção desconhecida: {nome}."),
                };
            }

            if (tempoReal != null)
            {
                var separador = tempoReal.LastIndexOf(':');
                opcoes = opcoes with
                {
                    HostTempoReal = tempoReal[..separador],
                    PortaTempoReal = Inteiro(tempoReal[(separador + 1)..], 1),
                };
            }
            else
            {
                opcoes = opcoes with { HostTempoReal = HostDe(opcoes.Servidor) };
            }
        }
        catch (Exception e) when (e is FormatException or UriFormatException or ArgumentException)
        {
            return (null, $"{e.Message}\n\n{Ajuda}");
        }

        return (opcoes, null);
    }

    private static string HostDe(Uri servidor) =>
        servidor.Host == "localhost" ? "127.0.0.1" : servidor.Host;

    private static int Inteiro(string valor, int minimo)
    {
        var numero = int.Parse(valor, CultureInfo.InvariantCulture);
        return numero >= minimo
            ? numero
            : throw new FormatException($"{valor} precisa ser pelo menos {minimo}.");
    }

    private static double Real(string valor)
    {
        var numero = double.Parse(valor, CultureInfo.InvariantCulture);
        return numero > 0 ? numero : throw new FormatException($"{valor} precisa ser positivo.");
    }
}
