using System;
using JogoDoGrupo.Client.Core.Api;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Personagem;
using UnityEngine;

namespace JogoDoGrupo.Client.Features.Personagem
{
    /// <summary>
    /// Monta a barra de XP no HUD ao entrar no mundo e avisa quando o personagem logado sobe de
    /// nível (<see cref="SubiuDeNivel"/>). Mesmo padrão do observador de Social/Combate.
    /// </summary>
    internal sealed class ObservadorDaBarraXp : MonoBehaviour
    {
        private const string NomeObservador = "Personagem.BarraXpObservador";

        private static ObservadorDaBarraXp _instancia;

        private IDisposable _subiuDeNivel;
        private PainelBarraXp _painel;
        private GameObject _jogadorAtendido;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Iniciar()
        {
            if (_instancia != null)
            {
                return;
            }

            var objeto = new GameObject(NomeObservador);
            DontDestroyOnLoad(objeto);
            _instancia = objeto.AddComponent<ObservadorDaBarraXp>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
        }

        private void Awake()
        {
            _subiuDeNivel = ConexaoTempoReal.Assinar<SubiuDeNivel>(
                PersonagemMessageIds.SubiuDeNivel,
                AoSubirDeNivel
            );
        }

        private void OnDestroy()
        {
            _subiuDeNivel?.Dispose();
            _subiuDeNivel = null;
            _painel?.Descartar();
            _painel = null;
        }

        private void AoSubirDeNivel(SubiuDeNivel aviso)
        {
            if (aviso == null || aviso.PersonagemId != EntradaNoJogo.PersonagemId)
            {
                return;
            }

            AvisosDoJogo.Mostrar(TextosPersonagem.AvisoSubiuDeNivel(aviso.Nivel));
        }

        private void Update()
        {
            var jogador = EntradaNoJogo.Jogador;
            if (!EntradaNoJogo.NoJogo || jogador == null || jogador == _jogadorAtendido)
            {
                return;
            }

            _jogadorAtendido = jogador;
            try
            {
                Montar(jogador);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private void Montar(GameObject jogador)
        {
            var regiao = Hud.Regiao(RegiaoDoHud.Experiencia);
            if (regiao == null)
            {
                Debug.LogWarning(
                    "[Personagem] O HUD não está montado; a barra de XP não foi criada."
                );
                return;
            }

            _painel?.Descartar();
            _painel = new PainelBarraXp(regiao);
            // A barra começa com o progresso do servidor, sem esperar o primeiro ganho de XP.
            _ = CarregarProgresso(EntradaNoJogo.PersonagemId);
        }

        private static async Awaitable CarregarProgresso(long personagemId)
        {
            try
            {
                var resposta = await ClienteApi.Get<ProgressoResponse>(
                    RotasPersonagem.ProgressoDe(personagemId)
                );
                if (
                    resposta.Sucesso
                    && resposta.Corpo != null
                    && EntradaNoJogo.PersonagemId == personagemId
                )
                {
                    ProgressoDoClient.Estado.Aplicar(resposta.Corpo);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
