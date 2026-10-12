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
- Não luta, não é alvo e não é montaria. Não tem bolsa própria.
- Ajuda a juntar o loot do dono: se um item que o dono poderia coletar com F está a até **8 m** do pet, o pet vai até o item, o servidor coleta para o inventário do dono e o pet volta a seguir. Um item por vez. Inventário cheio deixa o item no chão. Item de outra pessoa não sai do lugar.
- Dispensar remove. Desconectar remove para os outros. Na dungeon ele continua com o dono.
- Quem está na cidade vê o pet dos outros.
- Fora desta versão: combate do pet, coleta de recurso de profissão, nível do pet e a coleção de conhecimentos.

## Em aberto

- [ ] Qual modelo CC0 entra no cervo jovem (a implementação escolhe e registra a licença).

## Regras

Quem decide: **servidor** (existência, posição e a coleta). O client pede chamar ou dispensar e mostra. A coleta usa a mesma regra de dono e de inventário do loot no chão ([fatia jogável, seção 5](fatia-jogavel.md#5-loot-no-chão)).

O pet não é um inimigo e não entra no catálogo de criaturas hostis. A posição segue o mesmo jeito de publicar movimento que os mobs já usam, sem biblioteca nova.

## Depende de

[Mundo](mundo.md) (posição do dono) e [Itens](itens.md) (o loot que o dono pode coletar).

## Expõe

A posição do pet, para quem está na mesma área ver.
