# Bellator — Etapa 4: inimigos e inteligência artificial

Abra Assets/Game/Scenes/RomanArena.unity e pressione Play. Existem exatamente três inimigos iniciais, no pai Enemies: um gladiador comum, um pesado e um arqueiro. Não há ondas, seleção de personagem ou geração automática de inimigos.

## Tipos e valores iniciais
| Tipo | Vida | Velocidade | Arma / dano base | Comportamento |
|---|---:|---:|---|---|
| Comum (vermelho) | 85 | 3,1 m/s | Gládio / 12 | Aproxima, ataca e defende ocasionalmente com escudo |
| Pesado (cinza) | 160 | 2,05 m/s | Espada pesada / 27 | Corpo largo, aproximação lenta e preparação longa do golpe |
| Arqueiro (verde) | 55 | 3,6 m/s | Arco / 11 | Procura distância de tiro, recua sob ameaça e procura linha de visão |

Os valores ficam em ScriptableObjects/Enemies. As configurações de armas dos inimigos são assets próprios derivados das armas existentes; as armas do jogador foram preservadas. Todos os visuais são cópias da hierarquia geométrica anterior, com materiais e proporções diferentes, sem modelos ou animações externos.

Os controles do jogador continuam os mesmos: 1–4 para armas, mouse esquerdo para atacar/disparar, Q para golpe pesado, mouse direito para defender com escudo, Shift para correr e Espaço para esquivar. T continua sendo o comando de treino: restaura vida/resistência e reposiciona os três inimigos nos pontos iniciais. Ele não é um sistema de ondas. H/J continuam disponíveis para impactos manuais de teste da etapa 3.

## Componentes e arquivos
- Scripts/Enemies/EnemyDefinition.cs: arquétipo, atributos, arma, alcance de detecção, distância desejada, recuo e chance de defesa.
- Scripts/Enemies/EnemyBrain.cs: máquina de estados Idle, Chase, Attack, Recover, Retreat, Defend, Stunned e Dead.
- Scripts/Enemies/EnemyNavigation.cs: NavMeshAgent para caminho/desvio e CharacterController para deslocamento físico e gravidade.
- Scripts/Enemies/EnemyPoseAnimator.cs: caminhada, preparação/golpe, arco, escudo, atordoamento e morte visuais.
- Scripts/Enemies/EnemyEncounter.cs: agrupamento dos três atores e integração com o reset manual de treino.
- Prefabs/Enemies/Gladiator.prefab, Heavy.prefab e Archer.prefab.
- ScriptableObjects/Enemies: Gladiator, HeavyGladiator, Archer, CommonSword, HeavySword e ArcherBow.
- Materials/Enemies: vermelho, cinza e verde para identificação dos tipos.
- Scripts/Combat/CombatFaction.cs: equipe e opção de fogo amigo.
- Scripts/Combat/CombatQueries.cs: regras compartilhadas de acerto corpo a corpo e disparo balístico.
- Scripts/Editor/EnemyStageBuilder.cs e EnemyStageValidation.cs: criação protegida contra duplicação e testes no modo Play.

CharacterHealth, CharacterStamina, DamageReceiver, ShieldGuard, WeaponEquipment, WeaponDefinition, ImpactFeedback e CombatProjectile são os mesmos componentes utilizados pelo jogador. O PlayerCombat agora chama CombatQueries para que alcance, orientação, oclusão, dano único por golpe e cálculo balístico tenham uma implementação comum.

## Decisões de IA
A detecção considera alcance e linha de visão. A altura de visão permite ver acima de obstáculos baixos, mas não através de paredes. Um alvo visto permanece em memória por até quatro segundos sem visão direta. A posição de ataque é próxima ao jogador e respeita a orientação. O ataque usa preparação, janela ativa e recuperação da arma, além de um intervalo próprio do arquétipo. A resistência também é compartilhada com as regras existentes.

O comum pode escolher uma defesa curta quando está próximo; sua chance aumenta durante um ataque do jogador. A defesa usa exatamente o escudo e o custo de resistência da etapa anterior. O pesado tem mais vida, maior raio de colisão, velocidade menor e ataque mais lento. O arqueiro tenta manter cerca de 10 m, recua quando o jogador se aproxima de 6 m e avalia destinos laterais com caminho completo e linha de tiro quando perde a visão do alvo. Se não houver espaço para recuar, pode tentar se defender disparando da posição disponível.

