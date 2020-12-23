using System;
using System.Windows;
using Vintasoft.WpfTwain.ImageEncoders;

namespace WpfTwainAdvancedDemo
{
    /// <summary>
    /// Interaction logic for PdfSaveSettingsForm.xaml
    /// </summary>
    public partial class PdfSaveSettingsWindow : Window
    {

        #region Constructor

        public PdfSaveSettingsWindow(Window owner, bool isFileExist)
        {
            InitializeComponent();

            this.Owner = owner;

            if (!isFileExist)
            {
                rbCreateNewDocument.IsChecked = true;
                rbAddToDocument.IsEnabled = false;
            }
        }

        #endregion



        #region Properties

        bool _saveAllImages = false;
        public bool SaveAllImages
        {
            get { return _saveAllImages; }
        }

        TwainPdfEncoderSettings _encoderSettings = new TwainPdfEncoderSettings();
        public TwainPdfEncoderSettings EncoderSettings
        {
            get { return _encoderSettings; }
        }

        #endregion

        
        
        #region Methods

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            _saveAllImages = (bool)rbSaveAllImages.IsChecked;

            try
            {
                _encoderSettings.PdfMultiPage = (bool)rbAddToDocument.IsChecked;
                _encoderSettings.PdfACompatible = (bool)chkPdfACompatible.IsChecked;
                _encoderSettings.PdfDocumentInfo.Author = txtPdfAuthor.Text;
                _encoderSettings.PdfDocumentInfo.Title = txtPdfTitle.Text;

                if ((bool)rbComprNone.IsChecked)
                    _encoderSettings.PdfImageCompression = PdfImageCompression.None;
                else if ((bool)rbComprCCITT.IsChecked)
                    _encoderSettings.PdfImageCompression = PdfImageCompression.CcittFax;
                else if ((bool)rbComprLzw.IsChecked)
                    _encoderSettings.PdfImageCompression = PdfImageCompression.LZW;
                else if ((bool)rbComprJpeg.IsChecked)
                {
                    _encoderSettings.PdfImageCompression = PdfImageCompression.JPEG;
                    _encoderSettings.JpegQuality = (int)jpegQualityNumericUpDown.Value;
                }
                else if ((bool)rbComprZip.IsChecked)
                    _encoderSettings.PdfImageCompression = PdfImageCompression.ZIP;
                else if ((bool)rbComprAuto.IsChecked)
                    _encoderSettings.PdfImageCompression = PdfImageCompression.Auto;

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

            gbJpegCompression.IsEnabled = true;
        }

        private void DisableJpegCompressionQuality(object sender, RoutedEventArgs e)
        {
            if (!this.IsVisible)
                return;

            gbJpegCompression.IsEnabled = false;
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

    }
}
