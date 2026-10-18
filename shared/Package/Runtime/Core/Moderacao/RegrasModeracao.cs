#nullable enable
using System;

namespace JogoDoGrupo.Shared.Core.Moderacao
{
    /// <summary>
    /// Regras puras e limites de moderação e comandos de GM.
    /// </summary>
    public static class RegrasModeracao
    {
        public const int TamanhoMaximoMotivo = 200;
        public const int TamanhoMaximoDetalhes = 500;
        public const int MinimoMinutosSilencio = 1;
        public const int MaximoMinutosSilencio = 1440; // 24 horas
        public const int MinimoDiasBanimento = 1;
        public const int MaximoDiasBanimento = 365;

        public static readonly string[] ComandosValidos = new[]
        {
            "tp",
            "puxar",
            "silenciar",
            "expulsar",
            "banir",
            "ajuda",
        };

        public static readonly string[] MotivosDenunciaValidos = new[]
        {
            "ofensa",
            "trapaca",
            "spam",
            "comportamento",
            "outro",
        };

        public static bool EhComandoGmValido(string? comando)
        {
            if (string.IsNullOrWhiteSpace(comando))
            {
                return false;
            }

            var limpo = comando.Trim().ToLowerInvariant();
            for (var i = 0; i < ComandosValidos.Length; i++)
            {
                if (ComandosValidos[i] == limpo)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool EhMotivoDenunciaValido(string? motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return false;
            }

            var limpo = motivo.Trim().ToLowerInvariant();
            for (var i = 0; i < MotivosDenunciaValidos.Length; i++)
            {
                if (MotivosDenunciaValidos[i] == limpo)
                {
                    return true;
                }
            }

            return false;
        }

        public static int NormalizarMinutosSilencio(int minutos)
        {
            if (minutos < MinimoMinutosSilencio)
            {
                return 10; // Padrão de 10 minutos
            }

            return Math.Min(minutos, MaximoMinutosSilencio);
        }

        public static int NormalizarDiasBanimento(int dias)
        {
            if (dias < MinimoDiasBanimento)
            {
                return 1;
            }

            return Math.Min(dias, MaximoDiasBanimento);
        }
    }
}
