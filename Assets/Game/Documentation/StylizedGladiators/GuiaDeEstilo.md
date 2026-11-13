# Bellator — direção dos gladiadores estilizados

Referência: guerreiro ilustrado fornecido pelo usuário em 12/11/2026, com capacete dourado, crista vermelha, peitoral marrom, saia e capa vermelhas e escudo vermelho com aro dourado.

## Formas

Guerreiros adultos com anatomia simplificada, volumes amplos, ombros legíveis e mãos fechadas no encaixe original das armas. O corpo é uma superfície contínua nas transições entre tronco, ombros, braços e pernas. Rostos, dedos e sandálias recebem detalhes adicionais. Armaduras e acessórios são peças combinadas no renderer do personagem.

## Identidades

| Perfil real do projeto | Silhueta e equipamento visual | Paleta |
|---|---|---|
| Sobrevivente | Magro, remendo, faixas, couro simples, sem capacete ou capa longa | Terra, linho cru e couro |
| Colosso | Largo, placas sobre couro, ombreiras, duas grevas, capacete com crista longitudinal e capa | Bronze e vinho |
| Guerreiro | Atlético, couraça de couro com volumes peitorais e abdominais, capacete com proteção nasal e crista, capa | Vermelho, couro marrom e dourado |
| Caçador | Leve, colete ajustado, manto curto, bolsas, aljava e besta | Verde e couro |
| Gladiador comum | Capacete aberto, proteção assimétrica, sem capa longa | Azul e bronze |
| Gladiador pesado | Corpo largo, couraça segmentada e capacete fechado nas laterais | Ferro frio, couro e vermelho escuro |
| Arqueiro | Colete, aljava, cabelo preso e arco | Oliva e couro |

O escudo compartilhado recebe uma face vermelha abaulada, aro e ornamentos dourados. Espadas usam lâmina prateada com crista central, guarda dourada e empunhadura de couro. Arco e besta mantêm madeira, cordas e ferragens legíveis.

## Materiais e iluminação

Shader `Bellator/Gladiator Toon`, compatível com o URP 17.3 existente. Iluminação em três faixas com transições curtas, sombra quente e brilho metálico discreto. A identidade vem das cores, volumes e contrastes; não há textura de ruído fotográfico nem contorno externo espesso.

O shader fornece passes de profundidade, normais e sombra, além de iluminação Forward e Forward+. Não substitui o pipeline do projeto.

## Animação e desempenho

Preservar os rigs Generic, nomes dos ossos, curvas de animação, encaixes, escalas e colliders existentes. Não converter automaticamente para Humanoid: os clips do projeto usam os pivôs próprios do Bellator.

Gerar e gravar malhas somente no editor. Manter dois LODs, materiais compartilhados, até duas influências por vértice e um SkinnedMeshRenderer ativo por personagem. As capas têm dobras modeladas e seguem o tronco, sem simulação física adicional. Conferir o comportamento em ataques, esquivas e morte; não tratar um bake numericamente válido como aprovação artística automática.

## Apresentação

Enquadrar a geometria posada, incluindo capacetes, armas, escudos e capa. Preservar logo, fundo, botões e navegação existentes. Avaliar frente, perspectiva, lateral, costas, arena e seleção.
