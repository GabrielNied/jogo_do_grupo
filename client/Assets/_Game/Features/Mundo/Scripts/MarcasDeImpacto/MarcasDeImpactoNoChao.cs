using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Client.Core.TempoReal;
using JogoDoGrupo.Shared.Features.Combate;
using JogoDoGrupo.Shared.Features.Itens;
using JogoDoGrupo.Shared.Features.Skills;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JogoDoGrupo.Client.Features.Mundo.MarcasDeImpacto
{
    /// <summary>
    /// Marcas de impacto no chão (mundo.md, "Luz e som"): martelada no impacto da Martelada Sísmica
    /// e do golpe pesado de martelo, fuligem no de skill de fogo e de bola de fogo. Só visual, em
    /// cada client: lê as mensagens que o servidor já manda (<see cref="SkillAcertou"/>,
    /// <see cref="AtaqueAcertou"/>, <see cref="ProjetilLancado"/> e <see cref="ArmaAtivaInformada"/>,
    /// do shared) e não envia nada. A marca é um quadrado achatado sobre o chão, não toca no
    /// relevo nem em colisão, dura <see cref="ConfigMarcasDeImpacto.DuracaoSegundos"/> com fade e
    /// há no máximo <see cref="ConfigMarcasDeImpacto.MaximoDeMarcas"/> ao mesmo tempo.
    /// </summary>
    internal sealed class MarcasDeImpactoNoChao : MonoBehaviour
    {
        private const string NomeDoInimigo = "Inimigo ";
        private const string NomeDoBoneco = "Boneco de treino";
        private const float AlturaDoRaio = 1.5f;
        private const float AlcanceDoRaio = 6f;

        private static MarcasDeImpactoNoChao _instancia;
        private static bool _ligado;
        private static TipoEquipamento _armaLocal = TipoEquipamento.Espada;

        // Projéteis de fogo no ar: atacante -> até quando o acerto ainda conta.
        private static readonly Dictionary<long, List<double>> ProjeteisDeFogo =
            new Dictionary<long, List<double>>();

        private readonly FilaDeMarcas<MarcaViva> _fila = new FilaDeMarcas<MarcaViva>();

        private sealed class MarcaViva
        {
            public GameObject Objeto;
            public Material Material;
            public Color Cor;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reiniciar()
        {
            _instancia = null;
            _ligado = false;
            _armaLocal = TipoEquipamento.Espada;
            ProjeteisDeFogo.Clear();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Ligar()
        {
            if (_ligado)
            {
                return;
            }

            _ligado = true;
            ConexaoTempoReal.Assinar<SkillAcertou>(SkillsMessageIds.SkillAcertou, AoSkillAcertar);
            ConexaoTempoReal.Assinar<AtaqueAcertou>(
                CombateMessageIds.AtaqueAcertou,
                AoAtaqueAcertar
            );
            ConexaoTempoReal.Assinar<ProjetilLancado>(
                CombateMessageIds.ProjetilLancado,
                AoProjetilLancado
            );
            ConexaoTempoReal.Assinar<ArmaAtivaInformada>(
                CombateMessageIds.ArmaAtivaInformada,
                info =>
                {
                    if (info != null)
                    {
                        _armaLocal = info.TipoArma;
                    }
                }
            );
        }

        private static void AoSkillAcertar(SkillAcertou msg)
        {
            if (msg?.Acertos != null && DecisaoDeMarca.DaSkill(msg.Id, out var tipo))
            {
                MarcarAlvos(msg.Acertos, tipo);
            }
        }

        private static void AoProjetilLancado(ProjetilLancado msg)
        {
            if (msg == null || msg.TipoProjetil != TipoDeProjetil.BolaDeFogo)
            {
                return;
            }

            if (!ProjeteisDeFogo.TryGetValue(msg.AtacanteId, out var validades))
            {
                validades = new List<double>();
                ProjeteisDeFogo[msg.AtacanteId] = validades;
            }

            var agora = Time.timeAsDouble;
            validades.RemoveAll(v => v < agora);
            validades.Add(agora + ConfigMarcasDeImpacto.ValidadeDoProjetilSegundos);
        }

        private static void AoAtaqueAcertar(AtaqueAcertou msg)
        {
            if (msg?.Acertos == null || msg.Acertos.Length == 0)
            {
                return;
            }

            if (ConsumirProjetilDeFogo(msg.AtacanteId))
            {
                MarcarAlvos(msg.Acertos, TipoDeMarca.Fuligem);
                return;
            }

            // A arma só é conhecida para o jogador local (ArmaAtivaInformada vai ao dono).
            if (
                msg.AtacanteId == EntradaNoJogo.PersonagemId
                && DecisaoDeMarca.DoGolpePesadoDeMartelo(
                    msg.Tipo == TipoDeAtaque.Pesado,
                    _armaLocal
                )
            )
            {
                MarcarAlvos(msg.Acertos, TipoDeMarca.Martelada);
            }
        }

        private static bool ConsumirProjetilDeFogo(long atacanteId)
        {
            if (!ProjeteisDeFogo.TryGetValue(atacanteId, out var validades))
            {
                return false;
            }

            var agora = Time.timeAsDouble;
            validades.RemoveAll(v => v < agora);
            if (validades.Count == 0)
            {
                return false;
            }

            validades.RemoveAt(0);
            return true;
        }

        private static void MarcarAlvos(AcertoDeAtaque[] acertos, TipoDeMarca tipo)
        {
            foreach (var acerto in acertos)
            {
                if (acerto == null)
                {
                    continue;
                }

                var alvo = AcharAlvo(acerto.AlvoId);
                if (alvo != null)
                {
                    Garantir().Marcar(alvo.transform, tipo);
                }
            }
        }

        // O boneco de treino tem id negativo fixo; os inimigos de Mobs se chamam "Inimigo <id>".
        private static GameObject AcharAlvo(long alvoId) =>
            GameObject.Find(alvoId > 0 ? NomeDoInimigo + alvoId : NomeDoBoneco);

        private static MarcasDeImpactoNoChao Garantir()
        {
            if (_instancia == null)
            {
                var raiz = new GameObject("Marcas de impacto");
                DontDestroyOnLoad(raiz);
                _instancia = raiz.AddComponent<MarcasDeImpactoNoChao>();
            }

            return _instancia;
        }

        private void OnEnable() => SceneManager.activeSceneChanged += AoTrocarDeCena;

        private void OnDisable() => SceneManager.activeSceneChanged -= AoTrocarDeCena;

        private void AoTrocarDeCena(Scene antiga, Scene nova) => Remover(_fila.Limpar());

        private void Update()
        {
            var agora = Time.timeAsDouble;
            Remover(_fila.Expirar(agora, ConfigMarcasDeImpacto.DuracaoSegundos));
            foreach (var par in _fila.ComIdade(agora))
            {
                var opacidade =
                    FilaDeMarcas<MarcaViva>.Opacidade(
                        par.Value,
                        ConfigMarcasDeImpacto.DuracaoSegundos,
                        ConfigMarcasDeImpacto.FadeSegundos
                    ) * ConfigMarcasDeImpacto.OpacidadeMaxima;
                var cor = par.Key.Cor;
                cor.a = opacidade;
                par.Key.Material.color = cor;
            }
        }

        private void OnDestroy() => Remover(_fila.Limpar());

        private void Marcar(Transform alvo, TipoDeMarca tipo)
        {
            var origem = alvo.position;
            var ponto = origem;
            var normal = Vector3.up;
            if (
                Physics.Raycast(
                    origem + Vector3.up * AlturaDoRaio,
                    Vector3.down,
                    out var batida,
                    AlcanceDoRaio,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore
                ) && !batida.transform.IsChildOf(alvo)
            )
            {
                ponto = batida.point;
                normal = batida.normal;
            }

            var diametro =
                tipo == TipoDeMarca.Martelada
                    ? ConfigMarcasDeImpacto.DiametroMartelada
                    : ConfigMarcasDeImpacto.DiametroFuligem;
            var objeto = new GameObject(
                tipo == TipoDeMarca.Martelada ? "Marca martelada" : "Marca fuligem"
            );
            objeto.transform.SetParent(transform, false);
            objeto.transform.position = ponto + normal * ConfigMarcasDeImpacto.FolgaDoChao;
            objeto.transform.rotation =
                Quaternion.FromToRotation(Vector3.up, normal)
                * Quaternion.Euler(0f, UnityEngine.Random.value * 360f, 0f);
            objeto.transform.localScale = new Vector3(diametro, 1f, diametro);

            var filtro = objeto.AddComponent<MeshFilter>();
            filtro.sharedMesh = TexturasDasMarcas.Quadrado();
            var renderer = objeto.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var material = new Material(
                Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit")
            )
            {
                mainTexture = TexturasDasMarcas.Obter(tipo),
            };
            renderer.sharedMaterial = material;

            var viva = new MarcaViva
            {
                Objeto = objeto,
                Material = material,
                Cor = Color.white,
            };
            Remover(_fila.Adicionar(viva, Time.timeAsDouble, ConfigMarcasDeImpacto.Teto));
        }

        private static void Remover(List<MarcaViva> marcas)
        {
            foreach (var marca in marcas)
            {
                if (marca.Material != null)
                {
                    Destroy(marca.Material);
                }

                if (marca.Objeto != null)
                {
                    Destroy(marca.Objeto);
                }
            }
        }
    }
}
