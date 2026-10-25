# BELLATOR — balanceamento das quatro classes

Data: 24/10/2026. Unity preservada: **6000.3.26f1**. Prioridade: PvE atual. Nenhum sistema de rede foi adicionado e fogo entre jogadores permanece desabilitado.

## Estado inicial e reversão

O projeto já tinha alterações locais, quatro definições de classe, armas exclusivas, habilidades, postura, imunidade após atordoamento, combos finitos, rolamento e quickstep. Esses sistemas foram aproveitados. Os valores “antes” abaixo são os arquivos encontrados neste trabalho, não o HEAD do Git.

Backup: `BalanceBackups/BeforeClassBalance-20261024.zip`, fora de Assets, preserva Scripts, ScriptableObjects, Prefabs, Scenes e ProjectSettings anteriores a esta intervenção. Para reverter, feche a Unity, restaure somente os arquivos modificados desta etapa a partir do ZIP e remova os arquivos novos listados no fim deste relatório. Não restaure todo o ZIP por cima de trabalhos posteriores. Os caminhos internos Scripts, ScriptableObjects, Prefabs e Scenes correspondem a Assets/Game; ProjectSettings corresponde à raiz do projeto.

## Diagnóstico

- Sobrevivente: 70 HP, sem escudo, 18 de dano inicial, combo de 63; DPS de combo de 34,51. Pagava o risco do alcance curto e da vida baixa sem vantagem ofensiva suficiente sobre o Guerreiro. Sua postura de 65 era inferior à do Caçador (75), contrariando a identidade desejada.
- Colosso: 180 HP com 30% de redução (257,14 de vida efetiva sem bloqueio), 57 por primeiro golpe e DPS de combo de 49,61. Somava a maior resistência ao maior DPS corpo a corpo de combo. O Golpe Devastador atingia em esfera, inclusive pelas costas, a 3,4 m.
- Guerreiro: 120 HP, 18% de defesa, DPS de combo de 39,49. Base coerente; Postura de Combate adicionava 35% de dano e 15 pontos percentuais de defesa, ultrapassando a defesa passiva original do Colosso durante o efeito.
- Caçador: disparo normal de 60,9, especial de **182,7**, projétil a 60 m/s e alcance de 85 m. O especial superava a vida de todas as classes antes da defesa. A recarga tinha tempo mas não custo de stamina. Aim Lock já usa projéteis físicos sem perseguição após o lançamento; não era necessário recriá-lo.

## Atributos antes → depois

| Atributo | Sobrevivente | Colosso | Guerreiro | Caçador |
|---|---:|---:|---:|---:|
| Vida | 70 → 90 | 180 → 160 | 120 → 120 | 80 → 90 |
| Stamina | 120 → 120 | 80 → 95 | 100 → 100 | 110 → 110 |
| Caminhada | 4,3 → 4,3 | 2,4 → 2,4 | 3,5 → 3,5 | 4 → 4 |
| Corrida | 7,5 → 7,5 | 4,1 → 4,1 | 6,2 → 6,2 | 7 → 7 |
| Multiplicador da arma principal | 0,75 → 0,95 | 1,5 → 1,15 | 1 → 1 | 1,45 → 1,25 |
| Dano inicial real bruto | 18 → 22,8 | 57 → 43,7 | 24 → 24 | 60,9 → 52,5 |
| Dano pesado real bruto | 32,4 → 41,04 | 108,3 → 83,03 | 43,2 → 43,2 | Não existe pesado de besta |
| Defesa passiva | 5% → 5% | 30% → 24% | 18% → 18% | 7% → 5% |
| Postura máxima | 65 → 80 | 155 → 155 | 105 → 105 | 75 → 65 |
| Multiplicador de dano à postura | 0,85 → 0,85 | 1,45 → 1,3 | 1 → 1 | 0,8 → 0,8 |
| Custo do rolamento | 20 → 20 | 32 → 32 | 26 → 26 | 23 → 23 |
| Recarga da habilidade (s) | 14 → 16 | 12 → 16 | 16 → 18 | 10 → 14 |
| Regeneração de stamina/s | 22 → 23 | 22 → 20 | 22 → 24 | 22 → 21 |
| Atraso de regeneração (s) | 1,2 → 1,1 | 1,2 → 1,3 | 1,2 → 1,05 | 1,2 → 1,2 |
| Regeneração durante guarda | 35% → 25% | 35% → 25% | 35% → 25% | 35% → 25% |

