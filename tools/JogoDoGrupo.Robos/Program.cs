namespace JogoDoGrupo.Robos;

/// <summary>
/// Console dos robôs. Classe com nome próprio (e não top-level) para o projeto de testes, que
/// referencia este e o servidor, não ver dois <c>Program</c>.
/// </summary>
public static class ConsoleDosRobos
{
    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var (opcoes, erro) = OpcoesDeCarga.Ler(args);
        if (opcoes == null)
        {
            Console.Error.WriteLine(erro);
            return erro == OpcoesDeCarga.Ajuda ? 0 : 2;
        }

        using var cancelar = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancelar.Cancel();
        };

        HttpClient CriarHttp() => new() { BaseAddress = opcoes.Servidor };

        Func<CancellationToken, Task<TickDoServidor?>>? lerTick = null;
        if (!string.IsNullOrEmpty(opcoes.ChaveJwt))
        {
            lerTick = async parada =>
            {
                using var http = CriarHttp();
                return await TokenDeMetricas.LerAsync(http, opcoes, parada);
            };
        }

        RelatorioDeCarga relatorio;
        try
        {
            relatorio = await CargaDeRobos.RodarAsync(
                opcoes,
                CriarHttp,
                lerTick,
                linha => Console.Error.WriteLine(linha),
                cancelar.Token
            );
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Cancelado.");
            return 130;
        }

        if (opcoes.Json == "-")
        {
            Console.WriteLine(relatorio.ParaJson());
        }
        else
        {
            Console.WriteLine(relatorio.Texto());
            if (opcoes.Json != null)
            {
                await File.WriteAllTextAsync(opcoes.Json, relatorio.ParaJson());
                Console.Error.WriteLine($"JSON em {Path.GetFullPath(opcoes.Json)}");
            }
        }

        return relatorio.TotalDeErros > 0 ? 1 : 0;
    }
}
