using Client.MirGraphics;

namespace Client.MirControls
{
    public enum MirMessageBoxButtons { OK, OKCancel, YesNo, YesNoCancel, Cancel }

    public sealed class MirMessageBox : MirPanel
    {
        public MirLabel Label;
        public MirButton OKButton, CancelButton, NoButton, YesButton;
        public MirMessageBoxButtons Buttons;
        public bool AllowKeyPress = true;

        public MirMessageBox(string message, MirMessageBoxButtons b = MirMessageBoxButtons.OK, bool allowKeys = true)
        {
            Buttons = b;
            Modal = true;
            AllowKeyPress = allowKeys;
            AutoSize = false;
            Size = new Size(460, 204);
            PanelTitle = b is MirMessageBoxButtons.YesNo or MirMessageBoxButtons.YesNoCancel or MirMessageBoxButtons.OKCancel
                ? "Confirm" : "Notice";
            Location = new Point((Settings.ScreenWidth - Size.Width) / 2, (Settings.ScreenHeight - Size.Height) / 2);
            Label = new MirLabel
            {
                Parent = this, Text = message, AutoSize = false, NotControl = true,
                Location = new Point(24, 46), Size = new Size(412, 104),
                ForeColour = DarkFantasyPalette.Text, OutLine = false,
                DrawFormat = TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis
            };

            string[] labels = b switch
            {
                MirMessageBoxButtons.OK => new[] { "OK" },
                MirMessageBoxButtons.OKCancel => new[] { "OK", "Cancel" },
                MirMessageBoxButtons.YesNo => new[] { "Yes", "No" },
                MirMessageBoxButtons.YesNoCancel => new[] { "Yes", "No", "Cancel" },
                _ => new[] { "Cancel" }
            };
            int x = Size.Width - 20 - (labels.Length * 92 + (labels.Length - 1) * 10);
            foreach (string text in labels)
            {
                var action = new MirButton
                {
                    Skin = UiSurface.Button, AutoSize = false, Size = new Size(92, 28),
                    Parent = this, Text = text, Location = new Point(x, 158),
                    Selected = text is "OK" or "Yes"
                };
                action.Click += (o, e) => Dispose();
                switch (text)
                {
                    case "OK": OKButton = action; break;
                    case "Yes": YesButton = action; break;
                    case "No": NoButton = action; break;
                    case "Cancel": CancelButton = action; break;
                }
                x += 102;
            }
        }

        public override void Show()
        {
            if (Parent != null) return;

            Parent = MirScene.ActiveScene;

            Highlight();

            for (int i = 0; i < Program.Form.Controls.Count; i++)
            {
                TextBox T = Program.Form.Controls[i] as TextBox;
                if (T != null && T.Tag != null && T.Tag != null)
                    ((MirTextBox)T.Tag).DialogChanged();
            }
        }


        public override void OnKeyDown(KeyEventArgs e)
        {
            if (AllowKeyPress)
            {
                base.OnKeyDown(e);
                e.Handled = true;
            }
        }
        public override void OnKeyUp(KeyEventArgs e)
        {
            if (AllowKeyPress)
            {
                base.OnKeyUp(e);
                e.Handled = true;
            }
        }
        public override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            if (AllowKeyPress)
            {
                if (e.KeyChar == (char)Keys.Escape)
                {
                    switch (Buttons)
                    {
                        case MirMessageBoxButtons.OK:
                            if (OKButton != null && !OKButton.IsDisposed) OKButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.OKCancel:
                        case MirMessageBoxButtons.Cancel:
                        case MirMessageBoxButtons.YesNoCancel:
                            if (CancelButton != null && !CancelButton.IsDisposed) CancelButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.YesNo:
                            if (NoButton != null && !NoButton.IsDisposed) NoButton.InvokeMouseClick(null);
                            break;
                    }
                }

                else if (e.KeyChar == (char)Keys.Enter)
                {
                    switch (Buttons)
                    {
                        case MirMessageBoxButtons.OK:
                        case MirMessageBoxButtons.OKCancel:
                            if (OKButton != null && !OKButton.IsDisposed) OKButton.InvokeMouseClick(null);
                            break;
                        case MirMessageBoxButtons.YesNoCancel:
                        case MirMessageBoxButtons.YesNo:
                            if (YesButton != null && !YesButton.IsDisposed) YesButton.InvokeMouseClick(null);
                            break;

                    }
                }
                e.Handled = true;
            }
        }


        public static void Show(string message, bool close = false)
        {
            MirMessageBox box = new MirMessageBox(message);

            if (close) box.OKButton.Click += (o, e) => Program.Form.Close();

            box.Show();
        }

        #region Disposable

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);

            if (!disposing) return;

            Label = null;
            OKButton = null;
            CancelButton = null;
            NoButton = null;
            YesButton = null;
            Buttons = 0;

            for (int i = 0; i < Program.Form.Controls.Count; i++)
            {
                TextBox T = (TextBox) Program.Form.Controls[i];
                if (T != null && T.Tag != null)
                    ((MirTextBox) T.Tag).DialogChanged();
            }
        }

        #endregion
    }
}
