using System;
using System.Collections.Generic;
using JogoDoGrupo.Client.Core.Jogo;
using JogoDoGrupo.Shared.Features.Social;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Social
{
    /// <summary>
    /// Lista de membros aliados do grupo na lateral esquerda do HUD (<see cref="RegiaoDoHud.LateralEsquerda"/>).
    /// Mostra até 4 aliados (excluindo o jogador local), com nome, líder, vida exata, percentual
    /// e destaque quando a vida estiver crítica (&lt; 30%).
    /// </summary>
    internal sealed class PainelListaGrupoHud : IDisposable
    {
        public const string ClassePainel = "lista-grupo-hud";
        public const string ClasseLinha = "linha-aliado-hud";
        public const string ClasseOffline = "aliado-offline";
        public const string ClasseCritica = "vida-critica";
        public const string ClasseTopo = "linha-aliado-topo";
        public const string ClasseNome = "aliado-nome";
        public const string ClasseLider = "aliado-lider";
        public const string ClasseStatus = "aliado-status";
        public const string ClasseBarraFundo = "barra-vida-fundo";
        public const string ClasseBarraPreenchimento = "barra-vida-preenchimento";
        public const string ClasseVidaTexto = "aliado-vida-texto";

        private readonly VisualElement _container;
        private readonly EstadoDoGrupo _estado;
        private readonly List<ItemAliado> _linhas = new List<ItemAliado>(
            RegrasGrupo.MaximoDeMembros - 1
        );

        public VisualElement ElementoRaiz => _container;
        public IReadOnlyList<ItemAliado> Linhas => _linhas;

        public PainelListaGrupoHud(VisualElement regiao, EstadoDoGrupo estado)
        {
            _estado = estado ?? throw new ArgumentNullException(nameof(estado));

            _container = new VisualElement { name = "social.lista-grupo-hud" };
            _container.pickingMode = PickingMode.Ignore;
            _container.AddToClassList(ClassePainel);

            EstiloSocial.Aplicar(_container);

            for (var i = 0; i < RegrasGrupo.MaximoDeMembros - 1; i++)
            {
                var item = new ItemAliado();
                _linhas.Add(item);
                _container.Add(item.Raiz);
            }

            regiao.Add(_container);

            _estado.Mudou += Atualizar;
            Atualizar();
        }

        public void Descartar()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_estado != null)
            {
                _estado.Mudou -= Atualizar;
            }

            _container.RemoveFromHierarchy();
        }

        public void Atualizar()
        {
            if (!_estado.EmGrupo)
            {
                _container.style.display = DisplayStyle.None;
                return;
            }

            var membros = _estado.Membros;
            var localId = _estado.PersonagemLocalId;
            var liderId = _estado.LiderId;

            var indiceAliado = 0;
            for (var i = 0; i < membros.Count && indiceAliado < _linhas.Count; i++)
            {
                var membro = membros[i];
                if (membro == null || membro.PersonagemId == localId)
                {
                    continue;
                }

                _linhas[indiceAliado].Atualizar(membro, liderId);
                indiceAliado++;
            }

            for (var i = indiceAliado; i < _linhas.Count; i++)
            {
                _linhas[i].Esconder();
            }

            if (indiceAliado == 0)
            {
                _container.style.display = DisplayStyle.None;
            }
            else
            {
                _container.style.display = DisplayStyle.Flex;
            }
        }

        internal sealed class ItemAliado
        {
            public readonly VisualElement Raiz;
            public readonly Label Nome;
            public readonly Label IndicadorLider;
            public readonly Label StatusOffline;
            public readonly VisualElement BarraFundo;
            public readonly VisualElement BarraPreenchimento;
            public readonly Label TextoVida;

            public ItemAliado()
            {
                Raiz = new VisualElement();
                Raiz.pickingMode = PickingMode.Ignore;
                Raiz.AddToClassList(ClasseLinha);

                var linhaTopo = new VisualElement();
                linhaTopo.pickingMode = PickingMode.Ignore;
                linhaTopo.AddToClassList(ClasseTopo);

                Nome = new Label();
                Nome.pickingMode = PickingMode.Ignore;
                Nome.AddToClassList(ClasseNome);

                IndicadorLider = new Label("★");
                IndicadorLider.pickingMode = PickingMode.Ignore;
                IndicadorLider.AddToClassList(ClasseLider);

                StatusOffline = new Label("(Offline)");
                StatusOffline.pickingMode = PickingMode.Ignore;
                StatusOffline.AddToClassList(ClasseStatus);

                linhaTopo.Add(Nome);
                linhaTopo.Add(IndicadorLider);
                linhaTopo.Add(StatusOffline);

                BarraFundo = new VisualElement();
                BarraFundo.pickingMode = PickingMode.Ignore;
                BarraFundo.AddToClassList(ClasseBarraFundo);

                BarraPreenchimento = new VisualElement();
                BarraPreenchimento.pickingMode = PickingMode.Ignore;
                BarraPreenchimento.AddToClassList(ClasseBarraPreenchimento);

                TextoVida = new Label();
                TextoVida.pickingMode = PickingMode.Ignore;
                TextoVida.AddToClassList(ClasseVidaTexto);

                BarraFundo.Add(BarraPreenchimento);
                BarraFundo.Add(TextoVida);

                Raiz.Add(linhaTopo);
                Raiz.Add(BarraFundo);
                Esconder();
            }

            public void Esconder()
            {
                Raiz.style.display = DisplayStyle.None;
            }

            public void Atualizar(MembroDoGrupo membro, long liderId)
            {
                Raiz.style.display = DisplayStyle.Flex;
                Nome.text = membro.Nome ?? "";

                var ehLider = membro.PersonagemId == liderId;
                IndicadorLider.style.display = ehLider ? DisplayStyle.Flex : DisplayStyle.None;

                var offline = !membro.Online;
                StatusOffline.style.display = offline ? DisplayStyle.Flex : DisplayStyle.None;

                if (offline)
                {
                    Raiz.AddToClassList(ClasseOffline);
                }
                else
                {
                    Raiz.RemoveFromClassList(ClasseOffline);
                }

                var fracao =
                    membro.VidaMaxima > 0
                        ? Mathf.Clamp01((float)membro.Vida / membro.VidaMaxima)
                        : 0f;
                var percentual = Mathf.RoundToInt(fracao * 100f);

                BarraPreenchimento.style.width = Length.Percent(fracao * 100f);
                TextoVida.text =
                    $"{RegrasGrupo.TextoVidaExata(membro.Vida, membro.VidaMaxima)} ({percentual}%)";

                var critica =
                    !offline && RegrasGrupo.VidaEstaCritica(membro.Vida, membro.VidaMaxima);
                if (critica)
                {
                    Raiz.AddToClassList(ClasseCritica);
                    BarraFundo.AddToClassList(ClasseCritica);
                    BarraPreenchimento.AddToClassList(ClasseCritica);
                }
                else
                {
                    Raiz.RemoveFromClassList(ClasseCritica);
                    BarraFundo.RemoveFromClassList(ClasseCritica);
                    BarraPreenchimento.RemoveFromClassList(ClasseCritica);
                }
            }
        }
    }
}
