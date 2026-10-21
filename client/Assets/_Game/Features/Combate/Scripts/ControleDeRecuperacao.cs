using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Combate
{
    /// <summary>
    /// Controla a recuperação no client (#347): uso de poções de cura rápida com recarga compartilhada,
    /// refeições e descanso/sentar para recuperação acelerada fora de combate.
    /// </summary>
    internal sealed class ControleDeRecuperacao : MonoBehaviour
    {
        public const string TeclaPocaoPadrao = "<Keyboard>/h";
        public const string TeclaDescansoPadrao = "<Keyboard>/x";

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();

        private InputAction _acaoPocao;
        private InputAction _acaoDescanso;

        private bool _descansando;
        private bool _emCombate;
        private double _recargaPocaoAte;

        public bool Descansando => _descansando;
        public bool EmCombate => _emCombate;
        public double RecargaPocaoRestante =>
            Math.Max(0.0, _recargaPocaoAte - Time.unscaledTimeAsDouble);

        private void Awake()
        {
            _acaoPocao = new InputAction("ConsumirPocao", InputActionType.Button, TeclaPocaoPadrao);
            _acaoDescanso = new InputAction(
                "Descansar",
                InputActionType.Button,
                TeclaDescansoPadrao
            );
        }

        private void OnEnable()
        {
            _acaoPocao?.Enable();
            _acaoDescanso?.Enable();

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PocaoConsumida>(
                    CombateMessageIds.PocaoConsumida,
                    AoPocaoConsumida
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<RefeicaoConsumida>(
                    CombateMessageIds.RefeicaoConsumida,
                    AoRefeicaoConsumida
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<DescansoMudou>(
                    CombateMessageIds.DescansoMudou,
                    AoDescansoMudou
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<EstadoDeRecuperacao>(
                    CombateMessageIds.EstadoDeRecuperacao,
                    AoEstadoRecebido
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<RecuperacaoRecusada>(
                    CombateMessageIds.RecuperacaoRecusada,
                    AoRecuperacaoRecusada
                )
            );
        }

        private void OnDisable()
        {
            _acaoPocao?.Disable();
            _acaoDescanso?.Disable();

            foreach (var a in _assinaturas)
            {
                a.Dispose();
            }
            _assinaturas.Clear();
        }

        private void OnDestroy()
        {
            _acaoPocao?.Dispose();
            _acaoDescanso?.Dispose();
        }

        private void Update()
        {
            if (
                !EntradaNoJogo.NoJogo
                || EntradaNoJogo.Jogador == null
                || !ConexaoTempoReal.Conectado
                || Cursor.lockState != CursorLockMode.Locked
            )
            {
                return;
            }

            if (_acaoPocao != null && _acaoPocao.WasPressedThisFrame())
            {
                UsarPocao();
            }

            if (_acaoDescanso != null && _acaoDescanso.WasPressedThisFrame())
            {
                AlternarDescanso();
            }
        }

        public void UsarPocao(long instanciaId = 0)
        {
            if (!ConexaoTempoReal.Conectado)
            {
                return;
            }

            ConexaoTempoReal.Enviar(
                CombateMessageIds.ConsumirPocao,
                new ConsumirPocao { InstanciaId = instanciaId }
            );
        }

        public void UsarRefeicao(int modeloId)
        {
            if (!ConexaoTempoReal.Conectado)
            {
                return;
            }

            ConexaoTempoReal.Enviar(
                CombateMessageIds.ConsumirRefeicao,
                new ConsumirRefeicao { ModeloId = modeloId }
            );
        }

        public void AlternarDescanso()
        {
            if (!ConexaoTempoReal.Conectado)
            {
                return;
            }

            var novoEstado = !_descansando;
            ConexaoTempoReal.Enviar(
                CombateMessageIds.IntencaoDeDescanso,
                new IntencaoDeDescanso { Descansando = novoEstado }
            );
        }

        public void ConsultarEstado()
        {
            if (!ConexaoTempoReal.Conectado)
            {
                return;
            }

            ConexaoTempoReal.Enviar(
                CombateMessageIds.ConsultarEstadoDeRecuperacao,
                new ConsultarEstadoDeRecuperacao()
            );
        }

        private void AoPocaoConsumida(PocaoConsumida msg)
        {
            _recargaPocaoAte = Time.unscaledTimeAsDouble + msg.RecargaSegundos;
            AvisosDoJogo.Mostrar(
                $"Poção consumida (+{msg.VidaCurada} vida)",
                TipoDeAviso.Aviso,
                CategoriaDeAviso.Combate
            );
        }

        private void AoRefeicaoConsumida(RefeicaoConsumida msg)
        {
            AvisosDoJogo.Mostrar(
                $"Refeição consumida: {msg.Nome} (+{msg.BonusRegenVida:0} vida/s)",
                TipoDeAviso.Aviso,
                CategoriaDeAviso.Combate
            );
        }

        private void AoDescansoMudou(DescansoMudou msg)
        {
            if (
                EntradaNoJogo.Jogador != null
                && msg.PersonagemId == EntradaNoJogo.Jogador.PersonagemId
            )
            {
                _descansando = msg.Descansando;
                if (_descansando)
                {
                    AvisosDoJogo.Mostrar(
                        "Descansando: recuperação acelerada",
                        TipoDeAviso.Aviso,
                        CategoriaDeAviso.Combate
                    );
                }
                else
                {
                    AvisosDoJogo.Mostrar(
                        "Descanso encerrado",
                        TipoDeAviso.Aviso,
                        CategoriaDeAviso.Combate
                    );
                }
            }
        }

        private void AoEstadoRecebido(EstadoDeRecuperacao msg)
        {
            _emCombate = msg.EmCombate;
            _descansando = msg.Descansando;
            if (msg.RecargaPocaoRestanteSegundos > 0)
            {
                _recargaPocaoAte = Time.unscaledTimeAsDouble + msg.RecargaPocaoRestanteSegundos;
            }
        }

        private void AoRecuperacaoRecusada(RecuperacaoRecusada msg)
        {
            string texto;
            switch (msg.Codigo)
            {
                case CodigosErroCombate.PocaoEmRecarga:
                    texto = "Poção em recarga.";
                    break;
                case CodigosErroCombate.VidaCheia:
                    texto = "Vida já está cheia.";
                    break;
                case CodigosErroCombate.SemPocao:
                    texto = "Você não tem poção na bolsa.";
                    break;
                case CodigosErroCombate.SemRefeicao:
                    texto = "Você não tem essa refeição na bolsa.";
                    break;
                case CodigosErroCombate.AcaoBloqueadaEmCombate:
                    texto = "Esta ação só pode ser realizada fora de combate.";
                    break;
                default:
                    texto = "Ação de recuperação não permitida.";
                    break;
            }

            AvisosDoJogo.Mostrar(texto, TipoDeAviso.Erro, CategoriaDeAviso.Combate);
        }
    }
}
