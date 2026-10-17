using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Mundo;
using JogoDoGrupo.Shared.Features.Mundo;
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

        private readonly System.Collections.Generic.Dictionary<long, AnimadorDoVisual> _animadores =
            new System.Collections.Generic.Dictionary<long, AnimadorDoVisual>();

        private EtiquetasDeNomes _nomes;
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

        /// <summary>Identificador da área atual do jogador local.</summary>
        public string AreaAtual => _presenca?.AreaAtual ?? RotulosDeArea.IdCidade;

        /// <summary>Aliados do grupo em outra área (#903).</summary>
        public System.Collections.Generic.IReadOnlyList<AliadoEmOutraArea> AliadosEmOutraArea =>
            _presenca?.AliadosEmOutraArea ?? System.Array.Empty<AliadoEmOutraArea>();

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
            if (
                _presenca != null
                && _presenca.EstaCaido
                && _controle != null
                && _controle.InputAtivo
            )
            {
                _controle.InputAtivo = false;
            }

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
                    // A animação segue a velocidade interpolada, a mesma que move o corpo: sem
                    // deslizar parado nem andar no lugar.
                    if (_animadores.TryGetValue(avatar.PersonagemId, out var animador))
                    {
                        animador.Atualizar(avatar.Velocidade, true);
                    }
                }
            }

            _nomes ??= EtiquetasDeNomes.Criar(transform);
            _nomes.Mostrar(_presenca.Avatares.Todos, Camera.main);
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
            _presenca.Avatares.IdentidadeMudou += RecriarVisual;
            _presenca.EstadoDeQuedaMudou += AoMudarQueda;
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
                if (_presenca != null && _presenca.EstaCaido)
                {
                    _controle.InputAtivo = false;
                }
                else if (_velocidades is { } v)
                {
                    _controle.AplicarVelocidadesDoServidor(v.x, v.y);
                }
            }
        }

        private void AoMudarQueda(bool estaCaido)
        {
            if (_controle != null)
            {
                _controle.InputAtivo = !estaCaido;
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

        // O visual da raça (gancho do Core, ou cápsula de reserva) vira filho de uma raiz
        // "Jogador_<id>": Combate acha o Animator por esse nome e Itens prende a arma nela.
        private void CriarVisual(AvatarRemoto avatar)
        {
            var raiz = new GameObject($"Jogador_{avatar.PersonagemId}");
            raiz.transform.SetPositionAndRotation(
                avatar.Posicao,
                Quaternion.Euler(0f, avatar.RotacaoY, 0f)
            );
            raiz.transform.SetParent(transform, true);

            var visual = EntradaNoJogo.CriarVisualDeOutroJogador(avatar.Raca);
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            if (!visual.activeSelf)
            {
                visual.SetActive(true);
            }

            // Quem colide é o corpo do próprio jogador; o avatar é só imagem.
            foreach (var colisor in visual.GetComponentsInChildren<Collider>())
            {
                if (!colisor.isTrigger)
                {
                    colisor.enabled = false;
                }
            }

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            _visuais[avatar.PersonagemId] = raiz;
            _animadores[avatar.PersonagemId] = new AnimadorDoVisual(animator);
        }

        // O servidor informou outra raça de quem já aparecia: troca o modelo.
        private void RecriarVisual(AvatarRemoto avatar)
        {
            RemoverVisual(avatar);
            CriarVisual(avatar);
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

            // Aliados do grupo fora da área de interesse: só ponto no mapa (#789).
            foreach (var aliado in _presenca.AliadosLonge)
            {
                if (!_presenca.Avatares.TentarObter(aliado.PersonagemId, out _))
                {
                    destino.Add(
                        new MarcadorDoMapa(
                            aliado.X,
                            aliado.Z,
                            0f,
                            ProjecaoDoMapa.AreaDaCidade,
                            false,
                            true
                        )
                    );
                }
            }
        }

        private void RemoverVisual(AvatarRemoto avatar)
        {
            _animadores.Remove(avatar.PersonagemId);
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
