using System;
using System.Globalization;

namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>O que o texto do console pediu, depois de lido.</summary>
    public enum TipoDeComandoDeTeste
    {
        Ajuda = 0,
        Experiencia = 1,
        Teleporte = 2,
        Spawn = 3,
        Invalido = 4,
    }

    /// <summary>Resultado de <see cref="ComandosDeTeste.Interpretar"/>. Os números só valem no tipo correspondente.</summary>
    [System.Serializable]
    public class ComandoDeTesteInterpretado
    {
        public TipoDeComandoDeTeste Tipo = TipoDeComandoDeTeste.Invalido;
        public long Quantidade = 0;
        public float X = 0f;
        public float Y = 0f;
        public float Z = 0f;
        public string Aviso = "";
    }

    /// <summary>
    /// Lê o texto do console de teste. Não muda o jogo: quem decide é o servidor, e só com a
    /// chave de desenvolvimento. Números no formato invariante (ponto decimal).
    /// </summary>
    public static class ComandosDeTeste
    {
        /// <summary>Módulo da quantidade de <c>xp</c>, para os dois lados.</summary>
        public const long ExperienciaMaxima = 1000000;

        /// <summary>Tamanho máximo do texto, para não aceitar um pacote enorme.</summary>
        public const int TextoMaximo = 80;

        /// <summary>O que o comando <c>ajuda</c> responde (texto sem barra, usado no servidor).</summary>
        public const string Ajuda = "xp <quantidade> | tp <x> <y> <z> | spawn";

        /// <summary>Lista para o chat com barra (<c>/comandos</c> no client).</summary>
        public const string AjudaComBarra = "/xp <quantidade> | /tp <x> <y> <z> | /spawn | /ajuda";

        /// <summary>Texto vazio ou <c>ajuda</c> pede a lista. O resto vira um comando ou um aviso.</summary>
        public static ComandoDeTesteInterpretado Interpretar(string texto)
        {
            if (texto != null && texto.Length > TextoMaximo)
            {
                return Invalido("Texto longo demais.");
            }

            if (string.IsNullOrWhiteSpace(texto))
            {
                return new ComandoDeTesteInterpretado { Tipo = TipoDeComandoDeTeste.Ajuda };
            }

            var partes = texto
                .Trim()
                .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            var verbo = partes[0].ToLowerInvariant();
            if (verbo == "ajuda" || verbo == "help" || verbo == "?")
            {
                return partes.Length == 1
                    ? new ComandoDeTesteInterpretado { Tipo = TipoDeComandoDeTeste.Ajuda }
                    : Invalido("Comando não reconhecido. " + Ajuda);
            }

            if (verbo == "xp")
            {
                return Experiencia(partes);
            }

            if (verbo == "tp")
            {
                return Teleporte(partes);
            }

            if (verbo == "spawn")
            {
                return partes.Length == 1
                    ? new ComandoDeTesteInterpretado { Tipo = TipoDeComandoDeTeste.Spawn }
                    : Invalido("Use spawn, sem números.");
            }

            return Invalido("Comando não reconhecido. " + Ajuda);
        }

        private static ComandoDeTesteInterpretado Experiencia(string[] partes)
        {
            long quantidade;
            if (
                partes.Length != 2
                || !long.TryParse(
                    partes[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out quantidade
                )
                || quantidade == 0
                || quantidade > ExperienciaMaxima
                || quantidade < -ExperienciaMaxima
            )
            {
                return Invalido(
                    "Use xp <quantidade>, de -"
                        + ExperienciaMaxima
                        + " a "
                        + ExperienciaMaxima
                        + ", sem ser zero."
                );
            }

            return new ComandoDeTesteInterpretado
            {
                Tipo = TipoDeComandoDeTeste.Experiencia,
                Quantidade = quantidade,
            };
        }

        private static ComandoDeTesteInterpretado Teleporte(string[] partes)
        {
            float x;
            float y;
            float z;
            if (
                partes.Length != 4
                || !Numero(partes[1], out x)
                || !Numero(partes[2], out y)
                || !Numero(partes[3], out z)
            )
            {
                return Invalido("Use tp <x> <y> <z>, com ponto decimal.");
            }

            return new ComandoDeTesteInterpretado
            {
                Tipo = TipoDeComandoDeTeste.Teleporte,
                X = x,
                Y = y,
                Z = z,
            };
        }

        private static bool Numero(string texto, out float valor)
        {
            return float.TryParse(
                    texto,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out valor
                ) && float.IsFinite(valor);
        }

        private static ComandoDeTesteInterpretado Invalido(string aviso)
        {
            return new ComandoDeTesteInterpretado
            {
                Tipo = TipoDeComandoDeTeste.Invalido,
                Aviso = aviso,
            };
        }
    }
}
