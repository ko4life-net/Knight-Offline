using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Diagnostics;

namespace Knight_Offline
{
    public partial class MainForm : Form
    {
        private readonly int MaximumNumberOfBots = 1500;
        private GlobalConfiguration GlobalConfiguration;
        private List<BotTemplate> BotsTemplates = new List<BotTemplate>();
        private List<VirtualPlayer> Bots = new List<VirtualPlayer>();

        public MainForm()
        {
            InitializeComponent();

            GlobalConfiguration = new GlobalConfiguration()
            {
                TargetVersion = 1298
            };
        }

        private async void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            if (Bots.Count > 0)
            {
                e.Cancel = true;

                try
                {
                    await ArrestBots();
                }
                finally
                {
                    Close();
                }
            }
        }

        private void SpawnBotsClick(object sender, EventArgs e)
        {
            // For safety reasons, changing the number of bots requires resetting the objects
            if (Bots.Count == 0)
            {
                string DesignatedServerIP = ServerIP.Text.Trim();
                int DesignatedPort = Convert.ToInt32(Port.Text);
                int DesignatedNumberOfBots = Convert.ToInt32(NumberOfBots.Text.Trim());

                if (IsIPv4Valid(DesignatedServerIP) && IsValidPort(DesignatedPort) && 1 <= DesignatedNumberOfBots && DesignatedNumberOfBots <= MaximumNumberOfBots)
                {
                    if (!BotsTemplates.Any())
                    {
                        string BotsTemplatesPath = "./Config/BotsTemplates.json";

                        if (File.Exists(BotsTemplatesPath))
                        {
                            EventLogList.Items.Add("Loading bots templates");
                            BotsTemplates = JsonSerializer.Deserialize<List<BotTemplate>>(File.ReadAllText(BotsTemplatesPath));
                        }
                        else
                        {
                            EventLogList.Items.Add("Generating bots templates");
                            BotBalancer BotBalancer = new BotBalancer();
                            BotsTemplates = BotBalancer.GenerateTemplate(MaximumNumberOfBots);
                        }
                    }

                    EventLogList.Items.Add("Spawning bots");
                    ServerIP.Enabled = Port.Enabled = NumberOfBots.Enabled = SpawnBotsButton.Enabled = false;
                    ArrestBotsButton.Enabled = Command.Enabled = SendCommandButton.Enabled = true;

                    for (ushort h = 1; h <= DesignatedNumberOfBots; ++h)
                    {
                        BotConfiguration BotConfiguration;
                        string ConfigurationPath = "./Config/bot" + h + ".json";

                        if (File.Exists(ConfigurationPath))
                        {
                            BotConfiguration = JsonSerializer.Deserialize<BotConfiguration>(File.ReadAllText(ConfigurationPath));
                        }
                        else
                        {
                            string PseudoID = "test" + $"{h:D4}";

                            BotConfiguration = new BotConfiguration
                            {
                                InstanceID = h,
                                AccountID = PseudoID,
                                Password = PseudoID,
                                BotTemplate = BotsTemplates[h - 1]
                            };
                        }

                        CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
                        Bot Bot = new Bot(GlobalConfiguration, BotConfiguration);

                        Bots.Add(new VirtualPlayer()
                        {
                            CancellationTokenSource = CancellationTokenSource,
                            Task = Task.Factory.StartNew(() => Bot.Run(CancellationTokenSource, DesignatedServerIP, DesignatedPort).Wait()), // TaskCreationOptions.LongRunning, TaskScheduler.Default
                            Bot = Bot
                        });
                    }
                }
                else
                {
                    MessageBox.Show("Invalid configuration data");
                }
            }
        }

        private async void ArrestBotsClick(object sender, EventArgs e)
        {            
            ArrestBotsButton.Enabled = Command.Enabled = SendCommandButton.Enabled = false;
            await ArrestBots();
            ServerIP.Enabled = Port.Enabled = NumberOfBots.Enabled = SpawnBotsButton.Enabled = true;
        }

        // May also be called HaltBots, InhibitBots or SuppressBots
        private async Task ArrestBots()
        {
            EventLogList.Items.Add("Halting bots");

            Bots.ForEach(delegate (VirtualPlayer VirtualPlayer)
            {
                VirtualPlayer.CancellationTokenSource.Cancel();
            });

            await Task.WhenAll(Bots.Select(t => t.Task).ToList());
            Bots.Clear();
        }

        private void CommandKeyDown(object sender, KeyEventArgs e)
        {
            /*
            if (e.KeyData == Keys.Enter)
            {
                ServiceCommand(Command.Text.Trim());
                e.Handled = e.SuppressKeyPress = true;
                Command.Text = "";
                ActiveControl = Command;
            }
            */
        }

        private void SendCommandClick(object sender, EventArgs e)
        {            
            ServiceCommand(Command.Text);
            Command.Text = "";
            ActiveControl = Command;
        }

        // Not fixed yet
        private void CommunicationListMeasureItem(object sender, MeasureItemEventArgs e)
        {
            // e.ItemHeight = (int)e.Graphics.MeasureString(CommunicationList.Items[e.Index].ToString(), CommunicationList.Font, CommunicationList.Width).Height;
        }

        private void CommunicationListDrawItem(object sender, DrawItemEventArgs e)
        {
            /*
            e.DrawBackground();
            e.DrawFocusRectangle();
            e.Graphics.DrawString(CommunicationList.Items[e.Index].ToString(), e.Font, new SolidBrush(e.ForeColor), e.Bounds);
            */
        }

        private void ServiceCommand(string Command)
        {
            Command = Command.Trim();

            if (Command.Length > 0)
            {
                if (Command.Substring(0, 1) == "/")
                {
                    Command = Command.TrimStart('/');
                }

                string[] SplitCommand = Command.Split(' ');
                SplitCommand[0] = SplitCommand[0].ToLower();

                switch(SplitCommand[0])
                {
                    case "test":
                        TestingEnvironment();
                        break;
                    
                }
            }
        }

        private void TestingEnvironment()
        {
            // High quality gameplay here
            // Bots[0].Bot.test();
        }

        private bool IsIPv4Valid(string IPAddress)
        {
            if (string.IsNullOrWhiteSpace(IPAddress))
            {
                return false;
            }

            string[] Octets = IPAddress.Split('.');

            if (Octets.Length != 4)
            {
                return false;
            }

            return Octets.All(r => byte.TryParse(r, out byte TemporaryForParsing));
        }

        private bool IsValidPort(int Port)
        {
            return 1 <= Port && Port <= 65535;
        }
    }

    public class VirtualPlayer
    {
        public CancellationTokenSource CancellationTokenSource;
        public Task Task;
        public Bot Bot;
    }
}