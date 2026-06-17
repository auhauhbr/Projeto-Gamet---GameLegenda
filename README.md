<<<<<<< HEAD
# GameLegenda v0.1

Protótipo técnico Windows em .NET 9/WPF para validar captura de janela, OCR local plugável, glossário/cache e overlay transparente.

## Como rodar

```powershell
dotnet restore GameLegenda.sln
dotnet run --project src/GameLegenda.App/GameLegenda.App.csproj
```

Na janela do app:

- Clique em `Abrir janela de teste`.
- Clique em `Iniciar captura (F8)`.
- A tradução aparece como texto transparente sobre a janela capturada, sem caixa de fundo.
- `F9` mostra/oculta o overlay; `F10` liga o ajuste por arraste; `Esc` para a captura ativa.

## Traducao

Ordem usada pelo app:

- Cache e glossario local.
- DeepL, se estiver ligado e com chave configurada.
- LibreTranslate local, se estiver ligado e rodando em `http://127.0.0.1:5000/translate`.
- Dicionario local minimo para termos conhecidos.

Para usar DeepL, marque `DeepL`, cole sua chave da API e clique em `Aplicar`.

Para usar LibreTranslate local:

```powershell
pip install libretranslate
libretranslate
```

Depois deixe `Libre local` ligado no app e clique em `Aplicar`.

## Observações do v0.1

- O serviço tenta usar `Windows.Media.Ocr` por reflexão, sem pacote externo.
- Se o OCR nativo não estiver disponível no runtime, o app usa um fallback local via PowerShell para chamar o OCR do Windows.
- Para jogos como Final Fantasy, o fallback gera recortes ampliados da janela antes do OCR e preserva as coordenadas dos textos.
- O overlay é limpo quando a captura para e fica oculto quando a janela capturada perde foco.
- A tradução ainda é placeholder local com glossário e cache; motor offline real fica para a próxima versão.

## Testes

```powershell
dotnet run --project tests/GameLegenda.Core.Tests/GameLegenda.Core.Tests.csproj
```
=======
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
>>>>>>> origin/main
