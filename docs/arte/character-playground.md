# Character Playground v2

Ferramenta de **Editor** para inspecionar e controlar prefabs de personagens sem servidor. Menu **Jogo do Grupo → Personagem → Character Playground → Abrir ferramenta**, depois **Abrir cena**. Inspeção em Edit Mode; movimento e câmera reais em Play Mode, sem iniciar o fluxo do jogo.

## Operação

- Selecione **Human Male**, use **Spawn** para substituir a instância atual, ou **Reset** para recriá-la no Spawn Point e restaurar o estado inicial. Funcionam em Edit e Play Mode; Undo somente no Edit. Desabilitados em outras cenas.
- Frente, costas, laterais, corpo inteiro e aproximação controlam a câmera da cena e enquadram a Scene View aberta. A navegação livre é a do próprio Unity. Salve a cena para manter câmera e instância.
- O diagnóstico mostra personagem, Animator, validade do Avatar, Humanoid, altura real da malha, bones usados pela skin e os quatro sockets. A altura usa BakeMesh, evitando o bounds conservador da skin.
- Com a ferramenta aberta e o personagem selecionado em Edit Mode, a Scene View mostra os sockets e as referências métricas. Grid de 1 m, régua de 2 m, barra de 1 m e quadrado **Target Test Area** de 2 × 2 m são somente referências visuais. O chão tem 12 × 12 m e BoxCollider local; não há dummy funcional.

## Movimento e câmera em Play Mode

Entre em Play e clique no Game View. `AcoesJogador` respeita `TeclasDoJogo` e as preferências locais: padrões WASD/setas, Ctrl esquerdo para andar devagar, Espaço para pular, mouse para órbita e roda para zoom. Esc libera o cursor e suspende o controle; clique no Game View para retomar. Focar a ferramenta também suspende o controle, permitindo operar Spawn/Reset.

O personagem usa **ControleTerceiraPessoa** + **MovimentoTerceiraPessoa** sem cópia de fórmulas: correr 5,5 m/s por padrão, andar 2 m/s, aceleração 20 m/s², frenagem 15 m/s², rotação até 720°/s, gravidade 20 m/s² e pulo de 1,25 m. Grounded e colisão vêm do CharacterController; dimensões da cápsula derivam da altura da malha, com degrau de 0,3 m e limite de rampa de 45°. Valores e algoritmos têm fonte em Core, não na ferramenta.

**CameraTerceiraPessoa** + **OrbitaCamera** fazem follow, órbita, zoom entre 2 e 8 m (inicial 4,5 m), suavização e colisão. A câmera de inspeção fica desabilitada somente durante esse contexto. Ao sair do Play, a cena retorna ao estado de inspeção sem componentes runtime persistidos. Não há sprint separado: o padrão do jogo é correr, com Ctrl para caminhar. Movimento para trás/lados gira o personagem na direção do deslocamento; não introduz strafe com orientação travada.

`RuntimeCharacterPlayground` é o adaptador exclusivo do Editor: valida e chama por reflexão as duas assinaturas internas de configuração e cria/descarta as ações reais. Nenhuma reflexão participa dos cálculos por frame. Não instancia `SessaoNoMundo`, que depende de conexão, menus e janelas. Se a API interna mudar, o adaptador recusa a configuração com diagnóstico. Decisão: [ADR 0047](../decisoes/0047-contexto-local-do-playground.md).

A seção Runtime mostra personagem controlado, grounded, velocidade desejada/real, direção real, Animator/estado, câmera e input. Sem controller de animação mostra T-pose explicitamente. Reset descarta o contexto anterior, recria o prefab no mesmo Spawn Point e reconecta os componentes reais.

## Organização e expansão

Conteúdo gerado em `client/Assets/_Game/Features/Personagem/Dev/CharacterPlayground/`: `CharacterPlayground.unity`, `Personagens.asset` e três materiais simples do ambiente. Cena fora dos Build Settings e sem scripts runtime.

Código em `Personagem/Editor/Playground/`, assembly exclusivo de Editor:

- `CatalogoPlayground`: lista serializada de nome + prefab.
- `CharacterPlayground`: geração inicial, operação restrita à cena, medição e enquadramento.
- `JanelaCharacterPlayground`: UI Toolkit/Lumora e gizmos da Scene View.
- `ValidacaoCharacterPlayground`: geração e verificações reproduzíveis, com capturas.
- `RuntimeCharacterPlayground`: composição local dos componentes reais e gestão de input/cursor/câmera durante Play.
- `ValidacaoRuntimePlayground`: teste integrado com dispositivos virtuais do Input System e frames reais de Play.

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
| `MovimentoTerceiraPessoa`, `ControleTerceiraPessoa`, `AcoesJogador`, Input System, `CameraTerceiraPessoa`/`OrbitaCamera` | Reutilizados em Play pela composição local de Editor, sem a sessão. A câmera real mantém inclusive a regra de ocultar o modelo quando encosta em obstáculo. A inspeção permanece separada em Edit Mode. |
| Animator dos jogadores, Combate (`AnimacaoDeOutros`, animadores de ataque/esquiva), Skills (`ControleSkills`) | Dependem de parâmetros/eventos do jogo e servidor; fora da v1. Reutilizado apenas Animator/Avatar do prefab validado. |
| Itens: `ControleDoVisualEquipado`, `ApresentadorDoVisual`, `ModelosDeArma`; atributos de Personagem | Sistemas reais preservados para integração futura; diagnóstico apenas encontra encaixes pelo nome, sem equipar ou simular atributos. |
| Testes existentes: Core, Personagem, Combate, Itens, Skills, Desenvolvimento | Nenhum teste de servidor necessário para ferramenta sem regras de jogo. Validação de Editor exercita o ciclo de vida real de prefabs/cena; compilação inclui assemblies de testes. |

