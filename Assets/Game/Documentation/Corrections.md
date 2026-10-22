# Correções — equipamento fixo, HUD e seleção

Projeto preservado na Unity **6000.3.26f1**. Este documento substitui os comportamentos de troca de arma, painel de habilidade e início pela seleção descritos nos relatórios históricos das etapas 5 e 7.

## Comportamento atual

**Menu principal → Selecionar personagem → Confirmar seleção → Menu principal → Jogar → Arena.**

A seleção apresenta os quatro modelos 3D e suas informações. A confirmação salva a referência da classe durante a sessão e carrega apenas o menu principal. Reabrir a seleção restaura a escolha anterior. Não existe botão nem método de início de partida nesse controlador. Jogar carrega a arena diretamente, usando o Guerreiro quando ainda não existe escolha. Reiniciar pela pausa ou pela derrota continua disponível.

| Classe | Equipamento fixo | Defesa com escudo |
| --- | --- | --- |
| Sobrevivente | Espada curta (Gládio) | Não |
| Colosso | Espada pesada + escudo | Sim |
| Guerreiro | Espada (Gládio) + escudo | Sim |
| Caçador | Besta | Não |

As ações numéricas de equipamento foram removidas do Input System do combate, assim como o processamento de troca em PlayerCombat. Cada classe configura um único equipamento bloqueado: chamadas de outro slot são recusadas e chamadas de alternância de escudo não afetam o equipamento fixo. A montagem de armas, dano, defesa, disparos e projéteis continua usando os sistemas existentes. Os assets de armas gerais, incluindo o arco, são preservados para expansão e testes de treino.

O HUD não cria painel, texto, custo, temporizador ou barra de habilidade especial. O HUD de treino também não apresenta essas informações. **Q continua ativando a habilidade**, com as regras, resistência, recarga, efeitos e restrições anteriores. A tecla Q permanece na linha discreta de controles.

As instruções mostram ataque pesado somente para armas corpo a corpo, defesa somente para classes com escudo e recarga somente para a besta. Não existem instruções de troca de armas ou de escudo. O painel de munição e a mira aparecem somente com armas à distância. Vida, resistência, ondas, inimigos, pausa, telas finais e configurações permanecem integrados.

## Arquivos alterados

Todos os caminhos abaixo são relativos à pasta do projeto.

| Arquivo | Alteração |
| --- | --- |
| `Assets/Game/Scripts/Combat/CombatInput.cs` | Remove ações e bindings de slots 1–4 e alternância de escudo em 5. |
| `Assets/Game/Scripts/Combat/PlayerCombat.cs` | Remove processamento de troca durante o combate. |
| `Assets/Game/Scripts/Combat/WeaponEquipment.cs` | Adiciona inicialização de equipamento fixo e bloqueio de slots/alternância. Preserva a infraestrutura geral e o modo de treino. |
| `Assets/Game/Scripts/Combat/CombatHUD.cs` | Remove apresentação de habilidade e instruções antigas; fornece controles adequados ao equipamento. |
| `Assets/Game/Scripts/Classes/PlayerClassController.cs` | Monta e bloqueia o equipamento da classe na inicialização. |
| `Assets/Game/Scripts/Classes/GladiatorSelectionController.cs` | Confirma e retorna ao menu; restaura seleção anterior; remove início direto de partida. |
| `Assets/Game/Scripts/Match/GameInterface.cs` | Jogar carrega arena diretamente; remove painel de habilidade; controles por classe e munição apenas quando aplicável. |
| `Assets/Game/Scenes/GladiatorSelection.unity` | Remove Iniciar Partida, amplia Confirmar Seleção e atualiza instruções. |
| `Assets/Game/ScriptableObjects/Classes/Hunter.asset` | Remove espada secundária e atualiza a descrição do Caçador. |
| `Assets/Game/ScriptableObjects/Classes/Survivor.asset`, `Colossus.asset`, `Warrior.asset` | Definições verificadas e salvas com um equipamento principal; atributos e habilidades preservados. |
| `Assets/Game/ScriptableObjects/Classes/GladiatorClasses.asset` | Mantém Guerreiro como padrão; o instalador também verifica um único equipamento nas quatro definições. |
| `Assets/Game/Scripts/Editor/ClassStageBuilder.cs` | Atualiza geração futura de classes/seleção para o novo funcionamento. |
| `Assets/Game/Scripts/Editor/CorrectionsBuilder.cs` | Aplica os ajustes nos assets/cena existentes sem reconstruir a arena. |
| `Assets/Game/Scripts/Editor/CorrectionsValidation.cs` | Testa o fluxo, as quatro classes, teclas 1–5/Q/E, UI e integração. |
| `Assets/Game/Scripts/Editor/InterfaceStageValidation.cs` | Encaminha a antiga opção de teste da etapa 7 para a validação atual. |
| `Assets/Game/Scripts/Editor/ClassStageValidation.cs` | Adapta os testes de dano/habilidades ao fluxo de menu e à besta fixa. |
| `Assets/Game/Scripts/Editor/MatchStageValidation.cs` | Adapta a entrada nas partidas ao fluxo confirmar → menu → Jogar. |

