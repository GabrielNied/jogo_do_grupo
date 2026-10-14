using System;

namespace JogoDoGrupo.Shared.Features.Combate
{
    /// <summary>
    /// Regras puras e cálculos dos efeitos de status (#171, combate.md).
    /// </summary>
    public static class RegrasEfeitosDeStatus
    {
        /// <summary>Se o efeito acumula intensidade/stacks (DoT acumula até 3).</summary>
        public static bool PodeAcumular(TipoEfeitoDeStatus tipo)
        {
            return tipo == TipoEfeitoDeStatus.Sangramento || tipo == TipoEfeitoDeStatus.Queimadura;
        }

        /// <summary>Limite de acúmulos permitidos para o efeito.</summary>
        public static int MaximoAcumulos(TipoEfeitoDeStatus tipo)
        {
            return PodeAcumular(tipo) ? ConfigEfeitosDeStatus.MaximoAcumulosDoT : 1;
        }

        /// <summary>Duração padrão do efeito em segundos.</summary>
        public static double DuracaoPadraoSegundos(TipoEfeitoDeStatus tipo)
        {
            switch (tipo)
            {
                case TipoEfeitoDeStatus.Lentidao:
                    return ConfigEfeitosDeStatus.DuracaoLentidaoSegundos;
                case TipoEfeitoDeStatus.Atordoado:
                    return ConfigEfeitosDeStatus.DuracaoAtordoadoSegundos;
                case TipoEfeitoDeStatus.Sangramento:
                    return ConfigEfeitosDeStatus.DuracaoSangramentoSegundos;
                case TipoEfeitoDeStatus.Queimadura:
                    return ConfigEfeitosDeStatus.DuracaoQueimaduraSegundos;
                case TipoEfeitoDeStatus.Vulneravel:
                    return ConfigEfeitosDeStatus.DuracaoVulneravelSegundos;
                case TipoEfeitoDeStatus.Fortalecido:
                    return ConfigEfeitosDeStatus.DuracaoFortalecidoSegundos;
                default:
                    return 5.0;
            }
        }

        /// <summary>Modificador de velocidade do personagem (0,70 se tiver lentidão, 1,0 normal).</summary>
        public static double ModificadorVelocidade(bool temLentidao)
        {
            return temLentidao ? (1.0 - ConfigEfeitosDeStatus.PorcentagemLentidao) : 1.0;
        }

        /// <summary>Multiplicador de dano causado pelo atacante (1,15 se fortalecido, 1,0 normal).</summary>
        public static double MultiplicadorDanoCausado(bool temFortalecido)
        {
            return temFortalecido ? (1.0 + ConfigEfeitosDeStatus.PorcentagemFortalecido) : 1.0;
        }

        /// <summary>Multiplicador de dano recebido pelo defensor (1,15 se vulnerável, 1,0 normal).</summary>
        public static double MultiplicadorDanoRecebido(bool temVulneravel)
        {
            return temVulneravel ? (1.0 + ConfigEfeitosDeStatus.PorcentagemVulneravel) : 1.0;
        }

        /// <summary>Dano por segundo do DoT, multiplicado pelo número de acúmulos (1 a 3).</summary>
        public static int CalcularDanoDoT(TipoEfeitoDeStatus tipo, int acumulos)
        {
            var clamped = Math.Max(1, Math.Min(ConfigEfeitosDeStatus.MaximoAcumulosDoT, acumulos));
            if (tipo == TipoEfeitoDeStatus.Sangramento)
            {
                return ConfigEfeitosDeStatus.DanoPorSegundoSangramento * clamped;
            }
            if (tipo == TipoEfeitoDeStatus.Queimadura)
            {
                return ConfigEfeitosDeStatus.DanoPorSegundoQueimadura * clamped;
            }
            return 0;
        }

        /// <summary>
        /// Processa o acúmulo da barra de quebra de chefes quando recebem atordoamento.
        /// Ao atingir a capacidade (100), reseta a barra para 0 e ativa quebra/vulnerabilidade.
        /// </summary>
        public static void ProcessarQuebraBoss(
            int valorAtual,
            int incremento,
            out int novoValor,
            out bool quebrou
        )
        {
            var total = valorAtual + incremento;
            if (total >= ConfigEfeitosDeStatus.CapacidadeBarraDeQuebraBoss)
            {
                novoValor = 0;
                quebrou = true;
            }
            else
            {
                novoValor = total;
                quebrou = false;
            }
        }

        /// <summary>Nome amigável do status para interface ou logs.</summary>
        public static string NomeDoEfeito(TipoEfeitoDeStatus tipo)
        {
            switch (tipo)
            {
                case TipoEfeitoDeStatus.Lentidao:
                    return "Lentidão";
                case TipoEfeitoDeStatus.Atordoado:
                    return "Atordoado";
                case TipoEfeitoDeStatus.Sangramento:
                    return "Sangramento";
                case TipoEfeitoDeStatus.Queimadura:
                    return "Queimadura";
                case TipoEfeitoDeStatus.Vulneravel:
                    return "Vulnerável";
                case TipoEfeitoDeStatus.Fortalecido:
                    return "Fortalecido";
                default:
                    return tipo.ToString();
            }
        }
    }
}
