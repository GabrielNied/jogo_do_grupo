using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Presenca
{
    /// <summary>
    /// Liga a presença ao mundo: conecta ao entrar, mostra os outros jogadores como cápsulas e
    /// envia a posição do jogador local. Vive na cena do mundo, então some junto com ela.
    /// </summary>
    public sealed class PresencaNoMundo : MonoBehaviour
    {
        /// <summary>Acima desta distância (m) a posição do servidor vence a local.</summary>
        public const float DistanciaDeCorrecao = 3f;

        private readonly System.Collections.Generic.Dictionary<long, GameObject> _visuais =
            new System.Collections.Generic.Dictionary<long, GameObject>();

        private PresencaNaCidade _presenca;
        private bool _correcaoPendente;
        private Vector3 _posicaoCorrigida;
        private float _rotacaoCorrigida;

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

            if (_correcaoPendente)
            {
                _correcaoPendente = false;
                Corrigir(jogador);
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
            _presenca?.Dispose();
            _presenca = null;
        }

        private void Iniciar(long personagemId)
        {
            _presenca = new PresencaNaCidade(new ConexaoDePresencaReal(), personagemId);
            _presenca.Avatares.Criado += CriarVisual;
            _presenca.Avatares.Removido += RemoverVisual;
            _presenca.PosicaoDoServidor += (posicao, rotacao) =>
            {
                _correcaoPendente = true;
                _posicaoCorrigida = posicao;
                _rotacaoCorrigida = rotacao;
            };

            // Se já havia conexão, o construtor já entrou na cidade; senão, conecta agora.
            if (!ConexaoTempoReal.Conectado)
            {
                ConexaoTempoReal.Conectar(personagemId);
            }
        }

        private void Corrigir(GameObject jogador)
        {
            var distancia = Vector3.Distance(jogador.transform.position, _posicaoCorrigida);
            if (distancia <= DistanciaDeCorrecao)
            {
                return;
            }

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
