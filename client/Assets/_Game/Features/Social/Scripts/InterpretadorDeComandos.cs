using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Shared.Features.Social;

namespace JogoDoGrupo.Client.Features.Social
{
    internal enum AcaoDoComando
    {
        /// <summary>Nada a fazer (texto vazio).</summary>
        Nada,

        /// <summary>Enviar <see cref="ResultadoDoComando.Texto"/> ao servidor.</summary>
        Enviar,

        /// <summary>Só troca o canal em que a pessoa fala (ex.: <c>/g</c> sozinho).</summary>
        MudarCanal,
        Ignorar,
        Designorar,

        /// <summary>Não enviar; mostrar <see cref="ResultadoDoComando.Mensagem"/> no chat.</summary>
        Erro,

        /// <summary>Linha de Sistema informativa (ex.: <c>/comandos</c>).</summary>
        Informativo,

        /// <summary>Comando registrado em <see cref="ComandosDeBarraNoChat"/>.</summary>
        Registrado,
    }

    internal sealed class ResultadoDoComando
    {
        public AcaoDoComando Acao { get; set; }
        public CanalDeChat Canal { get; set; }

        /// <summary>Sussurro: o nome de quem recebe. Ignorar e designorar: o nome alvo.</summary>
        public string Nome { get; set; } = "";

        /// <summary>Texto limpo, pronto para enviar.</summary>
        public string Texto { get; set; } = "";

        /// <summary>Erro: o que mostrar à pessoa.</summary>
        public string Mensagem { get; set; } = "";

        /// <summary>Erro que deixa a caixa aberta com o texto, para a pessoa corrigir.</summary>
        public bool ManterAberto { get; set; }

        /// <summary>Com <see cref="AcaoDoComando.Registrado"/>.</summary>
        public string ComandoBarra { get; set; } = "";

        /// <summary>Com <see cref="AcaoDoComando.Registrado"/>; o que veio depois do verbo.</summary>
        public string RestoBarra { get; set; } = "";
    }

    /// <summary>
    /// O que o chat lembra entre uma mensagem e outra: o último canal usado (o primeiro é a
    /// Cidade), com quem foi o último sussurro enviado e quem sussurrou por último para você.
    /// </summary>
    internal sealed class EstadoDoEnvio
    {
        public CanalDeChat UltimoCanal { get; set; } = CanalDeChat.Cidade;
        public string UltimoAlvoDeSussurro { get; set; } = "";
        public string UltimoSussurroRecebido { get; set; } = "";

        /// <summary>Passa a falar no canal do resultado (envio ou troca de canal).</summary>
        public void Registrar(ResultadoDoComando resultado)
        {
            if (
                resultado.Acao != AcaoDoComando.Enviar
                && resultado.Acao != AcaoDoComando.MudarCanal
            )
            {
                return;
            }

            UltimoCanal = resultado.Canal;
            if (resultado.Canal == CanalDeChat.Sussurro)
            {
                UltimoAlvoDeSussurro = resultado.Nome;
            }
        }
    }

    /// <summary>
    /// Lê o que a pessoa digitou: texto sem barra vai ao último canal; <c>/c</c>, <c>/g</c>,
    /// <c>/s Nome texto</c> (também <c>/w</c>), <c>/r texto</c>, <c>/ignorar Nome</c> e
    /// <c>/designorar Nome</c> fazem o que dizem. Comando desconhecido nunca é enviado ao
    /// servidor. Só decide o que fazer; quem executa é o <see cref="ServicoDeChat"/>.
    /// </summary>
    internal static class InterpretadorDeComandos
    {
        public static ResultadoDoComando Interpretar(string entrada, EstadoDoEnvio estado)
        {
            var texto = RegrasChat.Limpar(entrada);
            if (texto.Length == 0)
            {
                return new ResultadoDoComando { Acao = AcaoDoComando.Nada };
            }

            if (texto[0] != '/')
            {
                return SemComando(texto, estado);
            }

            Separar(texto.Substring(1), out var comando, out var resto);
            switch (comando.ToLowerInvariant())
            {
                case "c":
                case "cidade":
                    return NoCanal(CanalDeChat.Cidade, "", resto);
                case "g":
                case "grupo":
                    return NoCanal(CanalDeChat.Grupo, "", resto);
                case "s":
                case "w":
                case "sussurrar":
                    return Sussurrar(resto);
                case "r":
                case "responder":
                    return Responder(resto, estado);
                case "ignorar":
                    return ComNome(AcaoDoComando.Ignorar, resto, "/ignorar Nome");
                case "designorar":
                    return ComNome(AcaoDoComando.Designorar, resto, "/designorar Nome");
                case "comandos":
                    return new ResultadoDoComando
                    {
                        Acao = AcaoDoComando.Informativo,
                        Mensagem = TextosDeChat.ListaCompletaDeComandos(),
                    };
                default:
                    if (ComandosDeBarraNoChat.Conhece(comando))
                    {
                        return new ResultadoDoComando
                        {
                            Acao = AcaoDoComando.Registrado,
                            ComandoBarra = comando,
                            RestoBarra = resto,
                        };
                    }

                    return Erro(TextosDeChat.ComandoDesconhecido(comando));
            }
        }

