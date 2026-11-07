# Bellator — novas interfaces, 07/11/2026

Abra `Assets/Game/Scenes/MainMenu.unity` e pressione Play na Unity 6000.3.26f1. Os controladores existentes constroem as telas com componentes uGUI reais. A cena de seleção mantém os modelos e as referências existentes.

## Telas e fluxos

- Menu: Jogar, Personagens, Ranking, Configurações, Créditos e Sair.
- Login: usuário ou e-mail, senha mascarada, revelar/ocultar, lembrar somente o usuário, ajuda de senha, cadastro, convidado e voltar.
- Cadastro: usuário, e-mail, senha e confirmação, validação real no armazenamento local existente e retorno ao login.
- Modos: Tradicional e Ondas Infinitas mantêm os mesmos valores `GameMode.Story` e `GameMode.Infinite`. Multiplayer aparece desabilitado, em desenvolvimento.
- Personagens: Sobrevivente, Colosso, Guerreiro e Caçador vêm do catálogo; as prévias usam os modelos reais da cena. Lista, setas e teclas 1–4 apenas selecionam. Confirmar inicia a arena somente após escolher um modo; quando aberto por Personagens, salva e volta ao menu. Voltar retorna aos modos ou ao menu conforme a origem. Nenhum atributo, barra ou painel de habilidade foi adicionado.
- Ranking: posições, jogadores, gladiadores, pontos, ondas e tempo vêm de `LocalRankingManager`. Dez espaços permanecem vazios quando não há registros. Convidados continuam bloqueados pelas regras existentes.

## Identidade visual e organização

Os PNG/JPEG recebidos estão preservados em `Source/`. As antigas imagens integrais estão em `Source/PreviousPresentation/`, fora de Resources. Nenhuma captura completa é usada como interface.

`Resources/BellatorUI/Elements/` contém somente fundo do Coliseu, fundo de seleção com pedestal, logo original com transparência, três ilustrações extraídas dos cartões de modos e fonte de títulos. Os dois fundos foram preparados a partir das referências, removendo os elementos de interface. A logo foi extraída exclusivamente do JPEG original de 720×415: o RGB dos elementos é preservado, o branco vira transparência e as bordas quase brancas recebem alpha suave. B, tipografia, cores e proporções não foram redesenhados. O JPEG original permanece intacto. Um brilho separado no céu melhora a leitura da palavra preta.

Os controles e textos são independentes: Canvas, CanvasScaler, TMP, Button, TMP_InputField, Toggle, imagens e molduras nativas. A mesma aparência central é aplicada por `BellatorUI` e `BellatorButtonHover`; há também o prefab `Prefabs/BellatorButton.prefab`. Os fundos e ilustrações usam compressão BC7 no desktop; a logo usa importação sem perdas; sem mipmaps, wrap Clamp e limite 2048.

Botões: escuro no estado normal; vermelho somente sob o ponteiro; vermelho mais escuro pressionado; cinza desabilitado. Foco do teclado e seleção do personagem usam marcas douradas. O foco não mantém o preenchimento vermelho.

Os menus ficam num palco 16:9 de referência 1920×1080 contido na tela. Em 4:3 e ultrawide há margens para preservar o enquadramento e evitar deformações/cortes. O HUD mantém sua escala anterior. A prévia 3D libera sua RenderTexture ao sair da seleção.

A fonte Cinzel acompanha licença SIL OFL em `Fonts/OFL.txt`.

## Autenticação

O projeto continua offline. `LocalAccountStore` recebeu uma extensão compatível para e-mail no perfil e busca de login por usuário/e-mail. Perfis antigos sem e-mail permanecem válidos. E-mails inválidos e duplicados são rejeitados. Senhas continuam usando o PBKDF2-SHA256, salt e número de iterações existentes, em arquivo separado; nunca são salvas no PlayerPrefs. Lembrar de mim guarda apenas o nome do usuário. E-mail não é verificado nem recebe mensagens: recuperação por e-mail exige um serviço futuro, e a ajuda informa essa limitação.

## Manutenção e verificação

- `Bellator/UI/Configure Original Artwork`: configura importação, gera extrações sem alterar fontes originais e salva o prefab de botão.
- `Bellator/UI/Test Original Screens in Play Mode`: executa testes de navegação, controles, autenticação real, modelos, modos e ranking em Play Mode. As contas e os resultados de teste ficam isolados em Temp. Preferências de usuário e resolução do Game View são restauradas.
- Evidências e relatório: `Assets/Game/Documentation/BellatorUI/`.
- Backup anterior às alterações: `BalanceBackups/UI-20261107-NewScreens/`.

Não houve alteração de combate, movimentação, IA, habilidades, regras de ondas, modelos ou câmera da partida.
