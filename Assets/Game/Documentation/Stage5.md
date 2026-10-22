# Etapa 5 — classes e habilidades

Unity 6000.3.26f1. Abra `Assets/Game/Scenes/GladiatorSelection.unity` e pressione Play. Clique no cartão de uma classe, em **Confirmar** e em **Iniciar partida**. As teclas 1–4 também selecionam cartões. A escolha é mantida durante a transição para RomanArena; abrir a arena diretamente usa Guerreiro como padrão. F1 volta à seleção. Alterar a classe exige nova confirmação.

| Classe | Vida | Resistência | Caminhar / correr | Dano corpo a corpo / distância | Redução de dano | Equipamentos |
| --- | --- | --- | --- | --- | --- | --- |
| Sobrevivente | 70 | 120 | 4,3 / 7,5 | 0,75x / 0,80x | 5% | Espada curta |
| Colosso | 180 | 80 | 2,4 / 4,1 | 1,50x / 0,80x | 30% | Espada pesada e escudo |
| Guerreiro | 120 | 100 | 3,5 / 6,2 | 1,00x / 1,00x | 18% | Espada e escudo |
| Caçador | 80 | 110 | 4,0 / 7,0 | 0,85x / 1,45x | 7% | Besta e espada curta |

Q ativa a habilidade. E executa ataque pesado (configurável no CombatInput); Q deixa de ser a antiga tecla de ataque pesado. Mouse esquerdo ataca/dispara; mouse direito defende quando houver escudo. R recarrega a besta, 1/2 alternam os equipamentos disponíveis e 5 alterna o escudo. WASD, Shift, Espaço e mouse preservam movimento, corrida, esquiva e câmera. T restaura o treino, inimigos, vida, resistência e recarga da habilidade. Esc libera o cursor.

- **Passos Desesperados:** custo 24, duração 5 s, recarga 14 s. Velocidade 1,45x; esquiva custa 14 em vez de 28, percorre 4,86 em vez de 3,6 unidades e reduz sua recarga de 0,8 para 0,4 s.
- **Golpe Devastador:** custo 40, recarga 12 s, preparação 0,6 s e recuperação 0,5 s. Raio 3,4; dano base 48 multiplicado pelo dano da classe (72 inicialmente). Acerta uma vez por alvo, respeita facções e paredes e empurra inimigos vivos usando seus controladores físicos.
- **Postura de Combate:** custo 25, duração 6 s, recarga 16 s. Dano 1,35x, redução de dano adicional de 15 pontos percentuais e custo dos bloqueios 0,60x.
- **Disparo Preciso:** custo 30, recarga 10 s, preparação 0,4 s e recuperação 0,25 s. Exige besta equipada e carregada. Multiplica o disparo por 3 (182,7 de dano inicial), consome a carga e inicia a recarga normal após o disparo.

O círculo de esferas coloridas indica a ativação; o HUD informa duração, disponibilidade e recarga. Não é possível ativar durante esquiva, bloqueio, guarda quebrada, ataque, recarga, outra habilidade, falta de resistência ou morte. Efeitos temporários são removidos ao terminar, morrer, trocar classe ou restaurar treino.

## Arquivos e ajustes

- `Scripts/Classes`: definições, atributos por ator, estado da escolha, aplicação da classe, habilidade, apresentação da seleção e pose provisória.
- `ScriptableObjects/Classes`: quatro configurações de classe, catálogo e armas iniciais independentes das armas de treino anteriores.
- `ScriptableObjects/Abilities`: quatro habilidades ajustáveis, com custo, duração, recarga e parâmetros do efeito.
- `Prefabs/Classes`: quatro aparências geométricas substituíveis, com ossos e encaixes das mãos. Não contêm lógica de movimento/combate.
- `Materials/Classes`: cores e materiais da seleção.
- `Scenes/GladiatorSelection.unity`: seleção com UI, quatro modelos, câmera e iluminação próprias.
- `Scenes/RomanArena.unity` e `Prefabs/Characters/PrimitivePlayer.prefab`: componentes de classe e habilidade adicionados ao jogador existente.
- `Scripts/Editor/ClassStageBuilder.cs`: criação inicial protegida contra substituição da cena existente.
- `Scripts/Editor/ClassStageValidation.cs`: testes de Play Mode pelo menu Bellator; resultados em `Documentation/Stage5Validation.txt`.

Atributos reutilizam CharacterHealth, CharacterStamina, ShieldGuard, DamageReceiver, CombatQueries e os projéteis existentes. A redução de dano da classe é aplicada antes do escudo; facções continuam impedindo fogo amigo. A força do golpe de área usa EnemyNavigation/CharacterController, preservando colisões e sincronização do NavMesh. Os inimigos mantêm os atributos anteriores quando não possuem ActorAttributes.

Build Settings inicia pela seleção, seguida da arena, mantendo SampleScene e as configurações de entrada existentes. Nenhum pacote, modelo externo, onda ou progressão foi adicionado. Valores são iniciais e podem ser ajustados nos ScriptableObjects pelo Inspector.

## Observações do ambiente

O Unity AI Assistant já instalado continua registrando mensagens de assinatura/entitlements; o jogo não depende desses serviços. O Console mantém também o histórico de tentativas de compilação anteriores às correções. Use os relatórios finais de validação para conferir o resultado de cada etapa.

## Resultado final de validação

Testes reais de Play Mode aprovados em Unity 6000.3.26f1: etapa 5 (quatro transições seleção/arena, confirmação, atributos, proporções/colisores, velocidade, equipamentos, mapeamento Q/E, habilidades, custo, recarga, expiração, falta de resistência, cancelamento por morte e retorno à seleção). O golpe de área atingiu dois inimigos uma vez e deslocou fisicamente um deles. O disparo especial atingiu um alvo com projétil real e exigiu recarga. As prévias de seleção foram renderizadas e inspecionadas, incluindo os escudos.

Regressões das etapas 2, 3 e 4 também aprovadas: movimento/colisão/câmera, combate/escudo/projéteis e navegação/ataques/morte dos três inimigos. Os testes anteriores restauram apenas durante suas execuções o jogador de treino original, para manter suas medidas de referência; o jogo normal usa os atributos da classe escolhida.

Relatórios: `Stage2Validation.txt`, `Stage3Validation.txt`, `Stage4Validation.txt`, `Stage5Validation.txt`. Imagens: `Stage5_Selection.png` e `Stage5_Survivor/Colossus/Warrior/Hunter.png`.