A regeneração anterior de 22/s, atraso de 1,2 s e escala de guarda de 0,35 estavam no prefab compartilhado. Agora o controlador aplica valores da definição da classe. Ataques, corrida, recarga e esquiva continuam impedindo regeneração durante a ação. Sem escudo, a escala de guarda não oferece benefício ao Sobrevivente ou Caçador. Manter guarda não tem drenagem contínua no sistema existente: somente impacto custa stamina; o campo guardDrain da arma já não era utilizado e não foi ativado.

Não foram alterados os multiplicadores de armas secundárias inacessíveis no equipamento fixo. As armas compartilhadas e os atributos dos inimigos não foram aumentados.

## Dano, tempo e recursos

Os tempos incluem preparação, execução de 0,04 s, período ativo, recuperação escalados pela velocidade da arma, recuperação adicional de 0,08 s e pausa final de combo de 0,2 s. A besta inclui recarga e não usa a pausa final de combo. O CSV gerado pela Unity é a fonte dos cálculos atuais.

| Classe | Combo/disparo bruto | Ciclo (s) | DPS de combo/cadência | Custo por ciclo |
|---|---:|---:|---:|---:|
| Sobrevivente | 79,8 em 3 golpes | 1,826 | 43,71 | 38,4 |
| Colosso | 98,325 em 2 golpes | 2,585 | 38,04 | 46 |
| Guerreiro | 84 em 3 golpes | 2,127 | 39,49 | 44,8 |
| Caçador | 52,5 em 1 disparo | 2,36 | 22,25 | 24, incluindo 8 da recarga |

O Sobrevivente passa a liderar o DPS corpo a corpo de combo, mantendo o menor dano por golpe e alcance de 1,8 m. O Colosso continua com maior dano individual, alcance de 2,45 m e postura, mas com maior comprometimento: pesado demora 1,655 s e custa 39,1 de stamina; dois pesados deixam 16,8 dos 95, insuficientes para rolar imediatamente. Guerreiro: pesado de 1,019 s e custo de 25,2. Sobrevivente: pesado de 0,851 s e custo de 21,6. Nenhum desses tempos foi encurtado por este trabalho.

O DPS de ciclo sustentado no CSV inclui uma pausa completa de regeneração após cada combo: dano / (tempo de ação + atraso de regeneração + custo / regeneração). É um modelo de repetição sustentável, não uma medição de combate ou estratégia ótima. Já o DPS de combo supõe stamina disponível e todos os golpes acertando. Ambos excluem defesa, deslocamento, esquiva, bloqueio e interrupções.

Besta: alcance 85 → 32 m, velocidade 60 → 40 m/s, recarga 1,6 → 1,8 s, custo do disparo 12 → 16 e recarga 0 → 8. Munição carregada/descarregada, recarga automática após o disparo e recarga manual foram preservadas. Se faltar stamina para recarregar, o jogador precisa recuperar stamina e pressionar recarga ou ataque novamente; o sistema não fornece munição gratuitamente. A 10 m, o voo nominal é de aproximadamente 0,25 s, além da preparação de 0,18 s; alvo pode sair da trajetória ou buscar cobertura.

## Rolamentos e impacto

| Classe | Duração | Recuperação | Distância | Invulnerabilidade antes → depois |
|---|---:|---:|---:|---:|
| Sobrevivente | 0,50 s | 0,14 s | 3,9 m | 0,220 → 0,220 s |
| Colosso | 0,72 s | 0,30 s | 3,2 m | 0,317 → 0,220 s |
| Guerreiro | 0,65 s | 0,22 s | 3,6 m | 0,286 → 0,220 s |
| Caçador | 0,54 s | 0,16 s | 3,6 m | 0,238 → 0,220 s |

