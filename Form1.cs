using System;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace InternetLog
{
    public partial class Form1 : Form
    {
        // === AYARLAR ===
        const int CHECK_INTERVAL_MS = 1000; // 1 saniye
        const int RECOVERY_SECONDS = 15;

        string logFile = "internet_log.txt";

        // === DURUM DEĞİŞKENLERİ ===
        bool isOnline = true;
        bool isRecovering = false;

        DateTime? disconnectTime = null;
        DateTime? recoveryStartTime = null;

        System.Windows.Forms.Timer timer;

        public Form1()
        {
            InitializeComponent();
            Init();
        }

        void Init()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Interval = CHECK_INTERVAL_MS;
            timer.Tick += Timer_Tick;
            timer.Start();

            Log("UYGULAMA BAŞLADI");
            Log("--------------------------------");
        }

        // === TIMER ===
        void Timer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            bool internetOk = IsInternetAvailable(out string reason);

            // 🔴 ONLINE → OFFLINE
            if (!internetOk && isOnline)
            {
                isOnline = false;
                isRecovering = false;
                disconnectTime = now;

                Log($"İNTERNET GİTTİ → {reason}");
            }

            // 🔴 OFFLINE DEVAM
            else if (!internetOk && !isOnline)
            {
                Log($"HALA YOK ({(now - disconnectTime.Value).TotalSeconds:F0} sn) → {reason}");
            }

            // 🟢 OFFLINE → ONLINE (RECOVERY BAŞLAR)
            else if (internetOk && !isOnline && !isRecovering)
            {
                isRecovering = true;
                recoveryStartTime = now;

                Log("İNTERNET GELDİ → 15 SN STABİLİTE KONTROLÜ");
            }

            // 🟡 RECOVERY SÜRECİ
            else if (internetOk && isRecovering)
            {
                double passed = (now - recoveryStartTime.Value).TotalSeconds;
                Log($"STABİL KONTROL: {passed:F0}/{RECOVERY_SECONDS} sn");

                if (passed >= RECOVERY_SECONDS)
                {
                    isRecovering = false;
                    isOnline = true;

                    Log("İNTERNET STABİL");
                    Log("--------------------------------");
                }
            }

            // 🟠 RECOVERY SIRASINDA TEKRAR GİDERSE
            else if (!internetOk && isRecovering)
            {
                isRecovering = false;
                isOnline = false;
                disconnectTime = now;

                Log("STABİL DEĞİL → TEKRAR GİTTİ");
            }
        }

        // === INTERNET DURUMU ===
        bool IsInternetAvailable(out string reason)
        {
            bool modem = PingTest("192.168.50.1");
            bool google = PingTest("8.8.8.8");
            bool cloud = PingTest("1.1.1.1");

            if (!modem)
            {
                reason = "MODEM YOK (LAN DOWN)";
                return false;
            }

            if (google || cloud)
            {
                reason = "İNTERNET VAR";
                return true;
            }

            reason = "ISS / WAN DOWN";
            return false;
        }

        // === PING ===
        bool PingTest(string host)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(host, 1000);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch
            {
                return false;
            }
        }

        // === LOG ===
        void Log(string message)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";

            // TXT log
            File.AppendAllText(logFile, line + Environment.NewLine);

            // RichTextBox log
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.Invoke(new Action(() =>
                {
                    richTextBox1.AppendText(line + Environment.NewLine);
                    richTextBox1.ScrollToCaret();
                }));
            }
            else
            {
                richTextBox1.AppendText(line + Environment.NewLine);
                richTextBox1.ScrollToCaret();
            }
        }

    }
}
