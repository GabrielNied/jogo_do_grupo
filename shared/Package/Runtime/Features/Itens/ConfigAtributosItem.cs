using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// A conversão do atributo do item (escala 0 a 100) para o número de combate (ATRIB-003) e a
    /// matriz de atributos elegíveis por categoria (ATRIB-001). <b>Todos os números são valores de
    /// TESTE</b> até a #627; trocá-los é mexer só aqui.
    /// <para>
    /// A conversão é linear por (atributo, categoria): <c>combate = no0 + (no100 - no0) x V /
    /// 100</c>. A mesma Força 80 pode valer diferente numa espada e num anel. Unidade do número de
    /// combate: dano da arma, vida/stamina/mana máximas e defesa em pontos; Força e Intelecto em
    /// fração de bônus de dano (0,10 = +10%); secundários na unidade do Combate (fração de 0 a 1
    /// para crítico, velocidade e penetração, acréscimo ao multiplicador no dano crítico, pontos por
    /// segundo na regeneração).
    /// </para>
    /// </summary>
    public static class ConfigAtributosItem
    {
        // Os quatro "principais da família"; o Espírito só entra onde a matriz o lista.
        private static readonly StatusItem[] Principais =
        {
            StatusItem.Forca,
            StatusItem.Vigor,
            StatusItem.Agilidade,
            StatusItem.Intelecto,
        };

        private static readonly Dictionary<TipoEquipamento, StatusItem[]> Matriz = CriarMatriz();

        private static readonly Dictionary<StatusItem, Conversao> Padrao = new Dictionary<
            StatusItem,
            Conversao
        >
        {
            // Dano da arma: base do golpe e da skill.
            { StatusItem.DanoDaArma, new Conversao(10, 30) },
            // Força e Intelecto do item: bônus de dano, em fração (0,10 = +10%).
            { StatusItem.Forca, new Conversao(0, 0.10) },
            { StatusItem.Intelecto, new Conversao(0, 0.10) },
            // Vigor, Agilidade e Espírito do item: vida, stamina e mana máximas.
            { StatusItem.Vigor, new Conversao(0, 150) },
            { StatusItem.Agilidade, new Conversao(0, 30) },
            { StatusItem.Espirito, new Conversao(0, 30) },
            { StatusItem.DefesaFisica, new Conversao(0, 40) },
            { StatusItem.DefesaMagica, new Conversao(0, 40) },
            { StatusItem.RegeneracaoVida, new Conversao(0, 2) },
            { StatusItem.RegeneracaoStamina, new Conversao(0, 5) },
            { StatusItem.RegeneracaoMana, new Conversao(0, 3) },
            { StatusItem.ChanceCritica, new Conversao(0, 0.10) },
            { StatusItem.DanoCritico, new Conversao(0, 0.50) },
            { StatusItem.VelocidadeAtaqueFisico, new Conversao(0, 0.20) },
            { StatusItem.VelocidadeAtaqueMagico, new Conversao(0, 0.20) },
            { StatusItem.PenetracaoFisica, new Conversao(0, 0.15) },
            { StatusItem.PenetracaoMagica, new Conversao(0, 0.15) },
        };

        // Exceções por categoria: o mesmo valor 0 a 100 vale diferente conforme a peça.
        private static readonly Dictionary<(TipoEquipamento, StatusItem), Conversao> PorCategoria =
            new Dictionary<(TipoEquipamento, StatusItem), Conversao>
            {
                { (TipoEquipamento.Martelo, StatusItem.DanoDaArma), new Conversao(14, 44) },
                { (TipoEquipamento.Arco, StatusItem.DanoDaArma), new Conversao(8, 28) },
                { (TipoEquipamento.Espada, StatusItem.Forca), new Conversao(0, 0.20) },
                { (TipoEquipamento.Martelo, StatusItem.Forca), new Conversao(0, 0.25) },
                { (TipoEquipamento.Arco, StatusItem.Forca), new Conversao(0, 0.20) },
                { (TipoEquipamento.Cajado, StatusItem.Intelecto), new Conversao(0, 0.20) },
                { (TipoEquipamento.Peito, StatusItem.Vigor), new Conversao(0, 250) },
                { (TipoEquipamento.Peito, StatusItem.DefesaFisica), new Conversao(0, 60) },
                { (TipoEquipamento.Peito, StatusItem.DefesaMagica), new Conversao(0, 60) },
                { (TipoEquipamento.Capacete, StatusItem.DefesaFisica), new Conversao(0, 50) },
                { (TipoEquipamento.Anel, StatusItem.ChanceCritica), new Conversao(0, 0.15) },
                { (TipoEquipamento.Brinco, StatusItem.DanoCritico), new Conversao(0, 0.75) },
            };

        /// <summary>
        /// Os atributos elegíveis da categoria (matriz de ATRIB-001): só eles têm faixa, entram no
        /// tier e valem no combate.
        /// </summary>
        public static StatusItem[] Elegiveis(TipoEquipamento tipo) =>
            Matriz.TryGetValue(tipo, out var lista)
                ? (StatusItem[])lista.Clone()
                : new StatusItem[0];

        public static bool Elegivel(TipoEquipamento tipo, StatusItem status) =>
            Matriz.TryGetValue(tipo, out var lista) && Array.IndexOf(lista, status) >= 0;

        /// <summary>Se o dano da arma da categoria é mágico (cajado); as demais armas são físicas.</summary>
        public static bool ArmaMagica(TipoEquipamento tipo) => tipo == TipoEquipamento.Cajado;

        /// <summary>Se a categoria é arma da mão principal (dá o dano-base do golpe).</summary>
        public static bool EhArma(TipoEquipamento tipo) =>
            tipo >= TipoEquipamento.Espada && tipo <= TipoEquipamento.Cajado;

        /// <summary>
        /// O valor de combate no 0 e no 100 do atributo na categoria, ou <c>null</c> se o atributo
        /// não tem conversão (legado, sem efeito).
        /// </summary>
        public static Conversao? De(TipoEquipamento tipo, StatusItem status)
        {
            if (PorCategoria.TryGetValue((tipo, status), out var propria))
            {
                return propria;
            }

            return Padrao.TryGetValue(status, out var padrao) ? padrao : (Conversao?)null;
        }

        /// <summary>
        /// O número de combate de <paramref name="valor"/> (0 a 100) do atributo na categoria. Zero
        /// se o atributo não é elegível na categoria ou não tem conversão (legado sem efeito).
        /// </summary>
        public static double Converter(TipoEquipamento tipo, StatusItem status, double valor)
        {
            var conversao = Elegivel(tipo, status) ? De(tipo, status) : null;
            return conversao?.Em(valor) ?? 0;
        }

        private static Dictionary<TipoEquipamento, StatusItem[]> CriarMatriz()
        {
            // "Principal da família" mais os atributos próprios da categoria.
            StatusItem[] Com(params StatusItem[] extras)
            {
                var lista = new List<StatusItem>(Principais);
                foreach (var extra in extras)
                {
                    if (!lista.Contains(extra))
                    {
                        lista.Add(extra);
                    }
                }

                return lista.ToArray();
            }

            return new Dictionary<TipoEquipamento, StatusItem[]>
            {
                {
                    TipoEquipamento.Espada,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.Forca,
                        StatusItem.ChanceCritica,
                        StatusItem.PenetracaoFisica,
                    }
                },
                {
                    TipoEquipamento.Martelo,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.Forca,
                        StatusItem.ChanceCritica,
                        StatusItem.PenetracaoFisica,
                    }
                },
                {
                    TipoEquipamento.Arco,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.Forca,
                        StatusItem.VelocidadeAtaqueFisico,
                        StatusItem.ChanceCritica,
                    }
                },
                {
                    TipoEquipamento.Cajado,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.Intelecto,
                        StatusItem.VelocidadeAtaqueMagico,
                        StatusItem.PenetracaoMagica,
                    }
                },
                {
                    TipoEquipamento.Escudo,
                    new[] { StatusItem.Vigor, StatusItem.DefesaFisica, StatusItem.DefesaMagica }
                },
                {
                    TipoEquipamento.Foco,
                    new[] { StatusItem.Intelecto, StatusItem.Espirito, StatusItem.RegeneracaoMana }
                },
                { TipoEquipamento.Capacete, Com(StatusItem.DefesaFisica, StatusItem.DefesaMagica) },
                { TipoEquipamento.Peito, Com(StatusItem.DefesaFisica, StatusItem.DefesaMagica) },
                { TipoEquipamento.Luvas, Com(StatusItem.DefesaFisica, StatusItem.ChanceCritica) },
                { TipoEquipamento.Calca, Com(StatusItem.DefesaFisica, StatusItem.RegeneracaoVida) },
                {
                    TipoEquipamento.Botas,
                    Com(StatusItem.DefesaFisica, StatusItem.RegeneracaoStamina)
                },
                {
                    TipoEquipamento.Cinto,
                    new[]
                    {
                        StatusItem.Vigor,
                        StatusItem.Agilidade,
                        StatusItem.Espirito,
                        StatusItem.RegeneracaoVida,
                        StatusItem.RegeneracaoStamina,
                        StatusItem.RegeneracaoMana,
                    }
                },
                {
                    TipoEquipamento.Anel,
                    Com(
                        StatusItem.ChanceCritica,
                        StatusItem.PenetracaoFisica,
                        StatusItem.PenetracaoMagica
                    )
                },
                {
                    TipoEquipamento.Brinco,
                    Com(
                        StatusItem.DanoCritico,
                        StatusItem.VelocidadeAtaqueFisico,
                        StatusItem.VelocidadeAtaqueMagico
                    )
                },
                {
                    TipoEquipamento.Adorno,
                    Com(StatusItem.DefesaMagica, StatusItem.RegeneracaoMana)
                },
            };
        }
    }

    /// <summary>O valor de combate no 0 e no 100 da escala do item; o meio é linear.</summary>
    public readonly struct Conversao
    {
        public readonly double No0;
        public readonly double No100;

        public Conversao(double no0, double no100)
        {
            No0 = no0;
            No100 = no100;
        }

        /// <summary>
        /// <c>no0 + (no100 - no0) x V / 100</c>, com V limitado à escala de
        /// <see cref="ConfigTier"/> (0 a 100).
        /// </summary>
        public double Em(double valor)
        {
            var limitado = Math.Min(
                Math.Max(valor, ConfigTier.EscalaMinima),
                ConfigTier.EscalaMaxima
            );
            var fracao =
                (limitado - ConfigTier.EscalaMinima)
                / (ConfigTier.EscalaMaxima - ConfigTier.EscalaMinima);
            return No0 + (No100 - No0) * fracao;
        }
    }
}
