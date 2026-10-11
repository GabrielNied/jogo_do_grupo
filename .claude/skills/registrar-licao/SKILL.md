---
name: registrar-licao
description: Registra em docs/licoes.md um problema de processo que acabou de acontecer (comando que travou, trabalho perdido, retrabalho de agente, ferramenta que enganou) e a regra para não repetir, e aponta a regra nos docs certos. Use quando algo deu errado no fluxo de trabalho, ao fim de uma tarefa que teve tropeço, ou quando pedirem para documentar o que se aprendeu.
---

# Registrar lição

Cada problema de processo vira uma lição escrita, para ninguém (pessoa ou IA) cair nele de novo. A lista fica em [docs/licoes.md](../../../docs/licoes.md), a fonte única; os outros docs só apontam para ela.

## Quando

- Algo travou, caiu, sumiu ou precisou ser refeito (computador travado, trabalho perdido, agente que parou sem editar, PR fechado antes do último commit).
- Uma ferramenta ou comando deu resposta enganosa (ex.: `unity:editor` achando que o Unity aberto é do projeto).
- Você contornou algo que vai acontecer de novo.

Não registre dúvida de design nem preferência de estilo: isso vai em `docs/features/` ou em [convenções](../../../docs/convencoes.md).

## Passos

1. **Leia** `docs/licoes.md` e veja se a lição já existe. Se existir, só reforce a entrada (data, PR) em vez de duplicar.
2. **Escreva a entrada** em `docs/licoes.md`, no bloco da data (crie `### AAAA-MM-DD · título` se não existir): **o que houve** (1 frase, com o efeito), **a regra** (imperativa, verificável) e a **origem** (PR ou issue).
3. **Aponte a regra onde ela será lida**, sem copiar o texto: um link em [agentes.md](../../../docs/agentes.md) se for de agentes, em `docs/arte/` se for de arte, em [git.md](../../../docs/git.md) se for de git, ou uma linha no [AGENTS.md](../../../AGENTS.md) se todo agente precisa saber.
4. **Dá para evitar com código?** Se for um script ou hook pequeno que impede o erro (regra 16 do AGENTS.md), implemente no mesmo PR; uma regra que uma máquina confere vale mais que um aviso. Se for grande, abra uma issue e cite no registro.
5. **Entregue:** inclua na mesma branch da tarefa; se o PR já foi mergeado, parta de `origin/main` e abra um PR de docs (`docs(core): ...`). Só Markdown: verifique com `git diff --check`.

## Checklist

- [ ] A lição diz o que houve e a regra, em poucas linhas.
- [ ] Não duplica outra lição nem copia texto de outro doc.
- [ ] Os links que apontam para a regra funcionam.
- [ ] Se dava para automatizar, foi automatizado ou há uma issue.
