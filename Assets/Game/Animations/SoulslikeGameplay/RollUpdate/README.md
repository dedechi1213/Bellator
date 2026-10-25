# Rolamento para as quatro classes

Espaço executa o rolamento nas quatro classes. WASD define a direção relativa à câmera, incluindo diagonais; sem direção, usa a frente do personagem. Com Aim Lock, o alvo válido é mantido pelo sistema existente.

Alt esquerdo executa o Quickstep do Sobrevivente e do Caçador. A tecla fica em `ThirdPersonInput > Quickstep Binding` no prefab `Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab`; também pode ser alterada por `RebindQuickstep`. A dica de controles mostra a tecla atual apenas nas classes que possuem Quickstep. Espaço tem prioridade se os dois comandos forem pressionados juntos.

| Classe | Deslocamento do rolamento | Duração de deslocamento | Recuperação | Stamina |
| --- | ---: | ---: | ---: | ---: |
| Sobrevivente | 3,9 m | 0,50 s | 0,14 s | 20 |
| Caçador | 3,6 m | 0,54 s | 0,16 s | 23 |
| Guerreiro | 3,6 m | 0,65 s | 0,22 s | 26 |
| Colosso | 3,2 m | 0,72 s | 0,30 s | 32 |

Os valores estão em `Assets/Game/ScriptableObjects/Classes/*.asset`, na seção `Dodge`. `Has Quickstep` e `Quickstep` configuram a esquiva secundária. Foram preservados os valores anteriores do Quickstep: Sobrevivente 2,6 m / 0,35 s / 0,12 s / 18 stamina; Caçador 2,5 m / 0,35 s / 0,14 s / 21 stamina. A habilidade do Sobrevivente continua aplicando seus modificadores às duas esquivas.

A janela de invulnerabilidade permanece entre 18% e 62% do deslocamento. O dano consulta o motor existente, sem desligar colliders. Distância pode diminuir por colisão. As duas esquivas compartilham bloqueio de ações e recuperação: apertar outro comando durante a animação não muda seu perfil nem enfileira outra esquiva.

## Animações e conexão

- `Survivor/DodgeRoll.anim`: clipe real, pernas mais recolhidas, curvas de articulações e correção geométrica de altura, usando a estrutura existente.
- Caçador: reutiliza `Assets/Game/Animations/SoulslikeStage1/Clips/Hunter/DodgeRoll.anim`; disparo e recarga permanecem nos mesmos estados e clipes.
- `Controllers/SurvivorGameplay.controller` e `HunterGameplay.controller`, na pasta pai: `Dodge` leva ao rolamento, `Quickstep` à esquiva curta. Ambos continuam conectados aos prefabs animados usados por `PlayerClassController` no jogador real.
- O corpo visual gira progressivamente; o motor desloca o CharacterController. O progresso da animação acompanha a duração e a recuperação da ação aceita. A câmera e o Aim Lock existentes não recebem alterações.

## Arquivos de código alterados/criados

- `Assets/Game/Scripts/Character/ThirdPersonInput.cs`: comando separado e reconfigurável.
- `Assets/Game/Scripts/Character/ThirdPersonMotor.cs`: seleção de perfil no mesmo motor, aplicada apenas após aceitar a esquiva.
- `Assets/Game/Scripts/Character/PlayerAnimationBridge.cs`: gatilho próprio do Quickstep.
- `Assets/Game/Scripts/Classes/GladiatorClassDefinition.cs`: perfil secundário opcional por classe.
- `Assets/Game/Scripts/Classes/PlayerClassController.cs`: conexão do perfil secundário na aplicação da classe.
- `Assets/Game/Scripts/Combat/CombatHUD.cs`: controles atualizados.
- `Assets/Game/Scripts/Editor/AnimationStageBuilder.cs`: geração restrita de rolamento e pose ajustada do Sobrevivente.
- `Assets/Game/Scripts/Editor/RollUpdateTools.cs`: migração dos dois perfis e conexão restrita dos assets, preservando ajustes em execuções posteriores.
- `Assets/Game/Scripts/Editor/PlayerIntegrationValidation.cs`: verificações adicionais ativadas por `-bellatorAllRolls`.

Também foram alterados os assets das classes Sobrevivente/Caçador e seus dois controllers de gameplay. Os perfis de Guerreiro/Colosso e os demais clipes foram preservados.

## Validação

Os resultados desta entrega ficam em `Evidence`, separados dos relatórios históricos das etapas anteriores. A validação usa a Unity 6000.3.26f1, as cenas reais, os prefabs reais, o CharacterController, o Animator e eventos do Input System em uma cópia isolada do projeto. As imagens são renderizações da câmera da arena. A avaliação subjetiva do controle com teclado físico permanece uma revisão manual.

Resultados em 23/10/2026:

- **IntegrationValidation.txt — PASS:** 64 casos direcionais de rolamento (oito direções × Aim Lock ligado/desligado × quatro classes), câmera rotacionada, distância e custo, clipe nativo sincronizado, colisão com parede, collider ativo, recuperação e ataques/Q após a esquiva. Comandos incompatíveis e troca entre esquivas durante a recuperação são rejeitados. Espaço e Alt testados pelo Input System nas duas classes ágeis; reconfiguração para outra tecla e prioridade do Espaço também passaram. Rolamentos e Quicksteps executaram no jogador real dos modos História e Infinito.
- **SoulslikeRegression.txt — PASS:** stamina, janela parcial de invulnerabilidade no sistema de dano, alvo/câmera, ataque, defesa, postura, habilidade original, disparo e recarga da besta.
- **CorrectionsRegression.txt — PASS:** seleção, controles, equipamento exclusivo, habilidades, interface e navegação.
- **MatchRegression.txt — PASS:** cinco ondas e travessia real da saída com as quatro classes, vitória, derrota, pausa e reinício.
- **InfiniteRegression.txt — PASS:** ondas, dificuldade, pontuação, ranking e retorno ao modo História.
- **AccountsRegression.txt — PASS:** criação/login, persistência, perfis isolados, recuperação de backups, falhas de armazenamento previstas e restrições de convidados. A execução final usou o código original de contas, idêntico ao entregue.
- **ScopeValidation.txt — PASS:** 755 arquivos existentes comparados com a captura inicial; somente 12 alterações intencionais, nenhuma exclusão ou alteração fora do escopo.
- **DeliveredAssets.txt — PASS:** hashes de 15 assets nativos entregues conferidos contra a cópia executada, incluindo prefab do jogador, quatro classes, quatro visuais e quatro controllers.
- **SurvivorArena.png, HunterArena.png, WarriorArena.png e ColossusArena.png:** imagens revisadas da câmera real, com rolamento, combate, locomoção e morte.

A primeira tentativa da regressão de contas encontrou erro de gravação temporária; a causa não foi determinada. A tentativa de diagnóstico completou os testes funcionais, mas seu registro adicional de exceções fez o teste reprovar ao detectar a falha de escrita provocada pelo próprio teste negativo. O registro adicional foi retirado e a repetição com os arquivos originais passou. As duas tentativas ficam arquivadas em `AccountsFirstAttempt.txt` e `AccountsDiagnosticAttempt.txt`; nenhuma alteração no sistema de contas foi entregue. O editor também registrou uma exceção do índice de busca `UnityEditor.Search.SearchDatabase`, fora do código do jogo.
