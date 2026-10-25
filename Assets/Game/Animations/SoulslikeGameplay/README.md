# BELLATOR — Etapa 2: animações no player real

Integração para Unity **6000.3.26f1**, usando o motor compartilhado, o CharacterController, a câmera e o Aim Lock que já existiam no projeto.

## Problemas identificados e corrigidos

1. O jogo instanciava os prefabs visuais antigos, sem os Animators da etapa 1. Os quatro assets de classe agora apontam para `animatedVisualPrefab`, com estrutura articulada e Animator. O campo `visualPrefab` continua servindo à apresentação na seleção de classes.
2. A ponte anterior enviava velocidade em unidades por segundo e parâmetros como `MoveZ`/`LockedOn`, enquanto os controllers preparados esperavam valores normalizados e `MoveY`/`IsLockedOn`. A ponte agora usa os nomes e intervalos corretos.
3. A hierarquia nova acrescentou MotionPivot, TorsoPivot e cotovelos. As referências de tronco, braços e sockets do equipamento foram adaptadas. As armas de demonstração foram retiradas das cópias de gameplay; `WeaponEquipment` monta uma única arma fixa e o escudo autorizado para cada classe.
4. Poses procedurais não devem disputar os mesmos ossos com o Animator. Os visuais de gameplay não têm `PrimitiveHumanoidAnimator`; os componentes antigos de combate/habilidade mantêm sua condição de não escrever poses quando a ponte controla um Animator. Os scripts continuam disponíveis às dependências anteriores, incluindo treinamento e apresentação.
5. Um tempo de saída fixo no Animator poderia encerrar um ataque antes da recuperação real ou deixar uma animação continuar após um cancelamento. Os controllers de gameplay deixam o relógio das ações sob responsabilidade dos sistemas existentes. A ponte reproduz os clips com tempo normalizado correspondente às fases de preparação, atividade e recuperação.
6. A velocidade visual agora parte do deslocamento **real** medido pelo CharacterController, evitando caminhada contínua contra uma parede. A cadência acompanha a velocidade e a escala da classe por `LocomotionRate`. Interrupções também zeram a velocidade medida.
7. Uma entrada recebida durante o último passo da recuperação da esquiva podia ser aceita quando o contador zerava dentro do mesmo passo. O motor agora descarta a entrada recebida enquanto a esquiva anterior ainda estava em andamento.

## Arquivos de gameplay

| Arquivo/pasta | Resultado |
| --- | --- |
| `Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab` | Visual inicial articulado, referência real do Animator na ponte, sockets atualizados; preserva componentes de movimento, câmera referenciada, Aim Lock e lógica |
| `Assets/Game/ScriptableObjects/Classes/{Survivor,Colossus,Warrior,Hunter}.asset` | Referência ao visual animado próprio; atributos e equipamento exclusivos preservados |
| `Assets/Game/Animations/SoulslikeGameplay/Visuals/` | SurvivorAnimatedVisual, ColossusAnimatedVisual, WarriorAnimatedVisual e HunterAnimatedVisual |
| `Assets/Game/Animations/SoulslikeGameplay/Controllers/` | SurvivorGameplay, ColossusGameplay, WarriorGameplay e HunterGameplay |
| `Assets/Game/Animations/SoulslikeStage1/Clips/` | Os 43 AnimationClips reais da etapa 1, compartilhados diretamente pelos novos controllers |

Os clips conectados abrangem Idle, Walk, Run, StrafeLeft, StrafeRight, WalkBackward, DodgeRoll, Quickstep, LightAttack, HeavyAttack, Block, HitReaction e Death, com as variantes de classe existentes. O Caçador utiliza os clips próprios **Fire** e **Reload**; ele continua sem ataque pesado de espada. As habilidades Q do Colosso e Caçador reutilizam HeavyAttack e Fire durante a preparação/execução, mantendo seus efeitos originais. Os buffs do Sobrevivente e Guerreiro preservam seus efeitos e podem acompanhar locomoção/defesa.

