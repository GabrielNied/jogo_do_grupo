using System;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Mundo;
using JogoDoGrupo.Shared.Features.Npcs;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Npcs
{
    /// <summary>
    /// Guarda ou morador ambiente da cidade (#1069). A posição vem de uma função do relógio UTC
    /// (<see cref="RotaDeNpc.Amostrar"/>), então não usa rede e todo client vê o mesmo lugar.
    /// Não tem colisor nem alvo: ninguém o ataca nem o empurra. Fora do raio de desenho fica parado
    /// e escondido, sem custo.
    /// </summary>
    public sealed class HabitanteNoMundo : MonoBehaviour
    {
        private const float GrausPorSegundo = 360f;
        private const float SuavizacaoDaAltura = 12f;

        private static readonly DateTime Epoca = new DateTime(
            1970,
            1,
            1,
            0,
            0,
            0,
            DateTimeKind.Utc
        );

        private HabitanteDaCidade _habitante;
        private GameObject _corpo;
        private Animator _animator;
        private BalaoDeFala _balao;
        private float _proximoBalao;
        private int _contadorDeFalas;
        private bool _visivel = true;
        private bool _temAnimator;
        private int _ultimoTurnoDeConversa = -1;

        public HabitanteDaCidade Habitante => _habitante;

        public static double RelogioDaCidade() => (DateTime.UtcNow - Epoca).TotalSeconds;

        public void Configurar(HabitanteDaCidade habitante)
        {
            _habitante = habitante;
            gameObject.name = $"Habitante_{habitante.Id}_{habitante.Nome}";

            _corpo = FabricaDeCorpoDeNpc.Criar(habitante.Papel, habitante.Semente, transform);
            _animator = _corpo.GetComponentInChildren<Animator>();
            _temAnimator = _animator != null && _animator.runtimeAnimatorController != null;
            _balao = BalaoDeFala.Criar(transform);
            _contadorDeFalas = habitante.Id;

            var amostra = habitante.Rota.Amostrar(RelogioDaCidade(), habitante.Fase);
            transform.position = AjustarNoChao(new Vector3(amostra.X, 0f, amostra.Z), 0f, true);
            transform.rotation = Quaternion.Euler(0f, amostra.RotacaoY, 0f);
        }

        private void Update()
        {
            if (_habitante == null)
            {
                return;
            }

            var jogador = EntradaNoJogo.Jogador;
            if (jogador == null)
            {
                return;
            }

            var posJogador = jogador.transform.position;
            var amostra = _habitante.Rota.Amostrar(RelogioDaCidade(), _habitante.Fase);
            var dx = posJogador.x - amostra.X;
            var dz = posJogador.z - amostra.Z;
            var distancia2 = dx * dx + dz * dz;

            var perto =
                distancia2
                <= ConfigPopulacaoDaCidade.RaioDeDesenho * ConfigPopulacaoDaCidade.RaioDeDesenho;
            if (perto != _visivel)
            {
                _visivel = perto;
                _corpo.SetActive(perto);
                if (!perto)
                {
                    _balao.Esconder();
                }
            }

            if (!_visivel)
            {
                return;
            }

            var alvo = AjustarNoChao(
                new Vector3(amostra.X, transform.position.y, amostra.Z),
                Time.deltaTime,
                false
            );
            transform.position = alvo;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                Quaternion.Euler(0f, amostra.RotacaoY, 0f),
                GrausPorSegundo * Time.deltaTime
            );

            var noRaio =
                distancia2
                <= ConfigPopulacaoDaCidade.RaioDoBalao * ConfigPopulacaoDaCidade.RaioDoBalao;
            AtualizarBalao(amostra, noRaio);

            if (_temAnimator)
            {
                _animator.SetBool(ParametrosDeNpc.Andando, !amostra.Parado);
                _animator.SetBool(ParametrosDeNpc.Falando, _balao.Visivel);
            }
        }

        private void AtualizarBalao(AmostraDeRota amostra, bool noRaio)
        {
            if (!noRaio || _habitante.Falas == null || _habitante.Falas.Length == 0)
            {
                if (!noRaio)
                {
                    _balao.Esconder();
                    _ultimoTurnoDeConversa = -1;
                }

                return;
            }

            if (amostra.Conversando && _habitante.ParceiroId != 0)
            {
                // Os dois do par se revezam: um turno cada, na mesma agenda calculada do relógio.
                var turno = (int)(amostra.TempoNaPausa / ConfigPopulacaoDaCidade.DuracaoDoBalao);
                if (turno == _ultimoTurnoDeConversa)
                {
                    return;
                }

                _ultimoTurnoDeConversa = turno;
                if (turno % 2 == _habitante.Lado)
                {
                    var indice = (turno / 2) % _habitante.Falas.Length;
                    _balao.Mostrar(
                        _habitante.Falas[indice],
                        ConfigPopulacaoDaCidade.DuracaoDoBalao
                    );
                }
                else
                {
                    _balao.Esconder();
                }

                return;
            }

            _ultimoTurnoDeConversa = -1;
            if (_balao.Visivel || Time.time < _proximoBalao)
            {
                return;
            }

            var fala = _habitante.Falas[_contadorDeFalas % _habitante.Falas.Length];
            _contadorDeFalas++;
            _balao.Mostrar(fala, ConfigPopulacaoDaCidade.DuracaoDoBalao);
            _proximoBalao = Time.time + ConfigPopulacaoDaCidade.EsperaEntreBaloes;
        }

        // O chão vem do mapa compartilhado (sem raycast: o jogador e outros colisores atrapalhariam).
        private static Vector3 AjustarNoChao(Vector3 posicao, float dt, bool imediato)
        {
            var chao = MapaDoMundo.AlturaEm(posicao.x, posicao.z);
            posicao.y = imediato
                ? chao
                : Mathf.Lerp(posicao.y, chao, 1f - Mathf.Exp(-SuavizacaoDaAltura * dt));
            return posicao;
        }
    }
}
