using System;
using System.Collections.Generic;
using System.Globalization;
using JogoDoGrupo.Client.Core.UI;
using JogoDoGrupo.Shared.Features.Itens;
using UnityEngine;
using UnityEngine.UIElements;

namespace JogoDoGrupo.Client.Features.Itens
{
    /// <summary>
    /// Bancada de fusão (I → Bancada, issue #616, melhoria #755): o jogador escolhe o item principal,
    /// o material e um atributo com layout Lumora UI e casas clicáveis com tier visível, vê a prévia
    /// em bloco fixo com motivos de validação e confirma. O servidor decide tudo de forma autoritativa.
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

            // Cabeçalho da Bancada
            var cabecalho = new VisualElement();
            cabecalho.AddToClassList(EstiloItens.CabecalhoSecao);
            var saldoTexto =
                "Fusão · Saldo: "
                + estado.Coroas
                + " coroas · "
                + estado.MateriaisDungeon
                + " "
                + ConfigFusao.NomeMaterialDungeon
                + " · "
                + estado.MateriaisRaid
                + " "
                + ConfigFusao.NomeMaterialRaid;
            cabecalho.Add(Texto(saldoTexto, EstiloItens.TituloSecao));
            lista.Add(cabecalho);

            if (!string.IsNullOrEmpty(aviso ?? estado.Aviso))
            {
                var msgAviso = Texto(aviso ?? estado.Aviso, EstiloCore.Mensagem);
                msgAviso.AddToClassList(EstiloCore.MensagemAviso);
                lista.Add(msgAviso);
            }

            lista.Add(
                Texto(
                    "Aprimore a peça principal consumindo um material da mesma categoria. O atributo escolhido avança metade da diferença até o do material (sem falha aleatória) e a peça sobe de nível (até +10).",
                    EstiloCore.Texto
                )
            );

            lista.Add(Botao("← Voltar aos equipamentos", () => _enviar(null, null)));

            var principais = new List<EntradaInventario>();
            foreach (var item in estado.Equipados)
            {
                if (RegrasEquipamentos.DadosDe(item) != null)
                {
                    principais.Add(item);
                }
            }
            foreach (var item in estado.Inventario)
            {
                if (RegrasEquipamentos.DadosDe(item) != null)
                {
                    principais.Add(item);
                }
            }

            if (principais.Count == 0)
            {
                lista.Add(
                    Texto(
                        "Você precisa de equipamentos para fundir. Volte aos equipamentos para obter peças.",
                        EstiloCore.Mensagem
                    )
                );
                return;
            }

            MontarEscolhas(lista, estado, principais);

