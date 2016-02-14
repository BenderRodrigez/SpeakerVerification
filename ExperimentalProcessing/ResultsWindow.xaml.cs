using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ExperimentalProcessing.Annotations;

namespace ExperimentalProcessing
{
    /// <summary>
    /// Логика взаимодействия для ResultsWindow.xaml
    /// </summary>
    public partial class ResultsWindow : INotifyPropertyChanged
    {
        private string _results;

        public string Results
        {
            get { return _results; }
            set
            {
                _results = value; 
                OnPropertyChanged("Results");
            }
        }

        public ResultsWindow()
        {
            InitializeComponent();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
