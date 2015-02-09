using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
#if NETFX_CORE
using WindowsPreview.Kinect;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;
#else
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Composition;
#endif

namespace DevAndSports.Kinect
{
    public class VideoSources : IDisposable
    {
        private KinectSensor _sensor;
        private List<IDisposable> _linkeds;

        public int Lightness { get; set; }

        public VideoSources()
        {
            _linkeds = new List<IDisposable>();
            _sensor = KinectSensor.GetDefault();
        }
        public void Dispose()
        {
            foreach (var linked in _linkeds)
                linked.Dispose();
            //_sensor.Close();
        }

        public IDisposable GetNewInfraredImageSource(out ImageSource result)
        {
            var linked = GetImageSource(_sensor, _sensor.InfraredFrameSource, out result);
            _linkeds.Add(linked);
            return linked;
        }

        public IDisposable GetNewDepthImageSource(out ImageSource result)
        {
            var linked = GetImageSource(_sensor, _sensor.DepthFrameSource, out result);
            _linkeds.Add(linked);
            return linked;
        }

        public IDisposable GetNewColorImageSource(out ImageSource result)
        {
            var linked = GetImageSource(_sensor, _sensor.ColorFrameSource, out result);
            _linkeds.Add(linked);
            return linked;
        }

        public IDisposable GetNewBodyIndexImageSource(out ImageSource result)
        {
            var linked = GetImageSource(_sensor, _sensor.BodyIndexFrameSource, out result);
            _linkeds.Add(linked);
            return linked;
        }

        public IDisposable GetNewGreenScreeningImageSource(out ImageSource result)
        {
            var linked = GetGreenScreeningImageSource(_sensor, out result);
            _linkeds.Add(linked);
            return linked;
        }

        private /*static*/ IDisposable GetImageSource(KinectSensor sensor, InfraredFrameSource infraredFrameSource, out ImageSource result)
        {
            var frameDescription = infraredFrameSource.FrameDescription;
            var pixelWidth = frameDescription.Width;
            var pixelHeight = frameDescription.Height;

            var writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight
#if !NETFX_CORE
                //, 96.0, 96.0, PixelFormats.Gray16, null
                , 96.0, 96.0, PixelFormats.Gray32Float, null
#endif
                );
            result = writeableBitmap;
            return new TaskLifeCycle(async task =>
            {
                if (!sensor.IsOpen) sensor.Open();
                using (var reader = infraredFrameSource.OpenReader())
                {
#if NETFX_CORE
                    //var backBuffer = new byte[pixelWidth * pixelHeight * 4];
                    var buffer = new byte[2];
                    var frameBufferLengthInPixel = pixelWidth * pixelHeight;
                    TypedEventHandler<InfraredFrameReader, InfraredFrameArrivedEventArgs> handler = (sender, args) =>
#else
                    EventHandler<InfraredFrameArrivedEventArgs> handler = (sender, args) =>
#endif
                    {
                        using (var frame = args.FrameReference.AcquireFrame())
                        {
                            if (frame == null) return;
#if NETFX_CORE
                            var tmp = frame.LockImageBuffer();
                            using (var frameBufferStream = tmp.AsStream())
                            using (var backBufferStream = writeableBitmap.PixelBuffer.AsStream())
                            {
                                backBufferStream.Position = 0;
                                var frameIndex = 0;
                                for (; frameIndex < frameBufferLengthInPixel; ++frameIndex)
                                {
                                    frameBufferStream.Read(buffer, 0, 2);
                                    var intensity = buffer[0];
                                    backBufferStream.WriteByte(intensity);
                                    backBufferStream.WriteByte(intensity);
                                    backBufferStream.WriteByte(intensity);
                                    backBufferStream.WriteByte(255);
                                }
                            }
                            tmp = null;
                            writeableBitmap.Invalidate();
                            callback(writeableBitmap);
#else
                            using (var buffer = frame.LockImageBuffer())
                            {
                                var lightness = Lightness;
                                var size = buffer.Size;
                                unsafe
                                {
                                    ushort* frameData = (ushort*)buffer.UnderlyingBuffer;
                                    writeableBitmap.Lock();
                                    //ushort* backBuffer = (ushort*)writeableBitmap.BackBuffer;
                                    float* backBuffer = (float*)writeableBitmap.BackBuffer;
                                    for (int i = 0; i < (int)(size / frameDescription.BytesPerPixel); ++i)
                                        backBuffer[i] = Math.Min(1.0f, (((float)frameData[i] / (float)ushort.MaxValue * 0.75f) * (1.0f - 0.01f)) + 0.01f);
                                    //backBuffer[i] = frameData[i] < (byte.MaxValue / 2)
                                    //    ? frameData[i]
                                    //    : (ushort)(frameData[i] + lightness);
                                    //Math.Min(InfraredOutputValueMaximum, (((float)frameData[i] / InfraredSourceValueMaximum * InfraredSourceScale) * (1.0f - InfraredOutputValueMinimum)) + InfraredOutputValueMinimum);
                                    //backBuffer[i] = frameData[i];
                                    //backBuffer[i] = frameData[i] + 50 > byte.MaxValue
                                    //    ? frameData[i]
                                    //    : (ushort)(frameData[i] + 50);
                                }
                                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
                                writeableBitmap.Unlock();
                            }
#endif

                        }
                    };
                    reader.FrameArrived += handler;
                    await task;
                    reader.FrameArrived -= handler;
                }
            });
        }

