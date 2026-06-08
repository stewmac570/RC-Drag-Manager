using System.Windows.Forms;

namespace RCDragManagerProd.UI.Forms
{
    // Shared race-day message-box helper (issue #258). Gives every validation,
    // warning, error, success, and confirmation dialog a consistent owner window,
    // title, button set, and severity icon so operators get reliable severity cues
    // instead of bare Windows popups.
    internal static class RaceDialogs
    {
        public static void Info(IWin32Window owner, string message, string title)
            => MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        // Successful operator actions (saved / completed). Same neutral icon as Info
        // but a distinct entry point keeps call sites self-documenting.
        public static void Success(IWin32Window owner, string message, string title)
            => MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public static void Warn(IWin32Window owner, string message, string title)
            => MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public static void Error(IWin32Window owner, string message, string title)
            => MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

        // Yes/No confirmation. Defaults to No so an accidental Enter never triggers
        // the action; destructive prompts get a warning icon, others a question icon.
        public static bool Confirm(IWin32Window owner, string message, string title, bool destructive = false)
            => MessageBox.Show(owner, message, title, MessageBoxButtons.YesNo,
                   destructive ? MessageBoxIcon.Warning : MessageBoxIcon.Question,
                   MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }
}
