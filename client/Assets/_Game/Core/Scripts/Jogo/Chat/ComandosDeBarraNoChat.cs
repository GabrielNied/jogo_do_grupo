using System;
using System.Collections.Generic;
using System.Linq;

namespace JogoDoGrupo.Client.Core.Jogo.Chat
{
    /// <summary>Resultado de um comando registrado por outra funcionalidade.</summary>
    public sealed class ResultadoDeComandoDeBarra
    {
        public bool FecharCaixa { get; set; } = true;
        public bool ManterAberto { get; set; }
    }

    /// <summary>Callbacks que o chat oferece ao executar um comando registrado.</summary>
    public sealed class ContextoDeComandoDeBarra
    {
        public Action<string> MostrarSistema { get; set; } = _ => { };
    }

    public delegate ResultadoDeComandoDeBarra ManipuladorDeComandoDeBarra(
        string comando,
        string resto,
        ContextoDeComandoDeBarra contexto
    );

    /// <summary>
    /// Comandos com barra além dos do Social. Cada funcionalidade registra verbos próprios
    /// (ex.: Desenvolvimento registra <c>xp</c>, <c>tp</c>, <c>spawn</c>).
    /// </summary>
    public static class ComandosDeBarraNoChat
    {
        private sealed class Entrada
        {
            public ManipuladorDeComandoDeBarra Manipulador;
            public string LinhaNaLista = "";
        }

        private static readonly Dictionary<string, Entrada> _porComando = new Dictionary<
            string,
            Entrada
        >(StringComparer.OrdinalIgnoreCase);

        private static readonly object _trava = new object();

        public static void Registrar(
            string comando,
            ManipuladorDeComandoDeBarra manipulador,
            string linhaNaLista
        )
        {
            if (string.IsNullOrWhiteSpace(comando))
            {
                throw new ArgumentException("Comando vazio.", nameof(comando));
            }

            if (manipulador == null)
            {
                throw new ArgumentNullException(nameof(manipulador));
            }

            var chave = comando.Trim().ToLowerInvariant();
            lock (_trava)
            {
                _porComando[chave] = new Entrada
                {
                    Manipulador = manipulador,
                    LinhaNaLista = string.IsNullOrWhiteSpace(linhaNaLista)
                        ? $"/{chave}"
                        : linhaNaLista.Trim(),
                };
            }
        }

        public static bool Conhece(string comando)
        {
            if (string.IsNullOrWhiteSpace(comando))
            {
                return false;
            }

            var chave = comando.Trim().ToLowerInvariant();
            lock (_trava)
            {
                return _porComando.ContainsKey(chave);
            }
        }

        public static ResultadoDeComandoDeBarra Executar(
            string comando,
            string resto,
            ContextoDeComandoDeBarra contexto
        )
        {
            if (string.IsNullOrWhiteSpace(comando))
            {
                return null;
            }

            var chave = comando.Trim().ToLowerInvariant();
            Entrada entrada;
            lock (_trava)
            {
                if (!_porComando.TryGetValue(chave, out entrada))
                {
                    return null;
                }
            }

            return entrada.Manipulador(chave, resto ?? "", contexto);
        }

        /// <summary>Linhas extras para <c>/comandos</c> (comandos registrados por features).</summary>
        public static IReadOnlyList<string> LinhasDoCatalogo()
        {
            lock (_trava)
            {
                return _porComando.Values.Select(e => e.LinhaNaLista).OrderBy(l => l).ToList();
            }
        }

        /// <summary>Nomes de comando (sem barra) para autocomplete quando a linha ainda não tem espaço.</summary>
        public static IReadOnlyList<string> NomesParaAutocomplete()
        {
            lock (_trava)
            {
                return _porComando.Keys.OrderBy(c => c).ToList();
            }
        }

        /// <summary>Só para testes EditMode; não use no jogo.</summary>
        public static void LimparParaTeste()
        {
            lock (_trava)
            {
                _porComando.Clear();
            }
        }
    }
}
