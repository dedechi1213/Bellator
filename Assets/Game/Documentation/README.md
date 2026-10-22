## Estado atual — etapa 6

Inicie por `Assets/Game/Scenes/GladiatorSelection.unity`. Escolha uma classe, confirme e inicie uma partida de sobrevivência com cinco ondas. Derrote todos, aguarde a abertura do portão principal e atravesse-o para conquistar a liberdade. Esc/P abre a pausa; os menus permitem reiniciar ou voltar à seleção. Consulte `Stage6.md` e `Stage6Validation.txt`. As seções abaixo registram a construção original da etapa 1; etapas 2–6 possuem documentação e relatórios próprios.
# Bellator — Etapa 1

> Projeto atualizado para a etapa 4: veja Stage4.md para os três inimigos, IA, navegação e facções.

> Projeto atualizado para a etapa 3: veja Stage3.md para armas, combate e controles da área de treino.

> Projeto atualizado para a etapa 2: veja Stage2.md para os controles do personagem e da câmera. Este documento descreve a construção original da arena.

Cena principal desta etapa: `Assets/Game/Scenes/RomanArena.unity`.
Abra essa cena diretamente no painel Project. A SampleScene e a lista original de Build Settings são preservadas. Para uma futura build, adicione RomanArena ao perfil de build desejado.

## Estrutura
- Scripts/Arena: MainGateController e ArenaSpawnPoint (somente metadados, sem personagens).
- Scripts/Editor: ArenaBuilder, gerador de edição protegido contra sobrescrita da cena existente.
- Prefabs/RomanArena.prefab: conjunto modular de primitivas, incluindo hierarquia e navegação.
- Materials: areia, travertino, calcário, pedra escura, madeira, ferro e tecido vermelho.
- Scenes/RomanArena.unity: arena persistente e câmera de apresentação.
- Navigation/RomanArenaNavMesh.asset: navegação preparada com AI Navigation já instalado.
- UI e ScriptableObjects: pastas reservadas para próximas etapas.
- Documentation: instruções, relatório de validação e imagem da cena.

## Hierarquia e escala
Arena contém Ground, Walls, Stands, Gates, Obstacles, Lighting e SpawnPoints. A câmera de apresentação fica na raiz da cena. A escala é de aproximadamente um metro por unidade. O piso útil tem diâmetro de 48,5 m; o conjunto externo ocupa aproximadamente 73 m. São sete níveis de assentos e quatro corredores de inimigos. O quinto acesso é o portão principal.

Somente cubos, cilindros e esferas nativos da Unity compõem a geometria. Não há modelos ou texturas externos. Colliders sólidos protegem o piso, paredes, pilares, obstáculos e portão; detalhes como faixas metálicas e estandartes não precisam de colliders. Os corredores possuem contenção provisória no fundo, que deverá ser substituída quando houver ambientes externos.

## Substituir os visuais
Obstáculos possuem um pai de posicionamento e filhos Visuals_Replaceable. Troque os filhos visuais por modelos definitivos, preservando os pais, posições e componentes necessários. Nos portões preserve MainExitGate, MovingGate e a referência movingGate do controlador. Reavalie os colliders e refaça o bake após alterações de geometria.

## Portão
MainGateController começa fechado. Open() e Close() controlam o deslocamento vertical de MovingGate, sem depender de combate ou vitória. O collider acompanha a peça. NavMeshObstacle com carving acompanha a passagem móvel; a peça é ignorada no bake estático. IsOpen só se torna verdadeiro quando a abertura termina. Nenhuma entrada de teclado foi associada nesta etapa.

## Navegação
NavMeshSurface está em Arena, coleta os colliders dos filhos e usa voxel de 0,15 m. Arquibancadas e paredes recebem área Not Walkable. A fundação é ignorada no bake para preservar o piso navegável superior. O solo, corredores e os desvios dos obstáculos compõem a superfície. Use o botão Bake do NavMeshSurface após mudanças; salve também a cena/prefab. Ao introduzir inimigos, ajuste raio, altura, degrau e inclinação do agente e refaça o bake. SpawnPoints possui um PlayerSpawn e quatro EnemySpawn, voltados ao centro, com gizmos coloridos visíveis no editor. Nenhum sistema de spawn é executado.

## Configuração preservada
Unity 6000.3.26f1; URP 17.3.0; Input System 1.20.0 (activeInputHandler = 1); AI Navigation 2.0.15. Nenhum pacote foi instalado. Arquivos existentes e configurações de Input, renderização e build não foram alterados.

## Escopo
Não contém jogador, inimigos, combate, vitória ou UI de jogo. A câmera fixa serve para apresentação do cenário. O cenário é um blockout detalhado e deverá passar por otimização e testes com o controlador de personagem quando ele for introduzido.


## Verificação realizada nesta etapa
Compilação real na Unity 6000.3.26f1 concluída sem erros de C#. Foram verificados cinco spawns sobre o NavMesh, rotas completas entre eles e ausência de colisores no volume de nascimento. A imagem ArenaPreview.png foi renderizada na Unity e inspecionada. O piso e demais cilindros usam MeshCollider da própria malha primitiva, corrigindo a incompatibilidade do CapsuleCollider padrão com cilindros achatados.

Os registros da Unity também apresentam mensagens de autorização/entitlements e exceções de serviços do pacote Unity AI Assistant já instalado. Esses serviços não são usados pela arena. As primeiras tentativas do bake produziram uma falha de validação; o problema de colisores foi corrigido e a validação final passou. O histórico dessas tentativas permanece no Console/log. Não foi realizado teste com personagem ou inimigo, pois esses sistemas estão fora desta etapa.

A iluminação possui sol com sombras suaves. A distância de sombras continua obedecendo aos perfis URP existentes (50 m), preservados; a câmera geral distante pode não mostrar as sombras dos objetos mais afastados. Para conferi-las, aproxime a Scene View. O menu Bellator > Validate and Preview Open Arena permite repetir as verificações e gerar novamente a imagem de apresentação. Esse comando salva a arena aberta e atualiza seu prefab; use-o depois de revisar alterações locais.





