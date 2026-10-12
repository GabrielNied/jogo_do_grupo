# Montarias

**Responsável:**
**Status:** decidido, fora da fatia jogável
**Faixa de IDs de mensagem:** a reservar quando a funcionalidade for criada

## Resumo

A pessoa chama uma montaria, anda mais rápido e desce. O servidor decide a posição e a velocidade.

## Definido

Decidido em 2026-10-12, a partir da proposta da #160:

- Uma montaria nesta primeira versão. Montar e desmontar no mundo aberto e na cidade, no estilo do World of Warcraft: ela aumenta a velocidade e não luta.
- Entrar em combate ou levar dano não desmonta. Começar um ataque ou uma skill desmonta. Não se ataca de cima dela.
- A velocidade é **9 m/s** (`VelocidadeDaMontariaMetrosPorSegundo`), acima da corrida de 5,5 m/s. Desmontar volta à corrida. O servidor só aceita esse teto enquanto a pessoa está montada.
- Desconectar desmonta. Quem está na cidade vê o outro montado.
- Fora desta versão: estábulo, várias montarias, personalização, montaria que luta e voo.

## Em aberto

- [ ] Qual modelo CC0 entra na primeira montaria (a implementação escolhe e registra a licença).

## Regras

Quem decide: **servidor** (estado montado, velocidade e posição). O client pede montar ou desmontar e mostra.

A velocidade segue o aviso e a confirmação que o Mundo já usa para andar e correr ([ADR 0024](../decisoes/0024-rede-sem-voltar-no-tempo.md)). Não há biblioteca nova: o teto entra no validador de movimento que já existe, só enquanto a pessoa está montada.

## Depende de

[Mundo](mundo.md) (posição e velocidade).

## Expõe

O estado montado, para quem está na mesma área ver.
