using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Itens.Visual
{
    /// <summary>Marca o modelo de arma que a Itens pôs num avatar (para tirar na próxima troca).</summary>
    public sealed class ArmaNoAvatar : MonoBehaviour
    {
        public PosicaoEquipamento Posicao;
        public EncaixeDoVisual Encaixe;
    }

    /// <summary>
    /// Põe, troca e tira os modelos de arma no corpo de um avatar, a partir do estado inteiro que o
    /// servidor mandou (<see cref="VisualEquipado"/>). O encaixe é achado pelo nome no visual do
    /// personagem (<see cref="RegrasVisualEquipado.NomeDoEncaixe"/>, criados pelo gerador do
    /// Personagem). Avatar sem encaixe (a cápsula dos outros jogadores, por enquanto) ganha um
    /// encaixe criado aqui, preso ao objeto raiz, na altura do peito.
    /// </summary>
    public static class ApresentadorDoVisual
    {
        /// <summary>Onde o encaixe criado na raiz fica (em metros, com o avatar de frente para +Z).</summary>
        public static readonly Vector3 MaoDireitaNaRaiz = new Vector3(0.45f, 0.95f, 0.15f);

        public static readonly Vector3 CostasNaRaiz = new Vector3(0f, 1.25f, -0.3f);

        /// <summary>
        /// Giro do modelo nas costas, em graus no eixo Z do encaixe: a arma fica na diagonal, não
        /// espetada para cima. A mão não gira (o osso da palma já tem a pose).
        /// </summary>
        public const float GiroNasCostas = 40f;

        /// <summary>Aplica o estado: tira o que estava e põe o que veio. Devolve quantos modelos ficaram.</summary>
        public static int Aplicar(Transform avatar, VisualEquipado visual)
        {
            if (avatar == null)
            {
                return 0;
            }

            Limpar(avatar);
            var postos = 0;
            if (visual?.Armas == null)
            {
                return 0;
            }

            foreach (var arma in visual.Armas)
            {
                var encaixe = RegrasVisualEquipado.EncaixeDe(arma.Posicao, visual.ConjuntoAtivo);
                if (encaixe == null)
                {
                    continue;
                }

                var modelo = ModelosDeArma.Criar(arma.Tipo);
                if (modelo == null)
                {
                    continue;
                }

                var pai = AcharOuCriarEncaixe(avatar, encaixe.Value);
                modelo.transform.SetParent(pai, false);
                if (encaixe.Value == EncaixeDoVisual.Costas)
                {
                    // Nas costas o meio da arma fica no encaixe (o cabo está na origem do modelo).
                    var giro = Quaternion.Euler(0f, 0f, GiroNasCostas);
                    modelo.transform.localRotation = giro;
                    modelo.transform.localPosition =
                        giro * new Vector3(0f, -ModelosDeArma.Comprimento(arma.Tipo) / 2f, 0f);
                }
                else
                {
                    modelo.transform.localPosition = Vector3.zero;
                    modelo.transform.localRotation = Quaternion.identity;
                }

                var marca = modelo.AddComponent<ArmaNoAvatar>();
                marca.Posicao = arma.Posicao;
                marca.Encaixe = encaixe.Value;
                postos++;
            }

            return postos;
        }

        /// <summary>Tira todos os modelos de arma do avatar.</summary>
        public static void Limpar(Transform avatar)
        {
            foreach (var arma in avatar.GetComponentsInChildren<ArmaNoAvatar>(true))
            {
                // Destroy só vale no fim do frame: solta antes, para a conta e a busca não a verem.
                arma.transform.SetParent(null, false);
                arma.gameObject.SetActive(false);
                if (Application.isPlaying)
                {
                    Object.Destroy(arma.gameObject);
                }
                else
                {
                    Object.DestroyImmediate(arma.gameObject);
                }
            }
        }

        /// <summary>O encaixe pelo nome (em qualquer profundidade), ou um novo preso à raiz.</summary>
        public static Transform AcharOuCriarEncaixe(Transform avatar, EncaixeDoVisual encaixe)
        {
            var nome = RegrasVisualEquipado.NomeDoEncaixe(encaixe);
            var achado = Procurar(avatar, nome);
            if (achado != null)
            {
                return achado;
            }

            var criado = new GameObject(nome).transform;
            criado.SetParent(avatar, false);
            criado.localPosition =
                encaixe == EncaixeDoVisual.MaoDireita ? MaoDireitaNaRaiz : CostasNaRaiz;
            return criado;
        }

        private static Transform Procurar(Transform raiz, string nome)
        {
            foreach (var filho in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (filho.name == nome)
                {
                    return filho;
                }
            }

            return null;
        }
    }
}
