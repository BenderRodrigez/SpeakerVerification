using System.ComponentModel;
using System.Windows;

namespace ExperimentalProcessing
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow
    {
        private MainWindow OwnerWindow { get; set; }

        public bool UseHz
        {
            get { return OwnerWindow.UseHz; }
            set { OwnerWindow.UseHz = value; }
        }

        public float HighPassFilterBorder
        {
            get
            {
                return OwnerWindow.HighPassFilterBorder;
            }
            set { OwnerWindow.HighPassFilterBorder = value; }
        }

        public float LowPassFilterBorder
        {
            get { return OwnerWindow.LowPassFilterBorder; }
            set { OwnerWindow.LowPassFilterBorder = value; }
        }

        public double EnergyLineBorder
        {
            get { return OwnerWindow.EnergyLineBorder; }
            set { OwnerWindow.EnergyLineBorder = value; }
        }

        public int FilterDiameter
        {
            get { return OwnerWindow.FilterDiameter; }
            set { OwnerWindow.FilterDiameter = value; }
        }

        public double CentralLimit
        {
            get { return OwnerWindow.CentralLimit; }
            set { OwnerWindow.CentralLimit = value; }
        }

        public double MaxFreqJumps
        {
            get { return OwnerWindow.MaxFreqJumps; }
            set { OwnerWindow.MaxFreqJumps = value; }
        }

        public float AdditveNoiseLevel
        {
            get { return OwnerWindow.AdditveNoiseLevel; }
            set { OwnerWindow.AdditveNoiseLevel = value; }
        }

        public double TonalSpeechSelectorBorder
        {
            get { return OwnerWindow.TonalSpeechSelectorBorder; }
            set { OwnerWindow.TonalSpeechSelectorBorder = value; }
        }

        public double MinimalVoicedSpeechLength
        {
            get { return OwnerWindow.MinimalVoicedSpeechLength; }
            set { OwnerWindow.MinimalVoicedSpeechLength = value; }
        }

        public SettingsWindow()
        {
            OwnerWindow = (Application.Current.MainWindow as MainWindow);
            InitializeComponent();
        }

        private void RenewCalculationsButton_OnClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.RenewCalculations();
        }

        private void RestButton_OnClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.RestButton();
        }

        private void SettingsWindow_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;//we always stay opened
        }

        private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.OpenFileButton();
        }

        private void SettingsWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            OwnerWindow = (Owner as MainWindow);
        }

        private void AlgorithmTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.TestAlogorithm();
        }
    }
}
