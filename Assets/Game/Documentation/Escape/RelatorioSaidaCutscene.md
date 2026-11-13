# Bellator — saída física e cutscene de liberdade

Implementação e testes em 13/11/2026, Unity 6000.3.26f1.

## Resultado

O modo História, de cinco ondas, agora permite atravessar um portão físico, percorrer um corredor romano e sair para um terraço externo. A sequência usa o gladiador escolhido, seu Animator, modelo, equipamentos e CharacterController reais. A vitória é registrada depois da caminhada e do fade, sem reinício automático.

As validações de Play Mode da saída, da regressão offline e do modo infinito terminaram com `RESULT: PASS`. Não foi realizado um novo teste multiplayer entre dois computadores nesta tarefa.

## Diagnóstico e backup

- O portão está em `Arena/Gates/MainExitGate`, próximo de `(0, 0, 29.55)`.
- A peça `TunnelBackBoundary`, no fundo do túnel, bloqueava fisicamente o caminho. Além dela, malhas combinadas das arquibancadas e da fachada preenchiam visualmente a área atrás do portão.
- A lógica antiga registrava vitória logo após cruzar um plano junto ao portão, antes de qualquer caminhada para fora.
- Backup anterior às mudanças: `EscapeBackups/BeforeEscape_20261113_150150/`, com a cena, o prefab compartilhado e os scripts afetados. A pasta foi adicionada ao `.gitignore`.
- O prefab original `Assets/Game/Prefabs/RomanArena.prefab` foi preservado. A instância da arena foi desvinculada somente na cena offline para permitir os recortes locais.

## Cenário e portão

- Removida a parede de fundo do túnel.
- Recortados os triângulos de 25 malhas locais, criando uma abertura visual real através da estrutura. Os assets originais das malhas foram mantidos; os recortes estão em `StoryEscape/Geometry`.
- Portão de madeira com tábuas, faixas de ferro, rebites e fechadura. A abertura eleva a geometria e seus colliders durante três segundos.
- Chamadas repetidas de abertura não reiniciam o movimento nem repetem o som.
- Abertura condicionada à conclusão das cinco ondas, sem inimigos vivos nem spawns pendentes. A progressão e o balanceamento das ondas não foram reescritos.
- Indicador verde e mensagem existente de saída liberada associados ao estado de fuga.
- Piso contínuo, paredes de pedra, teto, quatro conjuntos de arcos e colunas, tochas e iluminação quente no corredor.
- Terraço externo com piso, colunatas, balaustradas, estandartes e iluminação de céu. O exterior é uma área simples, sem uma cidade adicional.

## Sequência cinematográfica

1. Ao cruzar o ponto de ativação dentro do corredor, o controle de movimento, combate, habilidade e Aim Lock é suspenso. O HUD de combate desaparece.
2. A câmera passa suavemente do enquadramento de jogo para a entrada do corredor e depois acompanha o gladiador por trás.
3. O motor existente move o personagem com colisões, enquanto a ponte de animação existente reproduz a caminhada. O roteiro não teletransporta o personagem durante a sequência.
4. O som da arena diminui, vento e pássaros aumentam e a iluminação local clareia progressivamente.
5. O gladiador atravessa a abertura externa, dá passos no terraço, desacelera, para e observa o ambiente. A câmera abre o enquadramento e se afasta.
6. Fade para preto e painel existente de resultado com **A LIBERDADE FOI CONQUISTADA**, cinco ondas concluídas, gladiador, inimigos derrotados e tempo. O botão de menu principal continua disponível.

O roteiro só aceita História com as cinco ondas concluídas, vida positiva e portão totalmente aberto. Recusa atores de rede e sessões online conectadas. O disparo é protegido contra duplicação. Se o trajeto ficar bloqueado, interrompe a sequência e restaura os controles e a câmera, emitindo diagnóstico.

Foi usada uma sequência por scripts integrada à câmera e ao motor existentes. Não houve instalação de Timeline, Cinemachine ou outros pacotes. Os três arquivos de áudio são efeitos procedurais locais, com fontes e volumes editáveis na cena.

## Arquivos alterados ou adicionados

| Arquivo/pasta | Finalidade |
|---|---|
| `Assets/Game/Scenes/RomanArena.unity` | Abertura, portão detalhado, corredor, exterior, luzes, áudio e referências da sequência |
| `Assets/Game/Scripts/Arena/MainGateController.cs` | Abertura física idempotente, estado de movimento e som |
| `Assets/Game/Scripts/Match/GameManager.cs` | Integração do estado de fuga, suspensão de controles e vitória após a cutscene |
| `Assets/Game/Scripts/Match/GameInterface.cs` | Ocultar HUD durante a sequência e apresentar a vitória final |
| `Assets/Game/Scripts/Match/EscapeVictorySequence.cs` | Caminhada real, câmeras, áudio, iluminação, fade e recuperação de bloqueios |
| `Assets/Game/Scripts/Editor/EscapeMeshCut.cs` | Recorte não destrutivo das malhas originais |
| `Assets/Game/Scripts/Editor/EscapeSequenceTools.cs` | Auditoria, instalação protegida e renders de revisão |
| `Assets/Game/Scripts/Editor/EscapeSequenceValidation.cs` | Testes de Play Mode dos quatro gladiadores e isolamento do infinito |
| `Assets/Game/StoryEscape/Geometry/` | 25 malhas derivadas para a abertura física |
| `Assets/Game/StoryEscape/Audio/` | Som do portão, arena e ambiente externo |
| `Assets/Game/Documentation/Escape/` | Este relatório, logs e imagens dos testes |
| `.gitignore` | Exclusão da pasta de backup |

