using DevAndSports.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using Microsoft.Kinect.Wpf.Controls;
using Microsoft.Kinect;
using System.Windows.Controls.Primitives;
using System.Globalization;
using System.IO;

namespace DevAndSports
{
    public enum CameraMode
    {
        None,
        Color,
        Infrared,
        Depth,
        BodyIndex,
        GreenScreening
    }

    public class KinectUsageStore
    {
        private List<Usage> _usages { get; set; }

        public IEnumerable<Usage> Usages => _usages;

        public KinectUsageStore()
        {
            _usages = new List<Usage>
            {
                Get("retail / marketing", "1.jpg", "2.jpg"),
                Get("therapie", "3.jpg", "4.jpg"),
                Get("santé", "5.jpg", "6.jpg"),
                Get("éducation", "7.png", "8.jpg"),
                Get("formation", "9.jpg", "10.jpg"),
            };
        }

        private static Usage Get(string name, string i1, string i2)
        {
            return new Usage
            {
                Name = name.ToUpperInvariant(),
                Image1 = "/Assets/Images/Usages/Picture" + i1,
                Image2 = "/Assets/Images/Usages/Picture" + i2
            };
        }


        public class Usage
        {
            public string Name { get; set; }
            public string Image1 { get; set; }
            public string Image2 { get; set; }
        }
    }

    public partial class KinectControl : UserControl
    {
        #region CameraMode

        public CameraMode CameraMode
        {
            get { return (CameraMode)GetValue(CameraModeProperty); }
            set { SetValue(CameraModeProperty, value); }
        }

        public static readonly DependencyProperty CameraModeProperty =
            DependencyProperty.Register("CameraMode", typeof(CameraMode), typeof(KinectControl), new PropertyMetadata(default(CameraMode), OnCameraModePropertyChanged));

        private static void OnCameraModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as KinectControl;
            if (self == null) return;
            self.OnCameraModeChanged((CameraMode)e.NewValue);
        }

        #endregion

        #region UsageCases

        public bool UsageCases
        {
            get { return (bool)GetValue(UsageCasesProperty); }
            set { SetValue(UsageCasesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for UsageCases.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty UsageCasesProperty =
            DependencyProperty.Register("UsageCases", typeof(bool), typeof(KinectControl), new PropertyMetadata(false, OnUsageCasesPropertyChanged));

        private static void OnUsageCasesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as KinectControl;
            if (self == null) return;
            self.UsageCaseMode((bool)e.NewValue);
        }

        #endregion

        #region ShowBodySource

        public bool ShowBodySource
        {
            get { return (bool)GetValue(ShowBodySourceProperty); }
            set { SetValue(ShowBodySourceProperty, value); }
        }

        public static readonly DependencyProperty ShowBodySourceProperty =
            DependencyProperty.Register("ShowBodySource", typeof(bool), typeof(KinectControl), new PropertyMetadata(false, OnShowBodySourcePropertyChanged));

        private static void OnShowBodySourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as KinectControl;
            if (self == null) return;
            self.OnShowBodySourceChanged((bool)e.NewValue);
        }

        #endregion

        #region ShowVisualGestureMode

        public bool ShowVisualGestureMode
        {
            get { return (bool)GetValue(ShowVisualGestureModeProperty); }
            set { SetValue(ShowVisualGestureModeProperty, value); }
        }

        public static readonly DependencyProperty ShowVisualGestureModeProperty =
            DependencyProperty.Register("ShowVisualGestureMode", typeof(bool), typeof(KinectControl), new PropertyMetadata(false, OnShowVisualGestureModePropertyChanged));

        private static void OnShowVisualGestureModePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as KinectControl;
            if (self == null) return;
            self.OnShowVisualGestureModeChanged((bool)e.NewValue);
        }

        #endregion

        private CancellationTokenSource _cts = new CancellationTokenSource();
        private CancellationTokenSource _cameraModeCts = new CancellationTokenSource();
        private BodySourceViewModel _bodySourceViewModel = null;

        public KinectControl()
        {
            InitializeComponent();
            UsageCaseMode(true);
            Unloaded += KinectControl_Unloaded;
            OnCameraModeChanged(CameraMode);
        }

        private void KinectControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnCameraModeChanged(CameraMode mode)
        {
            _cameraModeCts.Cancel();
            //_cameraModeCts.Dispose();
            _cameraModeCts = new CancellationTokenSource();
            OnShowBodySourceChanged(ShowBodySource);
            ApplyCameraModeAsync(mode).ConfigureAwait(false);
        }

        private void UsageCaseMode(bool value)
        {
            if (value == false)
            {
                //KinectRegion.SetKinectRegion(null, null);
                Dispatcher.InvokeAsync(() =>
                {
                    UsagesContainer.Visibility = Visibility.Collapsed;
                });
                return;
            }
            KinectRegion.SetKinectRegion(this, KinectRegion);
            KinectRegion.KinectSensor = KinectSensor.GetDefault();
            Dispatcher.InvokeAsync(() =>
            {
                UsagesContainer.Visibility = Visibility.Visible;
                CameraImageContainer.Visibility = Visibility.Collapsed;
            });
        }

