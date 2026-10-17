using System.Collections.Generic;
using System.Threading;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Features.Mundo;
using JogoDoGrupo.Client.Features.Mundo.DiaENoite;
using JogoDoGrupo.Client.Features.Mundo.Presenca;
using JogoDoGrupo.Client.Features.Mundo.Terreno;
using JogoDoGrupo.Shared.Features.Mundo;
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
        public const string NomePecasDosBiomas = "PecasDosBiomas";
        public const string NomeEstruturas = "Estruturas";
        public const string NomeCidadesSecundarias = "CidadesSecundarias";
        public const string NomeBairrosFlorestais = "BairrosFlorestais";
        public const string NomeCaverna = "Caverna";
        public const string NomeLuz = "Sol";
        public const string NomeAmbiente = "Ambiente";
        public const string NomePreenchimento = "Preenchimento";
        public const string PrefabDaLanterna = "Lanterna";

        private const string PastaPrefabs = "Mundo/";

        // Os prefabs em Resources foram gerados com o módulo de 2 m. Enquanto o chão medido
        // for menor que LayoutDoHub.Lado, as peças são ampliadas para acompanhar o layout.
        private static float _escalaDasPecas = 1f;

        // Cópias tingidas dos materiais das peças, uma por material e bioma: as peças do mesmo
        // bioma dividem o mesmo material.
        private static readonly Dictionary<(Material, Bioma), Material> _materiaisTingidos =
            new Dictionary<(Material, Bioma), Material>();

        // Bairros provisórios que acompanham as clareiras do novo mapa.
        private static readonly (
            string Prefab,
            float X,
            float Z,
            float Guinada
        )[] BairrosDaFloresta =
        {
            ("Casa_Pedra_Larga", -77f, 44f, 24f),
            ("Casa_Madeira_Pequena", -59f, 12f, -63f),
            ("Casa_Madeira_Pequena", -91f, -25f, 52f),
            ("Barraca_Verde", -66f, -55f, -18f),
            ("Casa_Pedra_Pequena", -58f, 102f, -46f),
            ("Casa_Madeira_Pequena", -28f, 87f, 32f),
            ("Carroca", -89f, 65f, 75f),
            ("Casa_Madeira_Pequena", -112f, 82f, 34f),
            ("Casa_Pedra_Pequena", -145f, 59f, -28f),
            ("Barraca_Verde", -126f, 119f, 112f),
            ("Lanterna", -100f, 91f, 0f),
            ("Casa_Pedra_Larga", 70f, 62f, -28f),
            ("Casa_Madeira_Pequena", 98f, 33f, 41f),
            ("Casa_Pedra_Pequena", 63f, -23f, -49f),
            ("Barraca_Vermelha", 101f, -47f, 102f),
            ("Carroca", 50f, 91f, 122f),
            ("Casa_Madeira_Pequena", 111f, 101f, -42f),
            ("Casa_Pedra_Larga", 145f, 77f, 28f),
            ("Barraca_Vermelha", 123f, 132f, 158f),
            ("Lanterna", 95f, 109f, 0f),
            ("Casa_Pedra_Pequena", -27f, -73f, 38f),
            ("Casa_Madeira_Pequena", 21f, -80f, -40f),
            ("Casa_Pedra_Larga", 47f, -111f, 26f),
            ("Barraca_Verde", -51f, -118f, -110f),
            ("Carroca", 5f, -105f, 70f),
            ("Casa_Pedra_Pequena", -103f, -93f, 55f),
            ("Casa_Madeira_Pequena", -137f, -119f, -50f),
            ("Casa_Madeira_Pequena", 115f, -111f, -34f),
            ("Barraca_Verde", 145f, -87f, 96f),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Registrar()
        {
            EntradaNoJogo.DefinirConstrutorDeMundo(Construir);
            EntradaNoJogo.DefinirConstrutorDeMundoAsync(ConstruirAsync);
        }

        /// <summary>
        /// Cria o hub e devolve onde o jogador nasce (pés no chão, olhando para a fonte).
        /// </summary>
        public static PosicaoInicial Construir()
        {
            var raiz = Montar();
            // A presença vive sob o hub: some com a cena do mundo e conecta quando o jogador existir.
            var presenca = new GameObject("Presenca");
            presenca.AddComponent<PresencaNoMundo>();
            presenca.AddComponent<Minimapa>();
            presenca.AddComponent<InimigosConhecidosNoMapa>();
            presenca.transform.SetParent(raiz.transform, false);
            return new PosicaoInicial(
                LayoutDoHub.PontoDeNascimento,
                LayoutDoHub.RotacaoDeNascimento
            );
        }

        /// <summary>Entrada com o terreno dividido em quadros.</summary>
        public static async Awaitable<PosicaoInicial> ConstruirAsync(CancellationToken cancelar)
        {
            var raiz = await MontarAsync(cancelar);
            cancelar.ThrowIfCancellationRequested();
            var presenca = new GameObject("Presenca");
            presenca.AddComponent<PresencaNoMundo>();
            presenca.AddComponent<Minimapa>();
            presenca.AddComponent<InimigosConhecidosNoMapa>();
            presenca.transform.SetParent(raiz.transform, false);
            return new PosicaoInicial(
                LayoutDoHub.PontoDeNascimento,
                LayoutDoHub.RotacaoDeNascimento
            );
        }

        private static async Awaitable<GameObject> MontarAsync(CancellationToken cancelar)
        {
            var raiz = new GameObject(NomeRaiz);
            _escalaDasPecas = MedirEscalaDasPecas();
            CriarChao(raiz.transform);
            await Awaitable.NextFrameAsync(cancelar);

            var terreno = new GameObject(NomeTerreno);
            terreno.transform.SetParent(raiz.transform, false);
            await ConstrutorDoTerreno.ConstruirAsync(
                terreno.transform,
                MaterialDoTerreno(),
                cancelar
            );
            // Muros, estradas, props, peças dos biomas, estruturas e caverna: cada etapa cede o
            // quadro quando gasta o orçamento (o mesmo da vegetação, #502).
            foreach (var _ in MontarEtapas(raiz.transform, new OrcamentoDeQuadro()))
            {
                await Awaitable.NextFrameAsync(cancelar);
            }

            FinalizarMontagem(raiz.transform);
            return raiz;
        }

        /// <summary>
        /// Cria os objetos do hub na cena ativa, sob um objeto <see cref="NomeRaiz"/>, que é
        /// devolvido. Peça sem prefab em <c>Resources</c> é pulada com um aviso; só o chão e os
        /// muros têm uma versão simples de reserva, para o hub nunca ficar sem colisão.
        /// </summary>
        public static GameObject Montar()
        {
            return MontarComOrcamento(float.MaxValue, out _);
        }

        /// <summary>Monta o hub cedendo quadros quando o orçamento esgota (testes EditMode).</summary>
        internal static GameObject MontarComOrcamento(float orcamentoMs, out int cedencias)
        {
            cedencias = 0;
            var raiz = new GameObject(NomeRaiz);
            _escalaDasPecas = MedirEscalaDasPecas();

            CriarChao(raiz.transform);
            CriarTerreno(raiz.transform);
            foreach (var _ in MontarEtapas(raiz.transform, new OrcamentoDeQuadro(orcamentoMs)))
            {
                cedencias++;
            }

            FinalizarMontagem(raiz.transform);
            return raiz;
        }

        /// <summary>Limpa caches estáticos entre montagens nos testes.</summary>
        internal static void LimparCacheParaTeste()
        {
            _materiaisTingidos.Clear();
            MontadorDaVegetacao.Limpar();
        }

        /// <summary>Monta só as etapas pós-terreno, com a escala medida (testes EditMode).</summary>
        internal static (int Objetos, int Etapas) MontarEtapasParaTeste(float orcamentoMs)
        {
            _escalaDasPecas = MedirEscalaDasPecas();
            var raiz = new GameObject("RaizDeTeste");
            var etapas = 0;
            foreach (var _ in MontarEtapas(raiz.transform, new OrcamentoDeQuadro(orcamentoMs)))
            {
                etapas++;
            }

            var objetos = raiz.GetComponentsInChildren<Transform>(true).Length;
            Object.DestroyImmediate(raiz);
            return (objetos, etapas);
        }

        private static void FinalizarMontagem(Transform raiz)
        {
            var sol = CriarLuz(raiz);
            var preenchimento = CriarPreenchimento(raiz);
            ConfigurarAmbiente(raiz, sol, preenchimento);
        }

        /// <summary>
        /// Tudo o que vem depois do chão e do terreno, em etapas: cada <c>yield</c> é um ponto
        /// onde o orçamento do quadro esgotou e dá para ceder a vez. Quem quiser tudo de uma vez
        /// só percorre o enumerador.
        /// </summary>
        internal static IEnumerable<int> MontarEtapas(Transform raiz, OrcamentoDeQuadro orcamento)
        {
            foreach (var passo in CriarMuros(raiz, orcamento))
            {
                yield return passo;
            }

            foreach (var passo in CriarEstradas(raiz, orcamento))
            {
                yield return passo;
            }

            foreach (var passo in CriarProps(raiz, orcamento))
            {
                yield return passo;
            }

            foreach (var passo in CriarPropsFora(raiz, orcamento))
            {
                yield return passo;
            }

            var pecas = CriarRaizDasPecasDosBiomas(raiz);
            foreach (var passo in MontadorDaVegetacao.Montar(pecas, _escalaDasPecas, orcamento))
            {
                yield return passo;
            }

            foreach (var passo in CriarEstruturas(raiz, orcamento))
            {
                yield return passo;
            }

            CriarCidadesSecundarias(raiz);
            CriarBairrosFlorestais(raiz);

            foreach (var passo in Caverna.MontarPorEtapas(raiz, orcamento))
            {
                yield return passo;
            }
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

        // Até 2026-10-11 o terreno era de ~1 km, plano, feito de placas coloridas (campo, mata e
        // colinas) com um colisor só. Agora é o mundo de 3 km com relevo do MapaDoMundo: o
        // ConstrutorDoTerreno cria o chão em blocos (cada um com o seu colisor), a água, a lava e
        // as barreiras da borda. O prefab Terreno_Fora continua no disco e só empresta o material.
        private static void CriarTerreno(Transform pai)
        {
            var terreno = new GameObject(NomeTerreno);
            terreno.transform.SetParent(pai, false);
            ConstrutorDoTerreno.Construir(terreno.transform, MaterialDoTerreno());
        }

        // Placa plana e colorida do terreno antigo e, logo abaixo, a barreira dele. O jogo não
        // usa mais nenhuma das duas (o chão e as barreiras agora são do ConstrutorDoTerreno);
        // ficam aqui para quem precisar de um chão plano de teste.
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

        private static IEnumerable<int> CriarMuros(Transform pai, OrcamentoDeQuadro orcamento)
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

                if (orcamento.Esgotou())
                {
                    yield return i;
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

        private static IEnumerable<int> CriarEstradas(Transform pai, OrcamentoDeQuadro orcamento)
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
                        yield break;
                    }

                    if (orcamento.Esgotou())
                    {
                        yield return i;
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
                        yield break;
                    }
                }
            }
        }

        private static bool CriarEstrada(Transform pai, float x, float z)
        {
            return Instanciar(LayoutDoHub.PrefabEstrada, new Vector3(x, 0f, z), 0f, pai) != null;
        }

        private static IEnumerable<int> CriarProps(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var props = new GameObject(NomeProps).transform;
            props.SetParent(pai, false);

            foreach (var peca in LayoutDoHub.Props)
            {
                var instancia = Instanciar(
                    peca.Prefab,
                    new Vector3(peca.X, 0f, peca.Z),
                    peca.Guinada,
                    props
                );
                if (instancia != null && peca.Prefab == PrefabDaLanterna)
                {
                    CalibrarLanterna(instancia);
                }

                if (orcamento.Esgotou())
                {
                    yield return 0;
                }
            }
        }

        // O prefab da lanterna traz uma luz de ponto, mas gravada para o módulo antigo (fraca e de
        // alcance curto). Aqui ela é calibrada para o módulo de 4 m: cor de chama, força para
        // iluminar a base do poste e alcance de cinco módulos. Sem sombra (são muitas luzes).
        private static void CalibrarLanterna(GameObject lanterna)
        {
            var luz = lanterna.GetComponentInChildren<Light>(true);
            if (luz == null)
            {
                var objeto = new GameObject("Luz");
                objeto.transform.SetParent(lanterna.transform, false);
                objeto.transform.localPosition = new Vector3(
                    0f,
                    ConfigIluminacao.AlturaDaLuzDaLanterna / _escalaDasPecas,
                    0f
                );
                luz = objeto.AddComponent<Light>();
            }

            // Acende à noite e apaga de dia, pela hora do mundo (#382).
            luz.enabled = LuzDaLanterna.Acesa(AmbienteDosBiomas.Hora);
            if (luz.GetComponent<LuzDaLanterna>() == null)
            {
                luz.gameObject.AddComponent<LuzDaLanterna>();
            }

            luz.type = LightType.Point;
            luz.color = ConfigIluminacao.CorDaLanterna;
            luz.intensity = ConfigIluminacao.IntensidadeDaLanterna;
            luz.range = ConfigIluminacao.AlcanceDaLanterna;
            luz.shadows = LightShadows.None;
        }

        private static IEnumerable<int> CriarPropsFora(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var props = new GameObject(NomePropsFora).transform;
            props.SetParent(pai, false);

            // As peças do miolo ficam apoiadas no relevo. As que cairiam dentro da água ou da
            // lava (onde um bioma de fora avança sobre o miolo) são puladas.
            foreach (var peca in LayoutDoHub.PropsFora)
            {
                if (
                    MapaDoMundo.PertoDeCidadeSecundaria(peca.X, peca.Z, 8f)
                    || CaminhosDoMundo.LivreDeVegetacao(peca.X, peca.Z)
                    || PertoDeBairroFlorestal(peca.X, peca.Z)
                    || PertoDeEstrutura(peca.X, peca.Z)
                    || !PecasDosBiomas.ManterPecaAntiga(peca.X, peca.Z)
                )
                {
                    continue;
                }

                var altura = MapaDoMundo.AlturaEm(peca.X, peca.Z);
                if (altura < MapaDoMundo.NivelDaAgua)
                {
                    continue;
                }

                var instancia = Instanciar(
                    peca.Prefab,
                    new Vector3(peca.X, altura, peca.Z),
                    peca.Guinada,
                    props
                );
                Tingir(instancia, MapaDoMundo.BiomaEm(peca.X, peca.Z));
                if (orcamento.Esgotou())
                {
                    yield return 0;
                }
            }
        }

        internal static bool PertoDeBairroFlorestal(float x, float z)
        {
            foreach (var peca in BairrosDaFloresta)
            {
                var dx = x - peca.X;
                var dz = z - peca.Z;
                if (dx * dx + dz * dz < 13f * 13f)
                {
                    return true;
                }
            }

            return false;
        }

        internal static bool PertoDeEstrutura(float x, float z)
        {
            foreach (var lugar in EstruturasDoMapa.Lugares)
            {
                foreach (var peca in lugar.Pecas)
                {
                    var dx = x - peca.X;
                    var dz = z - peca.Z;
                    if (dx * dx + dz * dz < 12f * 12f)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void CriarBairrosFlorestais(Transform pai)
        {
            var raiz = new GameObject(NomeBairrosFlorestais).transform;
            raiz.SetParent(pai, false);
            foreach (var peca in BairrosDaFloresta)
            {
                if (CaminhosDoMundo.LivreDeVegetacao(peca.X, peca.Z, 3f))
                {
                    continue;
                }

                var altura = MapaDoMundo.AlturaEm(peca.X, peca.Z);
                var instancia = Instanciar(
                    peca.Prefab,
                    new Vector3(peca.X, altura, peca.Z),
                    peca.Guinada,
                    raiz
                );
                if (instancia != null && peca.Prefab == PrefabDaLanterna)
                {
                    CalibrarLanterna(instancia);
                }
            }
        }

        // Ruína, acampamento, passagem e marco: coordenadas fixas, cada peça na altura do chão.
        private static IEnumerable<int> CriarEstruturas(Transform pai, OrcamentoDeQuadro orcamento)
        {
            var raiz = new GameObject(NomeEstruturas).transform;
            raiz.SetParent(pai, false);

            foreach (var lugar in EstruturasDoMapa.Lugares)
            {
                foreach (var peca in lugar.Pecas)
                {
                    Instanciar(
                        peca.Prefab,
                        new Vector3(peca.X, peca.Altura, peca.Z),
                        peca.Guinada,
                        raiz
                    );
                    if (orcamento.Esgotou())
                    {
                        yield return 0;
                    }
                }
            }
        }

        private static void CriarCidadesSecundarias(Transform pai)
        {
            var raiz = new GameObject(NomeCidadesSecundarias).transform;
            raiz.SetParent(pai, false);

            CriarVila(
                raiz,
                "PassoDaNeve",
                MapaDoMundo.CidadeDaNeveX,
                MapaDoMundo.CidadeDaNeveZ,
                MapaDoMundo.CidadeDaNeveAltura,
                Bioma.Neve,
                new (string Prefab, float X, float Z, float Guinada)[]
                {
                    ("Fonte", 0f, 0f, 0f),
                    ("Casa_Pedra_Larga", -26f, -17f, 35f),
                    ("Casa_Madeira_Pequena", 23f, -23f, -35f),
                    ("Casa_Madeira_Pequena", -24f, 22f, 145f),
                    ("Casa_Pedra_Larga", 25f, 21f, -145f),
                    ("Barraca_Verde", -9f, -27f, 20f),
                    ("Carroca", 11f, 28f, 110f),
                    ("Lanterna", -11f, 10f, 0f),
                    ("Lanterna", 12f, -9f, 0f),
                }
            );

            CriarVila(
                raiz,
                "OasisDoDeserto",
                MapaDoMundo.CidadeDoDesertoX,
                MapaDoMundo.CidadeDoDesertoZ,
                MapaDoMundo.CidadeDoDesertoAltura,
                Bioma.Deserto,
                new (string Prefab, float X, float Z, float Guinada)[]
                {
                    ("Fonte", 0f, 0f, 0f),
                    ("Casa_Pedra_Larga", -27f, -18f, 40f),
                    ("Casa_Pedra_Pequena", 25f, -24f, -35f),
                    ("Casa_Pedra_Pequena", -25f, 24f, 140f),
                    ("Casa_Pedra_Larga", 26f, 19f, -140f),
                    ("Barraca_Vermelha", -11f, -29f, 25f),
                    ("Barraca_Verde", 10f, 28f, 115f),
                    ("Carroca", 32f, 1f, 90f),
                    ("Lanterna", -10f, 10f, 0f),
                    ("Lanterna", 12f, -9f, 0f),
                }
            );
        }

        private static void CriarVila(
            Transform pai,
            string nome,
            float centroX,
            float centroZ,
            float altura,
            Bioma bioma,
            (string Prefab, float X, float Z, float Guinada)[] pecas
        )
        {
            var vila = new GameObject(nome).transform;
            vila.SetParent(pai, false);
            foreach (var peca in pecas)
            {
                var instancia = Instanciar(
                    peca.Prefab,
                    new Vector3(centroX + peca.X, altura, centroZ + peca.Z),
                    peca.Guinada,
                    vila
                );
                if (instancia == null)
                {
                    continue;
                }

                Tingir(instancia, bioma);
                if (peca.Prefab == PrefabDaLanterna)
                {
                    CalibrarLanterna(instancia);
                }
            }
        }

        // Árvores e pedras além do miolo de 1 km, por bioma, na altura do chão e na cor do bioma.
        // Não são um objeto por peça: o MontadorDaVegetacao registra cada uma no desenho por
        // blocos (malha e material divididos) e cria só o colisor.
        private static Transform CriarRaizDasPecasDosBiomas(Transform pai)
        {
            var pecas = new GameObject(NomePecasDosBiomas).transform;
            pecas.SetParent(pai, false);
            return pecas;
        }

        private static void CriarPecasDosBiomas(Transform pai)
        {
            var pecas = CriarRaizDasPecasDosBiomas(pai);
            foreach (var _ in MontadorDaVegetacao.Montar(pecas, _escalaDasPecas)) { }
        }

        /// <summary>Refaz só as peças naturais da prévia depois de editar uma região.</summary>
        public static void AtualizarVegetacaoDaPrevia(Transform raiz)
        {
            var antigas = raiz.Find(NomePropsFora);
            if (antigas != null)
            {
                foreach (Transform peca in antigas)
                {
                    var x = peca.localPosition.x;
                    var z = peca.localPosition.z;
                    peca.gameObject.SetActive(PecasDosBiomas.ManterPecaAntiga(x, z));
                    peca.localPosition = new Vector3(x, MapaDoMundo.AlturaEm(x, z), z);
                }
            }

            var atual = raiz.Find(NomePecasDosBiomas);
            if (atual != null)
            {
                Object.DestroyImmediate(atual.gameObject);
            }

            PecasDosBiomas.LimparCache();
            CriarPecasDosBiomas(raiz);
        }

        // Troca os materiais da peça por cópias multiplicadas pela cor do bioma. Branco (campo,
        // mata, colinas e cidade) deixa a peça como ela é.
        private static void Tingir(GameObject peca, Bioma bioma)
        {
            var cor = CoresDosBiomas.DasPecas(bioma);
            if (peca == null || cor == Color.white)
            {
                return;
            }

            foreach (var renderizador in peca.GetComponentsInChildren<Renderer>(true))
            {
                var materiais = renderizador.sharedMaterials;
                for (var i = 0; i < materiais.Length; i++)
                {
                    materiais[i] = MaterialTingido(materiais[i], bioma, cor);
                }

                renderizador.sharedMaterials = materiais;
            }
        }

        private static Material MaterialTingido(Material original, Bioma bioma, Color cor)
        {
            if (original == null)
            {
                return null;
            }

            var chave = (original, bioma);
            if (_materiaisTingidos.TryGetValue(chave, out var pronto) && pronto != null)
            {
                return pronto;
            }

            var tingido = new Material(original) { name = $"{original.name}_{bioma}" };
            if (tingido.HasProperty("_BaseColor"))
            {
                tingido.SetColor("_BaseColor", tingido.GetColor("_BaseColor") * cor);
            }
            else if (tingido.HasProperty("_Color"))
            {
                tingido.SetColor("_Color", tingido.GetColor("_Color") * cor);
            }

            _materiaisTingidos[chave] = tingido;
            return tingido;
        }

        private static Light CriarLuz(Transform pai)
        {
            var objeto = new GameObject(NomeLuz);
            objeto.transform.SetParent(pai, false);
            objeto.transform.localRotation = Quaternion.Euler(
                ConfigIluminacao.AlturaDoSol,
                ConfigIluminacao.GiroDoSol,
                0f
            );

            // Cor e força vêm do AmbienteDosBiomas. A sombra é suave e não chega a preto, para o
            // chão claro não estourar nem a mata ficar escura demais. Marcado como o sol da cena
            // para o URP não escolher outra luz direcional como principal.
            var luz = objeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.color = AmbienteDosBiomas.Padrao.Sol;
            luz.intensity = AmbienteDosBiomas.Padrao.ForcaDoSol;
            luz.shadows = LightShadows.Soft;
            luz.shadowStrength = ConfigIluminacao.ForcaDaSombra;
            RenderSettings.sun = luz;
            return luz;
        }

        // Luz de preenchimento: uma segunda luz direcional, sem sombra, do lado oposto ao do sol e
        // bem mais fraca, para o lado do personagem que fica fora do sol (e o rosto, com a câmera
        // atrás dele) não ficar só na sombra. A cor e a força acompanham o bioma.
        private static Light CriarPreenchimento(Transform pai)
        {
            var objeto = new GameObject(NomePreenchimento);
            objeto.transform.SetParent(pai, false);
            objeto.transform.localRotation = Quaternion.Euler(
                ConfigIluminacao.AlturaDoPreenchimento,
                ConfigIluminacao.GiroDoPreenchimento,
                0f
            );

            var luz = objeto.AddComponent<Light>();
            luz.type = LightType.Directional;
            luz.shadows = LightShadows.None;
            luz.color = AmbienteDosBiomas.CorDoPreenchimento(AmbienteDosBiomas.Padrao);
            luz.intensity = AmbienteDosBiomas.ForcaDoPreenchimento(AmbienteDosBiomas.Padrao);
            return luz;
        }

        // A cena do mundo é nova: sem skybox, a luz ambiente seria preta. Fica em tom frio e
        // suave, para as sombras do sol não ficarem pretas. Desde 2026-10-11 as cores moram no
        // AmbienteDosBiomas (o padrão dele é o que estava aqui), que também liga a névoa e troca
        // tudo conforme o bioma em que o jogador está.
        private static void ConfigurarAmbiente(Transform pai, Light sol, Light preenchimento)
        {
            var nascimento = LayoutDoHub.PontoDeNascimento;
            AmbienteDosBiomas.Aplicar(
                AmbienteDosBiomas.Em(nascimento.x, nascimento.z),
                sol,
                preenchimento
            );

            var objeto = new GameObject(NomeAmbiente);
            objeto.transform.SetParent(pai, false);
            objeto.AddComponent<AmbienteDosBiomas>().Configurar(sol, preenchimento);
            // A hora do servidor (#163): escurece o ambiente, gira o sol e acende as lanternas.
            objeto.AddComponent<CicloDoDia>();
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
