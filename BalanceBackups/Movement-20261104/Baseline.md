# Bellator antes do refinamento da arena

Projeto em Unity 6000.3.26f1 com URP, Input System e AI Navigation. A arena principal é `Assets/Game/Scenes/RomanArena.unity`. A seleção e os modos entram pelo fluxo de `MainMenu.unity` e `GladiatorSelection.unity`.

- Os quatro jogadores já tinham malhas estilizadas próprias, dois níveis de detalhe e rigs articulados. Os inimigos já usavam as malhas reconstruídas, mas tinham ombros e quadris animados por código, sem joelhos e cotovelos articulados. As cinco armas existentes também já tinham malhas detalhadas.
- O jogador usava CharacterController, movimento relativo à câmera, velocidade ajustada pela classe, aceleração linear e rotação amortecida. A distância da esquiva era percorrida em velocidade constante. A rotação para o alvo no início do golpe era imediata.
- O Animator do jogador já usava Blend Trees de movimento e clipes para ações com o relógio do combate. A raiz física era movida pelo motor. Os clipes de locomoção usavam oscilações angulares com correção de altura do corpo, sem uma trajetória de apoio projetada para a passada.
- Aim Lock já mantinha a orientação ao alvo e a câmera tinha amortecimento, retração por obstáculos e controle de sensibilidade. A IA usava NavMeshAgent para caminho e CharacterController para colisões. As armas eram montadas nos encaixes e as habilidades eram definidas por classe.

Controles em jogo

| Entrada | Ação |
|---|---|
| WASD, Shift, mouse e roda | Movimento, corrida, órbita e zoom |
| Tab, C e Z | Travar ou soltar o alvo e alternar alvo |
| Espaço e Alt | Rolamento e Quickstep quando a classe possui a esquiva secundária |
| Clique esquerdo, E e botão direito | Ataque leve/disparo, ataque pesado quando disponível e defesa com escudo |
| Q e R | Habilidade da classe e recarga quando aplicável |

`GitStatusBefore.txt` identifica as alterações de arte e interface já presentes. A pasta `Assets` deste backup contém as versões anteriores dos arquivos existentes abrangidos pela intervenção. Os recursos novos serão identificados no relatório de entrega.