        private void OnShowBodySourceChanged(bool newValue)
        {
            if (!newValue)
            {
                BodySourceContainer.Visibility = Visibility.Collapsed;
                BodyControl.ViewModel = null;
                if (_bodySourceViewModel != null)
                {
                    _bodySourceViewModel.Dispose();
                    _bodySourceViewModel = null;
                }
                return;
            }

            var viewSpaceType = CameraMode == CameraMode.Color || CameraMode == CameraMode.GreenScreening
                ? ViewSpaceType.Color
                : ViewSpaceType.Depth;
            if (_bodySourceViewModel == null)
                _bodySourceViewModel = new BodySourceViewModel();
            _bodySourceViewModel.ViewSpaceType = viewSpaceType;
            BodyControl.ViewModel = _bodySourceViewModel;
            BodySourceContainer.Visibility = Visibility.Visible;
        }

        private async Task ApplyCameraModeAsync(CameraMode mode)
        {
            if (mode == CameraMode.None)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    CameraImageContainer.Visibility = Visibility.Collapsed;
                });
                return;
            }
            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, _cameraModeCts.Token))
            {
                var ct = cts.Token;
                var tcs = new TaskCompletionSource<bool>();
                ImageSource result;
                using (ct.Register(() => tcs.TrySetResult(true)))
                using (var videoSources = new VideoSources())
                {
                    bool lightButtonVisible = false;
                    IDisposable lifeCycle;
                    switch (mode)
                    {
                        case CameraMode.Color:
                            lifeCycle = videoSources.GetNewColorImageSource(out result);
                            break;
                        case CameraMode.Infrared:
                            lightButtonVisible = true;
                            lifeCycle = videoSources.GetNewInfraredImageSource(out result);
                            break;
                        case CameraMode.Depth:
                            lightButtonVisible = true;
                            lifeCycle = videoSources.GetNewDepthImageSource(out result);
                            break;
                        case CameraMode.BodyIndex:
                            lifeCycle = videoSources.GetNewBodyIndexImageSource(out result);
                            break;
                        case CameraMode.GreenScreening:
                            lifeCycle = videoSources.GetNewGreenScreeningImageSource(out result);
                            break;
                        case CameraMode.None:
                        default:
                            throw new IndexOutOfRangeException();
                    }
                    using (lifeCycle)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            //LightButton.Visibility = lightButtonVisible
                            //    ? Visibility.Visible
                            //    : Visibility.Collapsed;
                            UsagesContainer.Visibility = Visibility.Collapsed;
                            CameraImageContainer.Visibility = Visibility.Visible;
                            CameraImage.Source = result;
                        });

                        //RoutedPropertyChangedEventHandler<double> handler = (s, e) =>
                        //{
                        //    videoSources.Lightness = (int)e.NewValue;
                        //};

                        //if (lightButtonVisible)
                        //    LightSlider.ValueChanged += handler;

                        await tcs.Task;

                        //if (lightButtonVisible)
                        //    LightSlider.ValueChanged -= handler;
                    }
                }
            }
        }

        private Popup _lastPopup;

        private void OnShowVisualGestureModeChanged(bool value)
        {
            if (!value)
            {
                if (_lastPopup != null)
                    _lastPopup.IsOpen = false;
                _lastPopup = null;

                return;
            }

            if (_lastPopup == null)
            {
                _lastPopup = GetExtra();
                _lastPopup.HorizontalOffset = SystemParameters.WorkArea.Right - (_lastPopup.Child as FrameworkElement).Width;
                _lastPopup.VerticalOffset = SystemParameters.WorkArea.Bottom - (_lastPopup.Child as FrameworkElement).Height;
                EventHandler onClosed = null;
                onClosed = (sender, args) =>
                {
                    var p = sender as Popup;
                    p.Closed -= onClosed;
                    (p.DataContext as GesturesDetection)?.Dispose();
                    p.DataContext = null;
                };
                _lastPopup.Closed += onClosed;
                _lastPopup.DataContext = new GesturesDetection();
            }
        }

        private Popup GetExtra()
        {
            var template = FindResource("ExtraContent") as DataTemplate;
            if (template == null) return null;
            return template.LoadContent() as Popup;
        }

        private void TakePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var imgControl = CameraImage;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)imgControl.ActualWidth, (int)imgControl.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(imgControl);
                dc.DrawRectangle(brush, null, new Rect(new Point(), new Size(imgControl.ActualWidth, imgControl.ActualHeight)));
            }
            renderBitmap.Render(dv);
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
            string time = DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);
            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string path = System.IO.Path.Combine(myPhotos, "KinectTechDaysSession-" + time + ".png");
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }
            }
            catch (IOException)
            {
            }



            //var imgSource = imgControl.Source;
            //var writeableBitmap = imgSource as WriteableBitmap;
            //if (writeableBitmap == null) return;
            //try
            //{
            //    writeableBitmap.Lock();
            //    unsafe
            //    {
            //        byte* backBuffer = (byte*)writeableBitmap.BackBuffer;

            //    }
            //}
            //finally
            //{
            //    writeableBitmap.Unlock();
            //}
        }

        private void UsageCaseButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;
            var content = btn.Content as Image;
            if (content == null) return;
            var zoom = new ZoomImagePopupControl(content.Source);
            KinectRegionGrid.Children.Add(zoom);
            zoom.Focus();
            KinectRegion.InputPointerManager.CompleteGestures();
            e.Handled = true;
        }

        //private void LightButton_Click(object sender, RoutedEventArgs e)
        //{
        //    LightSliderContainer.Visibility = LightSliderContainer.Visibility == Visibility.Visible
        //        ? Visibility.Collapsed
        //        : Visibility.Visible;
        //}
    }
}
