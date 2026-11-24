using System.Diagnostics;
using System.Net;
using Client;
using Microsoft.Web.WebView2.Core;
using System.Net.Http.Headers;
using System.Net.Http.Handlers;
using Client.Utils;

namespace Launcher
{
    public partial class AMain : Form
    {
        long _totalBytes, _completedBytes;
        private int _fileCount, _currentCount;

        public volatile bool Completed, ErrorFound;
        public bool Checked, CleanFiles, LabelSwitch;
        private string _patchFailure;
        private readonly CancellationTokenSource _patchCancellation = new();

        public List<FileInformation> OldList;
        public Queue<FileInformation> DownloadList = new Queue<FileInformation>();
        public List<Download> ActiveDownloads = new List<Download>();

        private Stopwatch _stopwatch = Stopwatch.StartNew();

        public Thread _workThread;

        private bool dragging = false;
        private Point dragCursorPoint;
        private Point dragFormPoint;

        private Config ConfigForm = new Config();

        private bool Restart = false;

        public AMain()
        {
            InitializeComponent();

            BackColor = Color.FromArgb(1, 0, 0);
            TransparencyKey = Color.FromArgb(1, 0, 0);
        }

        public static void SaveError(string ex)
        {
            try
            {
                if (Settings.RemainingErrorLogs-- > 0)
                {
                    File.AppendAllText(@".\Error.txt",
                                       string.Format("[{0}] {1}{2}", DateTime.Now, ex, Environment.NewLine));
                }
            }
            catch
            {
            }
        }

        public void Start()
        {
            try
            {
                GetOldFileList();
                if (OldList.Count == 0)
                    throw new InvalidDataException("The patch manifest is empty or invalid.");

                _fileCount = OldList.Count;
                foreach (var info in OldList)
                {
                    _patchCancellation.Token.ThrowIfCancellationRequested();
                    CheckFile(info);
                }

                Checked = true;
                _currentCount = 0;
                _fileCount = DownloadList.Count;
                _stopwatch = Stopwatch.StartNew();

                // The existing launcher already performs downloads sequentially.
                // Use a loop rather than recursive calls for large patch lists.
                while (DownloadList.Count > 0)
                {
                    _patchCancellation.Token.ThrowIfCancellationRequested();
                    DownloadFile(new Download { Info = DownloadList.Dequeue() });
                }

                CleanUp();
            }
            catch (OperationCanceledException) when (_patchCancellation.IsCancellationRequested) { }
            catch (Exception ex)
            {
                ErrorFound = true;
                _patchFailure = ex.GetBaseException().Message;
                SaveError(ex.ToString());
            }
            finally
            {
                _stopwatch.Stop();
                Completed = true;
            }
        }

        private void CleanUp()
        {
            if (!CleanFiles) return;

            string[] fileNames = Directory.GetFiles(@".\", "*.*", SearchOption.AllDirectories);
            string fileName;
            for (int i = 0; i < fileNames.Length; i++)
            {
                if (fileNames[i].StartsWith(".\\Screenshots\\")) continue;

                fileName = Path.GetFileName(fileNames[i]);

                if (fileName == "Mir2Config.ini" || fileName == System.AppDomain.CurrentDomain.FriendlyName) continue;

                try
                {
                    if (!NeedFile(fileNames[i]))
                        File.Delete(fileNames[i]);
                }
                catch { }
            }
        }
        public bool NeedFile(string fileName)
        {
            for (int i = 0; i < OldList.Count; i++)
            {
                if (fileName.EndsWith(OldList[i].FileName))
                    return true;
            }

            return false;
        }

        private void GetOldFileList()
        {
            OldList = new List<FileInformation>();

            byte[] data = Download(Settings.P_PatchFileName);
            if (data != null && data.Length >= sizeof(int))
            {
                using MemoryStream stream = new MemoryStream(data);
                using BinaryReader reader = new BinaryReader(stream);

                if (reader.ReadByte() == 60)
                {
                    //assume we got a html page back with an error code so it's not a patchlist
                    return;
                }
                reader.BaseStream.Seek(0,SeekOrigin.Begin);
                int count = reader.ReadInt32();
                // Each record occupies at least one byte. Reject impossible
                // counts before trying to allocate or enumerate the manifest.
                if (count <= 0 || count > data.Length - sizeof(int))
                    throw new InvalidDataException("Invalid patch manifest file count.");

                for (int i = 0; i < count; i++)
                {
                    OldList.Add(new FileInformation(reader));
                }
            }
        }


        public void ParseOld(BinaryReader reader)
        {
            int count = reader.ReadInt32();

            for (int i = 0; i < count; i++)
                OldList.Add(new FileInformation(reader));
        }

        public void CheckFile(FileInformation old)
        {
            FileInformation info = GetFileInformation(Settings.P_Client + old.FileName);
            _currentCount++;

            if (info == null || old.Length != info.Length || old.Creation != info.Creation)
            {
                DownloadList.Enqueue(old);
                _totalBytes += old.Length;
            }
        }

        private HttpClient CreatePatchClient(HttpMessageHandler handler)
        {
            var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            if (Settings.P_NeedLogin)
            {
                string authInfo = Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(
                    Settings.P_Login + ":" + Settings.P_Password));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authInfo);
            }
            return client;
        }

