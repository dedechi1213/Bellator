# BELLATOR — Etapa 1: animações

Esta entrega é um laboratório independente. Não conecta os novos assets ao player principal. Não altera Aim Lock, câmera de combate, controles, regras de combate, modos, ranking ou contas.

## Diagnóstico da estrutura anterior

Os quatro gladiadores usam os mesmos meshes de primitivas, com materiais e escalas diferentes. A hierarquia tinha `BodyPivot` na altura do chão, ombros e quadris, mas não tinha cotovelos, joelhos ou tornozelos. Não havia AnimationClips nem Animator Controllers no projeto.

`PrimitiveHumanoidAnimator`, `CombatPoseAnimator`, `AbilityPoseOverlay` e `ClassPreviewPose` escrevem poses de forma procedural. Na tentativa anterior, a esquiva inclinava/abaixava o corpo; não era um rolamento de 360°. O `PlayerAnimationBridge` era uma ponte de parâmetros sem um controller compatível associado. Vários escritores de pose e um pivô no chão dificultam sobrepor animações e controlar quedas. Esses scripts existentes não foram modificados nesta etapa.

## Assets entregues

- `Clips/Common`: Idle, Walk, Run, StrafeLeft, StrafeRight, WalkBackward, DodgeRoll, Quickstep, LightAttack, HeavyAttack, Block, HitReaction e Death.
- `Clips/Survivor`: ataques leve e pesado mais curtos em amplitude.
- `Clips/Colossus`: os 13 movimentos com postura própria para espada longa e escudo, incluindo ataques mais amplos.
- `Clips/Hunter`: os 13 movimentos com postura de besta, além de Fire e Reload. LightAttack é um disparo rápido; HeavyAttack é um disparo com preparação maior. São apenas animações, sem projéteis ou dano.
- `Controllers`: Survivor, Colossus, Warrior e Hunter. Cada um contém quatro Blend Trees de locomoção e os estados de inspeção/ação.
- `PreviewPrefabs`: quatro cópias articuladas dos visuais existentes, com as armas fixas de cada classe. Nenhum modelo externo ou equipamento novo.
- `PreviewCatalog.asset`: associações entre classe, prefab e clips.
- `Evidence`: relatórios e sequências renderizadas pela Unity.
- Cena: `Assets/Game/Scenes/AnimationPreview.unity`.

São 43 arquivos `.anim` e quatro `.controller`. Os clips comuns são compartilhados; os estados executam em 1,16x para Sobrevivente, 0,80x para Colosso, 1x para Guerreiro e 1,10x para Caçador. Essas velocidades pertencem ao laboratório de animações e não alteram os atributos de gameplay.

## Como testar

1. Abra `Assets/Game/Scenes/AnimationPreview.unity` e entre em Play Mode.
2. Selecione uma das quatro classes no painel à esquerda.
3. Selecione cada animação. O painel tem rolagem para janelas pequenas.
4. Use Pausar, Velocidade e Tempo do clip para examinar preparação, execução e recuperação. O controle de tempo pausa a reprodução. Morte termina na pose caída; Reiniciar clip permite repetir.
5. Gire a câmera de teste para conferir a pose por outros ângulos. Ela ajusta a distância para enquadrar o corpo e as armas durante os golpes amplos.
6. Selecione **Testar transições do Animator** para usar os parâmetros reais do controller. Ajuste Speed entre 0 e 1, MoveX/MoveY entre -1 e 1 e as opções IsLockedOn, IsRunning e IsBlocking. Os botões disparam ações. Selecione novamente uma animação para voltar à inspeção individual.

Em Edit Mode, o Guerreiro fica visível como referência articulada. Essa referência é desativada ao iniciar o laboratório; o seletor instancia o prefab escolhido.

Os menus `Bellator > Animations Stage 1` permitem validar/renderizar novamente, testar em Play Mode ou reconstruir os assets do laboratório. A reconstrução preserva os GUIDs, mas substitui edições manuais nesses assets gerados. Os menus não fazem upgrade do player principal.

## Hierarquia e comportamento

`<Classe>PreviewPlayerRoot` contém somente Transform. Seu filho `VisualRoot` preserva a escala da classe e contém Animator, com `applyRootMotion = false`. Todos os bindings dos clips ficam abaixo desse Animator: `MotionPivot`, `BodyPivot/TorsoPivot`, cabeça, ombros/cotovelos e quadris/joelhos/tornozelos. Não há Collider, Rigidbody, CharacterController nem scripts de pose nos quatro prefabs novos.

