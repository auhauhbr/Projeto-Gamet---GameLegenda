# Projeto-Gamet---GameLegenda
Protótipo em .NET/WPF para capturar texto de jogos via OCR, traduzir e exibir a tradução em overlay transparente direto na sua tela e em tempo real - quando possível, claro

# GameLegenda

GameLegenda é um protótipo técnico em desenvolvimento para tradução visual de jogos no Windows.

A ideia do projeto é capturar textos exibidos em uma janela de jogo, reconhecer esses textos localmente por OCR, traduzir o conteúdo e exibir a tradução como um overlay transparente sobre a própria tela do jogo, próximo à posição original do texto. O objetivo é funcionar como uma legenda visual discreta, sem cobrir a interface do jogo com caixas ou janelas separadas.

## Status

Este projeto ainda está em fase inicial de testes.  
A versão atual valida os conceitos principais de captura de janela, OCR, cache de tradução, glossário e overlay transparente. Ainda existem limitações e várias partes serão melhoradas nas próximas versões.

## Funcionalidades atuais

- Captura de janela no Windows
- Reconhecimento de texto via OCR
- Overlay transparente e always-on-top
- Tradução posicionada próxima ao texto original
- Cache local de traduções
- Glossário interno
- Suporte inicial a provedores plugáveis de tradução
- Suporte planejado/experimental para DeepL e LibreTranslate
- Atalhos para iniciar/parar captura e controlar o overlay

## Objetivo

Criar uma ferramenta que ajude jogadores a entender diálogos, menus e textos de jogos que não possuem tradução oficial para português, mantendo a experiência visual do jogo o mais limpa possível.

## Tecnologias

- .NET 9
- WPF
- C#
- OCR local do Windows
- Integração plugável com serviços de tradução

## Aviso

Este projeto é experimental e está em desenvolvimento ativo.  
O funcionamento pode variar dependendo do jogo, resolução, fonte usada, janela capturada e qualidade do OCR.
