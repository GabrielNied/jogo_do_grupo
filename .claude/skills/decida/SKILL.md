---
name: decida
description: Organiza o quadro de tarefas do Jogo do Grupo, registra decisões pendentes e destrava o que o /trabalhe vai implementar. Use quando pedirem "/decida", "organizar as tasks", "ver o que está pendente" ou "destravar implementações". Não implementa funcionalidade.
---

# Decida (quadro e destravamento)

Você organiza e destrava. Não implementa. Não escreva código de jogo (client, server, shared), não puxe issue para `em-andamento` e não abra PR de funcionalidade. Decisão que vira regra fica em `docs/features/` e na issue, num PR de documentação.

`/decida` já autoriza escolher e registrar. Não pergunte o que decidir, não ofereça um cardápio e não espere OK. Implementar a leva é o `/trabalhe`.

## 1. Ler o quadro

`npm run tarefa -- listar` e os PRs abertos (`gh pr list`). Para o que está travado, leia a issue e a seção da doc. Olhe o [foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) e a [visão](../../../docs/visao-geral.md).

Separe em três grupos:

- **Pronta:** dá para o `/trabalhe` implementar sem decisão nova.
- **Travada:** label `bloqueada` ou `decisao`, ou a issue tem dúvida em aberto que muda o resultado.
- **Em andamento:** já tem dono. Não mexa (regra 15).

## 2. Decidir o que destrava

Para cada travada, nesta ordem:

1. A doc ou um ADR já respondeu: comente na issue com o trecho e tire a dúvida de aberto. Não reescreva a regra.
2. A issue traz uma proposta alinhada à visão e ao foco do marco: adote. Registre em `docs/features/<x>.md` (fonte única) e comente na issue o que ficou valendo.
3. Mudaria algo marcado como **Definido:** não mude. Deixe a proposta escrita na issue e siga com o definido.

Prefira o que é fácil de mudar (constante nomeada, [regra 17](../../../AGENTS.md)). Várias decisões da mesma leva vão num PR só de docs.

## 3. Organizar

- Duas issues descrevem a mesma cena: comente nas duas qual fica, e deixe a outra apontando para ela.
- Issue de épico grande sem cena: não a parta em código. Se couber, comente o próximo recorte jogável para o `/trabalhe` puxar.
- PR aberto que já cumpre a cena: não reimplemente. O relatório cita o link.

## 4. Entregar

Se mudou doc, abra um PR `docs` (skill [preparar-pr](../preparar-pr/SKILL.md)), com commit, push e sem esperar pedido (regra 12). Merge é humano: não peça para mergear.

Relatório curto:

- Prontas para `/trabalhe` (número e uma linha).
- Decisões registradas (issue e doc).
- O que continua parado, e por quê.
