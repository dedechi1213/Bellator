# BELLATOR — expansão da arena existente

Implementação e testes em 25/10/2026, Unity **6000.3.26f1**, mantendo **URP**. A cena RomanArena e o prefab original foram modificados diretamente, preservando seus GUIDs e as referências dos sistemas de jogo. Nenhum modelo externo foi utilizado.

## 1. Dimensões anteriores e novas

| Medida | Antes | Depois |
|---|---:|---:|
| Formato do campo | Circular | Elíptico |
| Piso geométrico | 48,5 × 48,5 m | 65 × 59 m |
| Campo útil aproximado, até a face interna do podium | 48 m de diâmetro | 63,5 × 57,5 m |
| Área útil aproximada | 1.810 m² | 2.868 m² |
| Fileiras de assentos | 7 | 15, em 3 níveis |
| Altura superior da nova fachada | — | 28,45 m |

O ganho de área útil é de aproximadamente **58,5%**. As dimensões úteis são aproximações: paredes e piso são segmentados, e os corredores dos portões são extensões do campo central. O transform geral Arena permanece com escala unitária. Os elementos foram reposicionados e reconstruídos individualmente.

## 2. Estruturas arquitetônicas

- 24 setores organizados na curvatura elíptica.
- 72 vãos de arcos na fachada, em três alturas, com profundidade e pedras geométricas.
- 72 colunas na fachada, além das 10 colunas adicionadas às cinco entradas; bases e capitéis.
- Cornijas, podium de pedra com juntas, bases escuras e parapeito superior.
- Os segmentos originais das paredes internas foram reaproveitados e ajustados às tangentes da elipse.

## 3. Arquibancadas

Três níveis com cinco fileiras cada, escadas radiais entre setores, corredores de separação e parapeitos. As fileiras acompanham a elipse e a altura aumenta progressivamente.

O público contém **2.103 silhuetas geométricas**, com distribuição irregular e quatro materiais discretos. Elas estão combinadas em **96 malhas**, organizadas em 24 grupos de distância; não existem 2.103 personagens ou scripts ativos. As cabeças usam geometria de oito faces. O público não possui colisores nem lança sombras.

## 4. Piso

O SandFloor original foi ampliado e recebeu um material exclusivo com textura de areia gerada localmente: variações terrosas, granulação e desgaste. A textura repete por UV; não há relevos físicos ou obstáculos pequenos no piso.

A superfície de colisão continua plana. Os seis conjuntos de obstáculos originais foram reaproveitados como ornamentos em galerias periféricas elevadas, com os colisores decorativos removidos. O centro ficou livre para deslocamento, rolagens e combate à distância.

## 5. Portões

Os cinco frames existentes foram preservados e reposicionados na nova borda. Foram adicionados arcos maiores, colunas laterais, estandartes, molduras, complementos de pedra nas laterais do podium e detalhes de tábuas no portão móvel.

O MainGateController, o MovingGate, seu colisor, o NavMeshObstacle com carving e os valores de abertura continuam os existentes. A zona MainExitCrossing e seu sinalizador foram reposicionados junto ao portão. Os corredores mantêm chão, laterais e limites posteriores sólidos.

**História:** as cinco ondas originais e a abertura seguida de travessia física foram testadas com as quatro classes. **Infinito:** o portão permaneceu fechado durante a progressão testada.

## 6. Iluminação

O sol existente recebeu intensidade 1,45, cor quente moderada, rotação 46° / −34° e sombras suaves com força 0,8. A iluminação ambiente mantém tons frios no céu e quentes nas regiões baixas para preservar leitura dos combatentes. Não foram adicionados bloom, neblina, luzes pontuais ou um novo pipeline. Os assets globais de qualidade/renderização foram preservados.

## 7. Decoração

22 estandartes em vermelho escuro e bronze, dois guardiões romanos geométricos elevados, escudos e lanças das estátuas, pedra aparente e os antigos obstáculos reaproveitados como ornamentos. A decoração permanece fora do espaço central de combate.

