# Lumora UI v1 — padrão das interfaces

**Estado:** adotado pelo usuário como padrão inicial para novas telas, após visualizar o laboratório da PR #365. A aprovação é da direção visual e do modelo de interface. Ajustes posteriores de cores, medidas e acabamento são permitidos por revisão centralizada; capturas e cobertura completa de resoluções ainda precisam ser registradas. Isso não declara todas as telas futuras validadas.

## Fonte única

- Valores editáveis: [config-visual.json](config-visual.json), agrupados por cores, tipografia, espaçamento, dimensões, bordas, ornamentos, responsividade e fontes.
- Implementação compartilhada: [Core/UI/Visual](../../client/Assets/_Game/Core/UI/Visual/), assembly `JogoDoGrupo.Client.Visual`.
- Referência funcional: [laboratório](../laboratorio-unity/README.md). Retratos com iniciais são placeholders, não padrão de arte final.
- Logo e símbolo: [matrizes aprovadas](../marca/). Tipografia e licenças: [padrão aprovado](../tipografia/PADRAO-TIPOGRAFICO.md).

Não copiar valores de cor/tamanho para cada tela. Editar o JSON e executar, da raiz:

```text
node design/interface/ferramentas/gerar-config-visual.mjs
node design/interface/ferramentas/gerar-config-visual.mjs --conferir
node --test design/interface/ferramentas/config-visual.test.mjs
```

O gerador produz `ConfigVisual.cs`, `TokensVisuais.uss` (incluindo os papéis tipográficos) e cópias idênticas das fontes estáticas aprovadas em `Resources/Lumora/Fontes`, com licenças. Copia também a logo, o fundo petróleo e a moldura social para `Resources/Lumora/Arte`. Não editar saídas à mão. CSharpier formata o C# gerado. Fontes e imagens binárias são importadas pelo Unity; seus `.meta` nunca devem ser escritos à mão. Mudar a configuração altera somente telas que já a consomem; telas antigas não são migradas automaticamente.

## Linguagens visuais

| Padrão | Uso | Aparência |
|---|---|---|
| Social | Grupo, amigos, guilda, cidade | Petróleo, molduras douradas, folhas nos cantos e calçada na base; reservar espaço real para a decoração |
| Combate | HUD, alvo, recursos, avisos urgentes | Mesma paleta e tipografia; decoração reduzida, leitura rápida e conteúdo prioritário |
| Pergaminho | Mural, informação narrativa e contribuições | Fundo claro texturizado e texto petróleo; conferir contraste sobre a textura real |

Ações principais usam dourado com texto petróleo; ações secundárias usam fundo petróleo e texto marfim; turquesa indica essência, foco e seleção. Verde é apoio natural. Erro tem mensagem textual além de cor. A textura não deve comprometer leitura. Preservar sombras da pedra, lettering da logo, O rachado e essência atrás do O; não redesenhar a marca para cada tela.

## Componentes e uso pelos agentes

Cada feature referencia apenas `JogoDoGrupo.Client.Visual` para o visual compartilhado, sem referência a `LaboratorioVisual` nem a outra feature. Montar a interface em C#, aplicar `EstiloVisual.Aplicar(raiz)` uma vez, então usar `ComponentesVisuais.Texto`, `Botao` e `Painel`. Estes ajudam a manter fontes reais e classes comuns. Aplicar o USS da própria tela após o estilo compartilhado; nele, usar `var(--lumora-...)` e classes locais para o layout. Não redefinir tokens para mudar silenciosamente a marca.

Exemplo de composição:

```csharp
var raiz = new VisualElement();
EstiloVisual.Aplicar(raiz);
var painel = ComponentesVisuais.Painel();
painel.Add(ComponentesVisuais.Texto("Sua expedição", titulo: true));
painel.Add(ComponentesVisuais.Texto("Prepare a aventura com seus amigos."));
painel.Add(ComponentesVisuais.Botao("Convidar", AbrirConvite, principal: true));
raiz.Add(painel);
```

O helper `Painel()` entrega a base neutra. `PainelRolavel(social: true)` cria uma superfície com rolagem vertical e a moldura de folhas/calçada aprovada, aplicada como fundo sem elemento adicional que intercepte o ponteiro. Os tokens de ornamentos reservam espaço ao redor do conteúdo. `Marca()` usa a imagem aprovada, sem reescrever o lettering. Pergaminho continua um recurso separado de [elementos](elementos/). Não preencher a interface com uma prancha raster nem copiar a implementação de demonstração como regra de gameplay.

