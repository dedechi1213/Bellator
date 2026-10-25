# BELLATOR — movimentação e combate Soulslike

Unity preservada: **6000.3.26f1**. Projeto existente, cenas, modos, armas fixas, contas e formato de salvamento preservados. Nenhum pacote instalado. Não há Cinemachine no manifesto existente; a câmera própria foi adaptada.

## Arquitetura

- `ThirdPersonMotor` continua sendo o único controlador físico do jogador, utilizando `CharacterController.Move`. Movimento e esquiva são divididos em passos de até 20 ms. Não foi adicionado Rigidbody ao jogador.
- `PlayerActionStateMachine` arbitra início de ações incompatíveis e interrupções por morte/desequilíbrio: Idle, Moving, Sprinting, Dodging, Attacking, Blocking, Staggered, Dead. Recarga e preparação de habilidade ocupam o estado Attacking e têm indicadores próprios.
- `PlayerLockOn` seleciona inimigos vivos, visíveis e alcançáveis, pontuando posição na tela e distância. Usa linha de visão do personagem e da câmera. O alvo atual é abandonado após morte, desativação, distância excessiva, obstrução/inacessibilidade prolongada ou fim da partida.
- `DamageReceiver` rejeita explicitamente impactos durante o intervalo invulnerável da esquiva. Os colisores continuam ativos. `ActorPosture` controla um recurso separado da vida, sua recuperação, desequilíbrio e imunidade temporária contra novos desequilíbrios.
- `PlayerCombat` mantém um conjunto de alvos atingidos por golpe. Ataques têm preparação, execução, janela ativa e recuperação; os tempos vêm da arma. Cliques próximos da janela de combo podem entrar no buffer. O último golpe impõe recuperação adicional e não encadeia automaticamente outro combo.
- `EnemyAttackCoordinator` compartilha um orçamento de ataques por vítima, com intervalo mínimo entre inícios. A reserva cobre preparação, dano e recuperação. Morte, interrupção e desativação liberam a reserva.
- `ArenaExitTrigger` mantém o limiar e a geometria originais da fuga. Além dos callbacks de trigger, recebe os trechos de deslocamento físico do motor, para reconhecer uma travessia entre atualizações da física. Teleportes não emitem esse evento. `GameManager` continua exigindo cinco ondas, portão aberto e jogador vivo.

## Controles finais

| Comando | Ação |
|---|---|
| W / A / S / D | Movimento relativo à câmera |
| Mouse | Girar a câmera livre |
| Roda do mouse | Aproximar/afastar a câmera |
| Shift | Correr enquanto houver stamina |
| Espaço | Esquiva direcional da classe; sem direção, usa a orientação atual |
| Tab | Ativar/desativar lock-on |
| C / Z | Próximo/anterior alvo visível |
| Botão esquerdo | Ataque leve ou disparo de besta |
| E | Ataque pesado de arma corpo a corpo |
| Botão direito | Defender com escudo quando a classe possuir um |
| Q | Habilidade especial original |
| R | Recarregar a besta descarregada; a recarga automática também foi preservada |
| Esc / P | Pausar |
| F1 | Retornar à seleção |

Os comandos de treino T/H/J continuam restritos ao sistema de treino existente, desativado na partida. Teclas numéricas não trocam o equipamento fixo das classes. Não foi reintroduzido painel visual de habilidades.

C/Z são configuráveis nos campos `Next Target Binding` e `Previous Target Binding` de `ThirdPersonInput`, usando caminhos do Input System. A API `RebindTargetSwitch` permite uma futura interface de remapeamento. O ataque pesado conserva a configuração existente em `CombatInput`.

## Valores iniciais por classe

| Classe | Caminhar/correr (m/s) | Esquiva | Duração + recuperação | Stamina | Distância | Postura máxima |
|---|---:|---|---|---:|---:|---:|
| Sobrevivente | 4,3 / 7,5 | Quickstep | 0,35 + 0,12 s | 18 | 2,6 m | 65 |
| Colosso | 2,4 / 4,1 | Rolamento | 0,72 + 0,30 s | 32 | 3,2 m | 155 |
| Guerreiro | 3,5 / 6,2 | Rolamento | 0,65 + 0,22 s | 26 | 3,6 m | 105 |
| Caçador | 4,0 / 7,0 | Quickstep | 0,35 + 0,14 s | 21 | 2,5 m | 75 |

