---
name: relato
description: Lê um relato F9 de sensação do jogo (quadros, linha do tempo em ms e ajustes do F10), diagnostica com números e propõe o valor para testar no F10 ou a correção. Use para "/relato", "olha o relato" ou quando a pessoa colar valores copiados do F10.
---

# Relato

O que é o F9 e o F10: [Relato e ajuste ao vivo](../../../docs/features/desenvolvimento.md#relato-e-ajuste-ao-vivo). Termos: [sensacao-do-jogo.md](../../../docs/sensacao-do-jogo.md). Não peça à pessoa para descrever de novo: os dados estão na pasta.

## 1. Ler, nesta ordem, e parar quando bastar

A pasta é o argumento ou `relatos/<conteúdo de relatos/ultimo.txt>` na raiz do checkout principal (worktree não tem relatos).

1. `nota.txt`: a frase, a latência e os valores do F10 fora do padrão.
2. `linha-do-tempo.txt`: só mudanças, em ms e com o quadro (`qNNN`).
3. `folha-impacto.png`: 12 quadros seguidos em volta do último acerto, numerados.
4. Só se precisar: `folha-geral.png`, um `quadros/NNN.png` específico ou um trecho de `quadros.csv`.

## 2. Diagnosticar com números

Ligue a frase a um termo da doc de sensação e prove com a linha do tempo. Exemplos:

- Resposta: clique em 412 ms, clipe do golpe em 520 ms, `AtaqueIniciado` em 560 ms.
- Peso: `AtaqueAcertou` em 640 ms sem `speed → 0` no alvo e sem `timeScale`, tremor de 1 cm.
- Animação: o clipe troca no meio da transição (`*`) ou volta ao parado antes do acerto.

Latência alta na nota muda o diagnóstico: diga se o problema é a rede e não a sensação.

## 3. Propor

- **Valor que existe no F10:** a mudança exata para a pessoa testar ("`Combate.HitStopPesado` de 0.07 para 0.1"). É barato e ela decide sentindo.
- **Valor ainda `const`:** a correção inclui movê-lo para o `Config<Assunto>` e registrar na `Sintonia` ([convenção](../../../docs/convencoes.md#valores-de-balanceamento)).
- **Código ou animação:** a causa e o arquivo.

## 4. Seguir

- **Valores copiados do F10** (`Grupo.Nome = valor (padrão X)`): grave como novo padrão no `Config<Assunto>`, atualize o número citado em `docs/features/<x>.md` se houver, e entregue pelo fluxo normal (issue, `puxar`, PR).
- **Mudança maior:** issue com o diagnóstico, os trechos da linha do tempo e a descrição dos quadros (as imagens não vão para o GitHub). Siga `/decida` para a ficha.
- Relatório curto: o que a pessoa sentiu, o que os números mostram, o que mudar.
