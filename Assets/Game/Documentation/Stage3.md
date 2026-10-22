# Bellator — Etapa 3: equipamentos e combate

Abra Assets/Game/Scenes/RomanArena.unity e pressione Play. A arena, o personagem, o controle de movimento, a câmera e os componentes de vida/resistência anteriores continuam na cena.

## Controles
| Comando | Ação |
|---|---|
| 1 | Gládio e escudo (se ativado) |
| 2 | Espada pesada, duas mãos |
| 3 | Arco |
| 4 | Besta |
| 5 | Ativar/desativar escudo na configuração compatível |
| Mouse esquerdo | Corte leve ou disparo |
| Q | Ataque pesado com espada |
| Mouse direito, mantido | Defesa, quando há escudo |
| R | Solicitar recarga da besta vazia; a recarga também começa automaticamente após o disparo e sua recuperação |
| H / J | Aplicar impacto manual de treino frontal / traseiro ao jogador |
| T | Restaurar vida dos bonecos/jogador, resistência e estado do combate |
| Esc / clique | Liberar cursor / recapturar cursor |

WASD, Shift, Espaço, movimento do mouse e roda mantêm as funções da etapa 2. O clique usado para recapturar o cursor não dispara um golpe. Trocas de equipamento ficam bloqueadas durante golpes, recarga ou esquiva. O ataque pesado é configurável em CombatInput > Heavy Attack Binding (padrão <Keyboard>/q), e também por RebindHeavy(). O mapa InputSystem_Actions existente não foi alterado.

H/J são comandos de teste do próprio jogador. Os bonecos nunca atacam, não possuem IA e não causam dano automaticamente. Eles permitem verificar a defesa sem introduzir inimigos nesta etapa.

## Assets e arquitetura
- Scripts/Combat/WeaponDefinition.cs: configuração modular de equipamento e ataques. Crie outros assets em Create > Bellator > Equipment > Weapon.
- ScriptableObjects/Weapons: ShortSword, HeavySword, Shield, Bow e Crossbow.
- Prefabs/Weapons: cinco representações geométricas, com Visuals_Replaceable.
- Prefabs/Projectiles: Arrow e Bolt; apenas primitivas, com simulação de trajetória e colisão por varredura.
- Prefabs/Training/TrainingDummy.prefab: boneco geométrico com CharacterHealth, DamageReceiver, ImpactFeedback, colliders e obstáculo de navegação com carving.
- Scripts/Combat/WeaponEquipment.cs: inventário de teste e montagem nos pontos RightHandSocket e LeftHandSocket.
- Scripts/Combat/PlayerCombat.cs: sequência de golpes, janelas de acerto, recuperação, combos e disparo/recarga.
- Scripts/Combat/ShieldGuard.cs: defesa frontal, custo de resistência e quebra de guarda.
- Scripts/Combat/DamageReceiver.cs: entrada única de dano que consulta defesa e aplica dano a CharacterHealth.
- Scripts/Combat/CombatProjectile.cs: trajetória balística, varredura de colisões, alcance e tempo de vida.
- Scripts/Combat/CombatPoseAnimator.cs e ImpactFeedback.cs: poses provisórias e reação ao impacto.
- Scripts/Combat/CombatHUD.cs e TrainingArenaControls.cs: mira, barras e controles da área de treino.
- Scripts/Editor/CombatStageBuilder.cs e CombatStageValidation.cs: instalação protegida contra duplicação e verificações em Play.

O prefab PrimitivePlayer recebeu os novos componentes, mantendo a hierarquia e os componentes anteriores. As referências à câmera continuam como overrides da instância na RomanArena. O prefab RomanArena da etapa 1 não foi substituído. A cena possui um novo pai Training para os quatro alvos e os controles de treino.

## Configurações iniciais
| Arma | Dano base | Alcance | Resistência base |
|---|---:|---:|---:|
| Gládio | 24 | 1,8 m | 14 |
| Espada pesada | 38 | 2,45 m | 23 |
| Arco | 27 | 60 m | 16 |
| Besta | 42 | 85 m | 12 |

AttackSpeed divide as durações das fases. Cada configuração de ataque possui preparação, janela ativa, recuperação, arco, multiplicador de dano, alcance e custo. RecoveryTime adiciona recuperação à arma. O gládio possui três golpes leves, com dano multiplicado por 1 / 1,1 / 1,4; a pesada possui dois. O pesado usa configuração própria. Um clique durante a janela de continuação guarda no máximo o próximo golpe; cada golpe cobra sua resistência quando realmente inicia. O combo expira após 0,8 s sem continuidade.

