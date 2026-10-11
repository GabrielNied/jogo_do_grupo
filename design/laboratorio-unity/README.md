# Laboratório visual no Unity — proposta para validação

**Atualização:** o usuário visualizou o laboratório e adotou sua direção como padrão inicial. O [guia Lumora UI v1](../interface/GUIA-DE-ESTILO.md) agora orienta as novas telas; a configuração central e a adaptação responsiva novas ainda precisam de validação na engine. As observações de proposta abaixo registram a primeira entrega, sem revogar essa adoção.

Área aditiva ligada à [tarefa #355](https://github.com/GabrielNied/jogo_do_grupo/issues/355). Implementação em [LaboratorioVisual](../../client/Assets/_Game/Features/LaboratorioVisual/Editor/). **Modelo adotado como padrão inicial; a nova centralização e responsividade aguardam validação na engine.**

## Abrir

1. Abrir o projeto `client/` desta branch no Unity **6000.6.5f1**.
2. Aguardar importação e compilação. Menu **Lumora → Design → Laboratório visual**.
3. A janela prepara cópias locais dos recursos de `design/`, importadas pelo próprio Unity. Fontes e imagens permanecem com suas matrizes nesta pasta; não há cópias binárias versionadas na engine.
4. Escolher um dos cinco exemplos no menu lateral. Não é necessário iniciar Play Mode, conectar servidor ou carregar uma cena.
5. Escolher área lógica de 1280 × 720, 1920 × 1080 ou 2560 × 1440. O zoom inicial de 50% permite uma visão geral; **avaliar legibilidade também em 100%**. O zoom não substitui escala de acessibilidade nem teste em monitor real.
6. Ativar **Comparar com arte** para ver a prancha correspondente ao lado. Desativar para avaliar a composição na área inteira.

A janela usa UI Toolkit real, mas roda no editor. Renderização e entrada em build/runtime ainda precisam de uma etapa futura de integração; não considerar este laboratório um menu entregue no jogo. Nenhuma cena, configuração, inicializador, tela existente, regra ou serviço foi alterado. O assembly só existe no editor, sem dependências de outras funcionalidades.

## Exemplos

| Menu | O que experimentar |
|---|---|
| Componentes | Paleta, títulos, acentos, botões, hover/foco/desabilitado, campo obrigatório, toggle, slider, barra e modal |
| Grupo e chat | Painel com folhas e calçada, cartões, convite, mensagens locais de até 240 caracteres; histórico demonstrativo limitado a 30 linhas |
| Inventário | Busca sem diferenciar maiúsculas/minúsculas, itens longos, estado vazio, inspeção em modal |
| Combate | Painel sem ornamentação pesada, barras, simulação de dano/restauração, atalhos e aviso |
| Cidade | Mural em pergaminho, contribuição fictícia, painel ornamentado e confirmação |

Nomes, quantidades, barras e itens são dados de demonstração, sem classes ou balanceamento. Retratos usam iniciais como placeholders explícitos; não foram recortados personagens das pranchas. A moldura existente é aplicada com fatias para preservar cantos, ainda sujeita a revisão de halos, escala das folhas e deformação. Não é promessa de reprodução idêntica às imagens geradas.

## Recursos e rastreabilidade

- Logo e imagens: [marca](../marca/) e [elementos](../interface/elementos/), sem alterações às matrizes aprovadas.
- Tipografia: [padrão aprovado](../tipografia/PADRAO-TIPOGRAFICO.md), que continua a fonte única da escolha.
- [Fontes estáticas](fontes/): instâncias derivadas com **FontTools 4.60.1**, nomes internos próprios `LumoraNunito` e `LumoraSource`; originais preservados, licenças OFL incluídas. Nunito 800 (opsz 28, demais eixos no padrão), Source 500/600/700. Fonte 600 está preparada para números; a primeira versão usa 500 nas barras e não declara números tabulares validados.
- [Preparação reproduzível](ferramentas/preparar-fontes.py): requer Python e `fonttools==4.60.1`; executar `python design/laboratorio-unity/ferramentas/preparar-fontes.py`. Confere ausência de eixo variável e presença de acentos/números.
- O importador local usa `Editor/RecursosGerados/`, ignorado pelo Git. Apenas estes recursos do laboratório recebem configuração de textura sem mipmaps/compressão e com transparência. Não modifica importadores existentes.
- A ficha de revisão inclui SHA256 dos recursos efetivamente usados. Exportações novas ficam em `revisoes/`, sem sobrescrever fichas anteriores.

## Validação

Seguir [VALIDACAO.md](VALIDACAO.md). A janela exporta uma ficha Markdown com contexto, hashes e alertas estruturais. Isso não substitui aprovação humana. Não há captura automática de tela nesta versão: anexar capturas reais da janela à ficha antes de aprovar.

O [guia Lumora UI v1](../interface/GUIA-DE-ESTILO.md) agora orienta os agentes. A [BASE-PROVISORIA.md](BASE-PROVISORIA.md) foi preservada como histórico da etapa anterior.

## Estado desta entrega

Preparação das fontes e validações de arquivo podem rodar fora da engine. Compilação, EditMode, importadores, cortes de texto e fidelidade visual precisam de Unity; consultar [VALIDACAO.md](VALIDACAO.md) para o resultado real desta sessão. Não confundir verificação pulada com aprovação.
