using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Contratos.Personagem;
using JogoDoGrupo.Server.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Desenvolvimento;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JogoDoGrupo.Server.Tests.Features.Desenvolvimento;

public class AcoesDeAdminTestes
{
    private const long Personagem = 7;

    private static PedirAcaoDeAdmin ItemValido() =>
        new()
        {
            Acao = AcaoDeAdmin.GerarItem,
            ModeloId = CatalogoEquipamentos.PrimeiroModelo,
            Grau = GrauEquipamento.Forte,
            Origem = OrigemDoLoot.Chefe,
            Quantidade = 3,
        };

    [Fact]
    public async Task Pedido_invalido_e_recusado_sem_chamar_nenhuma_funcionalidade()
    {
        var m = Montar();
        var pedido = ItemValido();
        pedido.Quantidade = ConfigAdmin.QuantidadeMaximaDeItens + 1;

        var resultado = await m.Executor.ExecutarAsync(Personagem, pedido, CancellationToken.None);

        Assert.False(resultado.Aceito);
        Assert.Empty(m.Chamadas);
    }

    [Fact]
    public async Task Gerar_item_leva_modelo_grau_origem_e_quantidade_ao_contrato_de_itens()
    {
        var m = Montar();

        var resultado = await m.Executor.ExecutarAsync(
            Personagem,
            ItemValido(),
            CancellationToken.None
        );

        Assert.True(resultado.Aceito);
        Assert.Equal(
            $"item {Personagem} {CatalogoEquipamentos.PrimeiroModelo} Forte Chefe 3",
            Assert.Single(m.Chamadas)
        );
    }

    [Theory]
    [InlineData(AcaoDeAdmin.EquiparUltimoItem, "equipar 7")]
    [InlineData(AcaoDeAdmin.LimparBolsa, "limpar 7")]
    [InlineData(AcaoDeAdmin.QuebrarEquipados, "durabilidade 7 quebrar")]
    [InlineData(AcaoDeAdmin.ConsertarEquipados, "durabilidade 7 consertar")]
    [InlineData(AcaoDeAdmin.MatarInimigosDaArea, "matar 7")]
    public async Task Acoes_sem_campos_chegam_ao_contrato_certo(AcaoDeAdmin acao, string esperado)
    {
        var m = Montar();

        var resultado = await m.Executor.ExecutarAsync(
            Personagem,
            new PedirAcaoDeAdmin { Acao = acao },
            CancellationToken.None
        );

        Assert.True(resultado.Aceito);
        Assert.Equal(esperado, Assert.Single(m.Chamadas));
    }

    [Fact]
    public async Task Valores_chegam_a_experiencia_coroas_essencias_teleporte_e_spawn()
    {
        var m = Montar();
        async Task Pedir(PedirAcaoDeAdmin pedido) =>
            Assert.True(
                (await m.Executor.ExecutarAsync(Personagem, pedido, CancellationToken.None)).Aceito
            );

        await Pedir(new PedirAcaoDeAdmin { Acao = AcaoDeAdmin.AjustarExperiencia, Valor = -50 });
        await Pedir(new PedirAcaoDeAdmin { Acao = AcaoDeAdmin.AjustarCoroas, Valor = 900 });
        await Pedir(new PedirAcaoDeAdmin { Acao = AcaoDeAdmin.CreditarEssencias, Valor = 5 });
        await Pedir(
            new PedirAcaoDeAdmin
            {
                Acao = AcaoDeAdmin.Teleportar,
                X = 1f,
                Y = 2f,
                Z = 3f,
            }
        );
        await Pedir(
            new PedirAcaoDeAdmin
            {
                Acao = AcaoDeAdmin.SpawnarInimigo,
                EspecieId = CatalogoCriaturas.LoboDoCampo,
                Quantidade = 4,
            }
        );

        Assert.Equal(
            new[]
            {
                "xp 7 -50",
                "coroas 7 900",
                "essencias 7 5",
                "tp 7 1 2 3",
                $"spawn 7 {CatalogoCriaturas.LoboDoCampo} 4",
            },
            m.Chamadas
        );
    }

    [Fact]
    public async Task Recusa_da_funcionalidade_volta_como_nao_aceito()
    {
        var m = Montar();
        m.Itens.Recusar = true;

        var resultado = await m.Executor.ExecutarAsync(
            Personagem,
            ItemValido(),
            CancellationToken.None
        );

        Assert.False(resultado.Aceito);
        Assert.Equal("recusado", resultado.Texto);
    }

    [Fact]
    public async Task Modulo_ligado_executa_a_acao()
    {
        var m = Montar();
        var modulo = Modulo(m, ligado: true);

        await modulo.PedirAcao(1, Personagem, ItemValido());

        Assert.Single(m.Chamadas);
    }

    [Fact]
    public async Task Modulo_desligado_ou_conta_fora_da_lista_ignora_a_acao()
    {
        var desligado = Montar();
        await Modulo(desligado, ligado: false).PedirAcao(1, Personagem, ItemValido());
        Assert.Empty(desligado.Chamadas);

        var foraDaLista = Montar();
        await Modulo(foraDaLista, ligado: true, contas: "5").PedirAcao(1, Personagem, ItemValido());
        Assert.Empty(foraDaLista.Chamadas);

        var naLista = Montar();
        await Modulo(naLista, ligado: true, contas: "1").PedirAcao(1, Personagem, ItemValido());
        Assert.Single(naLista.Chamadas);
    }

