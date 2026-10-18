using System;
using JogoDoGrupo.Shared.Features.Mobs;
using JogoDoGrupo.Shared.Features.Montarias;
using JogoDoGrupo.Shared.Features.Pets;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// De qual criatura vem cada item consumível de pet ou montaria (docs/features/pets.md e
    /// montarias.md). Funções puras; o sorteio de chance fica em <see cref="ConfigLoot"/>.
    /// </summary>
    public static class RegrasLootCompanheiros
    {
        /// <summary>
        /// Item de pet que a criatura comum ou elite pode largar, ou 0 se nenhum.
        /// Vários itens na mesma criatura sorteiam um deles com igual chance.
        /// </summary>
        public static int SortearItemDePet(int criaturaId, Random aleatorio)
        {
            var opcoes = ItensDePetDaCriatura(criaturaId);
            if (opcoes.Length == 0)
            {
                return 0;
            }

            return opcoes[aleatorio.Next(opcoes.Length)];
        }

        /// <summary>Item de montaria do chefe ou chefe raro, ou 0 se não houver.</summary>
        public static int ItemDeMontariaDoChefe(int criaturaId) =>
            criaturaId switch
            {
                CatalogoCriaturas.PresaVelha => CatalogoItens.RedeasDoLoboDePresa,
                CatalogoCriaturas.GuardiaoDoReflexo => CatalogoItens.RedeasDoCorcelDeCristal,
                _ => 0,
            };

        /// <summary>Se o id é consumível de pet registrável.</summary>
        public static bool EhItemDePet(int itemId) => RegrasPets.EhItemDePet(itemId);

        /// <summary>Se o id é consumível de montaria registrável.</summary>
        public static bool EhItemDeMontaria(int itemId) =>
            RegrasMontaria.TipoMontariaDoItem(itemId) > 0;

        private static int[] ItensDePetDaCriatura(int criaturaId)
        {
            switch (criaturaId)
            {
                case CatalogoCriaturas.JavaliDoCampo:
                case CatalogoCriaturas.JavaliFerrugem:
                    return new[]
                    {
                        CatalogoItens.SinoDoFilhoteDeJavali,
                        CatalogoItens.SinoDoCoelhoDoPasto,
                    };
                case CatalogoCriaturas.LoboDoCampo:
                    return new[] { CatalogoItens.SinoDoLoboFilhote };
                case CatalogoCriaturas.TatuRochoso:
                    return new[] { CatalogoItens.SinoDoTatuPequeno };
                case CatalogoCriaturas.CarapacaDeBrasa:
                    return new[] { CatalogoItens.ColarDaCarapacaJovem };
                case CatalogoCriaturas.CervoDeGalhadaOca:
                    return new[] { CatalogoItens.SinoDoCervoJovem };
                case CatalogoCriaturas.MariposaDeBreu:
                    return new[] { CatalogoItens.FrascoDaMariposaLuz };
                case CatalogoCriaturas.LoboDeGeada:
                    return new[] { CatalogoItens.SinoDoLoboDeGeadaFilhote };
                case CatalogoCriaturas.MorcegoDasSombras:
                    return new[]
                    {
                        CatalogoItens.SinoDaCorujaDaClareira,
                        CatalogoItens.FrascoDoMosquitoDoPantano,
                    };
                default:
                    return Array.Empty<int>();
            }
        }
    }
}