        private static ResultadoDoComando SemComando(string texto, EstadoDoEnvio estado)
        {
            if (
                estado.UltimoCanal == CanalDeChat.Sussurro
                && estado.UltimoAlvoDeSussurro.Length > 0
            )
            {
                return Enviar(CanalDeChat.Sussurro, estado.UltimoAlvoDeSussurro, texto);
            }

            // Sussurro sem alvo conhecido cai na Cidade (o primeiro canal).
            var canal =
                estado.UltimoCanal == CanalDeChat.Sussurro
                    ? CanalDeChat.Cidade
                    : estado.UltimoCanal;
            return Enviar(canal, "", texto);
        }

        private static ResultadoDoComando NoCanal(CanalDeChat canal, string nome, string texto)
        {
            if (texto.Length == 0)
            {
                return new ResultadoDoComando
                {
                    Acao = AcaoDoComando.MudarCanal,
                    Canal = canal,
                    Nome = nome,
                };
            }

            return Enviar(canal, nome, texto);
        }

        private static ResultadoDoComando Sussurrar(string resto)
        {
            Separar(resto, out var nome, out var texto);
            if (nome.Length == 0)
            {
                return Erro("Use /s Nome texto para sussurrar.");
            }

            return NoCanal(CanalDeChat.Sussurro, nome, texto);
        }

        private static ResultadoDoComando Responder(string resto, EstadoDoEnvio estado)
        {
            if (estado.UltimoSussurroRecebido.Length == 0)
            {
                return Erro("Ninguém sussurrou para você ainda.");
            }

            return NoCanal(CanalDeChat.Sussurro, estado.UltimoSussurroRecebido, resto);
        }

        private static ResultadoDoComando ComNome(AcaoDoComando acao, string resto, string uso)
        {
            Separar(resto, out var nome, out _);
            if (nome.Length == 0)
            {
                return Erro($"Use {uso}.");
            }

            return new ResultadoDoComando { Acao = acao, Nome = nome };
        }

        // Confere o tamanho antes de enviar (o servidor confere de novo e é quem decide).
        private static ResultadoDoComando Enviar(CanalDeChat canal, string nome, string texto)
        {
            var codigo = RegrasChat.Validar(texto);
            if (codigo != null)
            {
                return new ResultadoDoComando
                {
                    Acao = AcaoDoComando.Erro,
                    Mensagem = TextosDeChat.Recusa(codigo, 0),
                    ManterAberto = codigo == CodigosErroSocial.MensagemLongaDemais,
                };
            }

            return new ResultadoDoComando
            {
                Acao = AcaoDoComando.Enviar,
                Canal = canal,
                Nome = nome,
                Texto = RegrasChat.Limpar(texto),
            };
        }

        private static ResultadoDoComando Erro(string mensagem) =>
            new ResultadoDoComando { Acao = AcaoDoComando.Erro, Mensagem = mensagem };

        /// <summary>Divide na primeira sequência de espaços: a primeira palavra e o resto.</summary>
        private static void Separar(string texto, out string primeira, out string resto)
        {
            var limpo = texto.Trim();
            var indice = 0;
            while (indice < limpo.Length && !char.IsWhiteSpace(limpo[indice]))
            {
                indice++;
            }

            primeira = limpo.Substring(0, indice);
            resto = limpo.Substring(indice).Trim();
        }
    }
}
