using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Features.Npcs.UI;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Representação física e visual de um NPC no mundo do client (#1063).
    /// Possui corpo procedural com cápsula tingida pela cor do papel, colisor estático invencível,
    /// placa flutuante com nome/papel e prompt de interação quando o jogador está próximo.
    /// </summary>
    public sealed class NpcNoMundo : MonoBehaviour, IInteragivelNoMundo
    {
        public static readonly Vector3 TamanhoCorpo = new Vector3(0.85f, 1.8f, 0.85f);
        public const float AlturaPlaca = 2.3f;
        private const float AlturaBuscaChao = 5f;
        private const float AlcanceBuscaChao = 15f;

        private DefinicaoNpc _definicao;
        private GameObject _corpo;
        private Animator _animator;
        private GameObject _placaObjeto;
        private TextMesh _textoPlaca;
        private bool _jogadorNoAlcance;

        private static readonly System.Collections.Generic.List<NpcNoMundo> _ativos =
            new System.Collections.Generic.List<NpcNoMundo>();
        private static InputAction _interagir;
        private static int _instanciasAtivas;

        /// <summary>NPCs no mundo agora (para as etiquetas de nome).</summary>
        public static System.Collections.Generic.IReadOnlyList<NpcNoMundo> Ativos => _ativos;

        public static bool EstaAtivo(NpcNoMundo npc) => _ativos.Contains(npc);

        public DefinicaoNpc Definicao => _definicao;
        public bool JogadorNoAlcance => _jogadorNoAlcance;

        public float PosicaoX => transform.position.x;
        public float PosicaoZ => transform.position.z;
        public float RaioInteracao => _definicao?.RaioInteracao ?? 3f;

        public bool PodeInteragir() =>
            isActiveAndEnabled && _definicao != null && !JanelasDoJogo.MenuAberto;

        /// <summary>Disparado quando o jogador pressiona o botão de interagir no raio deste NPC.</summary>
        public static event Action<DefinicaoNpc> InteragiuComNpc;

        public void Configurar(DefinicaoNpc definicao)
        {
            _definicao = definicao;
            gameObject.name = $"Npc_{definicao.Id}_{definicao.Nome}";

            var posicao = new Vector3(definicao.X, definicao.Y, definicao.Z);
            posicao = AjustarPosicaoNoChao(posicao);
            transform.position = posicao;
            transform.rotation = Quaternion.Euler(0f, definicao.RotacaoY, 0f);

            ConfigurarColisor();
            CriarCorpoVisual();
            // O nome e o papel aparecem em EtiquetasDeNpcs (UI nítida), não mais em texto 3D.
        }

        private void ConfigurarColisor()
        {
            var colisor = GetComponent<CapsuleCollider>();
            if (colisor == null)
            {
                colisor = gameObject.AddComponent<CapsuleCollider>();
            }

            colisor.height = TamanhoCorpo.y;
            colisor.radius = TamanhoCorpo.x * 0.5f;
            colisor.center = new Vector3(0f, TamanhoCorpo.y * 0.5f, 0f);
        }

        private void CriarCorpoVisual()
        {
            if (_corpo != null)
            {
                Destroy(_corpo);
            }

            _corpo = FabricaDeCorpoDeNpc.Criar(_definicao.Papel, _definicao.Id, transform);
            _animator = _corpo.GetComponentInChildren<Animator>();
        }

        private void CriarPlacaDeNome()
        {
            if (_placaObjeto != null)
            {
                Destroy(_placaObjeto);
            }

            _placaObjeto = new GameObject("PlacaDeNome");
            _placaObjeto.transform.SetParent(transform, false);
            _placaObjeto.transform.localPosition = new Vector3(0f, AlturaPlaca, 0f);

            _textoPlaca = _placaObjeto.AddComponent<TextMesh>();
            _textoPlaca.alignment = TextAlignment.Center;
            _textoPlaca.anchor = TextAnchor.MiddleCenter;
            _textoPlaca.characterSize = 0.12f;
            _textoPlaca.fontSize = 24;
            _textoPlaca.color = Color.white;

            AtualizarTextoPlaca(false);
        }

        private void AtualizarTextoPlaca(bool noAlcance)
        {
            if (_textoPlaca == null || _definicao == null)
            {
                return;
            }

            var titulo = ObterTituloDoPapel(_definicao.Papel);
            var rotuloTecla = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir);
            if (noAlcance)
            {
                _textoPlaca.text = $"{_definicao.Nome}\n<{titulo}>\n[{rotuloTecla}] Falar";
            }
            else
            {
                _textoPlaca.text = $"{_definicao.Nome}\n<{titulo}>";
            }
        }

        private void OnEnable()
        {
            if (!_ativos.Contains(this))
                _ativos.Add(this);
            RegistroDeInteracao.Registrar(this);

            _instanciasAtivas++;
            if (_interagir == null)
            {
                _interagir = new InputAction(
                    "InteragirNpc",
                    InputActionType.Button,
                    TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
                );
                TeclasDoJogo.Mudou += AtualizarTecla;
                _interagir.AddBinding("<Gamepad>/buttonWest");
                _interagir.Enable();
            }
        }

        private static void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private void OnDisable()
        {
            _ativos.Remove(this);
            RegistroDeInteracao.Desregistrar(this);

            _instanciasAtivas--;
            if (_instanciasAtivas <= 0 && _interagir != null)
            {
                TeclasDoJogo.Mudou -= AtualizarTecla;
                _interagir.Dispose();
                _interagir = null;
                _instanciasAtivas = 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ReiniciarAtivos()
        {
            _ativos.Clear();
            RegistroDeInteracao.Limpar();
            if (_interagir != null)
            {
                TeclasDoJogo.Mudou -= AtualizarTecla;
                _interagir.Dispose();
                _interagir = null;
            }
            _instanciasAtivas = 0;
        }

        public void Interagir()
        {
            if (_definicao == null)
            {
                return;
            }

            InteragiuComNpc?.Invoke(_definicao);
            if (ConexaoTempoReal.Conectado)
            {
                ConexaoTempoReal.Enviar(NpcsMessageIds.FalarComNpc, new FalarComNpc(_definicao.Id));
            }
            else
            {
                var dialogo = CatalogoDialogosNpc.ObterDialogo(_definicao);
                JanelasDoJogo.Abrir(JanelaDialogoNpc.Chave);
                if (JanelaDialogoNpc.InstanciaAtual != null)
                {
                    JanelaDialogoNpc.InstanciaAtual.Atualizar(dialogo);
                }
            }
        }

        private void Update()
        {
            if (_definicao == null)
            {
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            var posJogador =
                jogador != null
                    ? jogador.transform.position
                    : (Camera.main != null ? Camera.main.transform.position : transform.position);

            var estaNoAlcance = _definicao.Alcanca(posJogador.x, posJogador.z);
            if (estaNoAlcance != _jogadorNoAlcance)
            {
                _jogadorNoAlcance = estaNoAlcance;
                AtualizarTextoPlaca(_jogadorNoAlcance);
                if (_animator != null && _animator.runtimeAnimatorController != null)
                {
                    _animator.SetBool(ParametrosDeNpc.Falando, _jogadorNoAlcance);
                }
            }

            if (
                _jogadorNoAlcance
                && !JanelasDoJogo.MenuAberto
                && _interagir != null
                && _interagir.WasPressedThisFrame()
                && RegistroDeInteracao.EhOMaisProximo(this, posJogador.x, posJogador.z)
            )
            {
                Interagir();
            }
        }

        // Com dois NPCs ou outros interagíveis no alcance, só o mais perto responde ao botão.
        private bool EhOMaisPerto(Vector3 posJogador) =>
            RegistroDeInteracao.EhOMaisProximo(this, posJogador.x, posJogador.z);

        private float DistanciaHorizontal(Vector3 posicao)
        {
            var dx = transform.position.x - posicao.x;
            var dz = transform.position.z - posicao.z;
            return dx * dx + dz * dz;
        }

        private void LateUpdate()
        {
            if (_placaObjeto != null && Camera.main != null)
            {
                _placaObjeto.transform.rotation = Camera.main.transform.rotation;
            }
        }

        private static Vector3 AjustarPosicaoNoChao(Vector3 posicao)
        {
            // O catálogo guarda Y = 0 (a cidade antiga era plana); no mapa publicado o chão da
            // cidade fica mais alto. Procura a partir do relevo do mapa, não só do Y do catálogo,
            // senão o NPC fica enterrado sob a praça (#1179).
            var relevo = MapaDoMundo.AlturaEm(posicao.x, posicao.z);
            if (relevo > posicao.y)
            {
                posicao.y = relevo;
            }

            // Só conta como chão o terreno e as malhas do mapa. Cápsulas e caixas ficam de fora:
            // a do próprio NPC (Configurar roda de novo a cada lista da área e ele subia 1,8 m
            // por vez) e as dos pontos de serviço da Economia, no mesmo lugar dos NPCs.
            var origem = posicao + Vector3.up * AlturaBuscaChao;
            var batidas = Physics.RaycastAll(
                origem,
                Vector3.down,
                AlcanceBuscaChao,
                ~0,
                QueryTriggerInteraction.Ignore
            );
            var achouChao = false;
            var alturaDoChao = 0f;
            foreach (var batida in batidas)
            {
                if (!(batida.collider is TerrainCollider) && !(batida.collider is MeshCollider))
                {
                    continue;
                }

                if (!achouChao || batida.point.y > alturaDoChao)
                {
                    alturaDoChao = batida.point.y;
                    achouChao = true;
                }
            }

            if (achouChao)
            {
                posicao.y = alturaDoChao;
            }

            return posicao;
        }

        public static Color ObterCorDoPapel(PapelNpc papel) =>
            papel switch
            {
                PapelNpc.Vendedor => new Color(0.95f, 0.78f, 0.25f), // Ouro
                PapelNpc.Merceeiro => new Color(0.95f, 0.78f, 0.25f), // Ouro
                PapelNpc.Armeiro => new Color(0.85f, 0.30f, 0.25f), // Vermelho / Ferro
                PapelNpc.Armadureiro => new Color(0.40f, 0.55f, 0.70f), // Aço
                PapelNpc.Taverneiro => new Color(0.85f, 0.60f, 0.20f), // Âmbar / Mel
                PapelNpc.Ferreiro => new Color(0.9f, 0.45f, 0.18f), // Bronze / Laranja
                PapelNpc.Banqueiro => new Color(0.2f, 0.75f, 0.45f), // Esmeralda
                PapelNpc.Leiloeiro => new Color(0.25f, 0.65f, 0.95f), // Safira / Azul Claro
                PapelNpc.DadorDeMissao => new Color(0.75f, 0.35f, 0.9f), // Ametista / Roxo
                PapelNpc.Cavalarico => new Color(0.60f, 0.40f, 0.20f), // Couro / Marrom
                PapelNpc.CuidadorDePets => new Color(0.35f, 0.80f, 0.50f), // Verde Natureza / Pets
                _ => new Color(0.85f, 0.85f, 0.85f), // Neutro
            };

        public static string ObterTituloDoPapel(PapelNpc papel) =>
            papel switch
            {
                PapelNpc.Vendedor => "Comerciante",
                PapelNpc.Merceeiro => "Merceeiro",
                PapelNpc.Armeiro => "Armeiro",
                PapelNpc.Armadureiro => "Armadureiro",
                PapelNpc.Taverneiro => "Taverneiro",
                PapelNpc.Ferreiro => "Ferreiro",
                PapelNpc.Banqueiro => "Banqueiro",
                PapelNpc.Leiloeiro => "Leiloeiro",
                PapelNpc.DadorDeMissao => "Missões",
                PapelNpc.Cavalarico => "Estábulo",
                PapelNpc.CuidadorDePets => "Cuidador de Pets",
                _ => "Habitante",
            };
    }
}
