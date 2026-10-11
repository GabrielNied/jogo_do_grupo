using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// Boca nas colinas e interior à parte (piso em <see cref="PrimeiraCaverna.InteriorY"/>).
    /// Atravessar a boca leva para dentro; a saída devolve à mesma boca. O client informa a
    /// nova posição no movimento de sempre: o servidor guarda, sem puxar de volta.
    /// </summary>
    internal sealed class Caverna : MonoBehaviour
    {
        private bool _dentro;
        private float _liberaEm;

        public static void Montar(Transform pai)
        {
            var raiz = new GameObject(ConstrutorHub.NomeCaverna);
            raiz.transform.SetParent(pai, false);
            raiz.AddComponent<Caverna>();

            var y = PrimeiraCaverna.EntradaY;
            Pilar(raiz.transform, PrimeiraCaverna.EntradaX - 1.6f, y, PrimeiraCaverna.EntradaZ);
            Pilar(raiz.transform, PrimeiraCaverna.EntradaX + 1.6f, y, PrimeiraCaverna.EntradaZ);
            Bloco(
                raiz.transform,
                new Vector3(PrimeiraCaverna.EntradaX, y + 2.4f, PrimeiraCaverna.EntradaZ),
                new Vector3(4f, 0.4f, 0.6f),
                false
            );

            var piso = PrimeiraCaverna.InteriorY;
            Bloco(
                raiz.transform,
                new Vector3(
                    PrimeiraCaverna.InteriorX,
                    piso - 0.25f,
                    PrimeiraCaverna.InteriorZ + 8f
                ),
                new Vector3(8f, 0.5f, 20f),
                true
            );
            Bloco(
                raiz.transform,
                new Vector3(
                    PrimeiraCaverna.InteriorX - PrimeiraCaverna.MeiaLargura,
                    piso + 1.5f,
                    PrimeiraCaverna.InteriorZ + 8f
                ),
                new Vector3(0.4f, 3f, 20f),
                true
            );
            Bloco(
                raiz.transform,
                new Vector3(
                    PrimeiraCaverna.InteriorX + PrimeiraCaverna.MeiaLargura,
                    piso + 1.5f,
                    PrimeiraCaverna.InteriorZ + 8f
                ),
                new Vector3(0.4f, 3f, 20f),
                true
            );
            Bloco(
                raiz.transform,
                new Vector3(PrimeiraCaverna.InteriorX, piso + 3f, PrimeiraCaverna.InteriorZ + 8f),
                new Vector3(6f, 0.3f, 20f),
                true
            );
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null || Time.unscaledTime < _liberaEm)
            {
                return;
            }

            var posicao = jogador.transform.position;
            if (!_dentro && PrimeiraCaverna.NaBoca(posicao.x, posicao.z))
            {
                Levar(
                    jogador,
                    new Vector3(
                        PrimeiraCaverna.NascimentoX,
                        PrimeiraCaverna.InteriorY,
                        PrimeiraCaverna.NascimentoZ
                    )
                );
                _dentro = true;
                _liberaEm = Time.unscaledTime + 0.4f;
                return;
            }

            if (_dentro && PrimeiraCaverna.NaSaida(posicao.x, posicao.z))
            {
                Levar(
                    jogador,
                    new Vector3(
                        PrimeiraCaverna.EntradaX,
                        PrimeiraCaverna.EntradaY,
                        PrimeiraCaverna.EntradaZ
                    )
                );
                _dentro = false;
                _liberaEm = Time.unscaledTime + 1.2f;
            }
        }

        private static void Levar(GameObject jogador, Vector3 destino)
        {
            var corpo = jogador.GetComponent<CharacterController>();
            if (corpo != null)
            {
                corpo.enabled = false;
            }

            jogador.transform.position = destino;
            if (corpo != null)
            {
                corpo.enabled = true;
            }
        }

        private static void Pilar(Transform pai, float x, float y, float z)
        {
            Bloco(pai, new Vector3(x, y + 1.2f, z), new Vector3(0.6f, 2.4f, 0.6f), false);
        }

        private static void Bloco(Transform pai, Vector3 centro, Vector3 escala, bool solido)
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = "Bloco";
            bloco.transform.SetParent(pai, false);
            bloco.transform.localPosition = centro;
            bloco.transform.localScale = escala;
            if (!solido)
            {
                Object.DestroyImmediate(bloco.GetComponent<Collider>());
            }
        }
    }
}
