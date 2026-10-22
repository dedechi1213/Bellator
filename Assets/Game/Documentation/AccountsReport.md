# BELLATOR — Contas locais e modo convidado

Implementação em Unity 6000.3.26f1, em 21/10/2026.

## Funcionalidades

- Antes do menu, a tela BELLATOR apresenta **Entrar**, **Criar conta** e **Jogar como convidado**.
- A observação do convidado aparece abaixo do botão, em um elemento separado e com fonte menor, mantendo tamanho mínimo legível.
- Cadastro com nome único, senha e confirmação; erros claros para dados vazios, nomes inválidos, duplicação e confirmação diferente.
- Os nomes são normalizados, desconsiderando espaços nas extremidades, variantes Unicode equivalentes e diferença entre maiúsculas/minúsculas na identificação. O nome exibido preserva a capitalização escolhida.
- Cadastro bem-sucedido inicia a sessão e abre o menu. Login válido recupera o mesmo identificador interno do perfil.
- Login inválido usa a mesma mensagem para usuário desconhecido e senha incorreta.
- O menu identifica a conta conectada ou o modo convidado. **Trocar conta** encerra a sessão e retorna à tela de acesso, preservando os perfis.
- A troca de sessão também é bloqueada no código enquanto uma cena de partida contém um GameManager.
- História e Infinito continuam disponíveis para contas e convidados. A sessão não altera ondas, equipamentos, IA, combate ou habilidades.
- Contas registram automaticamente o resultado do Infinito com o nome e o identificador interno do perfil; não há novo campo de nome.
- Convidados recebem pontuação e estatísticas normalmente e consultam o ranking, mas não registram resultados. A tela final informa: “Crie uma conta para registrar seus recordes no ranking.”
- O bloqueio do convidado existe em GameManager, ScoreManager e LocalRankingManager. A identidade é capturada ao iniciar a partida e vinculada à sessão que a iniciou.
- O ranking continua com dez resultados compartilhados entre as contas locais. Recordes pessoais são consultados pelo identificador do perfil.
- Cenas de menu sem câmera recebem uma câmera de apresentação em tempo de execução, para renderizar a interface também no player URP compilado.

## Senhas e armazenamento

O sistema é de **perfis locais**, sem servidor ou autenticação online. As credenciais usam PBKDF2-HMAC-SHA256 com **600.000 iterações**, salt aleatório individual de **16 bytes** e verificador derivado de **32 bytes**. Esses parâmetros seguem a orientação de PBKDF2 da [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html).

As senhas originais não são gravadas em JSON, PlayerPrefs, relatórios ou arquivos de autenticação. A derivação ocorre em uma tarefa de fundo, com a interface indicando que a operação está em andamento. A comparação dos verificadores de comprimento fixo usa acumulação de diferenças, sem encerrar no primeiro byte diferente.

Dentro de `Application.persistentDataPath`:

| Dados | Local |
|---|---|
| Perfis e credenciais locais | `BellatorAccounts/<identificador do perfil>/` |
| Perfil público | `profile.json` — versão, ID, nome e data de criação UTC |
| Credenciais | `credentials.bin` — versão, ID, nome normalizado, parâmetros, salt e verificador derivado |
| Recuperação | `profile.json.bak` e `credentials.bin.bak` |
| Ranking compartilhado | `BellatorInfiniteRanking.json` e cópia de recuperação existente |
| Configurações gerais | Sistema GameSettings/PlayerPrefs existente, sem credenciais |

Uma conta é preparada em uma pasta temporária própria e publicada por renomeação da pasta somente quando os dois arquivos e suas cópias estão completos. A criação usa exclusão mútua por arquivo e verifica novamente a unicidade antes da publicação. Nomes de usuário nunca são usados como caminhos de arquivos.

Arquivos ausentes resultam em lista vazia. Arquivos inválidos tentam usar a cópia de recuperação. Se uma identidade permanecer danificada, contas válidas ainda podem entrar e convidados continuam disponíveis; novos cadastros são suspensos para evitar recriar nomes cuja unicidade não possa ser verificada. Uma falha de gravação não sobrescreve os perfis existentes.

