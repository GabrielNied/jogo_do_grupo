# 0003. Organização por funcionalidade

**Data:** 2026-10-10
**Status:** aceita

## Contexto

Cada membro do grupo vai ser responsável por uma parte do jogo. Evitar conflitos de merge é prioridade.

## Decisão

- Pastas por funcionalidade (fatias verticais) no cliente, servidor, shared e docs.
- Funcionalidades não se referenciam diretamente; comunicam-se por eventos e contratos.
- Faixa de IDs de mensagem por funcionalidade.
- Módulos do servidor descobertos automaticamente.
- Um Assembly Definition por funcionalidade no Unity.
- CODEOWNERS definindo o revisor de cada pasta.

## Consequências

- Conflitos de merge ficam restritos a arquivos do core.
- Comunicação por eventos exige mais disciplina ao desenhar contratos.
