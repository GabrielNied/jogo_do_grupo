# 0040. Sensação ajustável ao vivo e relato com quadros e linha do tempo

**Data:** 2026-10-15
**Status:** aceita

## Contexto

O grupo quer melhorar a sensação do jogo (peso do golpe, resposta, animação, câmera). A IA não sente isso jogando sozinha: vê prints soltos e não acompanha o tempo real, e um teste automático completo pelo editor custaria caro sem resolver a parte de sensação. Quem joga sente, mas não sabe descrever o problema em termos técnicos. Os valores de sensação eram `const`, então cada tentativa exigia recompilar.

O [ADR 0017](0017-valores-de-balanceamento.md) dizia que ajuste em execução usa configuração carregada e validada pelo servidor. Isso vale para o que decide resultado, não para a apresentação local.

## Decisão

- **`Sintonia` (Core do client):** registro de valores de sensação ajustáveis em execução, só no editor e no build de desenvolvimento. Cada funcionalidade registra os do seu `Config<Assunto>` (`public static`), sem o Core conhecer nenhuma. No build de release o registro é ignorado e valem os padrões do código.
- **F10:** painel em Desenvolvimento que lista a `Sintonia` e copia os valores alterados para a IA gravar no código.
- **F9:** relato que guarda os últimos 3 s (quadros em miniatura, Animators, `timeScale`, câmera, teclas e mensagens do servidor) e grava texto curto e duas folhas de quadros. A skill `/relato` diagnostica a partir dele.
- **Convenção:** valor de sensação novo no client nasce na `Sintonia` ([Valores de balanceamento](../convencoes.md#valores-de-balanceamento)). Os antigos migram quando forem mexidos.

## Consequências

- A pessoa ajusta a sensação sem recompilar, e a IA recebe números (ms, quadros, valores) em vez de impressões.
- Valores de sensação deixam de ser `const`: um teste não pode usá-los em contexto constante, e quem mexe no valor ao vivo muda só a própria sessão.
- O servidor continua sendo a fonte do que decide resultado; nada na `Sintonia` muda dano, acerto ou custo.
- O teste automático jogando pelo editor fica de fora por enquanto.
