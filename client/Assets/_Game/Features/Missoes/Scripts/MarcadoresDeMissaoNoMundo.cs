using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Missoes;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Missoes
{
    /// <summary>
    /// Marcas de missão no mundo, como no World of Warcraft: "!" sobre quem tem missão para dar
    /// (amarelo; vermelho se for difícil, cinza se for fácil demais) e "?" sobre quem recebe a
    /// entrega (dourado quando está pronta). Também põe na bússola do HUD o próximo passo de cada
    /// missão acompanhada (o NPC da entrega, com quem falar ou o lugar do objetivo). As posições
    /// vêm do catálogo de NPCs do shared, sem depender da funcionalidade NPCs.
    /// </summary>
    internal sealed class MarcadoresDeMissaoNoMundo : MonoBehaviour
    {
        private const string CaminhoEstilo = "Missoes/EstiloMissoes";
        private const float DistanciaMaxima = 70f;
        private const float AlturaDaMarca = 3.3f;
        private const float IntervaloDaBussola = 0.5f;

        private static MarcadoresDeMissaoNoMundo _instancia;
        private GameObject _jogadorAtendido;
        private VisualElement _camada;
        private GameObject _objeto;
        private readonly Dictionary<int, Label> _marcas = new Dictionary<int, Label>();
        private readonly HashSet<string> _naBussola = new HashSet<string>();
        private readonly HashSet<string> _vistosNaBussola = new HashSet<string>();
        private float _proximaBussola;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
                return;
            var objeto = new GameObject("Missoes.MarcadoresNoMundo");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<MarcadoresDeMissaoNoMundo>();
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null)
            {
                if (_camada != null)
                    Desmontar();
                return;
            }

            if (jogador != _jogadorAtendido || _objeto == null)
            {
                _jogadorAtendido = jogador;
                Montar(jogador.scene);
            }
        }

        private void LateUpdate()
        {
            if (_camada == null || _jogadorAtendido == null)
                return;

            var camera = Camera.main;
            var origem = _jogadorAtendido.transform.position;
            var nivel = PerfilDoJogador.Nivel > 0 ? PerfilDoJogador.Nivel : 1;
            foreach (var npc in CatalogoNpcs.Todos)
            {
                var info = MarcadoresDeMissaoNpc.Avaliar(
                    npc.Id,
                    nivel,
                    ControleDeMissoes.Ativas,
                    ControleDeMissoes.Concluidas,
                    ControleDeMissoes.RotacaoDiaria,
                    ControleDeMissoes.RotacaoSemanal
                );
                var marca = Marca(npc.Id);
                var mundo = new Vector3(
                    npc.X,
                    MapaDoMundo.AlturaEm(npc.X, npc.Z) + AlturaDaMarca,
                    npc.Z
                );
                var perto = Vector3.Distance(origem, mundo) <= DistanciaMaxima;
                if (!info.Ativo || !perto || camera == null || marca.panel == null)
                {
                    marca.style.display = DisplayStyle.None;
                    continue;
                }

                var viewport = camera.WorldToViewportPoint(mundo);
                if (viewport.z <= 0f)
                {
                    marca.style.display = DisplayStyle.None;
                    continue;
                }

                var ponto = RuntimePanelUtils.CameraTransformWorldToPanel(
                    marca.panel,
                    mundo,
                    camera
                );
                marca.text = info.Simbolo;
                marca.style.display = DisplayStyle.Flex;
                marca.style.left = ponto.x;
                marca.style.top = ponto.y;
                marca.EnableInClassList(
                    "missao-marca-pronta",
                    info.Tipo == TipoMarcadorNpc.EntregarPronta
                );
                marca.EnableInClassList(
                    "missao-marca-pendente",
                    info.Tipo == TipoMarcadorNpc.EntregarPendente
                );
                marca.EnableInClassList(
                    "missao-marca-dificil",
                    info.Tipo == TipoMarcadorNpc.DisponivelDificil
                );
                marca.EnableInClassList(
                    "missao-marca-trivial",
                    info.Tipo == TipoMarcadorNpc.DisponivelTrivial
                );
                // Balança devagar, como as marcas flutuantes dos MMOs.
                marca.style.translate = new Translate(
                    Length.Percent(-50),
                    Length.Percent(-100 + Mathf.Sin(Time.time * 2.4f + npc.Id) * 8f)
                );
            }

            if (Time.time >= _proximaBussola)
            {
                _proximaBussola = Time.time + IntervaloDaBussola;
                AtualizarBussola();
            }
        }

        private void AtualizarBussola()
        {
            var bussola = Hud.Bussola;
            if (bussola == null)
                return;

            _vistosNaBussola.Clear();
            foreach (var progresso in ControleDeMissoes.ObterAcompanhadas())
            {
                var definicao = CatalogoDeMissoes.Obter(progresso.MissaoId);
                if (definicao == null || !ProximoPasso(definicao, progresso, out var destino))
                    continue;
                var id = "missao." + definicao.Id;
                bussola.Marcar(id, destino, "hud-bussola-missao");
                _vistosNaBussola.Add(id);
                _naBussola.Add(id);
            }

            _naBussola.RemoveWhere(id =>
            {
                if (_vistosNaBussola.Contains(id))
                    return false;
                bussola.Desmarcar(id);
                return true;
            });
        }

        private static bool ProximoPasso(
            DefinicaoDeMissao definicao,
            ProgressoDeMissaoDto progresso,
            out Vector3 destino
        )
        {
            destino = default;
            if (progresso.Estado == EstadoDaMissao.ProntaParaEntregar)
                return PosicaoDoNpc(definicao.NpcEntregadorId, out destino);

            foreach (var objetivo in definicao.Objetivos)
            {
                var feito = false;
                if (progresso.Objetivos != null)
                {
                    foreach (var p in progresso.Objetivos)
                    {
                        if (p.ObjetivoId == objetivo.Id && p.Concluido)
                            feito = true;
                    }
                }

                if (feito)
                    continue;
                if (
                    objetivo.Tipo == TipoDeObjetivo.Falar
                    && PosicaoDoNpc(objetivo.AlvoId, out destino)
                )
                    return true;
                if (
                    Mathf.Abs(objetivo.CoordenadaX) > 0.01f
                    || Mathf.Abs(objetivo.CoordenadaZ) > 0.01f
                )
                {
                    destino = new Vector3(
                        objetivo.CoordenadaX,
                        MapaDoMundo.AlturaEm(objetivo.CoordenadaX, objetivo.CoordenadaZ),
                        objetivo.CoordenadaZ
                    );
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool PosicaoDoNpc(int npcId, out Vector3 posicao)
        {
            var npc = npcId > 0 ? CatalogoNpcs.Obter(npcId) : null;
            posicao =
                npc != null
                    ? new Vector3(npc.X, MapaDoMundo.AlturaEm(npc.X, npc.Z), npc.Z)
                    : default;
            return npc != null;
        }

        private Label Marca(int npcId)
        {
            if (_marcas.TryGetValue(npcId, out var marca))
                return marca;
            marca = ComponentesHud.Texto(string.Empty, "missao-marca");
            marca.style.display = DisplayStyle.None;
            _camada.Add(marca);
            _marcas[npcId] = marca;
            return marca;
        }

        private void Montar(Scene cena)
        {
            Desmontar();
            var objeto = new GameObject("Missoes.MarcasNoMundo");
            _objeto = objeto;
            SceneManager.MoveGameObjectToScene(objeto, cena);
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = EstiloCore.CriarConfiguracaoDoPainel(
                "PainelMarcasDeMissao",
                -3.4f
            );
            objeto.SetActive(true);
            EstiloCore.Aplicar(documento.rootVisualElement);
            var estilo = Resources.Load<StyleSheet>(CaminhoEstilo);
            if (estilo != null)
                documento.rootVisualElement.styleSheets.Add(estilo);
            _camada = new VisualElement
            {
                name = "missoes.marcas",
                pickingMode = PickingMode.Ignore,
            };
            _camada.AddToClassList("missao-camada");
            documento.rootVisualElement.Add(_camada);
        }

        private void Desmontar()
        {
            _camada?.RemoveFromHierarchy();
            _camada = null;
            _marcas.Clear();
            _naBussola.Clear();
        }
    }
}
