# Bellator — interfaces com as imagens originais

Abra `Assets/Game/Scenes/MainMenu.unity` e entre em Play. As interfaces são montadas em tempo de execução pelos controladores existentes, sem reconstruir cenas nem substituir prefabs.

## Fluxos

- **Acesso:** Entrar utiliza `LocalAccountStore.LoginAsync`; Cadastrar utiliza `CreateAsync`, com confirmação de senha. Jogar como convidado abre o menu. Voltar abre o menu sem autenticar; Jogar solicita o acesso novamente.
- **Menu:** Jogar → modos → gladiadores → Continuar → arena. Personagens → gladiadores → Continuar salva a escolha e retorna ao menu. Ranking, Configurações e Créditos têm retorno ao menu. Sair fecha o executável; no Editor, encerra Play.
- **Modos:** o tradicional continua sendo `GameMode.Story`, com as cinco ondas existentes. Ondas Infinitas mantém `GameMode.Infinite`, pontuação e persistência existentes. Voltar da seleção de gladiador iniciada por Jogar retorna aos modos.
- **Gladiadores:** os quatro botões e teclas 1–4 atualizam o modelo 3D, nome, descrição, atributos, equipamento e habilidade. Clicar no espaço da habilidade exibe descrição, recarga e custo no rodapé. Esc volta. Trocar de personagem nunca carrega a arena.

O sistema de autenticação existente é **local, por nome de usuário**. Não existia cadastro por e-mail nem recuperação de senha; a interface informa isso, sem simular serviços online. Lembrar usuário salva apenas o nome, nunca a senha. Convidados podem consultar o ranking, mas continuam impedidos de registrar resultados pelas regras existentes.

## Imagens e apresentação

`Resources/BellatorUI/` contém cópias integrais das quatro imagens de `C:/Users/dizzy/Downloads/Menu`:

| Arquivo importado | Tela |
| --- | --- |
| MainMenu.png | Menu principal |
| Login.png | Login e cadastro |
| GameModes.png | Escolha do modo |
| CharacterSelection.png | Escolha de gladiador |

Importação: Sprite (2D and UI), Single, sem mipmaps, sem compressão, limite 2048. Canvas dos menus: referência 1920×1080, escala proporcional à resolução e âncoras normalizadas. O HUD da partida mantém sua escala anterior.

Os botões desenhados recebem áreas clicáveis transparentes. Campos e textos variáveis usam TextMeshPro. Máscaras cobrem somente os elementos estáticos que precisam mudar. Os cartões normal/selecionado são recortes em memória da imagem original. Nenhuma nova imagem foi gerada.

A prévia usa os modelos e armas já montados em `ClassPreviews` na cena de seleção, com câmera própria da cena e RenderTexture transparente. Somente o modelo escolhido fica ativo. A textura é liberada ao sair. A vida, velocidade, defesa, equipamentos e habilidades vêm do catálogo existente. Como não existe atributo independente de força, a barra Força mostra o maior multiplicador de dano da classe (corpo a corpo/distância), com unidade ×; as barras são normalizadas pelo maior valor do catálogo.

## Arquivos

Modificados:

- `Assets/Game/Scripts/Match/GameInterface.cs`: integração das telas, escala dos menus e acesso ao fluxo Jogar.
- `Assets/Game/Scripts/Match/GameInterface.Access.cs`: apresentação de autenticação com TMP, preservando o armazenamento real.
- `Assets/Game/Scripts/Match/GameInterface.Infinite.cs`: encaminha modos para seleção, em vez de carregar a arena diretamente.
- `Assets/Game/Scripts/Classes/GladiatorSelectionController.cs`: confirmação respeita a intenção de navegar ou jogar.

Criados:

- `Assets/Game/Scripts/Match/BellatorUI.cs`: componentes compartilhados de apresentação.
- `Assets/Game/Scripts/Match/GameInterface.Artwork.cs`: menu, modos e créditos. Os nomes da equipe não foram fornecidos; o texto dos créditos está disponível aqui para edição.
- `Assets/Game/Scripts/Classes/SelectionNavigation.cs`: intenção de navegação entre cenas.
- `Assets/Game/Scripts/Classes/GladiatorSelectionController.Artwork.cs`: apresentação da seleção e prévia 3D.
- `Assets/Game/Scripts/Editor/BellatorUISetup.cs`: configuração dos sprites e importação dos recursos oficiais TMP.
- `Assets/Game/Scripts/Editor/BellatorUIValidation.cs`: testes integrados em Play Mode.
- `Assets/TextMesh Pro/`: recursos oficiais essenciais do pacote instalado.
- `Assets/Game/Documentation/BellatorUI/`: relatório e capturas dos testes.

**Cenas e prefabs:** nenhum foi criado ou regravado pela implementação. Foram reutilizadas `MainMenu`, `GladiatorSelection` e `RomanArena`. Os controles antigos da seleção são ocultados apenas durante Play; suas referências serializadas ficam preservadas. A seleção usa um Canvas e o EventSystem existente.

Backup anterior às alterações: `BalanceBackups/UI-20261027/Assets/Game/`, contendo os quatro scripts modificados e as duas cenas de menu/seleção.

## Como testar

1. Abra MainMenu e entre em Play. Teste voltar, cadastrar com senhas diferentes, cadastrar corretamente, trocar conta e entrar novamente. Teste uma senha inválida antes da válida.
2. Entre como convidado. Abra ranking, configurações e créditos; use Voltar em cada tela.
3. Abra Personagens. Troque pelos quatro gladiadores. Confirme que apenas a prévia e os dados mudam. Continuar deve salvar a escolha e voltar ao menu.
4. Abra Jogar e escolha um modo. Voltar na seleção deve retornar aos modos. Entre novamente, selecione um personagem e use Continuar para iniciar a arena.
5. Repita com Ondas Infinitas. Uma conta autenticada deve registrar o resultado ao morrer; convidados não devem ter resultado salvo.
6. Na Game View, confira 1280×720, 1920×1080 e 2560×1440. Verifique alinhamento dos cliques e legibilidade.
7. Confira o Console. O teste integrado está em **Bellator → UI → Test Original Screens in Play Mode**. Ele usa contas/ranking temporários, verifica as áreas de clique pelo EventSystem, registra erros e gera `Assets/Game/Documentation/BellatorUI/Validation.txt` e capturas. A cena MainMenu é reaberta no término.

Não foram alterados scripts de movimentação, combate, IA, câmera da partida, ondas, pontuação, armazenamento de contas ou ranking.
