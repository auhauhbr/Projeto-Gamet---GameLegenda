using GameLegenda.Core.Models;

namespace GameLegenda.Core.Services;

public interface IOverlayPresenter
{
    void Show(IReadOnlyList<OverlayTranslation> translations);
    void Hide();
}
