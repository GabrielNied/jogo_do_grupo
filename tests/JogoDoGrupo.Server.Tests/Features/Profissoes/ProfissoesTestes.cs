using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Features.Profissoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Profissoes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Profissoes;

/// <summary>Regras de Profissões (#192) no serviço, com banco de verdade e sem posição exigida.</summary>
public sealed class ProfissoesTestes(ServidorDeTeste servidor) : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeProfissoes, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeProfissoes>());
    }

    private async Task<long> NovoAprendizAsync(params Profissao[] profissoes)
    {
        var id = await _ambiente.NovoPersonagemAsync();
        foreach (var profissao in profissoes)
        {
            Assert.Null(await ComServicoAsync(s => s.AprenderAsync(id, profissao, default)));
        }

        return id;
    }

    private void LiberarOsPontos()
    {
        var pontos = servidor.Services.GetRequiredService<PontosDeColetaDoMundo>();
        foreach (var ponto in ConfigProfissoes.Pontos)
        {
            pontos.Devolver(AreasDoMundo.Cidade, ponto.Id);
        }
    }

    private async Task<int> SaldoAsync(long id, int materialId)
    {
        var estado = await ComServicoAsync(s => s.ObterEstadoAsync(id, default));
        return estado.Materiais.FirstOrDefault(m => m.MaterialId == materialId)?.Quantidade ?? 0;
    }

    private async Task ColetarAsync(long id, params int[] nos)
    {
        foreach (var no in nos)
        {
            var (erro, _, _) = await ComServicoAsync(s => s.ColetarAsync(id, no, default));
            Assert.Null(erro);
        }
    }

    [Fact]
    public async Task Aprender_e_de_graca_vale_para_as_seis_e_nao_repete()
    {
        var id = await NovoAprendizAsync(ConfigProfissoes.Disponiveis);

        var estado = await ComServicoAsync(s => s.ObterEstadoAsync(id, default));
        Assert.Equal(6, estado.Aprendidas.Length);
        Assert.Equal(
            CodigosErroProfissoes.JaAprendida,
            await ComServicoAsync(s => s.AprenderAsync(id, Profissao.Ferreiro, default))
        );
    }

    [Theory]
    [InlineData(Profissao.Nenhuma)]
    [InlineData((Profissao)99)]
    public async Task Profissao_invalida_para_aprender_e_recusada(Profissao profissao)
    {
        var id = await _ambiente.NovoPersonagemAsync();

        Assert.Equal(
            CodigosErroProfissoes.ProfissaoInvalida,
            await ComServicoAsync(s => s.AprenderAsync(id, profissao, default))
        );
    }

    [Fact]
    public async Task Coletar_sem_aprender_a_profissao_e_recusado_e_o_ponto_continua()
    {
        LiberarOsPontos();
        var semProfissao = await _ambiente.NovoPersonagemAsync();
        var lenhador = await NovoAprendizAsync(Profissao.Lenhador);

        var (erro, resultado, _) = await ComServicoAsync(s =>
            s.ColetarAsync(semProfissao, ConfigProfissoes.NoArvore1, default)
        );

        Assert.Equal(CodigosErroProfissoes.ProfissaoNaoAprendida, erro);
        Assert.Null(resultado);
        await ColetarAsync(lenhador, ConfigProfissoes.NoArvore1);
    }

    [Fact]
    public async Task Coletar_credita_o_material_e_o_ponto_some_para_os_outros()
    {
        LiberarOsPontos();
        var primeiro = await NovoAprendizAsync(Profissao.Minerador);
        var segundo = await NovoAprendizAsync(Profissao.Minerador);

        var (erro, resultado, _) = await ComServicoAsync(s =>
            s.ColetarAsync(primeiro, ConfigProfissoes.NoVeioDeFerro1, default)
        );
        var (erroDoSegundo, _, area) = await ComServicoAsync(s =>
            s.ColetarAsync(segundo, ConfigProfissoes.NoVeioDeFerro1, default)
        );

        Assert.Null(erro);
        Assert.Equal(ConfigProfissoes.MaterialMinerioDeFerro, resultado!.MaterialId);
        Assert.Equal(
            ConfigProfissoes.ObterPonto(ConfigProfissoes.NoVeioDeFerro1)!.Quantidade,
            await SaldoAsync(primeiro, ConfigProfissoes.MaterialMinerioDeFerro)
        );
        Assert.Equal(CodigosErroProfissoes.NoEsgotado, erroDoSegundo);
        Assert.Equal(0, await SaldoAsync(segundo, ConfigProfissoes.MaterialMinerioDeFerro));
        Assert.Contains(
            ConfigProfissoes.NoVeioDeFerro1,
            (await ComServicoAsync(s => Task.FromResult(s.EstadoDosNos(area)))).Esgotados
        );
    }

    [Fact]
    public async Task Dois_pedidos_ao_mesmo_tempo_no_mesmo_ponto_so_um_leva()
    {
        LiberarOsPontos();
        var a = await NovoAprendizAsync(Profissao.Curtidor);
        var b = await NovoAprendizAsync(Profissao.Curtidor);

        var resultados = await Task.WhenAll(
            ComServicoAsync(s => s.ColetarAsync(a, ConfigProfissoes.NoCarcaca1, default)),
            ComServicoAsync(s => s.ColetarAsync(b, ConfigProfissoes.NoCarcaca1, default))
        );

        Assert.Single(resultados, r => r.Erro == null);
        Assert.Single(resultados, r => r.Erro == CodigosErroProfissoes.NoEsgotado);
        var total =
            await SaldoAsync(a, ConfigProfissoes.MaterialCouroCru)
            + await SaldoAsync(b, ConfigProfissoes.MaterialCouroCru);
        Assert.Equal(ConfigProfissoes.ObterPonto(ConfigProfissoes.NoCarcaca1)!.Quantidade, total);
    }

    [Fact]
    public async Task Coletar_ponto_que_nao_existe_e_recusado()
    {
        var id = await NovoAprendizAsync(Profissao.Lenhador);

        var (erro, _, _) = await ComServicoAsync(s => s.ColetarAsync(id, 999, default));

        Assert.Equal(CodigosErroProfissoes.NoInexistente, erro);
    }

    [Fact]
    public async Task Produzir_sem_aprender_a_profissao_e_recusado()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        var (erro, _) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaEspada, default)
        );

        Assert.Equal(CodigosErroProfissoes.ProfissaoNaoAprendida, erro);
    }

    [Fact]
    public async Task Produzir_sem_material_e_recusado_e_nao_gasta_nada()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(Profissao.Ferreiro, Profissao.Minerador);
        await ColetarAsync(id, ConfigProfissoes.NoVeioDeFerro1);
        var antes = await SaldoAsync(id, ConfigProfissoes.MaterialMinerioDeFerro);

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaEspada, default)
        );

        Assert.Equal(CodigosErroProfissoes.MateriaisInsuficientes, erro);
        Assert.Null(resultado);
        Assert.Equal(antes, await SaldoAsync(id, ConfigProfissoes.MaterialMinerioDeFerro));
        Assert.Empty(await _ambiente.BolsaAsync(id));
    }

    [Fact]
    public async Task Produzir_gasta_os_materiais_e_poe_o_item_na_bolsa()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(
            Profissao.Ferreiro,
            Profissao.Minerador,
            Profissao.Lenhador
        );
        await ColetarAsync(
            id,
            ConfigProfissoes.NoVeioDeFerro1,
            ConfigProfissoes.NoVeioDeFerro2,
            ConfigProfissoes.NoArvore1
        );

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaEspada, default)
        );

        Assert.Null(erro);
        Assert.Equal(173, resultado!.ModeloId);
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialMinerioDeFerro));
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialMadeira));
        var bolsa = await _ambiente.BolsaAsync(id);
        Assert.Single(bolsa);
        Assert.Equal(173, bolsa[0].Equipamento!.ModeloId);
    }

    [Fact]
    public async Task Bolsa_cheia_recusa_sem_gastar_material()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(
            Profissao.Ferreiro,
            Profissao.Minerador,
            Profissao.Lenhador
        );
        await ColetarAsync(
            id,
            ConfigProfissoes.NoVeioDeFerro1,
            ConfigProfissoes.NoVeioDeFerro2,
            ConfigProfissoes.NoArvore1
        );
        await _ambiente.DarItensAsync(
            id,
            CatalogoItens.EspadaDeTeste,
            RegrasItens.EspacosInventario
        );

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaEspada, default)
        );

        Assert.Equal(CodigosErroProfissoes.InventarioCheio, erro);
        Assert.Null(resultado);
        Assert.Equal(4, await SaldoAsync(id, ConfigProfissoes.MaterialMinerioDeFerro));
        Assert.Equal(2, await SaldoAsync(id, ConfigProfissoes.MaterialMadeira));
    }

    [Fact]
    public async Task Receita_que_nao_existe_e_recusada()
    {
        var id = await NovoAprendizAsync(Profissao.Ferreiro);

        var (erro, _) = await ComServicoAsync(s => s.ProduzirAsync(id, 999, default));

        Assert.Equal(CodigosErroProfissoes.ReceitaInexistente, erro);
    }

    [Fact]
    public async Task Materiais_nao_ficam_negativos_no_banco()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await using var escopo = servidor.Services.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        db.Add(
            new SaldoDeMaterialDeProfissao
            {
                PersonagemId = id,
                MaterialId = ConfigProfissoes.MaterialMadeira,
                Quantidade = -1,
            }
        );

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task Coletar_novos_materiais_de_erva_cristal_graos_e_carne()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(
            Profissao.Lenhador,
            Profissao.Minerador,
            Profissao.Curtidor
        );

        await ColetarAsync(
            id,
            ConfigProfissoes.NoArbustoErvas1,
            ConfigProfissoes.NoGeodoCristal1,
            ConfigProfissoes.NoPlantacaoGraos1,
            ConfigProfissoes.NoCaca1
        );

        Assert.Equal(2, await SaldoAsync(id, ConfigProfissoes.MaterialErvaMedicinal));
        Assert.Equal(2, await SaldoAsync(id, ConfigProfissoes.MaterialCristalArcano));
        Assert.Equal(2, await SaldoAsync(id, ConfigProfissoes.MaterialGraos));
        Assert.Equal(2, await SaldoAsync(id, ConfigProfissoes.MaterialCarneDeCaca));
    }

    [Fact]
    public async Task Produzir_curtidor_gasta_materiais_e_entrega_equipamento()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(Profissao.Curtidor, Profissao.Lenhador);
        await ColetarAsync(
            id,
            ConfigProfissoes.NoCarcaca1,
            ConfigProfissoes.NoCarcaca2,
            ConfigProfissoes.NoArvore1
        );

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaColete, default)
        );

        Assert.Null(erro);
        Assert.Equal(111, resultado!.ModeloId);
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialCouroCru));
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialMadeira));
        var bolsa = await _ambiente.BolsaAsync(id);
        Assert.Single(bolsa);
        Assert.Equal(111, bolsa[0].Equipamento!.ModeloId);
    }

    [Fact]
    public async Task Produzir_alquimista_gasta_materiais_e_entrega_foco()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(
            Profissao.Alquimista,
            Profissao.Minerador,
            Profissao.Lenhador
        );
        await ColetarAsync(
            id,
            ConfigProfissoes.NoGeodoCristal1,
            ConfigProfissoes.NoGeodoCristal2,
            ConfigProfissoes.NoArvore1
        );

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaFocoArcano, default)
        );

        Assert.Null(erro);
        Assert.Equal(177, resultado!.ModeloId);
        Assert.Equal(1, await SaldoAsync(id, ConfigProfissoes.MaterialCristalArcano));
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialMadeira));
        var bolsa = await _ambiente.BolsaAsync(id);
        Assert.Single(bolsa);
        Assert.Equal(177, bolsa[0].Equipamento!.ModeloId);
    }

    [Fact]
    public async Task Produzir_cozinheiro_gasta_materiais_e_entrega_refeicao()
    {
        LiberarOsPontos();
        var id = await NovoAprendizAsync(
            Profissao.Cozinheiro,
            Profissao.Lenhador,
            Profissao.Curtidor
        );
        await ColetarAsync(
            id,
            ConfigProfissoes.NoPlantacaoGraos1,
            ConfigProfissoes.NoCaca1,
            ConfigProfissoes.NoArbustoErvas1
        );

        var (erro, resultado) = await ComServicoAsync(s =>
            s.ProduzirAsync(id, ConfigProfissoes.ReceitaCaldoDosErrantes, default)
        );

        Assert.Null(erro);
        Assert.Equal(109, resultado!.ModeloId);
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialGraos));
        Assert.Equal(0, await SaldoAsync(id, ConfigProfissoes.MaterialCarneDeCaca));
        Assert.Equal(1, await SaldoAsync(id, ConfigProfissoes.MaterialErvaMedicinal));
        var bolsa = await _ambiente.BolsaAsync(id);
        Assert.Single(bolsa);
        Assert.Equal(109, bolsa[0].Equipamento!.ModeloId);
    }
}
