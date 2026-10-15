using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Lógica da presença: entra na cidade a cada conexão, mantém o registro dos outros
    /// jogadores e envia a posição local em taxa limitada. O jogador local é dono da própria
    /// posição (ADR 0024): o servidor valida e repassa aos outros, sem devolver. A posição do
    /// jogador só muda por fora em dois casos: o nascimento (<see cref="PosicaoDoServidor"/>, na
    /// primeira entrada) e a <see cref="PosicaoForcada"/> (teleporte, morte, empurrão ou trapaça
    /// grosseira), que é confirmada ao servidor na hora.
    /// </summary>
    public sealed class PresencaNaCidade : IDisposable
    {
        public const float EnviosPorSegundo = 10f;

        private readonly IConexaoDePresenca _conexao;
        private readonly LimitadorDeTaxa _limitador = new LimitadorDeTaxa(EnviosPorSegundo);
        private readonly IDisposable[] _assinaturas;
        private int _sequencia;
        private AliadoLonge[] _aliadosLonge = Array.Empty<AliadoLonge>();
        private float _idadeDosAliados;
        private bool _posicionado;
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
                _conexao.Assinar<PosicaoForcada>(
                    MundoMessageIds.PosicaoForcada,
                    AoReceberPosicaoForcada
                ),
                _conexao.Assinar<PosicoesDosAliados>(
                    MundoMessageIds.PosicoesDosAliados,
                    AoReceberAliados
                ),
                _conexao.Assinar<VelocidadesDeMovimento>(
                    MundoMessageIds.VelocidadesDeMovimento,
                    AoReceberVelocidades
                ),
            };

            // Já conectado ao criar: entra agora, sem esperar uma mudança de estado.
            if (_conexao.Conectado)
            {
                EntrarNaCidade();
            }
        }

        /// <summary>
        /// Segundos sem notícia dos aliados longe: passado isso, o servidor parou de mandar (saiu
        /// do grupo ou da cidade) e eles somem do mapa.
        /// </summary>
        public const float ValidadeDosAliadosLonge = 3f;

        public RegistroDeAvatares Avatares { get; }

        /// <summary>
        /// Aliados do grupo fora da área de interesse (#789): só posição, para o mapa e o minimapa.
        /// O modelo 3D continua só dos que estão em <see cref="Avatares"/>.
        /// </summary>
        public System.Collections.Generic.IReadOnlyList<AliadoLonge> AliadosLonge =>
            _aliadosLonge;

        /// <summary>
        /// Onde o jogador local nasce: só no primeiro <c>EstadoDaCidade</c>. Na reconexão o
        /// jogador continua onde está (ADR 0024).
        /// </summary>
        public event Action<Vector3, float> PosicaoDoServidor;

        /// <summary>
        /// O servidor mandou o jogador para outra posição (<c>PosicaoForcada</c>). Quem move o
        /// jogador aplica sempre; a confirmação ao servidor já é enviada aqui.
        /// </summary>
        public event Action<Vector3, float> PosicaoForcada;

        /// <summary>
        /// Andar e correr (m/s) que o servidor deu ao jogador local. Quem move o jogador aplica;
        /// a confirmação ao servidor já é enviada aqui (modelo do WoW: aplicar e confirmar).
        /// </summary>
        public event Action<float, float> VelocidadesDoServidor;

        /// <summary>
        /// Pede ao servidor outras velocidades, para testar (só vale com o servidor em
        /// desenvolvimento; se recusar, nada muda). Falso se não está conectado.
        /// </summary>
        public bool PedirVelocidadeDeTeste(float andar, float correr)
        {
            return _conexao.Conectado
                && _conexao.Enviar(
                    MundoMessageIds.PedirVelocidadeDeTeste,
                    new PedirVelocidadeDeTeste { Andar = andar, Correr = correr },
                    EntregaTempoReal.Confiavel
                );
        }

        /// <summary>Chame a cada quadro com a posição e a rotação do jogador local.</summary>
        public void Atualizar(float delta, Vector3 posicaoLocal, float rotacaoY)
        {
            Avatares.Atualizar(delta);
            _idadeDosAliados += delta;
            if (_idadeDosAliados > ValidadeDosAliadosLonge && _aliadosLonge.Length > 0)
            {
                _aliadosLonge = Array.Empty<AliadoLonge>();
            }

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
                _aliadosLonge = Array.Empty<AliadoLonge>();
            }
        }

        private void AoReceberAliados(PosicoesDosAliados mensagem)
        {
            _aliadosLonge = mensagem?.Aliados ?? Array.Empty<AliadoLonge>();
            _idadeDosAliados = 0f;
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
            if (estado.Eu != null && !_posicionado)
            {
                _posicionado = true;
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

        private void AoReceberPosicaoForcada(PosicaoForcada forcada)
        {
            if (forcada == null)
            {
                return;
            }

            PosicaoForcada?.Invoke(new Vector3(forcada.X, forcada.Y, forcada.Z), forcada.RotacaoY);
            _conexao.Enviar(
                MundoMessageIds.ConfirmarPosicaoForcada,
                new ConfirmarPosicaoForcada { Sequencia = forcada.Sequencia },
                EntregaTempoReal.Confiavel
            );
        }

        private void AoReceberVelocidades(VelocidadesDeMovimento velocidades)
        {
            if (velocidades == null)
            {
                return;
            }

            // Aplica antes de confirmar: o servidor só aperta o limite depois do ACK.
            VelocidadesDoServidor?.Invoke(velocidades.Andar, velocidades.Correr);
            _conexao.Enviar(
                MundoMessageIds.ConfirmarVelocidade,
                new ConfirmarVelocidade { Sequencia = velocidades.Sequencia },
                EntregaTempoReal.Confiavel
            );
        }

        private void AoReceberPosicao(PosicaoJogador posicao)
        {
            if (posicao == null)
            {
                return;
            }

            if (posicao.PersonagemId == Avatares.MeuPersonagemId)
            {
                // O jogador local é dono da própria posição (ADR 0024): não reconcilia.
                return;
            }

            Avatares.Mover(posicao);
        }
    }
}
