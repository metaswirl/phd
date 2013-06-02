using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Phone;
using Microsoft.Phone.Controls;

// Directives
using Microsoft.Devices;
using System.IO;

namespace PhotoGEN
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Variables
        PhotoCamera cam;

        private bool isCamInitialized = false;

        // Holds the current resolution index.
        int currentResIndex = 0;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.RunWorkerAsync();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
               
                while (cam == null || !isCamInitialized)
                {
                    System.Threading.Thread.Sleep(100);
                }
                lock (cam)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        int[] pixel = null;
                        int PixelHeight=0;
                        int PixelWidth=0;

                        bool rotateFlip = false;

                        if(imgExample.Visibility == Visibility.Visible)
                        {

                            WriteableBitmap bitmap = imgExample.Source as WriteableBitmap;
                            //using (MemoryStream stream = new MemoryStream(bmp.))
                            //{
                            //    WriteableBitmap bitmap = PictureDecoder.DecodeJpeg(stream);
                            pixel = (int[])bitmap.Pixels.Clone();
                            PixelHeight = bitmap.PixelHeight;
                            PixelWidth = bitmap.PixelWidth;
                            //}
                            rotateFlip = false;
                        }
                        else
                        {
                            pixel = new int[(int)(cam.PreviewResolution.Width*cam.PreviewResolution.Height)];
                            cam.GetPreviewBufferArgb32(pixel);
                            PixelHeight = (int)cam.PreviewResolution.Height;
                            PixelWidth = (int)cam.PreviewResolution.Width;
                            rotateFlip = true;
                        }


                        

                            byte[] data = new byte[(PixelHeight * PixelWidth * 3)];
                            int h =PixelHeight;
                            int w =PixelWidth;
                            int l = data.Length-3;
                            for (int x = 0; x < w; x++ )
                            {
                                for(int y=0; y<h; y++)
                                {
                                    int p = ((y*w) + x);
                                    int dataPos = 0;
                                    if(rotateFlip)
                                    {
                                        dataPos = (y*(w*3)) + ((w - x - 1)*3);

                                        data[l - dataPos + 0] = (byte)((pixel[p] & 0x00FF0000) >> 16);
                                        data[l - dataPos + 1] = (byte)((pixel[p] & 0x0000FF00) >> 8);
                                        data[l - dataPos + 2] = (byte)((pixel[p] & 0x000000FF));
                                    }
                                    else
                                    {
                                        dataPos = (y * (w * 3)) + (x * 3);

                                        data[dataPos + 0] = (byte)((pixel[p] & 0x00FF0000) >> 16);
                                        data[dataPos + 1] = (byte)((pixel[p] & 0x0000FF00) >> 8);
                                        data[dataPos + 2] = (byte)((pixel[p] & 0x000000FF));
                                    }
                                    
                                }
                            }

