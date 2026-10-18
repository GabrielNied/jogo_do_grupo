using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>Tipo de NPC de economia no hub (marcador procedural até haver modelo).</summary>
    public enum TipoNpcEconomia
    {
        Vendedor,
        Ferreiro,
    }

    /// <summary>
    /// Marcador no chão e contas de proximidade alinhadas a <see cref="ConfigNpcEconomia"/>.
    /// </summary>
    public static class MarcadorNpcEconomia
    {
        public const float DistanciaParaInteragir = 3f;

        public static readonly Vector3 Tamanho = new Vector3(0.8f, 1.6f, 0.8f);

        public static Vector3 Posicao(TipoNpcEconomia tipo, float altura)
        {
            return tipo switch
            {
                TipoNpcEconomia.Vendedor => new Vector3(
                    ConfigNpcEconomia.VendedorX,
                    altura,
                    ConfigNpcEconomia.VendedorZ
                ),
                _ => new Vector3(ConfigNpcEconomia.FerreiroX, altura, ConfigNpcEconomia.FerreiroZ),
            };
        }

        public static bool EstaPerto(Vector3 jogador, Vector3 marcador)
        {
            var dx = jogador.x - marcador.x;
            var dz = jogador.z - marcador.z;
            return dx * dx + dz * dz <= DistanciaParaInteragir * DistanciaParaInteragir;
        }

        public static GameObject Criar(Vector3 posicao, TipoNpcEconomia tipo)
        {
            var nome = tipo == TipoNpcEconomia.Vendedor ? "NpcVendedor" : "NpcFerreiro";
            var cor =
                tipo == TipoNpcEconomia.Vendedor
                    ? new Color(0.25f, 0.55f, 0.85f)
                    : new Color(0.55f, 0.35f, 0.22f);

            var raiz = new GameObject(nome);
            raiz.transform.position = posicao;

            var colisor = raiz.AddComponent<CapsuleCollider>();
            colisor.height = Tamanho.y;
            colisor.radius = Tamanho.x * 0.5f;
            colisor.center = new Vector3(0f, Tamanho.y / 2f, 0f);

            var corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpo.name = "Corpo";
            corpo.transform.SetParent(raiz.transform, false);
            corpo.transform.localScale = Tamanho;
            corpo.transform.localPosition = new Vector3(0f, Tamanho.y / 2f, 0f);
            Object.Destroy(corpo.GetComponent<CapsuleCollider>());

            var renderer = corpo.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = cor;
            }

            return raiz;
        }
    }
}
