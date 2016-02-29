using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ExperimentalProcessing.Annotations;

namespace ExperimentalProcessing
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow: INotifyPropertyChanged
    {
        public MainWindow OwnerWindow { get; set; }

        public string SignalNoiseRaito
        {
            get { return OwnerWindow.SignalNoiseRaito; }
        }

        public double NoiseAmplitude
        {
            get { return OwnerWindow.NoiseAmplitude; }
            set { OwnerWindow.NoiseAmplitude = value; }
        }

        public bool UseHz
        {
            get { return OwnerWindow.UseHz; }
            set { OwnerWindow.UseHz = value; }
        }

        public bool UseNoise
        {
            get { return OwnerWindow.UseNoise; }
            set { OwnerWindow.UseNoise = value; }
        }

        public string MaxEnergyInterval
        {
            get { return OwnerWindow.MaxEnergyInterval; }
            set { OwnerWindow.MaxEnergyInterval = value; }
        }

        public float HighPassFilterBorder
        {
            get
            {
                return OwnerWindow.HighPassFilterBorder;
            }
            set { OwnerWindow.HighPassFilterBorder = value; }
        }

        public bool SimulatePhoneChanel
        {
            get
            {
                return OwnerWindow.SimulatePhoneCnanel;
            }
            set { OwnerWindow.SimulatePhoneCnanel = value; }
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
            if (OwnerWindow != null)
                OwnerWindow.PropertyChanged += (sender, args) => OnPropertyChanged(args.PropertyName);
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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        private void GenerateReport_OnClick(object sender, RoutedEventArgs e)
        {
            OwnerWindow.GenerateReport();
        }
    }
}
