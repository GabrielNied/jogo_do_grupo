using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Skills;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JogoDoGrupo.Client.Features.Skills
{
    /// <summary>
    /// A barra de skills do próprio personagem: as teclas 1, 2 e 3 pedem o uso da skill do espaço
    /// (só a intenção e a mira) e tudo o que aparece vem do servidor: a barra da arma equipada
    /// (<see cref="BarraDeSkillsMudou"/>), o uso confirmado com a recarga (<see cref="SkillUsada"/>),
    /// o dano no alvo (<see cref="SkillAcertou"/>) e a recusa com o motivo (<see cref="SkillRecusada"/>).
    /// Nada é gasto nem previsto aqui.
    /// </summary>
    internal sealed class ControleSkills : MonoBehaviour
    {
        public const string BotaoDoEspaco1 = "<Keyboard>/1";
        public const string BotaoDoEspaco2 = "<Keyboard>/2";
        public const string BotaoDoEspaco3 = "<Keyboard>/3";

        /// <summary>Tempo que o aviso de recusa fica na tela.</summary>
        public const double DuracaoDoAvisoSegundos = 1.6;

        /// <summary>Enquanto a barra não chega, pede de novo a cada tanto.</summary>
        public const double IntervaloDoPedidoSegundos = 2.0;

        /// <summary>Folga, em metros, entre o topo do alvo e a origem do número de dano.</summary>
        public const float FolgaDoNumero = 0.35f;

        /// <summary>Volume do som de preparo e de cancelamento.</summary>
        public const float VolumeDoSom = 0.5f;

        private const string NomeDoBoneco = "Boneco de treino";
        private const string NomeDoInimigo = "Inimigo ";

        private readonly List<IDisposable> _assinaturas = new List<IDisposable>();
        private readonly EstadoDaBarra _estado = new EstadoDaBarra();
        private readonly EstadoDoPreparo _preparo = new EstadoDoPreparo();
        private readonly Dictionary<SomDeSkill, AudioClip> _clipes =
            new Dictionary<SomDeSkill, AudioClip>();
        private readonly Dictionary<long, GameObject> _alvos = new Dictionary<long, GameObject>();

        public static ControleSkills Instancia { get; private set; }

        public EstadoDaBarra Estado => _estado;

        public event Action<BarraDeSkillsMudou> BarraMudou;

        private void Awake()
        {
            if (Instancia != null && Instancia != this)
            {
                Destroy(this);
                return;
            }

            Instancia = this;
        }

        private long _personagemId;
        private PainelSkills _painel;
        private InputAction[] _teclas;
        private AudioSource _fonte;
        private int _sequencia;
        private double _proximoPedido;
        private double _avisoAte;
        private double _poderAte;

        /// <summary>Tempo que o aviso de poder disparado fica na tela.</summary>
        public const double DuracaoDoPoderSegundos = 1.4;

        public void Configurar(long personagemId, PainelSkills painel)
        {
            _personagemId = personagemId;
            _painel = painel;
            _fonte = gameObject.AddComponent<AudioSource>();
            _fonte.playOnAwake = false;
            _fonte.spatialBlend = 0f;

            var caminhos = new[] { BotaoDoEspaco1, BotaoDoEspaco2, BotaoDoEspaco3 };
            _teclas = new InputAction[ConfigSkills.SkillsPorArma];
            for (var i = 0; i < _teclas.Length; i++)
            {
                _teclas[i] = new InputAction(
                    "Skill" + (i + 1),
                    InputActionType.Button,
                    caminhos[i]
                );
                if (isActiveAndEnabled)
                {
                    _teclas[i].Enable();
                }
            }

            _assinaturas.Add(
                ConexaoTempoReal.Assinar<BarraDeSkillsMudou>(
                    SkillsMessageIds.BarraDeSkillsMudou,
                    AoReceberBarra
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SkillUsada>(SkillsMessageIds.SkillUsada, AoUsar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SkillAcertou>(SkillsMessageIds.SkillAcertou, AoAcertar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SkillRecusada>(SkillsMessageIds.SkillRecusada, AoRecusar)
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SkillPreparoIniciado>(
                    SkillsMessageIds.SkillPreparoIniciado,
                    AoIniciarPreparo
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<SkillCancelada>(
                    SkillsMessageIds.SkillCancelada,
                    AoCancelar
                )
            );
            _assinaturas.Add(
                ConexaoTempoReal.Assinar<PoderDisparou>(
                    SkillsMessageIds.PoderDisparou,
                    AoDispararPoder
                )
            );
        }

        private void OnEnable()
        {
            if (_teclas == null)
            {
                return;
            }

            foreach (var tecla in _teclas)
            {
                tecla.Enable();
            }
        }

        private void OnDisable()
        {
            if (_teclas == null)
            {
                return;
            }

            foreach (var tecla in _teclas)
            {
                tecla.Disable();
            }
        }

        private void OnDestroy()
        {
            if (Instancia == this)
            {
                Instancia = null;
            }

            foreach (var assinatura in _assinaturas)
            {
                assinatura.Dispose();
            }

            _assinaturas.Clear();
            if (_teclas != null)
            {
                foreach (var tecla in _teclas)
                {
                    tecla.Dispose();
                }
            }
        }

        private void Update()
        {
            if (_painel == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            var agora = Time.unscaledTimeAsDouble;
            var conectado = ConexaoTempoReal.Conectado;
            if (conectado && !_estado.Recebida && agora >= _proximoPedido)
            {
                _proximoPedido = agora + IntervaloDoPedidoSegundos;
                ConexaoTempoReal.Enviar(
                    SkillsMessageIds.PedirBarraDeSkills,
                    new PedirBarraDeSkills()
                );
            }

            if (conectado && Cursor.lockState == CursorLockMode.Locked)
            {
                for (var i = 0; i < _teclas.Length; i++)
                {
                    if (!_teclas[i].WasPressedThisFrame())
                    {
                        continue;
                    }

                    if (!_preparo.Ativo)
                    {
                        Pedir(i);
                    }
                    else if (i == _preparo.Espaco)
                    {
                        // A mesma tecla de novo cancela o preparo; o servidor cobra a parte do custo.
                        ConexaoTempoReal.Enviar(
                            SkillsMessageIds.CancelarSkill,
                            new CancelarSkill()
                        );
                    }
                }
            }

            if (_avisoAte > 0 && agora >= _avisoAte)
            {
                _avisoAte = 0;
                _painel.EsconderAviso();
            }

            if (_poderAte > 0 && agora >= _poderAte)
            {
                _poderAte = 0;
                _painel.EsconderPoder();
            }

            if (_preparo.Expirou(agora))
            {
                EncerrarPreparo();
            }

            if (_preparo.Ativo)
            {
                _painel.MostrarPreparo(
                    ApresentacaoSkills.TextoDoPreparo(_preparo.Nome, _preparo.Espaco),
                    _preparo.Fracao(agora)
                );
            }

            _painel.Mostrar(_estado, agora, _preparo.Ativo ? _preparo.Espaco : -1);
            _painel.AtualizarNumeros(Time.unscaledDeltaTime);
        }

        private void Pedir(int espaco)
        {
            // Espaço vazio: não há o que pedir (o servidor recusaria do mesmo jeito).
            if (_estado.Skill(espaco) == null)
            {
                return;
            }

            _sequencia++;
            ConexaoTempoReal.Enviar(
                SkillsMessageIds.UsarSkill,
                new UsarSkill
                {
                    Espaco = espaco,
                    DirecaoY = DirecaoDaMira(),
                    Sequencia = _sequencia,
                }
            );
        }

        /// <summary>Para onde a câmera olha no plano do chão, em graus (0 = +Z, cresce para +X).</summary>
        private static float DirecaoDaMira()
        {
            var camera = Camera.main;
            if (camera != null)
            {
                return camera.transform.eulerAngles.y;
            }

            var jogador = EntradaNoJogo.Jogador;
            return jogador != null ? jogador.transform.eulerAngles.y : 0f;
        }

        private void AoReceberBarra(BarraDeSkillsMudou barra)
        {
            _estado.Aplicar(barra, Time.unscaledTimeAsDouble);
            BarraMudou?.Invoke(barra);
        }

        private void AoIniciarPreparo(SkillPreparoIniciado preparo)
        {
            if (preparo == null)
            {
                return;
            }

            _preparo.Iniciar(
                preparo.Espaco,
                preparo.Sequencia,
                _estado.Skill(preparo.Espaco)?.Nome ?? preparo.Id,
                preparo.PreparoMs,
                Time.unscaledTimeAsDouble
            );
            Tocar(SomDeSkill.Preparo, preparo.PreparoMs / 1000f);
        }

        private void AoCancelar(SkillCancelada cancelada)
        {
            if (cancelada == null)
            {
                return;
            }

            if (_preparo.Ativo && _preparo.Sequencia == cancelada.Sequencia)
            {
                EncerrarPreparo();
            }

            Tocar(SomDeSkill.Cancelado, 0f);
            _painel.MostrarAviso(
                ApresentacaoSkills.TextoDoCancelamento(
                    cancelada,
                    CatalogoDeSkills.Obter(cancelada.Id)
                )
            );
            _avisoAte = Time.unscaledTimeAsDouble + DuracaoDoAvisoSegundos;
        }

        // O preparo acabou (efeito, recusa, cancelamento ou o servidor não avisou): some a barra e o som.
        private void EncerrarPreparo()
        {
            _preparo.Encerrar();
            _painel.EsconderPreparo();
            if (_fonte != null)
            {
                _fonte.Stop();
            }
        }

        private void Tocar(SomDeSkill som, float segundosDoPreparo)
        {
            if (_fonte == null)
            {
                return;
            }

            if (som == SomDeSkill.Cancelado)
            {
                _fonte.Stop();
            }

            // O do preparo muda de tamanho com o tempo da skill, então não fica guardado.
            AudioClip clipe;
            if (som == SomDeSkill.Preparo || !_clipes.TryGetValue(som, out clipe))
            {
                var amostras = SonsDeSkills.Gerar(som, segundosDoPreparo);
                clipe = AudioClip.Create(
                    "Skills." + som,
                    amostras.Length,
                    1,
                    SonsDeSkills.TaxaDeAmostragem,
                    false
                );
                clipe.SetData(amostras, 0);
                if (som != SomDeSkill.Preparo)
                {
                    _clipes[som] = clipe;
                }
            }

            _fonte.PlayOneShot(clipe, VolumeDoSom);
        }

        private void AoUsar(SkillUsada usada)
        {
            if (usada != null && _preparo.Ativo && _preparo.Sequencia == usada.Sequencia)
            {
                EncerrarPreparo();
            }

            if (usada != null)
            {
                _estado.Usada(usada.Espaco, usada.RecargaMs, Time.unscaledTimeAsDouble);
            }
        }

        private void AoDispararPoder(PoderDisparou aviso)
        {
            if (aviso == null)
            {
                return;
            }

            _painel.MostrarPoder(ApresentacaoSkills.TextoDoPoder(aviso));
            _poderAte = Time.unscaledTimeAsDouble + DuracaoDoPoderSegundos;
        }

        private void AoRecusar(SkillRecusada recusa)
        {
            if (recusa == null)
            {
                return;
            }

            if (_preparo.Ativo && _preparo.Sequencia == recusa.Sequencia)
            {
                EncerrarPreparo();
            }

            _painel.MostrarAviso(ApresentacaoSkills.TextoDaRecusa(recusa.Codigo));
            _avisoAte = Time.unscaledTimeAsDouble + DuracaoDoAvisoSegundos;
        }

        private void AoAcertar(SkillAcertou acertou)
        {
            if (acertou?.Acertos == null)
            {
                return;
            }

            foreach (var acerto in acertou.Acertos)
            {
                if (acerto == null)
                {
                    continue;
                }

                var alvo = AcharAlvo(acerto.AlvoId);
                var origem = alvo != null ? AcimaDaCabeca(alvo) : OrigemSemAlvo();
                _painel.AdicionarNumero(acerto.Dano.ToString(), origem);
            }
        }

        // O boneco de treino tem id negativo fixo; os inimigos de Mobs se chamam "Inimigo <id>".
        private GameObject AcharAlvo(long alvoId)
        {
            if (_alvos.TryGetValue(alvoId, out var guardado) && guardado != null)
            {
                return guardado;
            }

            var achado = GameObject.Find(alvoId > 0 ? NomeDoInimigo + alvoId : NomeDoBoneco);
            if (achado != null)
            {
                _alvos[alvoId] = achado;
            }

            return achado;
        }

        private static Vector3 AcimaDaCabeca(GameObject alvo)
        {
            var renderers = alvo.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return alvo.transform.position + Vector3.up * (1.8f + FolgaDoNumero);
            }

            var limites = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++)
            {
                limites.Encapsulate(renderers[i].bounds);
            }

            return new Vector3(limites.center.x, limites.max.y + FolgaDoNumero, limites.center.z);
        }

        // Sem o alvo na cena, o número nasce à frente do jogador.
        private static Vector3 OrigemSemAlvo()
        {
            var jogador = EntradaNoJogo.Jogador;
            return jogador != null
                ? jogador.transform.position + jogador.transform.forward * 2f + Vector3.up * 1.5f
                : Vector3.zero;
        }
    }
}
