using System.Collections.Concurrent;
using JogoDoGrupo.Server.Contratos.Combate;
using JogoDoGrupo.Server.Contratos.Economia;
using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.Persistencia;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Server.Features.Itens;
using JogoDoGrupo.Shared.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Npcs;
using JogoDoGrupo.Shared.Features.Pets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JogoDoGrupo.Server.Features.Pets;

/// <summary>
/// Os pets no servidor (docs/features/pets.md). O dono pede para chamar ou dispensar; a cada
/// passo do laço o servidor move o pet (seguir a 2 m, ir buscar loot) e publica a posição a quem
/// vê o dono, do mesmo jeito dos mobs: <c>PetApareceu</c> ao entrar no interesse, <c>PetMudou</c>
/// sequenciado enquanto anda, <c>PetSumiu</c> ao sair.
/// <para>
/// Pets não conhece Personagem, Itens nem Mobs: posição e interesse vêm do Mundo
/// (<see cref="IPosicoesDoMundo"/>, <see cref="IVizinhanca"/>) e o loot vem de Itens
/// (<see cref="ILootDoDono"/>), que decide o que o dono pode coletar e grava no inventário.
/// Chamar, dispensar, eventos e passo rodam no mesmo laço do tempo real, então o estado dos pets
/// não precisa de trava; a resposta assíncrona da coleta volta ao laço por <c>NoLaco</c>.
/// </para>
/// </summary>
public sealed class ServicoDePets(
    ServidorTempoReal? tempoReal = null,
    IPosicoesDoMundo? posicoes = null,
    IVizinhanca? vizinhanca = null,
    ILootDoDono? loot = null,
    ILogger<ServicoDePets>? log = null,
    IServiceScopeFactory? scopeFactory = null,
    IEstadoDeCombate? combate = null,
    ICarteiraCoroas? carteiraCoroas = null
) : IHostedService
{
    /// <summary>
    /// Deslocamento do canal do pet em relação ao do dono. Com <c>entidadeId = DonoId</c> o
    /// <c>PetMudou</c> cairia no canal da <c>PosicaoJogador</c> do dono e um descartaria o outro;
    /// sem entidade, todos os pets dividiriam o canal 0. Meia volta nos canais nunca cai no do dono.
    /// </summary>
    public const int DeslocamentoDoCanal = TransporteTempoReal.Canais / 2;

    private readonly Dictionary<long, PetDoDono> _pets = [];
    private readonly ConcurrentDictionary<long, HashSet<int>> _colecoesPorConta = new();

    /// <summary>Entidade do canal sequenciado do pet de <paramref name="donoId"/>.</summary>
    public static long EntidadeDoPet(long donoId) => donoId + DeslocamentoDoCanal;

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;

    /// <summary>Quantos pets existem agora.</summary>
    public int Quantidade => _pets.Count;

    /// <summary>Obtém a lista de espécies desbloqueadas pela conta.</summary>
    public HashSet<int> ObterColecao(long contaId)
    {
        if (contaId <= 0)
        {
            return [];
        }

        if (_colecoesPorConta.TryGetValue(contaId, out var existente))
        {
            return existente;
        }

        var novaColecao = new HashSet<int>();
        if (scopeFactory != null)
        {
            using var escopo = scopeFactory.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var salvos = db.Set<PetDaConta>()
                .Where(p => p.ContaId == contaId)
                .Select(p => p.Especie)
                .ToList();

            foreach (var sp in salvos)
            {
                novaColecao.Add(sp);
            }
        }

        return _colecoesPorConta.GetOrAdd(contaId, novaColecao);
    }

    /// <summary>Verifica se a conta possui a espécie de pet desbloqueada.</summary>
    public bool PossuiPet(long contaId, EspecieDePet especie)
    {
        if (RegrasPets.DesbloqueadoPorPadrao(especie))
        {
            return true;
        }

        var colecao = ObterColecao(contaId);
        lock (colecao)
        {
            return colecao.Contains((int)especie);
        }
    }

    /// <summary>Desbloqueia uma espécie diretamente em memória para testes de integração.</summary>
    public void DesbloquearParaTeste(long contaId, EspecieDePet especie)
    {
        var colecao = _colecoesPorConta.GetOrAdd(contaId, _ => new HashSet<int>());
        lock (colecao)
        {
            colecao.Add((int)especie);
        }
    }

    /// <summary>Envia a coleção de pets desbloqueados para a sessão do jogador.</summary>
    public void EnviarColecao(SessaoTempoReal sessao)
    {
        if (sessao == null)
        {
            return;
        }

        var colecao = ObterColecao(sessao.ContaId);
        int[] lista;
        lock (colecao)
        {
            lista = colecao.ToArray();
        }

        tempoReal?.Enviar(
            sessao,
            PetsMessageIds.ColecaoPets,
            new ColecaoPets { EspeciesDesbloqueadas = lista }
        );
    }

    /// <summary>Envia o catálogo de ofertas do cuidador de pets à sessão (#1068).</summary>
    public void ConsultarCuidador(SessaoTempoReal sessao)
    {
        if (sessao == null || tempoReal == null)
        {
            return;
        }

        tempoReal.Enviar(
            sessao,
            PetsMessageIds.RespostaCuidador,
            new RespostaCuidador(ConfigCuidadorDePets.Ofertas)
        );
    }

    /// <summary>
    /// Consome um item consumível de pet do inventário e registra o pet na conta do jogador.
    /// </summary>
    public async Task<bool> UsarItemPetAsync(
        long contaId,
        long personagemId,
        long instanciaId,
        SessaoTempoReal? sessao = null
    )
    {
        if (combate?.EmCombate(personagemId) == true)
        {
            if (sessao != null)
            {
                Recusar(sessao, CodigosErroPets.EmCombate);
            }
            return false;
        }

        if (scopeFactory == null)
        {
            return false;
        }

        using var escopo = scopeFactory.CreateScope();
        var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();

        var item = await db.Set<ItemInventario>()
            .SingleOrDefaultAsync(i => i.Id == instanciaId && i.PersonagemId == personagemId);

        if (item == null)
        {
            if (sessao != null)
            {
                Recusar(sessao, CodigosErroPets.ItemNaoEncontrado);
            }
            return false;
        }

        var especie = RegrasPets.EspecieDoItem(item.ItemId);
        if (especie == 0)
        {
            if (sessao != null)
            {
                Recusar(sessao, CodigosErroPets.ItemNaoEPet);
            }
            return false;
        }

        if (PossuiPet(contaId, especie))
        {
            if (sessao != null)
            {
                Recusar(sessao, CodigosErroPets.JaPossuiPet);
            }
            return false;
        }

        db.Set<ItemInventario>().Remove(item);

        var registro = new PetDaConta
        {
            ContaId = contaId,
            Especie = (int)especie,
            DesbloqueadoEm = DateTime.UtcNow,
        };
        db.Set<PetDaConta>().Add(registro);
        await db.SaveChangesAsync();

        var colecao = _colecoesPorConta.GetOrAdd(contaId, _ => new HashSet<int>());
        lock (colecao)
        {
            colecao.Add((int)especie);
        }

        if (tempoReal != null)
        {
            tempoReal.NoLaco(() =>
            {
                if (sessao != null)
                {
                    tempoReal.Enviar(
                        sessao,
                        PetsMessageIds.PetDesbloqueado,
                        new PetDesbloqueado
                        {
                            Especie = (int)especie,
                            Nome = RegrasPets.ObterNome(especie),
                        }
                    );

                    EnviarColecao(sessao);

                    if (!_pets.ContainsKey(personagemId))
                    {
                        AoChamar(sessao, new ChamarPet { Especie = especie });
                    }
                }
            });
        }

        return true;
    }

    /// <summary>
    /// Compra um pet vendido pela Cuidadora Mirna na Brasa Mansa por coroas (#1068).
    /// </summary>
    public async Task<(
        bool Sucesso,
        string? CodigoErro,
        string Mensagem,
        long SaldoRestante
    )> ComprarPetCuidadorAsync(
        long personagemId,
        int especie,
        SessaoTempoReal? sessao = null,
        CancellationToken ct = default
    )
    {
        if (!ConfigCuidadorDePets.EstaAVenda(especie))
        {
            var erro = CodigosErroPets.PetNaoAVenda;
            var msg = "Esta espécie de pet não está à venda no cuidador.";
            NotificarRecusaCompra(sessao, especie, erro, msg);
            return (false, erro, msg, 0);
        }

        if (combate?.EmCombate(personagemId) == true)
        {
            var erro = CodigosErroPets.EmCombate;
            var msg = "Não é possível comprar pets durante o combate.";
            NotificarRecusaCompra(sessao, especie, erro, msg);
            return (false, erro, msg, 0);
        }

        if (posicoes != null && posicoes.TentarObter(personagemId, out var pos))
        {
            var npc = CatalogoNpcs.CuidadorDePets;
            if (pos.Area != npc.Area || !npc.Alcanca(pos.X, pos.Z))
            {
                var erro = CodigosErroPets.LongeDoNpc;
                var msg = "Você está muito longe do cuidador de pets para comprar.";
                NotificarRecusaCompra(sessao, especie, erro, msg);
                return (false, erro, msg, 0);
            }
        }

        long contaId = sessao?.ContaId ?? 0;

        if (scopeFactory == null && carteiraCoroas == null)
        {
            return (false, CodigosErroPets.PetNaoAVenda, "Serviço indisponível.", 0);
        }

        if (scopeFactory != null)
        {
            using var escopo = scopeFactory.CreateScope();
            var db = escopo.ServiceProvider.GetRequiredService<JogoDbContext>();
            var carteira = carteiraCoroas ?? escopo.ServiceProvider.GetService<ICarteiraCoroas>();

            if (contaId <= 0)
            {
                var p = await db.Set<JogoDoGrupo.Server.Features.Personagem.Personagem>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == personagemId, ct);
                if (p != null)
                {
                    contaId = p.ContaId;
                }
            }

            if (contaId > 0 && PossuiPet(contaId, (EspecieDePet)especie))
            {
                var erro = CodigosErroPets.JaPossuiPet;
                var msg = "Você já possui este pet registrado em sua conta.";
                NotificarRecusaCompra(sessao, especie, erro, msg);
                return (false, erro, msg, 0);
            }

            var preco = ConfigCuidadorDePets.ObterPreco(especie);

            await using var tx = await db.Database.BeginTransactionAsync(ct);

            var jaTemNoBanco = await db.Set<PetDaConta>()
                .AnyAsync(p => p.ContaId == contaId && p.Especie == especie, ct);
            if (jaTemNoBanco)
            {
                var erro = CodigosErroPets.JaPossuiPet;
                var msg = "Você já possui este pet registrado em sua conta.";
                NotificarRecusaCompra(sessao, especie, erro, msg);
                return (false, erro, msg, 0);
            }

            if (carteira != null)
            {
                var debitou = await carteira.DebitarAsync(personagemId, preco, ct);
                if (!debitou)
                {
                    var erro = CodigosErroPets.SaldoInsuficiente;
                    var msg = "Você não possui coroas suficientes para comprar este pet.";
                    NotificarRecusaCompra(sessao, especie, erro, msg);
                    return (false, erro, msg, 0);
                }
            }

            var registro = new PetDaConta
            {
                ContaId = contaId,
                Especie = especie,
                DesbloqueadoEm = DateTime.UtcNow,
            };
            db.Set<PetDaConta>().Add(registro);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            if (contaId > 0)
            {
                var colecao = ObterColecao(contaId);
                lock (colecao)
                {
                    colecao.Add(especie);
                }
            }

            long saldoRestante = 0;
            if (carteira != null)
            {
                saldoRestante = await carteira.SaldoAsync(personagemId, ct);
            }

            if (sessao != null && tempoReal != null)
            {
                tempoReal.Enviar(
                    sessao,
                    PetsMessageIds.PetDesbloqueado,
                    new PetDesbloqueado
                    {
                        Especie = especie,
                        Nome = RegrasPets.ObterNome((EspecieDePet)especie),
                    }
                );
                tempoReal.Enviar(
                    sessao,
                    PetsMessageIds.RespostaCompraPet,
                    new RespostaCompraPet(
                        true,
                        especie,
                        null,
                        "Pet adquirido com sucesso!",
                        saldoRestante
                    )
                );
            }

            return (true, null, "Pet adquirido com sucesso!", saldoRestante);
        }

        // Testes isolados com mock sem banco
        if (contaId > 0 && PossuiPet(contaId, (EspecieDePet)especie))
        {
            var erro = CodigosErroPets.JaPossuiPet;
            var msg = "Você já possui este pet registrado em sua conta.";
            NotificarRecusaCompra(sessao, especie, erro, msg);
            return (false, erro, msg, 0);
        }

        var precoMem = ConfigCuidadorDePets.ObterPreco(especie);
        if (carteiraCoroas != null)
        {
            var debitou = await carteiraCoroas.DebitarAsync(personagemId, precoMem, ct);
            if (!debitou)
            {
                var erro = CodigosErroPets.SaldoInsuficiente;
                var msg = "Você não possui coroas suficientes para comprar este pet.";
                NotificarRecusaCompra(sessao, especie, erro, msg);
                return (false, erro, msg, 0);
            }
        }

        if (contaId > 0)
        {
            var colecao = ObterColecao(contaId);
            lock (colecao)
            {
                colecao.Add(especie);
            }
        }

        long saldoRestanteMem =
            carteiraCoroas != null ? await carteiraCoroas.SaldoAsync(personagemId, ct) : 0;
        return (true, null, "Pet adquirido com sucesso!", saldoRestanteMem);
    }

    private void NotificarRecusaCompra(
        SessaoTempoReal? sessao,
        int especie,
        string codigoErro,
        string mensagem
    )
    {
        if (sessao != null && tempoReal != null)
        {
            tempoReal.Enviar(
                sessao,
                PetsMessageIds.RespostaCompraPet,
                new RespostaCompraPet(false, especie, codigoErro, mensagem, 0)
            );
        }
    }

    /// <summary>Trata <see cref="ChamarPet"/> (laço de rede).</summary>
    public void AoChamar(SessaoTempoReal sessao, ChamarPet pedido)
    {
        var donoId = sessao.PersonagemId;
        if (_pets.ContainsKey(donoId))
        {
            return;
        }

        if (
            posicoes == null
            || !posicoes.TentarObter(donoId, out var dono)
            || !Enum.IsDefined(pedido.Especie)
        )
        {
            Recusar(sessao, CodigosErroPets.SemPosicao);
            return;
        }

        if (!PossuiPet(sessao.ContaId, pedido.Especie))
        {
            Recusar(sessao, CodigosErroPets.PetNaoDesbloqueado);
            return;
        }

        RegrasPets.PontoDeChegada(dono.X, dono.Z, dono.RotacaoY, out var x, out var z);
        var pet = new PetDoDono(
            donoId,
            pedido.Especie,
            x,
            RegrasPets.AlturaDoPet(x, z, dono.X, dono.Z, dono.Y),
            z,
            dono.RotacaoY
        );
        _pets[donoId] = pet;
        Publicar(pet, mudou: false);
    }

    /// <summary>Trata <see cref="DispensarPet"/> (laço de rede).</summary>
    public void AoDispensar(SessaoTempoReal sessao)
    {
        if (!_pets.TryGetValue(sessao.PersonagemId, out var pet))
        {
            Recusar(sessao, CodigosErroPets.SemPet);
            return;
        }

        Remover(pet);
    }

    /// <summary>
    /// Quem deixou uma área por sair do jogo ou cair fica sem pet. Quem só trocou de área (cidade
    /// e instância) continua no Mundo: o pet vai junto, em <see cref="AoEntrarNaArea"/>.
    /// </summary>
    public void AoSairDaArea(JogadorSaiuDaArea saida)
    {
        if (
            _pets.TryGetValue(saida.PersonagemId, out var pet)
            && (posicoes == null || !posicoes.TentarObter(saida.PersonagemId, out _))
        )
        {
            Remover(pet);
        }
    }

    /// <summary>
    /// Quem entra noutra área (caverna, dungeon ou volta à cidade) leva o pet: ele some para
    /// quem via na área antiga e reaparece junto do dono na nova.
    /// </summary>
    public void AoEntrarNaArea(JogadorEntrouNaArea entrada)
    {
        if (
            tempoReal != null
            && tempoReal.TentarObterSessao(entrada.PersonagemId, out var sessaoEntrou)
            && sessaoEntrou != null
        )
        {
            EnviarColecao(sessaoEntrou);
        }

        if (
            !_pets.TryGetValue(entrada.PersonagemId, out var pet)
            || posicoes == null
            || !posicoes.TentarObter(entrada.PersonagemId, out var dono)
        )
        {
            return;
        }

        if (tempoReal != null)
        {
            foreach (var id in pet.Vistos)
            {
                if (tempoReal.TentarObterSessao(id, out var sessao) && sessao != null)
                {
                    tempoReal.Enviar(
                        sessao,
                        PetsMessageIds.PetSumiu,
                        new PetSumiu { DonoId = pet.DonoId }
                    );
                }
            }
        }

        pet.Vistos.Clear();
        RegrasPets.PontoDeChegada(dono.X, dono.Z, dono.RotacaoY, out var x, out var z);
        pet.X = x;
        pet.Y = RegrasPets.AlturaDoPet(x, z, dono.X, dono.Z, dono.Y);
        pet.Z = z;
        pet.RotacaoY = dono.RotacaoY;
        pet.Andando = false;
        pet.Atividade = AtividadeDoPet.Seguindo;
        Publicar(pet, mudou: false);
    }

    /// <summary>Um passo de <paramref name="segundos"/> para todos os pets (laço do tempo real).</summary>
    public void Avancar(float segundos)
    {
        if (_pets.Count == 0 || posicoes == null)
        {
            return;
        }

        foreach (var pet in _pets.Values.ToArray())
        {
            try
            {
                Avancar(pet, segundos);
            }
            catch (Exception erro)
            {
                log?.LogError(erro, "Erro ao mover o pet do personagem {Dono}.", pet.DonoId);
            }
        }
    }

    private void Avancar(PetDoDono pet, float dt)
    {
        if (posicoes == null || !posicoes.TentarObter(pet.DonoId, out var dono))
        {
            Remover(pet);
            return;
        }

        var xAntes = pet.X;
        var zAntes = pet.Z;
        var andavaAntes = pet.Andando;

        if (
            RegrasPets.Distancia(pet.X, pet.Z, dono.X, dono.Z)
            > ConfigPets.DistanciaParaAlcancarMetros
        )
        {
            // Longe demais: volta para perto do dono e esquece o item.
            if (pet.Atividade != AtividadeDoPet.Coletando)
            {
                pet.Atividade = AtividadeDoPet.Seguindo;
            }

            Aplicar(pet, RegrasPets.Seguir(pet.X, pet.Z, pet.RotacaoY, dono.X, dono.Z, false, dt));
        }
        else
        {
            switch (pet.Atividade)
            {
                case AtividadeDoPet.Coletando:
                    pet.Andando = false;
                    break;
                case AtividadeDoPet.IndoAoLoot:
                    IrAoLoot(pet, dono, dt);
                    break;
                default:
                    Seguir(pet, dono, dt);
                    break;
            }
        }

        pet.Y = RegrasPets.AlturaDoPet(pet.X, pet.Z, dono.X, dono.Z, dono.Y);
        var mudou = pet.X != xAntes || pet.Z != zAntes || pet.Andando != andavaAntes;
        Publicar(pet, mudou);
    }

    private void Seguir(PetDoDono pet, PosicaoNoMundo dono, float dt)
    {
        if (loot != null)
        {
            pet.PausaDeLootEm = Math.Max(0f, pet.PausaDeLootEm - dt);
            pet.ProximaBuscaEm -= dt;
            if (pet.PausaDeLootEm <= 0f && pet.ProximaBuscaEm <= 0f)
            {
                pet.ProximaBuscaEm = ConfigPets.IntervaloDeBuscaDeLootSegundos;
                if (
                    loot.TentarEscolher(
                        pet.DonoId,
                        dono.Area,
                        pet.X,
                        pet.Z,
                        ConfigPets.RaioDeBuscaDeLootMetros,
                        out var alvo
                    )
                )
                {
                    pet.Alvo = alvo;
                    pet.Atividade = AtividadeDoPet.IndoAoLoot;
                    IrAoLoot(pet, dono, dt);
                    return;
                }
            }
        }

        Aplicar(
            pet,
            RegrasPets.Seguir(pet.X, pet.Z, pet.RotacaoY, dono.X, dono.Z, pet.Andando, dt)
        );
    }

    private void IrAoLoot(PetDoDono pet, PosicaoNoMundo dono, float dt)
    {
        if (loot == null || !loot.Continua(pet.DonoId, pet.Alvo.LootId))
        {
            // O dono pegou o item, ele expirou ou o dono perdeu o direito: volta a seguir.
            pet.Atividade = AtividadeDoPet.Seguindo;
            Seguir(pet, dono, dt);
            return;
        }

        var passo = RegrasPets.IrAte(
            pet.X,
            pet.Z,
            pet.RotacaoY,
            pet.Alvo.X,
            pet.Alvo.Z,
            ConfigPets.DistanciaParaPegarMetros,
            dt
        );
        Aplicar(pet, passo);
        if (!passo.Andando)
        {
            pet.Atividade = AtividadeDoPet.Coletando;
            _ = ColetarAsync(pet, pet.Alvo.LootId);
        }
    }

    private async Task ColetarAsync(PetDoDono pet, long lootId)
    {
        var resultado = ResultadoDaColetaParaODono.SemEspaco;
        try
        {
            resultado = await loot!.ColetarParaODono(pet.DonoId, lootId);
        }
        catch (Exception erro)
        {
            log?.LogError(erro, "Erro ao coletar loot pelo pet do personagem {Dono}.", pet.DonoId);
        }

        tempoReal?.NoLaco(() =>
        {
            // O pet pode ter sido dispensado enquanto a coleta rodava.
            if (!_pets.TryGetValue(pet.DonoId, out var atual) || !ReferenceEquals(atual, pet))
            {
                return;
            }

            pet.Atividade = AtividadeDoPet.Seguindo;
            if (resultado == ResultadoDaColetaParaODono.SemEspaco)
            {
                pet.PausaDeLootEm = ConfigPets.PausaSemEspacoSegundos;
            }
        });
    }

    private static void Aplicar(PetDoDono pet, PassoDoPet passo)
    {
        pet.X = passo.X;
        pet.Z = passo.Z;
        pet.RotacaoY = passo.RotacaoY;
        pet.Andando = passo.Andando;
    }

    // Diz a cada pessoa que vê o dono o que ela ainda não sabe: quem entrou no interesse recebe
    // o aparecimento; quem saiu recebe o sumiço; os demais recebem o movimento, se houve.
    private void Publicar(PetDoDono pet, bool mudou)
    {
        if (vizinhanca == null || tempoReal == null)
        {
            return;
        }

        var vendo = new HashSet<long>(vizinhanca.QuemVe(pet.DonoId)) { pet.DonoId };
        foreach (var id in pet.Vistos.Where(id => !vendo.Contains(id)).ToArray())
        {
            pet.Vistos.Remove(id);
            if (tempoReal.TentarObterSessao(id, out var saiu) && saiu != null)
            {
                tempoReal.Enviar(
                    saiu,
                    PetsMessageIds.PetSumiu,
                    new PetSumiu { DonoId = pet.DonoId }
                );
            }
        }

        if (mudou)
        {
            pet.Sequencia++;
        }

        foreach (var id in vendo)
        {
            if (!tempoReal.TentarObterSessao(id, out var sessao) || sessao == null)
            {
                continue;
            }

            if (pet.Vistos.Add(id))
            {
                tempoReal.Enviar(
                    sessao,
                    PetsMessageIds.PetApareceu,
                    new PetApareceu
                    {
                        DonoId = pet.DonoId,
                        Especie = pet.Especie,
                        X = pet.X,
                        Y = pet.Y,
                        Z = pet.Z,
                        RotacaoY = pet.RotacaoY,
                        Andando = pet.Andando,
                        Sequencia = pet.Sequencia,
                    }
                );
            }
            else if (mudou)
            {
                tempoReal.Enviar(
                    sessao,
                    PetsMessageIds.PetMudou,
                    new PetMudou
                    {
                        DonoId = pet.DonoId,
                        X = pet.X,
                        Y = pet.Y,
                        Z = pet.Z,
                        RotacaoY = pet.RotacaoY,
                        Andando = pet.Andando,
                        Sequencia = pet.Sequencia,
                    },
                    EntregaTempoReal.Sequenciada,
                    EntidadeDoPet(pet.DonoId)
                );
            }
        }
    }

    private void Remover(PetDoDono pet)
    {
        _pets.Remove(pet.DonoId);
        if (tempoReal != null)
        {
            foreach (var id in pet.Vistos)
            {
                if (tempoReal.TentarObterSessao(id, out var sessao) && sessao != null)
                {
                    tempoReal.Enviar(
                        sessao,
                        PetsMessageIds.PetSumiu,
                        new PetSumiu { DonoId = pet.DonoId }
                    );
                }
            }
        }

        pet.Vistos.Clear();
    }

    private void Recusar(SessaoTempoReal? sessao, string codigo)
    {
        if (sessao != null && tempoReal != null)
        {
            tempoReal.Enviar(
                sessao,
                PetsMessageIds.PetRecusado,
                new PetRecusado { Codigo = codigo }
            );
        }
    }
}
