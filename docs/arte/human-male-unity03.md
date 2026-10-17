# Human Male Base — primeiro teste Unity Humanoid

Cena isolada de validacao da TASK 03; **nao substitui os jogadores das racas** nem implementa controller/gameplay.

## Resultado

- Avatar valido e Humanoid (`Avatar.isValid` e `isHuman`), com T-pose conferida e `Optimize Game Objects` desligado.
- A primeira tentativa automatica nao reconheceu `Head`. O mapa explicito no importador resolveu; nenhum bone foi renomeado ou alterado.
- Altura da malha deformada: **1,8641605 m**, escala do modelo/prefab 1, pes em Y=0. A medicao usa `BakeMesh`: os bounds conservadores do renderer incluem margem e nao representam a altura corporal.
- Unity importa **33.586 vertices** por separacoes de UV/normais. O FBX original permanece byte a byte identico (27.504 vertices na origem); nao houve retopologia.
- Ate quatro influencias por vertice; zero vertices sem peso. `Weapon.R`, `Costas` e os encaixes permanecem acessiveis.
- Material URP Lit, BaseColor sRGB, Normal Map, Roughness/Metallic lineares. O mapa adicional MetallicSmoothness copia Metallic em RGB e usa `1 - Roughness` no alpha. Os quatro PNGs originais nao mudam.

**TASK 03 CONCLUIDA pela TASK 03B:** o usuario forneceu e autorizou `Old Man Idle.fbx`. O importador da animacao cria seu proprio Avatar Humanoid valido; o clip `mixamo.com` (12,166667 s) e reproduzido pelo Animator no Avatar ja valido do Human Male. Nao se copiam rotacoes de bones nem se usa Avatar do destino como se fosse o skeleton de origem.

Dois ciclos Mecanim avaliados a 60 Hz: normalizedTime final 1,999968, nove poses visuais distintas, 18 capturas frontal/lateral e 259 amostras de malha deformada. Quadril varia ate 79,18 mm; raiz sem deriva, root motion desligado e curvas de deslocamento/rotacao baked into pose. Foot IK ligado. Ajuste constante de chao da instancia: -0,345 mm. Sem alteracao de skeleton/rest pose, geometria ou pesos.

Inspecao de root/quadril, coluna, cabeca, ombros/axilas, bracos/cotovelos/maos e pernas/joelhos/pes: sem inversoes, colapso estrutural grave ou clipping evidente no ciclo Idle. Observacao: contato das solas varia de -6,19 mm a +4,88 mm para o pe mais apoiado; nao e contato matematicamente perfeito. Joelhos entre aproximadamente 153 e 171 graus, sem inversao visual. Idle nao valida passada/corrida ou root motion de locomocao. Ultima execucao sem warnings/errors registrados.

## Abrir e reproduzir a validacao

1. Abra o client deste branch no Unity 6000.6.5f1.
2. Abra [HumanMaleHumanoid.unity](../../client/Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/HumanMaleHumanoid.unity).
3. Confira o FBX em Rig: Humanoid, Create From This Model, Optimize Game Objects desligado. Em Configure, confira as atribuicoes e a T-pose.
4. Inspecione o prefab [HumanMaleHumanoid](../../client/Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/HumanMaleHumanoid.prefab), o material e os encaixes. Essa cena publicada continua como referencia em T-pose; a cena animada e gerada localmente pelo procedimento abaixo.

O mapeamento completo tem fonte no [gerador de editor](../../client/Assets/_Game/Features/Personagem/Editor/TesteHumanMaleHumanoid.cs), metodo `CriarMapa`: Quadril/Hips, Coluna/Spine, Tronco/Chest, Pescoco/Neck, Cabeca/Head e membros E/Left, D/Right; UpperChest vazio.

Para gerar novamente, com o editor fechado:

```powershell
$env:HUMAN_UNITY_TEST_OUTPUT = 'C:/caminho/para/evidencias'
npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.TesteHumanMaleHumanoid.Gerar
```

O comando gera cena, prefab, material, mapa combinado e evidencia JSON/PNG pelo Unity. Animacoes da pasta ValidacaoLocal nao viram dependencias do prefab publicado.

### Reproduzir a Idle fornecida

O arquivo foi identificado como Mixamo, sem identificacao CC0/CC-BY fornecida. Para respeitar a regra 9, FBX da Idle, seu importador e assets que o referenciam permanecem em `ValidacaoLocal/`, ignorados pelo Git. Nao sao distribuidos no repositorio publico. O gerador e as evidencias/documentacao sao entregues; quem reproduz fornece o seu arquivo autorizado.

```powershell
$env:HUMAN_IDLE_FBX = 'C:/caminho/Old Man Idle.fbx'
$env:HUMAN_UNITY_TEST_OUTPUT = 'C:/caminho/para/evidencias'
npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.ValidacaoRetargetHumanMale.Executar
```

Abra `Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/ValidacaoLocal/HumanMaleIdle.unity` e entre em Play para reproduzir com o Animator minimo (um estado). Essa cena e o controller sao criados pelo Unity, apenas locais. A avaliacao automatizada usa `Animator.Update` a 60 Hz no editor; captura as poses com `BakeMesh`, evitando reutilizacao de skinning GPU no mesmo frame, e recusa evidencia com uma unica imagem de pose. Nao se afirma um teste de gameplay/CharacterController.

## Origem e preservacao

Arquivos fornecidos localmente pelo usuario em `J:/BlenderAssetsAI/Human_Male_Base_v1_UnityTest/`; importados em `Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/`. Nao houve download de assets. O `.blend` de autoria nao foi adicionado ao projeto.

SHA-256 do FBX fornecido/importado: `e2c1e9d0a662bef29443639a5c3190cfc64b1fde8281d4ed6a538a8b2c813fb4`.

SHA-256 da Idle fornecida: `cef1cb3a8c0504fcc2549b06e0d320f83af0726df8b8d800833a4a3c3dc7f52e`. Ambos os FBXs conferidos por hash antes/depois, sem mudanca. Sem mudancas no rosto, geometria, skeleton, pesos ou texturas-fonte. Core, ProjectSettings e prefabs de racas preservados. A aprovacao e limitada ao retarget da Idle fornecida.
