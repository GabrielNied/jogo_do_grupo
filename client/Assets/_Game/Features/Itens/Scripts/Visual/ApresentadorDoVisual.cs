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
                var (posLocal, rotLocal) = ObterTransformacao(arma.Tipo, encaixe.Value);
                modelo.transform.localPosition = posLocal;
                modelo.transform.localRotation = rotLocal;

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

        /// <summary>
        /// Posição e rotação calibradas por tipo de arma e encaixe, alinhando empunhadura e bainha às costas.
        /// </summary>
        public static (Vector3 posicao, Quaternion rotacao) ObterTransformacao(
            TipoEquipamento tipo,
            EncaixeDoVisual encaixe
        )
        {
            if (encaixe == EncaixeDoVisual.MaoDireita)
            {
                switch (tipo)
                {
                    case TipoEquipamento.Espada:
                        // Empunhada na mão: lâmina aponta para a frente/diagonal do punho, guarda alinhada.
                        var rotEspada = Quaternion.Euler(75f, 0f, 0f);
                        var posEspada = rotEspada * new Vector3(0f, -0.09f, 0f);
                        return (posEspada, rotEspada);

                    case TipoEquipamento.Martelo:
                        // Empunhado pela base do cabo: cabeça de metal para a frente.
                        var rotMartelo = Quaternion.Euler(75f, 0f, 0f);
                        var posMartelo = rotMartelo * new Vector3(0f, -0.20f, 0f);
                        return (posMartelo, rotMartelo);

                    case TipoEquipamento.Arco:
                        // Empunhado pelo centro (y = 0.45m): haste vertical; corda no -X do modelo → -Z (costas).
                        var rotArco = Quaternion.Euler(0f, -90f, 0f);
                        var posArco = rotArco * new Vector3(0f, -0.45f, 0f);
                        return (posArco, rotArco);

                    case TipoEquipamento.Cajado:
                        // Empunhado na terça superior (y = 0.70m), gema para cima/à frente.
                        var rotCajado = Quaternion.Euler(15f, 0f, 0f);
                        var posCajado = rotCajado * new Vector3(0f, -0.70f, 0f);
                        return (posCajado, rotCajado);

                    default:
                        return (Vector3.zero, Quaternion.identity);
                }
            }
            else // EncaixeDoVisual.Costas
            {
                var comprimento = ModelosDeArma.Comprimento(tipo);
                switch (tipo)
                {
                    case TipoEquipamento.Espada:
                        // Punho no ombro direito (fácil de sacar), lâmina descendo para o quadril esquerdo.
                        var rotEspada = Quaternion.Euler(0f, 0f, 142f);
                        var posEspada =
                            rotEspada * new Vector3(0f, -comprimento / 2f, 0f)
                            + new Vector3(0f, 0f, -0.07f);
                        return (posEspada, rotEspada);

                    case TipoEquipamento.Martelo:
                        // Cabeça perto do ombro direito, cabo para o quadril esquerdo.
                        var rotMartelo = Quaternion.Euler(0f, 0f, -38f);
                        var posMartelo =
                            rotMartelo * new Vector3(0f, -comprimento / 2f, 0f)
                            + new Vector3(0f, 0f, -0.09f);
                        return (posMartelo, rotMartelo);

                    case TipoEquipamento.Arco:
                        // O arco já está no plano X-Y. Deita plano nas costas, inclinado na diagonal.
                        var rotArco = Quaternion.Euler(0f, 0f, -35f);
                        var posArco =
                            rotArco * new Vector3(0f, -0.45f, 0f) + new Vector3(0f, 0f, -0.07f);
                        return (posArco, rotArco);

                    case TipoEquipamento.Cajado:
                        // Gema no ombro direito, haste descendo na diagonal.
                        var rotCajado = Quaternion.Euler(0f, 0f, -38f);
                        var posCajado =
                            rotCajado * new Vector3(0f, -comprimento / 2f, 0f)
                            + new Vector3(0f, 0f, -0.07f);
                        return (posCajado, rotCajado);

                    default:
                        var rotPadrao = Quaternion.Euler(0f, 0f, -GiroNasCostas);
                        var posPadrao =
                            rotPadrao * new Vector3(0f, -comprimento / 2f, 0f)
                            + new Vector3(0f, 0f, -0.07f);
                        return (posPadrao, rotPadrao);
                }
            }
        }
    }
}
