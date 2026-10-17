# Character Playground v1

Ferramenta de **Editor** para inspecionar prefabs de personagens. Menu **Jogo do Grupo → Personagem → Character Playground → Abrir ferramenta**, depois **Abrir cena**. Não é uma entrada do jogo nem exige servidor ou Play Mode.

## Operação

- Selecione **Human Male**, use **Spawn** para substituir a instância atual, ou **Reset** para recriá-la no Spawn Point e restaurar o enquadramento. As operações suportam Undo e ficam desabilitadas durante Play Mode ou em outra cena.
- Frente, costas, laterais, corpo inteiro e aproximação controlam a câmera da cena e enquadram a Scene View aberta. A navegação livre é a do próprio Unity. Salve a cena para manter câmera e instância.
- O diagnóstico mostra personagem, Animator, validade do Avatar, Humanoid, altura real da malha, bones usados pela skin e os quatro sockets. A altura usa BakeMesh, evitando o bounds conservador da skin.
- Com a ferramenta aberta e o personagem selecionado, a Scene View mostra os sockets e as referências métricas. Grid de 1 m, régua de 2 m, barra de 1 m e quadrado **Target Test Area** de 2 × 2 m são somente referências visuais. O chão tem 12 × 12 m; não há colliders ou dummy funcional.

## Organização e expansão

Conteúdo gerado em `client/Assets/_Game/Features/Personagem/Dev/CharacterPlayground/`: `CharacterPlayground.unity`, `Personagens.asset` e três materiais simples do ambiente. Cena fora dos Build Settings e sem scripts runtime.

Código em `Personagem/Editor/Playground/`, assembly exclusivo de Editor:

- `CatalogoPlayground`: lista serializada de nome + prefab.
- `CharacterPlayground`: geração inicial, operação restrita à cena, medição e enquadramento.
- `JanelaCharacterPlayground`: UI Toolkit/Lumora e gizmos da Scene View.
- `ValidacaoCharacterPlayground`: geração e verificações reproduzíveis, com capturas.

Para adicionar um personagem, **Editar catálogo**, adicione uma entrada no Inspector com nome e prefab, salve e reabra a ferramenta para atualizar a lista. Não precisa alterar código, sistema de raças ou recriar a cena. O prefab deve ter origem nos pés, escala métrica e orientação +Z; o diagnóstico aceita rigs não Humanoid e mostra essa condição. O Reset retorna o prefab selecionado ao estado original, descartando alterações locais da instância.

Novas seções podem ser acrescentadas à janela conforme casos reais. A v1 não cria módulos vazios, controller de personagem nem contrato paralelo de gameplay. Decisão: [ADR 0046](../decisoes/0046-character-playground-no-editor.md).

## Auditoria e reutilização

| Área existente | Resultado para esta fundação |
|---|---|
| `_Game/Core` + `Features/<X>`, namespaces e asmdefs por funcionalidade | Conteúdo pertence a Personagem; subassembly Editor sem referência a outra feature. Sem tocar Core, Packages ou ProjectSettings. |
| Desenvolvimento: comandos locais, report F9/F10 e captura de HUD (`CapturaDaTela`) | Ferramentas ligadas ao jogo/sessão; não constituem sandbox de personagem. Reaproveitado o padrão de validação via `unity:executar` e RenderTexture, sem registrar painel no jogo. |
| LaboratorioVisual: EditorWindow/UI Toolkit; ferramentas Mundo/Mobs/Personagem via menu | Mesmo padrão de Editor; identidade reutilizada diretamente de `EstiloVisual`/`ComponentesVisuais`, sem depender da feature LaboratorioVisual. |
| Cenas: SampleScene e HumanMaleHumanoid; geradores de raças/prefabs Resources | Não havia Playground equivalente. Preservadas as cenas existentes; nova área `Personagem/Dev`, seguindo a propriedade da funcionalidade. |
| `DadosDasRacas`, `AplicadorAparencia`, `GeradorJogadoresDasRacas`, prefabs Jogador_* | São criação/visual do jogador real. Não associar artificialmente este modelo a uma raça nem substituir os prefabs atuais. Catálogo contém referências de arte, não regras de raça. |
| `MovimentoTerceiraPessoa`, `AcoesJogador`, Input System, `CameraTerceiraPessoa`/`OrbitaCamera` | Movimento/input/câmera de sessão não são necessários; câmera oculta modelo próximo e configuração depende da sessão. Usados Camera e Scene View do Editor, sem duplicar movimentação ou modificar APIs. |
| Animator dos jogadores, Combate (`AnimacaoDeOutros`, animadores de ataque/esquiva), Skills (`ControleSkills`) | Dependem de parâmetros/eventos do jogo e servidor; fora da v1. Reutilizado apenas Animator/Avatar do prefab validado. |
| Itens: `ControleDoVisualEquipado`, `ApresentadorDoVisual`, `ModelosDeArma`; atributos de Personagem | Sistemas reais preservados para integração futura; diagnóstico apenas encontra encaixes pelo nome, sem equipar ou simular atributos. |
| Testes existentes: Core, Personagem, Combate, Itens, Skills, Desenvolvimento | Nenhum teste de servidor necessário para ferramenta sem regras de jogo. Validação de Editor exercita o ciclo de vida real de prefabs/cena; compilação inclui assemblies de testes. |

## Dependências e limites

Reutiliza `Art/HumanMaleUnityTest/HumanMaleHumanoid.prefab`, o FBX, Avatar e material URP da [TASK 03](human-male-unity03.md), sem alterá-los. O código referencia somente UnityEditor/UnityEngine, Visual e APIs públicas de Core para capturar o painel; nenhuma alteração nesses assemblies. **Old Man Idle/ValidacaoLocal não é dependência** da cena ou catálogo: a versão publicada funciona em T-pose sem ela.

Sem animação, gameplay, rede, equipamentos, movimentação, face ou otimização nesta etapa. Gizmos aparecem somente na Scene View com a janela aberta e seleção no personagem; não são objetos exportados. O catálogo é fixo no caminho da ferramenta, mas suas entradas são editáveis. A captura automatizada do painel usa a mesma árvore visual num UIDocument temporário; não representa a moldura nativa do Editor.

A janela usa cópias temporárias das fontes SDF compartilhadas, incluindo material e atlas: salvar a cena não deve gravar o cache de glifos em Core. A validação confere os hashes dessas fontes após salvar os assets; não versionar caches produzidos por uma inspeção.

## Verificação reproduzível

Com Editor fechado, na raiz do worktree:

```text
npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.Playground.ValidacaoCharacterPlayground.Executar
```

`CHARACTER_PLAYGROUND_OUTPUT` escolhe a pasta dos PNGs e `validacao-playground.json`; padrão temporário do sistema, fora do git. O comando falha se duplicar instância, Reset perder posição/rotação/escala, Avatar/sockets/altura falharem, dependências locais entrarem ou assets originais mudarem. Também reabre a cena, confere exclusão do build e protege contra Spawn em cena alheia.

Validação inicial: 1,86416054 m, base 0 m, 54 bones usados pela skin, Humanoid válido e quatro encaixes encontrados; nenhuma advertência durante a operação. Logs de inicialização podem listar assemblies vazios e avisos internos do Unity já existentes. Resultados detalhados e capturas ficam no relatório da entrega.
