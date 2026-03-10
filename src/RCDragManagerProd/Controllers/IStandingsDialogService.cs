using RCDragManagerProd.UI.Forms;

namespace RCDragManagerProd.Controllers
{
    public interface IStandingsDialogService
    {
        void Show(string title, string content);
    }

    internal sealed class ScrollableStandingsDialogService : IStandingsDialogService
    {
        public void Show(string title, string content)
        {
            ScrollableTextDialog.Show(title, content);
        }
    }
}
