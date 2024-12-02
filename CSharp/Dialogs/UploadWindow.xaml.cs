using System;
using System.Windows;
using System.Net;
using System.IO;
using Vintasoft.WpfTwain;
using Vintasoft.WpfTwain.ImageEncoders;
using Vintasoft.WpfTwain.ImageUploading.Ftp;
using Vintasoft.WpfTwain.ImageUploading.Http;
using System.Text;

namespace WpfTwainAdvancedDemo
{
    /// <summary>
    /// Interaction logic for UploadWindow.xaml
    /// </summary>
    public partial class UploadWindow : Window
    {

        #region Fields

        /// <summary>
        /// Acquired image to upload.
        /// </summary>
        AcquiredImage _acquiredImageToUpload;

        /// <summary>
        /// FTP uploader.
        /// </summary>
        FtpUpload _ftpUpload = null;
        /// <summary>
        /// HTTP uploader.
        /// </summary>
        HttpUpload _httpUpload = null;

        #endregion



        #region Constructors

        public UploadWindow(Window owner, AcquiredImage acquiredImageToUpload)
        {
            InitializeComponent();

            this.Owner = owner;

            _acquiredImageToUpload = acquiredImageToUpload;
        }

        #endregion



        #region Methdos

        #region FTP upload

        /// <summary>
        /// Start image uploading process to FTP server.
        /// </summary>
        private void ftpUploadButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Owner as MainWindow;
            ftpUploadButton.IsEnabled = false;
            ftpUploadCancelButton.IsEnabled = true;
            ftpUploadProgressBar.Value = 0;

            try
            {
                _ftpUpload = new FtpUpload(this);
                _ftpUpload.StatusChanged += new EventHandler<Vintasoft.WpfTwain.ImageUploading.Ftp.StatusChangedEventArgs>(_ftpUpload_StatusChanged);
                _ftpUpload.ProgressChanged += new EventHandler<Vintasoft.WpfTwain.ImageUploading.Ftp.ProgressChangedEventArgs>(_ftpUpload_ProgressChanged);
                _ftpUpload.Completed += new EventHandler<Vintasoft.WpfTwain.ImageUploading.Ftp.CompletedEventArgs>(_ftpUpload_Completed);

                _ftpUpload.Host = ftpServerTextBox.Text;

                int ftpServerPort = 21;
                try
                {
                    ftpServerPort = int.Parse(ftpServerPortTextBox.Text);
                }
                catch
                {
                }
                _ftpUpload.Port = ftpServerPort;

                _ftpUpload.User = ftpUserTextBox.Text;
                _ftpUpload.Password = ftpPasswTextBox.Password;
                _ftpUpload.PassiveMode = (bool)flagPassModeCheckBox.IsChecked;
                _ftpUpload.Timeout = 5000;
                _ftpUpload.Path = ftpPathTextBox.Text;
                _ftpUpload.AddFile(ftpFileNameTextBox.Text, _acquiredImageToUpload.GetAsStream(GetImageFileFormat(ftpFileNameTextBox.Text)));
                _ftpUpload.PostData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "FTP error", MessageBoxButton.OK, MessageBoxImage.Error);
                ftpUploadButton.IsEnabled = true;
                ftpUploadCancelButton.IsEnabled = false;
            }
            finally
            {
                ftpUploadProgressBar.Maximum = _ftpUpload.BytesTotal;
            }
        }

        /// <summary>
        /// Cancel image uploading process.
        /// </summary>
        private void ftpUploadCancelButton_Click(object sender, RoutedEventArgs e)
        {
            _ftpUpload.Abort();
        }

        /// <summary>
        /// Status of uploading process is changed.
        /// </summary>
        private void _ftpUpload_StatusChanged(object sender, Vintasoft.WpfTwain.ImageUploading.Ftp.StatusChangedEventArgs e)
        {
            ftpStatusLabel.Content = e.StatusString;
        }

        /// <summary>
        /// Progress of uploading process is changed.
        /// </summary>
        private void _ftpUpload_ProgressChanged(object sender, Vintasoft.WpfTwain.ImageUploading.Ftp.ProgressChangedEventArgs e)
        {
            ftpUploadProgressBar.Value = e.BytesUploaded;
            if (e.StatusCode == Vintasoft.WpfTwain.ImageUploading.Ftp.StatusCode.SendingData)
                ftpStatusLabel.Content = string.Format("{0}{1} Uploaded {2} bytes from {3} bytes.", e.StatusString, Environment.NewLine, e.BytesUploaded, e.BytesTotal);
        }

        /// <summary>
        /// Uploading process is completed.
        /// </summary>
        private void _ftpUpload_Completed(object sender, Vintasoft.WpfTwain.ImageUploading.Ftp.CompletedEventArgs e)
        {
            ftpStatusLabel.Content = "";

            if (e.ErrorCode == Vintasoft.WpfTwain.ImageUploading.Ftp.ErrorCode.None)
                MessageBox.Show("FTP: Image is uploaded successfully!", "FTP");
            else
                MessageBox.Show(e.ErrorString, "FTP error", MessageBoxButton.OK, MessageBoxImage.Error);

            ftpUploadButton.IsEnabled = true;
            ftpUploadCancelButton.IsEnabled = false;
        }

        #endregion


        #region HTTP upload

        /// <summary>
        /// Start image uploading process to FTP server.
        /// </summary>
        private void httpUploadButton_Click(object sender, RoutedEventArgs e)
        {
#if DOTNET_3_5
            MessageBox.Show("Not supported in .NET Framework 3.5", "HTTP error");
#else
            string url = httpUrlTextBox.Text;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            string fileName = "test.jpg";
            string acquiredImageAsBase64String = _acquiredImageToUpload.GetAsBase64String(GetImageFileFormat(fileName));

            string postData = string.Format("fileName={0}", fileName);
            postData += string.Format("&imageFileAsBase64String={0}", WebUtility.UrlEncode(acquiredImageAsBase64String));
            byte[] data = Encoding.ASCII.GetBytes(postData);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    MessageBox.Show("HTTP: Image is uploaded successfully!", "HTTP");
                }
                else
                {
                    string responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
                    MessageBox.Show(responseString, "HTTP error");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "HTTP error");
            }
#endif
        }

        #endregion


        #region Form events handlers

        /// <summary>
        /// Exit the window.
        /// </summary>
        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion


        private TwainImageEncoderSettings GetImageFileFormat(string filename)
        {
            string filenameExt = Path.GetExtension(filename);
            switch (filenameExt)
            {
                case ".bmp":
                    return new TwainBmpEncoderSettings();

                case ".pdf":
                    return new TwainPdfEncoderSettings();

                case ".png":
                    return new TwainPngEncoderSettings();

                case ".tif":
                case ".tiff":
                    return new TwainTiffEncoderSettings();
            }

            return new TwainJpegEncoderSettings();
        }

        #endregion

    }
}
