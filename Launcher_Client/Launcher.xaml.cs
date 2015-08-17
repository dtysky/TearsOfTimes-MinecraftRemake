using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Minecraft.Launcher
{
    using Minecraft.Common.Helper;

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        private void Initialize()
        {
            Logger Log = Logger.Instance;
            Console.SetOut(Log);
            DataContext = Log.Content;
            Loaded += new RoutedEventHandler(delegate (object sender, RoutedEventArgs e)
            {
                InputBlock.KeyDown += new KeyEventHandler(delegate (object _sender, KeyEventArgs _e)
                {
                    if (_e.Key == Key.Enter)
                    {
                        Log.RunCommand(InputBlock.Text);
                        InputBlock.Text = "";
                        InputBlock.Focus();
                    }
                });
                Log.Updated += new Logger.UpdatedEventHandler(delegate (string value)
                {
                    Scroller.ScrollToBottom();
                    if (Log.Content.Output.Count > 500)
                        Log.Content.Output.RemoveAt(0);
                });
            }); 
        }

        private void Drag(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Min_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Button_Settings_Click(object sender, RoutedEventArgs e)
        {
            if (Settings_Form.Visibility == Visibility.Visible)
                Settings_Form.Visibility = Visibility.Hidden;
            else
                Settings_Form.Visibility = Visibility.Visible;
        }

        private void CheckBox_Log_Checked(object sender, RoutedEventArgs e)
        {
            Scroller.Visibility = Visibility.Visible;
            InputBlock.Visibility = Visibility.Visible;
            Logger.Instance.Enable = true;
        }

        private void CheckBox_Log_Unchecked(object sender, RoutedEventArgs e)
        {
            Scroller.Visibility = Visibility.Hidden;
            InputBlock.Visibility = Visibility.Hidden;
            Logger.Instance.Enable = false;
        }

        private void Button_Launch_Click(object sender, RoutedEventArgs e)
        {
            Loader.Client.DX12Test.Run();
        }
    }
}
