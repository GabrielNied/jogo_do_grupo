using JogoDoGrupo.Server.Features.Missoes;
using JogoDoGrupo.Server.Tests.Infra;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Npcs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JogoDoGrupo.Server.Tests.Features.Missoes;

/// <summary>
/// Testes das Missões Secundárias da Brasa Mansa e arredores (#1060).
/// Valida pré-requisitos, objetivos variados (Coletar, IrA, Falar, Matar),
/// cadeia de missões (102 -> 107), entrega a outro NPC e reputação de facção.
/// </summary>
public sealed class MissoesSecundariasTestes(ServidorDeTeste servidor)
    : IClassFixture<ServidorDeTeste>
{
    private readonly AmbienteDeNegociacao _ambiente = new(servidor);

    private async Task<T> ComServicoAsync<T>(Func<ServicoDeMissoes, Task<T>> acao)
    {
        await using var escopo = servidor.Services.CreateAsyncScope();
        return await acao(escopo.ServiceProvider.GetRequiredService<ServicoDeMissoes>());
    }

    [Fact]
    public async Task Secundarias_BloqueadasAtePreRequisitosCumpridos()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // Nível 1 sem progresso na história principal
        var (ok101, erro101, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, default)
        );
        Assert.False(ok101);
        Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erro101);

        var (ok102, erro102, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao102MinerioParaABigorna, default)
        );
        Assert.False(ok102);
        // Nível mínimo é 2 para a 102
        Assert.Equal(CodigosErroMissoes.NivelInsuficiente, erro102);

        // Subir para nível 3 mas sem missões principais concluídas
        await using (var escopo = servidor.Services.CreateAsyncScope())
        {
            await escopo
                .ServiceProvider.GetRequiredService<JogoDoGrupo.Server.Contratos.Personagem.IAjusteDeExperienciaDeTeste>()
                .AjustarAsync(id, 2000, default);
        }

        // Agora com nível 3, deve falhar por pré-requisitos não cumpridos
        var (ok102Nv3, erro102Nv3, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao102MinerioParaABigorna, default)
        );
        Assert.False(ok102Nv3);
        Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erro102Nv3);

        // Missao 107 requer especificamente a secundária 102 concluída
        var (ok107, erro107, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao107ReforcoDaPalicada, default)
        );
        Assert.False(ok107);
        Assert.Equal(CodigosErroMissoes.PreRequisitosNaoCumpridos, erro107);
    }

    [Fact]
    public async Task MissaoColetar_CaldeiraoDaLareira_ColetaProgressivaEConclusao()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // Concluir Etapas 1 a 4 para liberar a 101
        await ConcluirEtapasAteQuatroAsync(id);

        var (ok, erro, missao) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, default)
        );
        Assert.True(ok, erro);
        Assert.NotNull(missao);
        Assert.Equal(CatalogoDeMissoes.Missao101CaldeiraoDaLareira, missao.MissaoId);

        // Coleta parcial de carne (2 de 4)
        var atualizacao1 = await ComServicoAsync(s =>
            s.RegistrarColetaItemAsync(id, itemId: 1, quantidade: 2, default)
        );
        Assert.Single(atualizacao1);
        Assert.Equal(2, atualizacao1[0].Objetivos[0].QuantidadeAtual);
        Assert.False(atualizacao1[0].Objetivos[0].Concluido);
        Assert.Equal(EstadoDaMissao.EmAndamento, atualizacao1[0].Estado);

        // Coleta restante (mais 2 de 4)
        var atualizacao2 = await ComServicoAsync(s =>
            s.RegistrarColetaItemAsync(id, itemId: 1, quantidade: 2, default)
        );
        Assert.Single(atualizacao2);
        Assert.Equal(4, atualizacao2[0].Objetivos[0].QuantidadeAtual);
        Assert.True(atualizacao2[0].Objetivos[0].Concluido);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, atualizacao2[0].Estado);

        // Entregar e receber recompensa
        var saldoAntes = await _ambiente.SaldoAsync(id);
        var (okEntrega, erroEntrega, concluida, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao101CaldeiraoDaLareira, null, default)
        );
        Assert.True(okEntrega, erroEntrega);
        Assert.NotNull(concluida);
        Assert.Equal(EstadoDaMissao.Concluida, concluida.Estado);

        var saldoDepois = await _ambiente.SaldoAsync(id);
        Assert.Equal(saldoAntes + 35, saldoDepois); // 35 coroas definidas no catálogo
    }

    [Fact]
    public async Task CadeiaDeMissoes_MinerioParaABigornaDesbloqueiaReforcoDaPalicada()
    {
        var id = await _ambiente.NovoPersonagemAsync();

        // Concluir Etapas 1 a 5 para liberar a 102
        await ConcluirEtapasAteCincoAsync(id);

        // 1. Aceitar e completar Missao 102 (Minério para a Bigorna)
        var (ok102, erro102, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao102MinerioParaABigorna, default)
        );
        Assert.True(ok102, erro102);

        // Coleta 6 minérios de ferro (itemId: 2)
        await ComServicoAsync(s =>
            s.RegistrarColetaItemAsync(id, itemId: 2, quantidade: 6, default)
        );

        var (okEnt102, erroEnt102, conc102, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao102MinerioParaABigorna, null, default)
        );
        Assert.True(okEnt102, erroEnt102);
        Assert.NotNull(conc102);

        // 2. Agora que 102 está concluída, Missão 107 (Reforço da Paliçada) deve poder ser aceita
        var (ok107, erro107, missao107) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao107ReforcoDaPalicada, default)
        );
        Assert.True(ok107, erro107);
        Assert.NotNull(missao107);
        var def107 = CatalogoDeMissoes.Obter(CatalogoDeMissoes.Missao107ReforcoDaPalicada)!;
        Assert.Equal(CatalogoNpcs.IdFerreiro, def107.NpcDoadorId);
        Assert.Equal(CatalogoNpcs.IdValeria, def107.NpcEntregadorId);

        // Objetivo é falar com a Capitã Valéria
        var at107 = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdValeria, default)
        );
        Assert.Single(at107);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, at107[0].Estado);

        // Entregar para Valéria
        var (okEnt107, erroEnt107, conc107, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao107ReforcoDaPalicada, null, default)
        );
        Assert.True(okEnt107, erroEnt107);
        Assert.NotNull(conc107);
        Assert.Equal(EstadoDaMissao.Concluida, conc107.Estado);
    }

    [Fact]
    public async Task MissaoExploracao_OMascoteFugitivo_IrAAreaEFalarComNpc()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ConcluirEtapasAteCincoAsync(id);

        var (ok, erro, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao103OMascoteFugitivo, default)
        );
        Assert.True(ok, erro);

        // 1. Tentar falar com Mirna antes de ir até o local: objetivo 2 não avança ou fica incompleto
        // O primeiro objetivo é IrA Moinho Decrépito [25, 65] com raio 20m
        // Chegar longe (ex: x: 100, z: 100) não completa
        var atLonge = await ComServicoAsync(s =>
            s.RegistrarChegadaLocalAsync(id, 100f, 100f, "campo", default)
        );
        Assert.Empty(atLonge);

        // Chegar dentro do raio (ex: x: 26, z: 66)
        var atPerto = await ComServicoAsync(s =>
            s.RegistrarChegadaLocalAsync(id, 26f, 66f, "campo", default)
        );
        Assert.Single(atPerto);
        Assert.Equal(1, atPerto[0].Objetivos[0].QuantidadeAtual);
        Assert.True(atPerto[0].Objetivos[0].Concluido);
        Assert.False(atPerto[0].Objetivos[1].Concluido);
        Assert.Equal(EstadoDaMissao.EmAndamento, atPerto[0].Estado);

        // 2. Falar com a Cuidadora Mirna
        var atFalar = await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdCuidadorDePets, default)
        );
        Assert.Single(atFalar);
        Assert.True(atFalar[0].Objetivos[1].Concluido);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, atFalar[0].Estado);

        // Entregar
        var (okEnt, erroEnt, conc, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao103OMascoteFugitivo, null, default)
        );
        Assert.True(okEnt, erroEnt);
        Assert.NotNull(conc);
    }

    [Fact]
    public async Task MissaoCacada_VigilanciaNoPortaoLeste_AbateDeLobos()
    {
        var id = await _ambiente.NovoPersonagemAsync();
        await ConcluirEtapasAteSeisAsync(id);

        var (ok, erro, _) = await ComServicoAsync(s =>
            s.AceitarMissaoAsync(id, CatalogoDeMissoes.Missao105VigilanciaNoPortaoLeste, default)
        );
        Assert.True(ok, erro);

        // Derrotar 5 lobos (mobId: 2)
        for (int i = 0; i < 4; i++)
        {
            var at = await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 2, default));
            Assert.Contains(
                at,
                m => m.MissaoId == CatalogoDeMissoes.Missao105VigilanciaNoPortaoLeste
            );
        }

        var atFinal = await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 2, default));
        var mFinal = atFinal.Find(m =>
            m.MissaoId == CatalogoDeMissoes.Missao105VigilanciaNoPortaoLeste
        );
        Assert.NotNull(mFinal);
        Assert.Equal(EstadoDaMissao.ProntaParaEntregar, mFinal.Estado);

        var (okEnt, erroEnt, conc, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao105VigilanciaNoPortaoLeste,
                null,
                default
            )
        );
        Assert.True(okEnt, erroEnt);
        Assert.NotNull(conc);
    }

    // --- Auxiliares de Progressão da História Principal ---

    private async Task ConcluirEtapasAteQuatroAsync(long id)
    {
        await ComServicoAsync(s => s.GarantirMissaoInicialAsync(id, default));

        // Etapa 1
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdVendedor, default)
        );
        var (ok1, erro1, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao1PortoesDaBrasaMansa, null, default)
        );
        Assert.True(ok1, erro1);

        // Etapa 2
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdArmeiro, default)
        );
        var (ok2, erro2, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao2PrimeiraFerramenta,
                CatalogoEquipamentos.PrimeiroModeloDeArma,
                default
            )
        );
        Assert.True(ok2, erro2);

        // Etapa 3
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdFerreiro, default)
        );
        var (ok3, erro3, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao3CascaDeFerroECouro,
                CatalogoEquipamentos.PrimeiroModelo,
                default
            )
        );
        Assert.True(ok3, erro3);

        // Etapa 4
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdTaverneiro, default)
        );
        await ComServicoAsync(s =>
            s.RegistrarInteracaoNpcAsync(id, CatalogoNpcs.IdBanqueiro, default)
        );
        var (ok4, erro4, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(
                id,
                CatalogoDeMissoes.Missao4ProvisoesEGuardaVolumes,
                null,
                default
            )
        );
        Assert.True(ok4, erro4);
    }

    private async Task ConcluirEtapasAteCincoAsync(long id)
    {
        await ConcluirEtapasAteQuatroAsync(id);

        // Etapa 5 (matar 6 javalis)
        for (int i = 0; i < 6; i++)
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 1, default));

        var (ok5, erro5, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao5LimpandoATrilhaNorte, null, default)
        );
        Assert.True(ok5, erro5);
    }

    private async Task ConcluirEtapasAteSeisAsync(long id)
    {
        await ConcluirEtapasAteCincoAsync(id);

        // Etapa 6 (matar 5 lobos)
        for (int i = 0; i < 5; i++)
            await ComServicoAsync(s => s.RegistrarMorteInimigoAsync(id, 2, default));

        var (ok6, erro6, _, _) = await ComServicoAsync(s =>
            s.EntregarMissaoAsync(id, CatalogoDeMissoes.Missao6OEnxameNaMataFria, null, default)
        );
        Assert.True(ok6, erro6);
    }
}
