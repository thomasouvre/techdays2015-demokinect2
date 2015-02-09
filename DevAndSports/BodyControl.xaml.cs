using DevAndSports.Kinect;
using Microsoft.Kinect;
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
    public class JointViewModel : DependencyObject
    {
        #region IsTracked

        public bool IsTracked
        {
            get { return (bool)GetValue(IsTrackedProperty); }
            set { SetValue(IsTrackedProperty, value); }
        }

        public static readonly DependencyProperty IsTrackedProperty =
            DependencyProperty.Register("IsTracked", typeof(bool), typeof(JointViewModel), new PropertyMetadata(false));

        #endregion
        #region X

        public double X
        {
            get { return (double)GetValue(XProperty); }
            set { SetValue(XProperty, value); }
        }

        public static readonly DependencyProperty XProperty =
            DependencyProperty.Register("X", typeof(double), typeof(JointViewModel), new PropertyMetadata(default(double)));

        #endregion
        #region Y

        public double Y
        {
            get { return (double)GetValue(YProperty); }
            set { SetValue(YProperty, value); }
        }

        public static readonly DependencyProperty YProperty =
            DependencyProperty.Register("Y", typeof(double), typeof(JointViewModel), new PropertyMetadata(default(double)));

        #endregion
    }

    public class BoneViewModel : DependencyObject
    {
        #region JointParent

        public JointViewModel JointParent
        {
            get { return (JointViewModel)GetValue(JointParentProperty); }
            set { SetValue(JointParentProperty, value); }
        }

        public static readonly DependencyProperty JointParentProperty =
            DependencyProperty.Register("JointParent", typeof(JointViewModel), typeof(BoneViewModel), new PropertyMetadata(null));

        #endregion
        #region JointChild

        public JointViewModel JointChild
        {
            get { return (JointViewModel)GetValue(JointChildProperty); }
            set { SetValue(JointChildProperty, value); }
        }

        public static readonly DependencyProperty JointChildProperty =
            DependencyProperty.Register("JointChild", typeof(JointViewModel), typeof(BoneViewModel), new PropertyMetadata(null));

        #endregion
    }

    public class BodyViewModel : DependencyObject
    {
        #region IsTracked

        public bool IsTracked
        {
            get { return (bool)GetValue(IsTrackedProperty); }
            set { SetValue(IsTrackedProperty, value); }
        }

        public static readonly DependencyProperty IsTrackedProperty =
            DependencyProperty.Register("IsTracked", typeof(bool), typeof(BodyViewModel), new PropertyMetadata(false));

        #endregion

        private Dictionary<JointType, JointViewModel> _joints;
        private List<BoneViewModel> _bones;

        public IReadOnlyDictionary<JointType, JointViewModel> Joints => _joints;
        public IEnumerable<BoneViewModel> Bones => _bones;

        public BodyViewModel()
        {
            _joints = Enum.GetValues(typeof(JointType)).OfType<JointType>().ToDictionary(e => e, e => new JointViewModel());
            _bones = new List<BoneViewModel>();
            PopulateBones(BonesConnection.SkeletalHierarchy);
        }

        private void PopulateBones(BonesConnection connection)
        {
            var joint = _joints[connection.Parent];
            foreach(var child in connection.Children)
            {
                _bones.Add(new BoneViewModel { JointParent = joint, JointChild = _joints[child.Parent] });
                PopulateBones(child);
            }
        }
    }

    public enum ViewSpaceType
    {
        Depth,
        Color
    }

    public class BodySourceViewModel : DependencyObject, IDisposable
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        //private List<BodyViewModel> _bodyViewModels;// = new List<BodyViewModel>(KinectSensor.GetDefault().BodyFrameSource.BodyCount);
        //
        //public IEnumerable<BodyViewModel> BodyViewModels => _bodyViewModels;

        #region BodyViewModels

        public IEnumerable<BodyViewModel> BodyViewModels
        {
            get { return (IEnumerable<BodyViewModel>)GetValue(BodyViewModelsProperty); }
            set { SetValue(BodyViewModelsProperty, value); }
        }

        public static readonly DependencyProperty BodyViewModelsProperty =
            DependencyProperty.Register("BodyViewModels", typeof(IEnumerable<BodyViewModel>), typeof(BodySourceViewModel), new PropertyMetadata(null));

        #endregion

        #region ViewSpaceType

        public ViewSpaceType ViewSpaceType
        {
            get { return (ViewSpaceType)GetValue(ViewSpaceTypeProperty); }
            set { SetValue(ViewSpaceTypeProperty, value); }
        }

        public static readonly DependencyProperty ViewSpaceTypeProperty =
            DependencyProperty.Register("ViewSpaceType", typeof(ViewSpaceType), typeof(BodySourceViewModel), new PropertyMetadata(default(ViewSpaceType), OnViewSpaceTypePropertyChanged));

        private static void OnViewSpaceTypePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var self = d as BodySourceViewModel;
            if (self == null) return;
            self.OnViewSpaceTypeChanged((ViewSpaceType)e.NewValue);
        }

        #endregion

        #region ViewWidth

        public double ViewWidth
        {
            get { return (double)GetValue(ViewWidthProperty); }
            private set { SetValue(ViewWidthProperty, value); }
        }

        public static readonly DependencyProperty ViewWidthProperty =
            DependencyProperty.Register("ViewWidth", typeof(double), typeof(BodySourceViewModel), new PropertyMetadata(default(double)));

        #endregion

        #region ViewHeight

        public double ViewHeight
        {
            get { return (double)GetValue(ViewHeightProperty); }
            private set { SetValue(ViewHeightProperty, value); }
        }

        public static readonly DependencyProperty ViewHeightProperty =
            DependencyProperty.Register("ViewHeight", typeof(double), typeof(BodySourceViewModel), new PropertyMetadata(default(double)));

        #endregion

        public BodySourceViewModel()
        {
            OnViewSpaceTypeChanged(ViewSpaceType);
            InitAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            _tcs.TrySetResult(true);
        }

        private void OnViewSpaceTypeChanged(ViewSpaceType value)
        {
            var sensor = KinectSensor.GetDefault();
            FrameDescription desc;
            switch (value)
            {
                case ViewSpaceType.Depth:
                    desc = sensor.DepthFrameSource.FrameDescription;
                    break;
                case ViewSpaceType.Color:
                    desc = sensor.ColorFrameSource.FrameDescription;
                    break;
                default:
                    throw new IndexOutOfRangeException();
            }
            ViewWidth = desc.Width;
            ViewHeight = desc.Height;
        }

        private async Task InitAsync()
        {
            var sensor = KinectSensor.GetDefault();
            var mapper = sensor.CoordinateMapper;
            var bodyCount = sensor.BodyFrameSource.BodyCount;
            var bodies = new Body[bodyCount];
            var bodyVms = new List<BodyViewModel>(Enumerable.Range(0, KinectSensor.GetDefault().BodyFrameSource.BodyCount).Select(_ => new BodyViewModel()));
            BodyViewModels = bodyVms;
            using (var reader = sensor.BodyFrameSource.OpenReader())
            {
                EventHandler<BodyFrameArrivedEventArgs> handler = (sender, args) =>
                {
                    using (var frame = args.FrameReference.AcquireFrame())
                    {
                        if (frame == null) return;
                        frame.GetAndRefreshBodyData(bodies);
                        Body body;
                        BodyViewModel vm;
                        var isDepthSpace = ViewSpaceType == ViewSpaceType.Depth;
                        for (var i = 0; i < bodyCount; ++i)
                        {
                            body = bodies[i];
                            vm = bodyVms[i];
                            Dispatcher.Invoke(() =>
                            {
                                if (vm.IsTracked = body.IsTracked)
                                {
                                    foreach (var joint in body.Joints.Values)
                                    {
                                        var vmJoint = vm.Joints[joint.JointType];
                                        vmJoint.IsTracked = joint.TrackingState != TrackingState.NotTracked;
                                        if (isDepthSpace)
                                        {
                                            var position = mapper.MapCameraPointToDepthSpace(joint.Position);
                                            vmJoint.X = position.X;
                                            vmJoint.Y = position.Y;
                                        }
                                        else
                                        {
                                            var position = mapper.MapCameraPointToColorSpace(joint.Position);
                                            vmJoint.X = position.X;
                                            vmJoint.Y = position.Y;
                                        }
                                    }
                                }
                            });
                        }
                    }
                };
                reader.FrameArrived += handler;
                await _tcs.Task;
                reader.FrameArrived -= handler;
            }
        }
    }

    /// <summary>
    /// Interaction logic for BodyControl.xaml
    /// </summary>
    public partial class BodyControl : UserControl
    {
        #region ViewModel

        public BodySourceViewModel ViewModel
        {
            get { return (BodySourceViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(BodySourceViewModel), typeof(BodyControl), new PropertyMetadata(null));

        #endregion

        public BodyControl()
        {
            InitializeComponent();
        }
    }
}
