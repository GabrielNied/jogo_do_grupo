using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Core.Audio
{
    /// <summary>
    /// Áudio do client (docs/features/interface.md, Áudio): a música da área com transição
    /// cruzada, os sons da interface e os passos do próprio personagem. Só reage ao que o client
    /// já sabe; o servidor nunca manda tocar som. As funcionalidades pedem a faixa do mundo
    /// (<see cref="PedirMusicaDoMundo"/>) e os sons de interface (<see cref="TocarInterface"/>).
    /// </summary>
    public sealed class AudioDoJogo : MonoBehaviour
    {
        private const string PastaDaMusica = "Audio/Musica/";
        private const string PastaDaInterface = "Audio/Interface/";
        private const string PastaDosJingles = "Audio/Jingles/";

        private static AudioDoJogo _instancia;
        private static MusicaDoJogo _pedidaPeloMundo = MusicaDoJogo.Cidade;
        private static bool _abafada;

        private readonly Dictionary<MusicaDoJogo, AudioClip> _faixas =
            new Dictionary<MusicaDoJogo, AudioClip>();
        private readonly Dictionary<SomDeInterface, AudioClip> _sons =
            new Dictionary<SomDeInterface, AudioClip>();

        private AudioSource _musicaA;
        private AudioSource _musicaB;
        private AudioSource _interface;
        private float _ganhoA;
        private float _ganhoB;
        private bool _aEhAAtual = true;
        private MusicaDoJogo _tocando = MusicaDoJogo.Nenhuma;
        private float _ultimoClique;

        /// <summary>A faixa que está entrando (ou tocando) agora.</summary>
        public static MusicaDoJogo Tocando =>
            _instancia != null ? _instancia._tocando : MusicaDoJogo.Nenhuma;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            _pedidaPeloMundo = MusicaDoJogo.Cidade;
            _abafada = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject("Core.Audio");
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<AudioDoJogo>();
        }

        /// <summary>
        /// A faixa que o mundo quer agora (cidade, dungeon, chefe, vitória). Fora do mundo a
        /// abertura vale de qualquer jeito. A vitória toca uma vez e volta para a dungeon.
        /// </summary>
        public static void PedirMusicaDoMundo(MusicaDoJogo musica) => _pedidaPeloMundo = musica;

        /// <summary>Baixa a música enquanto o personagem está caído (e volta ao levantar).</summary>
        public static void AbafarMusica(bool abafar) => _abafada = abafar;

        /// <summary>Toca um som da interface (sem posição), no volume de Efeitos.</summary>
        public static void TocarInterface(SomDeInterface som, float volume = 1f)
        {
            _instancia?.Tocar(som, volume);
        }

        /// <summary>
        /// Liga o clique de botão de um painel de UI: qualquer <see cref="Button"/> dentro dele
        /// toca o clique. Chamado pelo <c>EstiloCore.Aplicar</c> em cada raiz.
        /// </summary>
        public static void LigarCliques(VisualElement raiz)
        {
            if (raiz == null)
            {
                return;
            }

            raiz.UnregisterCallback<ClickEvent>(AoClicar, TrickleDown.TrickleDown);
            raiz.RegisterCallback<ClickEvent>(AoClicar, TrickleDown.TrickleDown);
        }

        private static void AoClicar(ClickEvent evento)
        {
            if (!(evento.target is VisualElement alvo))
            {
                return;
            }

            var botao = alvo as Button ?? alvo.GetFirstAncestorOfType<Button>();
            if (botao != null && botao.enabledInHierarchy)
            {
                TocarInterface(SomDeInterface.Clique);
            }
        }

        private void Awake()
        {
            _musicaA = CriarFonte("Musica A");
            _musicaB = CriarFonte("Musica B");
            _interface = CriarFonte("Interface");
            AvisosDoJogo.Mostrado += AoMostrarAviso;
            JanelasDoJogo.Abriu += AoAbrirJanela;
            JanelasDoJogo.Fechou += AoFecharJanela;
        }

        private void OnDestroy()
        {
            AvisosDoJogo.Mostrado -= AoMostrarAviso;
            JanelasDoJogo.Abriu -= AoAbrirJanela;
            JanelasDoJogo.Fechou -= AoFecharJanela;
            if (_instancia == this)
            {
                _instancia = null;
            }
        }

        private AudioSource CriarFonte(string nome)
        {
            var objeto = new GameObject(nome);
            objeto.transform.SetParent(transform, false);
            var fonte = objeto.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f;
            fonte.ignoreListenerPause = true;
            return fonte;
        }

        private void Update()
        {
            var desejada = EscolhaDaMusica.Escolher(EntradaNoJogo.NoJogo, _pedidaPeloMundo);
            // A vitória que acabou devolve a faixa da dungeon.
            if (
                _tocando == MusicaDoJogo.Vitoria
                && desejada == MusicaDoJogo.Vitoria
                && !Atual.isPlaying
            )
            {
                _pedidaPeloMundo = EscolhaDaMusica.DepoisDe(MusicaDoJogo.Vitoria);
                desejada = _pedidaPeloMundo;
            }

            if (desejada != _tocando)
            {
                Trocar(desejada);
            }

            AtualizarVolumes(Time.unscaledDeltaTime);
            GarantirPassos();
        }

        private AudioSource Atual => _aEhAAtual ? _musicaA : _musicaB;

        private void Trocar(MusicaDoJogo musica)
        {
            _tocando = musica;
            var clipe = Faixa(musica);
            _aEhAAtual = !_aEhAAtual;
            var entra = Atual;
            entra.Stop();
            entra.clip = clipe;
            entra.loop = EscolhaDaMusica.Repete(musica);
            if (clipe != null)
            {
                entra.Play();
            }
        }

        private void AtualizarVolumes(float dt)
        {
            var passo = dt / Mathf.Max(0.01f, ConfigAudio.TransicaoSegundos);
            _ganhoA = Mathf.MoveTowards(_ganhoA, _aEhAAtual ? 1f : 0f, passo);
            _ganhoB = Mathf.MoveTowards(_ganhoB, _aEhAAtual ? 0f : 1f, passo);
            var volume =
                VolumesDoJogo.Musica
                * ConfigAudio.VolumeDaMusica
                * (_abafada ? ConfigAudio.MusicaComPersonagemCaido : 1f);
            _musicaA.volume = _ganhoA * volume;
            _musicaB.volume = _ganhoB * volume;
            if (_ganhoA <= 0f && _musicaA.isPlaying)
            {
                _musicaA.Stop();
            }

            if (_ganhoB <= 0f && _musicaB.isPlaying)
            {
                _musicaB.Stop();
            }
        }

        // O jogador local ganha os passos ao nascer no mundo.
        private static void GarantirPassos()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (jogador != null && jogador.GetComponent<PassosDoJogador>() == null)
            {
                jogador.AddComponent<PassosDoJogador>();
            }
        }

        private void Tocar(SomDeInterface som, float volume)
        {
            if (som == SomDeInterface.Clique)
            {
                if (Time.unscaledTime - _ultimoClique < ConfigAudio.IntervaloMinimoDoCliqueSegundos)
                {
                    return;
                }

                _ultimoClique = Time.unscaledTime;
            }

            var clipe = Som(som);
            if (clipe == null)
            {
                return;
            }

            _interface.PlayOneShot(
                clipe,
                Mathf.Clamp01(volume) * ConfigAudio.VolumeDaInterface * VolumesDoJogo.Efeitos
            );
        }

        private void AoMostrarAviso(string texto, TipoDeAviso tipo)
        {
            if (tipo == TipoDeAviso.Erro)
            {
                Tocar(SomDeInterface.Erro, 0.8f);
            }
        }

        private void AoAbrirJanela(string chave) => Tocar(SomDeInterface.Abrir, 0.7f);

        private void AoFecharJanela(string chave) => Tocar(SomDeInterface.Fechar, 0.6f);

        private AudioClip Faixa(MusicaDoJogo musica)
        {
            if (_faixas.TryGetValue(musica, out var clipe))
            {
                return clipe;
            }

            clipe = musica switch
            {
                MusicaDoJogo.Abertura => Resources.Load<AudioClip>(PastaDaMusica + "abertura"),
                MusicaDoJogo.Cidade => Resources.Load<AudioClip>(PastaDaMusica + "cidade"),
                MusicaDoJogo.Dungeon => Resources.Load<AudioClip>(PastaDaMusica + "dungeon"),
                MusicaDoJogo.Chefe => Resources.Load<AudioClip>(PastaDaMusica + "chefe"),
                MusicaDoJogo.Vitoria => Resources.Load<AudioClip>(
                    PastaDosJingles + "jingles_HIT15"
                ),
                _ => null,
            };
            _faixas[musica] = clipe;
            return clipe;
        }

        private AudioClip Som(SomDeInterface som)
        {
            if (_sons.TryGetValue(som, out var clipe))
            {
                return clipe;
            }

            var caminho = som switch
            {
                SomDeInterface.Clique => PastaDaInterface + "clique",
                SomDeInterface.Abrir => PastaDaInterface + "abrir",
                SomDeInterface.Fechar => PastaDaInterface + "fechar",
                SomDeInterface.Erro => PastaDaInterface + "erro",
                SomDeInterface.Confirmar => PastaDaInterface + "confirmar",
                SomDeInterface.Convite => PastaDaInterface + "convite",
                SomDeInterface.Moedas => PastaDaInterface + "moedas",
                SomDeInterface.Item => PastaDaInterface + "item",
                SomDeInterface.Livro => PastaDaInterface + "livro",
                SomDeInterface.MissaoConcluida => PastaDosJingles + "jingles_PIZZI03",
                SomDeInterface.SubirNivel => PastaDosJingles + "jingles_PIZZI07",
                _ => null,
            };
            clipe = caminho != null ? Resources.Load<AudioClip>(caminho) : null;
            _sons[som] = clipe;
            return clipe;
        }
    }
}
