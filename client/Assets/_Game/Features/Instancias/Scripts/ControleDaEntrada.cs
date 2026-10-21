using JogoDoGrupo.Client.Core.Audio;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.Jogo.Janelas;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Instancias;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Instancias
{
    /// <summary>
    /// A porta da dungeon em jogo: mostra "E: entrar" perto dela e, com E, pede
    /// <see cref="EntrarNaDungeon"/> ao servidor (que decide). Também leva as recusas ao aviso do
    /// HUD e o resultado da corrida ao painel. Criado pelo <see cref="ObservadorDeInstancias"/>
    /// na cena do mundo.
    /// </summary>
    internal sealed class ControleDaEntrada : MonoBehaviour
    {
        private InputAction _interagir;
        private Label _dica;
        private Label _indicadorDungeon;
        private GameObject _jogador;
        private EstadoDeInstancias _estado;
        private bool _estavaNaInstancia;
        private TelaDeResultadoDaDungeon _tela;

        public void Configurar(
            GameObject jogador,
            VisualElement raizDaInterface,
            EstadoDeInstancias estado
        )
        {
            _jogador = jogador;
            _estado = estado;

            _dica = new Label();
            _dica.pickingMode = PickingMode.Ignore;
            _dica.AddToClassList(EstiloCore.Dica);
            raizDaInterface.Add(_dica);
            MostrarDica(false);

            _indicadorDungeon = new Label();
            _indicadorDungeon.pickingMode = PickingMode.Ignore;
            _indicadorDungeon.AddToClassList(EstiloCore.Texto);
            _indicadorDungeon.style.position = Position.Absolute;
            _indicadorDungeon.style.top = 10;
            _indicadorDungeon.style.left = 10;
            _indicadorDungeon.style.paddingLeft = 8;
            _indicadorDungeon.style.paddingRight = 8;
            _indicadorDungeon.style.paddingTop = 4;
            _indicadorDungeon.style.paddingBottom = 4;
            _indicadorDungeon.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
            raizDaInterface.Add(_indicadorDungeon);
            _indicadorDungeon.style.display = DisplayStyle.None;

            _tela = new TelaDeResultadoDaDungeon(
                raizDaInterface,
                TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir)
            );

            _interagir = new InputAction(
                "InteragirDungeon",
                InputActionType.Button,
                TeclasDoJogo.Caminho(TeclasDoJogo.Interagir)
            );
            TeclasDoJogo.Mudou += AtualizarTecla;
            _interagir.AddBinding("<Gamepad>/buttonWest");
            if (isActiveAndEnabled)
            {
                _interagir.Enable();
            }

            _estado.EntradaRecusada += AoRecusar;
            _estado.EtapaConcluida += AoEtapaConcluida;
            _estado.ChefeDerrotado += AoChefeDerrotado;
            _estado.DungeonEncerrada += AoEncerrar;
        }

        private void OnEnable()
        {
            _interagir?.Enable();
        }

        private void OnDisable()
        {
            _interagir?.Disable();
        }

        private void OnDestroy()
        {
            if (_estado != null)
            {
                _estado.EntradaRecusada -= AoRecusar;
                _estado.EtapaConcluida -= AoEtapaConcluida;
                _estado.ChefeDerrotado -= AoChefeDerrotado;
                _estado.DungeonEncerrada -= AoEncerrar;
            }

            TeclasDoJogo.Mudou -= AtualizarTecla;
            _interagir?.Dispose();
            _interagir = null;
        }

        private void AtualizarTecla() =>
            _interagir?.ApplyBindingOverride(0, TeclasDoJogo.Caminho(TeclasDoJogo.Interagir));

        private static void AoRecusar(EntradaRecusada recusa) =>
            AvisosDoJogo.Mostrar(TextosInstancias.Recusa(recusa.Codigo), TipoDeAviso.Aviso);

        private static void AoEtapaConcluida(EtapaRaidConcluida etapa) =>
            AvisosDoJogo.Mostrar(
                TextosInstancias.EtapaRaid(
                    etapa.EtapaConcluida,
                    etapa.TotalEtapas,
                    etapa.EtapaFinal,
                    etapa.GanhouLootSemanal
                ),
                TipoDeAviso.Informacao
            );

        private static void AoChefeDerrotado(ChefeDerrotadoNaDungeon _)
        {
            AvisosDoJogo.Mostrar(
                "Chefe derrotado! +1 Essência de instância obtida. Colete seus itens antes de sair.",
                TipoDeAviso.Informacao
            );
            // Toque de vitória uma vez; depois o áudio volta sozinho para a faixa da dungeon.
            AudioDoJogo.PedirMusicaDoMundo(MusicaDoJogo.Vitoria);
        }

        private void AoEncerrar(DungeonEncerrada encerrada) => _tela?.Mostrar(encerrada);

        private void Update()
        {
            if (_dica == null || _jogador == null || !EntradaNoJogo.NoJogo)
            {
                return;
            }

            // Música da área: dungeon dentro da instância, cidade fora (só na troca).
            if (_estado.NaInstancia != _estavaNaInstancia)
            {
                _estavaNaInstancia = _estado.NaInstancia;
                AudioDoJogo.PedirMusicaDoMundo(
                    _estavaNaInstancia ? MusicaDoJogo.Dungeon : MusicaDoJogo.Cidade
                );
            }

            if (_indicadorDungeon != null)
            {
                if (_estado.NaInstancia)
                {
                    _indicadorDungeon.style.display = DisplayStyle.Flex;
                    var restantes = Mathf.Max(
                        0,
                        Mathf.CeilToInt((float)_estado.SegundosRestantesNoCronometro)
                    );
                    var tempoStr = TextosInstancias.FormatarTempo(restantes);
                    if (_estado.EhRaid)
                    {
                        _indicadorDungeon.text =
                            $"Raid 10 · Portão {_estado.EtapaAtual}/{_estado.TotalEtapas} · {tempoStr}";
                    }
                    else
                    {
                        _indicadorDungeon.text =
                            $"{TextosInstancias.NomeDaDungeon} · nível {_estado.NivelAtual} · {tempoStr}";
                    }
                }
                else
                {
                    _indicadorDungeon.style.display = DisplayStyle.None;
                }
            }

            _tela.Atualizar();
            var apertou = _interagir.WasPressedThisFrame();
            if (_tela.Visivel && apertou)
            {
                _tela.Fechar();
                return;
            }

            if (_estado.NaInstancia && _estado.VitoriaAlcancada && !JanelasDoJogo.MenuAberto)
            {
                MostrarDica(true);
                _dica.text = TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir) + ": sair da dungeon";
                if (apertou)
                {
                    _estado.PedirSair();
                }
                return;
            }

            var posicao = _jogador.transform.position;
            var perto =
                !_estado.NaInstancia
                && !JanelasDoJogo.MenuAberto
                && LayoutDaMinaDoReflexo.PertoDaEntrada(posicao.x, posicao.z);
            MostrarDica(perto);
            if (perto)
            {
                _dica.text =
                    TeclasDoJogo.Rotulo(TeclasDoJogo.Interagir)
                    + ": entrar na "
                    + TextosInstancias.NomeDaDungeon;
                if (apertou)
                {
                    ConexaoTempoReal.Enviar(
                        InstanciasMessageIds.EntrarNaDungeon,
                        new EntrarNaDungeon(),
                        EntregaTempoReal.Confiavel
                    );
                }
            }
        }

        private void MostrarDica(bool mostrar)
        {
            _dica.style.display = mostrar ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
