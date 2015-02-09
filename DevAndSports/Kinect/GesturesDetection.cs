using DevAndSports.Helpers;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Microsoft.Kinect.VisualGestureBuilder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DevAndSports.Kinect
{
    public class GesturesDetection : ViewModelBase, IDisposable
    {
        private TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();

        private float _progress;
        private uint _hitCount;
        private bool _isHappy;

        public float Progress
        {
            get { return _progress; }
            set { SetByRef(ref _progress, value); }
        }

        public uint HitCount
        {
            get { return _hitCount; }
            set { SetByRef(ref _hitCount, value); }
        }

        public bool IsHappy
        {
            get { return _isHappy; }
            set { SetByRef(ref _isHappy, value); }
        }

        public void Dispose()
        {
            _tcs.TrySetResult(true);
        }

        private IList<ProcessWindow> _windows;
        public IEnumerable<ProcessWindow> Windows => _windows;
        public ProcessWindow SelectedWindow { get; set; }

        public GesturesDetection()
        {
            _windows = ProcessWindow.GetWindows(p => p.EndsWith("- Microsoft Visual Studio")).ToList();
            SelectedWindow = _windows.FirstOrDefault();
            InitAsync().ConfigureAwait(false);
        }

        private async Task InitAsync()
        {
            var sensor = KinectSensor.GetDefault();
            if (!sensor.IsOpen) sensor.Open();
            var bodies = new Body[sensor.BodyFrameSource.BodyCount];
            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;
            using (var vgbSource = new VisualGestureBuilderFrameSource(sensor, 0))
            using (var vgbReader = vgbSource.OpenReader())
            using (var faceSource = new FaceFrameSource(sensor, 0, faceFrameFeatures))
            using (var faceReader = faceSource.OpenReader())
            using (var bodyReader = sensor.BodyFrameSource.OpenReader())
            {
                Action trySetTrackingId = () =>
                {
                    using (var frame = bodyReader.AcquireLatestFrame())
                    {
                        if (frame == null) return;
                        frame.GetAndRefreshBodyData(bodies);
                        var body = bodies.FirstOrDefault(b => b != null && b.IsTracked);

                        if (body == null) return;
                        vgbSource.TrackingId = body.TrackingId;
                        faceSource.TrackingId = body.TrackingId;
                    }
                };
                using (var vgbGestureDatabase = new VisualGestureBuilderDatabase("Gestures/flexionProgress.gba"))
                {
                    vgbSource.AddGestures(vgbGestureDatabase.AvailableGestures.ToList());
                }
                EventHandler<VisualGestureBuilderFrameArrivedEventArgs> vgbhandler = (s, e) =>
                {
                    using (var frame = e.FrameReference.AcquireFrame())
                    {
                        if (frame != null)
                        {
                            if (frame.IsTrackingIdValid)
                            {
                                var results = frame.ContinuousGestureResults;
                                if (results != null)
                                {
                                    var tmpIsHappy = faceReader.AcquireLatestFrame()?.FaceFrameResult?.FaceProperties[FaceProperty.Happy] == DetectionResult.Yes;

                                    foreach (var gesture in vgbSource.Gestures)
                                    {
                                        ContinuousGestureResult result = null;
                                        results.TryGetValue(gesture, out result);
                                        if (result == null) continue;
                                        var progress = result.Progress;
                                        ComputeProgress(progress, tmpIsHappy);
                                        //Progress = progress;
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                trySetTrackingId();
                            }
                        }
                    }
                    Progress = float.NaN;
                };
                vgbReader.FrameArrived += vgbhandler;
                await _tcs.Task;
                vgbReader.FrameArrived -= vgbhandler;
            }
        }

        private static readonly TimeSpan doneDuration = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan isHappyDuration = TimeSpan.FromMilliseconds(500);
        private DateTime _lastDone = DateTime.Now;
        private DateTime _lastHappy = DateTime.Now;


        private bool isStarted = false;
        private bool isDone = true;

        private void ComputeProgress(float progress, bool isHappy)
        {
            Progress = progress;
            if (isHappy)
                _lastHappy = DateTime.Now;
            IsHappy = isHappy || DateTime.Now - _lastHappy < isHappyDuration;
            const float tolerance = 0.2F;
            if (progress >= 0 - tolerance && progress <= 0 + tolerance)
            {
                // init
                if (DateTime.Now - _lastDone < doneDuration)
                {
                    _lastDone = DateTime.MinValue;
                    // okay !
                    if (IsHappy)
                        Hit();
                }
            }
            if (progress >= 1 - tolerance && progress <= 1 + tolerance)
            {
                _lastDone = DateTime.Now;
                // done
            }
        }

        private void Hit()
        {
            HitCount++;
            var selectedWindow = SelectedWindow;
            if (selectedWindow != null)
            {
                selectedWindow.SendKeyDown(Key.F5);
            }
        }

        public class ProcessWindow
        {
            private IntPtr _handle;
            public string Name { get; private set; }

            private ProcessWindow(IntPtr handle, string name)
            {
                _handle = handle;
                Name = name;
            }

            public bool SendKeyDown(Key key)
            {
                var keyCode = KeyInterop.VirtualKeyFromKey(key);
                return WindowsInterop.PostMessage(new HandleRef(null, _handle), 256, new IntPtr(keyCode), IntPtr.Zero);
            }

            public static IEnumerable<ProcessWindow> GetWindows(Predicate<string> filter)
            {
                return Process.GetProcesses().Where(p => filter(p.MainWindowTitle)).Select(p => new ProcessWindow(p.MainWindowHandle, p.MainWindowTitle));
            }

            private static class WindowsInterop
            {
                [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
                public static extern IntPtr FindWindow(IntPtr zeroOnly, string lpWindowName);

                [return: MarshalAs(UnmanagedType.Bool)]
                [DllImport("user32.dll", SetLastError = true)]
                public static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
            }
        }
    }
}
