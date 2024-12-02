﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Vintasoft.WpfTwain;
using Vintasoft.WpfTwain.ImageEncoders;
using Vintasoft.WpfTwain.ImageProcessing;

namespace WpfTwainAdvancedDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        #region Fields

        /// <summary>
        /// TWAIN device manager.
        /// </summary>
        DeviceManager _deviceManager;

        /// <summary>
        /// Current device.
        /// </summary>
        Device _currentDevice;

        /// <summary>
        /// Indicates that device is acquiring image(s).
        /// </summary>
        bool _isImageAcquiring;

        /// <summary>
        /// Acquired image collection.
        /// </summary>
        AcquiredImageCollection _images = new AcquiredImageCollection();

        /// <summary>
        /// Current image index in acquired image collection.
        /// </summary>
        int _imageIndex = -1;

        /// <summary>
        /// Determines that image acquistion must be canceled because application's window is closing.
        /// </summary>
        bool _cancelTransferBecauseWindowIsClosing;

        SaveFileDialog _saveFileDialog1 = new SaveFileDialog();

        #endregion



        #region Constructors

        public MainWindow()
        {
            // register the evaluation license for VintaSoft TWAIN .NET SDK
            Vintasoft.Twain.TwainGlobalSettings.Register("REG_USER", "REG_EMAIL", "EXPIRATION_DATE", "REG_CODE");

            InitializeComponent();

            this.Title = string.Format("VintaSoft WPF TWAIN Advanced Demo v{0}", TwainGlobalSettings.ProductVersion);

            _saveFileDialog1.FileName = "doc1";
            _saveFileDialog1.Filter = "BMP image|*.bmp|JPEG image|*.jpg|PNG image|*.png|TIFF image|*.tif|PDF document|*.pdf";
            _saveFileDialog1.FilterIndex = 2;

            // get country and language for TWAIN device manager
            CountryCode country;
            LanguageType language;
            GetCountryAndLanguage(out country, out language);

            // create TWAIN device manager
            _deviceManager = new DeviceManager(this, country, language);

            UpdateUI();
        }

        #endregion



        #region Methods

        #region Device manager

        /// <summary>
        /// Opens TWAIN device manager.
        /// </summary>
        private void openDeviceManagerButton_Click(object sender, RoutedEventArgs e)
        {
            // clear list of TWAIN devices
            devicesComboBox.Items.Clear();

            // if TWAIN device manager is opened
            if (_deviceManager.State == DeviceManagerState.Opened)
            {
                // close the TWAIN device manager
                _deviceManager.Close();

                // change text on this button
                openDeviceManagerButton.Content = "Open device manager";
            }

            // if TWAIN device manager is closed
            else
            {
                try
                {
                    // if TWAIN device manager 2.x must be used
                    if (twain2CompatibleCheckBox.IsChecked == true)
                    {
                        // get path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                        string twainDsmDllCustomPath = GetTwainDsmCustomPath(IntPtr.Size == 4);
                        // if file exist
                        if (twainDsmDllCustomPath != null)
                            // specify that SDK should use TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                            _deviceManager.TwainDllPath = twainDsmDllCustomPath;

                        _deviceManager.IsTwain2Compatible = true;
                    }
                    else
                        _deviceManager.IsTwain2Compatible = false;
                }
                catch (Exception ex)
                {
                    // show dialog with error message
                    MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device manager", MessageBoxButton.OK, MessageBoxImage.Error);

                    // open a HTML page with article describing how to solve the problem
                    OpenBrowser("https://www.vintasoft.com/docs/vstwain-dotnet/Programming-Twain-Device_Manager.html");

                    return;
                }

                // TWAIN device manager is found

                // if TWAIN device manager 2.x (64-bit) is used
                if (IntPtr.Size == 8 && _deviceManager.IsTwain2Compatible)
                {
                    if (!InitTwain2DeviceManagerMode())
                        return;
                }

                try
                {
                    // open the TWAIN device manager
                    _deviceManager.Open();
                }
                catch (TwainDeviceManagerException ex)
                {
                    // close the TWAIN device manager
                    _deviceManager.Close();

                    // show dialog with error message
                    MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device manager", MessageBoxButton.OK, MessageBoxImage.Error);

                    // open a HTML page with article describing how to solve the problem
                    OpenBrowser("http://www.vintasoft.com/docs/vstwain-dotnet/index.html?Programming-Twain-Device_Manager.html");

                    return;
                }

                DeviceCollection devices = _deviceManager.Devices;
                // for each available TWAIN device
                for (int i = 0; i < devices.Count; i++)
                {
                    // add the TWAIN device name to a combo box
                    devicesComboBox.Items.Add(devices[i].Info.ProductName);

                    // if TWAIN device is default device
                    if (devices[i] == _deviceManager.DefaultDevice)
                        // select TWAIN device in a combo box
                        devicesComboBox.SelectedIndex = i;
                }

                // change text on this button
                openDeviceManagerButton.Content = "Close device manager";
            }

            // update UI
            UpdateUI();
        }

        /// <summary>
        /// Initializes the device manager mode.
        /// </summary>
        private bool InitTwain2DeviceManagerMode()
        {
            // create a window that allows to view and edit mode of 64-bit TWAIN2 device manager
            SelectDeviceManagerModeWindow window = new SelectDeviceManagerModeWindow();
            // initialize window
            window.Owner = this;
            window.Use32BitDevices = _deviceManager.Are32BitDevicesUsed;

            // show dialog
            if (window.ShowDialog() == true)
            {
                // get path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                string twainDsmDllCustomPath = GetTwainDsmCustomPath(window.Use32BitDevices);
                // if file exist
                if (twainDsmDllCustomPath != null)
                    // specify that SDK should use TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK
                    _deviceManager.TwainDllPath = twainDsmDllCustomPath;

                // if device manager mode is changed
                if (window.Use32BitDevices != _deviceManager.Are32BitDevicesUsed)
                {
                    try
                    {
                        // if 32-bit devices must be used
                        if (window.Use32BitDevices)
                            _deviceManager.Use32BitDevices();
                        else
                            _deviceManager.Use64BitDevices();
                    }
                    catch (TwainDeviceManagerException ex)
                    {
                        // show dialog with error message
                        MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device manager", MessageBoxButton.OK, MessageBoxImage.Error);

                        return false;
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        #endregion


        #region Devices

        /// <summary>
        /// Selects the active device using TWAIN standard selection dialog.
        /// </summary>
        private void selectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            // show the TWAIN device selection dialog
            if (_deviceManager.ShowDefaultDeviceSelectionDialog())
            {
                DeviceCollection devices = _deviceManager.Devices;
                // for each device
                for (int i = 0; i < devices.Count; i++)
                {
                    // if device is default device
                    if (devices[i] == _deviceManager.DefaultDevice)
                        // select device in a combo box
                        devicesComboBox.SelectedIndex = i;
                }
            }
        }

        #endregion


        #region Device

        /// <summary>
        /// Gets information about device and device capabilities.
        /// </summary>
        private void getDeviceInfoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // find a device by device name
                Device device = _deviceManager.Devices.Find((string)devicesComboBox.SelectedItem);

                // show dialog with information about device and device capabilities
                DevCapsWindow deviceCapabilitiesWindow = new DevCapsWindow(this, device);
                deviceCapabilitiesWindow.ShowDialog();
            }
            catch (TwainDeviceException ex)
            {
                MessageBox.Show(GetFullExceptionMessage(ex), "Device error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (TwainDeviceCapabilityException ex)
            {
                MessageBox.Show(GetFullExceptionMessage(ex), "Device capability rror", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Enables/disables User Interface of device.
        /// </summary>
        private void showUICheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (showIndicatorsCheckBox == null)
                return;

            modalUICheckBox.IsEnabled = (bool)showUICheckBox.IsChecked;
            disableAfterAcquireCheckBox.IsEnabled = (bool)showUICheckBox.IsChecked;

            adfGroupBox.IsEnabled = (bool)!showUICheckBox.IsChecked;
        }

        /// <summary>
        /// Transfer mode is changed.
        /// </summary>
        private void transferModeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (imageAcquisitionProgressBar == null)
                return;

            if (transferModeComboBox.SelectedIndex == 0)
                imageAcquisitionProgressBar.Visibility = Visibility.Hidden;
            else
                imageAcquisitionProgressBar.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Enables/disables usage of ADF.
        /// </summary>
        private void useAdfCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (useDuplexCheckBox != null)
            {
                useDuplexCheckBox.IsEnabled = (bool)useAdfCheckBox.IsChecked;
                imagesToAcquireNumericUpDown.IsEnabled = (bool)useAdfCheckBox.IsChecked;
            }
        }

        #endregion


        #region Image acquisition

        /// <summary>
        /// Starts the image acquisition.
        /// </summary>
        private void acquireImageButton_Click(object sender, RoutedEventArgs e)
        {
            // specify that image acquisition is started
            _isImageAcquiring = true;
            // update UI
            UpdateUI();

            try
            {
                if (_currentDevice != null)
                    // unsubscribe from the device events
                    UnsubscribeFromDeviceEvents(_currentDevice);

                // find the device by device name
                string deviceName = (string)devicesComboBox.SelectedItem;
                Device device = _deviceManager.Devices.Find(deviceName);
                if (device == null)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show(string.Format("Device '{0}' is not found.", deviceName), "TWAIN device", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _currentDevice = device;
                // subscribe to the device events
                SubscribeToDeviceEvents(_currentDevice);

                // set the image acquisition parameters
                device.ShowUI = (bool)showUICheckBox.IsChecked;
                device.ModalUI = (bool)modalUICheckBox.IsChecked;
                device.ShowIndicators = (bool)showIndicatorsCheckBox.IsChecked;
                device.DisableAfterAcquire = (bool)disableAfterAcquireCheckBox.IsChecked;

                // trasfer mode
                if (transferModeComboBox.SelectedIndex == 0)
                    device.TransferMode = TransferMode.Native;
                else
                    device.TransferMode = TransferMode.Memory;

                try
                {
                    // open the device
                    device.Open();
                }
                catch (TwainException ex)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // set device capabilities

                // pixel type
                PixelType pixelType = PixelType.BW;
                try
                {
                    if (pixelTypeComboBox.SelectedIndex == 1)
                        pixelType = PixelType.Gray;
                    else if (pixelTypeComboBox.SelectedIndex == 2)
                        pixelType = PixelType.RGB;

                    if (device.PixelType != pixelType)
                        device.PixelType = pixelType;
                }
                catch (TwainException)
                {
                    MessageBox.Show(string.Format("Pixel type \"{0}\" is not supported.", pixelType), "TWAIN device");
                }

                // unit of measure
                try
                {
                    if (device.UnitOfMeasure != UnitOfMeasure.Inches)
                        device.UnitOfMeasure = UnitOfMeasure.Inches;
                }
                catch (TwainException)
                {
                    MessageBox.Show("Unit of measure \"Inches\" is not supported.", "TWAIN device");
                }

                // resolution
                Resolution resolution = new Resolution(100, 100);
                try
                {
                    if (resolutionComboBox.SelectedIndex == 1)
                        resolution = new Resolution(150, 150);
                    else if (resolutionComboBox.SelectedIndex == 2)
                        resolution = new Resolution(200, 200);
                    else if (resolutionComboBox.SelectedIndex == 3)
                        resolution = new Resolution(300, 300);
                    else if (resolutionComboBox.SelectedIndex == 4)
                        resolution = new Resolution(600, 600);

                    if (device.Resolution.GetXResolutionInDpi() != resolution.GetXResolutionInDpi() ||
                        device.Resolution.GetYResolutionInDpi() != resolution.GetYResolutionInDpi())
                        device.Resolution = resolution;
                }
                catch (TwainException)
                {
                    MessageBox.Show(string.Format("Resolution \"{0}\" is not supported.", resolution), "TWAIN device");
                }

                // if device is Fujitsu scanner
                if (device.Info.ProductName.ToUpper().StartsWith("FUJITSU"))
                {
                    DeviceCapability undefinedImageSizeCap = device.Capabilities.Find(DeviceCapabilityId.IUndefinedImageSize);
                    // if undefined image size is supported
                    if (undefinedImageSizeCap != null)
                    {
                        try
                        {
                            // enable undefined image size feature
                            undefinedImageSizeCap.SetValue(true);
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }
                    }
                }

                try
                {
                    // if ADF present
                    if (!device.Info.IsWIA && device.HasFeeder)
                    {
                        // enable/disable ADF if necessary
                        try
                        {
                            if (device.DocumentFeeder.Enabled != useAdfCheckBox.IsChecked)
                                device.DocumentFeeder.Enabled = (bool)useAdfCheckBox.IsChecked;
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }

                        // enable/disable duplex if necessary
                        try
                        {
                            if (device.DocumentFeeder.DuplexEnabled != useDuplexCheckBox.IsChecked)
                                device.DocumentFeeder.DuplexEnabled = (bool)useDuplexCheckBox.IsChecked;
                        }
                        catch (TwainDeviceCapabilityException)
                        {
                        }
                    }

                    if (acquireAllImagesRadioButton.IsChecked == false)
                    {
                        Int16 xferCount = (Int16)imagesToAcquireNumericUpDown.Value;

                        // patch for "bug" in Kodak drivers
                        if (device.Info.ProductName.ToUpper().StartsWith("KODAK"))
                        {
                            if (device.ShowUI && xferCount == 1)
                                xferCount = -1;
                        }

                        device.XferCount = xferCount;
                    }
                }
                catch (TwainException ex)
                {
                    MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device");
                }

                // if device supports asynchronous events
                if (device.IsAsyncEventsSupported)
                {
                    try
                    {
                        // enable all asynchronous events supported by device
                        device.AsyncEvents = device.GetSupportedAsyncEvents();
                    }
                    catch
                    {
                    }
                }


                try
                {
                    // start image acquition process
                    device.Acquire();
                }
                catch (TwainException ex)
                {
                    // specify that image acquisition is finished
                    _isImageAcquiring = false;

                    MessageBox.Show(GetFullExceptionMessage(ex), "TWAIN device", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            finally
            {
                // update UI
                UpdateUI();
            }
        }

        /// <summary>
        /// Subscribe to the device events.
        /// </summary>
        private void SubscribeToDeviceEvents(Device device)
        {
            device.ImageAcquiringProgress += new EventHandler<ImageAcquiringProgressEventArgs>(_device_ImageAcquiringProgress);
            device.ImageAcquired += new EventHandler<ImageAcquiredEventArgs>(_device_ImageAcquired);
            device.ScanFailed += new EventHandler<ScanFailedEventArgs>(_device_ScanFailed);
            device.AsyncEvent += new EventHandler<DeviceAsyncEventArgs>(device_AsyncEvent);
            device.ScanFinished += new EventHandler(device_ScanFinished);
        }

        /// <summary>
        /// Unsubscribe from the device events.
        /// </summary>
        private void UnsubscribeFromDeviceEvents(Device device)
        {
            device.ImageAcquiringProgress -= new EventHandler<ImageAcquiringProgressEventArgs>(_device_ImageAcquiringProgress);
            device.ImageAcquired -= new EventHandler<ImageAcquiredEventArgs>(_device_ImageAcquired);
            device.ScanFailed -= new EventHandler<ScanFailedEventArgs>(_device_ScanFailed);
            device.AsyncEvent -= new EventHandler<DeviceAsyncEventArgs>(device_AsyncEvent);
            device.ScanFinished -= new EventHandler(device_ScanFinished);
        }

        /// <summary>
        /// Image acquiring progress is changed.
        /// </summary>
        private void _device_ImageAcquiringProgress(object sender, ImageAcquiringProgressEventArgs e)
        {
            // image acquistion must be canceled because application's window is closing
            if (_cancelTransferBecauseWindowIsClosing)
            {
                // cancel image acquisition
                _currentDevice.CancelTransfer();
                return;
            }

            imageAcquisitionProgressBar.Value = (int)e.Progress;

            if (imageAcquisitionProgressBar.Value == 100)
            {
                imageAcquisitionProgressBar.Value = 0;
            }
        }

        /// <summary>
        /// Image is acquired.
        /// </summary>
        private void _device_ImageAcquired(object sender, ImageAcquiredEventArgs e)
        {
            // image acquistion must be canceled because application's window is closing
            if (_cancelTransferBecauseWindowIsClosing)
            {
                // cancel image acquisition
                _currentDevice.CancelTransfer();
                return;
            }

            _images.Add(e.Image);

            SetCurrentImage(_images.Count - 1);
        }

        /// <summary>
        /// Scan is failed.
        /// </summary>
        private void _device_ScanFailed(object sender, ScanFailedEventArgs e)
        {
            // show error message
            MessageBox.Show(e.ErrorString, "Scan is failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }


        /// <summary>
        /// An asynchronous event was generated by device.
        /// </summary>
        private void device_AsyncEvent(object sender, DeviceAsyncEventArgs e)
        {
            switch (e.DeviceEvent)
            {
                case DeviceEventId.PaperJam:
                    MessageBox.Show("Paper is jammed.");
                    break;

                case DeviceEventId.CheckDeviceOnline:
                    MessageBox.Show("Check that device is online.");
                    break;

                case DeviceEventId.CheckBattery:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, BatteryMinutes={2}, BatteryPercentage={3}",
                        e.DeviceName, e.DeviceEvent, e.BatteryMinutes, e.BatteryPercentage));
                    break;

                case DeviceEventId.CheckPowerSupply:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, PowerSupply={2}",
                        e.DeviceName, e.DeviceEvent, e.PowerSupply));
                    break;

                case DeviceEventId.CheckResolution:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, Resolution={2}",
                        e.DeviceName, e.DeviceEvent, e.Resolution));
                    break;

                case DeviceEventId.CheckFlash:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, FlashUsed={2}",
                        e.DeviceName, e.DeviceEvent, e.FlashUsed));
                    break;

                case DeviceEventId.CheckAutomaticCapture:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}, AutomaticCapture={2}, TimeBeforeFirstCapture={3}, TimeBetweenCaptures={4}",
                        e.DeviceName, e.DeviceEvent, e.AutomaticCapture, e.TimeBeforeFirstCapture, e.TimeBetweenCaptures));
                    break;

                default:
                    MessageBox.Show(string.Format("DeviceEvent: Device={0}, Event={1}",
                        e.DeviceName, e.DeviceEvent));
                    break;
            }

            // if device is enabled or transferring images
            if (_currentDevice.State >= DeviceState.Enabled)
                return;

            // close the device
            _currentDevice.Close();
        }

        /// <summary>
        /// Scan is finished.
        /// </summary>
        void device_ScanFinished(object sender, EventArgs e)
        {
            // close the device
            _currentDevice.Close();

            // specify that image acquisition is finished
            _isImageAcquiring = false;
            // update UI
            UpdateUI();
        }

        #endregion


        #region Acquired images

        #region Navigation

        /// <summary>
        /// Shows previous acquired image.
        /// </summary>
        private void previousImageButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentImage(_imageIndex - 1);
            // update UI
            UpdateUI();
        }

        /// <summary>
        /// Shows next acquired image.
        /// </summary>
        private void nextImageButton_Click(object sender, RoutedEventArgs e)
        {
            SetCurrentImage(_imageIndex + 1);
            // update UI
            UpdateUI();
        }

        #endregion


        #region Preview

        /// <summary>
        /// Gets the information about current image.
        /// </summary>
        private string GetCurrentImageInfo(int index, AcquiredImage acquiredImage)
        {
            ImageInfo imageInfo = acquiredImage.ImageInfo;
            return string.Format("Image {0} from {1} ({2} x {3}, {4} bpp, {5})", index + 1, _images.Count, imageInfo.Width, imageInfo.Height, imageInfo.BitCount, imageInfo.Resolution);
        }

        /// <summary>
        /// Sets the current image.
        /// </summary>
        private void SetCurrentImage(int index)
        {
            lock (this)
            {
                // dispose previous image if necessary
                if (image1.Source != null)
                    image1.Source = null;
                image1.Width = 0;
                image1.Height = 0;


                // get the image from the internal buffer of the device if image is present
                if (index >= 0)
                {
                    AcquiredImage acquiredImage = _images[index];

                    image1.Source = acquiredImage.GetAsBitmapSource();
                    SetImageScrolls();

                    imageInfoLabel.Content = GetCurrentImageInfo(index, acquiredImage);

                    _imageIndex = index;
                }
                // show "No images" text
                else
                {
                    imageInfoLabel.Content = "No images";

                    _imageIndex = -1;
                }

                // update UI
                UpdateUI();
            }
        }

        /// <summary>
        /// Changes preview mode of current image.
        /// </summary>
        private void stretchImageCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            SetImageScrolls();
        }

        /// <summary>
        /// Sets scrolls of image.
        /// </summary>
        private void SetImageScrolls()
        {
            if (image1 != null)
            {
                if (stretchImageCheckBox.IsChecked == true)
                {
                    image1.Width = imageScrollViewer.ViewportWidth - 5;
                    image1.Height = imageScrollViewer.ViewportHeight - 5;
                    image1.Stretch = Stretch.Fill;
                }
                else
                {
                    image1.Width = image1.Source.Width;
                    image1.Height = image1.Source.Height;
                    image1.Stretch = Stretch.None;
                }
            }
        }

        /// <summary>
        /// Window of application is resized.
        /// </summary>
        private void MainWindow_Resize(object sender, SizeChangedEventArgs e)
        {
            if (stretchImageCheckBox.IsChecked == true)
            {
                image1.Margin = new Thickness(2, 2, 2, 2);
            }

            if (_imageIndex >= 0)
            {
                UpdateLayout();
                SetImageScrolls();
            }
        }

        #endregion


        #region Processing

        /// <summary>
        /// Processes acquired image.
        /// </summary>
        private void processImageButton_Click(object sender, RoutedEventArgs e)
        {
            // get reference to current image
            AcquiredImage currentImage = _images[_imageIndex];

            // process current image
            ImageProcessingWindow window1 = new ImageProcessingWindow(currentImage);
            window1.ShowDialog();

            // update current image
            SetCurrentImage(_imageIndex);
        }

        /// <summary>
        /// Shows information about progress of image processing function.
        /// </summary>
        private void currentImage_ImageProcessingProgress(object sender, TwainAcquiredImageProcessingProgressEventArgs e)
        {
            imageAcquisitionProgressBar.Value = (int)e.Progress;
            if (imageAcquisitionProgressBar.Value == 100)
            {
                imageAcquisitionProgressBar.Value = 0;
            }
        }

        #endregion


        #region Save

        /// <summary>
        /// Saves acquired image.
        /// </summary>
        private void saveImageButton_Click(object sender, RoutedEventArgs e)
        {
            _saveFileDialog1.FileName = "";
            if (_saveFileDialog1.ShowDialog() == true)
            {
                bool isFileExist = File.Exists(_saveFileDialog1.FileName);
                bool saveAllImages = false;
                try
                {
                    TwainImageEncoderSettings encoderSettings = null;

                    switch (_saveFileDialog1.FilterIndex)
                    {
                        case 2:	// JPEG
                            JpegSaveSettingsWindow jpegSettingsDlg = new JpegSaveSettingsWindow(this);
                            if (!(bool)jpegSettingsDlg.ShowDialog())
                                return;

                            encoderSettings = jpegSettingsDlg.EncoderSettings;
                            break;

                        case 4: // TIFF
                            TiffSaveSettingsWindow tiffSettingsDlg = new TiffSaveSettingsWindow(this, isFileExist);
                            if (!(bool)tiffSettingsDlg.ShowDialog())
                                return;

                            saveAllImages = tiffSettingsDlg.SaveAllImages;
                            encoderSettings = tiffSettingsDlg.EncoderSettings;
                            break;

                        case 5: // PDF
                            PdfSaveSettingsWindow pdfSettingsDlg = new PdfSaveSettingsWindow(this, isFileExist);
                            if (!(bool)pdfSettingsDlg.ShowDialog())
                                return;

                            saveAllImages = pdfSettingsDlg.SaveAllImages;
                            encoderSettings = pdfSettingsDlg.EncoderSettings;
                            break;
                    }

                    Cursor = Cursors.Wait;

                    string filename = _saveFileDialog1.FileName;
                    // save all images to specified file
                    if (saveAllImages)
                    {
                        // save first image
                        _images[0].Save(filename, encoderSettings);

                        // enable multipage support if necessary
                        if (_saveFileDialog1.FilterIndex == 4)
                            ((TwainTiffEncoderSettings)encoderSettings).TiffMultiPage = true;
                        else if (_saveFileDialog1.FilterIndex == 5)
                            ((TwainPdfEncoderSettings)encoderSettings).PdfMultiPage = true;

                        // save second and next images
                        for (int i = 1; i < _images.Count; i++)
                            _images[i].Save(filename, encoderSettings);
                    }
                    // save only current image to specified file
                    else
                        _images[_imageIndex].Save(filename, encoderSettings);

                    Cursor = Cursors.Arrow;

                    MessageBox.Show("Image(s) saved successfully!");
                }
                catch (Exception ex)
                {
                    Cursor = Cursors.Arrow;
                    MessageBox.Show(GetFullExceptionMessage(ex), "Saving error");
                }
            }
        }

        #endregion


        #region Upload

        /// <summary>
        /// Uploads acquired image.
        /// </summary>
        private void uploadImageButton_Click(object sender, RoutedEventArgs e)
        {
            UploadWindow uploadWindow = new UploadWindow(this, _images[_imageIndex]);
            uploadWindow.ShowDialog();
        }

        #endregion


        #region Delete, clear

        /// <summary>
        /// Removes image from collection of acquired images
        /// </summary>
        private void deleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            // dispose the image
            _images[_imageIndex].Dispose();

            // remove image from image collection
            _images.RemoveAt(_imageIndex);

            if (_imageIndex >= (_images.Count - 1))
                _imageIndex = _images.Count - 1;

            SetCurrentImage(_imageIndex);
        }

        /// <summary>
        /// Clears acquired image collection.
        /// </summary>
        private void clearImagesButton_Click(object sender, RoutedEventArgs e)
        {
            // dispose all images from image collection and clear the image collection
            _images.ClearAndDisposeItems();

            _imageIndex = -1;

            SetCurrentImage(_imageIndex);
        }

        #endregion

        #endregion


        /// <summary>
        /// Returns country and language for TWAIN device manager.
        /// </summary>
        /// <remarks>
        /// Unfortunately only KODAK scanners allow to set country and language.
        /// </remarks>
        private void GetCountryAndLanguage(out CountryCode country, out LanguageType language)
        {
            country = CountryCode.Usa;
            language = LanguageType.EnglishUsa;

            switch (CultureInfo.CurrentUICulture.Parent.IetfLanguageTag)
            {
                case "de":
                    country = CountryCode.Germany;
                    language = LanguageType.German;
                    break;

                case "es":
                    country = CountryCode.Spain;
                    language = LanguageType.Spanish;
                    break;

                case "fr":
                    country = CountryCode.France;
                    language = LanguageType.French;
                    break;

                case "it":
                    country = CountryCode.Italy;
                    language = LanguageType.Italian;
                    break;

                case "pt":
                    country = CountryCode.Portugal;
                    language = LanguageType.Portuguese;
                    break;

                case "ru":
                    country = CountryCode.Russia;
                    language = LanguageType.Russian;
                    break;
            }
        }


        /// <summary>
        /// Updates UI.
        /// </summary>
        private void UpdateUI()
        {
            bool isDeviceManagerOpened = _deviceManager.State == DeviceManagerState.Opened;
            bool hasDevices = false;
            if (isDeviceManagerOpened)
            {
                if (_deviceManager.Devices.Count > 0)
                    hasDevices = true;
            }

            twain2CompatibleCheckBox.IsEnabled = _deviceManager.State == DeviceManagerState.Closed;
            openDeviceManagerButton.IsEnabled = !_isImageAcquiring;
            selectDeviceButton.IsEnabled = isDeviceManagerOpened && !_isImageAcquiring;

            acquireImageButton.IsEnabled = isDeviceManagerOpened && hasDevices && !_isImageAcquiring;

            devicesComboBox.IsEnabled = isDeviceManagerOpened && !_isImageAcquiring;
            getDeviceInfoButton.IsEnabled = isDeviceManagerOpened && hasDevices && !_isImageAcquiring;

            imageGroupBox.IsEnabled = isDeviceManagerOpened && hasDevices && !_isImageAcquiring;
            userInterfaceGroupBox.IsEnabled = isDeviceManagerOpened && hasDevices && !_isImageAcquiring;
            adfGroupBox.IsEnabled = isDeviceManagerOpened && hasDevices && !_isImageAcquiring;


            // image navigation/processing

            if (_imageIndex > 0)
                previousImageButton.IsEnabled = true;
            else
                previousImageButton.IsEnabled = false;

            if (_imageIndex < (_images.Count - 1))
                nextImageButton.IsEnabled = true;
            else
                nextImageButton.IsEnabled = false;

            processImageButton.IsEnabled = _images.Count > 0;
            saveImageButton.IsEnabled = _images.Count > 0;
            uploadImageButton.IsEnabled = _images.Count > 0;
            deleteImageButton.IsEnabled = _images.Count > 0;
            clearImagesButton.IsEnabled = _images.Count > 0;

            stretchImageCheckBox.IsEnabled = _images.Count > 0;
        }

        /// <summary>
        /// Application window is closing.
        /// </summary>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_currentDevice != null)
            {
                // if image is acquiring
                if (_currentDevice.State > DeviceState.Enabled)
                {
                    // cancel image acquisition
                    _currentDevice.CancelTransfer();
                    // specify that form must be closed when image acquisition is canceled
                    _cancelTransferBecauseWindowIsClosing = true;
                    // cancel form closing
                    e.Cancel = true;
                    return;
                }

                // unsubscribe from device events
                UnsubscribeFromDeviceEvents(_currentDevice);
                // close the device
                _currentDevice.Close();
                _currentDevice = null;
            }

            // close the device manager
            _deviceManager.Close();
            // dispose the device manager
            _deviceManager.Dispose();

            // dispose all images from image collection and clear the image collection
            _images.ClearAndDisposeItems();
        }

        /// <summary>
        /// Returns the message of exception and inner exceptions.
        /// </summary>
        private string GetFullExceptionMessage(System.Exception ex)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine(ex.Message);

            System.Exception innerException = ex.InnerException;
            while (innerException != null)
            {
                if (ex.Message != innerException.Message)
                    sb.AppendLine(string.Format("Inner exception: {0}", innerException.Message));
                innerException = innerException.InnerException;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Opens the browser with specified URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        private void OpenBrowser(string url)
        {
            ProcessStartInfo pi = new ProcessStartInfo("cmd", string.Format("/c start {0}", url));
            pi.CreateNoWindow = true;
            pi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(pi);
        }

        /// <summary>
        /// Returns path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK.
        /// </summary>
        /// <param name="use32BitDevice">The value indicating whether the 32-bit TWAIN device must be used.</param>
        /// <returns>The path to the TWAIN device manager 2.x from installation of VintaSoft TWAIN .NET SDK.</returns>
        private string GetTwainDsmCustomPath(bool use32BitDevice)
        {
            string twainFolderName = "TWAINDSM64";
            if (use32BitDevice)
                twainFolderName = "TWAINDSM32";

            string[] binFolderPaths = { @"..\..\Bin", @"..\..\..\..\..\Bin", @"..\..\..\..\..\..\Bin" };
            string binFolderPath = null;
            for (int i = 0; i < binFolderPaths.Length; i++)
            {
                if (Directory.Exists(Path.Combine(binFolderPaths[i], twainFolderName)))
                {
                    binFolderPath = binFolderPaths[i];
                    break;
                }
            }

            if (binFolderPath != null)
            {
                if (use32BitDevice)
                    // get path to the TWAIN device manager 2.x (32-bit) from installation of VintaSoft TWAIN .NET SDK
                    return Path.Combine(binFolderPath, "TWAINDSM32", "TWAINDSM.DLL");
                else
                    // get path to the TWAIN device manager 2.x (64-bit) from installation of VintaSoft TWAIN .NET SDK
                    return Path.Combine(binFolderPath, "TWAINDSM64", "TWAINDSM.DLL");
            }

            return null;
        }

        #endregion

    }
}
