using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Montarias;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Montarias
{
    /// <summary>
    /// Controla o estado de montaria do jogador local e a visualização dos outros
    /// jogadores montados na cena. Tecla de atalho: T.
    /// </summary>
    public sealed class ControleDeMontaria : MonoBehaviour
    {
        public static ControleDeMontaria Instancia { get; private set; }

        private readonly Dictionary<long, VisualDaMontaria> _visuaisAtivos =
            new Dictionary<long, VisualDaMontaria>();
        private readonly Dictionary<long, int> _estadosPendentes = new Dictionary<long, int>();
        private readonly HashSet<int> _colecaoDesbloqueada = new HashSet<int>
        {
            RegrasMontaria.TipoCastanho,
        };

        private IDisposable _assinatura;
        private IDisposable _assinaturaEquipada;
        private IDisposable _assinaturaColecao;
        private IDisposable _assinaturaDesbloqueada;
        private IDisposable _assinaturaRecusada;
        private Vector3 _posicaoInicioInvocacao;

        public bool MontadoLocal { get; private set; }
        public int MontariaEquipada { get; private set; } = RegrasMontaria.TipoPadrao;
        public IReadOnlyCollection<int> ColecaoDesbloqueada => _colecaoDesbloqueada;

        public bool Invocando { get; private set; }
        public float TempoInvocacaoAtual { get; private set; }
        public float DuracaoInvocacao => RegrasMontaria.TempoInvocacaoSegundos;

        public event Action<bool> EstadoMudou;
        public event Action<int> MontariaEquipadaMudou;
        public event Action ColecaoMudou;
        public event Action<bool, float, float> InvocacaoProgresso;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(gameObject);
                return;
            }

            Instancia = this;
        }

        private void OnEnable()
        {
            AssinarRede();
        }

        private void OnDisable()
        {
            DesassinarRede();
        }

        public void Iniciar()
        {
            AssinarRede();
        }

        public void Reiniciar()
        {
            CancelarInvocacao();
            DesassinarRede();
            LimparVisuais();
            MontadoLocal = false;
            _colecaoDesbloqueada.Clear();
            _colecaoDesbloqueada.Add(RegrasMontaria.TipoCastanho);
            ColecaoMudou?.Invoke();
        }

        private void AssinarRede()
        {
            if (_assinatura == null)
            {
                _assinatura = ConexaoTempoReal.Assinar<EstadoMontaria>(
                    MontariasMessageIds.EstadoMontaria,
                    AoReceberEstadoMontaria
                );
            }

            if (_assinaturaEquipada == null)
            {
                _assinaturaEquipada = ConexaoTempoReal.Assinar<MontariaEquipada>(
                    MontariasMessageIds.MontariaEquipada,
                    AoReceberMontariaEquipada
                );
            }

            if (_assinaturaColecao == null)
            {
                _assinaturaColecao = ConexaoTempoReal.Assinar<ColecaoMontarias>(
                    MontariasMessageIds.ColecaoMontarias,
                    AoReceberColecaoMontarias
                );
            }

            if (_assinaturaDesbloqueada == null)
            {
                _assinaturaDesbloqueada = ConexaoTempoReal.Assinar<MontariaDesbloqueada>(
                    MontariasMessageIds.MontariaDesbloqueada,
                    AoReceberMontariaDesbloqueada
                );
            }

            if (_assinaturaRecusada == null)
            {
                _assinaturaRecusada = ConexaoTempoReal.Assinar<MontariaRecusada>(
                    MontariasMessageIds.MontariaRecusada,
                    AoReceberMontariaRecusada
                );
            }
        }

        private void DesassinarRede()
        {
            _assinatura?.Dispose();
            _assinatura = null;
            _assinaturaEquipada?.Dispose();
            _assinaturaEquipada = null;
            _assinaturaColecao?.Dispose();
            _assinaturaColecao = null;
            _assinaturaDesbloqueada?.Dispose();
            _assinaturaDesbloqueada = null;
            _assinaturaRecusada?.Dispose();
            _assinaturaRecusada = null;
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo || EntradaNoJogo.Jogador == null)
            {
                if (Invocando)
                {
                    CancelarInvocacao();
                }
                return;
            }

            // Tecla T alterna montar / desmontar
            if (
                DeveProcessarAtalhoMontaria(
                    Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame,
                    JanelasDoJogo.MenuAberto,
                    BloqueioDeEntradaPorTexto.EntradaDeTextoAtiva()
                )
            )
            {
                AlternarMontaria();
            }

            // Atualiza tempo de invocação da montaria
            if (Invocando)
            {
                var jogador = EntradaNoJogo.Jogador;
                if (jogador == null)
                {
                    CancelarInvocacao();
                    return;
                }

                // Cancela se o jogador se deslocar durante a invocação
                var deslocamento = Vector3.Distance(
                    jogador.transform.position,
                    _posicaoInicioInvocacao
                );
                if (deslocamento > 0.25f)
                {
                    CancelarInvocacao("Invocação interrompida por movimento.");
                    return;
                }

                TempoInvocacaoAtual += Time.deltaTime;
                InvocacaoProgresso?.Invoke(true, TempoInvocacaoAtual, DuracaoInvocacao);

                if (TempoInvocacaoAtual >= DuracaoInvocacao)
                {
                    Invocando = false;
                    InvocacaoProgresso?.Invoke(false, DuracaoInvocacao, DuracaoInvocacao);
                    ChamarMontaria();
                }
            }

            // Tenta aplicar visuais pendentes para avatares que acabaram de ser instanciados na cena
            AplicarPendentes();
        }

        /// <summary>
        /// Avalia se a tecla de montaria (T) deve acionar a montaria, ignorando quando o menu do Esc
        /// estiver aberto ou quando houver digitação de texto ativa (chat, busca, reporte, etc.).
        /// </summary>
        public static bool DeveProcessarAtalhoMontaria(
            bool teclaApertada,
            bool menuAberto,
            bool entradaDeTextoAtiva
        )
        {
            return teclaApertada && !menuAberto && !entradaDeTextoAtiva;
        }

        public void AlternarMontaria()
        {
            if (MontadoLocal)
            {
                Desmontar();
            }
            else if (Invocando)
            {
                CancelarInvocacao("Invocação cancelada.");
            }
            else
            {
                IniciarInvocacao();
            }
        }

        public void IniciarInvocacao()
        {
            if (MontadoLocal || Invocando)
            {
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null)
            {
                _posicaoInicioInvocacao = jogador.transform.position;
            }

            Invocando = true;
            TempoInvocacaoAtual = 0f;
            InvocacaoProgresso?.Invoke(true, 0f, DuracaoInvocacao);
        }

        public void CancelarInvocacao(string motivo = null)
        {
            if (!Invocando)
            {
                return;
            }

            Invocando = false;
            TempoInvocacaoAtual = 0f;
            InvocacaoProgresso?.Invoke(false, 0f, DuracaoInvocacao);

            if (!string.IsNullOrEmpty(motivo))
            {
                AvisosDoJogo.Mostrar(motivo, TipoDeAviso.Aviso);
            }
        }

        public bool PossuiMontaria(int tipo)
        {
            return RegrasMontaria.DesbloqueadaPorPadrao(tipo)
                || _colecaoDesbloqueada.Contains(tipo);
        }

        public void EquiparMontaria(int tipo)
        {
            if (!RegrasMontaria.TipoValido(tipo))
            {
                return;
            }

            if (!PossuiMontaria(tipo))
            {
                AvisosDoJogo.Mostrar(
                    "Essa montaria ainda não foi desbloqueada.",
                    TipoDeAviso.Aviso
                );
                return;
            }

            MontariaEquipada = tipo;
            MontariaEquipadaMudou?.Invoke(tipo);

            ConexaoTempoReal.Enviar(
                MontariasMessageIds.EquiparMontaria,
                new EquiparMontaria { TipoMontaria = tipo }
            );

            AvisosDoJogo.Mostrar(
                $"Montaria equipada: {RegrasMontaria.ObterNome(tipo)}",
                TipoDeAviso.Informacao
            );
        }

        public void UsarItemMontaria(long instanciaId)
        {
            if (instanciaId <= 0)
            {
                return;
            }

            ConexaoTempoReal.Enviar(
                MontariasMessageIds.UsarItemMontaria,
                new UsarItemMontaria { InstanciaId = instanciaId }
            );
        }

        public void ChamarMontaria()
        {
            ConexaoTempoReal.Enviar(MontariasMessageIds.ChamarMontaria, new ChamarMontaria());
        }

        public void Desmontar()
        {
            if (Invocando)
            {
                CancelarInvocacao();
            }

            ConexaoTempoReal.Enviar(MontariasMessageIds.Desmontar, new Desmontar());
        }

        public void AoReceberMontariaEquipada(MontariaEquipada msg)
        {
            if (msg == null || !RegrasMontaria.TipoValido(msg.TipoMontaria))
            {
                return;
            }

            if (MontariaEquipada != msg.TipoMontaria)
            {
                MontariaEquipada = msg.TipoMontaria;
                MontariaEquipadaMudou?.Invoke(msg.TipoMontaria);
            }
        }

        public void AoReceberColecaoMontarias(ColecaoMontarias msg)
        {
            if (msg?.MontariasDesbloqueadas == null)
            {
                return;
            }

            _colecaoDesbloqueada.Clear();
            _colecaoDesbloqueada.Add(RegrasMontaria.TipoCastanho);
            foreach (var tipo in msg.MontariasDesbloqueadas)
            {
                if (RegrasMontaria.TipoValido(tipo))
                {
                    _colecaoDesbloqueada.Add(tipo);
                }
            }

            ColecaoMudou?.Invoke();
        }

        public void AoReceberMontariaDesbloqueada(MontariaDesbloqueada msg)
        {
            if (msg == null || !RegrasMontaria.TipoValido(msg.TipoMontaria))
            {
                return;
            }

            _colecaoDesbloqueada.Add(msg.TipoMontaria);
            AvisosDoJogo.Mostrar($"Montaria desbloqueada: {msg.Nome}!", TipoDeAviso.Informacao);
            ColecaoMudou?.Invoke();
        }

        public void AoReceberMontariaRecusada(MontariaRecusada msg)
        {
            if (msg == null)
            {
                return;
            }

            AvisosDoJogo.Mostrar(msg.Motivo, TipoDeAviso.Aviso);
        }

        public void AoReceberEstadoMontaria(EstadoMontaria msg)
        {
            if (msg == null)
            {
                return;
            }

            if (msg.PersonagemId == EntradaNoJogo.PersonagemId)
            {
                if (Invocando)
                {
                    Invocando = false;
                    InvocacaoProgresso?.Invoke(false, DuracaoInvocacao, DuracaoInvocacao);
                }

                var mudou = MontadoLocal != msg.Montado;
                MontadoLocal = msg.Montado;

                if (mudou)
                {
                    AvisosDoJogo.Mostrar(
                        msg.Montado ? "Montaria invocada" : "Desmontado",
                        TipoDeAviso.Informacao
                    );
                    EstadoMudou?.Invoke(msg.Montado);
                }

                if (msg.Montado)
                {
                    AplicarVisual(msg.PersonagemId, msg.TipoMontaria);
                }
                else
                {
                    RemoverVisual(msg.PersonagemId);
                }
            }
            else
            {
                if (msg.Montado)
                {
                    AplicarVisual(msg.PersonagemId, msg.TipoMontaria);
                }
                else
                {
                    RemoverVisual(msg.PersonagemId);
                }
            }
        }

        private void AplicarVisual(long personagemId, int tipo)
        {
            var avatar = ObterAvatar(personagemId);
            if (avatar == null)
            {
                _estadosPendentes[personagemId] = tipo;
                return;
            }

            _estadosPendentes.Remove(personagemId);

            if (_visuaisAtivos.TryGetValue(personagemId, out var existente) && existente != null)
            {
                return;
            }

            var visual = VisualDaMontaria.Criar(avatar.transform, tipo);
            _visuaisAtivos[personagemId] = visual;
        }

        private void RemoverVisual(long personagemId)
        {
            _estadosPendentes.Remove(personagemId);

            if (_visuaisAtivos.TryGetValue(personagemId, out var visual))
            {
                _visuaisAtivos.Remove(personagemId);
                if (visual != null)
                {
                    Destroy(visual.gameObject);
                }
            }
        }

        private void AplicarPendentes()
        {
            if (_estadosPendentes.Count == 0)
            {
                return;
            }

            var resolvidos = new List<long>();
            foreach (var par in _estadosPendentes)
            {
                var avatar = ObterAvatar(par.Key);
                if (avatar != null)
                {
                    var visual = VisualDaMontaria.Criar(avatar.transform, par.Value);
                    _visuaisAtivos[par.Key] = visual;
                    resolvidos.Add(par.Key);
                }
            }

            for (var i = 0; i < resolvidos.Count; i++)
            {
                _estadosPendentes.Remove(resolvidos[i]);
            }
        }

        private void LimparVisuais()
        {
            foreach (var par in _visuaisAtivos)
            {
                if (par.Value != null)
                {
                    Destroy(par.Value.gameObject);
                }
            }

            _visuaisAtivos.Clear();
            _estadosPendentes.Clear();
        }

        public static GameObject ObterAvatar(long personagemId)
        {
            if (EntradaNoJogo.NoJogo && EntradaNoJogo.PersonagemId == personagemId)
            {
                return EntradaNoJogo.Jogador;
            }

            return GameObject.Find($"Jogador_{personagemId}");
        }

        private void OnDestroy()
        {
            DesassinarRede();
            LimparVisuais();
            if (Instancia == this)
            {
                Instancia = null;
            }
        }
    }
}
