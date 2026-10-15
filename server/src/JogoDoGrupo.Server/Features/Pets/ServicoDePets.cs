using JogoDoGrupo.Server.Contratos.Itens;
using JogoDoGrupo.Server.Contratos.Mundo;
using JogoDoGrupo.Server.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pets;

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
    ServidorTempoReal tempoReal,
    IPosicoesDoMundo posicoes,
    IVizinhanca vizinhanca,
    ILootDoDono? loot,
    ILogger<ServicoDePets> log
) : IHostedService
{
    private readonly Dictionary<long, PetDoDono> _pets = [];

    public Task StartAsync(CancellationToken cancelamento) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancelamento) => Task.CompletedTask;

    /// <summary>Quantos pets existem agora.</summary>
    public int Quantidade => _pets.Count;

    /// <summary>Trata <see cref="ChamarPet"/> (laço de rede).</summary>
    public void AoChamar(SessaoTempoReal sessao, ChamarPet pedido)
    {
        var donoId = sessao.PersonagemId;
        if (_pets.ContainsKey(donoId))
        {
            return;
        }

        if (!posicoes.TentarObter(donoId, out var dono) || !Enum.IsDefined(pedido.Especie))
        {
            Recusar(sessao, CodigosErroPets.SemPosicao);
            return;
        }

        RegrasPets.PontoDeChegada(dono.X, dono.Z, dono.RotacaoY, out var x, out var z);
        var pet = new PetDoDono(donoId, pedido.Especie, x, dono.Y, z, dono.RotacaoY);
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
            && !posicoes.TentarObter(saida.PersonagemId, out _)
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
            !_pets.TryGetValue(entrada.PersonagemId, out var pet)
            || !posicoes.TentarObter(entrada.PersonagemId, out var dono)
        )
        {
            return;
        }

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

        pet.Vistos.Clear();
        RegrasPets.PontoDeChegada(dono.X, dono.Z, dono.RotacaoY, out var x, out var z);
        pet.X = x;
        pet.Y = dono.Y;
        pet.Z = z;
        pet.RotacaoY = dono.RotacaoY;
        pet.Andando = false;
        pet.Atividade = AtividadeDoPet.Seguindo;
        Publicar(pet, mudou: false);
    }

    /// <summary>Um passo de <paramref name="segundos"/> para todos os pets (laço do tempo real).</summary>
    public void Avancar(float segundos)
    {
        if (_pets.Count == 0)
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
                log.LogError(erro, "Erro ao mover o pet do personagem {Dono}.", pet.DonoId);
            }
        }
    }

    private void Avancar(PetDoDono pet, float dt)
    {
        if (!posicoes.TentarObter(pet.DonoId, out var dono))
        {
            Remover(pet);
            return;
        }

        var xAntes = pet.X;
        var zAntes = pet.Z;
        var andavaAntes = pet.Andando;
        pet.Y = dono.Y;

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
            log.LogError(erro, "Erro ao coletar loot pelo pet do personagem {Dono}.", pet.DonoId);
        }

        tempoReal.NoLaco(() =>
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
                    // Sequenciada no canal 0: o canal por entidade (DonoId) seria o mesmo da
                    // PosicaoJogador do dono, e um pacote fora de ordem descartaria o outro.
                    EntregaTempoReal.Sequenciada
                );
            }
        }
    }

    private void Remover(PetDoDono pet)
    {
        _pets.Remove(pet.DonoId);
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

        pet.Vistos.Clear();
    }

    private void Recusar(SessaoTempoReal sessao, string codigo) =>
        tempoReal.Enviar(sessao, PetsMessageIds.PetRecusado, new PetRecusado { Codigo = codigo });
}
