using System;
using System.Collections.Generic;
using System.Threading;
using JogoDoGrupo.Client.Core.Jogo.Controle;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Core.Jogo
{
    /// <summary>
    /// Entrada e saída do mundo. As funcionalidades registram quem monta o mundo (Mundo) e quem
    /// cria o visual do personagem (Personagem); o Core cria o jogador local (controle em 3ª
    /// pessoa, câmera e menu do Esc) e junta tudo, sem uma funcionalidade conhecer a outra.
    /// </summary>
    /// <remarks>
    /// Tudo o que é criado durante <see cref="Entrar"/> (inclusive pelo construtor de mundo e
    /// pelo visual) vai para uma cena só do mundo, criada em código e descarregada em
    /// <see cref="Sair"/>. Registre num método com
    /// <c>[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]</c>:
    /// <code>
    /// [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    /// private static void Registrar()
    /// {
    ///     EntradaNoJogo.DefinirConstrutorDeMundo(ConstrutorHub.Construir);
    /// }
    /// </code>
    /// </remarks>
    public static class EntradaNoJogo
    {
        private const string PrefixoCena = "JogoDoGrupo.Mundo";

        /// <summary>Altura do CharacterController do jogador, em metros.</summary>
        public const float AlturaJogador = 1.8f;

        /// <summary>Raio do CharacterController do jogador, em metros.</summary>
        public const float RaioJogador = 0.35f;

        private const float PeleJogador = 0.05f;

        private static readonly List<Behaviour> _desativados = new List<Behaviour>();

        private static Func<PosicaoInicial> _construirMundo;
        private static Func<CancellationToken, Awaitable<PosicaoInicial>> _construirMundoAsync;
        private static Func<long, GameObject> _criarVisual;
        private static Func<int, GameObject> _criarVisualDeOutro;
        private static Scene _cenaMundo;
        private static Scene _cenaAnterior;
        private static int _contadorCenas;
        private static CancellationTokenSource _cancelamento;
        private static bool _posicaoConfirmada;
        private static long _ultimoPersonagemId;

        internal const float TempoLimitePosicaoSegundos = 20f;

        /// <summary>O jogador ainda espera o cenário e a posição inicial do servidor.</summary>
        public static bool EntradaPendente { get; private set; }

        /// <summary>Se o jogador está no mundo (entre <see cref="Entrar"/> e <see cref="Sair"/>).</summary>
        public static bool NoJogo { get; private set; }

        /// <summary>Id do personagem que entrou, ou 0 fora do mundo.</summary>
        public static long PersonagemId { get; private set; }

        /// <summary>Jogador local (raiz com controle e visual), ou <c>null</c> fora do mundo.</summary>
        public static GameObject Jogador { get; private set; }

        /// <summary>
        /// Registra quem monta o mundo (funcionalidade Mundo). <paramref name="construir"/> é
        /// chamado em <see cref="Entrar"/>, cria os objetos do mundo e devolve onde o jogador nasce.
        /// <c>null</c> remove o registro (o Core monta o mundo padrão).
        /// </summary>
        public static void DefinirConstrutorDeMundo(Func<PosicaoInicial> construir)
        {
            _construirMundo = construir;
        }

        /// <summary>Registra a montagem do mundo em etapas, com cancelamento ao sair.</summary>
        public static void DefinirConstrutorDeMundoAsync(
            Func<CancellationToken, Awaitable<PosicaoInicial>> construir
        ) => _construirMundoAsync = construir;

        /// <summary>
        /// Registra quem cria o visual do personagem pelo Id (funcionalidade Personagem). O
        /// visual devolvido vira filho do jogador, com os pés no Y = 0 e a frente em +Z. Pode
        /// devolver uma instância ou o próprio prefab (o Core instancia). Um Animator no visual
        /// recebe <see cref="AnimadorDoVisual.ParametroVelocidade"/> e
        /// <see cref="AnimadorDoVisual.ParametroNoChao"/>. <c>null</c> remove o registro (o
        /// Core usa uma cápsula).
        /// </summary>
        public static void DefinirVisualDoJogador(Func<long, GameObject> criarVisual)
        {
            _criarVisual = criarVisual;
        }

        /// <summary>
        /// Registra quem cria o visual dos <b>outros</b> jogadores pela raça (funcionalidade
        /// Personagem; a raça é o número que o servidor manda no <c>JogadorVisivel</c>). O visual
        /// segue as mesmas regras de <see cref="DefinirVisualDoJogador"/>: pés no Y = 0, frente em
        /// +Z, Animator com <see cref="AnimadorDoVisual.ParametroVelocidade"/>. <c>null</c> remove o
        /// registro (o Core usa uma cápsula).
        /// </summary>
        public static void DefinirVisualDeOutroJogador(Func<int, GameObject> criarVisual)
        {
            _criarVisualDeOutro = criarVisual;
        }

        /// <summary>
        /// Cria o visual de outro jogador da raça (o Mundo chama ao ver alguém). Sempre devolve uma
        /// instância na cena: sem registro, raça sem prefab ou falha do criador, é a cápsula.
        /// </summary>
        public static GameObject CriarVisualDeOutroJogador(int raca) =>
            CriarVisualDeOutro(_criarVisualDeOutro, raca);

        internal static GameObject CriarVisualDeOutro(Func<int, GameObject> criar, int raca)
        {
            if (criar != null)
            {
                try
                {
                    var visual = criar(raca);
                    if (visual != null)
                    {
                        return visual.scene.IsValid()
                            ? visual
                            : UnityEngine.Object.Instantiate(visual);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                Debug.LogWarning(
                    $"[EntradaNoJogo] Sem visual para a raça {raca}; usando a cápsula."
                );
            }

            return MundoPadrao.CriarCapsula();
        }

        /// <summary>
        /// Esconde a interface, cria o <see cref="Hud"/>, monta o mundo, cria o jogador local com o visual do personagem e
        /// o posiciona no ponto de nascimento. Se já estiver no mundo, sai antes.
        /// </summary>
        public static async void Entrar(long personagemId)
        {
            if (NoJogo)
            {
                Debug.LogWarning("[EntradaNoJogo] Entrar chamado já no mundo; desmontando antes.");
                Desmontar();
            }

            _cancelamento?.Cancel();
            _cancelamento = new CancellationTokenSource();
            var cancelar = _cancelamento.Token;
            _ultimoPersonagemId = personagemId;
            _posicaoConfirmada = false;
            EntradaPendente = true;
            Navegacao.Abrir(TelasCore.Carregamento);
            OpcoesGravadas.Carregar();

            try
            {
                // Dá ao painel um quadro para aparecer antes de começar a montar o cenário.
                await Awaitable.NextFrameAsync(cancelar);
                MontarEntrada(personagemId);
                TelaCarregamento.Atual?.MostrarEtapa(EtapaDoCarregamento.Cenario);
                PosicaoInicial inicio;
                if (_construirMundoAsync != null)
                {
                    inicio = await _construirMundoAsync(cancelar);
                }
                else
                {
                    inicio = MontarMundo(_construirMundo, out _);
                }

                cancelar.ThrowIfCancellationRequested();
                DesativarDeOutrasCenas();
                Jogador = CriarJogador(personagemId, inicio);
                TelaCarregamento.Atual?.MostrarEtapa(EtapaDoCarregamento.Conexao);
                var inicioDaEspera = Time.realtimeSinceStartup;
                while (!ConexaoTempoReal.Conectado)
                {
                    cancelar.ThrowIfCancellationRequested();
                    if (
                        ConexaoTempoReal.Estado == EstadoTempoReal.Desconectado
                        && !string.IsNullOrEmpty(ConexaoTempoReal.UltimoErro)
                    )
                    {
                        throw new InvalidOperationException("Não foi possível conectar ao mundo.");
                    }

                    if (PrazoEsgotado(inicioDaEspera, Time.realtimeSinceStartup))
                    {
                        throw new TimeoutException("A conexão com o mundo não foi concluída.");
                    }

                    await Awaitable.NextFrameAsync(cancelar);
                }

                TelaCarregamento.Atual?.MostrarEtapa(EtapaDoCarregamento.Posicao);
                while (!_posicaoConfirmada)
                {
                    cancelar.ThrowIfCancellationRequested();
                    if (PrazoEsgotado(inicioDaEspera, Time.realtimeSinceStartup))
                    {
                        throw new TimeoutException("A conexão com o mundo não foi concluída.");
                    }

                    await Awaitable.NextFrameAsync(cancelar);
                }

                await Awaitable.NextFrameAsync(cancelar);
                EntradaPendente = false;
                Hud.DefinirVisivel(true);
                Jogador?.GetComponent<SessaoNoMundo>()?.LiberarEntrada();
                Navegacao.FecharTelaAtual();
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                if (cancelar.IsCancellationRequested)
                {
                    return;
                }

                Debug.LogException(e);
                Desmontar();
                TelaCarregamento.Atual?.MostrarFalha(
                    "Não foi possível entrar",
                    "A conexão com o mundo não foi concluída."
                );
            }
        }

        private static void MontarEntrada(long personagemId)
        {
            _cenaAnterior = SceneManager.GetActiveScene();
            _contadorCenas++;
            _cenaMundo = SceneManager.CreateScene($"{PrefixoCena}.{_contadorCenas}");
            SceneManager.SetActiveScene(_cenaMundo);
            NoJogo = true;
            PersonagemId = personagemId;
            // Antes do mundo: quem monta o mundo ou o visual já encontra as regiões.
            Hud.Montar(Navegacao.Container);
            Hud.DefinirVisivel(false);
        }

        /// <summary>Mundo chama depois de aplicar a primeira posição recebida do servidor.</summary>
        public static void SinalizarPronto()
        {
            if (EntradaPendente && NoJogo)
            {
                _posicaoConfirmada = true;
            }
        }

        internal static void TentarNovamente() => Entrar(_ultimoPersonagemId);

        internal static bool PrazoEsgotado(float inicio, float agora) =>
            agora - inicio >= TempoLimitePosicaoSegundos;

        /// <summary>
        /// Desmonta o mundo e o jogador e volta para a tela após o login (a seleção de
        /// personagem). Fora do mundo, só abre essa tela.
        /// </summary>
        public static void Sair()
        {
            Desmontar();
            Navegacao.AbrirTelaAposLogin();
        }

        /// <summary>
        /// Desmonta o mundo e o jogador sem abrir tela (ex.: sessão expirada, que abre o login).
        /// </summary>
        internal static void Desmontar()
        {
            _cancelamento?.Cancel();
            _cancelamento = null;
            EntradaPendente = false;
            _posicaoConfirmada = false;
            if (!NoJogo)
            {
                return;
            }

            NoJogo = false;
            Hud.Remover();
            // Fora do mundo não há presença: fecha o tempo real (no-op se já estava fechado).
            ConexaoTempoReal.Desconectar();
            PersonagemId = 0;
            Jogador = null;

            // Desativa já: o descarregamento da cena só termina depois, e o input e o cursor
            // precisam parar agora.
            if (_cenaMundo.IsValid() && _cenaMundo.isLoaded)
            {
                foreach (var objeto in _cenaMundo.GetRootGameObjects())
                {
                    objeto.SetActive(false);
                }
            }

            Reativar();

            if (_cenaAnterior.IsValid() && _cenaAnterior.isLoaded)
            {
                SceneManager.SetActiveScene(_cenaAnterior);
            }

            if (_cenaMundo.IsValid() && _cenaMundo.isLoaded)
            {
                SceneManager.UnloadSceneAsync(_cenaMundo);
            }

            _cenaMundo = default;
            _cenaAnterior = default;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Chama o construtor registrado; sem ele, ou se ele falhar, monta o
        /// <see cref="MundoPadrao"/> (devolvido em <paramref name="mundoPadrao"/>) e nasce na origem.
        /// </summary>
        internal static PosicaoInicial MontarMundo(
            Func<PosicaoInicial> construir,
            out GameObject mundoPadrao
        )
        {
            mundoPadrao = null;
            if (construir != null)
            {
                try
                {
                    return Validar(construir());
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning(
                        "[EntradaNoJogo] O construtor de mundo falhou; usando o mundo padrão."
                    );
                }
            }

            mundoPadrao = MundoPadrao.CriarMundo();
            return PosicaoInicial.Origem;
        }

        /// <summary>
        /// Chama o criador de visual registrado; sem ele, se ele falhar ou devolver
        /// <c>null</c>, devolve a cápsula do <see cref="MundoPadrao"/>. Se devolver um prefab
        /// (fora de cena), instancia.
        /// </summary>
        internal static GameObject CriarVisual(Func<long, GameObject> criar, long personagemId)
        {
            if (criar != null)
            {
                try
                {
                    var visual = criar(personagemId);
                    if (visual != null)
                    {
                        return visual.scene.IsValid()
                            ? visual
                            : UnityEngine.Object.Instantiate(visual);
                    }

                    Debug.LogWarning(
                        $"[EntradaNoJogo] O visual do personagem {personagemId} veio nulo; usando a cápsula."
                    );
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogWarning(
                        "[EntradaNoJogo] O criador de visual falhou; usando a cápsula."
                    );
                }
            }

            return MundoPadrao.CriarCapsula();
        }

        private static PosicaoInicial Validar(PosicaoInicial inicio)
        {
            var posicao = inicio.Posicao;
            var rotacao = inicio.Rotacao;
            if (float.IsNaN(posicao.x) || float.IsNaN(posicao.y) || float.IsNaN(posicao.z))
            {
                posicao = Vector3.zero;
            }

            // default(Quaternion) é (0, 0, 0, 0), que não é uma rotação válida.
            var tamanho =
                rotacao.x * rotacao.x
                + rotacao.y * rotacao.y
                + rotacao.z * rotacao.z
                + rotacao.w * rotacao.w;
            rotacao =
                tamanho < 0.0001f || float.IsNaN(tamanho)
                    ? Quaternion.identity
                    : Quaternion.Normalize(rotacao);

            return new PosicaoInicial(posicao, rotacao);
        }

        private static GameObject CriarJogador(long personagemId, PosicaoInicial inicio)
        {
            // Só a rotação horizontal: o jogador fica de pé mesmo que a rotação venha inclinada.
            var anguloInicial = inicio.Rotacao.eulerAngles.y;
            var raiz = new GameObject("Jogador");
            raiz.transform.SetPositionAndRotation(
                inicio.Posicao,
                Quaternion.Euler(0f, anguloInicial, 0f)
            );

            var corpo = raiz.AddComponent<CharacterController>();
            corpo.height = AlturaJogador;
            corpo.radius = RaioJogador;
            corpo.skinWidth = PeleJogador;
            // Com a pele somada ao centro, a base da cápsula toca o chão com os pés no Y = 0.
            corpo.center = new Vector3(0f, AlturaJogador / 2f + PeleJogador, 0f);
            corpo.stepOffset = 0.4f;
            corpo.slopeLimit = 45f;

            var visual = CriarVisual(_criarVisual, personagemId);
            visual.transform.SetParent(raiz.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            if (!visual.activeSelf)
            {
                visual.SetActive(true);
            }

            // Os colisores do visual bateriam no CharacterController; quem colide é ele.
            foreach (var colisor in visual.GetComponentsInChildren<Collider>())
            {
                if (!colisor.isTrigger)
                {
                    colisor.enabled = false;
                }
            }

            var animator = visual.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
            }

            var controle = raiz.AddComponent<ControleTerceiraPessoa>();

            var objetoCamera = new GameObject("CameraJogador");
            objetoCamera.tag = "MainCamera";
            var camera = objetoCamera.AddComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;
            camera.fieldOfView = 60f;
            objetoCamera.AddComponent<AudioListener>();
            var cameraJogador = objetoCamera.AddComponent<CameraTerceiraPessoa>();

            var sessao = raiz.AddComponent<SessaoNoMundo>();
            sessao.Configurar(controle, cameraJogador, new AnimadorDoVisual(animator));

            return raiz;
        }

        /// <summary>
        /// Desliga câmeras, AudioListeners e luzes direcionais das outras cenas abertas (ex.: a
        /// cena em que se deu Play), para não somarem com as do mundo. Religados ao sair.
        /// </summary>
        private static void DesativarDeOutrasCenas()
        {
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var cena = SceneManager.GetSceneAt(i);
                if (cena == _cenaMundo || !cena.isLoaded)
                {
                    continue;
                }

                foreach (var raiz in cena.GetRootGameObjects())
                {
                    Desativar(raiz.GetComponentsInChildren<Camera>());
                    Desativar(raiz.GetComponentsInChildren<AudioListener>());
                    foreach (var luz in raiz.GetComponentsInChildren<Light>())
                    {
                        if (luz.type == LightType.Directional)
                        {
                            Desativar(luz);
                        }
                    }
                }
            }
        }

        private static void Desativar(IEnumerable<Behaviour> componentes)
        {
            foreach (var componente in componentes)
            {
                Desativar(componente);
            }
        }

        private static void Desativar(Behaviour componente)
        {
            if (componente != null && componente.enabled)
            {
                componente.enabled = false;
                _desativados.Add(componente);
            }
        }

        private static void Reativar()
        {
            foreach (var componente in _desativados)
            {
                if (componente != null)
                {
                    componente.enabled = true;
                }
            }

            _desativados.Clear();
        }

        // Com "Enter Play Mode Options" sem recarregar o domínio, os campos estáticos
        // sobrevivem entre execuções; limpa antes de qualquer registro.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _construirMundo = null;
            _construirMundoAsync = null;
            _cancelamento?.Cancel();
            _cancelamento = null;
            EntradaPendente = false;
            _posicaoConfirmada = false;
            _ultimoPersonagemId = 0;
            _criarVisual = null;
            _criarVisualDeOutro = null;
            _desativados.Clear();
            _cenaMundo = default;
            _cenaAnterior = default;
            _contadorCenas = 0;
            NoJogo = false;
            PersonagemId = 0;
            Jogador = null;
        }
    }
}