Também foram atualizados este relatório, a documentação da etapa 7, relatórios de validação e capturas de tela geradas pelos testes. Arquivos `.meta` correspondentes são gerados pela Unity.

## Repetir os testes

- **Bellator > Test Fixed Equipment and Selection Corrections:** testa Jogar com Guerreiro padrão, as quatro seleções, retorno automático ao menu, retenção da escolha, botões sem obstrução de raycast, equipamento/escudo fixos e entradas reais pelo Input System para 1–5, Q, E e Esc. Confere efeitos e resistência da habilidade, restrições de recarga, ausência do painel, controles por classe, HUD, pausa, configurações, derrota e reinício. Relatório: `CorrectionsValidation.txt`.
- **Bellator > Test Stage 5 Classes in Play Mode:** testa os efeitos das quatro habilidades, dano compartilhado, projétil da besta, consumo de resistência, defesa, recargas e cancelamento na morte, com o novo fluxo de seleção/menu. Relatório: `Stage5Validation.txt`.
- **Bellator > Test Stage 6 Complete Matches in Play Mode:** verifica as cinco ondas, inimigos, spawns, NavMesh, pausa, derrota, vitória pelo cruzamento físico do portão, reinícios e menus nas quatro classes. Relatório: `Stage6Validation.txt`.

Os testes de integridade aceleram a eliminação de inimigos pelo sistema de dano e não avaliam balanceamento de uma partida humana. Configurações e modo de tela não foram modificados por estas correções. Nenhuma próxima etapa foi iniciada.

## Resultados executados em 21/10/2026

Todos os testes abaixo foram executados no Editor da Unity **6000.3.26f1**, em Play Mode, após as alterações.

| Verificação solicitada | Resultado |
| --- | --- |
| 1. Equipamentos | **PASS** — quatro equipamentos corretos; eventos de teclas 1–5 não provocam troca, reconstrução da arma ou alternância do escudo. Slots adicionais recusados. |
| 2. Habilidades | **PASS** — Q foi enviado pelo Input System nas quatro classes; consumo de resistência, efeitos, recarga e restrições preservados. Testes adicionais validaram dano e defesa. |
| 3. Seleção | **PASS** — confirmação retorna ao menu sem GameManager, WaveManager ou jogador de arena; reabertura mantém o cartão escolhido. Botão Iniciar Partida ausente. |
| 4. Jogar | **PASS** — inicia Guerreiro sem seleção anterior e carrega corretamente cada classe confirmada; atributos e equipamento aplicados antes do combate. |
| 5. Interface | **PASS** — painel da habilidade ausente, sem comandos de troca; instruções e munição específicas por classe; barras de vida/resistência atualizadas. Capturas da seleção e das quatro classes inspecionadas. |
| 6. Integridade | **PASS** — cinco ondas nas quatro classes; spawns, NavMesh, dano, habilidades, pausa, configurações durante pausa, derrota, reinícios, portão, travessia física para vitória e retorno ao menu. |

Relatórios completos: **CorrectionsValidation.txt**, **Stage5Validation.txt** e **Stage6Validation.txt**, todos com **RESULT: PASS**. Não foram encontrados erros de compilação C# nem exceções do jogo capturadas nesses testes. As mensagens já existentes de serviços Unity AI/entitlements são independentes da partida. Os testes solicitados foram executados no Editor; não há itens desses seis testes pendentes por impossibilidade de execução.
