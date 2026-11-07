# Bellator — movimentação e animação na arena

Refinamento de 4 de novembro de 2026 para Unity 6000.3.26f1. O [registro inicial](../../../../BalanceBackups/Movement-20261104/Baseline.md) descreve os controles e os sistemas encontrados. O backup inclui as versões anteriores dos scripts, malhas, clipes e prefabs abrangidos pelo trabalho.

## Personagens

| Personagem | Resultado |
|---|---|
| Guerreiro | Piloto da passada, da corrida, das laterais, do recuo e do rolamento. A túnica acompanha a coxa durante a flexão e o cotovelo e o joelho têm pesos mais graduais. |
| Sobrevivente | Usa os clipes comuns com as próprias velocidades, proporções e ataques. A túnica leve acompanha mais o movimento das pernas. |
| Colosso | Mantém postura, equipamento e tempos de ataque próprios. As curvas de passada usam um centro mais baixo e a túnica sob a armadura acompanha menos as pernas. |
| Caçador | Locomoção com postura própria de besta. O disparo amostra a pose de soltura antes de ler o ponto de saída do projétil. |
| Gladiador comum | Rig com tronco, cabeça, cotovelos, joelhos e tornozelos animados. Persegue com Blend Tree e usa clipes para ataque leve, defesa, impacto e morte. |
| Gladiador pesado | Mesmo grau de articulação com postura do Colosso e clipe de ataque pesado. Mantém a espada, o tempo de preparação e a configuração de dano da IA. |
| Arqueiro | Rig articulado, recuo voltado para o alvo e clipe próprio de arco. A posição da flecha vem do encaixe animado no instante de soltura. |

As malhas de personagens, armaduras, armas, materiais URP e mapas normais que já estavam no projeto formam a base visual desta etapa. A intervenção na arte refina a geometria da bainha, acrescenta uma seção de dobra e redistribui pesos nas articulações e no tecido. Os inimigos passaram a usar suas próprias versões articuladas. Cada personagem mantém dois níveis de detalhe, com até duas influências de osso por vértice.

## Movimento e combate

- O motor do jogador usa a velocidade medida após colisões, ao longo do quadro inteiro, para alimentar a cadência. O modelo muda de caminhada para corrida conforme ganha velocidade e retorna à caminhada durante a frenagem.
- Os clipes Idle, Walk, Run, StrafeLeft, StrafeRight e WalkBackward foram rebakeados. Os clipes de passada separam apoio e balanço, com correção de ângulo de quadril, joelho e tornozelo durante a autoria. A cadência usa o comprimento da passada em cada direção.
- Rolamento e Quickstep usam uma curva de viagem que acelera e depois freia. A integral mantém a distância configurada. A direção continua relativa à câmera e o visual do rolamento se alinha mais cedo à viagem. Stamina, recuperação, cooldown e invulnerabilidade vêm dos perfis existentes.
- A orientação para o golpe interpola na preparação e congela durante a janela de acerto. As animações de ação continuam seguindo os tempos do combate. A saída de golpe pesado e rolamento tem uma mistura ligeiramente mais longa.
- Aim Lock continua usando a seleção, a visibilidade e a perda de alvo existentes. A câmera reduz a correção de yaw muito perto do inimigo e separa o tremor visual da base do movimento. A colisão reaproveita os buffers nas consultas comuns e usa uma consulta completa quando o buffer lota.
- A IA mantém NavMeshAgent, CharacterController e os estados existentes. A navegação suaviza a velocidade e a curva de perseguição. Ataque, recuperação, defesa, dano e morte passam a conduzir o Animator dos três inimigos.

O deslocamento físico continua sob responsabilidade do motor ou do controlador de navegação. Os Animators trabalham na hierarquia visual com Root Motion desativado. A seleção, o equipamento e a habilidade de cada classe, as definições de dano e vida, História, Infinito, ranking e autenticação continuam usando os sistemas do projeto.

O menu **Bellator → Movement → Rebuild gait and enemy rigs** regenera os recursos a partir da arte e dos controllers existentes. Ele também verifica a trajetória de apoio dos clipes e os vínculos e deformações dos rigs dos inimigos.

## Validação

Os testes foram executados na Unity em uma cópia isolada do projeto. Os 59 arquivos gerados e seus metadados transferidos para o projeto principal foram conferidos por SHA-256.

| Teste | Resultado | Evidência |
|---|---|---|
| Piloto do Guerreiro | Passou com comandos do Input System, fases de combate, esquiva em oito direções com e sem alvo e colisão com parede. | [Log](Pilot.txt) · [Captura](WarriorPilot.png) |
| Integração dos quatro jogadores | Passou em História e Infinito. Cobre corrida, frenagem, ataque, defesa, disparo, recarga, habilidades, stamina, invulnerabilidade e as duas classes com Quickstep. | [Log](PlayerPlayMode.txt) |
| Apoio dos pés e deformação | Passou nos cinco clipes direcionais e de velocidade das quatro classes. Foram amostradas 504 poses dos jogadores e 270 dos inimigos nos dois LODs. | [Trajetórias e inimigos](AnimationValidation.txt) · [Malhas dos jogadores](SkinningValidation.txt) |
| Navegação e câmera | Passou com perseguição, recuo do arqueiro, troca de alvo, paredes e os cinco portais. A amostra de quatro segundos com 15 agentes terminou sem erros de jogo registrados. | [Log](ExpansionPlayMode.txt) |
| Combate e atributos | Passou nas quatro classes, incluindo 12 encontros curtos contra os três tipos de inimigo. | [Log](BalancePlayMode.txt) |
| Seleção, conta e ranking | Passou nas quatro prévias e em três resoluções. Exercitou cadastro, login, Infinito autenticado, registro de pontuação e a restrição de convidados. | [Log](ArtworkPlayMode.txt) |
| Regressões de combate e modos | Passaram movimento, combos, equipamento fixo, História e Infinito. História completou cinco ondas e validou a saída física pelo portão com cada classe. Infinito validou progressão, pontuação, derrota, reinício e retorno à História. | [Logs](Regressoes.txt) |

A suíte legada de contas parou ao procurar o antigo rótulo `GuestNotice`. A verificação da interface atual passou com conta local temporária, incluindo login, logout e persistência do resultado. A suíte de equipamento e seleção foi atualizada para identificar os três botões atuais e passou na repetição.

[Lista de arquivos atualizados e novos](Arquivos.md).

## Escopo visual

O estilo continua sendo o estilizado do Bellator. O apoio foi calculado para piso plano, com cadência aproximada nas misturas e em mudanças rápidas de direção. Validação manual prolongada, adaptação dos pés a rampas, ajuste individual das duas mãos em cada arma e benchmark em build independente são etapas separadas desta entrega.