## Dependências e limites

Reutiliza `Art/HumanMaleUnityTest/HumanMaleHumanoid.prefab`, o FBX, Avatar e material URP da [TASK 03](human-male-unity03.md), sem alterá-los. O assembly de Editor referencia Unity, Input System já existente, Visual e Core; nenhum desses assemblies foi alterado. **Old Man Idle/ValidacaoLocal não é dependência** da cena ou catálogo: a versão publicada funciona em T-pose sem ela.

Sem animação de locomoção, rede, combate, equipamentos, face ou otimização nesta etapa. Gizmos aparecem somente na Scene View em Edit Mode com a janela aberta e seleção no personagem; não são objetos exportados. O catálogo é fixo no caminho da ferramenta, mas suas entradas são editáveis. A captura automatizada do painel usa a mesma árvore visual num UIDocument temporário; não representa a moldura nativa do Editor.

A janela usa cópias temporárias das fontes SDF compartilhadas, incluindo material e atlas: salvar a cena não deve gravar o cache de glifos em Core. A validação confere os hashes dessas fontes após salvar os assets; não versionar caches produzidos por uma inspeção.

## Animações auditadas na TASK 05

Inventário pelo AssetDatabase/MCP: 77 clips em 14 modelos com animações; somente 1 Humanoid, local. Nenhuma conversão ou importação em massa foi realizada.

| Classe | Assets/clips | Uso nesta versão |
|---|---|---|
| Humanoid utilizável e com distribuição aprovada | Nenhum identificado | Não criar controller de locomoção. |
| Humanoid com licença não estabelecida | `ValidacaoLocal/OldManIdle.fbx`, `mixamo.com` (12,166667 s) | Apenas validação local anterior; não referenciado nem versionado. |
| Potencial candidato humanoide, atualmente Generic | `ThirdParty/Quaternius/RPGCharacters/Monk.fbx` (CC0): Roll, Walk, RecieveHit, RecieveHit_Attacking, Run, Death, PickUp, Attack2, Attack, Idle_Attacking, Idle (prefixo CharacterArmature) | Walk/Run poderiam passar por auditoria e import dedicado futuros; não são Humanoid utilizáveis comprovados agora. Nenhum import alterado. Sem Idle neutra aprovada para esta integração. |
| Generic dos modelos próprios das cinco raças | Aurideo, Cinereo, Granitico, Silvano, Valdren: Idle, Andar, Correr, Atacar, Esquivar, Morrer, Dano | Específicos desses rigs; não retargetar transform curves diretamente para o Human Male. |
| Generic de mobs | GuardiaoDoReflexo: Morrer, AtacarInvestida, Atacar, AtacarRaios, AtacarPisada, Andar, Idle; JavaliDoCampo, LoboDoCampo, PresaVelha, TatuRochoso: Andar, Idle, Atacar, Morrer | Não adotados para locomoção do personagem; ataques/mortes também fora de escopo. |
| Inadequados ao humano bípede | Pig: Armature/Idle e Jump; Dragon: Dragon_Attack, Hit, Death, Flying, Attack2 (prefixo DragonArmature) | Rigs de animais/monstro, Generic. |

Providenciar para a próxima etapa Idle neutra, Walk e Run **Humanoid**, in-place e com licença/proveniência estabelecida. Pulo só precisará de animação se for parte do conjunto escolhido; a mecânica já funciona. Até lá, a T-pose permite validar Input → Movimento → Chão → Câmera separadamente do Animator.

## Verificação reproduzível

Priorize o MCP na instância aberta: executar `ValidacaoCharacterPlayground.Executar()` em Edit Mode; entrar em Play e aguardar `ValidacaoRuntimePlayground.Executar(pasta)` (Task assíncrona). O teste usa teclas padrão, dispositivos virtuais e tempo de simulação mínimo equivalente a 60 Hz; desabilita temporariamente os dispositivos físicos do Input System e os restaura em finally, inclusive em falha. Não altera bindings salvos. Capturas reutilizam o mecanismo da v1.

Alternativa para geração/CI com Editor já fechado, na raiz do worktree:

```text
npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.Playground.ValidacaoCharacterPlayground.Executar
```

`CHARACTER_PLAYGROUND_OUTPUT` escolhe a pasta dos PNGs e `validacao-playground.json`; padrão temporário do sistema, fora do git. O comando falha se duplicar instância, Reset perder posição/rotação/escala, Avatar/sockets/altura falharem, dependências locais entrarem ou assets originais mudarem. Também reabre a cena, confere exclusão do build e protege contra Spawn em cena alheia.

Validação inicial: 1,86416054 m, base 0 m, 54 bones usados pela skin, Humanoid válido e quatro encaixes encontrados; nenhuma advertência durante a operação. Logs de inicialização podem listar assemblies vazios e avisos internos do Unity já existentes. Resultados detalhados e capturas ficam no relatório da entrega.

Na v2, o teste de Play Mode também verifica movimentos em seis direções, limite diagonal, rotação, mudança rápida de direção, parada, caminhada, pulo/gravidade/grounded, follow/órbita/zoom, Reset após queda, instância/câmera únicas, Esc/retomada, Avatar/sockets e preservação dos assets. Ao sair do Play, conferir câmera de inspeção ativa, ausência de controles/cápsulas temporários e dispositivos virtuais removidos. A validação depende de Game View e foco no Editor; ela não simula sessão ou rede.