`MotionPivot` fica no centro do corpo. O rolamento usa uma curva Euler contínua de 0 a 360°, com preparação, recolhimento e recuperação. O deslocamento local é visual e retorna ao ponto inicial. A morte usa uma queda única e conserva a pose final. A locomoção é no lugar; o movimento real no mundo fica para a etapa 2. Idle usa respiração do tronco e braços, mantendo as pernas e os pés imóveis. A defesa levanta o braço/escudo e mantém a pose final enquanto IsBlocking for verdadeiro.

O controller diferencia locomoção livre e orientada ao alvo. Caminhada não interrompe ataques ou esquivas. Hit e Die podem interromper ações vivas; Death não possui transições de saída. Fire/Reload são acionáveis apenas no controller do Caçador. Os estados de inspeção Idle/Walk/Run/Strafe são acessados diretamente pelo laboratório; o gameplay futuro deverá usar os Blend Trees.

## Parâmetros para a etapa 2

| Parâmetro | Tipo | Uso futuro |
| --- | --- | --- |
| Speed | Float | intensidade normalizada de movimento, 0–1 |
| MoveX / MoveY | Float | direção local em relação à orientação de combate, -1–1 |
| IsLockedOn | Bool | escolher os Blend Trees de avanço/lateral/recuo |
| IsRunning | Bool | escolher locomoção de corrida |
| IsBlocking | Bool | entrar e sustentar defesa |
| Dodge | Trigger | Quickstep para Sobrevivente/Caçador; rolamento para Guerreiro/Colosso |
| LightAttack / HeavyAttack | Trigger | ações visuais da arma fixa |
| Hit / Die | Trigger | reação e morte |
| Fire / Reload | Trigger | ações específicas da besta |

Na próxima etapa será necessário adaptar os nomes do `PlayerAnimationBridge` existente (por exemplo, MoveZ/LockedOn/Running/Blocking) para estes parâmetros, conectar o visual articulado à troca de classe e impedir que os antigos scripts procedurais escrevam nos mesmos ossos. Será preciso sincronizar duração dos clips com os estados, janelas de ataque, consumo de stamina, invulnerabilidade e recarga já existentes. Dano e invulnerabilidade não são eventos destes clips. O Aim Lock e a câmera existentes deverão continuar como fontes de orientação, sem receber curvas de animação.

## Limites visuais

São animações originais de primitivas articuladas, não captura de movimento. Armas e escudo seguem os sockets dos antebraços. A animação é feita no lugar e a correção de contato com o piso está gravada nas curvas; não há IK de terreno. A etapa 2 precisará ajustar a cadência dos passos à velocidade real e conferir contato das mãos/armas durante as transições e em terreno inclinado. Não há alteração de hitboxes, trajetória de projéteis ou alcance de ataques nesta entrega.

Consulte `Evidence/Validation.txt` e `Evidence/PlayModeValidation.txt` para os resultados da execução, e as imagens para as poses renderizadas.

## Verificação executada em 23/10/2026

Os assets foram gerados e compilados pela Unity 6000.3.26f1 em uma cópia isolada do projeto. A validação dos clips, bindings, hierarquia, loops e transições passou nas quatro classes. O teste em Play Mode passou nas 54 seleções individuais (13 por classe e mais disparo/recarga do Caçador), com zero erros dos scripts de preview/runtime. Também verificou a câmera acima do piso e o centro dos visuais dentro do enquadramento.

Foram revisadas visualmente sequências de poses renderizadas pela Unity e capturas da interface com as quatro classes. Isso inclui a rotação completa do rolamento, recuperação das ações, elevação/sustentação do escudo e pose final da morte. A interação foi exercitada por chamadas aos mesmos métodos usados pelo painel; não foi uma sessão manual de cliques no Editor.

Durante a revisão foram corrigidos a postura da espada longa do Colosso, o apoio da besta, a altura dos passos e a órbita da câmera de teste, que inicialmente ficava sob o piso. As curvas constantes foram reduzidas a dois pontos sem alterar as poses, evitando repetir milhares de chaves idênticas.

A cópia de teste também apresentou uma exceção interna de indexação `UnityEditor.Search.SearchDatabase` ao iniciar o Editor. Esse erro foi registrado separadamente no relatório de Play Mode; não foi tratado como um erro das animações e o sistema de busca do projeto principal não foi modificado.

O relatório `Evidence/IsolationValidation.txt` compara os 538 arquivos que já existiam no início desta etapa: nenhum mudou. A cena nova não foi adicionada à sequência de cenas de build. Na etapa 2 ainda será necessário conectar os novos visuais e parâmetros ao player e sincronizar as ações com o combate, conforme descrito acima.
