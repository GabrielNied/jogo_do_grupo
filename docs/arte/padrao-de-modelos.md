# Padrão dos modelos 3D próprios

Como são os modelos que o jogo gera no Blender por script, e como gerar de novo. A decisão de usar esse pipeline está no [ADR 0025](../decisoes/0025-modelos-proprios-no-blender-por-script.md).

![Javali do Campo](renders/JavaliDoCampo_vistas.png)
![Presa-Velha](renders/PresaVelha_vistas.png)
![Guardião do Reflexo](renders/GuardiaoDoReflexo_vistas.png)

## Estilo

- **Low poly com textura pintada.** A forma é simples (poucos polígonos, silhueta clara); o detalhe vem da textura: pelagem em fios, variação de tom, luz pintada (claro em cima, escuro embaixo) e oclusão de ambiente assada nas dobras. Nada de cor chapada.
- **Polígonos:** criatura comum até ~2.000 triângulos; chefe até ~4.000. Hoje: javali e Presa-Velha ~1.500, Guardião ~1.500.
- **Textura:** uma por modelo, 1024×1024 (`<Nome>_Cor.png`). Material com brilho ou luz própria ganha `<Nome>_Brilho.png` (R = metal, A = lisura, o `_MetallicGlossMap` do URP Lit) e `<Nome>_Emissao.png`.
- **Sombreamento:** suave nas partes orgânicas (pelo, pele, pedra); chapado em cascos, cristais e placas.
- **Paleta:** cada script tem a sua no topo (`PALETA`). Bichos do campo em marrons quentes e dessaturados; o que é mágico usa azul-ardósia com ciano como cor de destaque.

## Escala, eixos e esqueleto

- 1 unidade = 1 metro. Pés em Y = 0 e frente para +Z no Unity (no Blender: Z para cima, olhando para -Y). A raiz do prefab fica em escala 1; o servidor manda a escala da instância.
- Alturas atuais: Javali do Campo 0,9 m, Presa-Velha 1,5 m, Guardião do Reflexo 3,5 m (`ALTURA_ALVO` de cada script).
- **Esqueleto de quadrúpede** (pele rígida, cada parte segue um osso): `Raiz` > `Corpo` > `Cabeca` > `Orelha_E`/`Orelha_D`; `Corpo` > `Rabo`; `Corpo` > `Perna_DianteiraE`, `Perna_DianteiraD`, `Perna_TraseiraE`, `Perna_TraseiraD` (E = esquerda da criatura).

## Animações

Um clipe por ação, com estes nomes (o gerador do Unity e o teste `PrefabsDasCriaturasTestes` conferem):

| Clipe | Repete | Uso no Animator |
|---|---|---|
| `Idle` | sim | estado inicial |
| `Andar` | sim | parâmetro bool `Andando` |
| `Atacar` | não | gatilho `Atacar`; o golpe acontece perto de 1 s (o aviso do servidor) |
| `Morrer` | não | gatilho `Morrer` (o `ApresentadorDeInimigos` dispara) |

Criaturas pesadas usam o mesmo esqueleto com os clipes mais lentos (`peso` nas medidas).

## Como gerar

> **Rode um Blender por vez** (um comando termina antes do próximo começar; nunca em paralelo nem em background). Vários ao mesmo tempo travaram o computador ([lições](../licoes.md)). Confira com `tasklist | findstr blender` que nenhum ficou aberto.

