using System;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
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
        private Vector3 _ultimaPosicaoLocal;
        private bool _temPosicaoLocal;
        private AliadoLonge[] _aliadosLonge = Array.Empty<AliadoLonge>();
        private AliadoEmOutraArea[] _aliadosEmOutraArea = Array.Empty<AliadoEmOutraArea>();
        private string _areaAtual = RotulosDeArea.IdCidade;
        private float _idadeDosAliados;
        private bool _posicionado;
        private bool _descartado;
        private bool _estaCaido;

        public PresencaNaCidade(IConexaoDePresenca conexao, long meuPersonagemId)
        {
            _conexao = conexao;
            Avatares = new RegistroDeAvatares(meuPersonagemId);
            _conexao.EstadoMudou += AoMudarEstado;
            _assinaturas = new[]
            {
                _conexao.Assinar<EstadoDaCidade>(MundoMessageIds.EstadoDaCidade, AoReceberEstado),
                _conexao.Assinar<EstadoDaArea>(MundoMessageIds.EstadoDaArea, AoReceberEstadoDaArea),
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
                _conexao.Assinar<EstadoDeQueda>(
                    CombateMessageIds.EstadoDeQueda,
                    AoReceberEstadoDeQueda
                ),
                _conexao.Assinar<CanalAlterado>(
                    MundoMessageIds.CanalAlterado,
                    AoReceberCanalAlterado
                ),
                _conexao.Assinar<RecusaTrocaDeCanal>(
                    MundoMessageIds.RecusaTrocaDeCanal,
                    AoReceberRecusaTrocaDeCanal
                ),
                _conexao.Assinar<RespostaCanais>(
                    MundoMessageIds.RespostaCanais,
                    AoReceberRespostaCanais
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
        public System.Collections.Generic.IReadOnlyList<AliadoLonge> AliadosLonge => _aliadosLonge;

        /// <summary>Membros do grupo em outra área ou instância (#903).</summary>
        public System.Collections.Generic.IReadOnlyList<AliadoEmOutraArea> AliadosEmOutraArea =>
            _aliadosEmOutraArea;

        /// <summary>Identificador da área atual (<see cref="EstadoDaArea.Area"/>).</summary>
        public string AreaAtual => _areaAtual;

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

        /// <summary>Se o jogador local está caído ou sendo levantado (não pode andar).</summary>
        public bool EstaCaido => _estaCaido;

        /// <summary>Disparado quando o estado de queda do jogador local muda.</summary>
        public event Action<bool> EstadoDeQuedaMudou;

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

        /// <summary>Canal atual do jogador local no mundo persistente (ADR 0045).</summary>
        public int CanalAtual { get; private set; } = 1;

        /// <summary>Total de canais disponíveis no mundo.</summary>
        public int TotalCanais { get; private set; } = ConfigCanais.QuantidadeCanaisPadrao;

        /// <summary>Disparado quando o canal atual do jogador muda.</summary>
        public event Action<int, int> CanalMudou;

        /// <summary>Disparado quando o servidor recusa a troca de canal.</summary>
        public event Action<string, int> TrocaDeCanalRecusada;

        /// <summary>Disparado quando a lista de status dos canais é recebida.</summary>
        public event Action<RespostaCanais> CanaisRecebidos;

        /// <summary>Pede ao servidor a troca para o canal especificado.</summary>
        public bool PedirTrocaDeCanal(int canalDestino)
        {
            return _conexao.Conectado
                && _conexao.Enviar(
                    MundoMessageIds.PedirTrocaDeCanal,
                    new PedirTrocaDeCanal { CanalDestino = canalDestino },
                    EntregaTempoReal.Confiavel
                );
        }

        /// <summary>Pede ao servidor a lista de status dos canais.</summary>
        public bool ConsultarCanais()
        {
            return _conexao.Conectado
                && _conexao.Enviar(
                    MundoMessageIds.ConsultarCanais,
                    new ConsultarCanais(),
                    EntregaTempoReal.Confiavel
                );
        }

        /// <summary>Chame a cada quadro com a posição e a rotação do jogador local.</summary>
        public void Atualizar(float delta, Vector3 posicaoLocal, float rotacaoY)
        {
            Avatares.Atualizar(delta);
            _ultimaPosicaoLocal = posicaoLocal;
            _temPosicaoLocal = true;
            _idadeDosAliados += delta;
            if (_idadeDosAliados > ValidadeDosAliadosLonge)
            {
                if (_aliadosLonge.Length > 0)
                {
                    _aliadosLonge = Array.Empty<AliadoLonge>();
                }

                if (_aliadosEmOutraArea.Length > 0)
                {
                    _aliadosEmOutraArea = Array.Empty<AliadoEmOutraArea>();
                }
            }

            if (!_conexao.Conectado || !_limitador.Deve(delta) || _estaCaido)
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
            _aliadosEmOutraArea = mensagem?.EmOutraArea ?? Array.Empty<AliadoEmOutraArea>();
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

            _areaAtual = RotulosDeArea.IdCidade;
            Avatares.Aplicar(estado);
            if (estado.Eu != null && !_posicionado)
            {
                _posicionado = true;
                PosicaoDoServidor?.Invoke(
                    new Vector3(estado.Eu.X, estado.Eu.Y, estado.Eu.Z),
                    estado.Eu.RotacaoY
                );
            }
            else if (estado.Eu != null)
            {
                // Já posicionado: só é troca de área se o servidor trouxe o jogador de longe (volta
                // da instância). Na reconexão o servidor mantém a posição, perto do jogador.
                AplicarMudancaDeArea(estado.Eu);
            }
        }

        /// <summary>Longe assim (m) da posição local, o estado do servidor é troca de área.</summary>
        public const float DistanciaDeTrocaDeArea = 20f;

        private void AoReceberEstadoDaArea(EstadoDaArea estado)
        {
            if (estado == null)
            {
                return;
            }

            _areaAtual = string.IsNullOrEmpty(estado.Area) ? RotulosDeArea.IdCidade : estado.Area;
            Avatares.Aplicar(new EstadoDaCidade { Eu = estado.Eu, Outros = estado.Outros });
            _posicionado = true;
            AplicarMudancaDeArea(estado.Eu);
        }

        private void AplicarMudancaDeArea(JogadorVisivel eu)
        {
            if (eu == null)
            {
                return;
            }

            var destino = new Vector3(eu.X, eu.Y, eu.Z);
            if (MudouDeArea(destino))
            {
                PosicaoForcada?.Invoke(destino, eu.RotacaoY);
            }
        }

        private bool MudouDeArea(Vector3 destino)
        {
            if (!_temPosicaoLocal)
            {
                return true;
            }

            var dx = destino.x - _ultimaPosicaoLocal.x;
            var dz = destino.z - _ultimaPosicaoLocal.z;
            return dx * dx + dz * dz > DistanciaDeTrocaDeArea * DistanciaDeTrocaDeArea;
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

        private void AoReceberEstadoDeQueda(EstadoDeQueda queda)
        {
            if (queda == null || queda.PersonagemId != Avatares.MeuPersonagemId)
            {
                return;
            }

            var anterior = _estaCaido;
            _estaCaido =
                queda.Estado == FaseDeQueda.Caido || queda.Estado == FaseDeQueda.Levantando;
            if (anterior != _estaCaido)
            {
                EstadoDeQuedaMudou?.Invoke(_estaCaido);
            }
        }

        private void AoReceberCanalAlterado(CanalAlterado msg)
        {
            if (msg == null)
            {
                return;
            }

            CanalAtual = msg.Canal;
            TotalCanais = msg.TotalCanais;
            CanalMudou?.Invoke(msg.Canal, msg.TotalCanais);
        }

        private void AoReceberRecusaTrocaDeCanal(RecusaTrocaDeCanal msg)
        {
            if (msg == null)
            {
                return;
            }

            if (msg.CanalAtual > 0)
            {
                CanalAtual = msg.CanalAtual;
            }

            TrocaDeCanalRecusada?.Invoke(msg.Motivo, msg.CanalAtual);
        }

        private void AoReceberRespostaCanais(RespostaCanais msg)
        {
            if (msg == null)
            {
                return;
            }

            if (msg.CanalAtual > 0)
            {
                CanalAtual = msg.CanalAtual;
            }

            CanaisRecebidos?.Invoke(msg);
        }
    }
}
