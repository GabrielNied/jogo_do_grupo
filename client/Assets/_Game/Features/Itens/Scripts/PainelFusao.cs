using System;
using System.Collections.Generic;
using System.Globalization;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Bancada de fusão (I → Bancada, issue #616): o jogador escolhe o item principal, o material
    /// e um atributo, vê a prévia completa e confirma. Quem decide é o servidor; aqui só se
    /// filtram as escolhas com as mesmas regras puras (<see cref="RegrasFusao"/>) e se mostra o
    /// que ele respondeu.
    /// </summary>
    public sealed class PainelFusao
    {
        private readonly VisualElement _raiz;
        private readonly Action<string, object> _enviar;

        public PainelFusao(VisualElement raiz, Action<string, object> enviar)
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
            lista.Add(Texto("Fusão · saldo: " + estado.Coroas + " coroas"));
            if (!string.IsNullOrEmpty(aviso ?? estado.Aviso))
                lista.Add(Texto(aviso ?? estado.Aviso));
            lista.Add(
                Texto(
                    "Escolha a peça principal, um material da mesma categoria e um atributo que os dois tenham. O atributo anda metade da diferença até o valor do material (pode diminuir), a peça sobe um nível de fusão, até +10, e o material é destruído. Não há chance de falha."
                )
            );
            lista.Add(Botao("Voltar aos equipamentos", () => _enviar(null, null)));
            var principais = new List<EntradaInventario>();
            foreach (var item in estado.Equipados)
                if (RegrasEquipamentos.DadosDe(item) != null)
                    principais.Add(item);
            foreach (var item in estado.Inventario)
                if (RegrasEquipamentos.DadosDe(item) != null)
                    principais.Add(item);
            if (principais.Count == 0)
            {
                lista.Add(
                    Texto(
                        "Você precisa de equipamentos para fundir. Volte aos equipamentos para obter peças."
                    )
                );
            }
            else
            {
                MontarEscolhas(lista, estado, principais);
            }

            lista.Add(Texto("Favoritos: a proteção impede consumir uma peça como material."));
            foreach (var item in principais)
            {
                var entrada = item;
                lista.Add(
                    Botao(
                        (item.Equipamento.Favorito ? "Desproteger " : "Proteger ") + Nome(item),
                        () =>
                            _enviar(
                                "fusao/favorito",
                                new PedidoFavorito
                                {
                                    InstanciaId = entrada.InstanciaId,
                                    Favorito = !entrada.Equipamento.Favorito,
                                }
                            )
                    )
                );
            }
#if DEBUG
            lista.Add(Botao("Receber coroas de teste", () => _enviar("fusao/coroas-teste", null)));
#endif
        }

        private void MontarEscolhas(
            VisualElement lista,
            EquipamentosResponse estado,
            List<EntradaInventario> principais
        )
        {
            var previa = estado.Previa;
            var indicePrincipal = 0;
            for (var i = 0; i < principais.Count; i++)
                if (previa != null && principais[i].InstanciaId == previa.PrincipalId)
                    indicePrincipal = i;
            var nomes = new List<string>();
            foreach (var item in principais)
                nomes.Add(Nome(item));
            var principal = new DropdownField("Peça principal (resultado)", nomes, indicePrincipal);
            principal.name = "principal-fusao";
            principal.AddToClassList(EstiloCore.Campo);
            lista.Add(principal);
            var detalhesPrincipal = Texto("");
            lista.Add(detalhesPrincipal);

            var materiais = new List<EntradaInventario>();
            var material = new DropdownField("Material (será consumido)");
            material.name = "material-fusao";
            material.AddToClassList(EstiloCore.Campo);
            lista.Add(material);
            var detalhesMaterial = Texto("");
            detalhesMaterial.name = "detalhes-material-fusao";
            lista.Add(detalhesMaterial);

            var atributos = new List<StatusItem>();
            var atributo = new DropdownField("Atributo");
            atributo.name = "atributo-fusao";
            atributo.AddToClassList(EstiloCore.Campo);
            lista.Add(atributo);

            var motivo = Texto("");
            motivo.name = "motivo-fusao";
            lista.Add(motivo);
            var areaPrevia = new VisualElement();
            var consultar = Botao(
                "Ver prévia",
                () =>
                {
                    if (
                        material.index < 0
                        || material.index >= materiais.Count
                        || atributo.index < 0
                        || atributo.index >= atributos.Count
                    )
                        return;
                    _enviar(
                        "fusao/previa",
                        new PedidoFusao
                        {
                            PrincipalId = principais[principal.index].InstanciaId,
                            MaterialId = materiais[material.index].InstanciaId,
                            Status = atributos[atributo.index],
                        }
                    );
                }
            );
            consultar.name = "previa-fusao";
            lista.Add(consultar);
            lista.Add(areaPrevia);

            Action atualizarAtributos = () =>
            {
                atributos.Clear();
                var opcoes = new List<string>();
                if (material.index >= 0 && material.index < materiais.Count)
                {
                    var dadosPrincipal = principais[principal.index].Equipamento;
                    var dadosMaterial = materiais[material.index].Equipamento;
                    foreach (var status in RegrasFusao.Elegiveis(dadosPrincipal, dadosMaterial))
                    {
                        atributos.Add(status);
                        opcoes.Add(
                            TextosEquipamento.NomeStatus(status)
                                + " · "
                                + Numero(RegrasFusao.Procurar(dadosPrincipal, status).Valor)
                                + " → material "
                                + Numero(RegrasFusao.Procurar(dadosMaterial, status).Valor)
                        );
                    }
                }

                atributo.choices = opcoes;
                atributo.index = opcoes.Count == 0 ? -1 : 0;
                var dono = principais[principal.index].Equipamento;
                var noMaximo = dono.NivelFusao >= ConfigFusao.NivelMaximo;
                consultar.SetEnabled(opcoes.Count > 0 && !noMaximo);
                motivo.text = MotivoSemEscolha(
                    noMaximo,
                    materiais.Count,
                    opcoes.Count,
                    dono.NivelFusao
                );
                detalhesMaterial.text =
                    material.index >= 0 && material.index < materiais.Count
                        ? "Material que será consumido:\n"
                            + TextosEquipamento.Descrever(materiais[material.index])
                        : "";
            };

            Action atualizarMateriais = () =>
            {
                materiais.Clear();
                var opcoes = new List<string>();
                var dono = principais[principal.index];
                detalhesPrincipal.text = TextosEquipamento.Descrever(dono);
                foreach (var item in estado.Inventario)
                    if (
                        item.InstanciaId != dono.InstanciaId
                        && RegrasEquipamentos.DadosDe(item) != null
                        && item.Equipamento.Tipo == dono.Equipamento.Tipo
                        && !item.Equipamento.Favorito
                    )
                    {
                        materiais.Add(item);
                        opcoes.Add(Nome(item));
                    }

                material.choices = opcoes;
                material.index = opcoes.Count == 0 ? -1 : 0;
            };

            atualizarMateriais();
            if (previa != null)
            {
                for (var i = 0; i < materiais.Count; i++)
                    if (materiais[i].InstanciaId == previa.MaterialId)
                        material.index = i;
            }

            atualizarAtributos();
            if (previa != null)
            {
                for (var i = 0; i < atributos.Count; i++)
                    if (atributos[i] == previa.Status)
                        atributo.index = i;
                MontarPrevia(areaPrevia, estado, previa);
            }

            principal.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                areaPrevia.Clear();
                atualizarMateriais();
                atualizarAtributos();
            });
            material.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                areaPrevia.Clear();
                atualizarAtributos();
            });
            atributo.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                areaPrevia.Clear();
            });
        }

        private void MontarPrevia(
            VisualElement area,
            EquipamentosResponse estado,
            PreviaFusao previa
        )
        {
            area.Add(Texto(DescreverPrevia(previa)));
            var semCoroas = estado.Coroas < previa.Custo;
            var confirmar = Botao(
                "Confirmar fusão e consumir material",
                () =>
                    _enviar(
                        "fusao/confirmar",
                        new PedidoFusao
                        {
                            PrincipalId = previa.PrincipalId,
                            MaterialId = previa.MaterialId,
                            Status = previa.Status,
                            Confirmacao = previa.Confirmacao,
                        }
                    )
            );
            confirmar.name = "confirmar-fusao";
            confirmar.SetEnabled(!semCoroas && previa.MateriaisSuficientes);
            area.Add(confirmar);
            if (semCoroas)
                area.Add(
                    Texto(
                        "Coroas insuficientes: faltam "
                            + (previa.Custo - estado.Coroas)
                            + " para confirmar esta fusão."
                    )
                );
            if (!previa.MateriaisSuficientes)
                area.Add(
                    Texto(
                        "Faltam os materiais exigidos ("
                            + DescreverMateriais(previa)
                            + "). Eles ainda não existem no jogo."
                    )
                );
            area.Add(Botao("Cancelar prévia", () => _enviar("fusao/estado", null)));
        }

        /// <summary>
        /// Por que não dá para pedir a prévia agora (texto vazio quando dá), para o jogador saber
        /// o que fazer.
        /// </summary>
        public static string MotivoSemEscolha(
            bool noMaximo,
            int materiaisCompativeis,
            int atributosEmComum,
            int nivel
        )
        {
            if (noMaximo)
                return "Esta peça já está em +" + nivel + " e não pode ser fundida de novo.";
            if (materiaisCompativeis == 0)
                return "Não há material compatível na bolsa: precisa ser da mesma categoria de peça, estar na bolsa e não ser favorito.";
            if (atributosEmComum == 0)
                return "Essa peça e o material não têm nenhum atributo em comum para fundir.";
            return "";
        }

        public static string DescreverPrevia(PreviaFusao previa) =>
            previa.Principal
            + " (#"
            + previa.PrincipalId
            + ")\nMaterial: "
            + previa.Material
            + " (#"
            + previa.MaterialId
            + ") · compatível: mesma categoria ("
            + TextosEquipamento.NomeDoTipo(previa.Categoria)
            + ")\n"
            + TextosEquipamento.NomeStatus(previa.Status)
            + ": "
            + previa.ValorAtual
            + " → "
            + previa.ValorDepois
            + " ("
            + (previa.ValorDepois - previa.ValorAtual).ToString(
                "+0;-0;0",
                CultureInfo.InvariantCulture
            )
            + "; material "
            + previa.ValorMaterial
            + ")\nNível de fusão: +"
            + previa.NivelAtual
            + " → +"
            + previa.NivelDepois
            + " · Tier "
            + RegrasTier.Letra(previa.TierAntes)
            + " → "
            + RegrasTier.Letra(previa.TierDepois)
            + "\nCusto: "
            + previa.Custo
            + " coroas · materiais: "
            + DescreverMateriais(previa)
            + "\nO material "
            + previa.Material
            + " (#"
            + previa.MaterialId
            + ") será consumido e não volta.";

        private static string DescreverMateriais(PreviaFusao previa)
        {
            var partes = new List<string>();
            if (previa.MateriaisDungeon > 0)
                partes.Add(previa.MateriaisDungeon + " de dungeon");
            if (previa.MateriaisRaid > 0)
                partes.Add(previa.MateriaisRaid + " de raid");
            return partes.Count == 0 ? "nenhum" : string.Join(" e ", partes);
        }

        private static string Numero(double valor) =>
            ((int)Math.Round(valor, MidpointRounding.AwayFromZero)).ToString(
                CultureInfo.CurrentCulture
            );

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
