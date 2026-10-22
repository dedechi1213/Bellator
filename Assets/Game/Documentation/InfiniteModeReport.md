# BELLATOR — Modo Infinito e ranking local

Atualização posterior: o sistema de contas substituiu o nome livre por registro automático da conta; convidados continuam jogando, mas não registram resultados. Consulte `AccountsReport.md` para o fluxo atual. As descrições abaixo documentam a implementação original do Infinito.

Implementação em Unity 6000.3.26f1, em 21/10/2026.

## O que foi implementado

- **JOGAR → SELECIONAR MODO**, com História, Infinito, descrições e Voltar.
- Seleção de personagem continua apenas escolhendo o gladiador e retornando ao menu. Guerreiro permanece como padrão do catálogo existente.
- Modo História utiliza o mesmo `ArenaSurvival.asset`: cinco ondas, abertura do portão e vitória somente após atravessar a saída.
- Modo Infinito utiliza o mesmo `WaveManager`, as mesmas entradas validadas por NavMesh e os mesmos prefabs de inimigos. Não há condição de última onda ou chamada ao sistema de vitória.
- Vida, dano e frequência dos ataques aumentam por instância; os ScriptableObjects de inimigos e armas permanecem intactos.
- Contagem inicial e intervalo de cinco segundos. Limite inicial de **12 inimigos ativos**, com grupos sucessivos quando o total da onda excede esse limite.
- Pontos de 100/200/150 por comum/pesado/arqueiro; multiplicadores 1/1,2/1,5/2/2,5 por faixa de ondas; bônus separado de `500 + onda × 100`.
- Uma inscrição de morte por inimigo impede premiação repetida. Remover/desativar um inimigo vivo externamente não concede pontos; esse erro de configuração interrompe a geração e produz um diagnóstico.
- Estatísticas por partida: identificação única, nome, classe, pontos, maior onda iniciada, ondas concluídas, inimigos derrotados, sobrevivência e data UTC.
- Ranking local exclusivo do Infinito, com dez resultados. Critérios: pontos, ondas concluídas, tempo, data e identificação da partida.
- HUD com vida, resistência, onda, inimigos, pontos, relógio, recorde local e munição quando aplicável. A tecla Q e os equipamentos fixos existentes foram preservados.
- Tela final com estatísticas, novo recorde, nome opcional, registro, jogar novamente, ranking e menu. Navegar sem confirmar registra com o nome digitado ou “Gladiador”. Fechar o aplicativo na tela final também tenta registrar o resultado.

## Persistência e recuperação

O arquivo principal é `Path.Combine(Application.persistentDataPath, "BellatorInfiniteRanking.json")`. O Unity define a pasta conforme os campos Company Name e Product Name do projeto. O arquivo `.bak` contém uma cópia válida de recuperação; o `.tmp` é usado durante a gravação e substituição do principal.

Arquivo inexistente resulta em lista vazia. Erros de leitura, dados inválidos, arquivo corrompido e falhas de gravação são tratados sem impedir o início do jogo. A interface permite tentar registrar novamente. Caso o jogador saia após uma falha de gravação, não é possível garantir a conservação daquele resultado.

Nomes são limitados a 24 caracteres e removem caracteres de controle/marcação. A interface de ranking não interpreta texto do usuário como rich text. Identificações repetidas não criam duas linhas para uma mesma partida.

## Ajustar o balanceamento

Abra `Assets/Game/Scenes/RomanArena.unity`, selecione **MatchSystems** e expanda **Infinite Difficulty** no componente **WaveManager**.

| Configuração | Padrão |
|---|---:|
| Initial Countdown / Between Waves | 5 s / 5 s |
| Spawn Interval | 0,65 s |
| Maximum Active Enemies | 12 |
| Initial Enemies / Enemies Per Wave | 2 / 1 |
| Maximum Enemies Per Wave | 90 |
| Health Per Wave / Damage Per Wave | 0,08 / 0,035 |
| Heavy Frequency / Archer Frequency | 0,30 / 0,25 |
| Aggression Per Wave / Maximum Attack Rate | 0,015 / 1,6 |
| Minimum Attack Pause | 0,18 s |

O crescimento é linear até a onda 30 e logarítmico depois. O teto de quantidade **por onda** não limita o número de ondas. Atributos possuem limites de segurança para evitar valores extremos. Os ataques acelerados preservam tempos mínimos de preparação/ação. Frequência zero desativa o respectivo tipo especial.

## Estabilidade