As quatro definições ficam em `Assets/Game/ScriptableObjects/Classes`. Cada uma centraliza velocidades, aceleração, desaceleração, suavização de rotação, consumo de corrida, perfil de esquiva, postura, força contra postura e custo de bloqueio. A janela invulnerável inicial vai de 18% a 62% do deslocamento, excluindo toda a recuperação. Passos Desesperados continua modificando custo, distância e intervalo da esquiva.

O Sobrevivente usa velocidade de ataque 1,4 e custo base 12; Guerreiro 1,15/14; Colosso 0,8/23. O Caçador conserva somente a besta. O Colosso reduz o custo de bloqueio para 75% e aplica 1,45× dano à postura. O inimigo pesado tem postura máxima 165.

## Parâmetros configuráveis

- **Classe:** `dodge`, `posture`, `walkSpeed`, `runSpeed`, `acceleration`, `deceleration`, `turnSmoothTime`, `sprintDrain`, `postureDamageMultiplier`, `blockCostMultiplier`.
- **Arma/ataque:** dano, stamina, velocidade, alcance/arco, `windup`, `execution`, `active`, `recovery`, `postureMultiplier`, `comboWindowStart/End`, `dodgeCancelStart/End`, `comboInputBuffer`, `finisherRecovery`, `postureDamage`. Janelas são frações da duração total do golpe; cancelamento só ocorre na recuperação. O pesado usa início de cancelamento em 92% por padrão; leves, 80%.
- **Stamina:** máximo, regeneração/segundo, atraso e fator de regeneração durante defesa (35% inicial). Segurar escudo não drena continuamente; impactos frontais gastam stamina. Ações em andamento adiam a regeneração. Valores não ficam negativos.
- **Postura:** máximo, taxa/atraso de recuperação, duração do desequilíbrio e imunidade posterior. Durante o desequilíbrio, novos golpes não prolongam o atordoamento. Os golpes continuam causando dano à vida normalmente.
- **Câmera:** sensibilidade base, pitch mínimo/máximo, distância e limites, suavização, raio/máscara de colisão, suavização do lock-on e intensidade máxima de impacto. A colisão considera a região de recorte próxima da câmera.
- **Lock-on:** alcance de aquisição 18 m, alcance máximo 26 m e tolerância de obstrução 1 s. A checagem de caminho do alvo atual é espaçada em 0,3 s.
- **IA:** postura, máximo de atacantes corpo a corpo por alvo (2), intervalo entre inícios (0,45 s) e intervalo entre reações curtas a impactos (0,9 s). Configuráveis em cada `EnemyDefinition`.
- **Habilidades:** parâmetros anteriores preservados; `areaPostureDamage` ajusta a força de postura do Golpe Devastador.

O menu Configurações, disponível no menu principal e na pausa, mantém volume, sensibilidade geral, qualidade e modo de tela. Acrescenta sensibilidade horizontal e vertical, inversão vertical e intensidade de impacto de câmera. As novas preferências usam chaves próprias, sem migrar nem apagar contas/ranking.

## Scripts criados

| Script | Responsabilidade |
|---|---|
| Character/DodgeConfiguration.cs | Perfil serializável de rolamento/quickstep |
| Character/PlayerActionStateMachine.cs | Exclusividade de ações e interrupções |
| Character/PlayerLockOn.cs | Aquisição, troca, validação e indicador de alvo |
| Character/PlayerAnimationBridge.cs | Parâmetros opcionais para Animator |
| Combat/ActorPosture.cs | Postura, recuperação, desequilíbrio e imunidade |
| Enemies/EnemyAttackCoordinator.cs | Orçamento e espaçamento de ataques por alvo |
| Editor/SoulslikeStageTools.cs | Atualização explícita de prefabs, integridade e execução de regressões |
| Editor/SoulslikeValidation.cs | Testes reais da reformulação em Play Mode |

