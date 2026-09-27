using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace InternetLog
{
    public partial class Form1 : Form
    {
        const string GoogleHost = "8.8.8.8";
        const string CloudHost = "1.1.1.1";
        const int PingTimeoutMs = 3000;
        const int CheckIntervalMs = 15000;
        const int MaxDisplayLines = 500;
        const string DateFormat = "yyyy-MM-dd HH:mm:ss";
        const string DropMarker = "İNTERNET GİTTİ";

        // Log dosyası her zaman exe'nin yanında tutulur (çalışma klasöründen bağımsız)
        readonly string logFilePath = Path.Combine(AppContext.BaseDirectory, "internet_log.txt");

        // Son 24 saatteki kopmalar bellekte tutulur, dosya her seferinde okunmaz
        readonly List<DateTime> dropTimes = new List<DateTime>();

        Timer pingTimer;
        bool isChecking;

        bool? lastInternetState = null;
        bool lastGoogleState;
        bool lastCloudState;
        bool lastModemState;
        DateTime? internetLostAt;

        public Form1()
        {
            InitializeComponent();
            FormClosed += (s, e) => pingTimer?.Dispose();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            LoadOldLogs();
            Log("UYGULAMA BAŞLADI");

            await CheckInternetAsync(firstRun: true);
            StartPingTimer();
        }

        // =======================
        // TIMER
        // =======================
        void StartPingTimer()
        {
            pingTimer = new Timer();
            pingTimer.Interval = CheckIntervalMs;
            pingTimer.Tick += async (s, e) => await CheckInternetAsync(false);
            pingTimer.Start();
        }

        // =======================
        // ANA KONTROL
        // =======================
        async Task CheckInternetAsync(bool firstRun)
        {
            // Önceki kontrol hâlâ sürüyorsa üst üste binmesin
            if (isChecking)
                return;

            isChecking = true;
            try
            {
                string modemHost = GetDefaultGateway();

                // Pingler paralel ve asenkron: arayüz donmaz
                Task<bool> googleTask = PingHostAsync(GoogleHost);
                Task<bool> cloudTask = PingHostAsync(CloudHost);
                Task<bool> modemTask = modemHost != null ? PingHostAsync(modemHost) : Task.FromResult(false);
                await Task.WhenAll(googleTask, cloudTask, modemTask);

                if (IsDisposed)
                    return;

                bool google = googleTask.Result;
                bool cloud = cloudTask.Result;
                bool modem = modemTask.Result;

                bool internetAvailable = google || cloud;

                if (firstRun)
                {
                    lastGoogleState = google;
                    lastCloudState = cloud;
                    lastModemState = modem;
                    lastInternetState = internetAvailable;

                    if (!internetAvailable)
                        internetLostAt = DateTime.Now;

                    Log(internetAvailable ? "İNTERNET VAR" : "İNTERNET YOK");
                    Log(modemHost != null ? $"Modem adresi: {modemHost}" : "Modem adresi bulunamadı");
                    return;
                }

                // GENEL İNTERNET DURUMU (öncelikli)
                if (lastInternetState != internetAvailable)
                {
                    if (internetAvailable)
                    {
                        string duration = internetLostAt.HasValue
                            ? $" ({FormatDuration(DateTime.Now - internetLostAt.Value)} kesinti)"
                            : "";
                        Log("İNTERNET GELDİ" + duration);
                        internetLostAt = null;
                    }
                    else
                    {
                        internetLostAt = DateTime.Now;
                        Log(DropMarker);
                    }

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

                // Kayıt düşmese de eski kopmalar 24 saati aşınca başlık güncellensin
                UpdateFormTitle();
            }
            finally
            {
                isChecking = false;
            }
        }

        static string FormatDuration(TimeSpan span)
        {
            if (span.TotalHours >= 1)
                return $"{(int)span.TotalHours} sa {span.Minutes} dk {span.Seconds} sn";
            if (span.TotalMinutes >= 1)
                return $"{span.Minutes} dk {span.Seconds} sn";
            return $"{span.Seconds} sn";
        }

        // =======================
        // PING
        // =======================
        static async Task<bool> PingHostAsync(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(host, PingTimeoutMs);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // =======================
        // MODEM (VARSAYILAN AĞ GEÇİDİ)
        // =======================
        static string GetDefaultGateway()
        {
            try
            {
                IPAddress gateway = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                             && n.NetworkInterfaceType != NetworkInterfaceType.Loopback
                             && n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                    .SelectMany(n => n.GetIPProperties().GatewayAddresses)
                    .Select(g => g.Address)
                    .FirstOrDefault(a => a != null
                                      && a.AddressFamily == AddressFamily.InterNetwork
                                      && !a.Equals(IPAddress.Any));

                return gateway?.ToString();
            }
            catch
            {
                return null;
            }
        }

        // =======================
        // LOG
        // =======================
        void Log(string message)
        {
            DateTime now = DateTime.Now;
            string line = $"{now.ToString(DateFormat, CultureInfo.InvariantCulture)} | {message}";

            try
            {
                File.AppendAllText(logFilePath, line + Environment.NewLine);
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"(Log dosyasına yazılamadı: {ex.Message}){Environment.NewLine}");
            }

            if (message == DropMarker)
                dropTimes.Add(now);

            richTextBox1.AppendText(line + Environment.NewLine);
            TrimDisplay();
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();
            UpdateFormTitle();
        }

        // Ekranda yalnızca son MaxDisplayLines satır tutulur
        void TrimDisplay()
        {
            int excess = richTextBox1.Lines.Length - MaxDisplayLines - 1;
            if (excess <= 0)
                return;

            int cut = richTextBox1.GetFirstCharIndexFromLine(excess);
            richTextBox1.Select(0, cut);
            richTextBox1.SelectedText = "";
        }

        void LoadOldLogs()
        {
            if (!File.Exists(logFilePath))
                return;

            DateTime limit = DateTime.Now.AddHours(-24);
            var lastLines = new Queue<string>(MaxDisplayLines);

            try
            {
                // Dosya tek seferde okunur: kopma sayısı ve ekrandaki son satırlar birlikte çıkarılır
                foreach (var line in File.ReadLines(logFilePath))
                {
                    if (line.Contains(DropMarker)
                        && TryParseLogTime(line, out DateTime logTime)
                        && logTime >= limit)
                        dropTimes.Add(logTime);

                    if (lastLines.Count == MaxDisplayLines)
                        lastLines.Dequeue();
                    lastLines.Enqueue(line);
                }
            }
            catch (Exception ex)
            {
                richTextBox1.AppendText($"(Eski loglar okunamadı: {ex.Message}){Environment.NewLine}");
                return;
            }

            if (lastLines.Count > 0)
                richTextBox1.Text = string.Join(Environment.NewLine, lastLines) + Environment.NewLine;
        }

        // Hem eski "[yyyy-MM-dd HH:mm:ss] ..." hem yeni "yyyy-MM-dd HH:mm:ss | ..." formatını okur
        static bool TryParseLogTime(string line, out DateTime logTime)
        {
            logTime = default;
            string datePart;

            if (line.StartsWith("["))
            {
                int end = line.IndexOf(']');
                if (end <= 1)
                    return false;

                datePart = line.Substring(1, end - 1);
            }
            else
            {
                if (line.Length < DateFormat.Length)
                    return false;

                datePart = line.Substring(0, DateFormat.Length);
            }

            return DateTime.TryParseExact(
                datePart,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out logTime);
        }

        // =======================
        // FORM BAŞLIĞI (SON 24 SAAT KOPMA)
        // =======================
        void UpdateFormTitle()
        {
            DateTime limit = DateTime.Now.AddHours(-24);
            dropTimes.RemoveAll(t => t < limit);
            Text = $"InternetLog – Son 24 Saat: {dropTimes.Count} Kopma";
        }
    }
}
