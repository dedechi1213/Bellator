# Relatório de implementação — Bellator

Data: 07/11/2026. Projeto: C:/unity/Bellator. Unity: 6000.3.26f1.

## Entrega

Foram refeitas as seis telas solicitadas: menu principal, login, cadastro, modos, seleção de personagens e ranking. Configurações e créditos também receberam o estilo compartilhado dos controles, preservando suas funções.

A apresentação é construída pelos controladores existentes ao entrar em Play; as cenas e os modelos foram preservados. Abra Assets/Game/Scenes/MainMenu.unity e pressione Play.

## Telas e integração

| Tela | Implementação |
| --- | --- |
| Menu | Seis ações reais: jogar, personagens, ranking, configurações, créditos e sair. |
| Login | TMP_InputField de usuário/e-mail e senha; revelar/ocultar; lembrar somente nome; ajuda de senha; cadastro; convidado e voltar. |
| Cadastro | Usuário, e-mail, senha e confirmação; rejeição de e-mail inválido/duplicado e senhas diferentes; mesmo armazenamento local de credenciais. |
| Modos | Tradicional e Ondas Infinitas preservados; cada escolha segue para personagens. Multiplayer desabilitado e identificado como em desenvolvimento. |
| Personagens | Catálogo e quatro modelos 3D reais: Sobrevivente, Colosso, Guerreiro e Caçador. Lista, setas, teclas 1–4, indicação dourada, confirmar e voltar. Sem barras, atributos ou painel de habilidades. Escolher nunca inicia uma partida. |
| Ranking | Dados reais de LocalRankingManager: posição, jogador, gladiador, pontos, maior onda e tempo. Espaços sem registros ficam vazios. Regras existentes impedem convidados de salvar. |

## Recursos visuais

- Fontes das imagens recebidas preservadas intactas em Assets/UI/Bellator/Source/.
- Logo extraída exclusivamente do JPEG original recebido, sem redesenho ou geração. A transparência afeta o branco e a borda quase branca; os pixels RGB dos elementos são copiados. As proporções são mantidas por Image.preserveAspect. O hash SHA256 da cópia original confere com o arquivo recebido: 5F39FF5C1D71BA54B7EB572DC76E2251AFBD92FD661571957D8C7C94FF4AB570.
- Dois fundos sem controles preparados a partir das referências: Coliseu comum e seleção com pedestal. Os espaços antes cobertos pela interface foram preenchidos durante a preparação. Não se trata de reprodução idêntica de cada pixel. Detalhes e prompts estão em Assets/UI/Bellator/AssetPreparation.md.
- Ilustrações dos modos extraídas dos cartões enviados. Nenhum gladiador foi inventado.
- Controles, ícones simples, molduras e ornamentos são componentes reais, separados dos fundos. A fonte Cinzel possui licença SIL OFL incluída.
- Botões compartilham BellatorUI/BellatorButtonHover e o prefab Assets/UI/Bellator/Prefabs/BellatorButton.prefab. Normal escuro, hover vermelho, pressionado vermelho mais escuro, desabilitado cinza. Seleção e foco do teclado são dourados, sem vermelho permanente.
- Canvas de referência 1920×1080, com palco 16:9 contido e margens quando necessário. Fundo não é deformado para preencher telas de outras proporções. HUD da partida mantém sua escala.
- Texturas fotográficas com BC7 desktop e logo sem compressão destrutiva, sem mipmaps, Clamp, máximo 2048. Imagens integrais antigas foram retiradas de Resources e preservadas em Source/PreviousPresentation.

## Sistemas preservados

Autenticação continua local, usando o PBKDF2-SHA256, salt e 600.000 iterações existentes. O perfil recebeu e-mail opcional para compatibilidade com contas antigas; o formulário novo pede e-mail. Login local aceita usuário ou e-mail. Nenhuma senha é salva em PlayerPrefs. Não existe serviço online ou envio de mensagens.

As cenas MainMenu.unity e GladiatorSelection.unity permaneceram idênticas aos backups por comparação de hash. Não foram alterados combate, IA, movimentação, habilidades, modelos, regras de ondas ou câmera da partida nesta implementação. Alterações que já existiam no projeto antes da solicitação foram preservadas.

## Verificação

A validação é executada em Play Mode real pela Unity e aciona Button através de eventos de clique, com verificação de raycast no centro dos controles. Os estados de ponteiro, pressão, saída e foco são exercitados separadamente. Credenciais e pontuações de teste ficam isoladas em Temp; preferências e resolução do Game View são restauradas.

O resultado final detalhado está em Validation.txt, nesta pasta, acompanhado das capturas PNG. A validação cobre autenticação real válida/inválida, confirmação divergente, alternância de senha, lembrar usuário, compatibilidade com conta antiga, e-mail inválido e duplicado, convidado, navegação e retorno, quatro modelos reais, seleção sem início automático, modos tradicional e infinito, primeira onda real de teste, morte, gravação única no ranking e exclusão de convidados. Resoluções: 1024×768, 1280×720, 1920×1080, 2560×1080 e 2560×1440. Não foi gerado um novo executável distribuível.

Resultado final: PASS em Play Mode. Oito contas adicionais foram criadas, reabertas e autenticadas por e-mail; o ciclo final não registrou Error/Exception/Assert. Houve uma falha isolada de gravação em uma rodada intermediária, sem causa confirmada; ela não se repetiu na rodada seguinte nem nas oito criações adicionais. O armazenamento preservou os perfis existentes e mostrou a mensagem de erro adequada. O diagnóstico de falhas fica disponível em LocalAccountStore.LastError.

## Pendências e limites

- Multiplayer não tem sistema de rede e permanece indisponível.
- Recuperação/verificação de e-mail exige serviço futuro; a interface informa que a recuperação não existe para os perfis locais.
- Nomes e créditos da equipe continuam aguardando os dados do autor.
- Não foi enviada uma imagem específica da nova tela de ranking; foi aplicada a mesma identidade visual e mantidos dez espaços dinâmicos sem resultados inventados.

## Arquivos e backup

Inventário completo de scripts, recursos, realocações e evidências: ChangedFiles.txt nesta pasta. Guia de manutenção: Assets/UI/Bellator/README.md.

Backup anterior às alterações: BalanceBackups/UI-20261107-NewScreens/. Inclui scripts, cenas, README e imagens anteriores.