As janelas começam em 18% da duração e terminam antes do fim do deslocamento. Recuperação é vulnerável e bloqueia uma nova esquiva. Quickstep foi preservado nas duas classes leves: 0,35 s, com janela de aproximadamente 0,15 s (antes 0,154 s), custos 18/21 e recuperação 0,12/0,14 s. Não há invulnerabilidade durante toda a animação. A proteção existente após atordoamento permanece: 0,75 s de stagger e mais 1,25 s de imunidade à repetição; os multiplicadores reservados para PvP podem encurtar o stagger sem remover essa proteção.

## Habilidades

- **Passos Desesperados:** 5 → 4 s, custo 24 mantido, recarga 14 → 16 s; velocidade ×1,45 → ×1,2; custo de esquiva ×0,5 → ×0,8; distância ×1,35 → ×1,1; recuperação de esquiva ×0,5 → ×1. Não acrescenta invulnerabilidade nem regenera stamina. Mais mobilidade temporária com limite de recursos e intervalo vulnerável preservado.
- **Golpe Devastador:** custo 40 mantido; preparação 0,6 → 0,85 s, recuperação 0,5 → 0,65 s, recarga 12 → 16 s. Dano base 48 mantido; com a nova classe, 72 → 55,2 bruto. Postura base 85 mantida: 123,25 → 110,5. Área de esfera de 3,4 m → cone frontal de 110° e 2,8 m, sujeito a linha de visão. A direção fica comprometida durante a preparação. Marcadores no chão delimitam o arco frontal durante a preparação; oponentes podem recuar, contornar ou interromper por postura.
- **Postura de Combate:** duração 6 s e custo 25 mantidos; dano ×1,35 → ×1,15; defesa adicional 15 → 8 pontos percentuais; custo de bloqueio ×0,6 → ×0,8; recarga 16 → 18 s. A defesa temporária de 26% é ligeiramente superior aos 24% passivos do Colosso, mas sua vida e postura continuam menores e o benefício tem custo e duração limitada.
- **Disparo Preciso:** custo 30 mantido, dano especial ×3 → ×1,5; preparação 0,4 → 0,75 s, recuperação 0,25 → 0,4 s; recarga 10 → 14 s. Dano bruto 182,7 → 78,75. Continua consumindo um projétil carregado e exige recarga depois. Sem projétil guiado. Mesmo antes dos modificadores de PvP, seu dano após 5% de defesa é 74,81, inferior aos 90 HP das classes leves.

## Inimigos e PvE

Os valores encontrados e preservados: comum 85 HP, espada 12, velocidade 3,1, chance de defesa 0,55 e escudo com redução de 80% no arco frontal; pesado 160 HP, espada 27, velocidade 2,05, postura 165; arqueiro 55 HP, flecha 11, velocidade 3,6 e sem defesa ativa. A chance de defesa é uma decisão da IA, não redução passiva de todo dano.

Estimativa de impactos leves sem defesa, usando o combo cíclico: comum exige 4/2/4/2 impactos de Sobrevivente/Colosso/Guerreiro/Caçador; pesado exige 7/4/6/4; arqueiro exige 3/2/3/2. Isso não inclui aproximação, bloqueios, tempo de voo, especiais ou descanso de stamina. O arqueiro deixa de morrer em um disparo normal de Caçador; o especial ainda recompensa uma oportunidade preparada. O Colosso deixa de matar o comum com um pesado isolado (83,03 < 85).

## Arquitetura para ajustes futuros

Cada GladiatorClassDefinition contém recuperação de stamina e duas configurações independentes, pve e pvp: dano, dano especial, dano de postura, duração de atordoamento, cura e regeneração. PvE tem multiplicadores 1; a reserva PvP começa com dano 0,85, especial 0,85 adicional, postura 0,75, duração de stagger 0,6, cura 0,75 e regeneração 0,9. São pontos iniciais conservadores, não números validados por partidas.