//                           BitmapEncoder encoder = BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                            WebClient wc = new WebClient();
                            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                            wc.UploadStringCompleted += new UploadStringCompletedEventHandler(ImageServiceRequestCompleted);

                            wc.UploadStringAsync(new Uri("http://172.16.1.201:8080/actions/upload_picture"), "POST", "width=" + PixelWidth + "&picture=" + HttpUtility.UrlEncode(Convert.ToBase64String(data)));                        
                    });
                }
                System.Threading.Thread.Sleep(5000);
            }
        }

        void ImageServiceRequestCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if(!string.IsNullOrEmpty(e.Result) && e.Result.StartsWith("OK "))
            {
                string[] colorNames = e.Result.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if(colorNames.Length ==2 && !string.IsNullOrEmpty(colorNames[1]))
                {
                    string colorName = colorNames[1];
                    if(colorName == "green")
                    {
                        brdMain.BorderBrush = new SolidColorBrush(Color.FromArgb(255,0,255,0));
                    }
                    else
                    {
                        brdMain.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
                    }
                }
                else
                {
                    brdMain.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 128));
                }
            }
            else
            {
                brdMain.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 0, 128));
            }
        }

        //Code for initialization, capture completed, image availability events; also setting the source for the viewfinder.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            // Check to see if the camera is available on the device.
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true) ||
                 (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) == true))
            {
                // Initialize the camera, when available.
                if (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing))
                {
                    // Use front-facing camera if available.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.FrontFacing);
                }
                else
                {
                    // Otherwise, use standard camera on back of device.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
                }

                // Event is fired when the PhotoCamera object has been initialized.
                cam.Initialized += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(cam_Initialized);

                // Event is fired when the capture sequence is complete.
                cam.CaptureCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_CaptureCompleted);

                // Event is fired when the capture sequence is complete and an image is available.
                cam.CaptureImageAvailable += new EventHandler<Microsoft.Devices.ContentReadyEventArgs>(cam_CaptureImageAvailable);

                // Event is fired when the capture sequence is complete and a thumbnail image is available.
                cam.CaptureThumbnailAvailable += new EventHandler<ContentReadyEventArgs>(cam_CaptureThumbnailAvailable);

                // The event is fired when auto-focus is complete.
                cam.AutoFocusCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_AutoFocusCompleted);

                // The event is fired when the viewfinder is tapped (for focus).
                viewfinderCanvas.Tap += new EventHandler<GestureEventArgs>(focus_Tapped);

                // The event is fired when the shutter button receives a half press.
                CameraButtons.ShutterKeyHalfPressed += OnButtonHalfPress;

                // The event is fired when the shutter button receives a full press.
                CameraButtons.ShutterKeyPressed += OnButtonFullPress;

                // The event is fired when the shutter button is released.
                CameraButtons.ShutterKeyReleased += OnButtonRelease;

                //Set the VideoBrush source to the camera.
                viewfinderBrush.SetSource(cam);
            }
            else
            {
                // The camera is not supported on the device.
                this.Dispatcher.BeginInvoke(delegate()
                {
                    // Write message.
                    txtDebug.Text = "A Camera is not available on this device.";
                });               
            }
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (cam != null)
            {
                // Dispose camera to minimize power consumption and to expedite shutdown.
                cam.Dispose();

                // Release memory, ensure garbage collection.
                cam.Initialized -= cam_Initialized;
                cam.CaptureCompleted -= cam_CaptureCompleted;
                cam.CaptureImageAvailable -= cam_CaptureImageAvailable;
                cam.CaptureThumbnailAvailable -= cam_CaptureThumbnailAvailable;
                cam.AutoFocusCompleted -= cam_AutoFocusCompleted;
                CameraButtons.ShutterKeyHalfPressed -= OnButtonHalfPress;
                CameraButtons.ShutterKeyPressed -= OnButtonFullPress;
                CameraButtons.ShutterKeyReleased -= OnButtonRelease;
            }
        }

        // Update the UI if initialization succeeds.
        void cam_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {
                this.Dispatcher.BeginInvoke(delegate()
                {
                    // Write message.
                    txtDebug.Text = "Camera initialized.";

                  
                });
                isCamInitialized = true;
            }
        }

        // Ensure that the viewfinder is upright in LandscapeRight.
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (cam != null)
            {
                // LandscapeRight rotation when camera is on back of device.
                int landscapeRightRotation = 180;

                // Change LandscapeRight rotation for front-facing camera.
                if (cam.CameraType == CameraType.FrontFacing) landscapeRightRotation = -180;

                // Rotate video brush from camera.
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    // Rotate for LandscapeRight orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = landscapeRightRotation };
                }
                else
                {
                    // Rotate for standard landscape orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
                }
            }

            base.OnOrientationChanged(e);
        }

        private void ShutterButton_Click(object sender, RoutedEventArgs e)
        {
            if (cam != null)
            {
                try
                {
                    // Start image capture.
                    cam.CaptureImage();
                }
                catch (Exception ex)
                {
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        // Cannot capture an image until the previous capture has completed.
                        txtDebug.Text = ex.Message;
                    });
                }
            }
        }

        void cam_CaptureCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
        }


        // Informs when full resolution picture has been taken, saves to local media library and isolated storage.
        void cam_CaptureImageAvailable(object sender, Microsoft.Devices.ContentReadyEventArgs e)
        {        
        }

        // Informs when thumbnail picture has been taken, saves to isolated storage
        // User will select this image in the pictures application to bring up the full-resolution picture. 
        public void cam_CaptureThumbnailAvailable(object sender, ContentReadyEventArgs e)
        {          
        }

        // Activate a flash mode.
        // Cycle through flash mode options when the flash button is pressed.
        private void changeFlash_Clicked(object sender, RoutedEventArgs e)
        {

        }

        // Provide auto-focus in the viewfinder.
        private void focus_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            if (cam.IsFocusSupported == true)
            {
                //Focus when a capture is not in progress.
                try
                {
                    cam.Focus();
                }
                catch (Exception focusError)
                {
                    // Cannot focus when a capture is in progress.
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = focusError.Message;
                    });
                }
            }
            else
            {
                // Write message to UI.
                this.Dispatcher.BeginInvoke(delegate()
                {
                    txtDebug.Text = "Camera does not support programmable auto focus.";
                });
            }
        }

        void cam_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(delegate()
            {
                // Write message to UI.
                txtDebug.Text = "Auto focus has completed.";
            });
        }

        // Provide touch focus in the viewfinder.
        void focus_Tapped(object sender, GestureEventArgs e)
        {
         
        }

        private void changeRes_Clicked(object sender, System.Windows.RoutedEventArgs e)
        {
            // Variables
            IEnumerable<Size> resList = cam.AvailableResolutions;
            int resCount = resList.Count<Size>();
            Size res;

            // Poll for available camera resolutions.
            for (int i = 0; i < resCount; i++)
            {
                res = resList.ElementAt<Size>(i);
            }

            // Set the camera resolution.
            res = resList.ElementAt<Size>((currentResIndex + 1) % resCount);
            cam.Resolution = res;
            currentResIndex = (currentResIndex + 1) % resCount;

            // Update the UI.
            txtDebug.Text = String.Format("Setting capture resolution: {0}x{1}", res.Width, res.Height);
        }


        // Provide auto-focus with a half button press using the hardware shutter button.
        private void OnButtonHalfPress(object sender, EventArgs e)
        {
            if (cam != null)
            {
                // Focus when a capture is not in progress.
                try
                {
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = "Half Button Press: Auto Focus";
                    });

                    cam.Focus();
                }
                catch (Exception focusError)
                {
                    // Cannot focus when a capture is in progress.
                    this.Dispatcher.BeginInvoke(delegate()
                    {
                        txtDebug.Text = focusError.Message;
                    });
                }
            }
        }

        // Capture the image with a full button press using the hardware shutter button.
        private void OnButtonFullPress(object sender, EventArgs e)
        {
            if (cam != null)
            {
                cam.CaptureImage();
            }
        }

        // Cancel the focus if the half button press is released using the hardware shutter button.
        private void OnButtonRelease(object sender, EventArgs e)
        {

            if (cam != null)
            {
                cam.CancelFocus();
            }
        }


        private void ShowCameraImage(object sender, RoutedEventArgs e)
        {
            viewfinderCanvas.Visibility = Visibility.Visible;
            imgExample.Visibility = Visibility.Collapsed;
        }

        private void ShowImage1(object sender, RoutedEventArgs e)
        {
            viewfinderCanvas.Visibility = Visibility.Collapsed;
            imgExample.Visibility = Visibility.Visible;
            imgExample.Source = PictureDecoder.DecodeJpeg(new MemoryStream(Resourcses._02_schlecht));
        }

        private void ShowImage2(object sender, RoutedEventArgs e)
        {
            viewfinderCanvas.Visibility = Visibility.Collapsed;
            imgExample.Visibility = Visibility.Visible;
            imgExample.Source = PictureDecoder.DecodeJpeg(new MemoryStream(Resourcses._03_schlecht));
        }

        private void ShowImage3(object sender, RoutedEventArgs e)
        {
            viewfinderCanvas.Visibility = Visibility.Collapsed;
            imgExample.Visibility = Visibility.Visible;
            imgExample.Source = PictureDecoder.DecodeJpeg(new MemoryStream(Resourcses._04_schlecht));
        }

        private void ShowImage4(object sender, RoutedEventArgs e)
        {
            viewfinderCanvas.Visibility = Visibility.Collapsed;
            imgExample.Visibility = Visibility.Visible;
            imgExample.Source = PictureDecoder.DecodeJpeg(new MemoryStream(Resourcses._01_gut));
        }
    }
}
