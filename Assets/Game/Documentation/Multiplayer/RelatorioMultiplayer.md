# Bellator — relatório da implementação multiplayer

## Retomada após a interrupção — 13/11/2026

A implementação anterior foi mantida. Antes de modificar código, foram conferidos o Git diff, os módulos de rede, a configuração, os prefabs, as cenas, o catálogo, a build e os relatórios existentes. A build anterior já completava duelos locais; não foi necessário recriar personagens, combate, câmera, seleção ou modos offline.

### Correções adicionais desta retomada

- Inicialização dos serviços e autenticação agora respeitam o cancelamento e o limite de espera da tentativa, mesmo nas chamadas do SDK que não recebem CancellationToken. A conclusão tardia dessas operações não continua a tentativa cancelada nem cria uma sala.
- Operações de inicialização/autenticação ainda pendentes são reutilizadas na próxima tentativa, evitando chamadas simultâneas ao SDK para o mesmo estágio.
- O encerramento espera até oito segundos pela confirmação do SDK e sempre solicita o desligamento da rede local no finally. Ao solicitar a saída, a simulação da partida é suspensa imediatamente.
- Se a limpeza remota ainda estiver pendente, uma nova conexão fica bloqueada para que a sessão antiga não desligue a nova. O usuário pode retornar ao menu e usar os modos offline enquanto isso.
- Botões de escolha, pronto e início ficam indisponíveis durante conexão/encerramento.
- A leitura de mensagens de sala malformadas foi incluída no tratamento de exceções já existente.

Esses limites são da espera local. Não significam que uma requisição HTTP sem API de cancelamento foi interrompida no servidor Unity. A limpeza remota real ainda precisa ser validada com os serviços acessíveis.

| Arquivo | Alteração nesta retomada |
|---|---|
| Assets/Game/Scripts/Multiplayer/OnlineGame.cs | Cancelamento, reaproveitamento das operações pendentes, prazo de encerramento, suspensão da simulação e proteção das mensagens |
| Assets/Game/Scripts/Multiplayer/OnlineInterface.cs | Bloqueio dos comandos durante conexão e limpeza |
| Assets/Game/Scripts/Multiplayer/OnlineAsync.cs | Utilitário novo para espera cancelável de operações do SDK |
| Assets/Game/Scripts/Editor/OnlineAsyncValidation.cs | Testes novos de cancelamento, prazo, conclusão tardia e propagação de falhas |
| Assets/Game/Documentation/Multiplayer/ | Relatório e evidências atualizados |

Nenhum pacote foi adicionado ou atualizado nesta retomada. Os arquivos de movimentação e combate offline não foram reescritos. Foi criado um backup incremental dos módulos e validadores em `MultiplayerBackups/BeforeResume_20261113_142243/`, além do backup completo anterior.

### Resultados reexecutados

| Teste | Resultado |
|---|---|
| Compilação do Editor/ILPP e build Windows | PASS; build atualizada em 13/11/2026, 14:28, horário de Brasília |
| Guerreiro/Sobrevivente na build encontrada, com perda e atraso | PASS: seleção individual, movimento, animações, habilidades, rolamentos, dano, resultado e retorno à sala |
| Colosso/Caçador na nova build, com 75 ms de atraso de envio, 20 ms de jitter e 3% de perda por participante | PASS nos dois processos, incluindo enquadramento individual, menu oculto e ciclo completo |
| Tentativa de reduzir vida diretamente na réplica do cliente | Rejeitada |
| Encerramento abrupto do host na nova build | PASS: cliente encerrou a partida e apresentou aviso em português, sem migração |
| Encerramento abrupto do cliente na nova build | PASS: host encerrou a partida sem registrar ranking |
| Operação que nunca termina e cancelamento da espera | PASS em teste isolado; conclusão tardia não avança a conexão; falhas do SDK continuam sendo propagadas |
| Acesso atual, convidado, contas locais isoladas, senha correta/incorreta e ranking local | PASS |
| Modo tradicional: cinco ondas configuradas, movimento, habilidade e dano após abrir a interface online | PASS |
| Preservação dos prefabs, cenas e definições originais | 48 arquivos comparados; zero alterações |
| Inicialização real dos serviços Unity/Relay | BLOQUEADA por PlayerPrefsException; nenhum código de convite foi obtido |
| Relay entre dois computadores/redes diferentes | NÃO EXECUTADO; continua pendente |