    [Fact]
    public async Task Modulo_ignora_pedido_nulo()
    {
        var m = Montar();
        await Modulo(m, ligado: true).PedirAcao(1, Personagem, null);
        Assert.Empty(m.Chamadas);
    }

    private static DesenvolvimentoModule Modulo(Montagem m, bool ligado, string? contas = null)
    {
        var valores = new Dictionary<string, string?>
        {
            [PoliticaDeComandos.ChavePermitirComandos] = ligado ? "true" : "false",
        };
        if (contas != null)
        {
            valores[$"{PoliticaDeComandos.ChaveContas}:0"] = contas;
        }

        var configuracao = new ConfigurationBuilder().AddInMemoryCollection(valores).Build();
        var politica = PoliticaDeComandos.Ler(
            configuracao,
            new AmbienteDeTeste("Development"),
            NullLogger.Instance
        );
        var comandos = new ComandosDoServidor(m.Experiencia, m.Teleporte, new SpawnNulo());
        var modulo = new DesenvolvimentoModule();
        modulo.Configurar(politica, comandos, null, NullLogger.Instance, m.Executor);
        return modulo;
    }

    private static Montagem Montar()
    {
        var chamadas = new List<string>();
        var itens = new ItensFalsos(chamadas);
        var servicos = new ServiceCollection();
        servicos.AddScoped<IFerramentasDeItensDeTeste>(_ => itens);
        servicos.AddScoped<IAjusteDeCoroasDeTeste>(_ => new CoroasFalsas(chamadas));
        var provedor = servicos.BuildServiceProvider();
        var experiencia = new ExperienciaFalsa(chamadas);
        var teleporte = new TeleporteFalso(chamadas);
        var executor = new ExecutorDeAcoesDeAdmin(
            provedor.GetRequiredService<IServiceScopeFactory>(),
            experiencia,
            teleporte,
            new InimigosFalsos(chamadas)
        );
        return new Montagem(executor, chamadas, itens, experiencia, teleporte);
    }

    private sealed record Montagem(
        ExecutorDeAcoesDeAdmin Executor,
        List<string> Chamadas,
        ItensFalsos Itens,
        ExperienciaFalsa Experiencia,
        TeleporteFalso Teleporte
    );

    private sealed class ItensFalsos(List<string> chamadas) : IFerramentasDeItensDeTeste
    {
        public bool Recusar { get; set; }

        private Task<(bool, string)> Responder(string chamada)
        {
            chamadas.Add(chamada);
            return Task.FromResult(Recusar ? (false, "recusado") : (true, "ok"));
        }

        public Task<(bool Aceito, string Texto)> GerarAsync(
            long personagemId,
            int modeloId,
            GrauEquipamento grau,
            OrigemDoLoot origem,
            int quantidade,
            CancellationToken c
        ) => Responder($"item {personagemId} {modeloId} {grau} {origem} {quantidade}");

        public Task<(bool Aceito, string Texto)> EquiparUltimoGeradoAsync(
            long personagemId,
            CancellationToken c
        ) => Responder($"equipar {personagemId}");

        public Task<(bool Aceito, string Texto)> LimparBolsaAsync(
            long personagemId,
            CancellationToken c
        ) => Responder($"limpar {personagemId}");

        public Task<(bool Aceito, string Texto)> AjustarDurabilidadeDosEquipadosAsync(
            long personagemId,
            bool quebrar,
            CancellationToken c
        ) => Responder($"durabilidade {personagemId} {(quebrar ? "quebrar" : "consertar")}");

        public Task<(bool Aceito, string Texto)> CreditarEssenciasAsync(
            long personagemId,
            int quantidade,
            CancellationToken c
        ) => Responder($"essencias {personagemId} {quantidade}");
    }

    private sealed class CoroasFalsas(List<string> chamadas) : IAjusteDeCoroasDeTeste
    {
        public Task<(bool Aceito, string Texto)> AjustarAsync(
            long personagemId,
            long quantidade,
            CancellationToken c
        )
        {
            chamadas.Add($"coroas {personagemId} {quantidade}");
            return Task.FromResult((true, "ok"));
        }
    }

    private sealed class ExperienciaFalsa(List<string> chamadas) : IAjusteDeExperienciaDeTeste
    {
        public Task<(bool Aceito, string Texto)> AjustarAsync(
            long personagemId,
            long quantidade,
            CancellationToken cancelamento
        )
        {
            chamadas.Add($"xp {personagemId} {quantidade}");
            return Task.FromResult((true, "ok"));
        }
    }

    private sealed class TeleporteFalso(List<string> chamadas) : ITeleporteDeTeste
    {
        public (bool Aceito, string Texto) Teleportar(long personagemId, float x, float y, float z)
        {
            chamadas.Add($"tp {personagemId} {x} {y} {z}");
            return (true, "ok");
        }
    }

    private sealed class InimigosFalsos(List<string> chamadas) : IInimigosDeTeste
    {
        public (bool Aceito, string Texto) SpawnarEspecie(
            long personagemId,
            int especieId,
            int quantidade
        )
        {
            chamadas.Add($"spawn {personagemId} {especieId} {quantidade}");
            return (true, "ok");
        }

        public (bool Aceito, string Texto) MatarDaArea(long personagemId)
        {
            chamadas.Add($"matar {personagemId}");
            return (true, "ok");
        }
    }

    private sealed class SpawnNulo : ISpawnDeTeste
    {
        public (bool Aceito, string Texto) SpawnarPerto(long personagemId) => (true, "ok");
    }

    private sealed class AmbienteDeTeste(string nome) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = nome;
        public string ApplicationName { get; set; } = "Teste";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