Todos ficam sob `Assets/Game/Scripts`. Os arquivos `.meta` acompanham os novos scripts.

## Scripts modificados

- Character: `ThirdPersonInput`, `ThirdPersonMotor`, `ThirdPersonCamera`, `CharacterStamina`, `PrimitiveHumanoidAnimator`.
- Combat: `PlayerCombat`, `DamageReceiver`, `ShieldGuard`, `CombatQueries`, `WeaponDefinition`, `CombatHUD`, `CombatPoseAnimator`, `ImpactFeedback`.
- Classes: `GladiatorClassDefinition`, `PlayerClassController`, `ActorAttributes`, `PlayerAbility`, `AbilityDefinition`.
- Enemies: `EnemyBrain`, `EnemyDefinition`.
- Match: `GameManager`, `GameInterface`, `GameSettings`, `ArenaExitTrigger`.
- Editor: `CorrectionsValidation` e `InfiniteModeValidation` alinham a injeção de teclado à atualização Dynamic do Input System; o primeiro também recebeu mensagens de diagnóstico para input/pausa.

As definições de classes e ataques foram atualizadas. Os prefabs existentes recebem os componentes adicionais, preservando GUIDs, equipamento e referências. Scripts de contas, ranking, pontuação e agenda de ondas não precisaram ser reformulados.

## Animações definitivas

1. Crie um prefab visual com seu modelo e Avatar, configure um Animator Controller compatível e atribua-o ao Animator. Substitua `visualPrefab` na definição da classe; ajuste altura/raio do controlador nessa mesma definição.
2. Adicione `RightHandSocket` e `LeftHandSocket` como filhos das mãos. Sem sockets nomeados, um Animator humanoide usa os ossos das mãos como pontos de montagem. Ajuste os offsets da arma para o modelo.
3. Use os parâmetros abaixo no Controller. Parâmetros ausentes são ignorados sem emitir avisos; não é obrigatório implementar todos de uma vez.
4. Desative root motion: a ponte faz isso ao vincular o Animator, pois o deslocamento físico permanece no `CharacterController`. Os clipes devem acompanhar os tempos configurados na classe/arma. Não duplique aplicação de dano por Animation Events.
5. Configure estados visuais Idle/Walk/Run/Strafe, Roll/Quickstep, Light Attack 1–3, Heavy Attack, Block/Block Hit, Stagger e Death. A lógica não depende da existência desses clipes.

| Tipo | Parâmetros |
|---|---|
| Float | Speed, MoveX, MoveZ, DodgeProgress, AttackProgress |
| Int | ActionState, DodgeStyle, ComboStep, AttackPhase |
| Bool | LockedOn, HeavyAttack, Blocking, Staggered, Dead, Running, Dodging, Attacking, Reloading, Casting |

`MoveX/MoveZ` são velocidades locais em m/s. `ActionState` segue a ordem do enum documentada acima (0–7). `DodgeStyle`: 0 Roll, 1 Quickstep. `AttackPhase`: 0 None, 1 Preparation, 2 Execution, 3 Active, 4 Recovery. `ComboStep` começa em 1 para leves e 0 para pesados. O feedback de Block Hit pode usar o evento `DamageReceiver.Impacted`, cujo resultado informa bloqueio/quebra/desequilíbrio.

Com Animator compatível, os movimentos procedurais do jogador deixam de escrever a pose. Sem Controller, permanecem caminhada procedural, inclinação/retração dos membros na esquiva, golpes, reação de impacto, bloqueio e morte. Não foram criados clipes humanoides fictícios. O rolamento atual comunica o deslocamento com inclinação e recolhimento; uma cambalhota completa fica a cargo do futuro clipe, evitando girar o modelo geométrico através do chão.

## Execução dos testes

No editor, use **Bellator → Soulslike → Test Movement Camera and Combat**. Após editar os prefabs a partir de uma versão anterior, **Upgrade Existing Prefabs** aplica a atualização idempotente. **Check Scene and Prefab Integrity** verifica a composição das três cenas.