- Listas de inimigos e inscrições em eventos são limpas ao terminar a onda/partida e ao descarregar a cena.
- No Infinito, derrotados são descartados durante a própria onda; a lista não acumula todos os inimigos da partida.
- A fila representa apenas os adversários da onda atual, com total configurável e limite padrão de 90.
- HUD reutiliza as barras de vida dos inimigos, em quantidade limitada pelo pico de atores simultâneos.
- Projéteis conservam os controles existentes de tempo de vida, alcance, impacto e cancelamento na morte do dono. Todos são cancelados ao encerrar a partida.
- A progressão usa atualização de estado, sem novas corrotinas de spawn. Reiniciar carrega uma cena nova com pontuação zero.
- Não foi introduzido pooling de inimigos: o limite de atores e a limpeza imediata já limitam objetos vivos. Pooling exigiria redefinir estados de vários componentes de combate; sua adoção futura deve ser baseada em perfil de desempenho.

## Arquivos criados

Scripts em `Assets/Game/Scripts/Match/`:

- `GameModeManager.cs`
- `DifficultyScaler.cs`
- `MatchStatistics.cs`
- `ScoreManager.cs`
- `LocalRankingManager.cs`
- `GameInterface.Infinite.cs`
- `RankingPersistenceProbe.cs` — verificação opcional de persistência, inativa normalmente e excluída de builds sem Development Build.

Scripts em `Assets/Game/Scripts/Editor/`:

- `InfiniteModeValidation.cs`
- `InfiniteModeBuildValidation.cs`

Este relatório e os novos arquivos `InfiniteModeValidation.txt`, `InfiniteBuildValidation.txt`, `InfiniteHUD.png`, `InfiniteResults.png` e `InfiniteRanking.png` ficam em `Assets/Game/Documentation/`. Os arquivos `.meta` correspondentes são gerados pelo Unity.

## Arquivos modificados

- `Assets/Game/Scripts/Match/GameManager.cs`
- `Assets/Game/Scripts/Match/WaveManager.cs`
- `Assets/Game/Scripts/Match/GameInterface.cs`
- `Assets/Game/Scripts/Enemies/EnemyBrain.cs`
- `Assets/Game/Scripts/Classes/ActorAttributes.cs`
- `Assets/Game/Scripts/Editor/MatchStageValidation.cs`
- `Assets/Game/Scripts/Editor/CorrectionsValidation.cs`
- `Assets/Game/Scripts/Editor/ClassStageValidation.cs`

As validações anteriores foram adaptadas para clicar em Modo História após Jogar. Os objetivos de combate, classes e vitória continuam os mesmos. Não foi necessário reconstruir as cenas nem modificar os assets do Modo História. Uma cópia dos scripts/cenas originais foi preservada em `Temp/BellatorInfiniteBackup` antes das alterações; a pasta Temp pode ser limpa pelo Unity.

Foram atualizadas as evidências existentes em `Assets/Game/Documentation/`: `Stage6Validation.txt`, `Stage6_FinalWave.png`, `Stage6_GameOver.png`, `Stage6_Pause.png`, `Stage6_Victory.png`, `CorrectionsValidation.txt`, `Corrections_Selection.png` e `Corrections_Survivor.png`, `Corrections_Colossus.png`, `Corrections_Warrior.png`, `Corrections_Hunter.png`.

## Testes realizados

| Verificação | Resultado executado |
|---|---|
| Compilação dos scripts no Unity 6000.3.26f1 | PASS |
| Build Windows 64 bits, Development Build | PASS: zero erros, 204.752.668 bytes, 1 min 34 s |
| História: cinco ondas, composições originais, portão e travessia física | PASS com as quatro classes |
| Infinito após quinta onda e após onda 31 | PASS: 35 ondas sucessivas |
| Dificuldade progressiva | PASS: atributos de instâncias em jogo e cálculos até `int.MaxValue` |
| Pontos, multiplicadores e ausência de duplicação | PASS: 660 inimigos e 256.225 pontos na simulação de 35 ondas; faixas 1–31 verificadas separadamente |
| Grupos sucessivos e limite configurável | PASS: limite reduzido a dois, sem ultrapassar o teto nem perder mortes |
| Ranking JSON | PASS: leitura nova, arquivo inexistente, corrompido, formato inválido, backup, falha de gravação, nomes, dez melhores e desempates |
| Classes e habilidades no Infinito | PASS: quatro equipamentos fixos corretos e ativação pela tecla Q |
| Reinício do Infinito | PASS: mesma classe/modo, primeira onda, zero pontos e contadores zerados |
| Troca Infinito → menu → História | PASS: cinco ondas, atributos básicos, pontuação zerada e ausência de ranking de História |
| Regressão de seleção, combate, munição, pausa e configurações | PASS com as quatro classes |
| Interface | PASS: botões verificados por raycast real de uGUI; imagens de HUD/resultados/ranking inspecionadas visualmente em 1920×1080 |
| Persistência entre processos do executável | PASS: primeiro processo salva e encerra com código 0; segundo processo recupera nome, pontos, ondas e tempo e encerra com código 0 |

