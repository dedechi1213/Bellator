# Etapa 6 — sobrevivência na arena

Unity 6000.3.26f1. Inicie por `Scenes/GladiatorSelection.unity`: Play, escolher classe, confirmar e iniciar partida. A arena também pode ser aberta diretamente; nesse caso começa como Guerreiro.

A partida começa sem inimigos nem alvos de treino ativos. Após 5 segundos, começa a primeira onda. Cada inimigo aparece com intervalo de 0,65 s; há 8 segundos entre ondas. Estes valores e as composições estão em `ScriptableObjects/Waves/ArenaSurvival.asset`.

| Onda | Comuns | Pesados | Arqueiros |
| --- | --- | --- | --- |
| 1 | 2 | 0 | 0 |
| 2 | 3 | 0 | 0 |
| 3 | 2 | 1 | 0 |
| 4 | 3 | 0 | 1 |
| 5 | 2 | 2 | 2 |

Uma onda só termina depois de gerar toda a sua fila e derrotar todos os inimigos dela. Inimigos mortos são removidos ao iniciar a próxima onda. O gerenciador contabiliza somente os atores que criou; bonecos de treino e outros objetos não influenciam a contagem.

Após a última onda, uma mensagem anuncia a liberdade e o portão principal abre em 3 segundos. O marcador verde identifica a saída. A partida continua até o jogador atravessar o portão pelo lado interno e chegar ao corredor exterior. Matar a última onda não exibe imediatamente a vitória. O corredor original possui piso suficiente para a travessia antes de sua contenção provisória no fundo.

Vida zerada encerra a partida, interrompe geração e ataques, cancela projéteis, bloqueia controles e mostra Game Over. O corpo geométrico inclina para representar a morte, inclusive se o golpe fatal ocorrer durante esquiva. Reiniciar recarrega a arena com a mesma classe, vida/resistência completas, portão fechado e contagem inicial. Voltar à seleção permite escolher outra classe.

## Controles

- WASD, Shift, Espaço e mouse: movimento, corrida, esquiva e câmera.
- Mouse esquerdo: atacar/disparar; E: ataque pesado; mouse direito: escudo; Q: habilidade; R: recarga.
- 1/2: equipamentos da classe; 5: alternar escudo quando disponível.
- Esc ou P: pausar/retomar. A pausa congela ondas, IA, projéteis, abertura do portão e habilidades. O menu possui Retomar, Reiniciar e Voltar à seleção.
- F1: voltar à seleção durante a partida. T/H/J permanecem disponíveis somente no modo de treino das verificações anteriores; estão desativados na sobrevivência.

## Organização e dependências

`Scripts/Match` contém:

- `WaveSchedule`: ScriptableObject com composições e intervalos.
- `WaveManager`: fila de geração, posicionamento seguro, inimigos pertencentes à onda, contagem e eventos. Não depende de GameManager nem da UI.
- `GameManager`: estados Preparing, Fighting, Intermission, Escape, Paused, Victory e Defeat; observa vida e eventos de ondas/travessia; controla pausa, reinício e menu.
- `ArenaExitTrigger`: detector independente de travessia de dentro para fora. Usa trigger e Rigidbody cinemático compatíveis com CharacterController. Não depende do GameManager.
- `MatchHUD`: apresentação das ondas, mensagens, contagem, inimigos restantes e menus.

`UI/MatchHUD.prefab` contém a UI reutilizável, com referência ao GameManager aplicada na instância da cena. `Materials/ExitBeacon.mat` colore o marcador geométrico de saída.

`Scenes/RomanArena.unity` possui a raiz MatchSystems, com GameManager, WaveManager, WaveEnemies, MainExitCrossing, MatchUI e MatchEventSystem. As cenas e prefabs anteriores continuam presentes. A raiz Enemies e os alvos/controles de treino foram preservados, mas ficam inativos na partida normal. O HUD de vida/resistência continua ativo e atualiza as barras dos inimigos gerados.

A geração usa os quatro ArenaSpawnPoint existentes, faz rodízio entre entradas e procura alternativas próximas. Cada posição deve estar no NavMesh do agente, ter caminho completo até o jogador, espaço físico para sua cápsula e distância mínima de 5 m do jogador. Se todas as opções estiverem ocupadas, mantém o inimigo na fila e tenta novamente; não avança a onda nem força spawn sobre uma parede ou personagem.

Os atores de ondas recebem o jogador como alvo e perseguem esse alvo mesmo do outro lado da arena. A percepção e a linha de visão continuam necessárias para atacar; os atributos de percepção dos prefabs anteriores são preservados. Vida, facções, defesa, dano, armas, habilidades, projéteis, navegação e o controlador do portão são os sistemas já existentes.

## Repetir verificações

Menu Bellator > Test Stage 6 Complete Matches in Play Mode. O teste carrega seleção/arena, usa os botões de pausa e reinício, testa bloqueios de spawn, morte e atravessa fisicamente a saída nas quatro classes. Para exercitar rapidamente todas as transições de ondas, elimina os atores com o sistema de dano compartilhado; não se trata de um teste de balanceamento ou de uma partida completa jogada manualmente.

Os testes anteriores continuam isolados por `MatchStageBuilder.PrepareLegacySandbox`, utilizado somente pelos scripts de Editor: desativa a sobrevivência e restaura temporariamente a configuração de treino esperada por cada etapa. Isso não é incluído como opção nem atalho na partida normal.

Não foram adicionados pacotes, modelos externos, progressão permanente ou sistemas da próxima etapa. O balanceamento inicial de dano, vida e duração das partidas continua ajustável nos assets das classes, inimigos, armas e ondas.

## Resultado da verificação

`Stage6Validation.txt`: PASS em Play Mode na Unity 6000.3.26f1. As quatro classes completaram a sequência configurada de ondas e atravessaram o portão com CharacterController. Foram conferidos os botões de pausa, retomada, reinício após derrota/vitória e retorno à seleção; também bloqueio de entradas, nova tentativa de spawn, distância do jogador, fila pendente, colisões, NavMesh, cancelamento da geração e morte durante esquiva. Imagens da pausa, onda final, Game Over e vitória foram geradas na Unity e revisadas.

Mensagens dos serviços Unity AI Assistant/entitlements já instalados podem continuar no Console; esses serviços não são usados pela partida. Nenhum erro de compilação C# foi encontrado na versão validada. O teste automático não avalia se as cinco ondas estão equilibradas para jogadores humanos; todos os valores permanecem ajustáveis nos assets.

As regressões das etapas 2, 3, 4 e 5 também passaram: movimentação/colisão/câmera, armas/defesa/projéteis, navegação/combate/morte de inimigos e as quatro habilidades de classe. Os relatórios correspondentes foram atualizados. O preparo dos testes de classes aguarda a inicialização dos atores de treino reativados antes de medir o empurrão do Colosso.