A senha não é lembrada entre execuções: ao reabrir o jogo, o jogador escolhe entrar ou jogar como convidado.

## Compatibilidade do ranking

Os resultados anteriores são mantidos, inclusive os que não possuem identificador de perfil. Eles não são associados automaticamente a contas por semelhança de nome. Somente novos resultados vinculados a uma sessão de conta podem ser gravados.

Alterar o nome mostrado na interface ou passar outro nome ao método legado de salvamento não muda o titular da partida. Resultados de uma sessão antiga não podem ser registrados por outra sessão, mesmo que o jogador entre novamente na mesma conta. Os campos de identificação usados na decisão de gravar são capturados no início da partida, e o token de sessão não é persistido no JSON.

## Arquivos criados

- `Assets/Game/Scripts/Match/LocalAccountStore.cs`
- `Assets/Game/Scripts/Match/PlayerSession.cs`
- `Assets/Game/Scripts/Match/GameInterface.Access.cs`
- `Assets/Game/Scripts/Editor/AccountStageValidation.cs`
- Este relatório e as evidências de testes em `Assets/Game/Documentation/`.

Os arquivos `.meta` correspondentes são gerados pelo Unity.

## Arquivos modificados

- `Assets/Game/Scripts/Match/GameInterface.cs`
- `Assets/Game/Scripts/Match/GameInterface.Infinite.cs`
- `Assets/Game/Scripts/Match/GameManager.cs`
- `Assets/Game/Scripts/Match/ScoreManager.cs`
- `Assets/Game/Scripts/Match/MatchStatistics.cs`
- `Assets/Game/Scripts/Match/LocalRankingManager.cs`
- `Assets/Game/Scripts/Match/RankingPersistenceProbe.cs`
- `Assets/Game/Scripts/Editor/InfiniteModeValidation.cs`
- `Assets/Game/Scripts/Editor/CorrectionsValidation.cs`
- `Assets/Game/Scripts/Editor/MatchStageValidation.cs`
- `Assets/Game/Scripts/Editor/ClassStageValidation.cs`

As validações existentes foram adaptadas à tela de acesso e à substituição do nome livre por identidade de conta. Não foi necessário duplicar combate, criar outra arena ou reconstruir as cenas.

## Testes realizados

| Teste | Resultado |
|---|---|
| Criar conta, confirmação e campos vazios | PASS, incluindo entrada automática no menu |
| Nome já existente | PASS, incluindo diferença de maiúsculas/minúsculas e espaços nas extremidades |
| Login correto | PASS, mantendo o identificador interno |
| Senha incorreta e usuário inexistente | PASS, mesma mensagem genérica |
| Persistência de perfis e credenciais | PASS em dois processos independentes do player Windows |
| Infinito com conta | PASS, registro automático com conta/ID/estatísticas e sem duplicação |
| Infinito como convidado | PASS, pontuação e estatísticas preservadas, nenhum registro gravado |
| Ranking consultado pelo convidado | PASS, inclusive resultados de contas existentes |
| Troca entre duas contas | PASS, sem transportar identidade, seleção de classe ou autorização de resultados |
| Troca durante partida | PASS, rejeitada pelos métodos da sessão |
| Nome/ID de outro perfil usado na interface/resultado | PASS, não concede autorização de registro |
| História | PASS, cinco ondas e vitória pela travessia do portão com as quatro classes |
| Infinito prolongado e classes | PASS, 35 ondas sucessivas, quatro classes, Q e equipamentos fixos |
| Regressão de combate/configurações | PASS com as quatro classes |
| Arquivos ausentes, corrompidos, backups e gravação indisponível | PASS, incluindo continuidade do acesso como convidado |
| Credenciais persistidas | PASS, arquivos sem senha original, 600.000 iterações e salts distintos entre contas |
| Observação do convidado | PASS, elemento separado abaixo do botão, fonte menor e tamanho mínimo legível; revisão visual em 1920×1080 no Editor e 1280×720 no executável |
| Player Windows | PASS, compilação com zero erros e acesso inicial validado em janela normal |