O laboratório da etapa 1 e seus controllers permanecem separados. Na cena real, a configuração de classe e o prefab do player fornecem os Animators usados em jogo; não é necessário configurar referências manualmente.

## Scripts alterados

- `Character/PlayerAnimationBridge.cs`: parâmetros compatíveis, cadência por velocidade medida, reprodução das ações em seus tempos reais, defesa sustentada, hit, morte com pose final, disparo/recarga e direção visual da esquiva.
- `Character/ThirdPersonMotor.cs`: velocidade real exposta à animação, progresso visual de viagem/recuperação, contador de esquivas, limpeza de velocidade na interrupção e proteção da entrada durante a recuperação.
- `Combat/PlayerCombat.cs`: contador de ataques e mapeamento das fases reais para o tempo visual; stamina verificada antes de entrar no estado de ataque. O cálculo/aplicação de dano continua no sistema de combate existente.
- `Classes/GladiatorClassDefinition.cs`: referência adicional ao prefab visual de gameplay.
- `Classes/PlayerClassController.cs`: instancia o novo visual e encontra os pivôs/sockets articulados, sem acrescentar um escritor procedural de locomoção ao novo rig.
- `Editor/MatchStageValidation.cs`: substitui a expectativa antiga de inclinação procedural de 65° pela verificação do estado Death real no Animator.
- Novos `Editor/PlayerIntegrationTools.cs` e `Editor/PlayerIntegrationValidation.cs`: atualização explícita dos assets e validação da integração na arena real.

`ThirdPersonInput`, `CombatInput`, `PlayerLockOn`, `ThirdPersonCamera`, os scripts dos modos, ranking e contas não precisam ser substituídos para esta integração. Seus controles e comportamentos continuam sendo as fontes utilizadas pelo player.

## Movimento e sincronização

- WASD relativo à câmera; movimento diagonal normalizado.
- Sem alvo, o motor vira o PlayerRoot para a direção percorrida. Com alvo, mantém a orientação para o inimigo e a ponte alimenta os Blend Trees direcionais com a velocidade local medida.
- Shift conserva corrida, gasto de stamina e atributos da classe. A animação acompanha a velocidade real, inclusive durante aceleração e desaceleração.
- Espaço inicia deslocamento físico da esquiva pelo CharacterController. O clip acompanha viagem e recuperação. Os limites de invulnerabilidade, distância, custo e duração continuam configurados em `GladiatorClassDefinition.dodge`.
- Sobrevivente/Caçador usam Quickstep; Colosso/Guerreiro usam rolamento. No rolamento direcional, apenas o visual gira na direção da esquiva, voltando à orientação normal na recuperação. O PlayerRoot continua respeitando o Aim Lock. O Quickstep orienta o deslocamento/inclinação visual sem comandar a câmera.
- Ataques leves/pesados acompanham as janelas reais do combate, incluindo combos e cancelamento permitido para esquiva. Uma troca para caminhada não encerra um ataque enquanto a ação real continua ocupada.
- Botão direito sustenta Block somente com escudo; soltar retorna à locomoção. Bloqueios não são tratados como dano comum para a reação visual.
- HitReaction acompanha dano aceito fora de uma ação comprometida ou quebra de postura. A quebra de postura interrompe as ações pelo sistema de estados existente. Morte tem prioridade sobre as demais ações e sustenta a pose final.
- O Animator permanece com Root Motion desativado. Nenhum clip possui bindings de câmera, Lock-on, Rigidbody, CharacterController ou PlayerRoot.

## Como testar no jogo

