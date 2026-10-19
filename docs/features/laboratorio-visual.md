# Laboratório visual

**Responsável:**
**Status:** ferramenta do editor (estado de aprovação em [design/laboratorio-unity/README.md](../../design/laboratorio-unity/README.md))
**Faixa de IDs de mensagem:** nenhuma (sem rede)

## Resumo

Ferramenta de desenvolvimento isolada, sem gameplay. Abertura por **Lumora → Design → Laboratório visual** no editor; não registrada na navegação do jogo, sem acesso à rede e sem alterações a cenas existentes.

## Definido

Implementação em `client/Assets/_Game/Features/LaboratorioVisual/Editor/`, assembly exclusivo do editor. Componentes e interações locais em UI Toolkit, estilos USS e dados fictícios. Testes EditMode em `Tests/`.

Operação, recursos, limites, estado de aprovação e métodos de validação têm fonte única em [design/laboratorio-unity/README.md](../../design/laboratorio-unity/README.md). O usuário adotou a direção do laboratório como padrão inicial; novas interfaces seguem [Lumora UI v1](../../design/interface/GUIA-DE-ESTILO.md) e a biblioteca compartilhada, sem depender desta feature.


## Em aberto

Nenhum no momento; o que a ferramenta ainda precisa vive na doc do design linkada acima.

## Regras

Sem regra de jogo: é ferramenta de editor. Não usa servidor, não altera cenas existentes e não entra no build.

## Depende de

Nada de outra funcionalidade.

## Expõe

Nada.
