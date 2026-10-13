using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Loot
{
    /// <summary>
    /// Filtro de loot do jogador (#565): peças no chão de tier abaixo do mínimo não aparecem, não
    /// acendem a dica "F: pegar" e o servidor as deixa no chão quando a tecla F coleta tudo por
    /// perto (<see cref="ColetarLoot.TierMinimo"/>). É preferência deste computador, guardada no
    /// <c>PlayerPrefs</c>: o servidor não a persiste, o client a manda em cada pedido de coleta.
    /// Item que não é equipamento não tem tier e sempre passa.
    /// </summary>
    public static class FiltroDeLoot
    {
        public const string Chave = "itens.loot.tier_minimo";

        /// <summary>Os rótulos do seletor, na ordem de <see cref="TierItem"/> (do D ao S+).</summary>
        public static readonly string[] Rotulos =
        {
            "Tudo",
            "Tier C ou melhor",
            "Tier B ou melhor",
            "Tier A ou melhor",
            "Tier S ou melhor",
            "Só tier S+",
        };

        private static TierItem? _cache;

        /// <summary>O tier mínimo que o jogador quer ver e coletar (D = tudo).</summary>
        public static TierItem TierMinimo
        {
            get
            {
                if (_cache == null)
                {
                    _cache = Normalizar(PlayerPrefs.GetInt(Chave, (int)TierItem.D));
                }

                return _cache.Value;
            }
            set
            {
                _cache = Normalizar((int)value);
                PlayerPrefs.SetInt(Chave, (int)_cache.Value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Se o item aparece e é coletado com este <paramref name="minimo"/>.</summary>
        public static bool Passa(bool temTier, TierItem tier, TierItem minimo) =>
            !temTier || tier >= minimo;

        /// <summary>Valor guardado inválido (arquivo mexido) volta a "tudo".</summary>
        public static TierItem Normalizar(int valor) =>
            valor >= (int)TierItem.D && valor <= (int)TierItem.SMais ? (TierItem)valor : TierItem.D;

        /// <summary>Esquece o valor em memória (domínio sem recarregar, testes).</summary>
        public static void Reiniciar()
        {
            _cache = null;
        }
    }
}