        private Uri PatchUri(string fileName)
        {
            if (!Uri.TryCreate(Settings.P_Host + fileName.Replace('\\', '/'), UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new InvalidDataException("Invalid launcher patch host. Check the Host setting in Mir2Config.ini.");
            return uri;
        }

        public void DownloadFile(Download dl)
        {
            var info = dl.Info;
            string fileName = info.FileName.Replace('\\', '/');
            if (fileName != "PList.gz" && (info.Compressed != info.Length || info.Compressed == 0))
                fileName += ".gz";

            lock (ActiveDownloads) ActiveDownloads.Add(dl);
            string staging = null;
            try
            {
                using var progress = new ProgressMessageHandler(new HttpClientHandler { AllowAutoRedirect = true });
                progress.HttpReceiveProgress += (_, args) => dl.CurrentBytes = args.BytesTransferred;
                using var client = CreatePatchClient(progress);
                byte[] data = PatchDownload.FetchAsync(client, PatchUri(fileName),
                    attempt => { dl.CurrentBytes = 0; SaveError($"Retry {attempt}/{PatchDownload.MaxAttempts - 1}: {fileName}"); },
                    _patchCancellation.Token).GetAwaiter().GetResult();

                if (info.Compressed > 0 && info.Compressed != info.Length)
                    data = Functions.DecompressBytes(data);
                if (data.LongLength != info.Length)
                    throw new InvalidDataException($"Patch size mismatch for {info.FileName}.");

                // Never let a malformed manifest write outside the client root.
                string root = Path.GetFullPath(Settings.P_Client).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;
                string destination = Path.GetFullPath(Path.Combine(root, info.FileName));
                if (!destination.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Patch entry is outside the client directory.");

                string directory = Path.GetDirectoryName(destination);
                Directory.CreateDirectory(directory);
                staging = destination + "." + Guid.NewGuid().ToString("N") + ".patchtmp";
                File.WriteAllBytes(staging, data);
                File.SetLastWriteTime(staging, info.Creation);
                _patchCancellation.Token.ThrowIfCancellationRequested();

                string extension = Path.GetExtension(destination);
                bool executable = extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".pdb", StringComparison.OrdinalIgnoreCase);
                if (executable && File.Exists(destination))
                {
                    string backup = Path.Combine(directory, "Old__" + Path.GetFileName(destination));
                    if (File.Exists(backup)) File.Delete(backup);
                    File.Move(destination, backup);
                    try { File.Move(staging, destination); }
                    catch
                    {
                        if (!File.Exists(destination)) File.Move(backup, destination);
                        throw;
                    }
                    Restart = true;
                }
                else File.Move(staging, destination, overwrite: true);

                _currentCount++;
                _completedBytes += info.Length;
            }
            finally
            {
                if (staging != null && File.Exists(staging))
                    try { File.Delete(staging); } catch (IOException) { } catch (UnauthorizedAccessException) { }
                dl.CurrentBytes = 0;
                dl.Completed = true;
                lock (ActiveDownloads) ActiveDownloads.Remove(dl);
            }
        }

        public byte[] Download(string fileName)
        {
            using var client = CreatePatchClient(new HttpClientHandler { AllowAutoRedirect = true });
            return PatchDownload.FetchAsync(client, PatchUri(Path.ChangeExtension(fileName, ".gz")),
                attempt => SaveError($"Retry {attempt}/{PatchDownload.MaxAttempts - 1}: patch manifest"),
                _patchCancellation.Token).GetAwaiter().GetResult();
        }

        public FileInformation GetFileInformation(string fileName)
        {
            if (!File.Exists(fileName)) return null;

            FileInfo info = new FileInfo(fileName);
            return new FileInformation
            {
                FileName = fileName.Remove(0, Settings.P_Client.Length),
                Length = (int)info.Length,
                Creation = info.LastWriteTime
            };
        }

        private void AMain_Load(object sender, EventArgs e)
        {
            var envir = CoreWebView2Environment.CreateAsync(null, Settings.ResourcePath).Result;
            Main_browser.EnsureCoreWebView2Async(envir);

            if (Settings.P_BrowserAddress != "")
            {
                if (Uri.IsWellFormedUriString(Settings.P_BrowserAddress, UriKind.Absolute))
                {
                    Main_browser.NavigationCompleted += Main_browser_NavigationCompleted;
                    Main_browser.Source = new Uri(Settings.P_BrowserAddress);
                }
                else
                {
                    MessageBox.Show(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.LauncherBrowserFormatError), GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.BadBrowserFormat));
                }
            }

            RepairOldFiles();

            Launch_pb.Enabled = false;
            ProgressCurrent_pb.Width = 5;
            TotalProg_pb.Width = 5;
            Version_label.Text = string.Format("Build: {0}.{1}.{2}", Globals.ProductCodename, Settings.UseTestConfig ? "Debug" : "Release", Application.ProductVersion);

            if (Settings.P_ServerName != String.Empty)
            {
                Name_label.Visible = true;
                Name_label.Text = Settings.P_ServerName;
            }

            _workThread = new Thread(Start) { IsBackground = true };
            _workThread.Start();
        }

