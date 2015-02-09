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

namespace DevAndSports
{
    /// <summary>
    /// Interaction logic for ZoomImagePopupControl.xaml
    /// </summary>
    public partial class ZoomImagePopupControl : UserControl
    {
        public ZoomImagePopupControl(ImageSource source)
        {
            InitializeComponent();
            Content.Source = source;
        }

        private void OnLoadedStoryboardCompleted(object sender, System.EventArgs e)
        {
            var parent = (Panel)this.Parent;
            parent.Children.Remove(this);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
