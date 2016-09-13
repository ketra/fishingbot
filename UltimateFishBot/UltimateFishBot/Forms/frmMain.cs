using System;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using UltimateFishBot.Classes;
using UltimateFishBot.Classes.Helpers;
using UltimateFishBot.Forms;
using UltimateFishBot.Properties;
using System.IO;

namespace UltimateFishBot
{
    public partial class frmMain : Form
    {

        public enum KeyModifier
        {
            None    = 0,
            Alt     = 1,
            Control = 2,
            Shift   = 4
        }

        public enum HotKey
        {
            StartStop   = 0,
            PauseResume = 1
        }

        public frmMain()
        {
            InitializeComponent();

            m_manager = new Manager(this);
        }

        Form form2;
        Point MD = Point.Empty;
        Rectangle rect = Rectangle.Empty;
        float dpi = 1f;

        private void frmMain_Load(object sender, EventArgs e)
        {
            btnStart.Text       = Translate.GetTranslate("frmMain", "BUTTON_START");
            btnStop.Text        = Translate.GetTranslate("frmMain", "BUTTON_STOP");
            btnSettings.Text    = Translate.GetTranslate("frmMain", "BUTTON_SETTINGS");
            btnStatistics.Text  = Translate.GetTranslate("frmMain", "BUTTON_STATISTICS");
            btnHowTo.Text       = Translate.GetTranslate("frmMain", "BUTTON_HTU");
            btnClose.Text       = Translate.GetTranslate("frmMain", "BUTTON_EXIT");
            btnAbout.Text       = Translate.GetTranslate("frmMain", "BUTTON_ABOUT");
            lblStatus.Text      = Translate.GetTranslate("frmMain", "LABEL_STOPPED");
            this.Text           = "UltimateFishBot - v " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            ReloadHotkeys();
            CheckStatus();
            LoadBobber();
        }
        private void LoadBobber()
        {
            try
            {
                using (FileStream stream = new FileStream(Properties.Settings.Default.BobberIcon, FileMode.Open, FileAccess.Read))
                {
                    pictureBox2.Image = Image.FromStream(stream);
                }
            }
            catch (Exception ex)
            {
                Console.Out.WriteLine(ex.Message);
            }
        }

