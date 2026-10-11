using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Desenvolvimento
{
    /// <summary>IDs de mensagem de Desenvolvimento. Use somente valores entre 15000 e 15999.</summary>
    public static class DesenvolvimentoMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(15000, 15999);

        /// <summary>Client → servidor: texto de um comando de teste (<see cref="PedirComandoDeTeste"/>).</summary>
        public const ushort PedirComandoDeTeste = 15000;

        /// <summary>
        /// Servidor → client: o que o comando fez (<see cref="ResultadoDeComandoDeTeste"/>).
        /// Sem a chave de desenvolvimento, o servidor não responde.
        /// </summary>
        public const ushort ResultadoDeComandoDeTeste = 15001;
    }
}
