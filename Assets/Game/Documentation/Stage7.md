# Etapa 7 — Interface completa (com correções de equipamento e seleção)

Unity 6000.3.26f1. Abra `Assets/Game/Scenes/MainMenu.unity` e entre em Play. Essa é a primeira cena do build.

O fluxo atual é **Menu principal → Selecionar personagem → Confirmar seleção → Menu principal → Jogar → Arena**. Jogar funciona sem escolha anterior, usando o Guerreiro. A confirmação não inicia a partida; a escolha permanece durante a navegação entre cenas.

Cada classe tem equipamento fixo: Sobrevivente com espada curta; Colosso com espada pesada e escudo; Guerreiro com espada e escudo; Caçador somente com besta. As teclas numéricas não trocam equipamento nem escudo.

O HUD nativo mostra vida, resistência, arma, ondas e inimigos; munição, recarga da besta e mira aparecem para armas à distância. As reservas continuam ilimitadas. O painel visual da habilidade especial foi removido, preservando Q, consumo de resistência, efeitos, restrições e recarga. A linha de controles é discreta e específica para a classe.

Esc / P pausa e retoma. As configurações abertas na pausa mantêm o combate congelado; Esc nessa tela fecha apenas as configurações. Continuar, Reiniciar e Menu principal permanecem disponíveis. Vitória continua exigindo completar cinco ondas e atravessar o portão liberado. Derrota oferece reinício e menu principal.

As configurações de volume, sensibilidade, qualidade e modo de tela continuam salvas localmente. Modo de tela fica disponível no executável desktop compatível; no Editor, o controle informa a indisponibilidade. Sair encerra o executável ou o Play Mode no Editor.

A interface usa recursos nativos da Unity, com marrom, bege, vermelho escuro e dourado, sem imagens externas. Os sistemas anteriores de combate, personagens, IA, ondas, navegação e portão foram preservados.

Consulte **Corrections.md** para a relação dos arquivos alterados, o funcionamento atual e como repetir os testes. **CorrectionsValidation.txt**, **Stage5Validation.txt** e **Stage6Validation.txt** registram os testes atuais. O antigo **Stage7Validation.txt** é histórico e descreve a interface anterior às correções. A opção de teste da etapa 7 agora executa a validação atual.

Não foi iniciada a etapa 8.
