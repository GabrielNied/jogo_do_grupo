using System.Collections.Generic;
using JogoDoGrupo.Shared.Features.Itens;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>Como uma linha do tooltip é colorida.</summary>
    public enum EstiloLinha
    {
        Normal,
        Fixo,
        Sorteado,
        Ganho,
        Perda,
        Suave,
    }

    public sealed class LinhaTooltip
    {
        public string Texto;
        public EstiloLinha Estilo;
    }

    /// <summary>O que o tooltip de um item mostra, já formatado (sem Unity, para teste).</summary>
    public sealed class DadosTooltip
    {
        public string Nome = "";

        /// <summary>Só os itens que são equipamento têm tier.</summary>
        public TierItem? Tier;

        public readonly List<LinhaTooltip> Linhas = new List<LinhaTooltip>();

        /// <summary>Título da comparação ("Equipado: ..."), ou <c>null</c> sem comparação.</summary>
        public string TituloComparacao;

        public readonly List<LinhaTooltip> Comparacao = new List<LinhaTooltip>();

        /// <summary>Linha suave acima do nome (ex.: "Equipado · Capacete"), ou <c>null</c>.</summary>
        public string Cabecalho;

        /// <summary>
        /// A peça equipada no mesmo encaixe, para o segundo tooltip ao lado; <c>null</c> sem peça
        /// equipada ou sem comparação.
        /// </summary>
        public DadosTooltip Equipado;
    }

    /// <summary>
    /// Monta o tooltip: nome, tier, grau, família, nível/máximo, requisito, atributos
    /// (fixos e sorteados) e nível de fusão. Para peça da bolsa, compara com a equipada no
    /// encaixe automático (▲ ganho, ▼ perda).
    /// </summary>
    public static class TextosTooltip
    {
        public const string SetaGanho = "▲";
        public const string SetaPerda = "▼";

        /// <param name="estado">
        /// Estado do inventário, para comparar. <c>null</c> (ou item equipado) não compara.
        /// </param>
        public static DadosTooltip Montar(EntradaInventario entrada, EquipamentosResponse estado)
        {
            var dados = Basico(entrada);
            var item = RegrasEquipamentos.DadosDe(entrada);
            if (item != null && estado != null && entrada.Posicao == PosicaoEquipamento.Inventario)
            {
                Comparar(dados, item, estado);
                AvisarRecusa(dados, item, estado);
            }
            return dados;
        }

        private static DadosTooltip Basico(EntradaInventario entrada)
        {
            var dados = new DadosTooltip { Nome = entrada.Nome };
            var item = RegrasEquipamentos.DadosDe(entrada);
            if (item == null)
                return dados;

            dados.Nome = item.Nome;
            dados.Tier = RegrasTier.TierDe(item);
            var tipo =
                TextosEquipamento.NomeDoTipo(item.Tipo) + (item.DuasMaos ? " · duas mãos" : "");
            Adicionar(
                dados.Linhas,
                TextosEquipamento.NomeDoTier(item) + " · " + tipo,
                EstiloLinha.Suave
            );
            Adicionar(
                dados.Linhas,
                "Grau "
                    + item.Grau
                    + " · nível "
                    + item.Nivel
                    + "/"
                    + RegrasEquipamentos.NivelMaximo(item.Raridade),
                EstiloLinha.Normal
            );
            if (!string.IsNullOrEmpty(item.Familia))
            {
                Adicionar(
                    dados.Linhas,
                    "Família " + item.Familia + " · variante " + item.Variacao,
                    EstiloLinha.Normal
                );
            }
            Adicionar(
                dados.Linhas,
                "Requer personagem nível " + item.NivelPersonagemMinimo,
                EstiloLinha.Suave
            );
            for (var i = 0; i < item.Atributos.Length; i++)
            {
                var atributo = item.Atributos[i];
                Adicionar(
                    dados.Linhas,
                    TextosEquipamento.LinhaDoAtributo(item, i),
                    atributo.Fixo ? EstiloLinha.Fixo : EstiloLinha.Sorteado
                );
            }
            Adicionar(
                dados.Linhas,
                "Fusão +" + item.NivelFusao + "/" + ConfigFusao.NivelMaximo,
                EstiloLinha.Suave
            );

            return dados;
        }

        // Mostra antes do clique o motivo de o servidor recusar (nível, mão secundária com arma
        // de duas mãos), em vermelho, no encaixe que o clique direito usaria.
        private static void AvisarRecusa(
            DadosTooltip dados,
            Equipamento item,
            EquipamentosResponse estado
        )
        {
            var encaixe = LogicaInventario.EncaixeAutomatico(item.Tipo, estado);
            var motivo = TextosEquipamento.MotivoDaRecusa(
                LogicaInventario.RecusaNoEncaixe(item, encaixe, estado),
                item
            );
            if (motivo != null)
                Adicionar(dados.Linhas, motivo, EstiloLinha.Perda);
        }

        private static void Comparar(
            DadosTooltip dados,
            Equipamento item,
            EquipamentosResponse estado
        )
        {
            var encaixe = LogicaInventario.EncaixeAutomatico(item.Tipo, estado);
            var equipado = LogicaInventario.NoEncaixe(estado, encaixe);
            var atual = RegrasEquipamentos.DadosDe(equipado);
            if (atual != null)
            {
                dados.Equipado = Basico(equipado);
                dados.Equipado.Cabecalho = "Equipado · " + TextosEquipamento.NomePosicao(encaixe);
            }
            dados.TituloComparacao =
                atual == null
                    ? TextosEquipamento.NomePosicao(encaixe) + ": vazio"
                    : "Equipado (" + TextosEquipamento.NomePosicao(encaixe) + "): " + atual.Nome;

            var novos = Somar(item);
            var antigos = atual == null ? new Dictionary<StatusItem, double>() : Somar(atual);
            foreach (StatusItem status in System.Enum.GetValues(typeof(StatusItem)))
            {
                novos.TryGetValue(status, out var novo);
                antigos.TryGetValue(status, out var antigo);
                var delta = novo - antigo;
                if (System.Math.Abs(delta) < 0.005)
                    continue;
                var ganho = delta > 0;
                Adicionar(
                    dados.Comparacao,
                    (ganho ? SetaGanho : SetaPerda)
                        + " "
                        + TextosEquipamento.NomeStatus(status)
                        + " "
                        + (ganho ? "+" : "-")
                        + TextosEquipamento.Numero(System.Math.Abs(delta)),
                    ganho ? EstiloLinha.Ganho : EstiloLinha.Perda
                );
            }

            if (dados.Comparacao.Count == 0)
                Adicionar(
                    dados.Comparacao,
                    "Mesmos atributos da peça equipada.",
                    EstiloLinha.Suave
                );
        }

        private static Dictionary<StatusItem, double> Somar(Equipamento item)
        {
            var total = new Dictionary<StatusItem, double>();
            foreach (var atributo in item.Atributos)
            {
                total.TryGetValue(atributo.Status, out var atual);
                total[atributo.Status] = atual + atributo.Valor;
            }
            return total;
        }

        private static void Adicionar(List<LinhaTooltip> linhas, string texto, EstiloLinha estilo)
        {
            linhas.Add(new LinhaTooltip { Texto = texto, Estilo = estilo });
        }
    }
}
