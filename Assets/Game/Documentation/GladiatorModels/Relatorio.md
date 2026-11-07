# Bellator — reconstrução dos gladiadores

Entrega de 30/10/2026, na Unity 6000.3.26f1, com materiais URP/Lit. As malhas foram construídas e gravadas como assets da Unity. A geração acontece somente no editor; o jogo carrega os modelos prontos.

## Personagens e identidade

| Personagem | Alterações visuais | Equipamento funcional preservado |
|---|---|---|
| Sobrevivente | Corpo estreito, rosto magro, cabelo curto, cicatriz, tira de couro ajustada ao peito, linho com barra irregular e remendo costurado, antebraços enfaixados e sandálias | Gládio, sem escudo; Passos Desesperados |
| Colosso | Anatomia robusta, barba, capacete com crista longitudinal e proteções de rosto, ombreiras, manica segmentada, proteção metálica do tronco, saia de couro e duas grevas | Espada pesada e escudo; Golpe Devastador |
| Guerreiro | Corpo atlético, barba e cicatriz, proteção assimétrica do braço, cinturão, tiras cruzando o tronco, tecido azul, saia segmentada e greva | Gládio e escudo; Postura de Combate |
| Caçador | Corpo leve, cabelo preso atrás, colete e tiras de couro, manto curto verde, bolsas com abas, aljava com virotes e proteção reduzida | Besta, sem escudo; Disparo Preciso |
| Gladiador comum | Paleta própria, capacete aberto com proteções laterais, equipamento assimétrico, tecido vermelho e greva | Espada, escudo, IA e parâmetros originais |
| Gladiador pesado | Silhueta larga, ferro escuro, tronco e braços segmentados, capacete, barba e duas grevas | Espada pesada, IA e parâmetros originais |
| Arqueiro | Paleta oliva, barba, cabelo preso, couro, aljava, bolsas, manto e proteção leve | Arco, projéteis, recuo e IA originais |

O jogador de reserva herda a nova arte do Guerreiro pelo prefab já existente. Os quatro prefabs de seleção, quatro visuais animados de gameplay e quatro prefabs de prévia de animação foram atualizados. Os alvos de treino foram preservados.

## Geometria e materiais

- Troncos e membros com seções anatômicas variáveis, cintura, volume peitoral, clavículas, deltoides, antebraços, panturrilhas e transições contínuas nas articulações.
- Cabeças com mandíbula e bochechas modeladas; olhos, pálpebras, sobrancelhas, nariz, narinas, boca, orelhas, cabelos, barbas e cicatrizes em geometria.
- Palmas, dedos curvados, polegares e pés com dedos e tiras das sandálias.
- Roupas com dobras e barras irregulares; placas, rebites, cintos, bolsas e peças com espessura própria.
- Cinco armas reconstruídas: gládio, espada pesada, escudo oval com aro e rebites, arco curvo e besta com coronha, trilho, corda, gatilho e estribo.
- Vinte materiais compartilhados; seis conjuntos de texturas procedurais de 256 × 256, cada um com albedo e normal map, mipmaps e repetição. Couro, metal, madeira, linho, cabelo e pele têm acabamentos diferentes.
- Metal com pequenas marcas, couro com granulação e tecido com trama. Brilho controlado para manter o acabamento estilizado.

## Integração e preservação

Os nomes e caminhos dos ossos, pivôs e encaixes foram mantidos. As novas malhas usam SkinnedMeshRenderer com até duas influências por vértice. Os Animator Controllers, clips, root motion, curvas e referências de armas permanecem os originais. Não foram adicionados scripts de gameplay.

A comparação com a cópia de segurança verificou 2.188 transformações e componentes existentes, incluindo scripts, Animator, colliders e navegação. Foram comparados por SHA256 107 arquivos de definições, animações e controllers, além de 206 arquivos de scripts originais e seus metadados. Os filhos novos de arte são a única exclusão da comparação de transformações.

Vida, dano, stamina, habilidades, ataque, defesa, Aim Lock, seleção, IA, modos, pontuação e ranking continuam usando os sistemas existentes. Encaixes e pontos de disparo permanecem nas mesmas posições. Não foram adicionados colliders às novas malhas.

Os originais e as alterações de interface que já existiam antes desta tarefa estão preservados em `BalanceBackups/Models-20261030/Assets/Game/`. Essa pasta não é importada como conteúdo do jogo. `GitStatusBefore.txt` registra o estado inicial.

## Otimização

Cada personagem tem dois LODs com um único renderer por nível e seis slots de materiais. Apenas um LOD é exibido por vez. As peças pequenas são combinadas nas malhas, em vez de receberem um objeto renderizado separado por detalhe. As armas também usam malhas combinadas.

| Perfil | Triângulos LOD0 | Triângulos LOD1 |
|---|---:|---:|
| Sobrevivente | 11.456 | 6.316 |
| Colosso | 17.968 | 8.696 |
| Guerreiro | 14.272 | 6.888 |
| Caçador | 14.454 | 7.338 |
| Gladiador comum | 14.520 | 7.520 |
| Gladiador pesado | 18.328 | 8.664 |
| Arqueiro | 14.646 | 7.398 |

Os valores excluem armas. Os limites de culling dos visuais animados abrangem rolamento e morte. As prévias de seleção usam limites ajustados à malha para preservar o enquadramento automático.

## Testes realizados

