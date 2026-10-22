# Bellator — Etapa 2: personagem e câmera

Abra Assets/Game/Scenes/RomanArena.unity e pressione Play.

## Controles
- W/A/S/D: caminhar em relação à câmera.
- Shift esquerdo ou direito: correr enquanto houver resistência.
- Espaço: esquiva na direção pressionada; parado, esquiva para a frente do personagem.
- Mouse: girar a câmera; roda: aproximar/afastar (1,5 a 8 m).
- Escape: soltar o cursor e suspender comandos. Clique na Game View para recapturá-lo. Perder foco também solta o cursor.

## Organização
Player contém CharacterController, ThirdPersonInput, ThirdPersonMotor, CharacterHealth e CharacterStamina. VisualRoot_Replaceable/BodyPivot contém o humanoide geométrico, pivôs independentes de ombros e quadris, cabeça, tronco, braços, pernas, mãos e pés. PrimitiveHumanoidAnimator movimenta somente esse visual. O controlador de colisão não depende do visual nem das animações.

PlayerCamera contém ThirdPersonCamera, Camera, AudioListener e configuração URP. ArenaOverviewCamera da etapa 1 foi mantida e desativada na cena, evitando duas câmeras/listeners ativos. O prefab original RomanArena não foi substituído.

Prefabs novos:
- Assets/Game/Prefabs/Characters/PrimitivePlayer.prefab
- Assets/Game/Prefabs/Characters/ThirdPersonCamera.prefab

As referências entre jogador e câmera ficam configuradas na cena. Ao instanciar os prefabs em outra cena, associe Movement Camera no motor e Target/Input na câmera, ou use Configure().

## Ajustes
Valores iniciais: caminhada 3,5 m/s, corrida 6,2 m/s, aceleração 18 m/s², desaceleração 24 m/s² e gravidade -25 m/s². Esquiva: 3,6 m em 0,42 s, 28 de resistência e 0,8 s de recarga após terminar. Colisões interrompem o deslocamento sem atravessar o cenário. A esquiva não concede invulnerabilidade nesta etapa.

Vida: 100. Resistência: 100, consumo de corrida 18/s e regeneração 22/s após 1,2 s sem consumo. Exaustão exige recuperar uma pequena reserva antes de retomar corrida contínua. Vida e resistência expõem eventos Changed para uma futura interface. Nenhum HUD ou sistema de combate foi adicionado.

A câmera segue uma âncora de posição independente da rotação visual, limita a inclinação entre -25 e 65 graus e usa varredura esférica que ignora o próprio jogador. Obstáculos retraem a câmera imediatamente; a distância se recupera suavemente quando a visão fica livre. A esfera também cobre a área do plano de recorte próximo para evitar vazamento nas bordas da tela. Se a própria âncora estiver obstruída, a câmera se recolhe até ela.

Para um modelo definitivo, substitua VisualRoot_Replaceable e desative/remova PrimitiveHumanoidAnimator. Preserve Player, CharacterController e os componentes de lógica. Ajuste altura, centro e raio do CharacterController conforme o modelo. O jogador é ignorado em futuros bakes de navegação.

## Validação
O menu Bellator > Test Stage 2 in Play Mode executa verificações na arena real, entra e sai do Play automaticamente e grava Stage2Validation.txt. Os testes reposicionam o jogador somente no Play; o estado salvo da cena permanece intacto. O menu Install Stage 2 Player preserva personagens existentes e recusa instalação duplicada.

A versão Unity 6000.3.26f1, os pacotes, o asset de Input original e os Build Settings foram preservados. O input do jogador usa um mapa próprio do Input System instalado. Nenhuma dependência externa, arma, inimigo, animação importada ou modelo externo foi utilizado.

## Resultado desta execução
Compilação concluída na Unity 6000.3.26f1 sem erros de C#. A suíte no modo Play terminou com RESULT: PASS após o ajuste final das poses. Ela verificou bindings do Input System com dispositivos virtuais temporários, o CharacterController real contra os colisores da arena, velocidades, desaceleração, custo/duração/distância/recarga de esquiva, regeneração e limites/obstrução da câmera. As imagens PlayerWalk.png, PlayerRun.png e PlayerDodge.png foram capturadas no Play e inspecionadas, além da prévia inicial PlayerPreview.png. A sessão de testes foi encerrada e a cena retornou ao estado salvo.

Os testes de movimento usam passos controlados de 1/60 s, sem substituir colisores ou desativar as paredes da arena. Não representam avaliação subjetiva de sensibilidade do mouse ou teste de desempenho em outros computadores. Os ajustes de velocidade, sensibilidade e câmera continuam expostos no Inspector.

Persistem no registro mensagens NoSubscription/entitlements do Unity AI Assistant previamente instalado. O personagem não utiliza esse serviço. Nenhuma configuração desse pacote foi alterada.