        private void Main_browser_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (Main_browser.Source.AbsolutePath != "blank") Main_browser.Visible = true;
        }

        private void Launch_pb_Click(object sender, EventArgs e)
        {
            Launch();
        }

        private void Launch()
        {
            if (!Completed || ErrorFound) return;
            if (ConfigForm.Visible) ConfigForm.Visible = false;

            Program.Launch = true;
            Close();
        }

        private void Close_pb_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            Close();
        }

        private void Movement_panel_MouseClick(object sender, MouseEventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
            dragging = true;
            dragCursorPoint = Cursor.Position;
            dragFormPoint = this.Location;
        }

        private void Movement_panel_MouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private void Movement_panel_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                Point dif = Point.Subtract(Cursor.Position, new Size(dragCursorPoint));
                this.Location = Point.Add(dragFormPoint, new Size(dif));
            }
        }

        private void Launch_pb_MouseEnter(object sender, EventArgs e)
        {
            Launch_pb.Image = Client.Resources.Images.Launch_Hover;
        }

        private void Launch_pb_MouseLeave(object sender, EventArgs e)
        {
            Launch_pb.Image = Client.Resources.Images.Launch_Base1;
        }

        private void Close_pb_MouseEnter(object sender, EventArgs e)
        {
            Close_pb.Image = Client.Resources.Images.Cross_Hover;
        }

        private void Close_pb_MouseLeave(object sender, EventArgs e)
        {
            Close_pb.Image = Client.Resources.Images.Cross_Base;
        }

        private void Launch_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Launch_pb.Image = Client.Resources.Images.Launch_Pressed;
        }

        private void Launch_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Launch_pb.Image = Client.Resources.Images.Launch_Base1;
        }

        private void Close_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Close_pb.Image = Client.Resources.Images.Cross_Pressed;
        }

        private void Close_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Close_pb.Image = Client.Resources.Images.Cross_Base;
        }

        private void ProgressCurrent_pb_SizeChanged(object sender, EventArgs e)
        {
            ProgEnd_pb.Location = new Point((ProgressCurrent_pb.Location.X + ProgressCurrent_pb.Width), ProgressCurrent_pb.Location.Y);
            if (ProgressCurrent_pb.Width == 0) ProgEnd_pb.Visible = false;
            else ProgEnd_pb.Visible = true;
        }

        private void Config_pb_MouseDown(object sender, MouseEventArgs e)
        {
            Config_pb.Image = Client.Resources.Images.Config_Pressed;
        }

        private void Config_pb_MouseEnter(object sender, EventArgs e)
        {
            Config_pb.Image = Client.Resources.Images.Config_Hover;
        }

        private void Config_pb_MouseLeave(object sender, EventArgs e)
        {
            Config_pb.Image = Client.Resources.Images.Config_Base;
        }

        private void Config_pb_MouseUp(object sender, MouseEventArgs e)
        {
            Config_pb.Image = Client.Resources.Images.Config_Base;
        }

        private void Config_pb_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Hide();
            else ConfigForm.Show(Program.PForm);
            ConfigForm.Location = new Point(Location.X + Config_pb.Location.X - 183, Location.Y + 36);
        }

        private void TotalProg_pb_SizeChanged(object sender, EventArgs e)
        {
            ProgTotalEnd_pb.Location = new Point((TotalProg_pb.Location.X + TotalProg_pb.Width), TotalProg_pb.Location.Y);
            if (TotalProg_pb.Width == 0) ProgTotalEnd_pb.Visible = false;
            else ProgTotalEnd_pb.Visible = true;
        }

        private void InterfaceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (Completed && ErrorFound)
                {
                    InterfaceTimer.Enabled = false;
                    Launch_pb.Enabled = false;
                    ActionLabel.Text = "Update failed";
                    CurrentFile_label.Text = "Patch server unavailable or invalid update";
                    SpeedLabel.Text = "";
                    CurrentFile_label.Visible = true;
                    MessageBox.Show(this,
                        "The client could not finish checking or downloading updates.\n\n" +
                        _patchFailure + "\n\nCheck the launcher Host setting and your connection, then restart to retry. " +
                        "Details are saved in Error.txt.",
                        "Patch update failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (Completed)
                {
                    ActionLabel.Text = "";
                    CurrentFile_label.Text = GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.UpToDate);
                    SpeedLabel.Text = "";
                    ProgressCurrent_pb.Width = 550;
                    TotalProg_pb.Width = 550;
                    CurrentFile_label.Visible = true;
                    CurrentPercent_label.Visible = true;
                    TotalPercent_label.Visible = true;
                    CurrentPercent_label.Text = "100%";
                    TotalPercent_label.Text = "100%";
                    InterfaceTimer.Enabled = false;
                    Launch_pb.Enabled = true;

                    if (CleanFiles)
                    {
                        CleanFiles = false;
                        MessageBox.Show(GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.YourFilesCleanedUp), GameLanguage.ClientTextMap.GetLocalization(ClientTextKeys.CleanFiles));
                    }

                    if (Restart)
                    {
                        Program.Restart = true;

                        Close();
                    }

                    if (Settings.P_AutoStart)
                    {
                        Launch();
                    }
                    return;
                }

                var currentBytes = 0L;
                FileInformation currentFile = null;

                int activeCount;
                lock (ActiveDownloads)
                {
                    activeCount = ActiveDownloads.Count;
                    foreach (var dl in ActiveDownloads)
                        if (!dl.Completed) currentBytes += dl.CurrentBytes;
                    if (activeCount > 0) currentFile = ActiveDownloads[0].Info;
                }

                ActionLabel.Visible = true;
                SpeedLabel.Visible = true;
                CurrentFile_label.Visible = true;
                CurrentPercent_label.Visible = true;
                TotalPercent_label.Visible = true;

                if (LabelSwitch) ActionLabel.Text = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.FilesRemaining), _fileCount - _currentCount);
                else ActionLabel.Text = GameLanguage.ClientTextMap.GetLocalization((ClientTextKeys.MBRemaining), ((_totalBytes) - (_completedBytes + currentBytes)) / 1024 / 1024);

                if (Settings.P_Concurrency > 1)
                {
                    CurrentFile_label.Text = string.Format("<Concurrent> {0}", activeCount);
                    SpeedLabel.Text = ToSize(currentBytes / _stopwatch.Elapsed.TotalSeconds);
                }
                else
                {
                    if (currentFile != null)
                    {
                        CurrentFile_label.Text = string.Format("{0}", currentFile.FileName);
                        SpeedLabel.Text = ToSize(currentBytes / _stopwatch.Elapsed.TotalSeconds);
                        CurrentPercent_label.Text = ((int)(100 * currentBytes / currentFile.Length)).ToString() + "%";
                        ProgressCurrent_pb.Width = (int)(5.5 * (100 * currentBytes / currentFile.Length));
                    }
                }

                if (!(_completedBytes is 0 && currentBytes is 0 && _totalBytes is 0))
                {
                    TotalProg_pb.Width = (int)(5.5 * (100 * (_completedBytes + currentBytes) / _totalBytes));
                    TotalPercent_label.Text = ((int)(100 * (_completedBytes + currentBytes) / _totalBytes)).ToString() + "%";
                }

            }
            catch
            {
                //to-do 
            }

        }

        private void AMain_Click(object sender, EventArgs e)
        {
            if (ConfigForm.Visible) ConfigForm.Visible = false;
        }

        private void ActionLabel_Click(object sender, EventArgs e)
        {
            LabelSwitch = !LabelSwitch;
        }

        private void Credit_label_Click(object sender, EventArgs e)
        {
            if (Credit_label.Text == "Powered by Crystal M2") Credit_label.Text = "Designed by Breezer";
            else Credit_label.Text = "Powered by Crystal M2";
        }

        private void AMain_FormClosed(object sender, FormClosedEventArgs e)
        {
            _patchCancellation.Cancel();
            // Do not race the worker while it is installing a staged file.
            // RepairOldFiles restores an interrupted executable replacement
            // before the next patch check.
            if (Completed) MoveOldFilesToCurrent();

            Launch_pb?.Dispose();
            Close_pb?.Dispose();
        }

        private static string[] suffixes = new[] { " B", " KB", " MB", " GB", " TB", " PB" };

        private string ToSize(double number, int precision = 2)
        {
            // unit's number of bytes
            const double unit = 1024;
            // suffix counter
            int i = 0;
            // as long as we're bigger than a unit, keep going
            while (number > unit)
            {
                number /= unit;
                i++;
            }
            // apply precision and current suffix
            return Math.Round(number, precision) + suffixes[i];
        }

        private void RepairOldFiles()
        {
            var files = Directory.GetFiles(Settings.P_Client, "*", SearchOption.AllDirectories).Where(x => Path.GetFileName(x).StartsWith("Old__"));

            foreach (var oldFilename in files)
            {
                if (!File.Exists(oldFilename.Replace("Old__", "")))
                {
                    File.Move(oldFilename, oldFilename.Replace("Old__", ""));
                }
                else
                {
                    File.Delete(oldFilename);
                }
            }
        }

        private void MoveOldFilesToCurrent()
        {
            var files = Directory.GetFiles(Settings.P_Client, "*", SearchOption.AllDirectories).Where(x => Path.GetFileName(x).StartsWith("Old__"));

            foreach (var oldFilename in files)
            {
                string originalFilename = Path.Combine(Path.GetDirectoryName(oldFilename), (Path.GetFileName(oldFilename).Replace("Old__", "")));

                if (!File.Exists(originalFilename) && File.Exists(oldFilename))
                    File.Move(oldFilename, originalFilename);
            }
        }
    }
}
