using System;
using System.Collections.Generic;
using System.Globalization;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    public sealed class PainelInfusao
    {
        private readonly VisualElement _raiz;
        private readonly Action<string, object> _enviar;

        public PainelInfusao(VisualElement raiz, Action<string, object> enviar)
        {
            _raiz = raiz;
            _enviar = enviar;
        }

        public void Mostrar(EquipamentosResponse estado, string aviso = null)
        {
            _raiz.Clear();
            var lista = new ScrollView(ScrollViewMode.Vertical);
            lista.AddToClassList(EstiloCore.Lista);
            lista.style.flexGrow = 1;
            _raiz.Add(lista);
            lista.Add(Texto("Infusão · saldo: " + estado.Coroas + " coroas"));
            if (!string.IsNullOrEmpty(aviso ?? estado.Aviso))
                lista.Add(Texto(aviso ?? estado.Aviso));
            lista.Add(
                Texto(
                    "Cada doador do mesmo tipo e raridade aumenta o atributo marcado em 2% do valor-base. O doador será destruído após a confirmação."
                )
            );
            lista.Add(Botao("Voltar aos equipamentos", () => _enviar(null, null)));
            var receptores = new List<EntradaInventario>();
            foreach (var item in estado.Equipados)
                if (RegrasEquipamentos.DadosDe(item) != null)
                    receptores.Add(item);
            foreach (var item in estado.Inventario)
                if (RegrasEquipamentos.DadosDe(item) != null)
                    receptores.Add(item);
            if (receptores.Count == 0)
            {
                lista.Add(
                    Texto(
                        "Você precisa de equipamentos para infundir. Volte aos equipamentos para obter peças."
                    )
                );
            }
            else
            {
                var nomes = new List<string>();
                var indiceReceptor = 0;
                for (var i = 0; i < receptores.Count; i++)
                {
                    nomes.Add(Nome(receptores[i]));
                    if (
                        estado.Previa != null
                        && receptores[i].InstanciaId == estado.Previa.ReceptorId
                    )
                        indiceReceptor = i;
                }
                var receptor = new DropdownField(
                    "Equipamento que vai melhorar",
                    nomes,
                    indiceReceptor
                );
                receptor.name = "receptor-infusao";
                receptor.AddToClassList(EstiloCore.Campo);
                lista.Add(receptor);
                var doadores = new List<EntradaInventario>();
                var doador = new DropdownField("Doador que será consumido");
                doador.name = "doador-infusao";
                doador.AddToClassList(EstiloCore.Campo);
                lista.Add(doador);
                var detalhes = Texto("");
                lista.Add(detalhes);
                var detalhesDoador = Texto("");
                detalhesDoador.name = "detalhes-doador-infusao";
                lista.Add(detalhesDoador);
                Action mostrarDoador = () =>
                    detalhesDoador.text =
                        doador.index >= 0 && doador.index < doadores.Count
                            ? "Doador que será consumido:\n"
                                + TextosEquipamento.Descrever(doadores[doador.index])
                            : "";
                var areaPrevia = new VisualElement();
                PreviaInfusao previa = estado.Previa;
                var consultar = Botao(
                    "Ver prévia",
                    () =>
                    {
                        if (doador.index < 0 || doador.index >= doadores.Count)
                            return;
                        _enviar(
                            "infusao/previa",
                            new PedidoInfusao
                            {
                                ReceptorId = receptores[receptor.index].InstanciaId,
                                DoadorId = doadores[doador.index].InstanciaId,
                            }
                        );
                    }
                );
                lista.Add(consultar);
                lista.Add(areaPrevia);
                Action atualizar = () =>
                {
                    doadores.Clear();
                    var opcoes = new List<string>();
                    var recebe = receptores[receptor.index];
                    detalhes.text = TextosEquipamento.Descrever(recebe);
                    foreach (var item in estado.Inventario)
                        if (
                            item.InstanciaId != recebe.InstanciaId
                            && RegrasEquipamentos.DadosDe(item) != null
                            && RegrasInfusao.Validar(recebe.Equipamento, item.Equipamento) == null
                        )
                        {
                            doadores.Add(item);
                            opcoes.Add(Nome(item));
                        }
                    doador.choices = opcoes;
                    doador.index = opcoes.Count == 0 ? -1 : 0;
                    consultar.SetEnabled(opcoes.Count > 0);
                    mostrarDoador();
                    if (opcoes.Count == 0)
                        detalhes.text +=
                            "\nNão há doador compatível disponível, ou a peça atingiu o limite. Retire o doador equipado ou desmarque seu favorito, se necessário.";
                };
                atualizar();
                if (previa != null)
                {
                    for (var i = 0; i < doadores.Count; i++)
                        if (doadores[i].InstanciaId == previa.DoadorId)
                            doador.index = i;
                    mostrarDoador();
                    areaPrevia.Add(Texto(DescreverPrevia(previa)));
                    var confirmar = Botao(
                        "Confirmar infusão e consumir doador",
                        () =>
                        {
                            if (previa != null)
                                _enviar(
                                    "infusao/confirmar",
                                    new PedidoInfusao
                                    {
                                        ReceptorId = previa.ReceptorId,
                                        DoadorId = previa.DoadorId,
                                        Confirmacao = previa.Confirmacao,
                                    }
                                );
                        }
                    );
                    confirmar.SetEnabled(estado.Coroas >= previa.Custo);
                    areaPrevia.Add(confirmar);
                    if (estado.Coroas < previa.Custo)
                        areaPrevia.Add(Texto("Coroas insuficientes para confirmar esta infusão."));
                    areaPrevia.Add(Botao("Cancelar prévia", () => _enviar("infusao/estado", null)));
                }
                receptor.RegisterValueChangedCallback(_ =>
                {
                    previa = null;
                    areaPrevia.Clear();
                    atualizar();
                });
                doador.RegisterValueChangedCallback(_ =>
                {
                    previa = null;
                    areaPrevia.Clear();
                    mostrarDoador();
                });
            }
            lista.Add(Texto("Favoritos: a proteção impede consumir uma peça como doador."));
            foreach (var item in receptores)
            {
                var entrada = item;
                lista.Add(
                    Botao(
                        (item.Equipamento.Favorito ? "Desproteger " : "Proteger ") + Nome(item),
                        () =>
                            _enviar(
                                "infusao/favorito",
                                new PedidoFavorito
                                {
                                    InstanciaId = entrada.InstanciaId,
                                    Favorito = !entrada.Equipamento.Favorito,
                                }
                            )
                    )
                );
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            lista.Add(
                Botao("Receber coroas de teste", () => _enviar("infusao/coroas-teste", null))
            );
#endif
        }

        public static string DescreverPrevia(PreviaInfusao previa) =>
            previa.Receptor
            + "\n"
            + TextosEquipamento.NomeStatus(previa.Status)
            + ": "
            + previa.Antes.ToString("0.######", CultureInfo.CurrentCulture)
            + " → "
            + previa.Depois.ToString("0.######", CultureInfo.CurrentCulture)
            + "\nInfusões: "
            + previa.SlotsDepois
            + "/"
            + previa.Limite
            + " · custo: "
            + previa.Custo
            + " coroas\nSerá consumido: "
            + previa.Doador
            + " (#"
            + previa.DoadorId
            + ")";

        private static string Nome(EntradaInventario item) =>
            item.Equipamento.Nome + " (#" + item.InstanciaId + ")";

        private static Label Texto(string texto)
        {
            var label = new Label(texto);
            label.AddToClassList(EstiloCore.Texto);
            label.style.whiteSpace = WhiteSpace.Normal;
            return label;
        }

        private static Button Botao(string texto, Action acao)
        {
            var botao = new Button(acao) { text = texto };
            botao.AddToClassList(EstiloCore.Botao);
            return botao;
        }
    }
}