Resultados completos: `InfiniteModeValidation.txt`, `Stage6Validation.txt` e `CorrectionsValidation.txt`, todos com `RESULT: PASS`.

O teste de persistência executou o player Windows compilado, em modo sem janela/gráficos, com arquivo de ranking isolado em `Temp/InfiniteProcessRanking/`. A gravação e a leitura ocorreram em dois processos distintos; não foi apenas uma recarga da classe em memória. Evidência consolidada: `InfiniteBuildValidation.txt`. O executável e seus arquivos de apoio foram gerados em `Temp/BellatorInfinitePlayer/`; a pasta Temp é temporária e não substitui uma distribuição final do jogo.

Os três arquivos de cena e o asset `ArenaSurvival.asset` também foram comparados com as cópias originais: conteúdo idêntico.

Na simulação acelerada, os objetos Transform vivos passaram de 1.100 para 1.103, incluindo a criação das barras de vida. A leitura diagnóstica de memória gerenciada do Editor passou de 1.059.381.248 para 1.062.129.664 bytes. São observações de uma execução curta, com os pacotes do Editor carregados; não demonstram ausência de vazamentos em partidas de várias horas.

Os testes novos usam arquivos isolados em Temp, sem inserir resultados fictícios no ranking do jogador. O tempo de sobrevivência exibido nas imagens corresponde ao tempo real da simulação acelerada, e não a uma partida normal de 35 ondas.

## Problemas encontrados e corrigidos

- O projeto não é um repositório Git; foi feita uma cópia dos arquivos originais antes das edições.
- A interface uGUI precisa de ciclos de atualização após criar ou reativar painéis. O teste inicial tentava clicar no mesmo ciclo; a validação passou a aguardar a atualização e verificar a área clicável real.
- As validações antigas esperavam que Jogar carregasse a arena diretamente. Foram adaptadas à escolha explícita de Modo História e à transição de saída do Play Mode.
- O pacote de AI Assistant do Unity emitiu erros próprios de consulta/licenciamento durante atualizações do Editor. Isso não impediu os três conjuntos de testes concluídos. Nenhum pacote foi removido ou reconfigurado.
- Não restaram falhas funcionais nos testes executados. O balanceamento com jogadores e um teste de várias horas com perfil de memória nativa/GPU continuam fora da verificação realizada.

## Como testar manualmente

1. Abra **MainMenu** e entre em Play Mode. Clique em Jogar: deve aparecer Selecionar Modo, com Voltar.
2. Escolha **Modo História**. Derrote as cinco ondas, aguarde a abertura do portão e atravesse a saída: a vitória deve aparecer somente após a travessia.
3. Volte ao menu, escolha um dos quatro gladiadores e confirme. Isso deve retornar ao menu, sem iniciar combate.
4. Clique em Jogar → **Modo Infinito**. Observe contagem inicial, pontuação zero, intervalo de cinco segundos e ondas além da quinta. O portão deve continuar fechado.
5. Use Q e verifique o equipamento fixo da classe. O HUD não deve apresentar painel de habilidade especial nem opções para trocar armas.
6. Morra, confira as estatísticas, digite um nome e registre. Abra Ver Ranking e confira a classificação. Voltar retorna aos resultados.
7. Clique em Jogar Novamente: mesma classe e Modo Infinito, primeira onda, zero pontos e contadores reiniciados.
8. Termine e registre outra partida. Feche/reabra o jogo e consulte Ranking no menu. O resultado deve persistir se houver permissão de gravação na pasta de dados do aplicativo.
9. Depois do Infinito, volte ao menu e escolha História. Confirme que os inimigos usam os atributos básicos, há cinco ondas e o portão abre ao final.

Para executar as validações automatizadas no Editor, use **Bellator → Test Infinite Mode and Local Ranking**, **Test Stage 6 Complete Matches in Play Mode** e **Test Fixed Equipment and Selection Corrections**. Os testes novos usam arquivos isolados em Temp e não adicionam resultados fictícios ao ranking pessoal.

## Limites de verificação

O teste acelerado cobre muitos ciclos de spawn, morte e limpeza, mas não substitui uma partida real de várias horas, perfil de memória nativa/GPU ou balanceamento com jogadores. O ranking é local e não oferece proteção contra edição deliberada do JSON.