Evidências: `ResultadosRetomada.txt`, `AsyncValidation.txt`, `OfflineRegression.txt`, `BuildStatus.txt`, `PreservationCheck.txt` e `RelayStatus.txt`, nesta pasta. O nome interno HOST_DISCONNECT_PASS também é usado pelo probe no teste em que o cliente é encerrado; a identificação do arquivo e a mensagem do host distinguem os dois casos.

Não foram encontrados erros atuais de compilação C# nem conflito de controle entre jogador local e réplica. O log do Editor também contém mensagens de licenciamento/serviços do plugin Unity AI; elas não são falhas de compilação dos scripts Bellator. O teste legado de contas continua incompatível com os nomes/componentes antigos da interface, conforme explicado mais abaixo; a regressão da interface atual passou.

### Estado das etapas e pendências

Análise e backups, arquitetura Host/Client, modo adicional, seleção individual, replicação autoritativa, combate, arena, resultado e desconexões estão implementados e possuem testes locais. O login técnico anônimo e a criação/entrada por sessões Relay estão preparados no código, porém não foram comprovados contra os serviços Unity neste ambiente.

Para concluir a aprovação online, ainda é necessário:

1. Executar a pasta completa `C:\unity\Bellator\Builds\OnlineTest\` diretamente no Windows, com acesso normal ao perfil e à internet. O executável sozinho não basta.
2. No Unity Dashboard, selecionar o projeto Bellator já vinculado e confirmar que Authentication, Lobby e Relay estão disponíveis/ativados. Não inserir senhas ou chaves nos scripts.
3. Em dois computadores, criar a sala, copiar o código, entrar, escolher gladiadores diferentes, marcar pronto e concluir um duelo. Testar também código inválido, sala cheia e cancelamento real durante conexão.
4. Avaliar manualmente o conforto da movimentação e o resultado visual, além de ataques pesados, defesa, recarga e combinações de habilidades sob latência real. Os probes repetidos nesta retomada exercitam ataques leves e habilidades, não todas as combinações de ações.

A build está preparada para esse teste, mas o multiplayer pela internet **não está declarado concluído/aprovado**. O modo local de teste usa Netcode/Transport em loopback e não substitui Relay. Permanecem os limites já documentados: sem predição/reconciliação, compensação histórica de acertos, ranking online, matchmaking, coop ou migração de host.

Implementação no projeto Unity 6000.3.26f1, em 13/11/2026.

**Estado:** modo 1v1 implementado e testado em processos locais. A conexão real pelos serviços Unity/Relay e o teste em dois computadores ainda não estão aprovados. O ensaio de Relay foi interrompido na inicialização dos serviços por `PlayerPrefsException: Could not store preference value`; este ambiente também recusou conexões externas de telemetria. Não foi possível concluir criação de sessão, obter um código Relay ou testar sua entrada pela internet.

## Backup e preservação

Backup anterior às alterações: `MultiplayerBackups/BeforeOnline_20261113_100757/`, contendo Assets, Packages e ProjectSettings. Para restaurá-lo, feche a Unity e restaure essas três pastas a partir do backup. A pasta de backup é ignorada pelo Git.

A comparação SHA-256 com o backup não encontrou alterações nos prefabs, cenas e ScriptableObjects de gameplay originais. A arena e o jogador online são arquivos novos. Não foram removidos personagens, habilidades, Aim Lock, equipamentos nem modos offline.

## Pacotes resolvidos pelo Package Manager

| Pacote | Versão | Instalação |
|---|---|---|
| com.unity.netcode.gameobjects | 2.7.0 | Dependência direta |
| com.unity.services.multiplayer | 2.4.0 | Dependência direta |
| com.unity.transport | 2.7.4 | Dependência dos pacotes |
| com.unity.services.authentication | 3.8.0 | Dependência do SDK multiplayer |
| com.unity.services.core | 1.18.0 | Dependência dos serviços |

Não foram instalados os SDKs individuais redundantes de Lobby ou Relay. As sessões são criadas pelo Multiplayer Services SDK com `WithRelayNetwork()`, que coordena a rede e os serviços. Não existe servidor dedicado ou migração de host nesta versão.

## Arquitetura e funcionalidades

- `OnlineGame`: autenticação técnica anônima, sessões privadas de dois lugares, criação/entrada por código, cancelamento, roster e estados da partida. A conta local Bellator permanece independente da identidade Unity.
- `OnlineInterface`: acesso pelo menu principal e pela tela de modos; criar sala, entrar, copiar código, participantes, seleção, pronto, início pelo host, contagem, vida/stamina, resultado, volta à sala e saída. Usa BellatorUI e a logo original.
- A seleção original é aberta localmente como cena aditiva. Confirmar envia apenas a escolha do próprio participante, sem modificar a seleção offline. As câmeras e o enquadramento existentes da seleção são reutilizados.
- `NetworkGladiator`: prefab com NetworkObject, propriedade de rede para a classe, comandos de entrada vinculados ao proprietário e snapshots produzidos pelo host. O jogador local recebe sua câmera; o adversário não recebe controle local. As câmeras e listeners do menu são suspensos durante o combate e restaurados ao sair. O Canvas do menu fica oculto para permitir enxergar a arena.
- O host executa o motor, stamina, regras PvP, cooldowns, defesa, ataques, colisões, projéteis, habilidades, invulnerabilidade e morte usando os sistemas existentes. Os clientes não enviam posição, vida, dano ou uma vítima escolhida.
- As réplicas não simulam dano ou habilidades. Snapshots de vida, posição, rotação, animação e efeitos são aceitos apenas do servidor. Os ataques preservam o controle de alvos já atingidos do combate original.
- Entradas a 30 Hz, simulação no FixedUpdate do host e snapshots binários a 20 Hz. Contadores de ações toleram perda de mensagens sem repetir uma mesma pressão. Ticks antigos são descartados. Interpolação visual de 50 ms nos clientes.
- A arena online deriva da arena atual, com dois pontos de nascimento separados. Não carrega WaveManager, GameManager ou inimigos offline. O host inicia após ambos estarem prontos e carrega a cena usando o gerenciamento de cenas do Netcode.
- Contagem de quatro segundos sem movimento; vitória/derrota/empate decididos pelo host. Retorno à sala despawna jogadores e descarrega a arena para ambos.
- Aim Lock aceita o adversário, validando distância e visibilidade no host para jogadores remotos. A câmera local conserva o sistema existente.
- Colosso conserva o ataque em área e aplica impulso físico ao jogador atingido. Os projéteis remotos reutilizam os modelos existentes, com sua simulação e colisores desativados. Efeitos de habilidade usam a cor da definição original.
- Uma saída em combate encerra a partida. A perda do host encerra a sessão e informa que não há migração. São descartados jogadores, câmeras, projéteis e a cena online, com retorno ao menu/sala conforme o fluxo.
- Nenhum resultado online é gravado no ranking local. Convidados continuam excluídos do ranking. Ranking online compartilhado não foi implementado.

## Arquivos

### Novos

| Arquivo/grupo | Finalidade |
|---|---|
| Assets/Game/Scripts/Multiplayer/OnlineConfiguration.cs | Referências aos assets e protocolo |
| Assets/Game/Scripts/Multiplayer/OnlineProtocol.cs | Roster, comandos e snapshots |
| Assets/Game/Scripts/Multiplayer/OnlineGame.cs | Sessão, sala e partida |
| Assets/Game/Scripts/Multiplayer/NetworkGladiator.cs | Autoridade, entrada e sincronização |
| Assets/Game/Scripts/Multiplayer/OnlineInterface.cs | Interface integrada |
| Assets/Game/Scripts/Multiplayer/OnlineSmokeProbe.cs | Teste opt-in em builds de desenvolvimento |
| Assets/Game/Scripts/Multiplayer/OnlineTestDriver.cs | Simulação atual de atraso/perda no Transport, somente desenvolvimento |
| Assets/Game/Scripts/Editor/MultiplayerSetup.cs | Preparação, validação e build pelo menu Bellator |
| Assets/Game/Scripts/Editor/OnlineOfflineRegression.cs | Regressão do fluxo atual de acesso e modo tradicional |
| Assets/Game/Prefabs/Multiplayer/OnlineGladiator.prefab | Cópia exclusiva para rede |
| Assets/Game/Resources/BellatorOnline.asset | Catálogo, prefab e câmera online |
| Assets/Game/Scenes/OnlineArena.unity | Cópia isolada da arena |
| Assets/DefaultNetworkPrefabs.asset | Catálogo gerado pelo Netcode |
| Assets/Game/Documentation/Multiplayer/ | Relatório e evidências de testes |

Os respectivos `.meta` também foram gerados. A Unity pode gerar metadados de execução de testes em `Assets/Resources/` durante builds de desenvolvimento.

### Modificados

| Arquivo | Mudança |
|---|---|
| Character/CharacterHealth.cs | Réplica somente leitura para dano local; aplicação de snapshot |
| Character/PlayerLockOn.cs | Alvo fornecido pela validação online |
| Character/ThirdPersonMotor.cs | Impulso físico de habilidade; permanece inativo quando não usado |
| Classes/PlayerAbility.cs | Mira individual do host e impulso em jogador atingido |
| Classes/PlayerClassController.cs | Contexto PvP/PvE e proteção contra a classe remota alterar a câmera local |
| Classes/GladiatorSelectionController.cs | Confirmação e escolha inicial individuais online |
| Classes/GladiatorSelectionController.Artwork.cs | Voltar da seleção online sem sair da sessão |
| Combat/DamageReceiver.cs | Bloqueio de dano na réplica e feedback de impacto recebido do host |
| Combat/CombatProjectile.cs | Exposição somente leitura do proprietário para efeitos de rede |
| Match/GameInterface.Artwork.cs | Botão Multiplayer e ativação da opção já existente |
| Packages/manifest.json e packages-lock.json | Pacotes e versões resolvidas |
| ProjectSettings/EditorBuildSettings.asset | Inclusão da cena OnlineArena |
| ProjectSettings/ProjectSettings.asset | Referência ao Input Actions preloaded gerada pela Unity durante a build |
| .gitignore | Exclusão do backup |

Os caminhos de scripts da tabela são relativos a `Assets/Game/Scripts/`.

## Testes e evidências

| Verificação | Resultado | Evidência |
|---|---|---|
| Compilação Unity/ILPP e build Windows de desenvolvimento | PASS | Logs/Online-build.txt, build final em 13/11/2026 |
| Conexão de dois processos, roster e seleção pela tela original | PASS | ResultadosRede.txt |
| Guerreiro e Sobrevivente diferentes, animações, dano nos dois sentidos, habilidades, rolamentos, resultado e volta à sala | PASS | Online-render-host/client nos resultados |
| Colosso e Caçador diferentes; enquadramento individual, menu oculto, combate, habilidades, rolamentos e volta à sala | PASS | Online-delivery-host/client nos resultados |
| Atraso de envio de 75 ms, jitter de 20 ms e perda de 3% por participante | PASS | Duelo Colosso/Caçador com o simulador real do Transport; não é teste em duas redes físicas |
| Tentativa de reduzir vida diretamente na réplica do cliente | PASS: rejeitada | REPLICA_DAMAGE_REJECTED True |
| Encerramento abrupto do processo host durante combate | PASS | HOST_DISCONNECT_PASS; cliente encerra a sessão e informa em português que não há migração |
| Acesso atual com TMP, convidado, contas locais isoladas, senha correta/incorreta e elegibilidade do ranking | PASS | OfflineRegression.txt |
| Tradicional: quatro classes, cinco ondas, morte/reinício, portão e travessia física de vitória | PASS | TraditionalRegression.txt |
| Infinito: quatro classes, 35 ondas, habilidades, pontuação e retorno ao tradicional | PASS | InfiniteRegression.txt |
| Comparação de prefabs/cenas/definições originais nas pastas de gameplay | PASS: 48 arquivos, zero alterações | PreservationCheck.txt |
| Inicialização dos serviços Unity e Relay | BLOQUEADO antes da sessão | RelayStatus.txt; PlayerPrefsException |
| Duas builds em computadores e redes diferentes via Relay | PENDENTE | Requer execução externa e acesso aos serviços |
| Sala cheia/inexistente, copiar código e cancelar uma entrada real por Relay | Código preparado; cenário real pendente | Não houve código Relay disponível neste ambiente |

Os testes em processos são automatizados e dirigem os comandos dos jogadores; não substituem uma avaliação humana da sensação de controle. Tentativas de capturar imagens das execuções gráficas em segundo plano não produziram imagens para inspeção, portanto não afirmo aprovação visual manual. A câmera e o bloqueio do Canvas do menu foram verificados também por asserções em execução.

As primeiras execuções encontraram problemas de cobertura no roteiro (seleção enviada logo após conexão e pedidos de habilidade durante outras ações), avisos de inicialização de variável de rede, sobreposição do menu e disputa de Camera.main. O código/roteiro foi corrigido e reexecutado; os resultados PASS acima correspondem às verificações após essas correções. A câmera individual foi incluída explicitamente nas asserções do último duelo. A build foi gerada novamente após a revisão final.

O teste legado de contas em `AccountStageValidation.cs` falhou antes de testar login porque procura `GuestNotice`/`UnityEngine.UI.Text`, enquanto a tela atual usa `GuestRankingNotice`/TextMeshPro. O resultado foi preservado em `LegacyAccountValidation.txt`; os sistemas de contas não foram alterados para acomodar esse teste antigo. A regressão nova verifica a interface atual e usa um diretório temporário isolado para contas.

## Configurações e teste que dependem de você

1. O projeto já possui vínculo com um projeto Unity chamado Bellator. Em Project Settings > Services, confirme que esse é o projeto desejado. Não é necessário inserir credenciais em scripts.
2. No [Unity Dashboard](https://dashboard.unity3d.com/), selecione o mesmo projeto e verifique a disponibilidade de Authentication, Lobby e Relay. Conclua a ativação dos serviços se o Dashboard solicitar. O código usa autenticação anônima; não exige cadastro de um provedor social.
3. Execute a build fora deste ambiente restrito, com acesso normal ao perfil do Windows e à internet. A falha observada em PlayerPrefs impediu validar os serviços; não comprova que Lobby/Relay estejam desativados no Dashboard.
4. Copie **a pasta completa** `Builds/OnlineTest/` para cada computador. Não copie apenas o executável. Também é possível gerar novamente em Bellator > Multiplayer > Gerar build de teste Windows.
5. No primeiro computador: entrar na conta Bellator ou como convidado > Multiplayer > Criar sala > copiar o código. No segundo: Multiplayer > código > Entrar na sala.
6. Cada participante abre Escolher gladiador, confirma e marca Pronto. O host usa Iniciar partida. Teste os dois sentidos de ataque, habilidades, rolamentos, Tab/Aim Lock e encerramento.
7. Repita em redes diferentes e interrompa a conexão de cada participante. Esse teste real entre computadores e via Relay continua obrigatório antes de considerar o online aprovado.

## Limites desta primeira versão

- Não há predição local, reconciliação ou compensação histórica de acertos. O host valida a posição física atual; o cliente sente o tempo de ida/volta somado à interpolação. Os testes simulados verificam integridade da partida, não uma avaliação humana de conforto em conexões ruins.
- O host é confiável para os clientes nesta arquitetura; não é um servidor independente protegido contra manipulação do próprio host.
- Sem migração de host, reconexão automática, partidas públicas, matchmaking, ranking online, 2v2 ou cooperativo.
- A camada de sessão, protocolo, personagem e regras de partida está separada para permitir evolução dessas funcionalidades. Não foram adicionadas agora.

## Referências técnicas consultadas

- [Sessões e Relay no Multiplayer Services SDK](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Autenticação anônima Unity](https://docs.unity.com/en-us/authentication/use-anon-sign-in)
- APIs verificadas também no código instalado em Library/PackageCache, nas versões indicadas acima.
