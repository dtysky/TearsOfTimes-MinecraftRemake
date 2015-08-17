using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Minecraft.Common.Helper
{
    public class Logger : TextWriter
    {
        public static Logger Instance = new Logger();
        public Logger_Content Content = new Logger_Content();

        public bool Enable = true;

        private Logger()
        {

        }

        public override void WriteLine(string Value)
        {
            if (!Enable) return;
            Content.Output.Add(Value);
            Updated(Value);
        }

        public void RunCommand(string Command)
        {
            WriteLine(Command);
            ExecuteCommand(Command);
        }

        public override Encoding Encoding
        {
            get { return Encoding.Default; }
        }

        public delegate void UpdatedEventHandler(string Value);

        public event UpdatedEventHandler Updated = delegate { };

        public delegate void ExecuteCommandEventHandler(string Value);

        public event ExecuteCommandEventHandler ExecuteCommand = delegate { };

    }

    public class Logger_Content : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        string _Input = string.Empty;
        public string Input
        {
            get
            {
                return _Input;
            }
            set
            {
                _Input = value;
                OnPropertyChanged("Input");
            }
        }
        ObservableCollection<string> _Output = new ObservableCollection<string>() { };

        public ObservableCollection<string> Output
        {
            get
            {
                return _Output;
            }
            set
            {
                _Output = value;
                OnPropertyChanged("Output");
            }
        }




    }
}
