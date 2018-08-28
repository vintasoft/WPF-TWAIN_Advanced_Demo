using System;
using System.Windows;
using Vintasoft.WpfTwain.ImageEncoders;

namespace WpfTwainAdvancedDemo
{
    /// <summary>
    /// Interaction logic for JpegSaveSettingForm.xaml
    /// </summary>
    public partial class JpegSaveSettingsWindow : Window
    {

        #region Constructors

        public JpegSaveSettingsWindow(Window owner)
        {
            InitializeComponent();

            this.Owner = owner;
        }

        #endregion



        #region Properties

        TwainJpegEncoderSettings _encoderSettings = new TwainJpegEncoderSettings();
        public TwainJpegEncoderSettings EncoderSettings
        {
            get { return _encoderSettings; }
        }

        #endregion

        
        
        #region Methods

        private void bOk_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _encoderSettings.JpegQuality = nJpegQuality.Value;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error");
            }
        }

        private void bCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

    }
}
