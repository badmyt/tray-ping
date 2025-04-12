using NAudio.Wave;
using System.Net.NetworkInformation;
using System.Timers;

namespace TrayPing
{
    public partial class Form1 : Form
    {
        private const int SoundIntervalMilliseconds = 60_000 * 10; // 10 min
        private const int PingIntervalMilliseconds = 1000; // 1 sec

        private MemoryStream _silentSoundStream = null;

        private NotifyIcon trayIcon;
        private ContextMenuStrip trayMenu;
        private ToolStripMenuItem pauseResumeMenuItem;
        private System.Timers.Timer pingTimer;
        private System.Timers.Timer soundTimer;
        private Icon greenIcon;
        private Icon redIcon;
        private Icon yellowIcon;
        private bool isPaused = false;
        private Mutex singleInstanceMutex;

        public Form1()
        {
            InitializeDesigner();
            InitializeApp();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void InitializeApp()
        {
            // Check if another instance of the application is already running
            singleInstanceMutex = new Mutex(true, "TrayPingMutex", out bool createdNew);
            if (!createdNew)
            {
                Environment.Exit(0);
                return;
            }

            // Initialize Tray Icon
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Tray ping";
            trayIcon.Visible = true;

            // Load Icons
            greenIcon = new Icon("icons/green.ico");
            redIcon = new Icon("icons/red.ico");
            yellowIcon = new Icon("icons/yellow.ico");

            // Set initial icon
            trayIcon.Icon = redIcon;

            // Initialize Tray Menu
            trayMenu = new ContextMenuStrip();
            pauseResumeMenuItem = new ToolStripMenuItem("Pause", null, OnPauseResumeClick);
            trayMenu.Items.Add(pauseResumeMenuItem);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("Exit", null, OnExitClick);
            trayIcon.ContextMenuStrip = trayMenu;

            // Initialize Ping Timer
            pingTimer = new System.Timers.Timer(PingIntervalMilliseconds);
            pingTimer.Elapsed += OnPingTimerElapsed;
            pingTimer.AutoReset = true;
            pingTimer.Enabled = true;

            // Initialize Sound Timer
            soundTimer = new System.Timers.Timer(SoundIntervalMilliseconds);
            soundTimer.Elapsed += OnSoundTimerElapsed;
            soundTimer.AutoReset = true;
            soundTimer.Enabled = true;

            // Load Sound
            _silentSoundStream = LoadSilenceMp3();

            // Hide form window
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
        }

        private void OnPingTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (isPaused) return;

            bool pingable = false;
            Ping ping = new Ping();
            try
            {
                PingReply reply = ping.Send("8.8.8.8");
                pingable = reply.Status == IPStatus.Success;
            }
            catch (PingException)
            {
                // Discard PingExceptions and return false;
            }

            // Update tray icon based on ping result
            trayIcon.Icon = pingable ? greenIcon : redIcon;
        }

        private void OnSoundTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (isPaused) return;

            _silentSoundStream.Position = 0;

            using (var reader = new WaveFileReader(_silentSoundStream))
            using (var volumeStream = new WaveChannel32(reader) { Volume = 0.01f })
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(volumeStream);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(100);
                }
            }
        }

        private void OnPauseResumeClick(object sender, EventArgs e)
        {
            isPaused = !isPaused;

            if (isPaused)
            {
                // Pause timers
                pingTimer.Stop();
                soundTimer.Stop();
                trayIcon.Icon = yellowIcon;
                pauseResumeMenuItem.Text = "Resume";
            }
            else
            {
                // Resume timers
                pingTimer.Start();
                soundTimer.Start();
                trayIcon.Icon = redIcon; // Reset to initial state
                pauseResumeMenuItem.Text = "Pause";
            }
        }

        private void OnExitClick(object sender, EventArgs e)
        {
            // Release the mutex before exiting
            singleInstanceMutex.ReleaseMutex();

            // Exit application
            Environment.Exit(0);
        }

        private MemoryStream LoadSilenceMp3()
        {
            // Define the path to the silence.mp3 file
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "sounds", "arrow.wav");

            // Read the file into a byte array
            byte[] fileBytes = File.ReadAllBytes(filePath);

            // Create a MemoryStream from the byte array
            var stream  = new MemoryStream(fileBytes);

            return stream;
        }
    }
}
