# Bellator — tentativa de build Web

Data: 13/11/2026. Estado: bloqueado antes da compilação; build e ZIP não gerados.

## Bloqueio confirmado

Unity 6000.3.26f1 está instalada em `C:/Program Files/Unity/Hub/Editor/6000.3.26f1`.
Sua pasta `Editor/Data/PlaybackEngines` contém somente `windowsstandalonesupport`. O compilador e o suporte Web não estão instalados.

Foi executada a tentativa pelo Unity Hub:

```
Unity Hub.exe -- --headless install-modules --version 6000.3.26f1 --module webgl --childModules
```

O processo terminou com `Failed to load Bootstrap module` e `EPERM: operation not permitted` ao tentar gravar um arquivo temporário de `user-settings.json` em `C:/Users/dizzy/AppData/Roaming/UnityHub/`. A tentativa de acessar a janela do Hub também não forneceu uma interface utilizável: árvore de acessibilidade sem controles de instalação e capturas com timeout. Não foi possível concluir a instalação neste ambiente.

Ação necessária no computador: abrir Unity Hub, acessar **Installs**, localizar **6000.3.26f1**, abrir **Manage / Add modules**, selecionar **Web Build Support** e concluir a instalação. A instalação precisa incluir as ferramentas da própria versão 6000.3.26f1. Depois, reabrir o Editor para reconhecer o módulo.

## Inspeção inicial realizada

- Versão confirmada em ProjectVersion.txt: 6000.3.26f1.
- URP 17.3.0 instalado; GraphicsSettings referencia um Render Pipeline customizado.
- Netcode for GameObjects 2.7.0 e Multiplayer Services 2.4.0 instalados.
- Cenas habilitadas: MainMenu (primeira), GladiatorSelection, RomanArena e OnlineArena. SampleScene desabilitada.
- Multiplayer existente usa sessões Multiplayer Services com Relay; não precisa ser reconstruído.
- LocalAccountStore usa Task.Run para derivação de senhas. Esse fluxo precisa de adaptação/validação específica para Web antes de considerar o login compatível.
- Persistência de contas e ranking usa arquivos locais e precisa ser validada no armazenamento persistente do navegador, incluindo sincronização e recarga da página.
- O fluxo Relay inspecionado usa WithRelayNetwork sem seleção explícita de WSS no código do jogo. A configuração efetiva de WebSocket seguro precisa ser conferida no SDK e testada em uma build Web real.

Esses pontos são riscos identificados, não falhas reproduzidas no navegador: não há executável Web para reproduzi-las ainda. A auditoria completa de assets, memória, draw calls e qualidade visual permanece pendente.

## Arquivos e configurações preservados

Nesta tentativa não foram alterados scripts de gameplay, pacotes, importações de texturas, configurações Windows ou cenas. As alterações anteriores de saída/cutscene e remoção de créditos foram preservadas. Este relatório é o único novo arquivo desta tentativa.

Não foram criados index.html fictício, pasta de build vazia ou ZIP sem jogo. `Builds/Bellator_WebGL/` e `Builds/Bellator_WebGL.zip` ainda não são entregáveis disponíveis. Tamanho de download e tamanho descompactado: indisponíveis.

## Trabalho pendente após instalar o módulo

1. Criar perfil Web separado do Windows, configurar as quatro cenas e resolução adaptável inicial de 1280×720.
2. Corrigir e testar autenticação local, persistência e transporte WSS sem alterar o comportamento Windows.
3. Aplicar otimizações específicas da plataforma com base nos assets efetivamente incluídos e no relatório de build.
4. Compilar produção, configurar compressão compatível com a hospedagem e gerar index.html na raiz de Builds/Bellator_WebGL.
5. Servir via HTTP e executar os testes reais de menu, seleção, combate, habilidades, ondas, fuga, cutscene, retorno e multiplayer.
6. Medir tamanho, memória e desempenho, verificar caminhos e gerar Bellator_WebGL.zip com o conteúdo da build na raiz.
7. No itch.io: escolher HTML, enviar o ZIP, marcar para execução no navegador, configurar viewport 1280×720 e tela cheia, testar a página em rascunho antes de publicar.

Nenhum teste em Chrome, Edge ou Firefox foi realizado nesta tentativa. Os testes anteriores no Editor não equivalem a testes WebGL. Nenhuma publicação foi feita.
