using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Globalization;
using Timer = System.Windows.Forms.Timer;

namespace InternetLog
{
    public partial class Form1 : Form
    {
        Timer pingTimer;
        string logFilePath = "internet_log.txt";

        bool? lastInternetState = null;
        bool lastGoogleState;
        bool lastCloudState;
        bool lastModemState;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadOldLogs();
            Log("UYGULAMA BAŞLADI");

            CheckInternet(firstRun: true);
            StartPingTimer();
        }

        // =======================
        // TIMER
        // =======================
        void StartPingTimer()
        {
            pingTimer = new Timer();
            pingTimer.Interval = 15000;
            pingTimer.Tick += (s, e) => CheckInternet(false);
            pingTimer.Start();
        }

        // =======================
        // ANA KONTROL
        // =======================
               

        void CheckInternet(bool firstRun)
        {
            bool google = PingHost("8.8.8.8");
            bool cloud = PingHost("1.1.1.1");
            bool modem = PingHost("192.168.50.1");

            bool internetAvailable = google || cloud;

            if (firstRun)
            {
                lastGoogleState = google;
                lastCloudState = cloud;
                lastModemState = modem;
                lastInternetState = internetAvailable;

                Log(internetAvailable ? "İNTERNET VAR" : "İNTERNET YOK");
                UpdateFormTitle();
                return;
            }

            // GENEL İNTERNET DURUMU (öncelikli)
            if (lastInternetState != internetAvailable)
            {
                Log(internetAvailable ? "İNTERNET GELDİ" : "İNTERNET GİTTİ");
                lastInternetState = internetAvailable;
            }
            else
            {
                // TEK TEK KONTROLLER (sadece genel durum değişmediyse)

                if (lastGoogleState != google)
                    Log(google ? "Google bağlantısı GELDİ" : "Google bağlantısı GİTTİ");

                if (lastCloudState != cloud)
                    Log(cloud ? "Cloud bağlantısı GELDİ" : "Cloud bağlantısı GİTTİ");
            }

            // MODEM HER ZAMAN BAĞIMSIZ
            if (lastModemState != modem)
                Log(modem ? "Modem bağlantısı GELDİ" : "Modem bağlantısı GİTTİ");

            lastGoogleState = google;
            lastCloudState = cloud;
            lastModemState = modem;

            UpdateFormTitle();
        }


        // =======================
        // PING
        // =======================
        bool PingHost(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    return ping.Send(host, 3000).Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // =======================
        // LOG
        // =======================
        void Log(string message)
        {
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {message}";
            File.AppendAllText(logFilePath, line + Environment.NewLine);
            richTextBox1.AppendText(line + Environment.NewLine);
            richTextBox1.ScrollToCaret();
            UpdateFormTitle();
        }


        void LoadOldLogs()
        {
            if (File.Exists(logFilePath))
                richTextBox1.Text = File.ReadAllText(logFilePath);

            UpdateFormTitle();
        }


        // =======================
        // SON 24 SAAT KOPMA
        // =======================
        int GetLast24HoursDropCount()
        {
            if (!File.Exists(logFilePath))
                return 0;

            DateTime limit = DateTime.Now.AddHours(-24);
            int count = 0;

            foreach (var line in File.ReadLines(logFilePath))
            {
                if (!line.Contains("İNTERNET GİTTİ"))
                    continue;

                DateTime logTime;

                // 🟢 ESKİ FORMAT: [yyyy-MM-dd HH:mm:ss]
                if (line.StartsWith("["))
                {
                    int end = line.IndexOf(']');
                    if (end <= 1)
                        continue;

                    string datePart = line.Substring(1, end - 1);

                    if (!DateTime.TryParseExact(
                        datePart,
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out logTime))
                        continue;
                }
                // 🟢 YENİ FORMAT: yyyy-MM-dd HH:mm:ss |
                else
                {
                    string datePart = line.Substring(0, 19);

                    if (!DateTime.TryParseExact(
                        datePart,
                        "yyyy-MM-dd HH:mm:ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out logTime))
                        continue;
                }

                if (logTime >= limit)
                    count++;
            }

            return count;
        }


        // =======================
        // FORM BAŞLIĞI
        // =======================
        void UpdateFormTitle()
        {
            int drops = GetLast24HoursDropCount();
            Text = $"InternetLog – Son 24 Saat: {drops} Kopma";
        }
    }
}