        private void CheckStatus()
        {
            lblWarn.Text = Translate.GetTranslate("frmMain", "LABEL_CHECKING_STATUS");
            lblWarn.Parent = PictureBox1;

            try
            {
                Task.Factory.StartNew(() => (new WebClient()).DownloadString("http://www.fishbot.net/status.txt"),
                    TaskCreationOptions.LongRunning).ContinueWith(x =>
                    {
                        if (x.Result.ToLower().Trim() != "safe")
                        {
                            lblWarn.Text = Translate.GetTranslate("frmMain", "LABEL_NO_LONGER_SAFE");
                            lblWarn.ForeColor = Color.Red;
                            lblWarn.BackColor = Color.Black;
                        }
                        else
                            lblWarn.Visible = false;
                    }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                lblWarn.Text = (Translate.GetTranslate("frmMain", "LABEL_COULD_NOT_CHECK_STATUS") + ex);
            }                        
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            btnSettings.Enabled = false;
            btnStop.Enabled = true;

            if (m_manager.GetActualState() == Manager.FishingState.Stopped)
            {
                m_manager.Start();
                btnStart.Text = Translate.GetTranslate("frmMain", "BUTTON_PAUSE");
                lblStatus.Text = Translate.GetTranslate("frmMain", "LABEL_STARTED");
                lblStatus.Image = Resources.online;
            }
            else if (m_manager.GetActualState() == Manager.FishingState.Paused)
            {
                m_manager.Resume();
                btnStart.Text = Translate.GetTranslate("frmMain", "BUTTON_PAUSE");
                lblStatus.Text = Translate.GetTranslate("frmMain", "LABEL_RESUMED");
                lblStatus.Image = Resources.online;
            }
            else
            {
                btnSettings.Enabled = true;
                m_manager.Pause();
                btnStart.Text = Translate.GetTranslate("frmMain", "BUTTON_RESUME");
                lblStatus.Text = Translate.GetTranslate("frmMain", "LABEL_PAUSED");
                lblStatus.Image = Resources.online;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            btnSettings.Enabled = true;
            btnStop.Enabled = false;
            m_manager.Stop();
            btnStart.Text = Translate.GetTranslate("frmMain", "BUTTON_START");
            lblStatus.Text = Translate.GetTranslate("frmMain", "LABEL_STOPPED");
            lblStatus.Image = Resources.offline;
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {

            frmSettings.GetForm(this).Show();

        }

        private void btnStatistics_Click(object sender, EventArgs e)
        {

            frmStats.GetForm(m_manager).Show();

        }

        private void btnHowTo_Click(object sender, EventArgs e)
        {

            frmDirections.GetForm.Show();
         
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void StopFishing()
        {
            btnStop_Click(null, null);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_HOTKEY)
            {
                Keys key = (Keys)(((int)m.LParam >> 16) & 0xFFFF);                  // The key of the hotkey that was pressed.
                KeyModifier modifier = (KeyModifier)((int)m.LParam & 0xFFFF);       // The modifier of the hotkey that was pressed.
                int id = m.WParam.ToInt32();                                        // The id of the hotkey that was pressed.

                if (id == (int)HotKey.StartStop)
                {
                    if (m_manager.IsStoppedOrPaused())
                        btnStart_Click(null, null);
                    else
                        btnStop_Click(null, null);
                }
                else if (id == (int)HotKey.PauseResume)
                {
                    btnStart_Click(null, null);
                }
            }
        }

        public void ReloadHotkeys()
        {
            UnregisterHotKeys();

            foreach (HotKey hotKey in (HotKey[])Enum.GetValues(typeof(HotKey)))
            {
                Keys key = Keys.None;

                switch (hotKey)
                {
                    case HotKey.StartStop: key = Properties.Settings.Default.StartStopHotKey; break;
                    case HotKey.PauseResume: key = Properties.Settings.Default.PauseResumeKey; break;
                    default: continue;
                }

                KeyModifier modifiers = RemoveAndReturnModifiers(ref key);
                Win32.RegisterHotKey(this.Handle, (int)hotKey, (int)modifiers, (int)key);
            }
        }

        public void UnregisterHotKeys()
        {
            // Unregister all hotkeys before closing the form.
            foreach (HotKey hotKey in (HotKey[])Enum.GetValues(typeof(HotKey)))
                Win32.UnregisterHotKey(this.Handle, (int)hotKey);
        }

        private KeyModifier RemoveAndReturnModifiers(ref Keys key)
        {
            KeyModifier modifiers = KeyModifier.None;

            modifiers |= RemoveAndReturnModifier(ref key, Keys.Shift,   KeyModifier.Shift);
            modifiers |= RemoveAndReturnModifier(ref key, Keys.Control, KeyModifier.Control);
            modifiers |= RemoveAndReturnModifier(ref key, Keys.Alt,     KeyModifier.Alt);

            return modifiers;
        }

        private KeyModifier RemoveAndReturnModifier(ref Keys key, Keys keyModifier, KeyModifier modifier)
        {
            if ((key & keyModifier) != 0)
            {
                key &= ~keyModifier;
                return modifier;
            }

            return KeyModifier.None;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterHotKeys();
        }

        private Manager m_manager;
        private static int WM_HOTKEY = 0x0312;


        private void btnAbout_Click(object sender, EventArgs e)
        {
            about.GetForm.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            dpi = Win32.GetCurrentDPI();
            Hide();
            form2 = new Form();
            form2.BackColor = Color.Wheat;
            form2.TransparencyKey = form2.BackColor;
            form2.ControlBox = false;
            form2.MaximizeBox = false;
            form2.MinimizeBox = false;
            form2.FormBorderStyle = FormBorderStyle.None;
            //form2.WindowState = FormWindowState.Maximized;
            form2.MouseDown += form2_MouseDown;
            form2.MouseMove += form2_MouseMove;
            form2.Paint += form2_Paint;
            form2.MouseUp += form2_MouseUp;
            int screenLeft = SystemInformation.VirtualScreen.Left;
            int screenTop = SystemInformation.VirtualScreen.Top;
            int screenWidth = SystemInformation.VirtualScreen.Width;
            int screenHeight = SystemInformation.VirtualScreen.Height;

            form2.Size = new System.Drawing.Size(screenWidth, screenHeight);
            form2.Location = new System.Drawing.Point(screenLeft, screenTop);

            form2.Show();
        }

        void form2_MouseDown(object sender, MouseEventArgs e)
        {
            MD = e.Location;
        }

        void form2_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            Point MM = e.Location;
            int mmx = (int)(MM.X * dpi);
            int mmy = (int)(MM.Y * dpi);
            int mdx = (int)(MD.X * dpi);
            int mdy = (int)(MD.Y * dpi);
            rect = new Rectangle(Math.Min(mdx, mmx), Math.Min(mdy, mmy),
                                 Math.Abs(mdx - mmx), Math.Abs(mdy - mmy));
            form2.Invalidate();
        }

        void form2_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawRectangle(Pens.Red, rect);
        }

        void form2_MouseUp(object sender, MouseEventArgs e)
        {
            try
            {
                form2.Hide();
                Screen[] scr = Screen.AllScreens;
                Bitmap bmp = new Bitmap(rect.Width, rect.Height);
                using (Graphics G = Graphics.FromImage(bmp))
                {
                    G.CopyFromScreen(rect.Location, Point.Empty, rect.Size,
                                     CopyPixelOperation.SourceCopy);
                    SetBobber(bmp);
                }

            }
            finally
            {
                form2.Close();
                Show();
            }

        }
        public void SetBobber(Image img)
        {
            pictureBox2.Image = img;
            pictureBox2.Update();
            img.Save(Properties.Settings.Default.BobberIcon, System.Drawing.Imaging.ImageFormat.Jpeg);
        }


    }
}
