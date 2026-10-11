using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Shared.Features.Personagem;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Textos das telas de Personagem mostrados à pessoa (mensagens de
    /// <c>docs/features/personagem.md</c>).
    /// </summary>
    internal static class TextosPersonagem
    {
        public const string Generica = "Algo deu errado. Tente de novo.";

        public const string SemConexao =
            "Não foi possível conectar ao servidor. Verifique a conexão e tente de novo.";

        public const string ErroAoCarregar = "Não foi possível carregar os personagens.";

        // Sem uso desde 2026-10-11: o botão Entrar passou a chamar EntradaNoJogo.Entrar.
        public const string JogoNaoImplementado = "O jogo ainda não foi implementado.";

        public const string CarregandoFicha = "Carregando personagem...";

        public const string ErroAoCarregarFicha = "Não foi possível carregar o personagem.";

        public const string NotaDaFicha =
            "Atributos e equipamentos aparecem aqui quando forem implementados.";

        public static readonly string NomeInvalido =
            $"O nome deve ter de {RegrasPersonagem.NomeMinimo} a {RegrasPersonagem.NomeMaximo} "
            + "letras (A–Z, sem acento, espaço ou número).";

        public static readonly string LimiteAtingido =
            $"Você já tem o máximo de {RegrasPersonagem.MaximoPorConta} personagens.";

        // Códigos de CodigosErroCliente (Core) comparados pelo valor, para não depender de
        // símbolos que podem ainda não existir no Core desta versão.
        private const string CodigoHttpBloqueado = "HttpBloqueado";
        private const string CodigoCancelada = "Cancelada";

        /// <summary>
        /// Se a requisição foi cancelada pelo próprio client (ex.: ao sair do Play):
        /// não há nada para mostrar.
        /// </summary>
        public static bool FoiCancelada(string codigo)
        {
            return codigo == CodigoCancelada;
        }

        /// <summary>Nome da raça em português, para mostrar na interface.</summary>
        public static string NomeDaRaca(Raca raca)
        {
            return raca switch
            {
                Raca.Valdren => "Valdren",
                Raca.Aurideo => "Aurídeos",
                Raca.Granitico => "Graníticos",
                Raca.Cinereo => "Cinéreos",
                Raca.Silvano => "Silvanos",
                // Raça nova no enum ainda sem tradução: mostra o nome do código.
                _ => raca.ToString(),
            };
        }

        /// <summary>
        /// O personagem com esse Id na lista do servidor, ou <c>null</c> (lista nula também).
        /// </summary>
        public static PersonagemResumo ProcurarPorId(PersonagemResumo[] personagens, long id)
        {
            if (personagens == null)
            {
                return null;
            }

            foreach (var personagem in personagens)
            {
                if (personagem != null && personagem.Id == id)
                {
                    return personagem;
                }
            }

            return null;
        }

        /// <summary>
        /// Linhas "rótulo, valor" da janela do personagem (tecla C), na ordem em que aparecem.
        /// </summary>
        public static List<KeyValuePair<string, string>> LinhasDaFicha(PersonagemResumo personagem)
        {
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Nome", personagem.Nome),
                new KeyValuePair<string, string>("Raça", NomeDaRaca(personagem.Raca)),
                new KeyValuePair<string, string>("Nível", personagem.Nivel.ToString()),
            };
        }

        /// <summary>Mensagem para a recusa do <c>POST</c> de criação.</summary>
        public static string MensagemDaRecusa(string codigo)
        {
            return codigo switch
            {
                CodigosErroPersonagem.NomeInvalido => NomeInvalido,
                CodigosErroPersonagem.NomeJaExiste => "Esse nome já está em uso.",
                CodigosErroPersonagem.RacaInvalida => "Raça inválida.",
                CodigosErroPersonagem.LimitePersonagens => LimiteAtingido,
                CodigosErroCliente.SemConexao => SemConexao,
                CodigoHttpBloqueado => SemConexao,
                _ => Generica,
            };
        }
    }
}
