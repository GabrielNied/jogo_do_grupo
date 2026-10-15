# Sensação do jogo: como relatar

Para quem testa e sente que algo está errado (peso, resposta, animação, câmera) e para a IA que diagnostica. A ferramenta é o [relato F9 e o ajuste F10](features/desenvolvimento.md#relato-e-ajuste-ao-vivo); as regras de cada funcionalidade continuam em [features/](features/).

## Termos

| Termo | O que é | O que observar no relato |
|---|---|---|
| **Resposta** | Tempo entre o comando e o personagem reagir | Na linha do tempo, `entrada ↓Mouse 1` até a troca de clipe do jogador |
| **Antecipação** | O preparo antes do golpe, que avisa o que vem | Quadros entre a troca de clipe e o acerto; o inimigo avisa com `InimigoPreparaAtaque` |
| **Impacto** | O instante do contato | `AtaqueAcertou`, `timeScale` caindo (hit-stop), `speed 1 → 0` no alvo (pose congelada), tremor |
| **Peso** | Quanto o golpe parece ter massa | Soma de hit-stop, pausa de pose, recuo e tremor: tudo isso some no golpe "de papel" |
| **Recuperação** | O fim do golpe, até poder agir de novo | Quadros depois do acerto até o clipe voltar ao parado ou ao movimento |
| **Leitura** | Entender o que o inimigo vai fazer a tempo | O aviso do inimigo aparece cedo e claro nos quadros? |
| **Hit-stop** | O jogo quase para por alguns centésimos no acerto | `timeScale 1 → 0.05` e a volta; duração em ms |
| **Tremor** | A câmera sacode no impacto | `câmera tremor começou (N cm)` e `parou` |

## Relato bom

Aperte F9 logo depois do momento estranho (o relato guarda os 3 s anteriores) e escreva o que sentiu, não a causa:

- "o pesado não tem peso, parece que bate no ar"
- "demora para o golpe sair depois do clique"
- "o inimigo atacou sem eu ver o aviso"

Citar um jogo de referência ajuda muito: "quero o leve parecido com o do Elden Ring, curto e seco". Os jogos que o projeto segue estão em [referencias.md](referencias.md).

## Ajustar ao vivo

No F10 cada valor de sensação tem um slider. Mude, jogue, repita. Achou o ponto: "Copiar valores" e cole para a IA, que grava no `Config<Assunto>` da funcionalidade. Valor que ainda não aparece no F10 entra na [Sintonia](convencoes.md#valores-de-balanceamento) na próxima vez que for mexido.