## 8. Spawns

Os cinco componentes ArenaSpawnPoint e as referências serializadas do WaveManager foram preservados.

| Ponto | Antes (x, y, z) m | Depois (x, y, z) m |
|---|---|---|
| PlayerSpawn | (0; 0,08; −12) | (0; 0,08; −13) |
| EnemySpawn_45 | (15,63; 0,08; 15,63) | (20,51; 0,08; 18,38) |
| EnemySpawn_135 | (15,63; 0,08; −15,63) | (20,51; 0,08; −18,38) |
| EnemySpawn_225 | (−15,63; 0,08; −15,63) | (−20,51; 0,08; −18,38) |
| EnemySpawn_315 | (−15,63; 0,08; 15,63) | (−20,51; 0,08; 18,38) |

Não foi necessário aumentar a quantidade de entradas. Foram preservadas a alternância entre entradas, a busca por espaço livre, a distância mínima do jogador e a repetição de tentativas quando uma entrada está bloqueada. Não houve mudança no número ou composição das ondas.

## 9. Navegação

O NavMesh foi reconstruído com os novos colisores, mantendo o asset existente e seu GUID. A superfície continua coletando colisores dos filhos de Arena com voxel de 0,15 m. Resultado: 147 triângulos de navegação.

Foram verificados os cinco spawns, seus espaços livres e caminhos completos, além de **628 amostras do campo em uma grade de 2 m**. Também passaram 180 sondagens radiais dos limites físicos. A perseguição e o recuo dos arqueiros foram executados em Play Mode. Nenhum script de navegação ou inteligência artificial foi modificado.

## 10. Otimizações e desempenho

| Métrica da hierarquia Arena | Antes | Depois |
|---|---:|---:|
| Renderers | 990 | 835 |
| Colisores | 908 | 244 |
| Geometria visível, vértices somados | não medida | 202.369 |

A arquitetura foi combinada por material e setor durante a edição. Os materiais são compartilhados, possuem instancing habilitado, e os objetos fixos são marcados para batching estático. As peças do portão móvel permanecem fora do batching estático. A decoração não possui scripts Update. O público usa descarte por distância através de LODGroup, sem sombras e sem colisão.

A simplificação das cabeças e dos índices das malhas reduziu os novos assets de geometria/materiais de cerca de 180 MB durante a implementação para **25,2 MB em disco**. Isso não representa memória total do jogo.

**Amostra final no editor**, com 15 inimigos simultâneos e captura a 1920 × 1080: 127 frames em 4,00 s; média de **31,7 FPS**; pior frame **36,2 ms**; **433,9 MiB alocados** e **1.225,3 MiB reservados** pela Unity. A amostra inicial antes de simplificar o público foi de 25,3 FPS e 562,9 MiB alocados. São amostras curtas em uma sessão de editor; não constituem comparação controlada com a arena anterior ou garantia de desempenho em outros computadores.

## 11. Arquivos modificados e adicionados

Modificados:

- `Assets/Game/Scenes/RomanArena.unity` — iluminação e posicionamento de referências da cena.
- `Assets/Game/Prefabs/RomanArena.prefab` — arena original ampliada, arquitetura e decoração persistentes.
- `Assets/Game/Navigation/RomanArenaNavMesh.asset` — navegação reconstruída.
- `Assets/Game/Scripts/Editor/SoulslikeValidation.cs` — testes antigos agora descobrem a parede real por colisão, substituindo coordenadas fixas da arena menor.

Adicionados:

- `Assets/Game/ArenaExpansion/Materials/` — materiais e duas texturas geradas localmente.
- `Assets/Game/ArenaExpansion/Geometry/` — malhas combinadas persistentes.
- `Assets/Game/Scripts/Editor/ArenaExpansionTools.cs` — auditoria, migração, verificação geométrica e capturas, exclusivamente no editor.
- `Assets/Game/Scripts/Editor/ArenaExpansionValidation.cs` — verificações físicas e amostra de desempenho em Play Mode.
- `Assets/Game/Documentation/ArenaExpansion/` — este relatório, resultados e capturas reais.

