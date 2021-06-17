using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace bpng_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        BitmapImage[] pictures = new BitmapImage[2];
        int[] cutoff = new int[2] { 240, 245 };
        Bitmap output;

        public MainWindow()
        {
            InitializeComponent();
        }

        public void Update()
        {
            int[,] pattern = { { 0, 0 }, { 0, 1 } };
            const int blocksize = 2;

            try
            {
                cutoff[0] = Convert.ToInt32(numCutoff1.Text);
                cutoff[1] = Convert.ToInt32(numCutoff2.Text);
            }
            catch
            {
                return;
            }

            if (pictures[0] != null && pictures[1] != null)
            {
                System.Drawing.Size newSize = (bool)chkSize.IsChecked ? new System.Drawing.Size(Convert.ToInt32(pictures[0].Width), Convert.ToInt32(pictures[0].Height)) : new System.Drawing.Size(Convert.ToInt32(pictures[0].Width), Convert.ToInt32(pictures[0].Height));

                Bitmap temp = new Bitmap(BitmapConverter.ToBitmap(pictures[1]), newSize);
                output = new Bitmap(BitmapConverter.ToBitmap(pictures[0]), newSize);

                for (int blocky = 0; blocky < newSize.Height / blocksize; blocky++)
                {
                    for (int blockx = 0; blockx < newSize.Width / blocksize; blockx++)
                    {
                        for (int y = 0; y < blocksize; y++)
                        {
                            for (int x = 0; x < blocksize; x++)
                            {
                                int px = blockx * blocksize + x;
                                int py = blocky * blocksize + y;

                                if (pattern[y, x] > 0)
                                {
                                    Color c = temp.GetPixel(px, py);
                                    Color newcol = Color.FromArgb(ColorConv2(c.R), ColorConv2(c.G), ColorConv2(c.B));
                                    output.SetPixel(px, py, newcol);
                                }
                                else
                                {
                                    Color c = output.GetPixel(px, py);
                                    Color newcol = Color.FromArgb(ColorConv1(c.R), ColorConv1(c.G), ColorConv1(c.B));
                                    output.SetPixel(px, py, newcol);
                                }
                            }
                        }
                    }
                }
                resultPreview.Source = BitmapConverter.ToBitmapSource(output);
            }
        }

        public void Update(object sender, EventArgs e)
        {
            Update();
        }

        private void importImage1Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Images |*.jpg;*.png;*.gif";
            bool? result = dialog.ShowDialog();

            if (result is true)
            {
                pictures[0] = new BitmapImage(new Uri(dialog.FileName));
                image1Preview.Source = pictures[0];
                Update();
            }
        }

        private void importImage2Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Images |*.jpg;*.png;*.gif";
            bool? result = dialog.ShowDialog();

            if (result is true)
            {
                pictures[1] = new BitmapImage(new Uri(dialog.FileName));
                image2Preview.Source = pictures[1];
                Update();
            }
        }

        public int ColorConv1(int col)
        {
            return (int)(col * cutoff[0] / 255.0);
        }

        public int ColorConv2(int col)
        {
            return cutoff[1] + (int)((255.0 - cutoff[1]) * (col / 255.0));
        }

        private void numCutoff1_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void numCutoff2_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            byte[] gammadata = { 0x00, 0x00, 0x00, 0x04, 0x67, 0x41, 0x4D, 0x41, 0x00, 0x00, 0x05, 0xEB, 0xC1, 0x8A, 0xAF, 0xF8 };

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Portable Network Graphics Image File (.PNG) |*.png";
            bool? result = dialog.ShowDialog();

            if (result is true)
            {
                MemoryStream ms = new MemoryStream(1024);
                output.Save(ms, ImageFormat.Png);

                FileStream fs = new FileStream(dialog.FileName, FileMode.Create);
                ms.Seek(0, SeekOrigin.Begin);

                byte[] buf = new byte[8];

                ms.Read(buf, 0, 8);
                fs.Write(buf, 0, 8);

                BitmapDataReader bdr = new BitmapDataReader(ms);

                while (true)
                {
                    int len = bdr.ReadInt32();
                    int hdr = bdr.ReadInt32();

                    if (hdr == 0x67414D41)
                    {
                        ms.Seek(len + 4, SeekOrigin.Current);
                        fs.Write(gammadata, 0, 16);
                    }
                    else if (hdr == 0x73524742 || hdr == 0x6348524D)
                    {
                        ms.Seek(len + 4, SeekOrigin.Current);
                    }
                    else
                    {
                        int fullSz = len + 12;
                        if (buf.Length < fullSz) buf = new byte[fullSz];
                        ms.Seek(-8, SeekOrigin.Current);
                        ms.Read(buf, 0, fullSz);
                        fs.Write(buf, 0, fullSz);
                    }
                    if (hdr == 0x49454E44) 
                        break;
                }
                fs.Close();
            }
        }
    }
}
