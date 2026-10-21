# 0053. Volumes de áudio por código, sem AudioMixer

**Data:** 2026-10-21
**Status:** aceita

## Contexto

A regra do [Áudio](../features/interface.md#áudio) pede três volumes (Geral, Música e Efeitos) controlados no menu de opções, e sugeria um `AudioMixer` com três grupos. O `AudioMixer` é um asset que só o editor cria (não há API de runtime para montá-lo) e que o agente não pode escrever à mão (regra 9). Os sons de combate já eram sintetizados em código e tocavam numa `AudioSource` própria, fora de qualquer mixer.

## Decisão

- O **Geral** vale no `AudioListener.volume` (já era assim em `OpcoesGravadas`).
- **Música** e **Efeitos** viram multiplicadores em código: `VolumesDoJogo.Musica` e `VolumesDoJogo.Efeitos` (Core, `Scripts/Audio`), lidos das opções a cada uso. Cada fonte multiplica pelo volume do seu grupo: a música no `AudioDoJogo`, a interface e os passos no Core, os golpes e acertos no `ControleImpacto` do Combate.
- A escala do controle (0 a 100) vira amplitude ao quadrado, para o meio soar como metade.
- Sem mixer, efeitos de grupo (abafar, eco) ficam por conta de cada fonte; o único hoje, a música baixar com o personagem caído, é um fator no `AudioDoJogo`.

## Consequências

- Nada de asset a criar no editor; o volume muda na hora em que a pessoa mexe no controle.
- Fonte nova de som precisa lembrar de multiplicar por `VolumesDoJogo.Efeitos` (ou `Musica`). Se um dia o grupo quiser efeitos de mixer (snapshot, reverb), o `AudioMixer` entra criado por script de editor e substitui os multiplicadores.
