# 0024. Rede sem voltar no tempo: o client é dono do próprio corpo

**Data:** 2026-10-11
**Status:** aceita

## Contexto

O jogador não pode ver o personagem "voltar no tempo" (rubber banding). Isso estraga a experiência mais do que qualquer atraso. Em 2026-10-11 o jogo tinha três fontes disso:

- **Movimento:** o servidor media o tempo entre passos pela chegada dos pacotes. Com o jitter da rede, dois pacotes chegam quase juntos e parecem rápidos demais. O passo era recusado e os seguintes passavam a ser medidos a partir da posição antiga, que já tinha ficado para trás. A recusa virava cascata, e acima de 3 m o client era puxado de volta. Isso acontecia com jogador honesto.
- **Esquiva:** se o servidor recusava (ex.: sem vigor), o client desfazia o deslocamento e devolvia o personagem ao ponto de partida.
- **Ataque e esquiva:** a animação só começava quando o servidor respondia, uma ida e volta inteira depois do clique.

Como os MMOs de referência fazem, pelo que se sabe deles publicamente:

- **World of Warcraft:** o client é dono da própria posição e manda eventos de movimento com o próprio relógio e um sinal periódico. O servidor valida (velocidade autoritativa, com o aviso e a confirmação `FORCE_RUN_SPEED_CHANGE`) e repassa aos outros. Correção só por comando explícito (`MSG_MOVE_TELEPORT` com confirmação), nunca por reconciliação. Magias começam na hora; o servidor confirma ou cancela sem mexer na posição.
- **Final Fantasy XIV:** movimento do client. O servidor decide o acerto pela posição dele num instante (o "snapshot"), então às vezes se leva dano depois de sair da área na própria tela. A posição do jogador não volta.
- **Black Desert:** movimento e animação de combate locais; o servidor valida o resultado. A dessincronia aparece nos outros jogadores e nos NPCs, não no próprio personagem.
- **New World:** ficou conhecido por confiar demais no client em alguns pontos (houve exploits, como ficar invulnerável forçando atraso de rede). A lição: posição pode ser do client; resultado de combate, não.

## Decisão

**O client é dono do próprio corpo: posição, rotação e animação. O servidor é dono dos resultados: dano, acerto, recurso (vida, vigor, energia), loot, inventário e economia.** A regra 3 do AGENTS.md (servidor autoritativo nos resultados) continua valendo inteira.

1. **Movimento.** O client move o personagem e informa a posição (10 por segundo). O servidor valida com um **orçamento de deslocamento**: cada personagem junta até 2 s de corrida e gasta o que anda, então pacotes em rajada não viram recusa. Excesso pequeno (até 6 m) é aceito e conta como suspeita; suspeita repetida ou posição impossível é **caso grave**. O servidor não devolve a posição ao dono; só repassa aos outros. Números em `docs/features/mundo.md`.
2. **Correção só explícita.** A única forma de o servidor mudar a posição do jogador é a `PosicaoForcada`, com confirmação (`ConfirmarPosicaoForcada`). Até a confirmação, o movimento do client é ignorado, porque ainda é de antes do teleporte. Usos: teleporte e viagem rápida, renascer depois da morte, empurrão ou puxão de boss, e o caso grave (trapaça ou glitch). Jogador honesto nunca recebe correção pelo movimento normal.
3. **Ações (ataque, esquiva, skills).** O client anima e desloca na hora, sem esperar o servidor. O servidor decide se a ação vale (custo, recarga, alcance, acerto) e responde. **Recusa nunca desfaz o movimento:** o client mostra o motivo, para a ação onde está e devolve o controle. O que depende do resultado (número de dano, vida do alvo, recurso gasto) só aparece quando o servidor confirma.
4. **Recursos e cooldowns no client.** O client pode e deve conferir localmente o que sabe (vigor, recarga) antes de agir, para a recusa ser rara. O servidor confere de novo e é quem cobra.
5. **Estado com outros jogadores (inventário, loot, troca, economia, grupo).** Pedido e resposta, sem otimismo: a interface espera o servidor. Esses estados não têm o "voltar no tempo" visual de posição, mas podem ser explorados se o client antecipar.
6. **Outros jogadores e inimigos** aparecem pelas réplicas do servidor e são suavizados no client (interpolação). Um salto grande de quem está longe é aceitável; o próprio personagem nunca salta.
7. **Velocidade e mudanças de estado do corpo** (buff, lentidão, montaria) seguem o modelo de aviso e confirmação do #263: o servidor manda, o client aplica e confirma, e até a confirmação vale a mais permissiva das duas.

## Consequências

- **Fim do rubber banding** para jogador honesto: o jitter não vira recusa, e a esquiva recusada não volta.
- **Menos mensagens:** o dono deixa de receber a própria posição dez vezes por segundo.
- **Trapaça pequena passa às vezes.** Um speedhack leve rende no máximo 6 m de vantagem por vez, e repetir vira caso grave. O total de suspeitas por personagem fica guardado para a ferramenta de GM (#187) e a proteção contra trapaça (#211).
- **O acerto continua do servidor**, com a posição que ele tem. Como no FFXIV, quem desvia no último instante na própria tela pode ainda levar o golpe; a compensação de latência (#172) reduz isso.
- Funcionalidades novas seguem esta regra: animar e deslocar na hora, confirmar resultado no servidor, nunca desfazer posição na recusa, e corrigir posição só com `PosicaoForcada`.
- Os testes que esperavam a recusa estrita e o eco da posição mudaram para o modelo novo. Os testes de integração passam a confirmar o movimento consultando a posição do servidor (`PosicaoNoServidor.EsperarAsync`).