Os scripts ficam em [arte/blender/](../../arte/blender/): `lib/` (malha por partes, texturas em numpy, esqueleto e animação, FBX, prévias), `criaturas/` (um script por criatura) e `personagens/` (um por raça, veja [Personagens das raças](#personagens-das-raças)). Precisa do Blender 5.2; não precisa de Python instalado.

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python arte/blender/criaturas/javali.py
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python arte/blender/criaturas/presa_velha.py
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python arte/blender/criaturas/guardiao_do_reflexo.py
```

- Cada comando regrava o FBX e os PNG em `client/Assets/_Game/Features/Mobs/Art/Modelos/`.
- `-- --previa <pasta>` também renderiza as folhas de conferência (`_vistas`, `_rosto` e `_acoes`, com seis quadros de cada clipe). `-- --saida <pasta>` grava em outro lugar.
- Depois, com o editor do Unity fechado: `npm run unity:importar` (só se surgiu arquivo novo) e `npm run unity:executar -- JogoDoGrupo.Client.Features.Mobs.Editor.GeradorDeCriaturas.Gerar`, que monta material, controlador e prefab de cada criatura.

## Criatura nova

1. Copie o script mais parecido em `arte/blender/criaturas/` (quadrúpede: `javali.py`; variação de um que já existe: `presa_velha.py`).
2. Ajuste as constantes do topo (nome, altura, paleta) e as seções da malha; a pintura usa o nome da parte de cada pedaço.
3. Gere com `--previa`, olhe as folhas e repita até ficar bom.
4. Acrescente a receita no `GeradorDeCriaturas` e rode-o.

## Personagens das raças

Um modelo humanoide por raça, com o mesmo esqueleto, as mesmas animações e o mesmo padrão de textura, mudando só o **biotipo**, a **paleta**, os **adornos** e a **pintura extra** (marcas, fissuras, brilho). Aparência de cada uma em [personagem.md](../features/personagem.md#visual-do-jogador-arte).

- **Polígonos:** ~1.800 a 2.100 triângulos por raça. **Textura:** 1024×1024 `<Raca>_Cor.png`; `<Raca>_Emissao.png` quando há brilho (Aurídeo, Graníticos e Cinéreos; Valdren e Silvano não têm).
- **Escala:** 1,8 m do chão ao topo (`ALTURA_ALVO`), contando adornos (chifres, coroa, crista). Pés em Y = 0, frente +Z no Unity. Graníticos são os mais largos e de pernas mais curtas; Silvanos, os mais esguios e de pernas mais longas (`Biotipo` de cada script).
- **Esqueleto** (pele rígida; E = esquerda do personagem, D = direita): `Raiz` > `Quadril` > `Tronco` > `Cabeca`; `Tronco` > `Costas` (encaixe das costas); `Tronco` > `Braco_E/D` > `Antebraco_E/D` > `Mao_E/D`; `Mao_D` > `Weapon.R` (encaixe da mão direita); `Quadril` > `Perna_E/D` > `Canela_E/D` > `Pe_E/D`.
- **Clipes:** `Idle`, `Andar`, `Correr`, `Esquivar` (rolamento, sem deslocar o corpo), `Atacar` (golpe perto de 0,35 s), `Morrer` (cai de costas e fica) e `Dano`. Repetem: `Idle`, `Andar`, `Correr`. O blend tree usa andar a 2 m/s e correr a 5 m/s.
- **Biblioteca:** `arte/blender/lib/humanoide.py` (biotipo, malha do corpo, esqueleto, clipes, pintura base: pele, rosto, tecido, couro, cabelo). Cada raça é um script em `arte/blender/personagens/` com `PALETA`, `BIOTIPO`, `adornos` e `extra`.

### Gerar os personagens

```
"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" --background --factory-startup --python arte/blender/personagens/valdren.py
```

Troque `valdren.py` por `aurideo.py`, `granitico.py`, `cinereo.py` ou `silvano.py` (rode um de cada vez). Cada comando regrava o FBX e os PNG em `client/Assets/_Game/Features/Personagem/Art/Modelos/`; `-- --previa <pasta>` renderiza as folhas `_vistas`, `_rosto` e `_acoes`; `-- --saida <pasta>` grava em outro lugar. Depois, com o editor do Unity fechado: `npm run unity:importar` (só se surgiu arquivo novo) e `npm run unity:executar -- JogoDoGrupo.Client.Features.Personagem.Editor.GeradorJogadoresDasRacas.Gerar`, que monta material, controlador e prefab `Jogador_<Raca>` de cada raça.
