using System;
using JogoDoGrupo.Shared.Features.Combate;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>Um efeito de status pronto para exibição na interface.</summary>
    internal readonly struct ItemDeStatus
    {
        public ItemDeStatus(
            TipoEfeitoDeStatus tipo,
            int acumulos,
            float duracaoRestante,
            float duracaoTotal
        )
        {
            Tipo = tipo;
            Acumulos = acumulos < 1 ? 1 : acumulos;
            DuracaoRestante = duracaoRestante < 0f ? 0f : duracaoRestante;
            DuracaoTotal = duracaoTotal <= 0f ? 1f : duracaoTotal;
        }

        public TipoEfeitoDeStatus Tipo { get; }
        public int Acumulos { get; }
        public float DuracaoRestante { get; }
        public float DuracaoTotal { get; }

        public float Fracao => Math.Min(1f, DuracaoRestante / DuracaoTotal);

        public string Nome => RegrasEfeitosDeStatus.NomeDoEfeito(Tipo);

        public string TextoDescritivo
        {
            get
            {
                var sufixo = Acumulos > 1 ? " x" + Acumulos : "";
                var tempo =
                    DuracaoRestante.ToString(
                        "0.0",
                        System.Globalization.CultureInfo.InvariantCulture
                    ) + "s";
                return Nome + sufixo + " (" + tempo + ")";
            }
        }
    }

    /// <summary>Uma barra de quebra de chefe formatada para a interface.</summary>
    internal readonly struct ItemBarraDeQuebra
    {
        public ItemBarraDeQuebra(int atual, int capacidadeMax, bool quebrado)
        {
            Atual = Math.Max(0, atual);
            CapacidadeMax = Math.Max(1, capacidadeMax);
            Quebrado = quebrado;
        }

        public int Atual { get; }
        public int CapacidadeMax { get; }
        public bool Quebrado { get; }

        public float Fracao => Math.Min(1f, (float)Atual / CapacidadeMax);

        public string Texto => Quebrado ? "VULNERÁVEL" : "Quebra " + Atual + " / " + CapacidadeMax;
    }

    /// <summary>
    /// Formatação dos efeitos de status e barra de quebra para apresentação visual no client.
    /// </summary>
    internal static class ApresentacaoEfeitosDeStatus
    {
        public static ItemDeStatus MontarItem(InstanciaEfeitoDeStatus instancia)
        {
            return new ItemDeStatus(
                instancia.Tipo,
                instancia.Acumulos,
                instancia.DuracaoRestanteSegundos,
                instancia.DuracaoTotalSegundos
            );
        }

        public static ItemBarraDeQuebra MontarQuebra(BarraDeQuebraMudou msg)
        {
            if (msg == null)
            {
                return new ItemBarraDeQuebra(0, 100, false);
            }
            return new ItemBarraDeQuebra(msg.ValorAtual, msg.CapacidadeMax, msg.Quebrado);
        }
    }
}
