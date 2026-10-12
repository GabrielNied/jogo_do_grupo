using System;
using System.Collections.Generic;
using System.Globalization;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>Mostra apenas o estado recebido. Equipar e trocar conjuntos são intenções.</summary>
    public sealed class PainelEquipamentos
    {
        private readonly VisualElement _raiz;
        private readonly Action<string, object> _enviar;

        public PainelEquipamentos(VisualElement raiz, Action<string, object> enviar)
        {
            _raiz = raiz;
            _enviar = enviar;
        }

        public void Avisar(string texto)
        {
            _raiz.Clear();
            _raiz.Add(Texto(texto));
        }

        public void Mostrar(EquipamentosResponse estado, string aviso = null)
        {
            if (estado.ModoInfusao)
            {
                new PainelInfusao(_raiz, _enviar).Mostrar(estado, aviso);
                return;
            }
            _raiz.Clear();
            if (!string.IsNullOrEmpty(aviso))
                _raiz.Add(Texto(aviso));
            _raiz.Add(Texto("Conjunto de armas ativo: " + estado.ConjuntoAtivo));
            var conjuntos = new VisualElement();
            conjuntos.AddToClassList(EstiloCore.LinhaBotoes);
            for (var conjunto = 1; conjunto <= 2; conjunto++)
            {
                var numero = conjunto;
                var botao = Botao(
                    "Usar conjunto " + numero,
                    () => _enviar("conjunto", new PedidoConjunto { Conjunto = numero })
                );
                botao.SetEnabled(numero != estado.ConjuntoAtivo);
                conjuntos.Add(botao);
            }
            _raiz.Add(conjuntos);
            _raiz.Add(Texto("Bônus das peças e das armas ativas"));
            foreach (var bonus in estado.Bonus)
                _raiz.Add(
                    Texto(
                        NomeStatus(bonus.Status)
                            + ": +"
                            + bonus.Valor.ToString("0.##", CultureInfo.CurrentCulture)
                    )
                );
            var lista = new ScrollView(ScrollViewMode.Vertical);
            lista.AddToClassList(EstiloCore.Lista);
            lista.style.flexGrow = 1;
            foreach (PosicaoEquipamento posicao in Enum.GetValues(typeof(PosicaoEquipamento)))
            {
                if (posicao == PosicaoEquipamento.Inventario)
                    continue;
                EntradaInventario entrada = null;
                foreach (var item in estado.Equipados)
                    if (item.Posicao == posicao)
                        entrada = item;
                var linha = new VisualElement();
                linha.Add(
                    Texto(
                        NomePosicao(posicao)
                            + ": "
                            + (entrada == null ? "Vazio" : Descrever(entrada))
                    )
                );
                if (entrada != null)
                {
                    var destino = posicao;
                    linha.Add(
                        Botao(
                            "Desequipar",
                            () => _enviar("desequipar", new PedidoDesequipar { Posicao = destino })
                        )
                    );
                }
                lista.Add(linha);
            }
            lista.Add(
                Texto(
                    "Inventário: " + estado.Inventario.Length + "/" + RegrasItens.EspacosInventario
                )
            );
            foreach (var item in estado.Inventario)
            {
                var linha = new VisualElement();
                linha.Add(Texto(Descrever(item)));
                var dados = item.Equipamento ?? CatalogoEquipamentos.Obter(item.ItemId);
                if (dados != null)
                {
                    var destinos = new List<PosicaoEquipamento>();
                    var nomes = new List<string>();
                    foreach (
                        PosicaoEquipamento posicao in Enum.GetValues(typeof(PosicaoEquipamento))
                    )
                        if (RegrasEquipamentos.Aceita(dados.Tipo, posicao))
                        {
                            destinos.Add(posicao);
                            nomes.Add(NomePosicao(posicao));
                        }
                    var seletor = new DropdownField("Posição", nomes, 0);
                    seletor.AddToClassList(EstiloCore.Campo);
                    linha.Add(seletor);
                    var instancia = item.InstanciaId;
                    linha.Add(
                        Botao(
                            "Equipar",
                            () =>
                                _enviar(
                                    "equipar",
                                    new PedidoEquipar
                                    {
                                        InstanciaId = instancia,
                                        Posicao = destinos[seletor.index],
                                    }
                                )
                        )
                    );
                }
                linha.Add(LinhaDescartar(item.InstanciaId));
                lista.Add(linha);
            }
            _raiz.Add(lista);
            _raiz.Add(Botao("Infundir equipamentos", () => _enviar("infusao/estado", null)));
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var grauTeste = new DropdownField(
                "Grau do equipamento de teste",
                new List<string> { "Inicial", "Médio", "Forte" },
                0
            );
            grauTeste.AddToClassList(EstiloCore.Campo);
            _raiz.Add(grauTeste);
            _raiz.Add(
                Botao(
                    "Obter equipamento de teste",
                    () => _enviar("bau-teste?grau=" + grauTeste.index, null)
                )
            );
#endif
        }

        /// <summary>
        /// "Descartar" pede confirmação na própria linha: o primeiro clique troca o botão por
        /// "Confirmar descarte" e "Cancelar". Só a confirmação envia a intenção; o servidor
        /// decide (equipado e exemplar alheio ou já descartado são recusados).
        /// </summary>
        private VisualElement LinhaDescartar(long instancia)
        {
            var linha = new VisualElement();
            linha.AddToClassList(EstiloCore.LinhaBotoes);
            Button descartar = null;
            descartar = Botao(
                "Descartar",
                () =>
                {
                    linha.Clear();
                    linha.Add(Texto("Descartar destrói o item de vez. Confirmar?"));
                    linha.Add(
                        Botao(
                            "Confirmar descarte",
                            () =>
                                _enviar(
                                    "descartar",
                                    new PedidoDescartar { InstanciaId = instancia }
                                )
                        )
                    );
                    linha.Add(
                        Botao(
                            "Cancelar",
                            () =>
                            {
                                linha.Clear();
                                linha.Add(descartar);
                            }
                        )
                    );
                }
            );
            linha.Add(descartar);
            return linha;
        }

        public static string Descrever(EntradaInventario entrada)
        {
            var item = entrada.Equipamento;
            if (item == null)
                return entrada.Nome;
            var raridades = new[] { "Comum", "Incomum", "Raro", "Épico", "Lendário" };
            var texto =
                item.Nome
                + " · "
                + raridades[(int)item.Raridade]
                + " · "
                + item.Regiao
                + " · nível "
                + item.Nivel
                + "/"
                + RegrasEquipamentos.NivelMaximo(item.Raridade)
                + " · grau "
                + item.Grau;
            texto +=
                "\nRequer personagem nível "
                + item.NivelPersonagemMinimo
                + (item.DuasMaos ? " · duas mãos" : "");
            if (!string.IsNullOrEmpty(item.Familia))
                texto += "\nFamília " + item.Familia + " · variante " + item.Variacao;
            for (var i = 0; i < item.Atributos.Length; i++)
            {
                var atributo = item.Atributos[i];
                texto +=
                    "\n"
                    + NomeStatus(atributo.Status)
                    + " +"
                    + atributo.Valor.ToString("0.##", CultureInfo.CurrentCulture)
                    + (atributo.Fixo ? " (fixo)" : " (sorteado)")
                    + (i == item.IndiceInfusivel ? " · infusível" : "");
            }
            return texto
                + "\nInfusões: "
                + item.InfusoesUsadas
                + "/"
                + RegrasEquipamentos.LimiteInfusoes(item.Raridade);
        }

        public static string NomeStatus(StatusItem status)
        {
            switch (status)
            {
                case StatusItem.Forca:
                    return "Força";
                case StatusItem.PotenciaFogo:
                    return "Potência de fogo (%)";
                case StatusItem.PotenciaGelo:
                    return "Potência de gelo (%)";
                case StatusItem.Alcance:
                    return "Alcance (m)";
                case StatusItem.RecuperacaoVigor:
                    return "Recuperação de vigor (%)";
                default:
                    return status.ToString();
            }
        }

        public static string NomePosicao(PosicaoEquipamento posicao)
        {
            var nomes = new[]
            {
                "Inventário",
                "Capacete",
                "Peito",
                "Luvas",
                "Calça",
                "Botas",
                "Cinto",
                "Anel 1",
                "Anel 2",
                "Brinco 1",
                "Brinco 2",
                "Adorno",
                "Arma · conjunto 1",
                "Mão secundária · conjunto 1",
                "Arma · conjunto 2",
                "Mão secundária · conjunto 2",
            };
            return nomes[(int)posicao];
        }

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
