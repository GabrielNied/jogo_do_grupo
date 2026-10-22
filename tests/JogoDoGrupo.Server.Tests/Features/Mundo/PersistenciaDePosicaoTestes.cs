using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Mundo;
using JogoDoGrupo.Server.Tests.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mundo;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Tests.Features.Mundo;

public sealed class PersistenciaDePosicaoTestes(TempoRealFixture f)
    : IClassFixture<TempoRealFixture>
{
    [Fact]
    public async Task Salvar_e_carregar_persiste_posicao_no_banco()
    {
        using var conexao = new SqliteConnection("Data Source=:memory:");
        conexao.Open();
        var opcoes = new DbContextOptionsBuilder<JogoDbContext>().UseSqlite(conexao).Options;
        using var db = new JogoDbContext(opcoes);
        db.Database.EnsureCreated();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        using var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var persistencia = new PersistenciaDePosicaoDoMundo(scopeFactory);

        const long personagemId = 42;
        var chaoY = MapaDoMundo.AlturaEm(15f, 25f);

        persistencia.Salvar(personagemId, AreasDoMundo.Cidade, 15f, chaoY, 25f, 90f);

        var salvo = persistencia.Carregar(personagemId);
        Assert.NotNull(salvo);
        Assert.Equal(personagemId, salvo.PersonagemId);
        Assert.Equal(AreasDoMundo.Cidade, salvo.Area);
        Assert.Equal(15f, salvo.X);
        Assert.Equal(chaoY, salvo.Y);
        Assert.Equal(25f, salvo.Z);
        Assert.Equal(90f, salvo.RotacaoY);

        // Atualização da mesma linha
        await persistencia.SalvarAsync(personagemId, AreasDoMundo.Cidade, 30f, chaoY, 40f, 180f);
        var atualizado = await persistencia.CarregarAsync(personagemId);
        Assert.NotNull(atualizado);
        Assert.Equal(30f, atualizado.X);
        Assert.Equal(40f, atualizado.Z);
        Assert.Equal(180f, atualizado.RotacaoY);

        // Confere que não duplicou registro
        var total = await db.Set<PosicaoDoPersonagem>().CountAsync();
        Assert.Equal(1, total);
    }

    [Fact]
    public async Task Personagem_novo_sem_posicao_salva_entra_no_ponto_de_nascimento()
    {
        var (cliente, _, id) = await f.EntrarAsync();
        using var _c = cliente;

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estado = await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Equal(id, estado.Eu.PersonagemId);
        Assert.Equal(ValidadorDeMovimento.Nascimento.X, estado.Eu.X);
        Assert.Equal(ValidadorDeMovimento.Nascimento.Z, estado.Eu.Z);
    }

    [Fact]
    public async Task Personagem_com_posicao_salva_valida_restaura_coordenadas()
    {
        var persistencia = f.Fabrica.Services.GetRequiredService<IPersistenciaDePosicaoDoMundo>();
        var (cliente, _, id) = await f.EntrarAsync();
        using var _c = cliente;

        var chaoY = MapaDoMundo.AlturaEm(20f, 10f);
        persistencia.Salvar(id, AreasDoMundo.Cidade, 20f, chaoY, 10f, 135f);

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estado = await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Equal(id, estado.Eu.PersonagemId);
        Assert.Equal(20f, estado.Eu.X);
        Assert.Equal(chaoY, estado.Eu.Y);
        Assert.Equal(10f, estado.Eu.Z);
        Assert.Equal(135f, estado.Eu.RotacaoY);
    }

    [Theory]
    [InlineData(2000f, 0f, 0f, 0f)] // Fora do mapa em X
    [InlineData(0f, 0f, -2000f, 0f)] // Fora do mapa em Z
    [InlineData(float.NaN, 0f, 0f, 0f)] // Não numérico
    [InlineData(40f, 0f, 0f, 0f)] // Dentro de parede (muro leste da cidade)
    [InlineData(-40f, 0f, 0f, 0f)] // Dentro de parede (muro oeste da cidade)
    [InlineData(0f, 0f, -40f, 0f)] // Dentro de parede (muro sul da cidade)
    public async Task Posicao_salva_invalida_faz_fallback_para_nascimento(
        float x,
        float y,
        float z,
        float rot
    )
    {
        var persistencia = f.Fabrica.Services.GetRequiredService<IPersistenciaDePosicaoDoMundo>();
        var (cliente, _, id) = await f.EntrarAsync();
        using var _c = cliente;

        persistencia.Salvar(id, AreasDoMundo.Cidade, x, y, z, rot);

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estado = await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Equal(id, estado.Eu.PersonagemId);
        Assert.Equal(ValidadorDeMovimento.Nascimento.X, estado.Eu.X);
        Assert.Equal(ValidadorDeMovimento.Nascimento.Z, estado.Eu.Z);
    }

    [Fact]
    public async Task Desconexao_em_dungeon_restaura_na_entrada_da_dungeon_na_cidade()
    {
        var persistencia = f.Fabrica.Services.GetRequiredService<IPersistenciaDePosicaoDoMundo>();
        var (cliente, _, id) = await f.EntrarAsync();
        using var _c = cliente;

        // Registro salvo marcando área de instância (dungeon)
        persistencia.Salvar(id, "instancia:dungeon-123", -1440f, 10f, -1420f, 90f);

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        var estado = await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        Assert.Equal(id, estado.Eu.PersonagemId);
        Assert.Equal(LayoutDaMinaDoReflexo.EntradaX, estado.Eu.X);
        Assert.Equal(LayoutDaMinaDoReflexo.EntradaY, estado.Eu.Y);
        Assert.Equal(LayoutDaMinaDoReflexo.EntradaZ, estado.Eu.Z);
    }

    [Fact]
    public async Task AoEncerrar_salva_posicao_do_jogador_ao_desconectar()
    {
        var persistencia = f.Fabrica.Services.GetRequiredService<IPersistenciaDePosicaoDoMundo>();
        var (cliente, _, id) = await f.EntrarAsync();

        cliente.Enviar(MundoMessageIds.EntrarNaCidade, new EntrarNaCidade());
        await cliente.ReceberAsync<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade);

        var destinoX = 12f;
        var destinoZ = -18f;
        var destinoY = MapaDoMundo.AlturaEm(destinoX, destinoZ);

        cliente.Enviar(
            MundoMessageIds.IntencaoDeMovimento,
            new IntencaoDeMovimento
            {
                X = destinoX,
                Y = destinoY,
                Z = destinoZ,
                RotacaoY = 180f,
                Sequencia = 1,
            }
        );

        // Dá tempo do passo ser processado antes de desconectar
        await Task.Delay(50);

        // Desconecta a sessão
        cliente.Sair();
        cliente.Dispose();

        // Espera a persistência processar
        await Task.Delay(100);

        var salvo = persistencia.Carregar(id);
        Assert.NotNull(salvo);
        Assert.Equal(destinoX, salvo.X);
        Assert.Equal(destinoY, salvo.Y);
        Assert.Equal(destinoZ, salvo.Z);
        Assert.Equal(180f, salvo.RotacaoY);
    }

    [Fact]
    public void ValidadorDeMovimento_DentroDeParede_identifica_muros_e_libera_portao()
    {
        // Muros da cidade ficam em +-40 m
        Assert.True(ValidadorDeMovimento.DentroDeParede(40f, 0f));
        Assert.True(ValidadorDeMovimento.DentroDeParede(-40f, 0f));
        Assert.True(ValidadorDeMovimento.DentroDeParede(0f, -40f));
        Assert.True(ValidadorDeMovimento.DentroDeParede(39.8f, 15f));

        // Vão do portão norte (Z = 40 m, X entre -4 e +4 m)
        Assert.False(ValidadorDeMovimento.DentroDeParede(0f, 40f));
        Assert.False(ValidadorDeMovimento.DentroDeParede(2f, 40f));
        Assert.False(ValidadorDeMovimento.DentroDeParede(-2f, 40f));

        // Fora das paredes
        Assert.False(ValidadorDeMovimento.DentroDeParede(0f, -24f)); // Nascimento
        Assert.False(ValidadorDeMovimento.DentroDeParede(100f, 100f)); // Campo aberto
    }
}