A espada detecta alvos na frente do personagem e respeita o alcance e as paredes. Uma coleção por golpe impede dano repetido no mesmo DamageReceiver, mesmo que ele tenha vários colliders. O personagem mantém a orientação do golpe durante sua execução. Não há invulnerabilidade em golpes ou esquivas.

O escudo reduz 80% do dano dentro de um cone frontal de 120 graus. Um bloqueio consome 6 + 65% do dano em resistência; manter o escudo levantado consome 3/s. Sem resistência suficiente, o golpe causa dano completo e quebra a guarda por 1,2 s. Ataques traseiros passam pela defesa. Solte a defesa para permitir a regeneração normal. Correr e esquivar ficam indisponíveis durante ataque, recarga ou bloqueio; caminhar continua em velocidade reduzida.

O arco dispara flechas a 34 m/s com gravidade 9,81 m/s². A besta dispara virotes a 60 m/s, gravidade 3,5 m/s² e recarga de 1,6 s. O tiro mira no centro da câmera; uma solução balística de arco baixo compensa a queda quando a distância é alcançável. Cada projétil possui limite de alcance, vida útil, colisão contínua por varredura esférica, exclusão dos colliders do proprietário e somente um impacto. Não usa Rigidbody: a gravidade é integrada explicitamente, e as colisões são consultadas na física da Unity. A obstrução entre o corpo e a saída da arma é verificada para evitar disparos através de paredes próximas. A munição é ilimitada na área de teste.

Para novos sistemas, aplique dano usando DamageReceiver.Receive(DamageInfo); chamar CharacterHealth.Damage diretamente ignora a defesa. ImpactFeedback faz flash vermelho em dano, ciano em bloqueio e amarelo em quebra de guarda. Os bonecos inclinam ao receber dano e tombam visualmente ao esgotar a vida; T restaura o treino.

## Substituição por modelos definitivos
Troque Visuals_Replaceable nos prefabs de armas, flecha/virote e boneco. Preserve os pais, scripts e o transform Muzzle nas armas de distância. Os offsets/rotações de montagem ficam no WeaponDefinition. Mantenha os sockets de mão e associe-os aos ossos do novo modelo quando o humanoide provisório for substituído. CombatPoseAnimator e PrimitiveHumanoidAnimator devem então dar lugar ao sistema definitivo de animação. A detecção de acerto permanece separada da geometria visual.

## Verificação e limites
O menu Bellator > Test Stage 3 Combat in Play Mode entra e sai do Play e grava Stage3Validation.txt. Ele usa os colliders reais, dispositivos de Input temporários e passos controlados para cobrir dano, combos, intervalo, orientação, alcance, oclusão, defesa, quebra de guarda, tiros, recarga e referências. As posições e danos de teste só existem durante o Play. As imagens Combat*.png foram geradas na Unity para inspeção das armas/poses e do HUD.

Não foram adicionados pacotes, modelos, texturas ou animações externos. A Unity permanece em 6000.3.26f1; manifest, Input original, perfis de renderização e Build Settings foram preservados. Esta etapa não contém IA, ondas, progressão ou lógica de vitória.

## Resultado desta entrega
- Compilação real na Unity 6000.3.26f1 concluída sem erros de C#.
- Stage3Validation.txt: RESULT: PASS na rodada final, com captura de exceções dos componentes Bellator durante o Play. Nenhuma exceção dos novos componentes nessa rodada.
- A primeira execução identificou uma falha na inicialização do bloco de propriedades usado pelo flash de impacto; ela foi corrigida e a suíte foi executada novamente. Também foi corrigida a composição da pose de descanso para evitar acúmulo de rotação do braço e manter o escudo vertical.
- A suíte da etapa 2 foi executada novamente com o combate instalado: RESULT: PASS para caminhada, corrida, esquiva, colisões, resistência e câmera.
- As capturas CombatSword, CombatHeavy, CombatShield, CombatBow, CombatCrossbow e CombatHUD foram inspecionadas. CombatHUD mostra a interface real capturada no Play; as demais são renderizações da câmera na Unity.
- A cena voltou ao estado salvo, fora do Play, com jogador no spawn original. Posicionamentos/danos feitos pelos testes não foram gravados na cena.

Continuam aparecendo mensagens de autorização/assinatura do Unity AI Assistant já instalado. Esse serviço não é usado pelo combate e não foi alterado. Não foi feito teste de desempenho em hardware diferente nem ajuste definitivo de balanceamento/sensibilidade; os valores continuam expostos nos assets e componentes.
