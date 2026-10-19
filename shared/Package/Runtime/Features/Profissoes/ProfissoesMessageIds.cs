using JogoDoGrupo.Shared.Core;

namespace JogoDoGrupo.Shared.Features.Profissoes
{
    /// <summary>IDs de mensagem de Profissões. Use somente valores entre 8000 e 8999.</summary>
    public static class ProfissoesMessageIds
    {
        public static readonly MessageIdRange Faixa = new MessageIdRange(8000, 8999);

        /// <summary>Client → servidor: pede as profissões aprendidas e os materiais.</summary>
        public const ushort ConsultarProfissoes = 8000;

        /// <summary>Servidor → client: <see cref="EstadoDasProfissoes"/>.</summary>
        public const ushort EstadoDasProfissoes = 8001;

        /// <summary>Client → servidor: <see cref="AprenderProfissao"/>.</summary>
        public const ushort AprenderProfissao = 8002;

        /// <summary>Client → servidor: <see cref="ColetarNo"/>.</summary>
        public const ushort ColetarNo = 8003;

        /// <summary>Servidor → client: <see cref="ColetaConcluida"/>.</summary>
        public const ushort ColetaConcluida = 8004;

        /// <summary>Client → servidor: <see cref="ProduzirReceita"/>.</summary>
        public const ushort ProduzirReceita = 8005;

        /// <summary>Servidor → client: <see cref="ProducaoConcluida"/>.</summary>
        public const ushort ProducaoConcluida = 8006;

        /// <summary>Servidor → client: <see cref="ProfissoesRecusada"/>.</summary>
        public const ushort ProfissoesRecusada = 8007;

        /// <summary>Client → servidor: pede quais pontos de coleta estão esgotados.</summary>
        public const ushort ConsultarNos = 8008;

        /// <summary>Servidor → client: <see cref="EstadoDosNos"/>.</summary>
        public const ushort EstadoDosNos = 8009;
    }
}
