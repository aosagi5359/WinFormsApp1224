namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "圖像文件(JPeg. Gif. Bmp, etc|*.jpg;*.jpeg;*.gif;*.bmp;*.tif;*.tiff;*.png|所有文件(*.*)|*.*";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    Bitmap MyBitmap = new Bitmap(openFileDialog.FileName);
                    this.pictureBox1.Image = MyBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                int Height = this.pictureBox1.Image.Height;
                int Width = this.pictureBox1.Image.Width;
                Bitmap newBitmap = new Bitmap(Width, Height);
                Bitmap oldbitmap = (Bitmap)this.pictureBox1.Image;

                int[] Prewitt_Gx = new int[] { -1, 0, 1, -1, 0, 1, -1, 0, 1 };
                int[] Prewitt_Gy = new int[] { -1, -1, -1, 0, 0, 0, 1, 1, 1 };
                int[] Sobel_Gx = new int[] { -1, 0, 1, -2, 0, 2, -1, 0, 1 };
                int[] Sobel_Gy = new int[] { -1, -2, -1, 0, 0, 0, 1, 2, 1 };

                int threshold;
                if (!int.TryParse(this.textBox1.Text, out threshold))
                {
                    MessageBox.Show("請輸入有效的門檻值！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                for (int x = 1; x < Width - 1; x++)
                {
                    for (int y = 1; y < Height - 1; y++)
                    {
                        int[] pixel_mask = new int[9];
                        pixel_mask[0] = oldbitmap.GetPixel(x - 1, y - 1).G;
                        pixel_mask[1] = oldbitmap.GetPixel(x, y - 1).G;
                        pixel_mask[2] = oldbitmap.GetPixel(x + 1, y - 1).G;
                        pixel_mask[3] = oldbitmap.GetPixel(x - 1, y).G;
                        pixel_mask[4] = oldbitmap.GetPixel(x, y).G;
                        pixel_mask[5] = oldbitmap.GetPixel(x + 1, y).G;
                        pixel_mask[6] = oldbitmap.GetPixel(x - 1, y + 1).G;
                        pixel_mask[7] = oldbitmap.GetPixel(x, y + 1).G;
                        pixel_mask[8] = oldbitmap.GetPixel(x + 1, y + 1).G;

                        int Gx = 0, Gy = 0;
                        for (int i = 0; i < 9; i++)
                        {
                            Gx += pixel_mask[i] * Prewitt_Gx[i]; // 或替換為 Sobel_Gx
                            Gy += pixel_mask[i] * Prewitt_Gy[i]; // 或替換為 Sobel_Gy
                        }

                        int edgeMagnitude = (int)Math.Sqrt(Gx * Gx + Gy * Gy);
                        edgeMagnitude = Math.Min(255, Math.Max(0, edgeMagnitude));

                        if (edgeMagnitude > threshold)
                        {
                            newBitmap.SetPixel(x, y, Color.FromArgb(edgeMagnitude, edgeMagnitude, edgeMagnitude));
                        }
                        else
                        {
                            newBitmap.SetPixel(x, y, Color.Black);
                        }
                    }
                }

                this.pictureBox2.Image = newBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Bitmap oldBitmap = (Bitmap)this.pictureBox2.Image;
                Bitmap thinnedBitmap = ZhangSuenThinning(oldBitmap);
                this.pictureBox3.Image = thinnedBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }

        private Bitmap ZhangSuenThinning(Bitmap image)
        {
            int width = image.Width;
            int height = image.Height;
            bool[,] binaryImage = new bool[width, height];
            Bitmap resultImage = new Bitmap(width, height);

            // 將圖像轉為二值陣列
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = image.GetPixel(x, y);
                    binaryImage[x, y] = pixel.R > 128; // 假設像素值大於128為白，否則為黑
                }
            }

            bool pixelsChanged;
            do
            {
                pixelsChanged = false;

                // 第一階段
                List<(int, int)> pixelsToRemove = new List<(int, int)>();
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        if (ShouldRemove(binaryImage, x, y, true))
                            pixelsToRemove.Add((x, y));
                    }
                }
                foreach (var pixel in pixelsToRemove)
                {
                    binaryImage[pixel.Item1, pixel.Item2] = false;
                    pixelsChanged = true;
                }

                // 第二階段
                pixelsToRemove.Clear();
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        if (ShouldRemove(binaryImage, x, y, false))
                            pixelsToRemove.Add((x, y));
                    }
                }
                foreach (var pixel in pixelsToRemove)
                {
                    binaryImage[pixel.Item1, pixel.Item2] = false;
                    pixelsChanged = true;
                }
            } while (pixelsChanged);

            // 將二值陣列轉回圖像
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultImage.SetPixel(x, y, binaryImage[x, y] ? Color.White : Color.Black);
                }
            }

            return resultImage;
        }

        private bool ShouldRemove(bool[,] binaryImage, int x, int y, bool firstPass)
        {
            if (!binaryImage[x, y]) return false;

            // 計算鄰居 B(P1) 和 A(P1)
            int[] dx = { 0, 1, 1, 1, 0, -1, -1, -1 };
            int[] dy = { -1, -1, 0, 1, 1, 1, 0, -1 };
            int countNeighbors = 0;
            int transitionCount = 0;
            bool prev = binaryImage[x + dx[7], y + dy[7]];

            for (int i = 0; i < 8; i++)
            {
                bool current = binaryImage[x + dx[i], y + dy[i]];
                if (current) countNeighbors++;
                if (prev && !current) transitionCount++;
                prev = current;
            }

            // 條件檢查
            if (countNeighbors < 2 || countNeighbors > 6) return false;
            if (transitionCount != 1) return false;

            if (firstPass)
            {
                if (binaryImage[x, y - 1] && binaryImage[x + 1, y] && binaryImage[x, y + 1]) return false;
                if (binaryImage[x + 1, y] && binaryImage[x, y + 1] && binaryImage[x - 1, y]) return false;
            }
            else
            {
                if (binaryImage[x, y - 1] && binaryImage[x + 1, y] && binaryImage[x - 1, y]) return false;
                if (binaryImage[x, y - 1] && binaryImage[x, y + 1] && binaryImage[x - 1, y]) return false;
            }

            return true;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                if (this.pictureBox1.Image != null)
                {
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PNG圖像|*.png|JPEG圖像|*.jpg|所有文件|*.*";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        this.pictureBox1.Image.Save(saveFileDialog.FileName);
                        MessageBox.Show("圖像保存成功！", "訊息顯示");
                    }
                }
                else
                {
                    MessageBox.Show("沒有可保存的圖像！", "錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "訊息顯示");
            }
        }
    }
}
