---
name: relato
description: Diagnostica um report beta do jogo (issue `reporte-beta` ou pasta em relatos/) com os números da linha do tempo, do estado, do console e do log do servidor, e propõe a correção ou o valor para testar no F10. Use para "/relato", "olha o report", ao puxar uma issue `reporte-beta` ou quando a pessoa colar valores copiados do F10.
---

# Relato

O que é o report e o F10: [Report beta e ajuste ao vivo](../../../docs/features/desenvolvimento.md#report-beta-e-ajuste-ao-vivo). Termos de sensação: [sensacao-do-jogo.md](../../../docs/sensacao-do-jogo.md). Não peça à pessoa para descrever de novo: os dados estão na issue ou na pasta.

## 1. Ler, nesta ordem, e parar quando bastar

**Issue `reporte-beta`** (o caso normal): o corpo já traz tudo. O comentário `<!-- reporte-beta -->` no topo tem tipo, severidade, área e commit.

1. A frase e o tipo. A frase é o sintoma de quem testou, não a instrução de implementação.
2. **Estado no momento:** cena, janela aberta e a última mensagem de cada tipo (ex.: `JogadorMorreu há 9.8 s` explica "ando morto").
3. **Linha do tempo:** só mudanças, em ms e com o quadro (`qNNN`). Os últimos 3 s têm imagem.
4. **Console do Unity** e **log do servidor**, se vieram: exceção ou aviso perto do momento costuma ser a causa.
5. As imagens: folha de impacto primeiro, depois a tela inteira (UX) e a folha geral.

**Pasta local** (argumento, ou `relatos/<conteúdo de relatos/ultimo.txt>` na raiz do checkout principal; worktree não tem relatos): os mesmos arquivos, mais `quadros/NNN.png` e `quadros.csv` para um quadro específico.

## 2. Diagnosticar com números

Confira em `docs/features/<área>.md` o que deveria acontecer. Depois ligue a frase a um fato da evidência e prove:

- Bug: `JogadorMorreu` às 3120 ms e `↓W` com `Morrer → Correr` às 5810 ms, sem mensagem do servidor recusando o movimento.
- Resposta: clique em 412 ms, clipe do golpe em 520 ms, `AtaqueIniciado` em 560 ms.
- Peso: `AtaqueAcertou` em 640 ms sem `speed → 0` no alvo e sem `timeScale`, tremor de 1 cm.
- UX: o que a tela inteira mostra contra o que a doc da interface pede.

Latência alta muda o diagnóstico: diga se o problema é a rede. Sem evidência suficiente, diga o que faltou e o que o próximo report precisa mostrar.

## 3. Propor e seguir

- **Issue puxada:** escreva a [ficha](../../../docs/agentes.md#ficha-da-tarefa) no corpo (o que acontece, causa com os números, onde mexer, fora do escopo) e implemente pelo fluxo normal. Duplicada de uma "parecida" listada: feche como duplicada.
- **Valor que existe no F10:** a mudança exata para testar ("`Combate.HitStopPesado` de 0.07 para 0.1").
- **Valor ainda `const`:** a correção move para o `Config<Assunto>` e registra na `Sintonia` ([convenção](../../../docs/convencoes.md#valores-de-balanceamento)).
- **Valores copiados do F10** (`Grupo.Nome = valor (padrão X)`): grave como novo padrão no `Config<Assunto>` e atualize o número em `docs/features/<x>.md`, se houver.
- **Conferir a correção:** peça um report novo do mesmo momento e compare a linha do tempo.
- Relatório curto: o que a pessoa viu, o que os números mostram, o que mudou.