        private static IDisposable GetImageSource(KinectSensor sensor, DepthFrameSource depthFrameSource, out ImageSource result)
        {
            var frameDescription = depthFrameSource.FrameDescription;
            var pixelWidth = frameDescription.Width;
            var pixelHeight = frameDescription.Height;

            var writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight
#if !NETFX_CORE
                //, 96.0, 96.0, PixelFormats.Gray16, null
                , 96.0, 96.0, PixelFormats.Gray32Float, null
#endif
                );
            result = writeableBitmap;
            return new TaskLifeCycle(async task =>
            {
                if (!sensor.IsOpen) sensor.Open();
                using (var reader = depthFrameSource.OpenReader())
                {
#if NETFX_CORE
                    TypedEventHandler<DepthFrameReader, DepthFrameArrivedEventArgs> handler = (sender, args) =>
#else
                    EventHandler<DepthFrameArrivedEventArgs> handler = (sender, args) =>
#endif
                    {
                        using (var frame = args.FrameReference.AcquireFrame())
                        {
                            if (frame == null) return;
#if NETFX_CORE
                            var tmpBuffer = frame.LockImageBuffer();
                            var frameBufferStream = tmpBuffer.AsStream();
                            var backBufferStream = writeableBitmap.PixelBuffer.AsStream();
                            backBufferStream.Position = 0;
                            var frameIndex = 0;
                            for (; frameIndex < frameBufferStream.Length; frameIndex += 2)
                            {
                                var buffer = new byte[2];
                                frameBufferStream.Read(buffer, 0, 2);
                                var intensity = BitConverter.ToInt16(buffer, 0);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte(255);
                            }
                            writeableBitmap.Invalidate();
                            callback(writeableBitmap);
#else
                            using (var buffer = frame.LockImageBuffer())
                            {
                                var size = buffer.Size;
                                unsafe
                                {
                                    ushort* frameData = (ushort*)buffer.UnderlyingBuffer;
                                    writeableBitmap.Lock();
                                    float* backBuffer = (float*)writeableBitmap.BackBuffer;
                                    for (int i = 0; i < (int)(size / frameDescription.BytesPerPixel); ++i)
                                        backBuffer[i] = Math.Min(1.0f, (((float)frameData[i] / (float)ushort.MaxValue * 0.75f) * (1.0f - 0.01f)) + 0.01f);
                                    //ushort* backBuffer = (ushort*)writeableBitmap.BackBuffer;
                                    //for (int i = 0; i < (int)(size / frameDescription.BytesPerPixel); ++i)
                                    //    backBuffer[i] = frameData[i];
                                }
                                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
                                writeableBitmap.Unlock();
                            }
#endif
                        }
                    };
                    reader.FrameArrived += handler;
                    await task;
                    reader.FrameArrived -= handler;
                }
            });
        }

        private static IDisposable GetImageSource(KinectSensor sensor, ColorFrameSource colorFrameSource, out ImageSource result)
        {
            var format = ColorImageFormat.Bgra;
            var frameDescription = colorFrameSource.CreateFrameDescription(format);
            var pixelWidth = frameDescription.Width;
            var pixelHeight = frameDescription.Height;

            var writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight
#if !NETFX_CORE
                , 96.0, 96.0, PixelFormats.Bgra32, null
#endif
                );
            result = writeableBitmap;
            return new TaskLifeCycle(async task =>
            {
                if (!sensor.IsOpen) sensor.Open();
                using (var reader = colorFrameSource.OpenReader())
                {
#if NETFX_CORE
                    TypedEventHandler<ColorFrameReader, ColorFrameArrivedEventArgs> handler = (sender, args) =>
#else
                    EventHandler<ColorFrameArrivedEventArgs> handler = (sender, args) =>
#endif
                    {
                        using (var frame = args.FrameReference.AcquireFrame())
                        {
                            if (frame == null) return;
#if NETFX_CORE
                            var tmpBuffer = frame.LockImageBuffer();
                            var frameBufferStream = tmpBuffer.AsStream();
                            var backBufferStream = writeableBitmap.PixelBuffer.AsStream();
                            backBufferStream.Position = 0;
                            var frameIndex = 0;
                            for (; frameIndex < frameBufferStream.Length; frameIndex += 2)
                            {
                                var buffer = new byte[2];
                                frameBufferStream.Read(buffer, 0, 2);
                                var intensity = BitConverter.ToInt16(buffer, 0);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte((byte)intensity);
                                backBufferStream.WriteByte(255);
                            }
                            writeableBitmap.Invalidate();
                            callback(writeableBitmap);
#else
                            using (var buffer = frame.LockRawImageBuffer())
                            {

                                writeableBitmap.Lock();
                                frame.CopyConvertedFrameDataToIntPtr(
                                    writeableBitmap.BackBuffer,
                                    (uint)(pixelWidth * pixelHeight * frameDescription.BytesPerPixel),
                                    format);

                                //var size = buffer.Size;
                                //unsafe
                                //{
                                //    ushort* frameData = (ushort*)buffer.UnderlyingBuffer;
                                //    writeableBitmap.Lock();
                                //    ushort* backBuffer = (ushort*)writeableBitmap.BackBuffer;
                                //    for (int i = 0; i < (int)(size / frameDescription.BytesPerPixel); ++i)
                                //        backBuffer[i] = frameData[i];
                                //}
                                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
                                writeableBitmap.Unlock();
                            }
#endif
                        }
                    };
                    reader.FrameArrived += handler;
                    await task;
                    reader.FrameArrived -= handler;
                }
            });
        }

        private static IDisposable GetImageSource(KinectSensor sensor, BodyIndexFrameSource bodyIndexFrameSource, out ImageSource result)
        {
            var bodyCount = sensor.BodyFrameSource.BodyCount;
            var frameDescription = bodyIndexFrameSource.FrameDescription;
            var pixelWidth = frameDescription.Width;
            var pixelHeight = frameDescription.Height;

            var writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight
#if !NETFX_CORE
                , 96.0, 96.0, PixelFormats.Bgra32, null
#endif
                );
            result = writeableBitmap;
            return new TaskLifeCycle(async task =>
            {
                if (!sensor.IsOpen) sensor.Open();
                using (var reader = bodyIndexFrameSource.OpenReader())
                {
                    EventHandler<BodyIndexFrameArrivedEventArgs> handler = (sender, args) =>
                    {
                        using (var frame = args.FrameReference.AcquireFrame())
                        {
                            if (frame == null) return;
                            using (var buffer = frame.LockImageBuffer())
                            {
                                var size = pixelWidth * pixelHeight;
                                const byte b1 = byte.MaxValue;
                                const byte b2 = byte.MinValue;
                                unsafe
                                {
                                    byte* frameData = (byte*)buffer.UnderlyingBuffer;
                                    byte* backBuffer = (byte*)writeableBitmap.BackBuffer;
                                    writeableBitmap.Lock();
                                    int j = 0;
                                    for (int i = 0; i < size; ++i)
                                    {
                                        j = i * 4;
                                        backBuffer[j + 0] = b1;
                                        backBuffer[j + 1] = frameData[i] > bodyCount ? b1 : b2;
                                        backBuffer[j + 2] = b1;
                                        backBuffer[j + 3] = b1;
                                    }
                                }
                                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, pixelWidth, pixelHeight));
                                writeableBitmap.Unlock();
                            }
                        }
                    };
                    reader.FrameArrived += handler;
                    await task;
                    reader.FrameArrived -= handler;
                }
            });
        }

        private static IDisposable GetGreenScreeningImageSource(KinectSensor sensor, out ImageSource result)
        {
            var bodyCount = sensor.BodyFrameSource.BodyCount;
            var frameDescription = sensor.ColorFrameSource.FrameDescription;
            var pixelWidth = frameDescription.Width;
            var pixelHeight = frameDescription.Height;

            var writeableBitmap = new WriteableBitmap(pixelWidth, pixelHeight
#if !NETFX_CORE
                , 96.0, 96.0, PixelFormats.Bgra32, null
#endif
                );
            result = writeableBitmap;

            var colorMappedToDepthPoints = new DepthSpacePoint[pixelWidth * pixelHeight];
            var bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
            var bitmapBackBufferSize = (uint)((writeableBitmap.BackBufferStride * (writeableBitmap.PixelHeight - 1)) +
                (writeableBitmap.PixelWidth * bytesPerPixel));

            return new TaskLifeCycle(async task =>
            {
                if (!sensor.IsOpen) sensor.Open();
                using (var reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.BodyIndex | FrameSourceTypes.Color | FrameSourceTypes.Depth))
                {
                    EventHandler<MultiSourceFrameArrivedEventArgs> handler = (sender, args) =>
                    {
                        var multiFrame = args.FrameReference.AcquireFrame();
                        if (multiFrame == null) return;
                        var bodyIndexFrameReference = multiFrame.BodyIndexFrameReference;
                        var colorFrameReference = multiFrame.ColorFrameReference;
                        var depthFrameReference = multiFrame.DepthFrameReference;
                        if (bodyIndexFrameReference == null || colorFrameReference == null || depthFrameReference == null) return;
                        var mapper = multiFrame.KinectSensor.CoordinateMapper;

                        BodyIndexFrame bodyIndexFrame = null;
                        ColorFrame colorFrame = null;
                        DepthFrame depthFrame = null;
                        var isLocked = false;
                        try
                        {
                            bodyIndexFrame = bodyIndexFrameReference.AcquireFrame();
                            colorFrame = colorFrameReference.AcquireFrame();
                            depthFrame = depthFrameReference.AcquireFrame();
                            if (depthFrame == null || colorFrame == null || bodyIndexFrame == null) return;

                            var depthWidth = depthFrame.FrameDescription.Width;
                            var depthHeight = depthFrame.FrameDescription.Height;
                            using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                            {
                                mapper.MapColorFrameToDepthSpaceUsingIntPtr(
                                    depthFrameData.UnderlyingBuffer,
                                    depthFrameData.Size,
                                    colorMappedToDepthPoints);
                            }
                            depthFrame.Dispose();
                            depthFrame = null;

                            writeableBitmap.Lock();
                            isLocked = true;

                            colorFrame.CopyConvertedFrameDataToIntPtr(writeableBitmap.BackBuffer, bitmapBackBufferSize, ColorImageFormat.Bgra);

                            // We're done with the ColorFrame 
                            colorFrame.Dispose();
                            colorFrame = null;


                            using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                            {
                                unsafe
                                {
                                    byte* bodyIndexDataPointer = (byte*)bodyIndexData.UnderlyingBuffer;
                                    int colorMappedToDepthPointCount = colorMappedToDepthPoints.Length;
                                    fixed (DepthSpacePoint* colorMappedToDepthPointsPointer = colorMappedToDepthPoints)
                                    {
                                        // Treat the color data as 4-byte pixels
                                        uint* bitmapPixelsPointer = (uint*)writeableBitmap.BackBuffer;

                                        for (int colorIndex = 0; colorIndex < colorMappedToDepthPointCount; ++colorIndex)
                                        {
                                            float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                                            float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;

                                            // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                                            if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                                                !float.IsNegativeInfinity(colorMappedToDepthY))
                                            {
                                                int depthX = (int)(colorMappedToDepthX + 0.5f);
                                                int depthY = (int)(colorMappedToDepthY + 0.5f);

                                                // If the point is not valid, there is no body index there.
                                                if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                                                {
                                                    int depthIndex = (depthY * depthWidth) + depthX;

                                                    // If we are tracking a body for the current pixel, do not zero out the pixel
                                                    if (bodyIndexDataPointer[depthIndex] != 0xff)
                                                    {
                                                        continue;
                                                    }
                                                }
                                            }
                                            bitmapPixelsPointer[colorIndex] = 0;
                                        }
                                    }
                                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
                                }
                            }
                        }
                        finally
                        {
                            if (isLocked)
                                writeableBitmap.Unlock();
                            if (bodyIndexFrame != null)
                                bodyIndexFrame.Dispose();
                            if (colorFrame != null)
                                colorFrame.Dispose();
                            if (depthFrame != null)
                                depthFrame.Dispose();
                        }
                    };
                    reader.MultiSourceFrameArrived += handler;
                    await task;
                    reader.MultiSourceFrameArrived -= handler;
                }
            });
        }

        private class TaskLifeCycle : IDisposable
        {
            private bool _isDisposed;
            private TaskCompletionSource<bool> _tce;

            public TaskLifeCycle(Action<Task> callback)
            {
                _isDisposed = false;
                _tce = new TaskCompletionSource<bool>();
                callback(_tce.Task);
            }

            public void Dispose()
            {
                if (_isDisposed) return;
                _isDisposed = true;
                _tce.SetResult(true);
            }
        }
    }

}
