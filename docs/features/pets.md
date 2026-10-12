# Pets

**Responsável:**
**Status:** decidido, fora da fatia jogável
**Faixa de IDs de mensagem:** a reservar quando a funcionalidade for criada

## Resumo

A pessoa chama um pet, ele segue por perto e some quando é dispensado. O servidor decide onde o pet está.

## Definido

Decidido em 2026-10-12, a partir da proposta da #161 e do cervo jovem do livro:

- O primeiro pet é o **cervo jovem**. Ele já vem com o personagem; a captura pelo bestiário, que obtém outros, continua proposta em [mobs](mobs.md).
- Um pet chamado por vez. Chamar de novo no mesmo pet não duplica.
- Ele anda até o dono e para a **2 m** (`DistanciaDeSeguidaMetros`). Não fica em cima do dono. Se passar de **20 m** (`DistanciaParaAlcancarMetros`), o servidor o coloca de volta na distância de seguida.
- Não luta, não carrega item, não é alvo e não é montaria.
- Dispensar remove. Desconectar remove para os outros. Entrar numa dungeon também remove; ao sair, ele não volta sozinho.
- Quem está na cidade vê o pet dos outros.
- Fora desta versão: combate do pet, coleta, nível do pet e a coleção de conhecimentos.

## Em aberto

- [ ] Qual modelo CC0 entra no cervo jovem (a implementação escolhe e registra a licença).

## Regras

Quem decide: **servidor** (existência e posição). O client pede chamar ou dispensar e mostra.

O pet não é um inimigo e não entra no catálogo de criaturas hostis. A posição segue o mesmo jeito de publicar movimento que os mobs já usam, sem biblioteca nova.

## Depende de

[Mundo](mundo.md) (posição do dono).

## Expõe

A posição do pet, para quem está na mesma área ver.
