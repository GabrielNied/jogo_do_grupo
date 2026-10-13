using System.Data;
using JogoDoGrupo.Server.Contratos.Mobs;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Mobs;
using Microsoft.EntityFrameworkCore;

namespace JogoDoGrupo.Server.Features.Itens;

/// <summary>
/// O loot no chão no servidor (docs/features/fatia-jogavel.md, seção 5). Quando Mobs publica
/// <see cref="InimigoDerrotado"/>, cada participante sorteia o próprio loot, que cai onde o
/// inimigo morreu e só o dono vê. O client pede a coleta; aqui se confere o dono, a distância
/// (com a posição que o Mundo aceitou) e o espaço no inventário, e só então o item é gravado.
/// <para>
/// Os eventos chegam do laço de IA de Mobs e do laço de rede: nada aqui bloqueia. O chão fica
/// em memória (<see cref="ChaoDeLoot"/>) e a gravação no banco roda numa tarefa.
/// </para>
/// </summary>
public sealed class ServicoDeLoot(
    ChaoDeLoot chao,
    ISorteadorDeLoot sorteador,
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    IServiceScopeFactory escopos,
    GeradorEquipamentos gerador,
    TimeProvider relogio,
    ILogger<ServicoDeLoot> log
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken parada)
    {
        using var varredura = new PeriodicTimer(
            TimeSpan.FromSeconds(ConfigLootServidor.IntervaloDaVarreduraSegundos)
        );
        try
        {
            while (await varredura.WaitForNextTickAsync(parada))
            {
                try
                {
                    Expirar();
                }
                catch (Exception erro)
                {
                    log.LogError(erro, "Erro ao expirar o loot do chão.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // O servidor está parando.
        }
    }

    /// <summary>
    /// Trata <see cref="InimigoDerrotado"/>: um sorteio por participante. Quem não participou
    /// não recebe nada; o dono que está fora do jogo vê o item ao voltar, se ainda der tempo.
    /// </summary>
    public void AoInimigoDerrotado(InimigoDerrotado derrota)
    {
        var agora = relogio.GetUtcNow();
        var ficha = CatalogoCriaturas.Obter(derrota.CriaturaId);
        var origem = TabelaDeLoot.OrigemDe(
            ficha?.Categoria ?? CategoriaDeCriatura.Comum,
            derrota.Variante
        );
        // Nível 0: quem publicou não informou; vale o da ficha (ou 1, se a criatura é desconhecida).
        var nivel = derrota.Nivel > 0 ? derrota.Nivel : ficha?.Nivel ?? 1;
        foreach (var donoId in derrota.Participantes.Distinct())
        {
            var quedas = sorteador.Sortear(origem, nivel);
            if (quedas.Length == 0)
            {
                continue;
            }

            // Os valores saem sorteados já na queda: o tier da peça aparece no chão (filtro de
            // loot, #565) e é esse mesmo exemplar que entra no inventário.
            var novos = chao.Largar(
                donoId,
                quedas,
                derrota.Area,
                derrota.X,
                derrota.Z,
                agora,
                queda => gerador.Gerar(queda.ModeloId, Random.Shared, queda.Origem, queda.Grau)
            );
            var sessao = SessaoDe(donoId);
            if (sessao != null)
            {
                Mostrar(sessao, novos, agora);
            }
        }
    }

    /// <summary>Quem entra na área recebe de novo os itens dele que ainda estão no chão dela.</summary>
    public void AoEntrarNaArea(JogadorEntrouNaArea entrada)
    {
        var sessao = SessaoDe(entrada.PersonagemId);
        if (sessao == null)
        {
            return;
        }

        var agora = relogio.GetUtcNow();
        var daArea = chao.DoDono(entrada.PersonagemId, agora)
            .Where(l => l.Area == entrada.Area)
            .ToArray();
        Mostrar(sessao, daArea, agora);
    }

    /// <summary>Trata um <c>ColetarLoot</c> (laço de rede).</summary>
    public void AoColetar(SessaoTempoReal sessao, ColetarLoot pedido)
    {
        var donoId = sessao.PersonagemId;
        var agora = relogio.GetUtcNow();
        var noMundo = posicoes.TentarObter(donoId, out var posicao);

        if (pedido.LootId == RegrasLoot.TudoPorPerto)
        {
            var separados = noMundo
                ? SepararOQueAlcanca(donoId, posicao, agora, pedido.TierMinimo)
                : [];
            if (separados.Count == 0)
            {
                Recusar(sessao, pedido.LootId, CodigosErroItens.LootNaoEncontrado);
                return;
            }

            _ = ColetarAsync(sessao, pedido.LootId, separados);
            return;
        }

        if (!chao.TentarObter(donoId, pedido.LootId, agora, out var loot))
        {
            Recusar(sessao, pedido.LootId, CodigosErroItens.LootNaoEncontrado);
            return;
        }

        if (!noMundo || !Alcanca(posicao, loot))
        {
            Recusar(sessao, pedido.LootId, CodigosErroItens.LongeDemais);
            return;
        }

        // Outro pedido do mesmo dono pode ter separado o item entre as duas chamadas.
        if (!chao.TentarSeparar(donoId, loot.LootId, agora))
        {
            Recusar(sessao, pedido.LootId, CodigosErroItens.LootNaoEncontrado);
            return;
        }

        _ = ColetarAsync(sessao, pedido.LootId, [loot]);
    }

    /// <summary>
    /// Tira do chão o que passou do prazo e avisa os donos que estão no jogo. O laço chama a
    /// cada <see cref="ConfigLootServidor.IntervaloDaVarreduraSegundos"/>; os testes chamam direto.
    /// </summary>
    public void Expirar()
    {
        foreach (var loot in chao.RemoverExpirados(relogio.GetUtcNow()))
        {
            var sessao = SessaoDe(loot.DonoId);
            if (sessao != null)
            {
                tempoReal.Enviar(
                    sessao,
                    ItensMessageIds.LootSumiu,
                    new LootSumiu { LootId = loot.LootId }
                );
            }
        }
    }

    // Grava os itens separados que couberem; o que não coube (ou tudo, se o banco falhar) volta
    // para o chão. Só depois de gravado o item sai do chão e o dono é avisado.
    private async Task ColetarAsync(
        SessaoTempoReal sessao,
        long lootIdDoPedido,
        IReadOnlyList<LootReservado> separados
    )
    {
        var donoId = sessao.PersonagemId;
        IReadOnlyList<ItemInventario> gravados;
        try
        {
            gravados = await GravarOsQueCabemAsync(donoId, separados);
        }
        catch (Exception erro)
        {
            log.LogError(erro, "Erro ao coletar loot do personagem {Personagem}.", donoId);
            foreach (var loot in separados)
            {
                chao.Devolver(loot.LootId);
            }

            return;
        }

        for (var i = 0; i < separados.Count; i++)
        {
            var loot = separados[i];
            if (i >= gravados.Count)
            {
                chao.Devolver(loot.LootId);
                continue;
            }

            chao.Confirmar(loot.LootId);
            tempoReal.Enviar(
                sessao,
                ItensMessageIds.LootColetado,
                new LootColetado { LootId = loot.LootId, Entrada = gravados[i].ParaEntrada() }
            );
        }

        if (gravados.Count < separados.Count)
        {
            Recusar(sessao, lootIdDoPedido, CodigosErroItens.InventarioCheio);
        }
    }

    /// <summary>
    /// Confere o espaço e grava, numa transação só, os primeiros itens que couberem (na ordem
    /// recebida). A transação é a mesma do baú (veja <c>ItensRotas</c>): no SQLite ela já começa
    /// com a trava de escrita, então coletas e aberturas de baú simultâneas rodam uma depois da
    /// outra e o inventário nunca passa de <see cref="RegrasItens.EspacosInventario"/>.
    /// </summary>
    private async Task<IReadOnlyList<ItemInventario>> GravarOsQueCabemAsync(
        long personagemId,
        IReadOnlyList<LootReservado> separados
    )
    {
        await using var escopo = escopos.CreateAsyncScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
        await using var transacao = await db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable
        );

        var capacidade = Bolsa.CapacidadeDe(personagemId);
        var existentes = await db.Set<ItemInventario>()
            .Where(i =>
                i.PersonagemId == personagemId && i.Posicao == PosicaoEquipamento.Inventario
            )
            .OrderBy(i => i.Id)
            .ToListAsync();
        Bolsa.Normalizar(existentes, capacidade);
        var levados = separados.Take(Bolsa.Livres(existentes, capacidade)).ToList();
        var novos = levados
            .Select(l => new ItemInventario { PersonagemId = personagemId, ItemId = l.ItemId })
            .ToList();
        if (novos.Count == 0)
        {
            return novos;
        }

        // Cada item vai para a primeira casa livre.
        Bolsa.Guardar(existentes, novos, capacidade);

        // A origem e o grau da queda valem para a peça que entra no inventário: é aqui que os
        // valores dos atributos são sorteados e o tier de origem é gravado.
        for (var i = 0; i < novos.Count; i++)
        {
            // A peça que caiu já tem os valores sorteados; sem ela (queda sem gerador), gera aqui.
            var equipamento =
                levados[i].Equipamento
                ?? gerador.Gerar(
                    novos[i].ItemId,
                    Random.Shared,
                    levados[i].Origem,
                    levados[i].Grau
                );
            if (equipamento != null)
                novos[i].GravarEquipamento(equipamento);
        }

        db.Set<ItemInventario>().AddRange(novos);
        await db.SaveChangesAsync();
        await transacao.CommitAsync();
        return novos;
    }

    // Separa, para a coleta, todos os itens livres do dono que ele alcança de onde está. Peça de
    // tier abaixo de tierMinimo (filtro do jogador, #565) fica no chão; item sem tier passa.
    private List<LootReservado> SepararOQueAlcanca(
        long donoId,
        PosicaoNoMundo posicao,
        DateTimeOffset agora,
        TierItem tierMinimo = TierItem.D
    )
    {
        var separados = new List<LootReservado>();
        foreach (var loot in chao.DoDono(donoId, agora))
        {
            if (
                Alcanca(posicao, loot)
                && PassaNoFiltro(loot, tierMinimo)
                && chao.TentarSeparar(donoId, loot.LootId, agora)
            )
            {
                separados.Add(loot);
            }
        }

        return separados;
    }

    private static bool PassaNoFiltro(LootReservado loot, TierItem tierMinimo) =>
        loot.Equipamento == null || RegrasTier.TierDe(loot.Equipamento) >= tierMinimo;

    private static bool Alcanca(PosicaoNoMundo posicao, LootReservado loot) =>
        posicao.Area == loot.Area
        && RegrasLoot.DentroDoRaioDeColeta(posicao.X, posicao.Z, loot.X, loot.Z);

    private SessaoTempoReal? SessaoDe(long personagemId) =>
        tempoReal.Sessoes.FirstOrDefault(s => s.PersonagemId == personagemId);

    private void Mostrar(
        SessaoTempoReal sessao,
        IReadOnlyList<LootReservado> itens,
        DateTimeOffset agora
    )
    {
        foreach (var loot in itens)
        {
            tempoReal.Enviar(
                sessao,
                ItensMessageIds.LootApareceu,
                new LootApareceu { Loot = loot.ParaMensagem(agora) }
            );
        }
    }

    private void Recusar(SessaoTempoReal sessao, long lootId, string codigo) =>
        tempoReal.Enviar(
            sessao,
            ItensMessageIds.ColetaRecusada,
            new ColetaRecusada { LootId = lootId, Codigo = codigo }
        );
}