1. Abra `Assets/Game/Scenes/MainMenu.unity`.
2. Entre com sua conta ou como convidado, escolha a classe pelo fluxo existente e inicie História ou Infinito.
3. Clique na janela Game para capturar o mouse no Editor, se necessário.
4. Teste WASD, diagonal, Shift e mouse. Use Tab para travar um inimigo; A/D e S devem reproduzir lateral/recuo com o corpo orientado ao alvo.
5. Teste Espaço com e sem alvo e observe o deslocamento real e a stamina. Experimente também sem stamina suficiente e junto a paredes.
6. Teste clique esquerdo, E, defesa com botão direito e Q. No Caçador, confira o disparo e a recarga; E continua sem criar um ataque de espada.
7. Confira dano, quebra de postura, morte e reinício. Experimente os quatro gladiadores e os dois modos.

Os menus `Bellator > Player Stage 2` permitem reconstruir as conexões e executar o teste integrado. A ferramenta altera somente as conexões previstas nesta entrega; não recria a arena, os modos ou o Aim Lock.

## Evidências e limites

`Evidence/IntegrationValidation.txt` registra a execução em Play Mode, carregando MainMenu e RomanArena, validando estados do Animator, deslocamento físico, fases de dano e entradas reais do Input System com dispositivos de teste. As imagens `*Arena.png` foram renderizadas pela câmera da arena durante esses testes e revisadas visualmente.

Nas folhas de imagens, leia da esquerda para a direita e de cima para baixo: Idle, Walk, Run, Strafe com alvo; recuo com alvo, esquiva, ataque leve/disparo, ataque pesado (ou Caçador sem espada); defesa/ausência de escudo, dano, Q/recarga, morte.

As regressões adicionais registram câmera/Lock-on, partidas com cinco ondas e saída, Infinito/progressão/ranking e contas. Os testes de ranking/contas usam seus repositórios temporários de validação. A distinção de convidado continua preservada.

### Resultado executado em 23/10/2026

| Validação | Resultado | Evidência |
| --- | --- | --- |
| Integração dos quatro personagens na arena e comandos do Input System | PASS | `Evidence/IntegrationValidation.txt` |
| Movimento, câmera, aquisição/troca/perda de alvo, esquivas, stamina e combate | PASS | `Evidence/SoulslikeRegression.txt` |
| Equipamentos fixos, habilidades, interface e correções anteriores | PASS | `Evidence/CorrectionsRegression.txt` |
| História: cinco ondas, portão, vitória, derrota/reinício nos quatro personagens | PASS | `Evidence/MatchRegression.txt` |
| Infinito: progressão, estatísticas, ranking e distinção de convidado | PASS | `Evidence/InfiniteRegression.txt` |
| Contas e persistência | PASS | `Evidence/AccountsRegression.txt` |
| Referências: 43 clips, quatro visuais e quatro controllers; cópias entregues iguais às testadas | PASS | `Evidence/BindingsValidation.txt` |
| Escopo: 703 arquivos preexistentes conferidos; 11 alterações previstas, nenhuma inesperada | PASS | `Evidence/ScopeValidation.txt` |

`Evidence/RegressionSummary.txt` resume as cinco suítes de regressão. A única atualização necessária em uma verificação antiga foi trocar a busca de `BodyPivot` com inclinação procedural de 65° pela confirmação do estado **Death** no Animator. O fluxo do modo História não foi alterado para satisfazer esse teste.

A cópia isolada apresentou a mesma exceção interna de indexação `UnityEditor.Search.SearchDatabase` observada na etapa 1. Os testes distinguem esse problema do Editor dos erros dos scripts de jogo. As suítes acima terminaram com sucesso.

Esta execução usa testes automatizados em Play Mode e inspeção das capturas; não representa uma sessão manual prolongada com mouse e teclado físicos. As animações continuam sendo de primitivas articuladas, sem IK para terreno inclinado. A cadência é aproximada e ajustável por `walkCycleDistance` e `runCycleDistance` na ponte. O refinamento de contato dos pés em rampas, contato das duas mãos com a arma e sensação subjetiva das transições ainda merece uma rodada manual de ajuste, sem impedir as ações e os fluxos testados.
