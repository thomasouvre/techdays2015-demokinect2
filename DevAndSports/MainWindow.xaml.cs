using DevAndSports.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Runtime.CompilerServices;
using System.Windows.Controls.Primitives;

namespace DevAndSports
{
    public class MainWindowViewModel : ViewModelBase
    {
        private CameraMode _cameraMode;
        private bool _usageCasesMode;
        private bool _showBodySource;
        private bool _showViewGestureMode;
        private ObservableCollection<Settings> _settings;
        private Settings _selected;

        public CameraMode CameraMode
        {
            get { return _cameraMode; }
            set { SetByRef(ref _cameraMode, value); }
        }

        public bool UsageCasesMode
        {
            get { return _usageCasesMode; }
            set { SetByRef(ref _usageCasesMode, value); }
        }

        public bool ShowBodySource
        {
            get { return _showBodySource; }
            set { SetByRef(ref _showBodySource, value); }
        }

        public bool ShowVisualGestureMode
        {
            get { return _showViewGestureMode; }
            set { SetByRef(ref _showViewGestureMode, value); }
        }

        public IEnumerable<Settings> SettingsList => _settings;

        public Settings Selected
        {
            get { return _selected; }
            set { SetByRef(ref _selected, value); }
        }

        public MainWindowViewModel()
        {
            _settings = new ObservableCollection<Settings>
            {
                new Settings { Label = "Usages", UsageCasesMode = true, CameraMode = CameraMode.None, ShowBodySource = false },
                new Settings { Label = "None Mode", CameraMode = CameraMode.None },
                new Settings { Label = "Color Mode", CameraMode = CameraMode.Color },
                new Settings { Label = "Depth Mode", CameraMode = CameraMode.Depth },
                new Settings { Label = "Infrared Mode", CameraMode = CameraMode.Infrared },
                new Settings { Label = "Body Index Mode", CameraMode = CameraMode.BodyIndex },
                new Settings { Label = "Green Screening Mode", CameraMode = CameraMode.GreenScreening },
                new Settings { Label = "Body", UsageCasesMode = false, ShowBodySource = true },
                new Settings { Label = "Final", UsageCasesMode = false, ShowBodySource = false, ShowVisualGestureMode = true },
            };
        }

        protected override void OnPropertyChanged([CallerMemberName]string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            switch (propertyName)
            {
                case nameof(Selected):
                    OnSelectedChanged();
                    break;
                default:
                    break;
            }
        }

        private void OnSelectedChanged()
        {
            var selected = Selected;
            if (selected == null) return;
            if (selected.CameraMode.HasValue)
            {
                CameraMode = selected.CameraMode.Value;
                UsageCasesMode = false;
            }
            if (selected.UsageCasesMode.HasValue)
            {
                //CameraMode = CameraMode.None;
                UsageCasesMode = selected.UsageCasesMode.Value;
            }
            if (selected.ShowBodySource.HasValue)
            {
                ShowBodySource = selected.ShowBodySource.Value;
            }
            if (selected.ShowVisualGestureMode.HasValue)
            {
                ShowVisualGestureMode = selected.ShowVisualGestureMode.Value;
            }
        }

        public class Settings
        {
            public string Label { get; set; }
            public CameraMode? CameraMode { get; set; }
            public bool? UsageCasesMode { get; set; }
            public bool? ShowBodySource { get; set; }
            public bool? ShowVisualGestureMode { get; set; }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DateTime _lastGripClickTime;

        public MainWindow()
        {
            InitializeComponent();
            InitGrip();
            ComputeWindowState();
        }

        private void InitGrip()
        {
            Grip.MouseDown += Grip_MouseDown;
        }

        private void Grip_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (DateTime.Now - _lastGripClickTime < TimeSpan.FromMilliseconds(500))
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else if (WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
            }
            else
            {
                _lastGripClickTime = DateTime.Now;
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ComputeWindowState();
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            ComputeWindowState();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            ComputeWindowState();
        }

        private void ComputeWindowState()
        {
            switch (WindowState)
            {
                case WindowState.Normal:
                    Root.Margin = new Thickness(0);
                    RestoreButton.Visibility = Visibility.Collapsed;
                    MaximizeButton.Visibility = Visibility.Visible;
                    break;
                case WindowState.Minimized:
                    break;
                case WindowState.Maximized:
                    Root.Margin = new Thickness(5);
                    RestoreButton.Visibility = Visibility.Visible;
                    MaximizeButton.Visibility = Visibility.Collapsed;
                    break;
                default:
                    break;
            }
        }
    }
}
