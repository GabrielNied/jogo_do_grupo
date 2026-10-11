using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Lógica da presença: entra na cidade a cada conexão, mantém o registro dos outros
    /// jogadores e envia a posição local em taxa limitada. O servidor decide; a posição enviada
    /// é só uma intenção, e a posição autoritativa do próprio jogador chega por
    /// <see cref="PosicaoDoServidor"/>.
    /// </summary>
    public sealed class PresencaNaCidade : IDisposable
    {
        public const float EnviosPorSegundo = 10f;

        private readonly IConexaoDePresenca _conexao;
        private readonly LimitadorDeTaxa _limitador = new LimitadorDeTaxa(EnviosPorSegundo);
        private readonly IDisposable[] _assinaturas;
        private int _sequencia;
        private bool _descartado;

        public PresencaNaCidade(IConexaoDePresenca conexao, long meuPersonagemId)
        {
            _conexao = conexao;
            Avatares = new RegistroDeAvatares(meuPersonagemId);
            _conexao.EstadoMudou += AoMudarEstado;
            _assinaturas = new[]
            {
                _conexao.Assinar<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade, AoReceberEstado),
                _conexao.Assinar<JogadorEntrou>(MundoMessageIds.JogadorEntrou, AoEntrar),
                _conexao.Assinar<JogadorSaiu>(MundoMessageIds.JogadorSaiu, AoSair),
                _conexao.Assinar<PosicaoJogador>(MundoMessageIds.PosicaoJogador, AoReceberPosicao),
            };

            // Já conectado ao criar: entra agora, sem esperar uma mudança de estado.
            if (_conexao.Conectado)
            {
                EntrarNaCidade();
            }
        }

        public RegistroDeAvatares Avatares { get; }

        /// <summary>
        /// Posição e rotação autoritativas do jogador local (snapshot de entrada ou resposta a uma
        /// intenção). Quem move o jogador decide se a diferença justifica corrigir.
        /// </summary>
        public event Action<Vector3, float> PosicaoDoServidor;

        /// <summary>Chame a cada quadro com a posição e a rotação do jogador local.</summary>
        public void Atualizar(float delta, Vector3 posicaoLocal, float rotacaoY)
        {
            Avatares.Atualizar(delta);
            if (!_conexao.Conectado || !_limitador.Deve(delta))
            {
                return;
            }

            _sequencia++;
            _conexao.Enviar(
                MundoMessageIds.IntencaoDeMovimento,
                new IntencaoDeMovimento
                {
                    X = posicaoLocal.x,
                    Y = posicaoLocal.y,
                    Z = posicaoLocal.z,
                    RotacaoY = rotacaoY,
                    Sequencia = _sequencia,
                },
                EntregaTempoReal.Sequenciada
            );
        }

        public void Dispose()
        {
            if (_descartado)
            {
                return;
            }

            _descartado = true;
            _conexao.EstadoMudou -= AoMudarEstado;
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            Avatares.Limpar();
        }

        private void AoMudarEstado(EstadoTempoReal estado)
        {
            if (estado == EstadoTempoReal.Conectado)
            {
                EntrarNaCidade();
            }
            else if (
                estado == EstadoTempoReal.Reconectando
                || estado == EstadoTempoReal.Desconectado
            )
            {
                // Sem conexão não há presença; o snapshot da reconexão repõe todo mundo.
                Avatares.Limpar();
            }
        }

        private void EntrarNaCidade()
        {
            _conexao.Enviar(
                MundoMessageIds.EntrarNaCidade,
                new EntrarNaCidade(),
                EntregaTempoReal.Confiavel
            );
        }

        private void AoReceberEstado(EstadoDaCidade estado)
        {
            if (estado == null)
            {
                return;
            }

            Avatares.Aplicar(estado);
            if (estado.Eu != null)
            {
                PosicaoDoServidor?.Invoke(
                    new Vector3(estado.Eu.X, estado.Eu.Y, estado.Eu.Z),
                    estado.Eu.RotacaoY
                );
            }
        }

        private void AoEntrar(JogadorEntrou mensagem) => Avatares.Entrou(mensagem?.Jogador);

        private void AoSair(JogadorSaiu mensagem)
        {
            if (mensagem != null)
            {
                Avatares.Saiu(mensagem.PersonagemId);
            }
        }

        private void AoReceberPosicao(PosicaoJogador posicao)
        {
            if (posicao == null)
            {
                return;
            }

            if (posicao.PersonagemId == Avatares.MeuPersonagemId)
            {
                PosicaoDoServidor?.Invoke(
                    new Vector3(posicao.X, posicao.Y, posicao.Z),
                    posicao.RotacaoY
                );
                return;
            }

            Avatares.Mover(posicao);
        }
    }
}