As telas existentes usam a integração `EstiloCore.Aplicar(raizDocumento)`, que instala o mesmo padrão nos documentos independentes. As classes antigas `titulo`, `subtitulo`, `texto`, `painel`, `botao` e `botao-principal` são aliases do estilo compartilhado, sem cópia dos valores. Novos formulários podem usar `campo` e `campo-erro`, acompanhados de mensagem textual; os estados dos campos estão no USS compartilhado. A tipografia por classe também vale para elementos adicionados depois de respostas do servidor. Para desenhos com `Painter2D`, usar `EstiloVisual.Cor(ConfigVisual.Cores.<Papel>)`.

Dentro do fluxo do Core, `EstiloCore.CriarPainel()` cria o painel rolável e mantém as classes de integração. Uma `JanelaDoJogo` pode sobrescrever `MolduraSocial` para solicitar a moldura decorada. O registro das janelas, seus atalhos e suas ações continuam sob responsabilidade dos donos atuais. Ver o [registro da adaptação das interfaces existentes](ADAPTACAO-INTERFACES.md).

Novos tipos de controle precisam incluir normal, hover, pressionado, foco, desabilitado e erro quando aplicável, usando os tokens do respectivo estado. Slots, tooltips e barras ainda não têm factories compartilhadas; usar os exemplos do laboratório e promover um componente reutilizável quando necessário, com testes. Não declarar um componente disponível se só existir na prancha.

## Responsividade e legibilidade obrigatórias

Criar o `PanelSettings` de qualquer documento do jogo com `EstiloCore.CriarConfiguracaoDoPainel(nome, ordem)`: escala pela resolução (referência 1920 × 1080, seguindo a altura) e tema num ponto só ([ADR 0035](../../docs/decisoes/0035-fonte-sdf-e-escala-pela-resolucao.md)). Usar a largura **resolvida da raiz da tela**, não só `Screen.width`. `EstiloVisual` acompanha `GeometryChangedEvent` e aplica `lumora-compacto` e `lumora-estreito` pelos limiares da configuração. O USS específico deve reagir a essas classes com empilhamento, quebra e rolagem. Os componentes compartilhados já permitem quebra de texto e linhas; cada composição ainda precisa de teste visual. USS não recebe automaticamente as media queries de um site.

- Leitura parte do token `Tipografia.Leitura` (atualmente 16 px); botões partem de `Controle` (18 px). Títulos usam Nunito Sans 800 e leitura/controles Source Sans 3 nos pesos aprovados. Cada papel usa o Font Asset SDF da fonte (`<fonte>-SDF.asset`), gerado pelo Unity com `GeradorDeFontes`; o USS nunca aponta para o `.ttf`. Evitar fallback e negrito sintético.
- Prever aumento de texto até `LeituraMaxima`; não reduzir fonte para fazer caber. Conteúdo longo deve quebrar ou rolar.
- Ligar o controle de leitura a `EstiloVisual.DefinirTamanhoTexto(raiz, tamanho)` e reaplicar após reconstruir conteúdo dinâmico. O helper preserva a hierarquia dos títulos e não deixa subtítulos menores que o corpo aumentado.
- Usar `min-width: 0`, flex grow/shrink conforme o papel, limites em vez de larguras rígidas no conteúdo e scroll em listas longas. Dimensões fixas são aceitáveis em ícones, retratos e alvos de controle, não como solução para a página inteira.
- Foco visível, navegação por teclado, Esc em modais, alvo mínimo pelo token e ícones acompanhados de tooltip/nome. Modais precisam evitar foco nos controles do fundo e restaurar foco ao fechar.
- Conferir contraste sobre o fundo final: referência de leitura normal 4,5:1 e texto grande 3:1; transparência, textura e estados exigem conferência real, não apenas cálculo das cores sólidas.
- Testar 1280 × 720, 1920 × 1080 e 2560 × 1440; adicionar 960 × 720 para layout compacto e 640 × 720 para raiz estreita/painel. Testar texto base, 20 e máximo configurado, nomes longos, acentos, listas vazias e erros. Zoom do editor a 50% não comprova leitura.

## Validação e evolução

Executar a verificação de configuração para impedir divergência entre JSON, C# e USS; conferir recursos e fontes com os validadores existentes. Para alterações C#, seguir os comandos proporcionais de [AGENTS.md](../../AGENTS.md), incluindo testes EditMode e compilação quando houver Unity.

Para cada nova tela registrar: padrão usado, resolução/área lógica, tamanho de texto, estados testados, capturas e limitações. Usar a [ficha de revisão](../laboratorio-unity/VALIDACAO.md). Aprovação do padrão v1 não substitui essa verificação por tela. Sem Unity, registrar a pendência e nunca afirmar que houve renderização.

Exceções visuais devem ser justificadas na tarefa e revisadas; ajustes globais passam pela configuração e pelo laboratório antes de propagar. Não migrar telas fora do escopo da tarefa. Próximas revisões preservam o histórico e atualizam a versão da configuração quando houver mudança de esquema.
