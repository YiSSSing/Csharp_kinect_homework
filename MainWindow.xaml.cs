﻿//------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.CoordinateMappingBasics
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;

    /// <summary>
    /// Interaction logic for MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies;
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>;'''''''''''''''''''''''''''''''''''''''''''''''''''''''
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader multiFrameSourceReader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// The size in bytes of the bitmap back buffer
        /// </summary>
        private uint bitmapBackBufferSize = 0;

        /// <summary>
        /// Intermediate storage for the color to depth mapping
        /// </summary>
        private DepthSpacePoint[] colorMappedToDepthPoints = null;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);

            this.multiFrameSourceReader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            this.coordinateMapper = this.kinectSensor.CoordinateMapper;

            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

            int depthWidth = depthFrameDescription.Width;
            int depthHeight = depthFrameDescription.Height;

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            this.colorMappedToDepthPoints = new DepthSpacePoint[colorWidth * colorHeight];

            this.bitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            
            // Calculate the WriteableBitmap back buffer size
            this.bitmapBackBufferSize = (uint)((this.bitmap.BackBufferStride * (this.bitmap.PixelHeight - 1)) + (this.bitmap.PixelWidth * this.bytesPerPixel));
                                   
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            this.kinectSensor.Open();

            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            this.DataContext = this;

            this.InitializeComponent();

            ComboBoxInitialize();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
            }
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {

            bool bodyDataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame() )
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    bodyDataReceived = true;
                }
            }

            if (bodyDataReceived)
            {

                foreach (Body body in this.bodies)
                {

                    if (body.IsTracked)
                    {
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                        // convert the joint points to depth (display) space
                        Dictionary<JointType, Point> jointPoints = new Dictionary<JointType, Point>();

                        foreach (JointType jointType in joints.Keys)
                        {
                            // sometimes the depth(Z) of an inferred joint may show as negative
                            // clamp down to 0.1f to prevent coordinatemapper from returning (-Infinity, -Infinity)
                            CameraSpacePoint position = joints[jointType].Position;
                            if (position.Z < 0)
                            {
                                position.Z = InferredZPositionClamp;
                            }

                            DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);
                            jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                        }
                        test_x.Text = jointPoints[JointType.ShoulderLeft].X.ToString();
                        test_y.Text = jointPoints[JointType.ShoulderRight].X.ToString();
                        double lenY = jointPoints[JointType.SpineBase].Y - jointPoints[JointType.SpineShoulder].Y;
                        double lenX = jointPoints[JointType.ShoulderRight].X - jointPoints[JointType.ShoulderLeft].X;
                        double neckY = jointPoints[JointType.Neck].Y;
                        this.DrawItemOnLHand(body.HandRightState, jointPoints[JointType.HandLeft]);
                        this.DrawItemOnRHand(body.HandRightState, jointPoints[JointType.HandRight]);
                        this.DrawColthOnBody(jointPoints[JointType.ShoulderLeft], lenX, lenY, neckY);
                    }
                }
            }

        }

        /// <summary>
        /// draw items to player's left hand, kinect's coordinate on background is 500*360 according to 25000*16000
        /// </summary>
        /// <param name="hs">hand state for kinect</param>
        /// <param name="handPosition">hand position</param>
        private void DrawItemOnRHand(HandState hs, Point handPosition)
        {
            double positionX = handPosition.X * 50, positionY = handPosition.Y * 38;

            //Note : png size of items are 2000*2000
            if (hs != HandState.NotTracked)
            {
                double right = 23000 - positionX, bottom = 14000 - positionY;
                if (positionX < 0)
                {
                    positionX = 0;
                    right = 23000;
                }
                if (positionY < 0)
                {
                    positionY = 0;
                    bottom = 14000;
                }
                if (23000 < positionX)
                {
                    positionX = 23000;
                    right = 0;
                }
                if (14000 < positionY)
                {
                    bottom = 0;
                    positionY = 14000;
                }
                Item_bag.Margin = new Thickness(positionX, positionY, right, bottom);
            }
        }

        /// <summary>
        /// draw items to player's left hand, kinect's coordinate on background is 500*360 according to 25000*16000
        /// </summary>
        /// <param name="hs">hand state for kinect</param>
        /// <param name="handPosition">hand position</param>
        private void DrawItemOnLHand(HandState hs, Point handPosition)
        {
            double positionX = handPosition.X * 50, positionY = handPosition.Y * 38;

            //Note : png size of items are 2000*2000
            if (hs != HandState.NotTracked)
            {
                double right = 23000 - positionX , bottom = 14000 - positionY;
                if (positionX < 0)
                {
                    positionX = 0;
                    right = 23000;
                }
                if (positionY < 0)
                {
                    positionY = 0;
                    bottom = 14000;
                }
                if (23000 < positionX)
                {
                    positionX = 23000;
                    right = 0;
                }
                if (14000 < positionY)
                {
                    bottom = 0;
                    positionY = 14000;
                }
                Item_photographer.Margin = new Thickness(positionX, positionY, right, bottom);
            }
        }

        /// <summary>
        /// draw cloth, margin to center of the player's body
        /// </summary>
        /// <param name="bodyPosition">player's body coordinate</param>
        private void DrawColthOnBody(Point shoudlerLeftPos, double lenX, double lenY, double neckY)
        {
            //double positionX = bodyPos.X *50, positionY = bodyPos.Y*44.444;
            double positionX = shoudlerLeftPos.X * 43, positionY = neckY * 26.5;
            //Note : png size of shyatsu is 3000*3000
            double right = 25000 - positionX - lenX*90, bottom = 16000 - positionY - lenY*65;
            //double right = shoudlerRightPos.X *50, bottom = shoudlerRightPos.Y*44.444;

            if (positionX < 0)
            {
                positionX = 0;
                right = 22000;
            }
            if (positionY < 0)
            {
                positionY = 0;
                bottom = 13000;
            }
            if (22000 < positionX)
            {
                positionX = 22000;
                right = 0;
            }
            if (13000 < positionY)
            {
                bottom = 0;
                positionY = 13000;
            }
            Cloth.Margin = new Thickness(positionX, positionY, right, bottom);
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.multiFrameSourceReader != null)
            {
                // MultiSourceFrameReder is IDisposable
                this.multiFrameSourceReader.Dispose();
                this.multiFrameSourceReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the user clicking on the screenshot button
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void ScreenshotButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a render target to which we'll render our composite image
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap((int)CompositeImage.ActualWidth, (int)CompositeImage.ActualHeight, 96.0, 96.0, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush brush = new VisualBrush(CompositeImage);
                dc.DrawRectangle(brush, null, new Rect(new Point(), new Size(CompositeImage.ActualWidth, CompositeImage.ActualHeight)));
            }

            renderBitmap.Render(dv);

            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

            string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            string path = Path.Combine(myPhotos, "KinectScreenshot-CoordinateMapping-" + time + ".png");

            // Write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                this.StatusText = string.Format(Properties.Resources.SavedScreenshotStatusTextFormat, path);
            }
            catch (IOException)
            {
                this.StatusText = string.Format(Properties.Resources.FailedScreenshotStatusTextFormat, path);
            }
        }

        /// <summary>
        /// Handles the depth/color/body index frame data arriving from the sensor
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            int depthWidth = 0;
            int depthHeight = 0;
                    
            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            bool isBitmapLocked = false;

            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();           

            // If the Frame has expired by the time we process this event, return.
            if (multiSourceFrame == null)
            {
                return;
            }

            // We use a try/finally to ensure that we clean up before we exit the function.  
            // This includes calling Dispose on any Frame objects that we may have and unlocking the bitmap back buffer.
            try
            {                
                depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame();
                colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame();
                bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame();
                //bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame();


                // If any frame has expired by the time we process this event, return.
                // The "finally" statement will Dispose any that are not null.
                if ((depthFrame == null) || (colorFrame == null) || (bodyIndexFrame == null) )
                {
                    return;
                }

                // Process Depth
                FrameDescription depthFrameDescription = depthFrame.FrameDescription;

                depthWidth = depthFrameDescription.Width;
                depthHeight = depthFrameDescription.Height;

                // Access the depth frame data directly via LockImageBuffer to avoid making a copy
                using (KinectBuffer depthFrameData = depthFrame.LockImageBuffer())
                {
                    this.coordinateMapper.MapColorFrameToDepthSpaceUsingIntPtr(
                        depthFrameData.UnderlyingBuffer,
                        depthFrameData.Size,
                        this.colorMappedToDepthPoints);
                }

                // We're done with the DepthFrame 
                depthFrame.Dispose();
                depthFrame = null;

                // Process Color

                // Lock the bitmap for writing
                this.bitmap.Lock();
                isBitmapLocked = true;

                colorFrame.CopyConvertedFrameDataToIntPtr(this.bitmap.BackBuffer, this.bitmapBackBufferSize, ColorImageFormat.Bgra);

                // We're done with the ColorFrame 
                colorFrame.Dispose();
                colorFrame = null;

                // We'll access the body index data directly to avoid a copy
                using (KinectBuffer bodyIndexData = bodyIndexFrame.LockImageBuffer())
                {
                    unsafe
                    {
                        byte* bodyIndexDataPointer = (byte*)bodyIndexData.UnderlyingBuffer;

                        int colorMappedToDepthPointCount = this.colorMappedToDepthPoints.Length;

                        fixed (DepthSpacePoint* colorMappedToDepthPointsPointer = this.colorMappedToDepthPoints)
                        {
                            // Treat the color data as 4-byte pixels
                            uint* bitmapPixelsPointer = (uint*)this.bitmap.BackBuffer;

                            // Loop over each row and column of the color image
                            // Zero out any pixels that don't correspond to a body index
                            for (int colorIndex = 0; colorIndex < colorMappedToDepthPointCount; ++colorIndex)
                            {
                                float colorMappedToDepthX = colorMappedToDepthPointsPointer[colorIndex].X;
                                float colorMappedToDepthY = colorMappedToDepthPointsPointer[colorIndex].Y;

                                // The sentinel value is -inf, -inf, meaning that no depth pixel corresponds to this color pixel.
                                if (!float.IsNegativeInfinity(colorMappedToDepthX) &&
                                    !float.IsNegativeInfinity(colorMappedToDepthY))
                                {
                                    // Make sure the depth pixel maps to a valid point in color space
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
                        this.bitmap.AddDirtyRect(new Int32Rect(0, 0, this.bitmap.PixelWidth, this.bitmap.PixelHeight));
                    }
                }
            }
            finally
            {
                if (isBitmapLocked)
                {
                    this.bitmap.Unlock();
                }

                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                }
            }
        }

        /// <summary>
        /// Handles the event which the sensor becomes unavailable (E.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        private void ComboCountry_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string src = ComboCountry.SelectedItem.ToString();
            CountrySelectChanged();
            switch (src)
            {
                case "台灣":
                    {
                        src = "Images/taiwan.jpg";
                        ComboForeground.Items.Add("路邊攤");
                        break;
                    }
                case "日本":
                    {
                        src = "Images/japan.jpg";
                        ComboForeground.Items.Add("鳥居");
                        break;
                    }
                case "美國":
                    {
                        src = "Images/america.jpg";
                        ComboForeground.Items.Add("直升機");
                        break;
                    }
                default: src = "Images/Background.png"; break;
            }
            BackgroundImage.Source = new BitmapImage(new Uri(src, UriKind.Relative));
        }

        private void ComboCloth_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (ComboCloth.SelectedItem.ToString())
            {
                case "顯示": Cloth.Visibility = Visibility.Visible; break;
                case "不顯示": Cloth.Visibility = Visibility.Hidden; break;
            }
            
        }

        private void ComboForeground_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (ComboForeground.SelectedItem.ToString())
            {
                case "路邊攤": Foreground_eggcake.Visibility = Visibility.Visible; break;
                case "鳥居": Foreground_torii.Visibility = Visibility.Visible; break;
                case "直升機": Foreground_heli.Visibility = Visibility.Visible; break;
                default:
                    {
                        Foreground_eggcake.Visibility = Visibility.Hidden;
                        Foreground_torii.Visibility = Visibility.Hidden;
                        Foreground_heli.Visibility = Visibility.Hidden;
                        break;
                    }
            }
        }

        private void ComboItem_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            switch (ComboItem.SelectedItem.ToString())
            {
                case "相機": Item_photographer.Visibility = Visibility.Visible; break;
                case "手提包": Item_bag.Visibility = Visibility.Visible; break;
                default:
                    {
                        Item_photographer.Visibility = Visibility.Hidden;
                        Item_bag.Visibility = Visibility.Hidden;
                        break;
                    }
            }
        }

        private void ComboBoxInitialize()
        {
            ComboCountry.Items.Add("台灣");
            ComboCountry.Items.Add("日本");
            ComboCountry.Items.Add("美國");

            ComboItem.Items.Add("不顯示");
            ComboForeground.Items.Add("不顯示");
            ComboCloth.Items.Add("不顯示");

            ComboItem.Items.Add("相機");
            ComboItem.Items.Add("手提包");

            ComboCloth.Items.Add("顯示");

            ComboCountry.SelectedIndex = 0;
            ComboItem.SelectedIndex = 0;
            ComboCloth.SelectedIndex = 0;
        }

        private void CountrySelectChanged()
        {
            ComboForeground.SelectedIndex = 0;

            ComboForeground.Items.Remove("路邊攤");
            ComboForeground.Items.Remove("鳥居");
            ComboForeground.Items.Remove("直升機");
        }



    }
}