            // Seção de favoritos / proteção
            var blocoFav = new VisualElement();
            blocoFav.AddToClassList(EstiloItens.Resumo);
            blocoFav.Add(
                Texto(
                    "Proteção de favoritos: itens protegidos (★) nunca podem ser consumidos como material.",
                    EstiloItens.ResumoTitulo
                )
            );
            var linhaFav = new VisualElement();
            linhaFav.AddToClassList(EstiloCore.LinhaBotoes);
            linhaFav.style.flexWrap = Wrap.Wrap;
            foreach (var item in principais)
            {
                var entrada = item;
                linhaFav.Add(
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
            blocoFav.Add(linhaFav);
            lista.Add(blocoFav);

#if DEBUG
            var linhaBotoesTeste = new VisualElement();
            linhaBotoesTeste.AddToClassList(EstiloCore.LinhaBotoes);
            linhaBotoesTeste.Add(
                Botao("Receber coroas de teste", () => _enviar("fusao/coroas-teste", null))
            );
            linhaBotoesTeste.Add(
                Botao("Receber essências de teste", () => _enviar("fusao/materiais-teste", null))
            );
            lista.Add(linhaBotoesTeste);
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
            {
                if (previa != null && principais[i].InstanciaId == previa.PrincipalId)
                {
                    indicePrincipal = i;
                }
            }

            // --- Palco Hero da Fusão ---
            var palco = new VisualElement();
            palco.AddToClassList(EstiloItens.FusaoPalco);
            lista.Add(palco);

            var destaquePrincipal = new VisualElement();
            destaquePrincipal.AddToClassList(EstiloItens.FusaoSlotDestaque);
            var rotuloHeroPrincipal = Texto(
                "Peça Principal (Resultado)",
                EstiloItens.FusaoSlotRotulo
            );
            destaquePrincipal.Add(rotuloHeroPrincipal);
            var containerHeroPrincipal = new VisualElement();
            destaquePrincipal.Add(containerHeroPrincipal);
            var nomeHeroPrincipal = Texto("", EstiloItens.FusaoSlotNome);
            destaquePrincipal.Add(nomeHeroPrincipal);
            palco.Add(destaquePrincipal);

            var setaHero = new Label("➜");
            setaHero.AddToClassList(EstiloItens.FusaoSeta);
            palco.Add(setaHero);

            var destaqueMaterial = new VisualElement();
            destaqueMaterial.AddToClassList(EstiloItens.FusaoSlotDestaque);
            var rotuloHeroMaterial = Texto("Material (Consumido)", EstiloItens.FusaoSlotRotulo);
            destaqueMaterial.Add(rotuloHeroMaterial);
            var containerHeroMaterial = new VisualElement();
            destaqueMaterial.Add(containerHeroMaterial);
            var nomeHeroMaterial = Texto("", EstiloItens.FusaoSlotNome);
            destaqueMaterial.Add(nomeHeroMaterial);
            palco.Add(destaqueMaterial);

            // --- Seção 1: Peça Principal ---
            lista.Add(Texto("1. Peça Principal a aprimorar:", EstiloItens.TituloSecao));
            var nomesPrincipais = new List<string>();
            foreach (var item in principais)
            {
                nomesPrincipais.Add(Nome(item));
            }
            var principalDropdown = new DropdownField(
                "Peça principal",
                nomesPrincipais,
                indicePrincipal
            );
            principalDropdown.name = "principal-fusao";
            principalDropdown.AddToClassList(EstiloCore.Campo);
            lista.Add(principalDropdown);

            var gradePrincipais = new VisualElement();
            gradePrincipais.AddToClassList(EstiloItens.FusaoGradeCasas);
            lista.Add(gradePrincipais);

            var detalhesPrincipal = Texto("", EstiloItens.FusaoPreviaLinha);
            lista.Add(detalhesPrincipal);

            // --- Seção 2: Material ---
            lista.Add(
                Texto("2. Material compatível (mesmo tipo, na bolsa):", EstiloItens.TituloSecao)
            );
            var materiais = new List<EntradaInventario>();
            var materialDropdown = new DropdownField("Material (será consumido)");
            materialDropdown.name = "material-fusao";
            materialDropdown.AddToClassList(EstiloCore.Campo);
            lista.Add(materialDropdown);

            var gradeMateriais = new VisualElement();
            gradeMateriais.AddToClassList(EstiloItens.FusaoGradeCasas);
            lista.Add(gradeMateriais);

            var detalhesMaterial = Texto("", EstiloItens.FusaoPreviaLinha);
            detalhesMaterial.name = "detalhes-material-fusao";
            lista.Add(detalhesMaterial);

            // --- Seção 3: Atributos ---
            lista.Add(Texto("3. Atributo a fundir:", EstiloItens.TituloSecao));
            var atributos = new List<StatusItem>();
            var atributoDropdown = new DropdownField("Atributo");
            atributoDropdown.name = "atributo-fusao";
            atributoDropdown.AddToClassList(EstiloCore.Campo);
            lista.Add(atributoDropdown);

            var listaChipsAtributos = new VisualElement();
            listaChipsAtributos.AddToClassList(EstiloItens.FusaoAtributosLista);
            lista.Add(listaChipsAtributos);

            // --- Seção 4: Bloco Fixo de Prévia e Ações ---
            var blocoPrevia = new VisualElement();
            blocoPrevia.AddToClassList(EstiloItens.FusaoPreviaBloco);
            lista.Add(blocoPrevia);

            var tituloPrevia = Texto(
                "4. Prévia e Confirmação da Fusão",
                EstiloItens.FusaoPreviaTitulo
            );
            blocoPrevia.Add(tituloPrevia);

            var textoPrevia = Texto("", EstiloItens.FusaoPreviaLinha);
            blocoPrevia.Add(textoPrevia);

            var avisoSemCoroas = Texto("", EstiloItens.FusaoMotivoAlerta);
            blocoPrevia.Add(avisoSemCoroas);

            var avisoSemMateriais = Texto("", EstiloItens.FusaoMotivoAlerta);
            blocoPrevia.Add(avisoSemMateriais);

            var motivoLabel = Texto("", EstiloItens.FusaoMotivo);
            motivoLabel.name = "motivo-fusao";
            blocoPrevia.Add(motivoLabel);

            var acoesLinha = new VisualElement();
            acoesLinha.AddToClassList(EstiloItens.FusaoAcoes);
            blocoPrevia.Add(acoesLinha);

            var consultarBtn = Botao(
                "Ver prévia",
                () =>
                {
                    if (
                        materialDropdown.index < 0
                        || materialDropdown.index >= materiais.Count
                        || atributoDropdown.index < 0
                        || atributoDropdown.index >= atributos.Count
                    )
                    {
                        return;
                    }

                    _enviar(
                        "fusao/previa",
                        new PedidoFusao
                        {
                            PrincipalId = principais[principalDropdown.index].InstanciaId,
                            MaterialId = materiais[materialDropdown.index].InstanciaId,
                            Status = atributos[atributoDropdown.index],
                        }
                    );
                }
            );
            consultarBtn.name = "previa-fusao";
            acoesLinha.Add(consultarBtn);

            var confirmarBtn = Botao(
                "Confirmar fusão e consumir material",
                () =>
                {
                    if (previa == null)
                    {
                        return;
                    }

                    _enviar(
                        "fusao/confirmar",
                        new PedidoFusao
                        {
                            PrincipalId = previa.PrincipalId,
                            MaterialId = previa.MaterialId,
                            Status = previa.Status,
                            Confirmacao = previa.Confirmacao,
                        }
                    );
                }
            );
            confirmarBtn.name = "confirmar-fusao";
            confirmarBtn.AddToClassList(EstiloCore.BotaoPrincipal);
            acoesLinha.Add(confirmarBtn);

            var cancelarBtn = Botao("Cancelar prévia", () => _enviar("fusao/estado", null));
            acoesLinha.Add(cancelarBtn);

            // Helpers de Atualização
            Action atualizarBlocoPrevia = () =>
            {
                var principal = principais[principalDropdown.index];
                var noMaximo = principal.Equipamento.NivelFusao >= ConfigFusao.NivelMaximo;

                if (previa != null)
                {
                    textoPrevia.text = DescreverPrevia(previa);
                    var semCoroas = estado.Coroas < previa.Custo;
                    var semMats = !previa.MateriaisSuficientes;

                    avisoSemCoroas.text = semCoroas
                        ? "Coroas insuficientes: faltam "
                            + (previa.Custo - estado.Coroas)
                            + " coroas para confirmar esta fusão."
                        : "";
                    avisoSemCoroas.style.display = semCoroas
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;

                    avisoSemMateriais.text = semMats
                        ? "Materiais insuficientes: exigido "
                            + DescreverMateriais(previa)
                            + ". Saldo: "
                            + estado.MateriaisDungeon
                            + " "
                            + ConfigFusao.NomeMaterialDungeon
                            + ", "
                            + estado.MateriaisRaid
                            + " "
                            + ConfigFusao.NomeMaterialRaid
                            + "."
                        : "";
                    avisoSemMateriais.style.display = semMats
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;

                    confirmarBtn.SetEnabled(!semCoroas && !semMats);
                    cancelarBtn.style.display = DisplayStyle.Flex;

                    motivoLabel.text = semCoroas
                        ? "Saldo insuficiente: " + estado.Coroas + " / " + previa.Custo + " coroas."
                        : (
                            semMats
                                ? "Materiais insuficientes: " + DescreverMateriais(previa) + "."
                                : ""
                        );
                }
                else
                {
                    textoPrevia.text =
                        "Selecione uma peça principal, um material e um atributo. Em seguida, clique em 'Ver prévia' para calcular o resultado determinístico antes de confirmar.";
                    avisoSemCoroas.style.display = DisplayStyle.None;
                    avisoSemMateriais.style.display = DisplayStyle.None;
                    confirmarBtn.SetEnabled(false);
                    cancelarBtn.style.display = DisplayStyle.None;

                    motivoLabel.text = MotivoSemEscolha(
                        noMaximo,
                        materiais.Count,
                        atributos.Count,
                        principal.Equipamento.NivelFusao
                    );
                }
            };

            Action atualizarPalco = () =>
            {
                var principal = principais[principalDropdown.index];
                containerHeroPrincipal.Clear();
                containerHeroPrincipal.Add(
                    MontarCasaItem(principal, true, null, "slot-hero-principal")
                );
                nomeHeroPrincipal.text =
                    Nome(principal)
                    + "\nNível +"
                    + principal.Equipamento.NivelFusao
                    + " · Tier "
                    + RegrasTier.Letra(RegrasTier.TierDe(principal.Equipamento));

                containerHeroMaterial.Clear();
                if (materialDropdown.index >= 0 && materialDropdown.index < materiais.Count)
                {
                    var mat = materiais[materialDropdown.index];
                    containerHeroMaterial.Add(
                        MontarCasaItem(mat, true, null, "slot-hero-material")
                    );
                    nomeHeroMaterial.text =
                        Nome(mat)
                        + "\nTier "
                        + RegrasTier.Letra(RegrasTier.TierDe(mat.Equipamento));
                }
                else
                {
                    containerHeroMaterial.Add(
                        MontarCasaItem(null, false, null, "slot-hero-material-vazio")
                    );
                    nomeHeroMaterial.text =
                        materiais.Count == 0
                            ? "(Nenhum material na bolsa)"
                            : "(Selecione o material)";
                }
            };

            Action atualizarGradePrincipais = () =>
            {
                gradePrincipais.Clear();
                for (var i = 0; i < principais.Count; i++)
                {
                    var idx = i;
                    var item = principais[i];
                    var selecionado = idx == principalDropdown.index;
                    var slot = MontarCasaItem(
                        item,
                        selecionado,
                        () =>
                        {
                            if (principalDropdown.index != idx)
                            {
                                principalDropdown.index = idx;
                            }
                        },
                        "slot-principal-" + idx
                    );
                    gradePrincipais.Add(slot);
                }
            };

            Action atualizarGradeMateriais = () =>
            {
                gradeMateriais.Clear();
                if (materiais.Count == 0)
                {
                    var vazioMsg = Texto(
                        "Não há material compatível na bolsa: precisa ser da mesma categoria de peça, estar na bolsa e não ser favorito.",
                        EstiloItens.FusaoMotivo
                    );
                    gradeMateriais.Add(vazioMsg);
                    return;
                }

                for (var i = 0; i < materiais.Count; i++)
                {
                    var idx = i;
                    var item = materiais[i];
                    var selecionado = idx == materialDropdown.index;
                    var slot = MontarCasaItem(
                        item,
                        selecionado,
                        () =>
                        {
                            if (materialDropdown.index != idx)
                            {
                                materialDropdown.index = idx;
                            }
                        },
                        "slot-material-" + idx
                    );
                    gradeMateriais.Add(slot);
                }
            };

            Action atualizarChipsAtributos = () =>
            {
                listaChipsAtributos.Clear();
                if (materialDropdown.index >= 0 && materialDropdown.index < materiais.Count)
                {
                    var dadosPrincipal = principais[principalDropdown.index].Equipamento;
                    var dadosMaterial = materiais[materialDropdown.index].Equipamento;

                    for (var i = 0; i < atributos.Count; i++)
                    {
                        var idx = i;
                        var st = atributos[i];
                        var valPrinc = RegrasFusao.Procurar(dadosPrincipal, st).Valor;
                        var valMat = RegrasFusao.Procurar(dadosMaterial, st).Valor;
                        var chip = Botao(
                            TextosEquipamento.NomeStatus(st)
                                + " · "
                                + Numero(valPrinc)
                                + " → material "
                                + Numero(valMat),
                            () =>
                            {
                                if (atributoDropdown.index != idx)
                                {
                                    atributoDropdown.index = idx;
                                }
                            }
                        );
                        chip.name = "chip-atributo-" + (int)st;
                        chip.AddToClassList(EstiloItens.FusaoAtributoChip);
                        if (idx == atributoDropdown.index)
                        {
                            chip.AddToClassList(EstiloItens.FusaoAtributoAtivo);
                        }
                        listaChipsAtributos.Add(chip);
                    }
                }
            };

            Action atualizarAtributos = () =>
            {
                atributos.Clear();
                var opcoes = new List<string>();
                if (materialDropdown.index >= 0 && materialDropdown.index < materiais.Count)
                {
                    var dadosPrincipal = principais[principalDropdown.index].Equipamento;
                    var dadosMaterial = materiais[materialDropdown.index].Equipamento;
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

                atributoDropdown.choices = opcoes;
                atributoDropdown.index = opcoes.Count == 0 ? -1 : 0;
                var dono = principais[principalDropdown.index].Equipamento;
                var noMaximo = dono.NivelFusao >= ConfigFusao.NivelMaximo;
                consultarBtn.SetEnabled(opcoes.Count > 0 && !noMaximo);

                detalhesMaterial.text =
                    materialDropdown.index >= 0 && materialDropdown.index < materiais.Count
                        ? "Material que será consumido:\n"
                            + TextosEquipamento.Descrever(materiais[materialDropdown.index])
                        : "";

                atualizarChipsAtributos();
                atualizarPalco();
                atualizarBlocoPrevia();
            };

            Action atualizarMateriais = () =>
            {
                materiais.Clear();
                var opcoes = new List<string>();
                var dono = principais[principalDropdown.index];
                detalhesPrincipal.text = TextosEquipamento.Descrever(dono);
                foreach (var item in estado.Inventario)
                {
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
                }

                materialDropdown.choices = opcoes;
                materialDropdown.index = opcoes.Count == 0 ? -1 : 0;

                atualizarGradeMateriais();
            };

            atualizarMateriais();
            if (previa != null)
            {
                for (var i = 0; i < materiais.Count; i++)
                {
                    if (materiais[i].InstanciaId == previa.MaterialId)
                    {
                        materialDropdown.index = i;
                    }
                }
            }

            atualizarAtributos();
            if (previa != null)
            {
                for (var i = 0; i < atributos.Count; i++)
                {
                    if (atributos[i] == previa.Status)
                    {
                        atributoDropdown.index = i;
                    }
                }
            }

            atualizarGradePrincipais();
            atualizarPalco();
            atualizarBlocoPrevia();

            principalDropdown.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                atualizarMateriais();
                atualizarAtributos();
                atualizarGradePrincipais();
            });

            materialDropdown.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                atualizarAtributos();
                atualizarGradeMateriais();
            });

            atributoDropdown.RegisterValueChangedCallback(_ =>
            {
                previa = null;
                atualizarChipsAtributos();
                atualizarBlocoPrevia();
            });
        }

        private static VisualElement MontarCasaItem(
            EntradaInventario entrada,
            bool selecionada,
            Action aoClicar,
            string nomeElemento = null
        )
        {
            var slot = new VisualElement();
            if (!string.IsNullOrEmpty(nomeElemento))
            {
                slot.name = nomeElemento;
            }
            slot.AddToClassList(EstiloItens.Slot);
            if (selecionada)
            {
                slot.AddToClassList(EstiloItens.SlotSelecionado);
            }

            var icone = new VisualElement { pickingMode = PickingMode.Ignore };
            icone.AddToClassList(EstiloItens.Icone);
            var sigla = new Label { pickingMode = PickingMode.Ignore };
            sigla.AddToClassList(EstiloCore.Texto);
            sigla.AddToClassList(EstiloItens.Sigla);
            slot.Add(icone);
            slot.Add(sigla);

            if (entrada == null)
            {
                slot.AddToClassList(EstiloItens.SlotVazio);
                return slot;
            }

            var dados = RegrasEquipamentos.DadosDe(entrada);
            var tipo = LogicaInventario.TipoDe(entrada);
            if (tipo != null)
            {
                var sprite = IconesEquipamento.Carregar(tipo.Value);
                if (sprite != null)
                {
                    icone.style.backgroundImage = new StyleBackground(sprite);
                    sigla.text = "";
                }
                else
                {
                    sigla.text = TextosEquipamento.SiglaDoTipo(tipo.Value);
                }
            }
            else
            {
                sigla.text = !string.IsNullOrEmpty(entrada.Nome)
                    ? entrada.Nome.Substring(0, 1)
                    : "?";
            }

            if (dados != null)
            {
                var tier = RegrasTier.TierDe(dados);
                slot.AddToClassList(EstiloItens.Tier(tier));
                var letra = new Label(RegrasTier.Letra(tier)) { pickingMode = PickingMode.Ignore };
                letra.AddToClassList(EstiloCore.Texto);
                letra.AddToClassList(EstiloItens.LetraTier);
                slot.Add(letra);

                if (dados.NivelFusao > 0)
                {
                    var seloNivel = new Label("+" + dados.NivelFusao)
                    {
                        pickingMode = PickingMode.Ignore,
                    };
                    seloNivel.AddToClassList(EstiloItens.SeloNovo);
                    slot.Add(seloNivel);
                }

                if (dados.Favorito)
                {
                    slot.AddToClassList(EstiloItens.SlotFavorito);
                    var seloFav = new Label("★") { pickingMode = PickingMode.Ignore };
                    seloFav.AddToClassList(EstiloItens.SeloFavorito);
                    slot.Add(seloFav);
                }

                slot.tooltip = TextosEquipamento.Descrever(entrada);
            }

            if (aoClicar != null)
            {
                slot.RegisterCallback<PointerDownEvent>(e =>
                {
                    if (e.button == 0)
                    {
                        aoClicar();
                        e.StopPropagation();
                    }
                });
            }

            return slot;
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
            {
                return "Esta peça já está em +" + nivel + " e não pode ser fundida de novo.";
            }

            if (materiaisCompativeis == 0)
            {
                return "Não há material compatível na bolsa: precisa ser da mesma categoria de peça, estar na bolsa e não ser favorito.";
            }

            if (atributosEmComum == 0)
            {
                return "Essa peça e o material não têm nenhum atributo em comum para fundir.";
            }

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
            {
                partes.Add(previa.MateriaisDungeon + " " + ConfigFusao.NomeMaterialDungeon);
            }

            if (previa.MateriaisRaid > 0)
            {
                partes.Add(previa.MateriaisRaid + " " + ConfigFusao.NomeMaterialRaid);
            }

            return partes.Count == 0 ? "nenhum" : string.Join(" e ", partes);
        }

        private static string Numero(double valor) =>
            ((int)Math.Round(valor, MidpointRounding.AwayFromZero)).ToString(
                CultureInfo.CurrentCulture
            );

        private static string Nome(EntradaInventario item) =>
            item.Equipamento.Nome + " (#" + item.InstanciaId + ")";

        private static Label Texto(string texto, string classeEstilo = null)
        {
            var label = new Label(texto);
            if (!string.IsNullOrEmpty(classeEstilo))
            {
                label.AddToClassList(classeEstilo);
            }
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
