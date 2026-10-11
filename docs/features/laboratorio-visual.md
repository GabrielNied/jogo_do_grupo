# Laboratório visual

Ferramenta de desenvolvimento isolada, sem gameplay. Abertura por **Lumora → Design → Laboratório visual** no editor; não registrada na navegação do jogo, sem acesso à rede e sem alterações a cenas existentes.

Implementação em `client/Assets/_Game/Features/LaboratorioVisual/Editor/`, assembly exclusivo do editor. Componentes e interações locais em UI Toolkit, estilos USS e dados fictícios. Testes EditMode em `Tests/`.

Operação, recursos, limites, estado de aprovação e métodos de validação têm fonte única em [design/laboratorio-unity/README.md](../../design/laboratorio-unity/README.md). O usuário adotou a direção do laboratório como padrão inicial; novas interfaces seguem [Lumora UI v1](../../design/interface/GUIA-DE-ESTILO.md) e a biblioteca compartilhada, sem depender desta feature.
