using System;
using System.Collections.Generic;
using System.Linq;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Missoes;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Gerenciador autoritativo do estado de missões no client (#179, #1061).
    /// </summary>
    internal sealed class ControleDeMissoes : MonoBehaviour
    {
        private static ControleDeMissoes _instancia;
        private static readonly List<ProgressoDeMissaoDto> _ativas =
            new List<ProgressoDeMissaoDto>();
        private static readonly HashSet<int> _concluidas = new HashSet<int>();

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        public static event Action EstadoMudou;
        public static event Action<AvisoDeMissao> AvisoRecebido;

        public static IReadOnlyList<ProgressoDeMissaoDto> Ativas => _ativas;
        public static IReadOnlyCollection<int> Concluidas => _concluidas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            _ativas.Clear();
            _concluidas.Clear();
            EstadoMudou = null;
            AvisoRecebido = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Missoes.Controle");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ControleDeMissoes>();
        }

        public static bool EstaConcluida(int missaoId) => _concluidas.Contains(missaoId);

        public static ProgressoDeMissaoDto ObterAtiva(int missaoId)
        {
            for (int i = 0; i < _ativas.Count; i++)
            {
                if (_ativas[i].MissaoId == missaoId)
                    return _ativas[i];
            }
            return null;
        }

        public static List<ProgressoDeMissaoDto> ObterAcompanhadas()
        {
            var resultado = new List<ProgressoDeMissaoDto>();
            for (int i = 0; i < _ativas.Count; i++)
            {
                if (_ativas[i].Acompanhada)
                    resultado.Add(_ativas[i]);
            }
            return resultado;
        }

        public static void Consultar()
        {
            ConexaoTempoReal.Enviar(MissoesMessageIds.ConsultarMissoes, new ConsultarMissoes());
        }

        public static void Aceitar(int missaoId)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.AceitarMissao,
                new AceitarMissao { MissaoId = missaoId }
            );
        }

        public static void Abandonar(int missaoId)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.AbandonarMissao,
                new AbandonarMissao { MissaoId = missaoId }
            );
        }

        public static void Entregar(int missaoId, int itemIdEscolhido = 0)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.EntregarMissao,
                new EntregarMissao { MissaoId = missaoId, ItemIdEscolhido = itemIdEscolhido }
            );
        }

        public static void AlternarAcompanhamento(int missaoId, bool acompanhada)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.AlternarAcompanhamento,
                new AlternarAcompanhamento { MissaoId = missaoId, Acompanhada = acompanhada }
            );
        }

        public static void Compartilhar(int missaoId)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.CompartilharMissao,
                new CompartilharMissao { MissaoId = missaoId }
            );
        }

        public static void InteragirComNpc(int npcId)
        {
            ConexaoTempoReal.Enviar(
                MissoesMessageIds.InteragirComNpcMissao,
                new InteragirComNpcMissao { NpcId = npcId }
            );
        }

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDasMissoes>(
                    MissoesMessageIds.EstadoDasMissoes,
                    AoEstadoDasMissoes
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<MissaoAtualizada>(
                    MissoesMessageIds.MissaoAtualizada,
                    AoMissaoAtualizada
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<OperacaoDeMissaoRecusada>(
                    MissoesMessageIds.OperacaoDeMissaoRecusada,
                    AoRecusar
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ConviteDeMissaoCompartilhada>(
                    MissoesMessageIds.ConviteDeMissaoCompartilhada,
                    AoConviteCompartilhado
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<AvisoDeMissao>(
                    MissoesMessageIds.AvisoDeMissao,
                    AoAvisoDeMissao
                )
            );
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assinaturas.Count; i++)
            {
                _assinaturas[i]?.Dispose();
            }
            _assinaturas.Clear();
        }

        private static void AoEstadoDasMissoes(EstadoDasMissoes estado)
        {
            _ativas.Clear();
            if (estado.Ativas != null)
            {
                _ativas.AddRange(estado.Ativas);
            }

            _concluidas.Clear();
            if (estado.Concluidas != null)
            {
                for (int i = 0; i < estado.Concluidas.Length; i++)
                {
                    _concluidas.Add(estado.Concluidas[i]);
                }
            }

            EstadoMudou?.Invoke();
        }

        private static void AoMissaoAtualizada(MissaoAtualizada msg)
        {
            if (msg.Missao == null)
                return;

            var indice = _ativas.FindIndex(m => m.MissaoId == msg.Missao.MissaoId);
            if (msg.Missao.Estado == EstadoDaMissao.Concluida)
            {
                if (indice >= 0)
                    _ativas.RemoveAt(indice);
                _concluidas.Add(msg.Missao.MissaoId);
            }
            else
            {
                if (indice >= 0)
                    _ativas[indice] = msg.Missao;
                else
                    _ativas.Add(msg.Missao);
            }

            EstadoMudou?.Invoke();
        }

        private static void AoRecusar(OperacaoDeMissaoRecusada recusa)
        {
            var texto = ObterTextoRecusa(recusa.CodigoErro);
            AvisosDoJogo.Mostrar(texto, TipoDeAviso.Aviso);
        }

        private static void AoConviteCompartilhado(ConviteDeMissaoCompartilhada convite)
        {
            AvisosDoJogo.Mostrar(
                $"O líder {convite.NomeLider} compartilhou a missão \"{convite.TituloMissao}\".",
                TipoDeAviso.Informacao
            );
        }

        private static void AoAvisoDeMissao(AvisoDeMissao aviso)
        {
            AvisoRecebido?.Invoke(aviso);
            var tipo =
                aviso.TipoAviso == "concluida" ? TipoDeAviso.Sucesso : TipoDeAviso.Informacao;
            AvisosDoJogo.Mostrar(aviso.Mensagem, tipo);
        }

        private static string ObterTextoRecusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroMissoes.MissaoNaoEncontrada:
                    return "Missão não encontrada.";
                case CodigosErroMissoes.NivelInsuficiente:
                    return "Nível insuficiente para aceitar esta missão.";
                case CodigosErroMissoes.PreRequisitosNaoCumpridos:
                    return "Pré-requisitos da missão não foram cumpridos.";
                case CodigosErroMissoes.LimiteMissoesAtingido:
                    return "Limite máximo de missões ativas atingido.";
                case CodigosErroMissoes.MissaoJaAceita:
                    return "Esta missão já está em andamento.";
                case CodigosErroMissoes.MissaoJaConcluida:
                    return "Esta missão já foi concluída.";
                case CodigosErroMissoes.MissaoPrincipalNaoAbandona:
                    return "Missões principais não podem ser abandonadas.";
                case CodigosErroMissoes.ObjetivosNaoCumpridos:
                    return "Objetivos da missão ainda não foram concluídos.";
                case CodigosErroMissoes.LongeDoNpc:
                    return "Aproxime-se do NPC para interagir.";
                case CodigosErroMissoes.ApenasLiderCompartilha:
                    return "Apenas o líder do grupo pode compartilhar missões.";
                case CodigosErroMissoes.LimiteRastreadorAtingido:
                    return "Limite de missões no rastreador atingido (máx. 4).";
                default:
                    return "Operação de missão recusada.";
            }
        }
    }
}
