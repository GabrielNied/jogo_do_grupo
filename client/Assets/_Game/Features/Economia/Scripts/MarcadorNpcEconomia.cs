using JogoDoGrupo.Shared.Features.Economia;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Economia
{
    /// <summary>Tipo de NPC de economia no hub (marcador procedural até haver modelo).</summary>
    public enum TipoNpcEconomia
    {
        Vendedor,
        Ferreiro,
        Armeiro,
        Armadureiro,
        Taverneiro,
        Merceeiro,
        Leiloeiro,
        BancoGuilda,
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
                TipoNpcEconomia.Armeiro => new Vector3(
                    ConfigNpcEconomia.ArmeiroX,
                    altura,
                    ConfigNpcEconomia.ArmeiroZ
                ),
                TipoNpcEconomia.Armadureiro => new Vector3(
                    ConfigNpcEconomia.ArmadureiroX,
                    altura,
                    ConfigNpcEconomia.ArmadureiroZ
                ),
                TipoNpcEconomia.Taverneiro => new Vector3(
                    ConfigNpcEconomia.TaverneiroX,
                    altura,
                    ConfigNpcEconomia.TaverneiroZ
                ),
                TipoNpcEconomia.Merceeiro => new Vector3(
                    ConfigNpcEconomia.MerceeiroX,
                    altura,
                    ConfigNpcEconomia.MerceeiroZ
                ),
                TipoNpcEconomia.Ferreiro => new Vector3(
                    ConfigNpcEconomia.FerreiroX,
                    altura,
                    ConfigNpcEconomia.FerreiroZ
                ),
                TipoNpcEconomia.Leiloeiro => new Vector3(
                    ConfigMercado.LeiloeiroX,
                    altura,
                    ConfigMercado.LeiloeiroZ
                ),
                TipoNpcEconomia.BancoGuilda => new Vector3(
                    ConfigBancoGuilda.BancoGuildaX,
                    altura,
                    ConfigBancoGuilda.BancoGuildaZ
                ),
                _ => new Vector3(ConfigNpcEconomia.VendedorX, altura, ConfigNpcEconomia.VendedorZ),
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
            var nome = tipo switch
            {
                TipoNpcEconomia.Armeiro => "NpcArmeiro",
                TipoNpcEconomia.Armadureiro => "NpcArmadureiro",
                TipoNpcEconomia.Taverneiro => "NpcTaverneiro",
                TipoNpcEconomia.Merceeiro => "NpcMerceeiro",
                TipoNpcEconomia.Ferreiro => "NpcFerreiro",
                TipoNpcEconomia.Leiloeiro => "NpcLeiloeiro",
                TipoNpcEconomia.BancoGuilda => "NpcBancoGuilda",
                _ => "NpcVendedor",
            };
            var cor = tipo switch
            {
                TipoNpcEconomia.Armeiro => new Color(0.85f, 0.30f, 0.25f),
                TipoNpcEconomia.Armadureiro => new Color(0.40f, 0.55f, 0.70f),
                TipoNpcEconomia.Taverneiro => new Color(0.85f, 0.60f, 0.20f),
                TipoNpcEconomia.Merceeiro => new Color(0.95f, 0.75f, 0.25f),
                TipoNpcEconomia.Ferreiro => new Color(0.55f, 0.35f, 0.22f),
                TipoNpcEconomia.Leiloeiro => new Color(0.25f, 0.65f, 0.95f),
                TipoNpcEconomia.BancoGuilda => new Color(0.95f, 0.80f, 0.20f),
                _ => new Color(0.25f, 0.55f, 0.85f),
            };

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
