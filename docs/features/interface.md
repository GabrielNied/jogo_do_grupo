# Interface (Core)

**Responsável:**
**Status:** em definição (números em **proposta**, a calibrar)
**Faixa de IDs de mensagem:** não tem (tudo local no client; o servidor não participa)

## Resumo

Telas e menus do client que não pertencem a uma funcionalidade do jogo. Por enquanto, o menu de opções aberto com Esc (issue #89). Código no Core do client; mudanças em `Core/` pedem review do responsável.

## Definido

- O menu abre com **Esc**, tem **continuar**, **opções**, **sair da conta** e **sair do jogo**. "Sair da conta" já existe (apaga o token gravado; veja [personagem.md](personagem.md)).
- Opções iniciais (da issue #89): volume (geral, música, efeitos), qualidade gráfica, resolução e tela cheia, sensibilidade da câmera, idioma. Gravadas neste computador.
- Remapear teclas fica fora (tarefa própria, #90).

## Em aberto

- [ ] Abrir o menu pausa o jogo? Proposta: **não**. O jogo é online e persistente, o mundo segue rodando; o personagem fica parado e vulnerável (sem tela de pausa).
- [ ] Idiomas disponíveis na primeira versão (proposta: só português; a opção existe mas lista um idioma).
- [ ] Sair do jogo/da conta dentro de dungeon ou combate: sai na hora ou tem espera (proposta: espera de 10 s em combate)? Decisão do grupo.
- [ ] Remapear teclas (#90): só listado, não desenhado aqui.
- [ ] HUD configurável (#93): só listado, não desenhado aqui.
- [ ] Áudio (#96): só listado; os sons em si não são desenhados aqui.

## Regras

**Base:** menus de pausa de jogos de PC online (WoW, New World). **Igual:** Esc abre o menu; opções ficam num submenu. **Diferente:** não pausa o jogo (proposta).

### Menu (Esc)

| Item | Regra |
|---|---|
| Esc | abre o menu; Esc de novo ou **Continuar** fecha. Se há outro painel aberto (inventário, mapa), Esc fecha o painel primeiro |
| Continuar | fecha o menu |
| Opções | abre a lista abaixo; **Voltar** retorna ao menu |
| Sair da conta | apaga o token e volta ao login (já existe) |
| Sair do jogo | fecha o aplicativo |

Com o menu aberto, o cursor fica livre e a câmera e o movimento não recebem input (**proposta**).

### Opções iniciais

| Opção | Valores (proposta) | Padrão |
|---|---|---|
| Volume geral / música / efeitos | 0 a 100, passo 5 | 80 / 70 / 80 |
| Qualidade gráfica | Baixa, Média, Alta (níveis de qualidade do Unity) | Média |
| Resolução | as do monitor | resolução atual |
| Tela cheia | sim/não | sim |
| Sensibilidade da câmera | 0,5x a 2,0x, passo 0,1 | 1,0x |
| Idioma | Português | Português |

- Gravadas neste computador (arquivo local do client, não na conta); valor inválido ou ausente volta ao padrão. Aplicam na hora, sem botão "Aplicar" (proposta).
- Servidor autoritativo não se aplica: nada aqui afeta regra de jogo.
- **Testes esperados** (client, EditMode): salvar e ler as opções; valor fora da faixa é limitado; arquivo corrompido volta aos padrões.

### Fora desta primeira versão

Remapear teclas, ajustes de HUD, acessibilidade (daltonismo, tamanho de texto), controle de gamepad e opções de rede.

## Depende de

[Personagem](personagem.md) (sair da conta), Core do client (input, UI base).

## Expõe

Nada.