DamageReceiver aplica os modificadores no impacto conforme o tipo do alvo e preserva primeiro a autorização de facção. Um alvo jogador é identificado pelo controlador de classe; AI pelo EnemyBrain; outros alvos são de treinamento. Essa classificação está disponível em DamageReceiver.TargetKind para a futura integração com seleção de alvos. O filtro e comportamento do PlayerLockOn atual não foram alterados. Identificação por facção, alcance, obstrução e acessibilidade permanecem iguais. Um futuro adaptador poderá usar TargetKind ao incluir jogadores elegíveis.

Projéteis propagam a identificação de disparo especial. O Golpe Devastador também informa que o impacto é especial. Os bônus temporários de Postura de Combate participam do dano normal, modulado pelo dano PvP; abilityEffect escala os benefícios acima do valor neutro (velocidade, esquiva, dano, defesa e custo de bloqueio), abilityDuration escala a duração dos buffs e pushStrength escala o empurrão por tipo de alvo. A reserva PvP começa em 0,85/0,8/0,5 para esses três parâmetros. ConfigureCombatContext reserva a seleção de efeitos, cura e regeneração por contexto; Apply sempre começa em PvE e nenhum modo existente ativa o contexto PvP. A definição de classe não é modificada durante o jogo.

Ainda será necessário adaptar para rede: autoridade e validação de ações, relógios de cooldown, proprietário/time de cada ator, regras de fogo amigo e alvo, compensação de latência, replicação de projéteis e animações, estados de vida/postura/stamina, regras de entrada em partidas e custos/prevenção de fraude. A escolha do contexto por modo e os valores definitivos de efeitos deverão ser integrados às regras reais do multiplayer. Nenhum mecanismo de rede foi implementado nesta etapa.

## Seis confrontos teóricos

Consulte `ClassBalanceMatchups.txt` para a simulação local executada com os assets carregados. É uma troca de acertos garantidos sem movimento, cobertura, bloqueio ou esquiva, com pausas de stamina; não são partidas PvP e não produz taxa de vitória.

1. **Sobrevivente × Colosso:** troca parada favorece fortemente o Colosso. Sobrevivente precisa provocar erro, sair do cone e explorar a recuperação de 1,655 s do pesado. Corrida 7,5 contra 4,1 e esquiva com ciclo de 0,64 contra 1,02 s permitem esse jogo; encurralado, está em desvantagem. Requer atenção prioritária em testes futuros de duelos.
2. **Sobrevivente × Guerreiro:** Guerreiro vence trocas frontais e pode bloquear; Sobrevivente tem maior DPS de combo e mobilidade, mas menor dano individual. Ângulo, stamina da guarda e finisher decidem oportunidades. A postura não permite travamento permanente.
3. **Sobrevivente × Caçador:** aproximação e cobertura favorecem Sobrevivente; linhas abertas favorecem Caçador. Recarga de 1,8 s e custo de 8 reduzem a capacidade de esquivar indefinidamente após cada tiro. A superioridade de velocidade do Sobrevivente permite encurtar distância se não desperdiçar stamina.
4. **Colosso × Guerreiro:** maior vida/postura/dano individual do Colosso favorecem troca direta. Guerreiro precisa alternar defesa, recuperação de stamina superior e punição de erros. O confronto parado ainda apresenta diferença grande e precisa ser investigado com jogadores; não se promete equivalência de vitórias.
5. **Colosso × Caçador:** alcance favorece Caçador, escudo e vida favorecem Colosso quando alcança o alvo. Arena aberta pode produzir perseguição longa; velocidade finita de projétil, alcance de 32 m e custos de recarga limitam a pressão, mas não substituem testes de mapas e modos. Risco residual de kiting em PvP aberto.
6. **Guerreiro × Caçador:** guarda frontal oferece aproximação com custo; Caçador precisa buscar flancos e punir recuperações. Guerreiro não possui ataque à distância e não deve atravessar campo aberto sem gerenciar a guarda. Caçador é frágil ao chegar ao alcance da espada.

Os modelos deliberadamente expõem os confrontos desfavoráveis. Uma vitória próxima de 50% deve ser investigada com dados reais de jogadores de habilidade semelhante, nunca inferida do CSV.