Golpes recebidos usam DamageReceiver e o flash de ImpactFeedback. Dano não bloqueado interrompe a preparação do ataque e provoca um breve Stunned; a quebra de guarda causa atordoamento maior. A morte é detectada imediatamente pelo evento Changed da vida: cancela o golpe, desliga o NavMeshAgent e o CharacterController, impede novos ataques e deixa apenas a animação geométrica de queda. Projéteis ainda em voo daquele inimigo são cancelados para que não causem dano após sua morte. Não há ragdoll nem remoção automática dos corpos.

## Navegação e colisões
O NavMesh e o pacote AI Navigation existentes foram reutilizados. O agente calcula caminhos e evita outros agentes; a posição real é movida pelo CharacterController, que colide com paredes, chão, obstáculos e personagens. O agente é sincronizado com essa posição real. Raios e prioridades distintos ajudam a separar os três corpos. Um pequeno desvio lateral tenta resolver bloqueios locais persistentes. Não há teleporte rotineiro durante perseguição; Warp é usado na colocação inicial e no reset manual de treino.

## Facções
Player usa a equipe Player; os três inimigos usam Gladiators. Neutral mantém o comportamento dos bonecos de treino. DamageReceiver recusa dano do mesmo time, e a consulta de melee também filtra aliados. Projéteis param fisicamente ao atingir um aliado, mas não tiram sua vida. No CombatFaction do atacante, Allow Friendly Fire permite habilitar dano entre aliados explicitamente. A verificação ocorre na entrada compartilhada de dano, sem duplicar vida para inimigos.

## Modelos futuros
Troque VisualRoot_Replaceable dentro de cada prefab, preservando a raiz com lógica, CharacterController, agente, equipe e vida. Reassocie BodyPivot, ombros e quadris no EnemyPoseAnimator, além dos sockets de mão no WeaponEquipment. Para um modelo final com Animator, substitua apenas a animação provisória. Mantenha os colliders do personagem separados dos visuais das armas, como nas etapas anteriores.

## Testes
Bellator > Test Stage 4 Enemies in Play Mode executa verificações na arena real e grava Stage4Validation.txt. O teste cobre os três arquétipos, navegação em volta de barricada, acertos no jogador, bloqueio, defesa da IA, atordoamento, facções, flechas, recuo, obstrução, morte e reset. As suítes anteriores isolam o grupo Enemies somente durante os próprios testes para preservar seus cenários determinísticos; a IA permanece ativa normalmente no jogo.

A versão Unity 6000.3.26f1, os pacotes, as cenas anteriores, o Input original e os perfis de renderização foram preservados. Os Build Settings continuam com a configuração original; abra RomanArena diretamente para testar esta etapa.

## Resultado da entrega
- Compilação final concluída na Unity 6000.3.26f1 sem erros de C#. O conflito de nome encontrado no utilitário de testes foi corrigido.
- Stage4Validation.txt: RESULT: PASS, incluindo perseguição em volta de obstáculo real, golpes do comum/pesado no jogador, bloqueio desses golpes, defesa ocasional do comum, stun, facções e fogo amigo configurável, flechas do arqueiro, recuo, reposicionamento atrás de cobertura, morte, cancelamento de ataques/projéteis e separação física dos três atores ativos.
- Stage3Validation.txt: RESULT: PASS após a extração das regras compartilhadas de acerto e disparo.
- Stage2Validation.txt: RESULT: PASS com a IA integrada ao projeto e isolada somente durante essa suíte.
- Nenhum erro dos novos componentes ou do NavMeshAgent foi capturado na rodada final de IA. As imagens EnemyCommon, EnemyHeavy, EnemyArcher, EnemyEncounter e EnemiesHUD foram geradas durante a execução na Unity e conferidas visualmente.
- Testes alteraram posições, vida e estados somente no Play. O modo Play foi encerrado; a cena salva mantém os três inimigos iniciais ativos. Não existe solicitação de teste pendente.

Os registros mantêm mensagens de licença/assinatura do pacote Unity AI Assistant já instalado e o histórico das tentativas anteriores de compilação. Esse serviço não é usado pela IA do jogo. Os testes cobrem os três atores e os obstáculos da arena atual; não substituem avaliação de balanceamento, testes prolongados de aglomeração ou desempenho com grandes quantidades de agentes. Nenhum pacote, modelo ou animação externo foi instalado.
