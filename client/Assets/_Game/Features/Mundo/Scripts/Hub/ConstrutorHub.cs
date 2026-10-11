using JogoDoGrupo.Client.Core.Jogo;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Mundo.Hub
{
    /// <summary>
    /// Monta o hub (área segura) em tempo de execução a partir dos prefabs de
    /// <c>Resources/Mundo/</c> e registra-se no Core como construtor de mundo. O Core chama
    /// <see cref="Construir"/> quando o jogador entra; tudo o que é criado vai para a cena do
    /// mundo, que o Core descarrega ao sair.
    /// </summary>
    public static class ConstrutorHub
    {
        public const string NomeRaiz = "Hub";
        public const string NomeChao = "Chao";
        public const string NomeMuros = "Muros";
        public const string NomeEstradas = "Estradas";
        public const string NomeProps = "Props";
        public const string NomeTerreno = "TerrenoFora";
        public const string NomePropsFora = "PropsFora";
        public const string NomeLuz = "Sol";

        private const string PastaPrefabs = "Mundo/";

        // Os prefabs em Resources foram gerados com o módulo de 2 m. Enquanto o chão medido
        // for menor que LayoutDoHub.Lado, as peças são ampliadas para acompanhar o layout.
        private static float _escalaDasPecas = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            EntradaNoJogo.DefinirConstrutorDeMundo(Construir);
        }

        /// <summary>
        /// Cria o hub e devolve onde o jogador nasce (pés no chão, olhando para a fonte).
        /// </summary>
        public static PosicaoInicial Construir()
        {
            Montar();
            return new PosicaoInicial(
                LayoutDoHub.PontoDeNascimento,
                LayoutDoHub.RotacaoDeNascimento
            );
        }

        /// <summary>
        /// Cria os objetos do hub na cena ativa, sob um objeto <see cref="NomeRaiz"/>, que é
        /// devolvido. Peça sem prefab em <c>Resources</c> é pulada com um aviso; só o chão e os
        /// muros têm uma versão simples de reserva, para o hub nunca ficar sem colisão.
        /// </summary>
        public static GameObject Montar()
        {
            var raiz = new GameObject(NomeRaiz);
            _escalaDasPecas = MedirEscalaDasPecas();

            CriarChao(raiz.transform);
            CriarTerreno(raiz.transform);
            CriarMuros(raiz.transform);
            CriarEstradas(raiz.transform);
            CriarProps(raiz.transform);
            CriarPropsFora(raiz.transform);
            CriarLuz(raiz.transform);
            ConfigurarAmbiente();

            return raiz;
        }

        private static void CriarChao(Transform pai)
        {
            var chao = Instanciar(LayoutDoHub.PrefabChao, Vector3.zero, 0f, pai);
            if (chao == null)
            {
                chao = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chao.transform.SetParent(pai, false);
                chao.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                chao.transform.localScale = new Vector3(LayoutDoHub.Lado, 1f, LayoutDoHub.Lado);
            }

            chao.name = NomeChao;
        }

        // Terreno de ~1 km montado em código (o prefab Terreno_Fora continua no disco, no tamanho
        // antigo). As placas cobrem campo, mata e colinas sem tapar a cidade. Um colisor na
        // raiz e barreiras na borda. A face de cima fica em Y = 0.
        private static void CriarTerreno(Transform pai)
        {
            var terreno = new GameObject(NomeTerreno);
            terreno.transform.SetParent(pai, false);

            var metade = LayoutDoHub.Metade;
            var borda = LayoutDoHub.MetadeDoTerreno;
            var material = MaterialDoTerreno();
            var campo = new Color(0.55f, 0.68f, 0.34f);
            var mata = new Color(0.2f, 0.38f, 0.18f);
            var colinas = new Color(0.52f, 0.44f, 0.28f);

            // Norte inteiro: campo, para onde sai a estrada.
            CriarPlaca(terreno.transform, "Campo", -borda, borda, metade, borda, material, campo);
            // Leste e sul leste: mata. Oeste e sul oeste: colinas.
            CriarPlaca(terreno.transform, "Mata", metade, borda, -borda, metade, material, mata);
            CriarPlaca(terreno.transform, "Mata", 0f, metade, -borda, -metade, material, mata);
            CriarPlaca(
                terreno.transform,
                "Colinas",
                -borda,
                -metade,
                -borda,
                metade,
                material,
                colinas
            );
            CriarPlaca(
                terreno.transform,
                "Colinas",
                -metade,
                0f,
                -borda,
                -metade,
                material,
                colinas
            );

            var chao = terreno.AddComponent<BoxCollider>();
            chao.center = new Vector3(0f, -0.5f, 0f);
            chao.size = new Vector3(LayoutDoHub.LadoDoTerreno, 1f, LayoutDoHub.LadoDoTerreno);

            const float alturaDaBarreira = 20f;
            const float espessura = 1f;
            var deslocamento = borda + espessura / 2f;
            var lado = LayoutDoHub.LadoDoTerreno;
            CriarBarreira(
                terreno.transform,
                "Barreira_Norte",
                new Vector3(0f, alturaDaBarreira / 2f, deslocamento),
                new Vector3(lado + 2f * espessura, alturaDaBarreira, espessura)
            );
            CriarBarreira(
                terreno.transform,
                "Barreira_Sul",
                new Vector3(0f, alturaDaBarreira / 2f, -deslocamento),
                new Vector3(lado + 2f * espessura, alturaDaBarreira, espessura)
            );
            CriarBarreira(
                terreno.transform,
                "Barreira_Oeste",
                new Vector3(-deslocamento, alturaDaBarreira / 2f, 0f),
                new Vector3(espessura, alturaDaBarreira, lado)
            );
            CriarBarreira(
                terreno.transform,
                "Barreira_Leste",
                new Vector3(deslocamento, alturaDaBarreira / 2f, 0f),
                new Vector3(espessura, alturaDaBarreira, lado)
            );
        }

        private static void CriarPlaca(
            Transform pai,
            string nome,
            float x0,
            float x1,
            float z0,
            float z1,
            Material origem,
            Color cor
        )
        {
            var placa = GameObject.CreatePrimitive(PrimitiveType.Cube);
            placa.name = nome;
            placa.transform.SetParent(pai, false);
            placa.transform.localPosition = new Vector3((x0 + x1) * 0.5f, -0.5f, (z0 + z1) * 0.5f);
            placa.transform.localScale = new Vector3(x1 - x0, 1f, z1 - z0);

            var colisor = placa.GetComponent<Collider>();
            if (Application.isPlaying)
            {
                Object.Destroy(colisor);
            }
            else
            {
                Object.DestroyImmediate(colisor);
            }

            var renderizador = placa.GetComponent<Renderer>();
            var material = origem != null ? new Material(origem) : renderizador.material;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", cor);
            }

            material.color = cor;
            renderizador.sharedMaterial = material;
        }

        private static void CriarBarreira(
            Transform pai,
            string nome,
            Vector3 centro,
            Vector3 tamanho
        )
        {
            var barreira = new GameObject(nome);
            barreira.transform.SetParent(pai, false);
            barreira.transform.localPosition = centro;
            var caixa = barreira.AddComponent<BoxCollider>();
            caixa.size = tamanho;
        }

        private static Material MaterialDoTerreno()
        {
            var modelo = Resources.Load<GameObject>(PastaPrefabs + LayoutDoHub.PrefabTerreno);
            var renderizador =
                modelo != null ? modelo.GetComponentInChildren<Renderer>(true) : null;
            if (renderizador != null && renderizador.sharedMaterial != null)
            {
                return renderizador.sharedMaterial;
            }

            var chao = Resources.Load<GameObject>(PastaPrefabs + LayoutDoHub.PrefabChao);
            renderizador = chao != null ? chao.GetComponentInChildren<Renderer>(true) : null;
            return renderizador != null ? renderizador.sharedMaterial : null;
        }

        private static void CriarMuros(Transform pai)
        {
            var muros = new GameObject(NomeMuros).transform;
            muros.SetParent(pai, false);

            var ultimo = LayoutDoHub.CelulasPorLado - 1;
            for (var i = 0; i <= ultimo; i++)
            {
                var centro = LayoutDoHub.CentroDaCelula(i);
                var borda = i == 0 || i == ultimo;

                // Lados sul e norte inteiros; lados oeste e leste sem as células de canto.
                CriarMuro(muros, centro, LayoutDoHub.CentroDaCelula(0), borda);

                // O lado norte tem o portão: o vão não tem muro e as duas células vizinhas viram
                // torres.
                if (!LayoutDoHub.EhVaoDoPortao(centro))
                {
                    CriarMuro(
                        muros,
                        centro,
                        LayoutDoHub.CentroDaCelula(ultimo),
                        borda || LayoutDoHub.EhTorreDoPortao(centro)
                    );
                }

                if (!borda)
                {
                    CriarMuro(muros, LayoutDoHub.CentroDaCelula(0), centro, false);
                    CriarMuro(muros, LayoutDoHub.CentroDaCelula(ultimo), centro, false);
                }
            }
        }

        private static void CriarMuro(Transform pai, float x, float z, bool torre)
        {
            var posicao = new Vector3(x, 0f, z);
            var nome = torre ? LayoutDoHub.PrefabTorre : LayoutDoHub.PrefabMuro;
            var altura = torre ? LayoutDoHub.AlturaTorre : LayoutDoHub.AlturaMuro;

            if (Instanciar(nome, posicao, 0f, pai) != null)
            {
                return;
            }

            var reserva = GameObject.CreatePrimitive(PrimitiveType.Cube);
            reserva.name = nome;
            reserva.transform.SetParent(pai, false);
            reserva.transform.localPosition = posicao + new Vector3(0f, altura / 2f, 0f);
            reserva.transform.localScale = new Vector3(
                LayoutDoHub.Modulo,
                altura,
                LayoutDoHub.Modulo
            );
        }

        private static void CriarEstradas(Transform pai)
        {
            var estradas = new GameObject(NomeEstradas).transform;
            estradas.SetParent(pai, false);

            for (var i = 0; i < LayoutDoHub.CelulasPorLado; i++)
            {
                for (var j = 0; j < LayoutDoHub.CelulasPorLado; j++)
                {
                    var x = LayoutDoHub.CentroDaCelula(i);
                    var z = LayoutDoHub.CentroDaCelula(j);
                    if (!LayoutDoHub.EhEstradaDaCidade(x, z))
                    {
                        continue;
                    }

                    // Só células dentro da área andável, mais a do vão do portão: os muros
                    // ocupam o resto da borda.
                    var noPortao =
                        j == LayoutDoHub.CelulasPorLado - 1 && LayoutDoHub.EhVaoDoPortao(x);
                    if (!noPortao && !LayoutDoHub.DentroDaAreaAndavel(new Vector3(x, 0f, z), 0f))
                    {
                        continue;
                    }

                    // Um prefab que falta já foi avisado na primeira vez; não repete 100 vezes.
                    if (!CriarEstrada(estradas, x, z))
                    {
                        return;
                    }
                }
            }

            // A estrada que sai do portão, sobre o terreno de fora.
            var celulasFora = (int)(LayoutDoHub.ComprimentoDaEstradaFora / LayoutDoHub.Modulo);
            for (var k = 1; k <= celulasFora; k++)
            {
                var z = LayoutDoHub.Metade + LayoutDoHub.Modulo * (k - 0.5f);
                for (var i = 0; i < LayoutDoHub.CelulasPorLado; i++)
                {
                    var x = LayoutDoHub.CentroDaCelula(i);
                    if (LayoutDoHub.EhVaoDoPortao(x) && !CriarEstrada(estradas, x, z))
                    {
                        return;
                    }
                }
            }
        }

        private static bool CriarEstrada(Transform pai, float x, float z)
        {
            return Instanciar(LayoutDoHub.PrefabEstrada, new Vector3(x, 0f, z), 0f, pai) != null;
        }

        private static void CriarProps(Transform pai)
        {
            var props = new GameObject(NomeProps).transform;
            props.SetParent(pai, false);

            foreach (var peca in LayoutDoHub.Props)
            {
                Instanciar(peca.Prefab, new Vector3(peca.X, 0f, peca.Z), peca.Guinada, props);
            }
        }

        private static void CriarPropsFora(Transform pai)
        {
            var props = new GameObject(NomePropsFora).transform;
            props.SetParent(pai, false);

            foreach (var peca in LayoutDoHub.PropsFora)
            {
                Instanciar(peca.Prefab, new Vector3(peca.X, 0f, peca.Z), peca.Guinada, props);
            }
        }

        private static void CriarLuz(Transform pai)
        {
            var objeto = new GameObject(NomeLuz);
            objeto.transform.SetParent(pai, false);
            objeto.transform.localRotation = Quaternion.Euler(50f, -30f, 0f);

            var luz = objeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = new Color(1f, 0.96f, 0.86f);
            luz.intensity = 1.1f;
            luz.shadows = LightShadows.Soft;
        }

        // A cena do mundo é nova: sem skybox, a luz ambiente seria preta. Fica em tom frio e
        // suave, para as sombras do sol não ficarem pretas.
        private static void ConfigurarAmbiente()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.5f, 0.52f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.32f, 0.3f, 0.26f);
        }

        private static GameObject Instanciar(
            string prefab,
            Vector3 posicao,
            float guinada,
            Transform pai
        )
        {
            var modelo = Resources.Load<GameObject>(PastaPrefabs + prefab);
            if (modelo == null)
            {
                Debug.LogWarning($"[Mundo] Prefab '{PastaPrefabs}{prefab}' não encontrado.");
                return null;
            }

            var instancia = Object.Instantiate(modelo, pai);
            instancia.name = prefab;
            instancia.transform.SetLocalPositionAndRotation(
                posicao,
                Quaternion.Euler(0f, guinada, 0f)
            );
            instancia.transform.localScale = Vector3.one * _escalaDasPecas;
            return instancia;
        }

        // O cubo do chão foi gerado com a largura da cidade da época. A razão até o lado atual
        // é a escala de todas as peças (1 quando os prefabs já nascem no tamanho do layout).
        private static float MedirEscalaDasPecas()
        {
            var modelo = Resources.Load<GameObject>(PastaPrefabs + LayoutDoHub.PrefabChao);
            var renderizador =
                modelo != null ? modelo.GetComponentInChildren<Renderer>(true) : null;
            if (renderizador == null)
            {
                return 1f;
            }

            var largura = renderizador.transform.localScale.x;
            if (largura < 1f)
            {
                return 1f;
            }

            return LayoutDoHub.Lado / largura;
        }
    }
}
