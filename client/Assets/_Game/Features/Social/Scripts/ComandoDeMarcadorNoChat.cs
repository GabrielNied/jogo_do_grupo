using System;
using System.Globalization;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Comandos /marcar e /desmarcar no chat para definir marcadores táticos de grupo sobre alvos.
    /// </summary>
    internal static class ComandoDeMarcadorNoChat
    {
        public const string VerboMarcar = "marcar";
        public const string VerboDesmarcar = "desmarcar";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            ComandosDeBarraNoChat.Registrar(
                VerboMarcar,
                ExecutarMarcar,
                "/marcar <caveira|x|escudo|lua|estrela> [id] — define marcador tático no alvo"
            );

            ComandosDeBarraNoChat.Registrar(
                VerboDesmarcar,
                ExecutarDesmarcar,
                "/desmarcar [id] — remove o marcador tático do alvo"
            );
        }

        internal static TipoMarcadorDeAlvo ConverterMarcador(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
            {
                return TipoMarcadorDeAlvo.Nenhum;
            }

            switch (nome.Trim().ToLowerInvariant())
            {
                case "caveira":
                case "skull":
                case "1":
                    return TipoMarcadorDeAlvo.Caveira;
                case "x":
                case "cruz":
                case "cross":
                case "2":
                    return TipoMarcadorDeAlvo.Cruz;
                case "escudo":
                case "shield":
                case "3":
                    return TipoMarcadorDeAlvo.Escudo;
                case "lua":
                case "moon":
                case "4":
                    return TipoMarcadorDeAlvo.Lua;
                case "estrela":
                case "star":
                case "5":
                    return TipoMarcadorDeAlvo.Estrela;
                default:
                    return TipoMarcadorDeAlvo.Nenhum;
            }
        }

        internal static bool InterpretarMarcar(
            string resto,
            out TipoMarcadorDeAlvo marcador,
            out long alvoId
        )
        {
            marcador = TipoMarcadorDeAlvo.Nenhum;
            alvoId = 0;

            var partes = (resto ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 0)
            {
                return false;
            }

            marcador = ConverterMarcador(partes[0]);
            if (marcador == TipoMarcadorDeAlvo.Nenhum)
            {
                return false;
            }

            if (
                partes.Length >= 2
                && long.TryParse(
                    partes[1],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var id
                )
            )
            {
                alvoId = id;
            }

            return true;
        }

        internal static bool InterpretarDesmarcar(string resto, out long alvoId)
        {
            alvoId = 0;
            var partes = (resto ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (
                partes.Length >= 1
                && long.TryParse(
                    partes[0],
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var id
                )
            )
            {
                alvoId = id;
                return true;
            }

            return false;
        }

        internal static long EncontrarAlvoMaisProximo(
            Vector3 posicaoOrigem,
            float alcanceMaximo = 50f
        )
        {
            long melhorId = 0;
            float menorDistanciaSq = alcanceMaximo * alcanceMaximo;

            var objetos = UnityEngine.Object.FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude
            );
            foreach (var t in objetos)
            {
                if (t == null)
                {
                    continue;
                }

                if (t.name.StartsWith("Inimigo ") || t.name.StartsWith("Inimigo_"))
                {
                    var partes = t.name.Split(
                        new[] { ' ', '_' },
                        StringSplitOptions.RemoveEmptyEntries
                    );
                    if (
                        partes.Length >= 2
                        && long.TryParse(
                            partes[1],
                            NumberStyles.Integer,
                            CultureInfo.InvariantCulture,
                            out var id
                        )
                    )
                    {
                        float distSq = (t.position - posicaoOrigem).sqrMagnitude;
                        if (distSq < menorDistanciaSq)
                        {
                            menorDistanciaSq = distSq;
                            melhorId = id;
                        }
                    }
                }
            }

            return melhorId;
        }

        private static ResultadoDeComandoDeBarra ExecutarMarcar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (!InterpretarMarcar(resto, out var marcador, out var alvoId))
            {
                contexto.MostrarSistema("Use /marcar <caveira|x|escudo|lua|estrela> [id]");
                return new ResultadoDeComandoDeBarra();
            }

            if (alvoId <= 0)
            {
                var origem =
                    EntradaNoJogo.Jogador != null
                        ? EntradaNoJogo.Jogador.transform.position
                        : Vector3.zero;
                alvoId = EncontrarAlvoMaisProximo(origem);
            }

            if (alvoId <= 0)
            {
                contexto.MostrarSistema("Nenhum alvo especificado ou próximo para marcar.");
                return new ResultadoDeComandoDeBarra();
            }

            EstadoMarcadoresTaticos.Instancia.PedirDefinirMarcador(alvoId, marcador);
            return new ResultadoDeComandoDeBarra();
        }

        private static ResultadoDeComandoDeBarra ExecutarDesmarcar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (!InterpretarDesmarcar(resto, out var alvoId))
            {
                var origem =
                    EntradaNoJogo.Jogador != null
                        ? EntradaNoJogo.Jogador.transform.position
                        : Vector3.zero;
                alvoId = EncontrarAlvoMaisProximo(origem);
            }

            if (alvoId <= 0)
            {
                contexto.MostrarSistema("Nenhum alvo especificado ou próximo para desmarcar.");
                return new ResultadoDeComandoDeBarra();
            }

            EstadoMarcadoresTaticos.Instancia.PedirDefinirMarcador(
                alvoId,
                TipoMarcadorDeAlvo.Nenhum
            );
            return new ResultadoDeComandoDeBarra();
        }
    }
}