## Laboratório de desenvolvimento

Cena `Assets/Game/Scenes/BalanceLab.unity`, cópia separada do ambiente da arena com NavMesh existente. Sistemas de partida, ondas, interface normal e inimigos previamente colocados foram removidos apenas da cópia. Não foi adicionada às cenas do build.

Abra **Bellator → Balance → Open Development Lab** e entre em Play. F8 alterna o painel e o controle de combate. Escolha classe, crie um comum/pesado/arqueiro e use alvos passivos para medir dano ou desmarque antes de criar para ativar a IA. Os inimigos novos usam os prefabs reais. “Restaurar classe e limpar alvos” encerra a tentativa e devolve os recursos. “Zerar medições” mantém a situação atual.

Métricas: dano efetivamente causado e recebido após defesa/bloqueio, DPS observado incluindo espera desde o início da tentativa, ataques iniciados (inclui erros), impactos, esquivas aceitas, stamina realmente consumida, usos aceitos de habilidades e tempo de sobrevivência. Para cada alvo morto, tempo desde o primeiro impacto e quantidade de impactos; uma eliminação de um golpe registra 0 s desde o primeiro impacto, não um TTK incluindo preparação. O contador de ataques exibido na morte é o total da tentativa, não uma atribuição exclusiva quando há vários alvos. Para comparar classes, use uma tentativa com um único alvo e posição inicial fixa.

Exportação grava um TXT em Application.persistentDataPath e informa o caminho no Console. A ferramenta só compila no editor ou em Development Build. O HUD normal não recebeu painéis de habilidades nem métricas.

**Bellator → Balance → Export Attribute Calculations** gera o CSV e os seis confrontos. **Test All Four Classes in Play Mode** executa a suíte específica; os arquivos ClassBalanceValidation.txt e os demais relatórios de regressão registram resultados reais e limitações.

## Arquivos desta etapa

- Alterados: quatro assets de classe, HunterWeapon, quatro assets de habilidade; GladiatorClassDefinition, PlayerClassController, PlayerAbility, AbilityDefinition, CharacterStamina, CharacterHealth, DamageReceiver, ActorPosture, WeaponDefinition, PlayerCombat, CombatQueries e CombatProjectile.
- Adicionados: CombatRuleConfiguration.cs, BalanceTrainingLab.cs, ClassBalanceTools.cs, BalanceLab.unity, ClassBalanceReport.md, ClassBalanceChangedFiles.txt, ClassBalanceAITrials.txt, CSV de cálculos, TXT de confrontos e TXT de validação, com seus arquivos .meta.
- Backup externo a Assets: BeforeClassBalance-20261024.zip.
- Os testes de regressão existentes podem atualizar seus arquivos de evidência/documentação. Não há alteração intencional nos prefabs, nos inimigos, nas animações, nas cenas originais, na câmera, no Aim Lock, em contas ou ranking.

## Verificação e limites

Os resultados finais das execuções desta etapa são registrados em `ClassBalanceValidation.txt`, `SoulslikeValidation.txt`, `Stage6Validation.txt` e `InfiniteModeValidation.txt`, conforme os testes concluídos. Somente relatórios atualizados durante esta execução contam como evidência. A suíte de laboratório verifica ações e dano contra prefabs reais; a regressão de ondas usa eliminações de teste para percorrer as transições e pontuação, não partidas completas jogadas normalmente. Cálculos de DPS e confrontos são modelos locais. A avaliação de diversão, dificuldade integral por classe e equilíbrio PvP/cooperativo continua exigindo sessões humanas e dados de jogo; esta entrega é uma base balanceada e instrumentada para essas iterações.

### Execuções concluídas nesta entrega

