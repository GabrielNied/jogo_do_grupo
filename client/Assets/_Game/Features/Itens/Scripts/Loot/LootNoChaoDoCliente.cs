using System;
using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
    /// <summary>
    /// Os itens do jogador no chão, como o client os conhece (issue #369). Só mostra: quem
    /// sorteia, guarda o dono e valida a coleta é o servidor. O loot é individual, então aqui só
    /// chegam os itens deste personagem.
    /// </summary>
    public sealed class LootNoChaoDoCliente
    {
        private readonly Dictionary<long, ItemNoChao> _itens = new Dictionary<long, ItemNoChao>();
        private readonly List<long> _vencidos = new List<long>();

        /// <summary>Um item entrou (ou foi trocado, com o mesmo id).</summary>
        public event Action<ItemNoChao> Apareceu;

        /// <summary>Um item saiu do chão (coletado, sumido ou vencido).</summary>
        public event Action<ItemNoChao> Saiu;

        /// <summary>Os itens no chão agora.</summary>
        public IEnumerable<ItemNoChao> Itens => _itens.Values;

        /// <summary>Quantos itens estão no chão.</summary>
        public int Quantidade => _itens.Count;

        /// <summary>O servidor avisou de um item: troca o que havia com o mesmo id.</summary>
        public void Receber(LootApareceu mensagem)
        {
            var loot = mensagem?.Loot;
            if (loot == null || loot.LootId == RegrasLoot.TudoPorPerto)
            {
                return;
            }

            if (_itens.TryGetValue(loot.LootId, out var antigo))
            {
                _itens.Remove(loot.LootId);
                Saiu?.Invoke(antigo);
            }

            var item = new ItemNoChao(loot, Math.Max(0, loot.ExpiraEmSegundos));
            _itens[loot.LootId] = item;
            Apareceu?.Invoke(item);
        }

        /// <summary>O item foi para o inventário. Devolve o nome para o aviso.</summary>
        public string Receber(LootColetado mensagem)
        {
            if (mensagem == null)
            {
                return "";
            }

            var nome = Tirar(mensagem.LootId)?.Nome;
            if (string.IsNullOrEmpty(nome))
            {
                nome = mensagem.Entrada?.Nome ?? "";
            }

            return nome;
        }

        /// <summary>O item expirou no servidor.</summary>
        public void Receber(LootSumiu mensagem)
        {
            if (mensagem != null)
            {
                Tirar(mensagem.LootId);
            }
        }

        /// <summary>
        /// Passa o tempo: o item que venceu sai daqui mesmo antes do <see cref="LootSumiu"/>
        /// (que pode chegar até 1 s depois). Tempo inválido não anda.
        /// </summary>
        public void Avancar(float segundos)
        {
            if (float.IsNaN(segundos) || float.IsInfinity(segundos) || segundos <= 0f)
            {
                return;
            }

            _vencidos.Clear();
            foreach (var item in _itens.Values)
            {
                item.SegundosRestantes -= segundos;
                if (item.SegundosRestantes <= 0f)
                {
                    _vencidos.Add(item.LootId);
                }
            }

            foreach (var id in _vencidos)
            {
                Tirar(id);
            }
        }

        /// <summary>
        /// Algum item ao alcance da tecla F (<see cref="RegrasLoot.DentroDoRaioDeColeta"/>)? Só
        /// para mostrar a dica e não mandar pedido à toa; o servidor confere de novo.
        /// </summary>
        public bool AlgumAoAlcance(float x, float z, TierItem tierMinimo = TierItem.D) =>
            ObterAoAlcance(x, z, tierMinimo, out _, out _);

        /// <summary>
        /// Procura o item mais próximo ao alcance e o total de itens ao alcance que passam no filtro.
        /// Devolve verdadeiro se houver pelo menos um item ao alcance.
        /// </summary>
        public bool ObterAoAlcance(
            float x,
            float z,
            TierItem tierMinimo,
            out ItemNoChao maisProximo,
            out int total
        )
        {
            maisProximo = null;
            total = 0;
            var menorDistanciaSq = double.PositiveInfinity;

            foreach (var item in _itens.Values)
            {
                if (
                    !item.Passa(tierMinimo)
                    || !RegrasLoot.DentroDoRaioDeColeta(x, z, item.X, item.Z)
                )
                {
                    continue;
                }

                total++;
                var dx = (double)(x - item.X);
                var dz = (double)(z - item.Z);
                var distSq = dx * dx + dz * dz;
                if (distSq < menorDistanciaSq)
                {
                    menorDistanciaSq = distSq;
                    maisProximo = item;
                }
            }

            return total > 0;
        }

        /// <summary>O item aparece na tela a até <see cref="RegrasLoot.RaioDeVisaoMetros"/> do jogador.</summary>
        public static bool Visivel(ItemNoChao item, float x, float z)
        {
            var dx = x - item.X;
            var dz = z - item.Z;
            return dx * dx + dz * dz <= RegrasLoot.RaioDeVisaoMetros * RegrasLoot.RaioDeVisaoMetros;
        }

        /// <summary>Esquece tudo (ao sair da conta). Não avisa <see cref="Saiu"/>.</summary>
        public void Limpar()
        {
            _itens.Clear();
        }

        private ItemNoChao Tirar(long lootId)
        {
            if (!_itens.TryGetValue(lootId, out var item))
            {
                return null;
            }

            _itens.Remove(lootId);
            Saiu?.Invoke(item);
            return item;
        }
    }

    /// <summary>Um item do jogador no chão, com o tempo que falta.</summary>
    public sealed class ItemNoChao
    {
        public ItemNoChao(LootNoChao loot, float segundosRestantes)
        {
            LootId = loot.LootId;
            ItemId = loot.ItemId;
            Nome = loot.Nome ?? "";
            TemTier = loot.TemTier;
            Tier = loot.Tier;
            X = loot.X;
            Z = loot.Z;
            SegundosRestantes = segundosRestantes;
        }

        public long LootId { get; }
        public int ItemId { get; }
        public string Nome { get; }

        /// <summary>Se é equipamento e, portanto, tem <see cref="Tier"/>.</summary>
        public bool TemTier { get; }

        public TierItem Tier { get; }

        /// <summary>Se o item passa no filtro de loot do jogador (#565).</summary>
        public bool Passa(TierItem tierMinimo) => FiltroDeLoot.Passa(TemTier, Tier, tierMinimo);

        public float X { get; }
        public float Z { get; }

        /// <summary>Segundos até sumir, contados no client a partir do que o servidor mandou.</summary>
        public float SegundosRestantes { get; internal set; }
    }
}
