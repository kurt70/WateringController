using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace WateringApplication
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void swtRunning_Toggled(object sender, RoutedEventArgs e)
        {
            if (swtRunning.IsOn)
            {

            }

        }

        private void btnManualRun_Checked(object sender, RoutedEventArgs e)
        {
            var btn = (ToggleButton)sender;
            if (btn.IsChecked.HasValue && btn.IsChecked.Value)
            {
                btn.Content = "Running.....";
            }
            else if (btn.IsChecked.HasValue && !btn.IsChecked.Value)
            {
                btn.Content = "Run manually";
            }

        }
    }
}
