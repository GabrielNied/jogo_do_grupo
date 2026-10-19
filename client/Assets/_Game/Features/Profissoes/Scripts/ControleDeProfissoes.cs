using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Profissoes;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Profissoes
{
    /// <summary>
    /// O estado de Profissões no client (#192): o que o personagem aprendeu, os materiais e quais
    /// pontos de coleta estão esgotados. O servidor decide tudo; aqui só se guarda o que ele
    /// mandou e se avisa a pessoa do resultado.
    /// </summary>
    internal sealed class ControleDeProfissoes : MonoBehaviour
    {
        private const float IntervaloDaConsultaDosNos = 15f;

        private static ControleDeProfissoes _instancia;
        private static readonly Dictionary<int, float> _voltaEm = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> _materiais = new Dictionary<int, int>();
        private static readonly HashSet<int> _aprendidas = new HashSet<int>();

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private float _proximaConsulta;

        /// <summary>O estado de profissões ou materiais mudou (a janela redesenha).</summary>
        public static event Action EstadoMudou;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            _voltaEm.Clear();
            _materiais.Clear();
            _aprendidas.Clear();
            EstadoMudou = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;

            var objeto = new GameObject("Profissoes.Controle");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ControleDeProfissoes>();
        }

        public static bool Aprendeu(Profissao profissao) => _aprendidas.Contains((int)profissao);

        public static int Saldo(int materialId) =>
            _materiais.TryGetValue(materialId, out var quantidade) ? quantidade : 0;

        /// <summary>Se o ponto está disponível para coletar (esgotado some até o respawn).</summary>
        public static bool NoDisponivel(int noId) =>
            !_voltaEm.TryGetValue(noId, out var volta) || Time.realtimeSinceStartup >= volta;

        public static void ConsultarTudo()
        {
            ConexaoTempoReal.Enviar(
                ProfissoesMessageIds.ConsultarProfissoes,
                new ConsultarProfissoes()
            );
            ConsultarNos();
        }

        public static void ConsultarNos() =>
            ConexaoTempoReal.Enviar(ProfissoesMessageIds.ConsultarNos, new ConsultarNos());

        public static void Aprender(Profissao profissao) =>
            ConexaoTempoReal.Enviar(
                ProfissoesMessageIds.AprenderProfissao,
                new AprenderProfissao { Profissao = profissao }
            );

        public static void Coletar(int noId) =>
            ConexaoTempoReal.Enviar(ProfissoesMessageIds.ColetarNo, new ColetarNo { NoId = noId });

        public static void Produzir(int receitaId) =>
            ConexaoTempoReal.Enviar(
                ProfissoesMessageIds.ProduzirReceita,
                new ProduzirReceita { ReceitaId = receitaId }
            );

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDasProfissoes>(
                    ProfissoesMessageIds.EstadoDasProfissoes,
                    AoEstadoDasProfissoes
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDosNos>(
                    ProfissoesMessageIds.EstadoDosNos,
                    AoEstadoDosNos
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ColetaConcluida>(
                    ProfissoesMessageIds.ColetaConcluida,
                    AoColetar
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ProducaoConcluida>(
                    ProfissoesMessageIds.ProducaoConcluida,
                    AoProduzir
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ProfissoesRecusada>(
                    ProfissoesMessageIds.ProfissoesRecusada,
                    AoRecusar
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
                assinatura?.Dispose();
            _assinaturas.Clear();
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                _proximaConsulta = 0f;
                return;
            }

            if (Time.realtimeSinceStartup < _proximaConsulta)
                return;

            // Os pontos voltam sozinhos (o client conta o respawn); a consulta periódica só
            // corrige a conta e pega o que outros jogadores levaram.
            if (_proximaConsulta == 0f)
                ConsultarTudo();
            else
                ConsultarNos();
            _proximaConsulta = Time.realtimeSinceStartup + IntervaloDaConsultaDosNos;
        }

        private static void AoEstadoDasProfissoes(EstadoDasProfissoes estado)
        {
            _aprendidas.Clear();
            foreach (var profissao in estado.Aprendidas)
                _aprendidas.Add(profissao);
            _materiais.Clear();
            foreach (var saldo in estado.Materiais)
                _materiais[saldo.MaterialId] = saldo.Quantidade;
            EstadoMudou?.Invoke();
        }

        private static void AoEstadoDosNos(EstadoDosNos estado)
        {
            _voltaEm.Clear();
            for (var i = 0; i < estado.Esgotados.Length; i++)
                _voltaEm[estado.Esgotados[i]] =
                    Time.realtimeSinceStartup + estado.SegundosParaVoltar[i];
        }

        private static void AoColetar(ColetaConcluida coleta)
        {
            var ponto = ConfigProfissoes.ObterPonto(coleta.NoId);
            if (ponto != null)
                _voltaEm[coleta.NoId] =
                    Time.realtimeSinceStartup + ConfigProfissoes.RespawnSegundos;
            AvisosDoJogo.Mostrar(TextosProfissoes.Coleta(coleta), TipoDeAviso.Informacao);
        }

        private static void AoProduzir(ProducaoConcluida producao) =>
            AvisosDoJogo.Mostrar(TextosProfissoes.Producao(producao), TipoDeAviso.Informacao);

        private static void AoRecusar(ProfissoesRecusada recusa) =>
            AvisosDoJogo.Mostrar(TextosProfissoes.Recusa(recusa.Codigo), TipoDeAviso.Aviso);
    }
}
