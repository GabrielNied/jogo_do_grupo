namespace JogoDoGrupo.Shared.Features.Missoes
{
    /// <summary>Client → servidor: pede para listar missões ativas e concluídas.</summary>
    public class ConsultarMissoes { }

    /// <summary>Client → servidor: pede para aceitar uma missão do NPC.</summary>
    public class AceitarMissao
    {
        public int MissaoId;
    }

    /// <summary>Client → servidor: pede para abandonar uma missão secundária ou diária.</summary>
    public class AbandonarMissao
    {
        public int MissaoId;
    }

    /// <summary>Client → servidor: pede para entregar uma missão pronta para entrega.</summary>
    public class EntregarMissao
    {
        public int MissaoId;
        public int ItemIdEscolhido;
    }

    /// <summary>Client → servidor: o líder do grupo compartilha a missão com o grupo.</summary>
    public class CompartilharMissao
    {
        public int MissaoId;
    }

    /// <summary>Client → servidor: alterna se a missão está no rastreador do HUD (até 4 acompanhadas).</summary>
    public class AlternarAcompanhamento
    {
        public int MissaoId;
        public bool Acompanhada;
    }

    /// <summary>
    /// Sem uso: o servidor não trata mais este pedido (ele não conferia a distância). A conversa
    /// conta para as missões quando os NPCs aceitam o <c>FalarComNpc</c>. O id fica reservado.
    /// </summary>
    public class InteragirComNpcMissao
    {
        public int NpcId;
    }

    /// <summary>Servidor → client: lista completa das missões do personagem.</summary>
    public class EstadoDasMissoes
    {
        public ProgressoDeMissaoDto[] Ativas = System.Array.Empty<ProgressoDeMissaoDto>();
        public int[] Concluidas = System.Array.Empty<int>();
        public int[] RotacaoDiariaIds = System.Array.Empty<int>();
        public int[] RotacaoSemanalIds = System.Array.Empty<int>();
        public long ProximoResetDiarioSegundos;
        public long ProximoResetSemanalSegundos;
    }

    /// <summary>Servidor → client: atualização de uma missão específica (progresso de objetivo ou estado).</summary>
    public class MissaoAtualizada
    {
        public ProgressoDeMissaoDto Missao = new ProgressoDeMissaoDto();
    }

    /// <summary>Servidor → client: recusa de operação de missão.</summary>
    public class OperacaoDeMissaoRecusada
    {
        public string CodigoErro = "";
        public int MissaoId;

        public OperacaoDeMissaoRecusada() { }

        public OperacaoDeMissaoRecusada(string codigoErro, int missaoId = 0)
        {
            CodigoErro = codigoErro;
            MissaoId = missaoId;
        }
    }

    /// <summary>Servidor → client: convite de missão compartilhada pelo líder do grupo.</summary>
    public class ConviteDeMissaoCompartilhada
    {
        public int MissaoId;
        public string NomeLider = "";
        public string TituloMissao = "";
    }

    /// <summary>Servidor → client: aviso popup ou feedback na tela (Aceita, Objetivo Cumprido, Concluída).</summary>
    public class AvisoDeMissao
    {
        public int MissaoId;
        public string Titulo = "";
        public string TipoAviso = ""; // "aceita", "objetivo_cumprido", "concluida"
        public string Mensagem = "";
    }
}
