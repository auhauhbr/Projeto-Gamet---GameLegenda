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
