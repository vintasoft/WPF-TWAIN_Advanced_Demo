using System;
using System.Windows;
using Vintasoft.WpfTwain.ImageEncoders;

namespace WpfTwainAdvancedDemo
{
    /// <summary>
    /// Interaction logic for TiffSaveSettingsForm.xaml
    /// </summary>
    public partial class TiffSaveSettingsWindow : Window
    {

        #region Constructor

        public TiffSaveSettingsWindow(Window owner, bool isFileExist)
        {
            InitializeComponent();

            this.Owner = owner;

            if (!isFileExist)
            {
                createNewDocumentRadioButton.IsChecked = true;
                addToDocumentRadioButton.IsEnabled = false;
            }
        }

        #endregion



        #region Properties

        bool _saveAllImages = false;
        public bool SaveAllImages
        {
            get { return _saveAllImages; }
        }

        TwainTiffEncoderSettings _encoderSettings = new TwainTiffEncoderSettings();
        public TwainTiffEncoderSettings EncoderSettings
        {
            get { return _encoderSettings; }
        }

        #endregion

        
        
        #region Methods

        private void useStripsRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (rowsPerStripLabel == null)
                return;

            rowsPerStripLabel.IsEnabled = true;
            rowsPerStripNumericUpDown.IsEnabled = true;
            tileSizeLabel.IsEnabled = false;
            tileSizeNumericUpDown.IsEnabled = false;
        }

        private void useTilesRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (rowsPerStripLabel == null)
                return;

            rowsPerStripLabel.IsEnabled = false;
            rowsPerStripNumericUpDown.IsEnabled = false;
            tileSizeLabel.IsEnabled = true;
            tileSizeNumericUpDown.IsEnabled = true;
        }

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            _saveAllImages = (bool)saveAllImagesRadioButton.IsChecked;

            try
            {
                _encoderSettings.TiffMultiPage = (bool)addToDocumentRadioButton.IsChecked;

                if ((bool)comprNoneRadioButton.IsChecked)
                    _encoderSettings.TiffCompression = TiffCompression.None;
                else if ((bool)comprCcittRadioButton.IsChecked)
                    _encoderSettings.TiffCompression = TiffCompression.CCITGroup4;
                else if ((bool)comprLzwRadioButton.IsChecked)
                    _encoderSettings.TiffCompression = TiffCompression.LZW;
                else if ((bool)comprJpegRadioButton.IsChecked)
                {
                    _encoderSettings.TiffCompression = TiffCompression.JPEG;
                    _encoderSettings.JpegQuality = (int)jpegQualityNumericUpDown.Value;
                }
                else if ((bool)comprZipRadioButton.IsChecked)
                    _encoderSettings.TiffCompression = TiffCompression.ZIP;
                else if ((bool)comprAutoRadioButton.IsChecked)
                    _encoderSettings.TiffCompression = TiffCompression.Auto;

                if (useStripsRadioButton.IsChecked == true)
                    _encoderSettings.UseTiles = false;
                else
                    _encoderSettings.UseTiles = true;
                _encoderSettings.RowsPerStrip = (int)rowsPerStripNumericUpDown.Value;
                _encoderSettings.TileSize = (int)tileSizeNumericUpDown.Value;

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void EnableJpegCompressionQuality(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible)
                return;

            jpegCompressionGroupBox.IsEnabled = true;
        }

        private void DisableJpegCompressionQuality(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible)
                return;

            jpegCompressionGroupBox.IsEnabled = false;
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

    }
}
