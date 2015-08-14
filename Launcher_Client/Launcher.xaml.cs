using System;
using System.Collections.Generic;
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
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

        private void Button_Launch_Click(object sender, RoutedEventArgs e)
        {

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
        }

        private void CheckBox_Log_Unchecked(object sender, RoutedEventArgs e)
        {
            Scroller.Visibility = Visibility.Hidden;
            InputBlock.Visibility = Visibility.Hidden;
        }
    }
}
