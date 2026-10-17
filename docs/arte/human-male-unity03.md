# Human Male Base — primeiro teste Unity Humanoid

Cena isolada de validacao da TASK 03; **nao substitui os jogadores das racas** nem implementa controller/gameplay.

## Resultado

- Avatar valido e Humanoid (`Avatar.isValid` e `isHuman`), com T-pose conferida e `Optimize Game Objects` desligado.
- A primeira tentativa automatica nao reconheceu `Head`. O mapa explicito no importador resolveu; nenhum bone foi renomeado ou alterado.
- Altura da malha deformada: **1,8641605 m**, escala do modelo/prefab 1, pes em Y=0. A medicao usa `BakeMesh`: os bounds conservadores do renderer incluem margem e nao representam a altura corporal.
- Unity importa **33.586 vertices** por separacoes de UV/normais. O FBX original permanece byte a byte identico (27.504 vertices na origem); nao houve retopologia.
- Ate quatro influencias por vertice; zero vertices sem peso. `Weapon.R`, `Costas` e os encaixes permanecem acessiveis.
- Material URP Lit, BaseColor sRGB, Normal Map, Roughness/Metallic lineares. O mapa adicional MetallicSmoothness copia Metallic em RGB e usa `1 - Roughness` no alpha. Os quatro PNGs originais nao mudam.

**Ponto de parada solicitado:** foram encontrados 76 clipes de animacao no projeto, mas **zero `AnimationClip.humanMotion` adequados**. Os clipes de racas e do Monk, inclusive Idle/Walk, estao importados como Generic. Nao foram convertidos, sintetizados ou baixados clipes; nao foi criado um AnimatorController vazio. Reproducao Humanoid e retarget em movimento **nao estao aprovados** nesta etapa.

As capturas do Unity confirmam visibilidade, orientacao, contato com o chao e material em repouso. Nao demonstram deformacao animada. A execucao final terminou sem warnings/errors do gerador; o erro de Head foi somente da primeira tentativa, resolvido pelo mapa manual.

## Abrir e reproduzir a validacao

1. Abra o client deste branch no Unity 6000.6.5f1.
2. Abra [HumanMaleHumanoid.unity](../../client/Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/HumanMaleHumanoid.unity).
3. Confira o FBX em Rig: Humanoid, Create From This Model, Optimize Game Objects desligado. Em Configure, confira as atribuicoes e a T-pose.
4. Inspecione o prefab [HumanMaleHumanoid](../../client/Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/HumanMaleHumanoid.prefab), o material e os encaixes. O personagem fica em T-pose ate haver um clipe Humanoid autorizado.

O mapeamento completo tem fonte no [gerador de editor](../../client/Assets/_Game/Features/Personagem/Editor/TesteHumanMaleHumanoid.cs), metodo `CriarMapa`: Quadril/Hips, Coluna/Spine, Tronco/Chest, Pescoco/Neck, Cabeca/Head e membros E/Left, D/Right; UpperChest vazio.

Para gerar novamente, com o editor fechado:

```powershell
$env:HUMAN_UNITY_TEST_OUTPUT = 'C:/caminho/para/evidencias'
npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.TesteHumanMaleHumanoid.Gerar
```

O comando gera cena, prefab, material, mapa combinado e evidencia JSON/PNG pelo Unity. Inventaria novamente os clipes e para se nao houver Humanoid. Se no futuro existir um clipe adequado, cria um Animator minimo e registra amostragem; a aprovacao visual em movimento continua necessaria.

## Origem e preservacao

Arquivos fornecidos localmente pelo usuario em `J:/BlenderAssetsAI/Human_Male_Base_v1_UnityTest/`; importados em `Assets/_Game/Features/Personagem/Art/HumanMaleUnityTest/`. Nao houve download de assets. O `.blend` de autoria nao foi adicionado ao projeto.

SHA-256 do FBX fornecido/importado: `e2c1e9d0a662bef29443639a5c3190cfc64b1fde8281d4ed6a538a8b2c813fb4`.

Sem mudancas no FBX, rosto, geometria, skeleton, pesos ou texturas-fonte. So importadores, assets derivados de material, prefab/cena de teste e gerador de editor. Core, ProjectSettings e prefabs de racas preservados.
