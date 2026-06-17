namespace GameLegenda.Core.Models;

public sealed record GameProfile(
    string Id,
    string Name,
    GameStyle Style,
    string SourceLanguage,
    string TargetLanguage,
    IReadOnlyList<CaptureRegion> CaptureRegions,
    OverlayAppearance Appearance,
    IReadOnlyDictionary<string, string> Glossary)
{
    public static GameProfile CreateDefault()
    {
        var placement = new OverlayPlacement(OverlayPlacementMode.BottomCenter, 0.5, 0.82, 720, true);
        var region = new CaptureRegion("dialog", "Dialogo principal", 0, 0, 0, 0, GameTextKind.Dialogue, true, placement);

        return new GameProfile(
            "default-test-profile",
            "Perfil de Teste",
            GameStyle.VisualNovel,
            "en",
            "pt-BR",
            [region],
            OverlayAppearance.Default,
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Iron Sword"] = "Espada de Ferro",
                ["Health Potion"] = "Pocao de Vida",
                ["Quest Updated"] = "Missao atualizada",
                ["Items"] = "Itens",
                ["Magic"] = "Magia",
                ["Equip"] = "Equipar",
                ["Status"] = "Estado",
                ["Order"] = "Ordem",
                ["Configuration"] = "Configuracao",
                ["Quick Save"] = "Salvar rapido",
                ["Save"] = "Salvar",
                ["Back"] = "Voltar",
                ["Use"] = "Usar",
                ["Key Items"] = "Itens-chave",
                ["Sort"] = "Ordenar",
                ["Staff"] = "Cajado",
                ["Knife"] = "Faca",
                ["Ether"] = "Eter",
                ["Antidote"] = "Antidoto",
                ["Echo Grass"] = "Erva do Eco",
                ["Remedy"] = "Remedio",
                ["Cottage"] = "Cabana",
                ["Hammer"] = "Martelo",
                ["Chain Mail"] = "Cota de Malha",
                ["Potion"] = "Pocao",
                ["Clothes"] = "Roupas",
                ["Hi-Potion"] = "Pocao Alta",
                ["Phoenix Down"] = "Pena de Fenix",
                ["Eye Drops"] = "Colirio",
                ["Gold Needle"] = "Agulha Dourada",
                ["Tent"] = "Tenda",
                ["Rapier"] = "Rapieira",
                ["Leather Armor"] = "Armadura de Couro",
                ["Leather Cap"] = "Capuz de Couro",
                ["Leather Shield"] = "Escudo de Couro",
                ["Close Menu"] = "Fechar menu",
                ["Confirm"] = "Confirmar",
                ["Time"] = "Tempo"
            });
    }
}

public enum GameStyle
{
    Rpg3D,
    Rpg2DIsometric,
    VisualNovel,
    JrpgMenuHeavy,
    Emulator,
    Custom
}

public enum GameTextKind
{
    Dialogue,
    Item,
    Quest,
    Menu,
    Description,
    Document,
    Unknown
}
