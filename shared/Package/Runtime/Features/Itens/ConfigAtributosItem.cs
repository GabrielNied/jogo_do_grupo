using System;
using System.Collections.Generic;

namespace JogoDoGrupo.Shared.Features.Itens
{
    /// <summary>
    /// A conversão do atributo do item (escala 0 a 100) para o número de combate (ATRIB-003) e a
    /// matriz de atributos elegíveis por categoria (ATRIB-001). Números propostos pela simulação da
    /// #627 (<c>tools/JogoDoGrupo.Simulacoes</c>); trocá-los é mexer só aqui.
    /// <para>
    /// A conversão é linear por (atributo, categoria): <c>combate = no0 + (no100 - no0) x V /
    /// 100</c>. Unidade: dano da arma, dano físico/mágico flat, vida e defesa em pontos; secundários
    /// na unidade do Combate.
    /// </para>
    /// </summary>
    public static class ConfigAtributosItem
    {
        // Principal de cada família na armadura (v3, #709).
        private static readonly StatusItem[] Principais =
        {
            StatusItem.VidaMaxima,
            StatusItem.DanoFisico,
            StatusItem.DanoMagico,
            StatusItem.RegeneracaoStamina,
        };

        private static readonly Dictionary<TipoEquipamento, StatusItem[]> Matriz = CriarMatriz();

        private static readonly Dictionary<StatusItem, Conversao> Padrao = new Dictionary<
            StatusItem,
            Conversao
        >
        {
            { StatusItem.DanoDaArma, new Conversao(20, 50) },
            { StatusItem.DanoFisico, new Conversao(0, 5) },
            { StatusItem.DanoMagico, new Conversao(0, 5) },
            { StatusItem.VidaMaxima, new Conversao(0, 100) },
            { StatusItem.DefesaFisica, new Conversao(0, 12) },
            { StatusItem.DefesaMagica, new Conversao(0, 12) },
            { StatusItem.RegeneracaoVida, new Conversao(0, 1.5) },
            { StatusItem.RegeneracaoStamina, new Conversao(0, 1.2) },
            { StatusItem.RegeneracaoMana, new Conversao(0, 0.8) },
            { StatusItem.ChanceCritica, new Conversao(0, 0.03) },
            { StatusItem.DanoCritico, new Conversao(0, 0.15) },
            { StatusItem.VelocidadeAtaqueFisico, new Conversao(0, 0.08) },
            { StatusItem.VelocidadeAtaqueMagico, new Conversao(0, 0.08) },
            { StatusItem.PenetracaoFisica, new Conversao(0, 0.06) },
            { StatusItem.PenetracaoMagica, new Conversao(0, 0.06) },
        };

        private static readonly Dictionary<(TipoEquipamento, StatusItem), Conversao> PorCategoria =
            new Dictionary<(TipoEquipamento, StatusItem), Conversao>
            {
                { (TipoEquipamento.Martelo, StatusItem.DanoDaArma), new Conversao(28, 72) },
                { (TipoEquipamento.Arco, StatusItem.DanoDaArma), new Conversao(16, 46) },
                { (TipoEquipamento.Espada, StatusItem.DanoFisico), new Conversao(0, 8) },
                { (TipoEquipamento.Martelo, StatusItem.DanoFisico), new Conversao(0, 10) },
                { (TipoEquipamento.Arco, StatusItem.DanoFisico), new Conversao(0, 8) },
                { (TipoEquipamento.Cajado, StatusItem.DanoMagico), new Conversao(0, 8) },
                { (TipoEquipamento.Peito, StatusItem.VidaMaxima), new Conversao(0, 160) },
                { (TipoEquipamento.Anel, StatusItem.VidaMaxima), new Conversao(0, 50) },
                { (TipoEquipamento.Brinco, StatusItem.VidaMaxima), new Conversao(0, 50) },
                { (TipoEquipamento.Adorno, StatusItem.VidaMaxima), new Conversao(0, 50) },
                { (TipoEquipamento.Peito, StatusItem.DefesaFisica), new Conversao(0, 18) },
                { (TipoEquipamento.Peito, StatusItem.DefesaMagica), new Conversao(0, 18) },
                { (TipoEquipamento.Capacete, StatusItem.DefesaFisica), new Conversao(0, 15) },
                { (TipoEquipamento.Escudo, StatusItem.DefesaFisica), new Conversao(0, 20) },
                { (TipoEquipamento.Escudo, StatusItem.DefesaMagica), new Conversao(0, 20) },
                { (TipoEquipamento.Anel, StatusItem.ChanceCritica), new Conversao(0, 0.06) },
                { (TipoEquipamento.Brinco, StatusItem.DanoCritico), new Conversao(0, 0.20) },
            };

        public static StatusItem[] Elegiveis(TipoEquipamento tipo) =>
            Matriz.TryGetValue(tipo, out var lista)
                ? (StatusItem[])lista.Clone()
                : new StatusItem[0];

        public static bool Elegivel(TipoEquipamento tipo, StatusItem status) =>
            Matriz.TryGetValue(tipo, out var lista) && Array.IndexOf(lista, status) >= 0;

        public static bool ArmaMagica(TipoEquipamento tipo) => tipo == TipoEquipamento.Cajado;

        public static bool EhArma(TipoEquipamento tipo) =>
            tipo >= TipoEquipamento.Espada && tipo <= TipoEquipamento.Cajado;

        public static Conversao? De(TipoEquipamento tipo, StatusItem status)
        {
            if (PorCategoria.TryGetValue((tipo, status), out var propria))
            {
                return propria;
            }

            return Padrao.TryGetValue(status, out var padrao) ? padrao : (Conversao?)null;
        }

        public static double Converter(TipoEquipamento tipo, StatusItem status, double valor)
        {
            var conversao = Elegivel(tipo, status) ? De(tipo, status) : null;
            return conversao?.Em(valor) ?? 0;
        }

        private static Dictionary<TipoEquipamento, StatusItem[]> CriarMatriz()
        {
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
                        StatusItem.DanoFisico,
                        StatusItem.ChanceCritica,
                        StatusItem.PenetracaoFisica,
                    }
                },
                {
                    TipoEquipamento.Martelo,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.DanoFisico,
                        StatusItem.ChanceCritica,
                        StatusItem.PenetracaoFisica,
                    }
                },
                {
                    TipoEquipamento.Arco,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.DanoFisico,
                        StatusItem.VelocidadeAtaqueFisico,
                        StatusItem.ChanceCritica,
                    }
                },
                {
                    TipoEquipamento.Cajado,
                    new[]
                    {
                        StatusItem.DanoDaArma,
                        StatusItem.DanoMagico,
                        StatusItem.VelocidadeAtaqueMagico,
                        StatusItem.PenetracaoMagica,
                    }
                },
                {
                    TipoEquipamento.Escudo,
                    new[]
                    {
                        StatusItem.VidaMaxima,
                        StatusItem.DefesaFisica,
                        StatusItem.DefesaMagica,
                    }
                },
                {
                    TipoEquipamento.Foco,
                    new[] { StatusItem.DanoMagico, StatusItem.RegeneracaoMana }
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
                        StatusItem.VidaMaxima,
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
