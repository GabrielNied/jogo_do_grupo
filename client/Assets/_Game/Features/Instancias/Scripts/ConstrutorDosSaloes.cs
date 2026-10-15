using JogoDoGrupo.Shared.Features.Instancias;
using JogoDoGrupo.Shared.Features.Mundo;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// Monta por código os salões da Mina do Reflexo (antecâmara, três salões, corredores e o
    /// salão do boss) no trecho vazio do mundo reservado por <see cref="LayoutDaMinaDoReflexo"/>.
    /// O chão é o próprio terreno, que o servidor também usa para a altura dos inimigos e dos
    /// avisos do boss; aqui só entram paredes, teto e luzes. Existe sempre na cena do mundo: quem
    /// está na cidade nunca chega lá, porque o servidor isola a instância em outra área.
    /// </summary>
    internal static class ConstrutorDosSaloes
    {
        public const string NomeRaiz = "MinaDoReflexo";

        /// <summary>Espessura de parede e teto (m).</summary>
        public const float Espessura = 1f;

        /// <summary>Comprimento máximo de cada pedaço de parede, que acompanha o relevo (m).</summary>
        public const float PassoDaParede = 6f;

        /// <summary>Quanto a parede afunda abaixo do ponto mais baixo do chão dela (m).</summary>
        public const float FolgaAbaixoDoChao = 3f;

        private static readonly Color CorDaRocha = new Color(0.3f, 0.28f, 0.27f);
        private static readonly Color CorDaLuz = new Color(1f, 0.72f, 0.42f);

        /// <summary>Altura do teto, igual em toda a dungeon: o ponto mais alto do chão mais o pé-direito.</summary>
        public static float AlturaDoTeto()
        {
            var maior = float.MinValue;
            foreach (var trecho in LayoutDaMinaDoReflexo.Trechos)
            {
                for (var x = trecho.De; x <= trecho.Ate; x += 4f)
                {
                    for (var z = -trecho.MetadeDaLargura; z <= trecho.MetadeDaLargura; z += 4f)
                    {
                        maior = Mathf.Max(
                            maior,
                            MapaDoMundo.AlturaEm(
                                LayoutDaMinaDoReflexo.OrigemX + x,
                                LayoutDaMinaDoReflexo.OrigemZ + z
                            )
                        );
                    }
                }
            }

            return maior + LayoutDaMinaDoReflexo.AlturaDoTeto;
        }

        public static GameObject Montar(Scene cena)
        {
            var raiz = new GameObject(NomeRaiz);
            SceneManager.MoveGameObjectToScene(raiz, cena);

            var rocha = CriarMaterial(CorDaRocha);
            var teto = AlturaDoTeto();
            var trechos = LayoutDaMinaDoReflexo.Trechos;
            for (var i = 0; i < trechos.Length; i++)
            {
                MontarTrecho(raiz.transform, rocha, teto, trechos, i);
            }

            return raiz;
        }

        private static void MontarTrecho(
            Transform pai,
            Material rocha,
            float teto,
            LayoutDaMinaDoReflexo.Trecho[] trechos,
            int indice
        )
        {
            var trecho = trechos[indice];
            var x0 = LayoutDaMinaDoReflexo.OrigemX + trecho.De;
            var x1 = LayoutDaMinaDoReflexo.OrigemX + trecho.Ate;
            var z0 = LayoutDaMinaDoReflexo.OrigemZ - trecho.MetadeDaLargura;
            var z1 = LayoutDaMinaDoReflexo.OrigemZ + trecho.MetadeDaLargura;

            var grupo = new GameObject($"{trecho.Tipo}_{indice}").transform;
            grupo.SetParent(pai, false);

            // Paredes compridas, um pedaço a cada PassoDaParede para acompanhar o relevo.
            for (var x = x0; x < x1; x += PassoDaParede)
            {
                var fim = Mathf.Min(x + PassoDaParede, x1);
                ParedeAoLongoDeX(grupo, rocha, teto, x, fim, z0 - (Espessura * 0.5f), z0);
                ParedeAoLongoDeX(grupo, rocha, teto, x, fim, z1 + (Espessura * 0.5f), z1);
            }

            // Paredes das pontas: inteiras nas extremidades; entre trechos, só os lados do vão.
            Ponta(
                grupo,
                rocha,
                teto,
                x0,
                z0,
                z1,
                indice > 0 ? trechos[indice - 1] : (LayoutDaMinaDoReflexo.Trecho?)null,
                trecho,
                -1
            );
            Ponta(
                grupo,
                rocha,
                teto,
                x1,
                z0,
                z1,
                indice < trechos.Length - 1
                    ? trechos[indice + 1]
                    : (LayoutDaMinaDoReflexo.Trecho?)null,
                trecho,
                +1
            );

            // Teto: uma laje por trecho, na mesma altura em toda a dungeon.
            Bloco(
                grupo,
                rocha,
                "Teto",
                new Vector3((x0 + x1) * 0.5f, teto + (Espessura * 0.5f), (z0 + z1) * 0.5f),
                new Vector3(x1 - x0 + (2f * Espessura), Espessura, z1 - z0 + (2f * Espessura))
            );

            // Luz quente no meio do trecho, já que o teto tira o sol.
            var luz = new GameObject("Luz");
            luz.transform.SetParent(grupo, false);
            var chao = MapaDoMundo.AlturaEm((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);
            luz.transform.position = new Vector3((x0 + x1) * 0.5f, chao + 4f, (z0 + z1) * 0.5f);
            var componente = luz.AddComponent<Light>();
            componente.type = LightType.Point;
            componente.color = CorDaLuz;
            componente.intensity =
                trecho.Tipo == LayoutDaMinaDoReflexo.TipoDeTrecho.Corredor ? 1.5f : 3f;
            componente.range =
                trecho.Tipo == LayoutDaMinaDoReflexo.TipoDeTrecho.Corredor ? 14f : 26f;
            componente.shadows = LightShadows.None;
        }

        // Parede paralela a X, em zCentro, entre x0 e x1; zBorda é o ponto do chão que ela cerca.
        private static void ParedeAoLongoDeX(
            Transform pai,
            Material material,
            float teto,
            float x0,
            float x1,
            float zCentro,
            float zBorda
        )
        {
            var baixo =
                Mathf.Min(
                    MapaDoMundo.AlturaEm(x0, zBorda),
                    MapaDoMundo.AlturaEm((x0 + x1) * 0.5f, zBorda),
                    MapaDoMundo.AlturaEm(x1, zBorda)
                ) - FolgaAbaixoDoChao;
            Bloco(
                pai,
                material,
                "Parede",
                new Vector3((x0 + x1) * 0.5f, (baixo + teto) * 0.5f, zCentro),
                new Vector3(x1 - x0, teto - baixo, Espessura)
            );
        }

        // Parede paralela a Z, em xCentro, entre z0 e z1.
        private static void ParedeAoLongoDeZ(
            Transform pai,
            Material material,
            float teto,
            float xCentro,
            float xBorda,
            float z0,
            float z1
        )
        {
            var baixo =
                Mathf.Min(
                    MapaDoMundo.AlturaEm(xBorda, z0),
                    MapaDoMundo.AlturaEm(xBorda, (z0 + z1) * 0.5f),
                    MapaDoMundo.AlturaEm(xBorda, z1)
                ) - FolgaAbaixoDoChao;
            Bloco(
                pai,
                material,
                "Parede",
                new Vector3(xCentro, (baixo + teto) * 0.5f, (z0 + z1) * 0.5f),
                new Vector3(Espessura, teto - baixo, z1 - z0)
            );
        }

        // Uma ponta do trecho: sem vizinho fecha tudo; com vizinho, o mais largo fecha só os
        // lados do vão (a largura do vão é a do mais estreito).
        private static void Ponta(
            Transform pai,
            Material material,
            float teto,
            float x,
            float z0,
            float z1,
            LayoutDaMinaDoReflexo.Trecho? vizinho,
            LayoutDaMinaDoReflexo.Trecho proprio,
            int lado
        )
        {
            var xParede = x + (lado * Espessura * 0.5f);
            var meio = (z0 + z1) * 0.5f;
            if (vizinho == null)
            {
                ParedeAoLongoDeZ(pai, material, teto, xParede, x, z0, z1);
                return;
            }

            var vao = Mathf.Min(proprio.MetadeDaLargura, vizinho.Value.MetadeDaLargura);
            if (proprio.MetadeDaLargura <= vao)
            {
                return;
            }

            ParedeAoLongoDeZ(pai, material, teto, xParede, x, z0, meio - vao);
            ParedeAoLongoDeZ(pai, material, teto, xParede, x, meio + vao, z1);
        }

        private static void Bloco(
            Transform pai,
            Material material,
            string nome,
            Vector3 centro,
            Vector3 tamanho
        )
        {
            var bloco = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bloco.name = nome;
            bloco.transform.SetParent(pai, false);
            bloco.transform.position = centro;
            bloco.transform.localScale = tamanho;
            bloco.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material CriarMaterial(Color cor)
        {
            var modelo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var material = new Material(modelo.GetComponent<Renderer>().sharedMaterial)
            {
                color = cor,
            };
            Object.Destroy(modelo);
            return material;
        }
    }
}
