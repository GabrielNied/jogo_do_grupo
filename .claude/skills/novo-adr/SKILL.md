---
name: novo-adr
description: Registra uma decisão técnica (arquitetura, biblioteca, ferramenta ou processo) como ADR em docs/decisoes/. Use quando uma decisão for tomada ou pedirem um ADR.
---

# Novo ADR

ADRs registram **por que** uma decisão técnica foi tomada. Ficam em `docs/decisoes/NNNN-titulo-curto.md`.

## Passos

1. **Número:** liste `docs/decisoes/` e use o maior número existente + 1, com 4 dígitos (`0005`).
2. **Arquivo:** `docs/decisoes/NNNN-titulo-em-kebab-case.md`, português sem acento no nome do arquivo (ex.: `0005-biblioteca-de-rede.md`).
3. **Conteúdo:** copie `docs/decisoes/_template.md` e preencha:
   - Título: `# NNNN. Frase curta com a decisão` (ex.: `# 0005. LiteNetLib para a rede`).
   - **Data:** a data de hoje (AAAA-MM-DD).
   - **Status:** `proposta` se ainda vai ser discutida pelo grupo; `aceita` se já foi decidida.
   - **Contexto:** o problema e as opções avaliadas, em poucas frases.
   - **Decisão:** o que foi escolhido, em tópicos.
   - **Consequências:** o que melhora, o que piora e o que fica limitado.
   Siga o tom dos ADRs existentes: curto, direto, em português.
4. **Se a decisão substitui outra:** no ADR antigo, troque o status para `substituída por NNNN`. Não apague o ADR antigo.
5. **Atualize quem aponta para a decisão:**
   - Se ela estava em "Decisões em aberto" de `docs/arquitetura.md`, remova de lá e, se fizer sentido, descreva o resultado na seção certa com link para o ADR.
   - Se muda comandos, ferramentas ou regras que os assistentes de IA precisam seguir, atualize o `AGENTS.md`.
   - Se muda um item de "Status e próximos passos" do `README.md`, atualize a lista.

Sugestão de commit: `docs(core): adiciona ADR NNNN sobre <assunto>` (use o escopo da funcionalidade afetada, ou `core` se for geral).