Os scripts e assets de personagens, armas, animações, câmera, Aim Lock, combate, contas, ranking e progressão não foram alterados. Não foram criados novos modos, inimigos ou componentes de rede.

## 12. Testes executados

Todos os resultados abaixo foram obtidos executando a Unity instalada, sem simular resultados:

1. **GeometryValidation.txt — PASS:** cinco spawns com caminhos e folga; 628 amostras planas e conectadas; 180 sondagens de limites; ausência de scripts faltantes.
2. **PlayModeValidation.txt — PASS:** corrida física em oito direções, rolamentos laterais e recuperação; corrida/rolamento contra oito regiões das paredes; retração de câmera; órbita junto aos cinco portais; câmera livre; aquisição e troca de Aim Lock; movimento e rolamento com alvo travado; perseguição por IA; recuo do arqueiro; 15 agentes simultâneos e amostra de desempenho.
3. **CombatRegression.txt — PASS:** comandos reais do Input System; movimento em quatro direções; câmera livre e colisão; seleção/troca/perda de alvo; velocidade, sprint, stamina, rolagem e habilidade das quatro classes; combo de três ataques leves; ataque pesado; defesa frontal/traseira; quebra de guarda; postura; múltiplos atacantes; projétil físico da besta atingindo o alvo selecionado; recarga; pausa; morte e reinício.
4. **StoryRegression.txt — PASS:** quatro classes, composições exatas das cinco ondas, intervalos, spawn bloqueado e nova tentativa, derrota/reinício, abertura do portão e vitória somente após cruzá-lo com CharacterController.
5. **InfiniteRegression.txt — PASS:** progressão até a onda 35 com uma classe e duas ondas com cada uma das outras três; spawn, navegação, limite de inimigos, pontuação, escala de dificuldade, portão fechado, pausa, derrota, reinício e transição de volta ao História. Os fixtures de conta e ranking foram isolados em Temp.

## 13. Limitações e pendências

- **Ainda falta um benchmark de uma versão compilada**, prolongado e em hardware-alvo. A amostra de 31,7 FPS no editor não comprova uma meta de 60 FPS. Memória reservada inclui o editor e caches; não foi realizado teste longo de vazamentos.
- A captura direta da janela pelo controle do computador falhou. As imagens foram produzidas de verdade pelo renderizador da Unity e por ScreenCapture durante Play Mode; não são imagens conceituais.
- As novas arquibancadas e galerias são cenário. Elas não foram transformadas em áreas jogáveis; o combate permanece no piso e nos corredores originais.
- Não foram implementados multiplayer, occlusion bake ou lightmap bake. Não são necessários para os testes funcionais executados; a eficiência em build deve orientar qualquer etapa adicional.
- Nenhuma falha funcional foi encontrada nas verificações executadas. Uma avaliação manual prolongada da composição e do ritmo de combate continua sendo útil.

## Capturas reais

- `Before.png` — arena original antes das alterações.
- `Overview.png` — conjunto ampliado visto de cima.
- `ArenaInterior.png` — visão interna próxima à altura do jogador, em edição.
- `ExitGate.png` — detalhe do portão e das arquibancadas, em edição.
- `Gameplay.png` — Play Mode, equipamento real e 15 agentes na amostra de estresse; o HUD está em preparação porque as ondas ficam sob controle do teste.
- `StoryFinalWave.png` e `StoryVictory.png` — última onda e vitória da regressão do História.
- `InfiniteHUD.png` — HUD do Infinito na arena ampliada.

As ferramentas ficam disponíveis em **Bellator → Arena Expansion**. A geometria é salva nos assets e no prefab; não depende de construção em tempo de execução.