As contas fictícias e os arquivos de ranking dos testes ficam em subpastas de `Temp`; não são adicionados aos dados pessoais da instalação.

Os relatórios `AccountStageValidation.txt`, `InfiniteModeValidation.txt`, `Stage6Validation.txt` e `CorrectionsValidation.txt` registram `RESULT: PASS`. A verificação de persistência compilou o player Windows e usou um processo para criar a conta e registrar o resultado, seguido por outro processo para autenticar e recuperar o mesmo ID e dados do ranking.

O relatório `AccountBuildValidation.txt` consolida a compilação Windows com zero erros, a gravação e leitura em processos separados (ambos encerrados com código 0) e a apresentação em janela normal a 1280×720. O player de desenvolvimento foi gerado em `Temp/BellatorInfinitePlayer/`; seus auxiliares de teste são inativos em jogos normais e excluídos de compilações sem Development Build.

Imagens de conferência: `Accounts_Access.png`, `Accounts_Create.png`, `Accounts_Menu.png`, `Accounts_AccountResults.png`, `Accounts_GuestResults.png` e `Accounts_Access1280x720.png`. Os relatórios/imagens das regressões anteriores também foram atualizados. O conteúdo das três cenas foi comparado à cópia anterior e permaneceu idêntico.

## Problemas encontrados

- Uma validação antiga precisou importar o namespace da sessão; a compilação foi corrigida antes dos testes.
- A captura do player iniciado oculto produziu uma imagem preta neste ambiente. A captura em uma janela normal foi executada e inspecionada com sucesso em 1280×720. Também foi adicionada uma câmera de apresentação às cenas de menu sem câmera, para garantir um contexto de renderização do player URP.
- Uma repetição do teste perdeu foco/avanço de quadros durante a conferência do executável. A validação foi ajustada para focar o Game View, lidar com pausa do Editor e emitir diagnóstico de tempo limite; a conferência do player e a rodada final no Editor foram separadas.
- O projeto não é um repositório Git. Antes das alterações, scripts e cenas foram copiados para `Temp/BellatorAccountsBackup-<data e hora>`. A pasta Temp pode ser limpa pelo Unity.
- O pacote existente de AI Assistant continuou emitindo mensagens próprias de consulta/licenciamento no Editor. Nenhum pacote foi removido ou alterado.

## Como testar

1. Abra `Assets/Game/Scenes/MainMenu.unity` e entre em Play Mode.
2. Confira a observação abaixo de Jogar como convidado. Entre em Criar conta, preencha nome, senha e confirmação e confirme. O menu deve identificar a conta automaticamente.
3. Clique em Trocar conta. Tente cadastrar o mesmo nome usando outra capitalização: o cadastro deve ser recusado.
4. Entre com senha incorreta e com usuário inexistente: a mensagem deve ser a mesma. Depois entre com as credenciais corretas.
5. Jogue o Infinito e morra. Confira o registro automático com o nome da conta, sem campo para digitar outro nome.
6. Volte ao menu, troque de conta e entre como convidado. Consulte o ranking e jogue os dois modos.
7. Ao morrer no Infinito como convidado, confira a pontuação e estatísticas, a mensagem de cadastro e a ausência de campo de nome ou registro de resultado.
8. Crie uma segunda conta, alterne entre ambas e confira os nomes/recordes pessoais. A seleção de classe volta ao padrão ao trocar de sessão, para não transportar escolhas entre perfis.
9. Feche/reabra o jogo compilado e entre novamente. Os perfis e os resultados de contas devem permanecer disponíveis.

Validações automatizadas disponíveis no menu Bellator: **Test Local Accounts and Guest Mode**, **Test Infinite Mode and Local Ranking**, **Test Stage 6 Complete Matches in Play Mode** e **Test Fixed Equipment and Selection Corrections**. Execute uma por vez.

## Limitações

Perfis locais com derivação de senha não equivalem a autenticação de servidor. Alguém com controle dos arquivos ou do executável pode alterar dados locais. O sistema não implementa autenticação online, recuperação de senha por email, sincronização, exclusão ou renomeação de contas, que não foram solicitadas.
