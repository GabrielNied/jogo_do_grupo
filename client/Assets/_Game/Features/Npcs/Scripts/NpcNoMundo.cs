using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Representação física e visual de um NPC no mundo do client (#1063).
    /// Possui corpo procedural com cápsula tingida pela cor do papel, colisor estático invencível,
    /// placa flutuante com nome/papel e prompt de interação quando o jogador está próximo.
    /// </summary>
    public sealed class NpcNoMundo : MonoBehaviour
    {
        public static readonly Vector3 TamanhoCorpo = new Vector3(0.85f, 1.8f, 0.85f);
        public const float AlturaPlaca = 2.3f;
        private const float AlturaBuscaChao = 5f;
        private const float AlcanceBuscaChao = 15f;

        private DefinicaoNpc _definicao;
        private GameObject _corpo;
        private GameObject _placaObjeto;
        private TextMesh _textoPlaca;
        private bool _jogadorNoAlcance;

        public DefinicaoNpc Definicao => _definicao;
        public bool JogadorNoAlcance => _jogadorNoAlcance;

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
            CriarPlacaDeNome();
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

            _corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _corpo.name = "CorpoVisual";
            _corpo.transform.SetParent(transform, false);
            _corpo.transform.localScale = TamanhoCorpo;
            _corpo.transform.localPosition = new Vector3(0f, TamanhoCorpo.y * 0.5f, 0f);

            var colisorFilho = _corpo.GetComponent<Collider>();
            if (colisorFilho != null)
            {
                Destroy(colisorFilho);
            }

            var renderer = _corpo.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = ObterCorDoPapel(_definicao.Papel);
            }
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
            if (noAlcance)
            {
                _textoPlaca.text = $"{_definicao.Nome}\n<{titulo}>\n[E] Falar";
            }
            else
            {
                _textoPlaca.text = $"{_definicao.Nome}\n<{titulo}>";
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
            }

            if (_jogadorNoAlcance && Input.GetKeyDown(KeyCode.E))
            {
                InteragiuComNpc?.Invoke(_definicao);
            }
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
            var origem = posicao + Vector3.up * AlturaBuscaChao;
            if (
                Physics.Raycast(
                    origem,
                    Vector3.down,
                    out var batida,
                    AlcanceBuscaChao,
                    ~0,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                posicao.y = batida.point.y;
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
                _ => "Habitante",
            };
    }
}