- **Compilação na Unity 6000.3.26f1:** assemblies de jogo e editor recompilados; nenhum erro de C# encontrado no log. A suíte de laboratório final executou sobre esses assemblies.
- **ClassBalanceValidation: PASS**, incluindo as quatro classes, ações, bloqueio, defesa, recarga com stamina, janela de invulnerabilidade, recuperação, custo/duração/cooldown de habilidades e dano físico contra os três prefabs de inimigo. A verificação explícita identificou e permitiu corrigir um custo de recarga que inicialmente não havia sido importado pela Unity; a rodada final confirmou o custo configurado.
- **12 encontros com IA ativa: PASS nos testes de funcionamento**, medidos em Play Mode. Resultados brutos em ClassBalanceAITrials.txt. Não se exige que um alvo morra no limite da amostra; alvo sobrevivente é registrado como tentativa incompleta, sem inventar TTK.
- **SoulslikeValidation: PASS**, incluindo câmera, Aim Lock, projétil físico, Input System, esquiva, combos, guarda, postura, proteção contra atordoamento repetido e interrupção por morte.
- **MatchStageValidation: PASS** nas quatro classes: cinco ondas originais, pausas, spawns, portão, travessia física para vitória, derrota e reinício com seleção preservada.
- **InfiniteModeValidation: PASS** nas quatro classes. Sobrevivente percorreu 35 ondas consecutivas com eliminações de teste; as demais percorreram duas cada. Escala de dificuldade também foi verificada numericamente até int.MaxValue. Pontuação, ranking isolado de teste, resultados, equipamento fixo, Q, pausa e retorno ao História passaram. Isso não representa sobreviver normalmente a 35 ondas com cada classe.
- **AccountStageValidation: PASS**, com contas isoladas em Temp: criação/login/logout, identidades estáveis, gravação única de resultados, modo convidado nos dois modos de jogo, leitura de ranking, impedimento de gravação de ranking por convidados e recuperação de armazenamento. Os dados normais de conta/ranking não foram usados como fixtures.

### Amostras com IA ativa

Bot simples: aproximação/reposicionamento, leves/pesados, tentativa de Q após dois segundos e esquiva reativa. Não usa bloqueio estratégico, portanto não explora toda a identidade defensiva do Guerreiro e Colosso. A escolha de ataques depende do contador de ações e não é uma política ótima ou idêntica em eficácia para todas as classes. A amostra não deve ser usada como ranking de classes.

| Classe | Comum: duração / dano recebido | Pesado: duração / dano recebido | Arqueiro: duração / dano recebido |
|---|---|---|---|
| Sobrevivente | 2,10 s / 11,4, alvo derrotado | 6,08 s / 51,3, alvo com 2,7 HP | 1,87 s / 10,5, alvo derrotado |
| Colosso | 4,20 s / 18,2, alvo derrotado | 6,08 s / 41,0, alvo com 33,3 HP | 6,10 s / 8,4, alvo com 11,3 HP |
| Guerreiro | 6,06 s / 17,8, alvo com 21,2 HP | 6,08 s / 42,1, alvo com 7,2 HP | 2,48 s / 9,0, alvo derrotado |
| Caçador | 3,23 s / 0, alvo derrotado | 5,63 s / 0, alvo derrotado | 3,33 s / 10,5, alvo derrotado |

Esses tempos começam na tentativa e incluem aproximação; diferem do tempo entre primeiro e último impacto registrado pelo laboratório. As amostras mostram o benefício real de distância do Caçador contra melee lento e o risco de gastar recursos em habilidades/esquivas sem uma boa oportunidade. Há risco residual de perseguição longa em mapas abertos. No PvP teórico, as trocas paradas ainda favorecem bastante o Colosso contra Guerreiro e Sobrevivente; mobilidade, esquiva, bloqueio e falhas de ataque precisam ser medidos com jogadores antes de aprovar números competitivos definitivos. Não foram executadas partidas PvP ou cooperativas.

A captura de tela via controle do computador não respondeu neste ambiente; a confirmação da execução veio dos relatórios produzidos pelos testes dentro da Unity. As imagens produzidas pelos testes de regressão foram atualizadas, mas não são apresentadas como inspeção visual manual desta entrega. O arquivo ClassBalanceChangedFiles.txt lista as alterações de código/dados comparadas ao backup inicial e as evidências atualizadas.
