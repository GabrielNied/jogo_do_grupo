---
name: decida
description: Organiza o quadro de tarefas do Jogo do Grupo, registra decisões pendentes e destrava o que o /trabalhe vai implementar. Use quando pedirem "/decida", "organizar as tasks", "ver o que está pendente" ou "destravar implementações". Não implementa funcionalidade.
---

# Decida (quadro e destravamento)

Você organiza e destrava. Não implementa. Não escreva código de jogo (client, server, shared), não puxe issue para `em-andamento` e não abra PR de funcionalidade. Decisão que vira regra fica em `docs/features/` e na issue, num PR de documentação.

`/decida` já autoriza escolher e registrar. Não pergunte o que decidir, não ofereça um cardápio e não espere OK. Implementar a leva é o `/trabalhe`. Melhoria do que já está no jogo, em forma de tarefa nova, é o `/melhore`.

## 1. Ler o quadro

`npm run tarefa -- listar` e os PRs abertos (`gh pr list`). Para o que está travado, leia a issue e a seção da doc. Olhe o [foco atual](../../../docs/features/fatia-jogavel.md#foco-atual) e a [visão](../../../docs/visao-geral.md).

Separe em três grupos:

- **Pronta:** dá para o `/trabalhe` implementar sem decisão nova.
- **Travada:** label `bloqueada` ou `decisao`, ou a issue tem dúvida em aberto que muda o resultado.
- **Em andamento:** já tem dono. Não mexa (regra 15).

Depois rode `npm run tarefa -- relevancia`: ele lista as tarefas livres cuja regra mudou depois delas, as mais defasadas primeiro. Confira as do topo pelo `git diff` de cada uma (`relevancia <N>`) e dê a saída de [Conferir a relevância](../puxar-tarefa/SKILL.md#21-conferir-a-relevância). Aqui você pode fechar: a issue já feita ou obsoleta sai com `gh issue close <N> --reason "not planned"` e um comentário com o commit que a substituiu.

## 2. Decidir o que destrava

Para cada travada, nesta ordem:

1. A doc ou um ADR já respondeu: comente na issue com o trecho e tire a dúvida de aberto. Não reescreva a regra.
2. A issue traz uma proposta alinhada à visão e ao foco do marco: adote. Registre em `docs/features/<x>.md` (fonte única) e comente na issue o que ficou valendo.
3. Mudaria algo marcado como **Definido:** não mude. Deixe a proposta escrita na issue e siga com o definido.

Prefira o que é fácil de mudar (constante nomeada, [regra 17](../../../AGENTS.md)). Várias decisões da mesma leva vão num PR só de docs.

## 3. Pesquisar como implementar (só se a escolha técnica muda o resultado)

Para cada pronta ou recém-destravada cuja implementação tem uma escolha técnica real (biblioteca, algoritmo, padrão do Unity ou do .NET), deixe o caminho pesquisado, para o `/trabalhe` não refazer (regra 13). Pule quando o repo já resolve ou a tarefa é só regra e doc.

1. **Repo primeiro:** procure ajudante, padrão ou ADR existente (`docs/decisoes/`, `shared/`, `Core/`). Reaproveitar vence qualquer biblioteca nova.
2. **Depois a web** (WebSearch, modo padrão; `extended` só se vier raso), no máximo 2 buscas por tarefa. Procure biblioteca pronta, pacote do Unity (UPM) ou técnica consagrada, e compare no máximo 3 candidatas.
3. **Filtros que eliminam:** licença permissiva (MIT, Apache-2.0, BSD; assets só CC0 ou CC-BY, regra 9); compatível com Unity 6000.6 e C# 9 se tocar `shared/` ou client, ou com .NET 10 se for só servidor; mantida (release recente); sem tornar o servidor menos autoritativo (regra 3); sem referência entre funcionalidades (regra 2). Descarte o que falha, sem discutir.
4. **Escolha uma e registre:** comente na issue a recomendação (nome, versão, licença, por que, e o que fazer se não servir). Biblioteca ou ferramenta nova que entra no projeto vira ADR (skill [novo-adr](../novo-adr/SKILL.md)) no PR de docs. Se o melhor é escrever o próprio código, diga isso e por quê.
5. **Não instale nem adicione dependência:** adicionar o pacote é implementação, do `/trabalhe`. Nada de protótipo para testar a biblioteca.

## 4. Organizar

- **Prioridade:** toda issue que você criar, dividir ou recortar sai com um rótulo `prio:*` ([Prioridade das tarefas](../../../docs/agentes.md#prioridade-das-tarefas)). Confira também as abertas sem rótulo (o `listar` marca com ⚠) e classifique-as. Mudou a prioridade pela decisão? Troque o rótulo e diga na issue.
- Duas issues descrevem a mesma cena: comente nas duas qual fica, e deixe a outra apontando para ela.
- Issue de épico grande sem cena: não a parta em código. Se couber, comente o próximo recorte jogável para o `/trabalhe` puxar.
- PR aberto que já cumpre a cena: não reimplemente. O relatório cita o link.

## 5. Entregar

Se mudou doc, abra um PR `docs` (skill [preparar-pr](../preparar-pr/SKILL.md)), com commit, push e sem esperar pedido (regra 12). Merge é humano: não peça para mergear.

Relatório curto:

- Prontas para `/trabalhe` (número e uma linha).
- Decisões registradas (issue e doc), com a biblioteca ou técnica recomendada quando houve pesquisa.
- O que continua parado, e por quê.