Os novos assets e scripts também possuem seus arquivos `.meta` da Unity.

## Testes executados na Unity

### Saída e cutscene — aprovado

Log completo: `PlayModeValidation.txt`. Executado com **Sobrevivente, Colosso, Guerreiro e Caçador**, carregando a cena e o gladiador reais em cada caso.

- O CharacterController encontra o portão fechado e não passa antes da liberação.
- As cinco ondas são processadas pelo WaveManager existente, com verificação de que o portão continua fechado enquanto há ondas, inimigos ou spawns pendentes.
- A abertura ocorre uma vez; chamadas repetidas não a reiniciam.
- Verificação física de obstruções ao longo da passagem e raycasts de continuidade do piso.
- O ator caminha fisicamente até o trigger e a sequência começa automaticamente.
- O ator original percorre mais de 25 metros durante a sequência, preservando a instância do Animator e reproduzindo animação de movimento.
- Verificação dos quatro estágios de câmera, deslocamento da câmera e ausência de interseção com colliders.
- Controle de combate/movimento bloqueado, HUD oculto e segunda ativação recusada.
- Sem salto de posição do ator, queda pelo piso ou interrupção do percurso nos casos testados.
- Exterior alcançado antes do fade completo; vitória com cinco ondas registrada; mensagem final presente.
- Retorno ao menu após cada gladiador, sem objetos da sequência, escala de tempo alterada ou cursor preso.
- O modo infinito recusa o início da cutscene e mantém o portão fechado.

Para acelerar a preparação das partidas de teste, o relógio das ondas foi avançado pelo método de simulação existente e os inimigos foram derrotados pelo DamageReceiver real com dano elevado de teste. Isso não altera assets de balanceamento. O posicionamento inicial junto ao portão é uma preparação de teste; dentro da cutscene a locomoção é real, com verificação de ausência de teletransporte. Não representa quatro sessões completas jogadas manualmente.

### Regressão offline — aprovado

Log: `OfflineRegression.txt`.

- Tela de acesso, entrada como convidado, opção online e exclusão de convidados do ranking.
- Criação de conta local em diretório isolado de teste, senha correta/incorreta e associação do ranking à conta.
- Modo tradicional com cinco ondas e jogador offline sem restrições de autoridade de rede.
- Movimentação, ativação da habilidade Q e recebimento de dano.

### Modo infinito e ranking — aprovado

Log: `InfiniteRegression.txt`; imagens `InfiniteHUD.png`, `InfiniteResults.png` e `InfiniteRanking.png`.

- 35 ondas sucessivas, ultrapassando a quinta e a trigésima primeira, sem abrir o portão.
- Pontuação por dano/morte real, limites de inimigos ativos, ausência de mortes duplicadas e limpeza das listas.
- Os quatro gladiadores com equipamentos próprios, habilidade Q, pausa, derrota, resultado e navegação.
- Persistência local do ranking, rejeição de duplicatas/dados inválidos, recuperação de backup e exclusão de resultados de História.
- Retorno de Infinito para História mantendo cinco ondas, vida/dano base dos inimigos e assets compartilhados intactos.
- Esse teste não equivale a uma sessão de estabilidade de várias horas.

### Compilação e preservação do multiplayer

Os scripts foram compilados pelo Editor Unity 6000.3.26f1 e os testes acima executaram as cenas reais. A última pequena correção restaura também as permissões anteriores de corrida, esquiva e rotação no caminho de recuperação.

Comparação com o Git confirmou ausência de alterações em `Assets/Game/Prefabs/RomanArena.prefab`, `Assets/Game/Scenes/OnlineArena.unity`, `Assets/Game/Scripts/Multiplayer/` e `Packages/manifest.json`. A nova sequência não foi adicionada à arena online. Nenhum pacote foi instalado.

Não foram realizados nesta tarefa novos testes de Relay, host/cliente ou desconexões entre máquinas. A preservação da rede aqui foi verificada pela separação da cena, pelas condições de ativação e pela comparação dos arquivos; isso não substitui um teste online de ponta a ponta.

## Evidências e uso

- `BeforeAudit.txt`: diagnóstico dos bloqueios originais.
- `GeometryChanges.txt`: lista das malhas recortadas e da parede removida.
- `BeforeGate.png` e `ClosedGate.png`: portão anterior e novo portão fechado.
- `PhysicalCorridor.png` e `Exterior.png`: corredor e terraço reais.
- `Cinematic_1.png` a `Cinematic_5.png`: renders da câmera durante a sequência do primeiro gladiador. Os renders de câmera não incluem o Canvas de interface; a mensagem final foi conferida por teste de UI.

Para jogar: iniciar pelo menu, escolher História e um gladiador, concluir as cinco ondas, aguardar o portão terminar de subir e atravessar o corredor. A cutscene começa alguns metros depois do portão. Ao final, usar **MENU PRINCIPAL**.

Não há configuração de Dashboard, conta Unity, credenciais ou pacotes adicionais necessária para esta funcionalidade. Não foi gerada uma nova build distribuível nesta tarefa; as validações relatadas ocorreram no Editor. Para distribuição, gerar uma build com a versão atual do projeto.

Opcionais de acabamento: substituir os efeitos procedurais por gravações de áudio e ajustar a direção artística do exterior. Esses ajustes não são necessários para a passagem e a cutscene funcionarem nos casos testados.
