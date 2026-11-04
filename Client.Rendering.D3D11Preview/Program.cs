using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CrystalM2.Rendering.Core;

namespace CrystalM2.Rendering.D3D11Preview
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new LibPreviewForm(args));
        }
    }

    // Real .Lib frames, presented in a separate Direct3D11 window. Does not
    // attach to or modify the production Mir2/Mir3 client or game protocol.
    internal sealed class LibPreviewForm : Form
    {
        private readonly Panel _viewport = new Panel { Dock = DockStyle.Fill };
        private readonly NumericUpDown _frameNumber = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 0,
            Width = 110
        };
        private readonly Label _status = new Label
        {
            AutoSize = true,
            Padding = new Padding(6, 8, 4, 0),
            Text = "Choose a CrystalM2 .Lib to preview the untouched baked artwork."
        };
        private readonly System.Windows.Forms.Timer _timer = new System.Windows.Forms.Timer
        {
            Interval = 16
        };

        private readonly string[] _args;
        private BakedSpriteD3D11? _renderer;
        private string? _libraryPath;
        private bool _loading;

        internal LibPreviewForm(string[] args)
        {
            _args = args;
            Text = "CrystalM2 — D3D11 baked sprite compatibility preview (not the game client)";
            ClientSize = new Size(1120, 760);
            StartPosition = FormStartPosition.CenterScreen;

            var bar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(7, 7, 7, 3)
            };
            var open = new Button { Text = "Open .Lib", Width = 110 };
            open.Click += (_, _) => ChooseLibrary();
            var frameLabel = new Label { Text = "Frame:", AutoSize = true,
                                         Padding = new Padding(10, 9, 4, 0) };
            bar.Controls.Add(open);
            bar.Controls.Add(frameLabel);
            bar.Controls.Add(_frameNumber);
            bar.Controls.Add(_status);

            Controls.Add(_viewport);
            Controls.Add(bar);

            _frameNumber.ValueChanged += (_, _) =>
            {
                if (!_loading && _libraryPath is not null)
                    LoadFrame(_libraryPath, (int)_frameNumber.Value);
            };
            _viewport.Resize += (_, _) =>
            {
                if (_renderer != null && _viewport.ClientSize.Width > 0 &&
                    _viewport.ClientSize.Height > 0)
                    _renderer.Resize(_viewport.ClientSize.Width, _viewport.ClientSize.Height);
            };
            _timer.Tick += (_, _) =>
            {
                try
                {
                    _renderer?.Draw();
                }
                catch (Exception ex)
                {
                    _timer.Stop();
                    _status.Text = "D3D11 rendering stopped: " + ex.Message;
                    MessageBox.Show(this, ex.ToString(), "D3D11 preview error",
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            Shown += (_, _) => StartRenderer();
        }

        private void StartRenderer()
        {
            try
            {
                _renderer = new BakedSpriteD3D11(_viewport.Handle,
                    Math.Max(1, _viewport.ClientSize.Width),
                    Math.Max(1, _viewport.ClientSize.Height));
                _timer.Start();

                if (_args.Length > 0 && File.Exists(_args[0]))
                {
                    int frameIndex = 0;
                    if (_args.Length > 1) int.TryParse(_args[1], out frameIndex);
                    LoadFrame(_args[0], frameIndex);
                }
            }
            catch (Exception ex)
            {
                _status.Text = "Direct3D11 unavailable: " + ex.Message;
                MessageBox.Show(this, ex.ToString(), "Cannot start hardware D3D11",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChooseLibrary()
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Choose an existing CrystalM2 sprite library",
                Filter = "Mir .Lib libraries|*.Lib|All files|*.*",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
                LoadFrame(dialog.FileName, 0);
        }

        private void LoadFrame(string path, int frameIndex)
        {
            if (_renderer == null) return;

            try
            {
                // Only load user-selected local data; never modify or repack
                // the existing library. No assets are committed to GitHub.
                using var stream = new FileStream(path, FileMode.Open,
                                                  FileAccess.Read, FileShare.Read);
                using var lib = new LegacyMirLibReader(stream);
                if (lib.Count == 0)
                    throw new InvalidDataException("This library contains no frames.");

                frameIndex = Math.Clamp(frameIndex, 0, lib.Count - 1);
                LegacySpriteFrame frame = lib.ReadFrame(frameIndex);
                _renderer.SetSprite(frame);

                _loading = true;
                try
                {
                    _libraryPath = path;
                    _frameNumber.Maximum = lib.Count - 1;
                    _frameNumber.Value = frameIndex;
                }
                finally { _loading = false; }

                _status.Text = $"{Path.GetFileName(path)}  " +
                    $"[{frameIndex + 1}/{lib.Count}]  " +
                    $"{frame.Width}×{frame.Height}, offsets ({frame.OffsetX}, {frame.OffsetY}); " +
                    "original BGRA, baked lighting retained.";
            }
            catch (Exception ex)
            {
                _status.Text = "Cannot decode selected LIB: " + ex.Message;
                MessageBox.Show(this, ex.Message, "Invalid or unsupported LIB frame",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Stop();
                _timer.Dispose();
                _renderer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
