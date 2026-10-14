using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Liga a presença ao mundo: conecta ao entrar, mostra os outros jogadores como cápsulas e
    /// envia a posição do jogador local. Vive na cena do mundo, então some junto com ela.
    /// </summary>
    public sealed class PresencaNoMundo : MonoBehaviour
    {
        /// <summary>
        /// Acima desta distância (m) a posição forçada teleporta o jogador na hora;
        /// abaixo ou igual a isto, a correção é suavizada.
        /// </summary>
        public const float DistanciaDeCorrecao = ConfigInterpolacaoMundo.DistanciaDeCorrecao;

        private readonly System.Collections.Generic.Dictionary<long, GameObject> _visuais =
            new System.Collections.Generic.Dictionary<long, GameObject>();

        private PresencaNaCidade _presenca;
        private ControleTerceiraPessoa _controle;

        // Última velocidade do servidor: pode chegar antes de o controle do jogador existir.
        private Vector2? _velocidades;
        private bool _correcaoPendente;
        private bool _posicaoInicialPendente;
        private Vector3 _posicaoCorrigida;
        private float _rotacaoCorrigida;

        /// <summary>A presença da cena do mundo, ou <c>null</c> fora dela. O mapa lê os outros jogadores daqui.</summary>
        public static PresencaNoMundo Atual { get; private set; }

        private void OnEnable()
        {
            Atual = this;
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (_presenca == null)
            {
                if (jogador == null || !EntradaNoJogo.NoJogo)
                {
                    return;
                }

                Iniciar(EntradaNoJogo.PersonagemId);
            }

            if (jogador == null)
            {
                return;
            }

            LigarControle(jogador);
            if (_correcaoPendente)
            {
                if (
                    _posicaoInicialPendente
                    || (jogador.transform.position - _posicaoCorrigida).sqrMagnitude
                        > DistanciaDeCorrecao * DistanciaDeCorrecao
                )
                {
                    _correcaoPendente = false;
                    Corrigir(jogador);
                    if (_posicaoInicialPendente)
                    {
                        _posicaoInicialPendente = false;
                        EntradaNoJogo.SinalizarPronto();
                    }
                }
                else
                {
                    CorrigirSuave(jogador, Time.deltaTime);
                }
            }

            _presenca.Atualizar(
                Time.deltaTime,
                jogador.transform.position,
                jogador.transform.eulerAngles.y
            );

            foreach (var avatar in _presenca.Avatares.Todos)
            {
                if (_visuais.TryGetValue(avatar.PersonagemId, out var visual) && visual != null)
                {
                    visual.transform.SetPositionAndRotation(
                        avatar.Posicao,
                        Quaternion.Euler(0f, avatar.RotacaoY, 0f)
                    );
                }
            }
        }

        private void OnDestroy()
        {
            if (Atual == this)
            {
                Atual = null;
            }

            LigarControle(null);
            _presenca?.Dispose();
            _presenca = null;
        }

        private void Iniciar(long personagemId)
        {
            _presenca = new PresencaNaCidade(new ConexaoDePresencaReal(), personagemId);
            _presenca.Avatares.Criado += CriarVisual;
            _presenca.Avatares.Removido += RemoverVisual;
            _presenca.VelocidadesDoServidor += (andar, correr) =>
            {
                _velocidades = new Vector2(andar, correr);
                _controle?.AplicarVelocidadesDoServidor(andar, correr);
            };
            _presenca.PosicaoForcada += (posicao, rotacao) =>
            {
                _correcaoPendente = true;
                _posicaoCorrigida = posicao;
                _rotacaoCorrigida = rotacao;
            };
            _presenca.PosicaoDoServidor += (posicao, rotacao) =>
            {
                _correcaoPendente = true;
                _posicaoInicialPendente = true;
                _posicaoCorrigida = posicao;
                _rotacaoCorrigida = rotacao;
            };

            // Se já havia conexão, o construtor já entrou na cidade; senão, conecta agora.
            if (!ConexaoTempoReal.Conectado)
            {
                ConexaoTempoReal.Conectar(personagemId);
            }
        }

        // Liga o controle do jogador atual (o jogador é recriado a cada entrada no mundo): a
        // velocidade do servidor vai para ele, e mudar o Inspector vira pedido ao servidor.
        private void LigarControle(GameObject jogador)
        {
            var controle = jogador != null ? jogador.GetComponent<ControleTerceiraPessoa>() : null;
            if (controle == _controle)
            {
                return;
            }

            if (_controle != null)
            {
                _controle.VelocidadesPedidasNoInspector -= PedirVelocidade;
            }

            _controle = controle;
            if (_controle != null)
            {
                _controle.VelocidadesPedidasNoInspector += PedirVelocidade;
                if (_velocidades is { } v)
                {
                    _controle.AplicarVelocidadesDoServidor(v.x, v.y);
                }
            }
        }

        private void PedirVelocidade(float andar, float correr) =>
            _presenca?.PedirVelocidadeDeTeste(andar, correr);

        private void Corrigir(GameObject jogador)
        {
            // Nascimento ou posição forçada pelo servidor: aplica sempre (ADR 0024).
            // O CharacterController desfaz posições definidas direto no transform enquanto ativo.
            var corpo = jogador.GetComponent<CharacterController>();
            if (corpo != null)
            {
                corpo.enabled = false;
            }

            jogador.transform.SetPositionAndRotation(
                _posicaoCorrigida,
                Quaternion.Euler(0f, _rotacaoCorrigida, 0f)
            );
            if (corpo != null)
            {
                corpo.enabled = true;
            }
        }

        private void CorrigirSuave(GameObject jogador, float delta)
        {
            var novaPos = Vector3.MoveTowards(
                jogador.transform.position,
                _posicaoCorrigida,
                ConfigInterpolacaoMundo.VelocidadeSuavizacaoCorrecao * delta
            );
            var novaRot = Mathf.MoveTowardsAngle(
                jogador.transform.eulerAngles.y,
                _rotacaoCorrigida,
                360f * delta
            );

            var corpo = jogador.GetComponent<CharacterController>();
            if (corpo != null)
            {
                corpo.enabled = false;
            }

            jogador.transform.SetPositionAndRotation(novaPos, Quaternion.Euler(0f, novaRot, 0f));
            if (corpo != null)
            {
                corpo.enabled = true;
            }

            if ((jogador.transform.position - _posicaoCorrigida).sqrMagnitude < 0.001f)
            {
                _correcaoPendente = false;
            }
        }

        private void CriarVisual(AvatarRemoto avatar)
        {
            var capsula = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            capsula.name = $"Jogador_{avatar.PersonagemId}";
            var colisor = capsula.GetComponent<Collider>();
            if (colisor != null)
            {
                Destroy(colisor);
            }

            // A cápsula do Unity tem 2 m de altura; o pivô fica nos pés.
            var raiz = new GameObject(capsula.name);
            capsula.name = "Capsula";
            capsula.transform.SetParent(raiz.transform, false);
            capsula.transform.localPosition = new Vector3(0f, EntradaNoJogo.AlturaJogador / 2f, 0f);
            capsula.transform.localScale = new Vector3(
                EntradaNoJogo.RaioJogador * 2f,
                EntradaNoJogo.AlturaJogador / 2f,
                EntradaNoJogo.RaioJogador * 2f
            );
            raiz.transform.SetPositionAndRotation(
                avatar.Posicao,
                Quaternion.Euler(0f, avatar.RotacaoY, 0f)
            );
            raiz.transform.SetParent(transform, true);
            _visuais[avatar.PersonagemId] = raiz;
        }

        /// <summary>
        /// Copia os outros jogadores da presença para o mapa. Todos estão na área da cidade:
        /// é a única presença que o client recebe.
        /// </summary>
        public void CopiarOutros(List<MarcadorDoMapa> destino)
        {
            destino.Clear();
            if (_presenca == null)
            {
                return;
            }

            foreach (var avatar in _presenca.Avatares.Todos)
            {
                destino.Add(
                    new MarcadorDoMapa(
                        avatar.Posicao.x,
                        avatar.Posicao.z,
                        avatar.RotacaoY,
                        ProjecaoDoMapa.AreaDaCidade,
                        false
                    )
                );
            }
        }

        private void RemoverVisual(AvatarRemoto avatar)
        {
            if (_visuais.TryGetValue(avatar.PersonagemId, out var visual))
            {
                _visuais.Remove(avatar.PersonagemId);
                if (visual != null)
                {
                    Destroy(visual);
                }
            }
        }
    }
}
