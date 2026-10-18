using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Chat;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Pets;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Pets
{
    /// <summary>
    /// Mostra os pets: o do jogador e o dos outros que o servidor diz que estão por perto. Só
    /// reage ao que o servidor manda (aparece, mudou, sumiu); quem decide onde o pet está é o
    /// servidor. Suaviza a posição entre os pacotes e dá um balanço de passo ao modelo.
    /// </summary>
    internal sealed class ApresentadorDePets : MonoBehaviour
    {
        /// <summary>Onde o gerador de editor põe o prefab: <c>Resources/Pets/Pet_&lt;especie&gt;</c>.</summary>
        public const string PrefixoDoPrefab = "Pets/Pet_";

        /// <summary>Nome do filho do prefab que recebe o balanço do passo.</summary>
        public const string NomeDoModelo = "Modelo";

        private sealed class PetNoClient
        {
            public long DonoId;
            public GameObject Raiz;
            public Transform Modelo;
            public Vector3 Alvo;
            public float AlvoRotacaoY;
            public Vector3 Velocidade;
            public bool Andando;
            public int Sequencia;
            public float FaseDoPasso;
        }

        private static ApresentadorDePets _instancia;

        private readonly Dictionary<long, PetNoClient> _pets = new Dictionary<long, PetNoClient>();
        private readonly HashSet<int> _colecao = new HashSet<int>();
        private readonly List<System.IDisposable> _assinaturas = new List<System.IDisposable>();

        /// <summary>Evento disparado quando a coleção de pets desbloqueados muda.</summary>
        public static event System.Action ColecaoMudou;

        /// <summary>Cópia do conjunto de espécies de pets desbloqueadas para a conta.</summary>
        public static HashSet<int> Colecao =>
            _instancia != null ? new HashSet<int>(_instancia._colecao) : new HashSet<int>();

        /// <summary>Se a conta possui a espécie de pet desbloqueada.</summary>
        public static bool Possui(EspecieDePet especie) =>
            _instancia != null && _instancia._colecao.Contains((int)especie);

        /// <summary>Se o pet do jogador local está no mundo agora (o <c>/pet</c> alterna por isso).</summary>
        public static bool TemPetProprio =>
            _instancia != null && _instancia._pets.ContainsKey(EntradaNoJogo.PersonagemId);

        /// <summary>Envia intenção de chamar o pet da espécie indicada.</summary>
        public static void Chamar(EspecieDePet especie)
        {
            ConexaoTempoReal.Enviar(PetsMessageIds.ChamarPet, new ChamarPet { Especie = especie });
        }

        /// <summary>Envia intenção de dispensar o pet ativo.</summary>
        public static void Dispensar()
        {
            ConexaoTempoReal.Enviar(PetsMessageIds.DispensarPet, new DispensarPet());
        }

        /// <summary>Envia intenção de usar o item de pet da bolsa.</summary>
        public static void UsarItem(long instanciaId)
        {
            ConexaoTempoReal.Enviar(
                PetsMessageIds.UsarItemPet,
                new UsarItemPet { InstanciaId = instanciaId }
            );
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject("Pets.Apresentador");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ApresentadorDePets>();
        }

        private void Awake()
        {
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PetApareceu>(PetsMessageIds.PetApareceu, AoAparecer)
            );
            _assinaturas.Add(ConexaoTempoReal.Assinar<PetMudou>(PetsMessageIds.PetMudou, AoMudar));
            _assinaturas.Add(ConexaoTempoReal.Assinar<PetSumiu>(PetsMessageIds.PetSumiu, AoSumir));
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PetRecusado>(PetsMessageIds.PetRecusado, AoRecusar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<ColecaoPets>(PetsMessageIds.ColecaoPets, AoReceberColecao)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PetDesbloqueado>(
                    PetsMessageIds.PetDesbloqueado,
                    AoDesbloquearPet
                )
            );
        }

        private void OnDestroy()
        {
            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            LimparTodos();
        }

        private void AoAparecer(PetApareceu dados)
        {
            if (dados == null)
            {
                return;
            }

            if (_pets.TryGetValue(dados.DonoId, out var existente))
            {
                // Já estava aqui (ex.: o servidor refez a vista): só reposiciona.
                existente.Alvo = new Vector3(dados.X, dados.Y, dados.Z);
                existente.AlvoRotacaoY = dados.RotacaoY;
                existente.Andando = dados.Andando;
                existente.Sequencia = dados.Sequencia;
                return;
            }

            var prefab = Resources.Load<GameObject>(PrefixoDoPrefab + (int)dados.Especie);
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>(PrefixoDoPrefab + (int)EspecieDePet.CervoJovem);
            }

            if (prefab == null)
            {
                Debug.LogWarning(
                    "Pets: falta o prefab Resources/" + PrefixoDoPrefab + (int)dados.Especie
                );
                return;
            }

            var posicao = new Vector3(dados.X, dados.Y, dados.Z);
            var raiz = Instantiate(prefab, posicao, Quaternion.Euler(0f, dados.RotacaoY, 0f));
            raiz.name = "Pet_" + dados.DonoId;
            _pets[dados.DonoId] = new PetNoClient
            {
                DonoId = dados.DonoId,
                Raiz = raiz,
                Modelo = raiz.transform.Find(NomeDoModelo),
                Alvo = posicao,
                AlvoRotacaoY = dados.RotacaoY,
                Andando = dados.Andando,
                Sequencia = dados.Sequencia,
            };
        }

        private void AoMudar(PetMudou dados)
        {
            // Descarta o pacote velho que chegou depois de um novo.
            if (
                dados == null
                || !_pets.TryGetValue(dados.DonoId, out var pet)
                || dados.Sequencia <= pet.Sequencia
            )
            {
                return;
            }

            pet.Sequencia = dados.Sequencia;
            pet.Alvo = new Vector3(dados.X, dados.Y, dados.Z);
            pet.AlvoRotacaoY = dados.RotacaoY;
            pet.Andando = dados.Andando;
        }

        private void AoSumir(PetSumiu dados)
        {
            if (dados != null && _pets.TryGetValue(dados.DonoId, out var pet))
            {
                Destruir(pet);
                _pets.Remove(dados.DonoId);
            }
        }

        private void AoReceberColecao(ColecaoPets dados)
        {
            if (dados == null || dados.EspeciesDesbloqueadas == null)
            {
                return;
            }

            _colecao.Clear();
            foreach (var especie in dados.EspeciesDesbloqueadas)
            {
                _colecao.Add(especie);
            }

            ColecaoMudou?.Invoke();
        }

        private void AoDesbloquearPet(PetDesbloqueado dados)
        {
            if (dados == null)
            {
                return;
            }

            _colecao.Add(dados.Especie);
            ChatDoJogo.AdicionarLinhaSistema($"Pet desbloqueado: {dados.Nome}!");
            ColecaoMudou?.Invoke();
        }

        private static void AoRecusar(PetRecusado dados)
        {
            if (dados == null)
            {
                return;
            }

            ChatDoJogo.AdicionarLinhaSistema(TextoDaRecusa(dados.Codigo));
        }

        internal static string TextoDaRecusa(string codigo)
        {
            switch (codigo)
            {
                case CodigosErroPets.SemPosicao:
                    return "Entre no mundo para chamar o pet.";
                case CodigosErroPets.SemPet:
                    return "Você não está com um pet.";
                case CodigosErroPets.EmCombate:
                    return "Não é possível usar item de pet em combate.";
                case CodigosErroPets.JaPossuiPet:
                    return "Você já possui este pet em sua coleção.";
                case CodigosErroPets.PetNaoDesbloqueado:
                    return "Você ainda não desbloqueou este pet.";
                case CodigosErroPets.ItemNaoEncontrado:
                    return "Item de pet não encontrado no inventário.";
                case CodigosErroPets.ItemNaoEPet:
                    return "Este item não é um pet consumível.";
                default:
                    return "Não foi possível mexer no pet agora.";
            }
        }

        private void Update()
        {
            if (!EntradaNoJogo.NoJogo)
            {
                // Saiu do mundo (cena descarregada): os pets vão junto.
                if (_pets.Count > 0)
                {
                    LimparTodos();
                }

                return;
            }

            var dt = Time.deltaTime;
            foreach (var pet in _pets.Values)
            {
                if (pet.Raiz == null)
                {
                    continue;
                }

                Mover(pet, dt);
                Balancar(pet, dt);
            }
        }

        private static void Mover(PetNoClient pet, float dt)
        {
            var transformacao = pet.Raiz.transform;
            var atual = transformacao.position;
            var salto = new Vector2(pet.Alvo.x - atual.x, pet.Alvo.z - atual.z).magnitude;
            if (salto > ConfigVisualDoPet.SaltoDeTeletransporteMetros)
            {
                // Alcançou o dono: vai direto, sem deslizar pelo mapa.
                transformacao.position = pet.Alvo;
                pet.Velocidade = Vector3.zero;
            }
            else
            {
                transformacao.position = Vector3.SmoothDamp(
                    atual,
                    pet.Alvo,
                    ref pet.Velocidade,
                    ConfigVisualDoPet.SuavizacaoDaPosicaoSegundos
                );
            }

            var rotacao = Mathf.MoveTowardsAngle(
                transformacao.eulerAngles.y,
                pet.AlvoRotacaoY,
                ConfigVisualDoPet.GiroMaximoGrausPorSegundo * dt
            );
            transformacao.rotation = Quaternion.Euler(0f, rotacao, 0f);
        }

        private static void Balancar(PetNoClient pet, float dt)
        {
            if (pet.Modelo == null)
            {
                return;
            }

            if (!pet.Andando)
            {
                pet.FaseDoPasso = 0f;
                pet.Modelo.localPosition = Vector3.Lerp(
                    pet.Modelo.localPosition,
                    Vector3.zero,
                    10f * dt
                );
                pet.Modelo.localRotation = Quaternion.Slerp(
                    pet.Modelo.localRotation,
                    Quaternion.identity,
                    10f * dt
                );
                return;
            }

            pet.FaseDoPasso += dt * ConfigVisualDoPet.PassosPorSegundo * Mathf.PI * 2f;
            var salto =
                Mathf.Abs(Mathf.Sin(pet.FaseDoPasso)) * ConfigVisualDoPet.AlturaDoPassoMetros;
            pet.Modelo.localPosition = new Vector3(0f, salto, 0f);
            pet.Modelo.localRotation = Quaternion.Euler(
                Mathf.Sin(pet.FaseDoPasso) * ConfigVisualDoPet.BalancoDoPassoGraus,
                0f,
                0f
            );
        }

        private void LimparTodos()
        {
            foreach (var pet in _pets.Values)
            {
                Destruir(pet);
            }

            _pets.Clear();
        }

        private static void Destruir(PetNoClient pet)
        {
            if (pet.Raiz != null)
            {
                Destroy(pet.Raiz);
            }
        }
    }
}
