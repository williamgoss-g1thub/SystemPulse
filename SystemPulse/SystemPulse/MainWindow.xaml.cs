using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace SystemPulse
{
    public partial class MainWindow : Window
    {
        private LoggingService? _loggingService;
        private NotifyIcon? _trayIcon;
        private System.Windows.Threading.DispatcherTimer? _uiTimer;

        public MainWindow()
        {
            InitializeComponent();

            Database.Initialize();

            _loggingService = new LoggingService();
            _loggingService.Start();

            SetupTrayIcon();
            LoadRecentReadings();
            CheckForAlerts();
            StartUiRefreshTimer();
        }

        private void StartUiRefreshTimer()
        {
            _uiTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _uiTimer.Tick += (s, e) =>
            {
                LoadRecentReadings();
                AlertPanel.Children.Clear();
                CheckForAlerts();
            };
            _uiTimer.Start();
        }

        private void SetupTrayIcon()
        {
            _trayIcon = new NotifyIcon
            {
                Text = "SystemPulse",
                Icon = new System.Drawing.Icon(System.IO.Path.Combine(AppContext.BaseDirectory, "systempulse_icon.ico")),
                Visible = true
            };

            _trayIcon.DoubleClick += (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Open", null, (s, e) =>
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            });
            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _loggingService?.Stop();
                _trayIcon!.Visible = false;
                Application.Current.Shutdown();
            });
            _trayIcon.ContextMenuStrip = contextMenu;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
            base.OnStateChanged(e);
        }

        private void LoadRecentReadings()
        {
            var rows = new List<ReadingRow>();

            using var connection = new Microsoft.Data.Sqlite.SqliteConnection(Database.ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
        SELECT Timestamp, SensorType, Value, SystemState
        FROM Readings
        WHERE Timestamp >= $cutoff
        ORDER BY Id DESC;
    ";
            command.Parameters.AddWithValue("$cutoff", DateTime.UtcNow.AddHours(-24).ToString("o"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                rows.Add(new ReadingRow
                {
                    Timestamp = DateTime.Parse(reader.GetString(0)).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                    SensorType = reader.GetString(1),
                    Value = reader.GetDouble(2),
                    SystemState = reader.GetString(3)
                });
            }

            ReadingsGrid.ItemsSource = rows;

            if (rows.Count > 0)
            {
                var latestBattery = rows.FirstOrDefault(r => r.SensorType == "battery_health_pct");
                var latestCharge = rows.FirstOrDefault(r => r.SensorType == "battery_charge_pct");
                var latestGpu = rows.FirstOrDefault(r => r.SensorType == "gpu_temp");

                if (latestBattery != null)
                {
                    BatteryHealthText.Text = $"{latestBattery.Value:F0}%";
                    BatteryHealthText.Foreground = latestBattery.Value >= 80 ? System.Windows.Media.Brushes.LimeGreen
                        : latestBattery.Value >= 60 ? System.Windows.Media.Brushes.Orange
                        : System.Windows.Media.Brushes.OrangeRed;
                }
                if (latestCharge != null)
                {
                    BatteryChargeText.Text = $"{latestCharge.Value:F0}%";
                    BatteryChargeText.Foreground = latestCharge.Value >= 40 ? System.Windows.Media.Brushes.LimeGreen
                        : latestCharge.Value >= 20 ? System.Windows.Media.Brushes.Orange
                        : System.Windows.Media.Brushes.OrangeRed;
                }
                if (latestGpu != null)
                {
                    GpuTempText.Text = $"{latestGpu.Value:F0}°C";
                    GpuTempText.Foreground = latestGpu.Value <= 70 ? System.Windows.Media.Brushes.LimeGreen
                        : latestGpu.Value <= 85 ? System.Windows.Media.Brushes.Orange
                        : System.Windows.Media.Brushes.OrangeRed;
                }
            }
        }

        private void CheckForAlerts()
        {
            var alerts = DriftDetector.CheckForDrift();

            foreach (var alert in alerts)
            {
                string message = alert.SensorType == "battery_health_pct"
                    ? $"⚠ Battery health has dropped to {alert.CurrentValue:F1}% (from {alert.BaselineValue:F1}%)"
                    : $"⚠ GPU idle temp is up {alert.DeltaPercent:F1}°C vs your 14-day average";

                var textBlock = new System.Windows.Controls.TextBlock
                {
                    Text = message,
                    Foreground = System.Windows.Media.Brushes.OrangeRed,
                    FontWeight = System.Windows.FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 4)
                };
                AlertPanel.Children.Add(textBlock);
            }

            if (alerts.Count == 0)
            {
                AlertPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = "✓ No unusual trends detected",
                    Foreground = System.Windows.Media.Brushes.Green
                });
            }
        }
    }
}