As suites foram executadas de verdade na Unity instalada, numa cópia temporária do projeto com os mesmos assets e pacotes. Os assets entregues foram conferidos por hash após a transferência para o projeto principal.

| Evidência | Resultado | O que foi verificado |
|---|---|---|
| `BuildValidation.txt` | PASS | Malhas e pesos válidos, dois LODs, referências de equipamento e caminhos originais das curvas de animação |
| `PreservationValidation.txt` | PASS | 2.188 transformações/componentes preservados; 107 arquivos de definições/animações/controllers e 206 arquivos de scripts/metadados idênticos |
| `SkinningValidation.txt` | PASS | 504 poses de animação amostradas nos dois LODs, totalizando 1.008 bakes de skinning, com vértices finitos e limites estáveis |
| `PlayerPlayMode.txt` | PASS | Quatro classes na arena real: Idle, Walk, Run, movimento diagonal e relativo à câmera, Aim Lock, strafing, ataques, janelas de contato, defesa, hit, habilidades, stamina, invulnerabilidade, morte e reinício; rolamentos em oito direções com e sem alvo; Quicksteps secundários; carregamento dos modos História e Infinito |
| `ExpansionPlayMode.txt` | PASS | Corrida/esquiva em oito direções, paredes, colisão da câmera, portais, troca de alvo, perseguição, recuo do arqueiro e 15 agentes simultâneos |
| `BalancePlayMode.txt` | PASS | Vida, stamina, dano, defesa, custos, cooldowns, esquivas, habilidades, recarga e acertos físicos das quatro classes; 12 encontros curtos reais contra gladiador comum, pesado e arqueiro |
| `ArtworkPlayMode.txt` | PASS | Quatro prévias 3D, troca e confirmação de seleção, retorno ao menu, entrada em História e Infinito, fluxo local de conta/convidado, pontuação e ranking; resoluções 1280 × 720, 1920 × 1080 e 2560 × 1440; botões verificados por raycasts do EventSystem |
| `DeliveryValidation.txt` | PASS | Assets transferidos para o projeto principal correspondem aos arquivos validados |

O teste de integração de jogador mais antigo recebeu, pela nova ferramenta de validação, a confirmação da tela atual de seleção através do seu handler público; as assertions de combate e movimento foram mantidas. A suite atual de interface verificou separadamente os botões e a navegação reais.

As renderizações `Before.png` e `After.png`, as quatro folhas de poses `*Arena.png`, as quatro imagens `Selection*.png` e `ArenaStress.png` documentam o resultado. `AITrials.txt` contém as medições dos encontros curtos; não são partidas completas nem taxas de vitória.

Uma primeira execução de balanceamento concluiu suas verificações de jogo, mas foi marcada como falha por uma exceção de `UnityEditor.Search.SearchDatabase`. A configuração de busca ausente na cópia temporária foi preenchida e a indexação na inicialização foi desativada **somente nessa cópia**; a repetição completa passou. Essa opção pertence às [preferências de busca da Unity](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/editor-settings-reference/preferences/search). `EditorIndexDiagnostic.txt` preserva o diagnóstico. A preferência do projeto principal não foi alterada.

## Limitações

- A direção é estilizada, com malhas paramétricas e detalhes procedurais. Não foram usados modelos externos, escultura no Blender ou texturas fotográficas. Pele e tecido usam microtexturas repetíveis, sem um atlas pintado individualmente para cada corpo.
- As animações e a movimentação originais foram mantidas. Os inimigos continuam usando o rig simplificado e o animador procedural existentes. Não foram acrescentados IK, animação facial, simulação de tecido ou novas animações de mãos e dedos.
- As poses foram amostradas e o combate foi testado; não houve revisão manual de todos os instantes possíveis de contato entre arma, tecido e corpo.
- Não há hardware-alvo formal definido. A amostra com 15 agentes foi executada no editor em modo automático e não comprova 60 FPS de uma versão compilada com renderização contínua. Um benchmark prolongado em build permanece pendente.
- O teste legado Stage 4 espera uma barricada em sua antiga posição no centro da arena e falhou nessa expectativa. `ArenaExpansionTools.cs` já deslocava esses objetos para a periferia e removia seus colliders antes desta tarefa. A validação da arena expandida é a referência para a navegação atual. O resultado legado foi mantido como diagnóstico.

## Arquivos e manutenção

- `Assets/Game/Art/Gladiators/`: 27 malhas, materiais e texturas.
- `Assets/Game/Scripts/Editor/GladiatorMeshWorkshop.cs`: construção de superfícies e buffers de malha.
- `GladiatorArtGeometry.cs` e `GladiatorWeaponGeometry.cs`: anatomia, identidade e equipamentos.
- `GladiatorModelUpgrade.cs`: atualização dos prefabs, prévias, renderizações e validação de referências.
- `GladiatorModelValidation.cs`: testes de skinning e integração com as suites existentes.

O comando **Bellator → Models → Rebuild gladiator artwork** regenera os assets sem trocar seus GUIDs. **Validate baked skin and animation poses** verifica as poses gravadas. Reexecutar construtores de etapas antigas pode recriar a arte anterior; use o reconstrutor de modelos por último nesse fluxo.

`Before.png` e `After.png` mostram, da esquerda para a direita, Sobrevivente, Colosso, Guerreiro e Caçador, renderizados pela Unity. As capturas de arena e seleção são produzidas durante Play Mode.
