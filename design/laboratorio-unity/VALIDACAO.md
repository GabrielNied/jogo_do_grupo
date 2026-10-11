# Validação do laboratório

**Atualização:** o usuário visualizou o laboratório e adotou o modelo como padrão inicial em [Lumora UI v1](../interface/GUIA-DE-ESTILO.md). Em 2026-10-11, Unity 6000.6.5f1 confirmou compilação, `.meta` das quatro fontes estáticas Lumora na `main` e testes EditMode de `LaboratorioVisualTestes` e `EstiloVisualTestes` (466 no total). A matriz visual com capturas em várias resoluções continua checklist humana no editor.

## Consolidação Lumora UI v1 — tarefa #372

- Cinco testes Node passaram: configuração inválida, propagação de cor, sincronização de C#/USS/fontes, cálculo de contraste e rejeição de fonte inválida. Referências a tokens existentes e ausência de cores duplicadas nos estilos compartilhados/laboratório também são verificadas.
- CSharpier passou; metadados de scripts, pastas, estilos e textos foram gerados pelo comando oficial.
- Novos testes EditMode cobrem limites responsivos, larguras inválidas, leitura aumentada, recursos ausentes e aplicação idempotente do estilo. **Executados na engine em 2026-10-11** (`npm run unity:compilar`, `npm run unity:testes`).
- Quatro `.meta` das fontes em `Core/UI/Visual/Resources/Lumora/Fontes/` estão na `main` (PR #399). Capturas dos layouts compacto/estreito: exportar ficha pelo menu **Lumora > Design > Laboratório visual** conforme a matriz abaixo.
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
- Compilação Unity: **0 erros** (1 aviso CS0618 em `TelaLaboratorio`).
- EditMode: **466 passaram** (inclui laboratório e biblioteca visual). Renderização pixel a pixel: matriz humana abaixo.
- Formatação CSharpier e presença dos metadados de texto passaram. O verificador Git de arquivos Unity passou na preparação desta entrega. O `diff --check` aponta espaços finais nos campos vazios dos `.meta` emitidos pelo gerador oficial; esses metadados foram preservados sem edição manual. Os demais arquivos passaram na verificação de espaços.
- O laboratório roda em EditorWindow. Depois da aprovação, validar também em UIDocument runtime/build antes de incorporar componentes ao jogo.
- Capturas anexadas à ficha de revisão: **opcionais na issue #366**; o modelo já foi adotado como padrão inicial. Novos ajustes visuais registram ficha em `design/laboratorio-unity/revisoes/`.

Não ampliar o trabalho para outras telas para corrigir um problema do laboratório. Ajustar aqui, repetir a avaliação afetada e registrar o resultado.
