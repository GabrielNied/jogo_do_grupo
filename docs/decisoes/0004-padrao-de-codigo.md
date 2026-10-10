# 0004. Padrão de código com CSharpier

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Várias pessoas vão escrever C# em client, server e shared, cada uma com seu editor. Sem um padrão automático, os PRs viram discussão de estilo e diffs cheios de mudanças de formatação, o que também gera conflitos de merge.

## Decisão

- Usar o **CSharpier** como formatador único de C#, sem opções de estilo para configurar.
- Instalar como **dotnet tool local** (`dotnet-tools.json` na raiz), com versão fixada, para todo mundo e o CI usarem a mesma versão.
- O CI (`.github/workflows/format.yml`) roda `dotnet csharpier check .` em todo PR e push na `main`.
- Cada pessoa instala o plugin do editor com "formatar ao salvar".
- O `.editorconfig` define charset, fim de linha, indentação e regras de nomes.
- Começar pela versão mínima: sem analisadores obrigatórios e sem hook de pre-commit por enquanto.

## Consequências

- Estilo deixa de ser assunto de review.
- Diffs ficam menores e os conflitos por formatação somem.
- Quem esquecer de formatar tem o PR barrado pelo CI até rodar `dotnet csharpier format .`.
- Atualizar a versão do CSharpier pode reformatar arquivos; isso entra num PR separado.
- Regras de nomes e qualidade (analisadores) ainda não são obrigatórias; podem virar um ADR futuro.