O método `Bellator.Editor.SoulslikeStageTools.BatchRun` executa a suíte nova e, em sequência, as regressões existentes de equipamento/seleção, História, Infinito/ranking e contas/convidado. Os testes de persistência usam arquivos isolados em Temp. Resultados individuais são gravados em Documentation e em Temp; `Temp/BellatorSoulslike.batch.txt` registra o resultado conjunto.

No modo batch, o executor cria teclado/mouse virtuais do Input System e emula o foco da Game View. A injeção de teclas ocorre antes da atualização Dynamic, evitando que o loop do editor consuma os eventos antes do jogo. As verificações usam a física real, mas não equivalem a uma sessão manual de avaliação da sensação de combate. `Documentation/SoulslikeBatchValidation.txt` preserva os resultados mesmo após a Unity limpar seus arquivos temporários ao encerrar.

## Resultados executados em 22/10/2026

Compilação dos scripts de jogo e de editor: **0 erros e 0 avisos** na verificação final. A Unity 6000.3.26f1 também compilou e executou os scripts em Play Mode na cópia temporária do projeto. Os prefabs atualizados e os relatórios foram transferidos de volta ao projeto principal; não é necessário executar um construtor de cenas para usar as mudanças.

| Suíte | Resultado | Cobertura |
|---|---|---|
| SoulslikeValidation | PASS | Input System; quatro direções e diagonais; movimento relativo à câmera; órbita/zoom/colisão; lock-on/troca/obstrução/morte/distância; rolamento e quickstep das quatro classes; janelas de invulnerabilidade; stamina; habilidades e desconto de Passos Desesperados; ataques leves/pesados; buffer/combo; cancelamento; escudo frontal/costas; quebra de guarda/postura; três inimigos e orçamento de ataques; disparo no alvo travado; recarga; configurações/pausa; morte/reinício/menu |
| CorrectionsValidation | PASS | Seleção das quatro classes, equipamento fixo, teclas 1–5 sem troca de arma, Q e cooldowns, ausência de painel de habilidades, HUD, recarga, pausa/configurações, menus e raycasts da interface |
| MatchStageValidation | PASS | Cinco ondas completas com cada classe; composições originais; entradas bloqueadas e novas tentativas de spawn; morte durante esquiva; abertura do portão; travessia física e vitória; reinício após vitória/derrota; retorno ao menu |
| InfiniteModeValidation | PASS | 35 ondas no cenário de estresse, demais classes, escala de dificuldade, pontuação, limites de atores, descarte de cadáveres, ranking/top 10/desempates, backup, falhas de gravação, resultados vinculados à conta, pausa/reinício e retorno ao História |
| AccountStageValidation | PASS | Criação/login/logout/troca de conta, identidade estável, duas contas isoladas, convidado sem registrar ranking, persistência/recarregamento, recuperação de backups e preservação de arquivos danificados |

Relatórios completos: `SoulslikeValidation.txt`, `CorrectionsValidation.txt`, `Stage6Validation.txt`, `InfiniteModeValidation.txt`, `AccountStageValidation.txt`. O resultado conjunto está em `SoulslikeBatchValidation.txt`. `SoulslikeSettings.png` registra a nova tela de configurações, inspecionada visualmente em 1280 × 819.

A preparação de dados do teste do Infinito repete apenas operações de fixture que encontram bloqueios transitórios de gravação do Windows. Rejeições de autenticação/validação continuam falhando imediatamente. As rotinas de produção de contas, ranking e seus formatos de arquivo permanecem inalterados.

## Limitações e trabalho futuro

- Nenhum problema funcional pendente foi identificado nas suítes executadas. Isso não substitui avaliação manual prolongada da sensação de combate e do balanceamento.
- Os custos, tempos e resistências são valores iniciais expostos no Inspector. O teste de 35 ondas utilizou o teto controlado de dois inimigos ativos; não é um benchmark de desempenho em todas as combinações do teto normal de inimigos do jogo.
- Modelos e animações humanoides definitivos continuam pendentes. A lógica e a ponte de Animator estão prontas; o visual geométrico comunica as ações com poses procedurais, sem uma cambalhota humanoide completa.
- Não foi gerado nem testado um executável standalone nesta tarefa. As execuções relatadas ocorreram no editor Unity com física e cenas reais.
