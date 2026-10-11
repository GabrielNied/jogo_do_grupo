# Validação do laboratório

**Atualização:** o usuário visualizou o laboratório e adotou o modelo como padrão inicial em [Lumora UI v1](../interface/GUIA-DE-ESTILO.md). Os resultados abaixo são da entrega inicial; a aprovação qualitativa não prova cobertura de todas as resoluções. A centralização de tokens e os novos layouts compactos ainda precisam de validação no Unity, incluindo importação dos `.meta` das fontes compartilhadas.

## Consolidação Lumora UI v1 — tarefa #372

- Cinco testes Node passaram: configuração inválida, propagação de cor, sincronização de C#/USS/fontes, cálculo de contraste e rejeição de fonte inválida. Referências a tokens existentes e ausência de cores duplicadas nos estilos compartilhados/laboratório também são verificadas.
- CSharpier passou; metadados de scripts, pastas, estilos e textos foram gerados pelo comando oficial.
- Novos testes EditMode cobrem limites responsivos, larguras inválidas, leitura aumentada, recursos ausentes e aplicação idempotente do estilo. **Não executados nesta revisão: Unity indisponível no ambiente, conforme diagnóstico anterior.**
- Permanecem pendentes: compilação/importação Unity, quatro `.meta` das fontes em `Core/UI/Visual/Resources/Lumora/Fontes/`, testes EditMode e capturas dos layouts compacto/estreito. Os `.meta` binários devem ser gerados pelo Unity, nunca escritos à mão.
- Nenhuma tela antiga foi migrada. A nova biblioteca compartilhada requer review do Core.

## Automatizada fora do Unity

Na raiz do repositório:

```text
node --test design/laboratorio-unity/ferramentas/validacao.test.mjs
node design/laboratorio-unity/ferramentas/validar.mjs
dotnet csharpier check client/Assets/_Game/Features/LaboratorioVisual
npm run unity:meta -- --conferir
```

O validador confere recursos não substituídos por ponteiros LFS, pesos estáticos, ausência de eixos variáveis, integridade das matrizes aprovadas, escopo editor, isolamento de dependências e pares de contraste sólidos. Gradientes, textura, translucidez, foco e texto sobre as barras ainda exigem avaliação da renderização real. Um contraste calculado de duas cores sólidas não aprova automaticamente a tela.

## Na engine

```text
npm run unity:compilar
npm run unity:testes
```

Os testes EditMode de `LaboratorioVisualTestes` verificam navegação, rejeição de índices inválidos, fechamento de modal ao navegar, limites e persistência do tamanho de texto, busca/estado vazio, comparação e independência de instâncias. O relatório estrutural recusa declarar sucesso sem painel renderizado. Eles não provam fidelidade às imagens.

## Matriz de avaliação visual

Abrir cada um dos cinco exemplos em 1280 × 720, 1920 × 1080 e 2560 × 1440; conferir texto em 16, 20 e 24 px. Usar zoom 100% na avaliação de legibilidade; 50% serve para composição geral. Comparar com a arte e depois desligar a comparação para conferir a área completa.

- Tipografia: família correta, peso, acentos, números e nomes longos, sem fallback silencioso.
- Hierarquia: título/ação principal perceptíveis, logo preservada, essência atrás do O original.
- Ornamentação: cantos sem deformação excessiva, calçada proporcional, sem halos de recorte sobre azul; folhas não cobrem texto nem capturam clique.
- Conteúdo: sem corte, sobreposição ou rolagem inacessível. Aumentar leitura não pode reduzir texto automaticamente.
- Estados: hover, pressionado, foco visível com Tab, desabilitado sem ação, erro no campo vazio e recuperação após corrigir.
- Fluxos: abrir/fechar convite; Esc fecha modal; enviar chat vazio e preenchido; procurar item existente, inexistente e limpar filtro; simular/restaurar vida; trocar menu repetidamente.
- Contraste: conferir textos pequenos, placeholders, barra preenchida, pergaminho texturizado e estados secundários sobre o fundo real.

**Exportar ficha de revisão**, anexar capturas reais e registrar cada problema com exemplo, área, tamanho de texto e estado. Marcar `aprovado`, `ajustar` ou `pendente` por componente. A ficha nasce pendente; nenhum script deve marcá-la aprovada.

## Limites e resultado da sessão

- Fontes estáticas geradas e verificadas fora da engine.
- Testes Node: **2 passaram**, cobrindo extremos do contraste, peso incorreto de fonte, ponteiro LFS e arquivo truncado. Validador de recursos passou; pares sólidos medidos: marfim/petróleo 9,58:1, petróleo/dourado 6,99:1, texto secundário/petróleo 7,88:1.
- Compilação Unity: **pulada pelo comando oficial — Unity não instalado neste ambiente**.
- EditMode, importação e renderização: **pendentes de Unity**. Código e estilos ainda não foram confirmados pelo compilador/importador real.
- Formatação CSharpier e presença dos metadados de texto passaram. O verificador Git de arquivos Unity passou na preparação desta entrega. O `diff --check` aponta espaços finais nos campos vazios dos `.meta` emitidos pelo gerador oficial; esses metadados foram preservados sem edição manual. Os demais arquivos passaram na verificação de espaços.
- O laboratório roda em EditorWindow. Depois da aprovação, validar também em UIDocument runtime/build antes de incorporar componentes ao jogo.
- Capturas e aprovação visual do usuário: **pendentes**. Nenhum guia definitivo foi declarado aprovado.

Não ampliar o trabalho para outras telas para corrigir um problema do laboratório. Ajustar aqui, repetir a avaliação afetada e registrar o resultado.
