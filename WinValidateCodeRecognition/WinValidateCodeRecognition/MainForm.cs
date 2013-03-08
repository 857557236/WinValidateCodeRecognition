﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WinValidateCodeRecognition
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        static string imgName = string.Empty;//目标文件名称，不包含拓展名
        static string imgTemplatePath = string.Empty;//图片模板完全路径
        static string imgSavePath = @"G:\";//生成效果图保存路径
        private void button1_Click(object sender, EventArgs e)
        {
            Image image = Image.FromFile(txtTargetPath.Text);
            Image img = (Image)image.Clone();
            Bitmap bmp = new Bitmap((Image)image.Clone());
            int gray = 0;
            Graphics g = Graphics.FromImage(image);
            int sum = 0;
            int[] zf = new int[256];//灰度数组

            #region 灰度平均值
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    //灰度算法
                    gray = (bmp.GetPixel(x, y).R * 299 + bmp.GetPixel(x, y).G * 587 + bmp.GetPixel(x, y).B * 114 + 500) / 1000;
                    zf[gray]++;
                    sum += gray;
                }
            }
            int avg = sum / (bmp.Width * bmp.Height);
            #endregion

            #region 以获得的灰度平均值为阀值，对图像进行二值化处理
            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    gray = (bmp.GetPixel(x, y).R * 299 + bmp.GetPixel(x, y).G * 587 + bmp.GetPixel(x, y).B * 114 + 500) / 1000;
                    zf[gray]++;
                    sum += gray;
                    Color color = new Color();
                    if (gray > avg)
                    {
                        color = Color.FromArgb(255, 255, 255);
                    }
                    else
                    {
                        color = Color.FromArgb(0, 0, 0);
                    }
                    g.DrawLine(new Pen(color, 1), x, y, x + 1, y + 1);
                }
            }
            #endregion

            #region 直方图绘制
            //Graphics gg = Graphics.FromImage(img);
            ////string k = ((int)(bmp.Height * 0.5) / zf.Max()).ToString();
            //for (int i = 0; i < zf.Length; i++)
            //{
            //    Pen p = new Pen(Color.Red, 1);
            //    gg.DrawLine(p, i, 0, i, zf[i]);
            //}
            #endregion


            image.Save(imgSavePath + imgName + @"_二值化图.jpg");
            image.Dispose();
            g.Dispose();
            //img.Save(@"F:\validateCodeImg\" + imgName + @"_直方图.jpg");
            //gg.Dispose();
            MessageBox.Show("OK!\nGray_AVG:" + avg);//灰度平均值
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int redress = Convert.ToInt32(txtRedress.Text);
            picBoxPrev.ImageLocation = ImageMainMatch(redress);
        }

        #region 图像主体识别
        /// <summary>
        /// 
        /// </summary>
        /// <param name="redress"></param>
        /// <returns></returns>
        private static string ImageMainMatch(int redress)
        {
            string imgNameTmp = imgName;
            Image imgTg = Image.FromFile(imgSavePath + imgNameTmp + @".jpg");
            Image imgMT = Image.FromFile(imgTemplatePath);
            Bitmap bmpTg = new Bitmap(imgTg);
            Bitmap bmpMT = new Bitmap(imgMT);

            Point[] p = GetTargetPoints(bmpTg, bmpMT, redress);
            Image imgTmp = (Image)imgTg.Clone();
            Graphics g = Graphics.FromImage(imgTmp);
            Pen pen = new Pen(Color.Red, 1);
            g.DrawLine(pen, p[0], p[1]);
            g.DrawLine(pen, p[1], p[2]);
            g.DrawLine(pen, p[2], p[3]);
            g.DrawLine(pen, p[3], p[0]);

            string savePath = imgSavePath + imgNameTmp + @"_主体匹配.jpg";
            try
            {
                imgTmp.Save(savePath);
            }
            catch (Exception)
            {
                ;
            }
            finally
            {
                g.Dispose();
                imgTg.Dispose();
                imgMT.Dispose();
                imgTmp.Dispose();
            }

            return savePath;
        }
        #endregion

        #region 获取主体矩形边界坐标
        /// <summary>
        /// 获取主体矩形边界坐标，顺时针
        /// </summary>
        /// <param name="bmpTg">目标图片</param>
        /// <param name="bmpMT">模板图片</param>
        /// <param name="redress">容错率(0-100)</param>
        /// <returns></returns>
        private static Point[] GetTargetPoints(Bitmap bmpTg, Bitmap bmpMT, int redress)
        {
            //主体矩形边界坐标,顺时针
            Point[] points ={
                 new Point(0,0),
                 new Point(0,0),
                 new Point(0,0),
                 new Point(0,0),
                            };

            int mtMainPxSum = 0;//模板主体像素数
            int matchPxNum = 0;//目标图片与模板主体匹配成功的像素数

            int tmpRGB = 0;
            int mtxDiff = 0;//模板图片主体x起始坐标距离0的距离
            int mtyDiff = 0;//模板图片主体y起始坐标距离0的距离
            int mtMainW = 0;//模板图片主体宽
            int mtMainH = 0;//模板图片主体高
            List<int> mtxs = new List<int>();//模板图片x坐标集合
            List<int> mtys = new List<int>();//模板图片y坐标集合

            for (int x = 0; x < bmpMT.Width; x++)
            {
                for (int y = 0; y < bmpMT.Height; y++)
                {
                    if (bmpMT.GetPixel(x, y).B < 150)
                    {
                        mtxs.Add(x);
                        mtys.Add(y);
                    }
                }
            }

            mtxDiff = mtxs.Min() - 0;
            mtyDiff = mtys.Min() - 0;
            mtMainW = mtxs.Max() - mtxs.Min() + 1;
            mtMainH = mtys.Max() - mtys.Min() + 1;

            for (int tgx = 0; tgx < bmpTg.Width - mtMainW; tgx++)
            {
                for (int tgy = 0; tgy < bmpTg.Height - mtMainH; tgy++)
                {
                    mtMainPxSum = 0;
                    matchPxNum = 0;
                    for (int mtx = 0; mtx < mtMainW; mtx++)
                    {
                        for (int mty = 0; mty < mtMainH; mty++)
                        {
                            if (bmpMT.GetPixel(mtx + mtxDiff, mty + mtyDiff).B > 100)
                                continue;
                            mtMainPxSum++;//统计模板主体像素数
                            tmpRGB = bmpTg.GetPixel(tgx + mtx, tgy + mty).B - bmpMT.GetPixel(mtx + mtxDiff, mty + mtyDiff).B;
                            tmpRGB = tmpRGB > 0 ? tmpRGB : tmpRGB * (-1);
                            if (tmpRGB < 150)//如果匹配
                            {
                                matchPxNum++;//统计匹配的像素数
                            }
                        }
                    }
                    //模板匹配结束一轮后
                    int accuracy = (int)((matchPxNum * 1.0f / mtMainPxSum) * 100);//正确率
                    if (accuracy + redress < 100)//如果正确率加上容错率没有大于百分百，说明没有匹配上，进行下一轮匹配
                    {
                        continue;
                    }
                    points[0].X = tgx;
                    points[0].Y = tgy;
                    points[1].X = tgx + mtMainW;
                    points[1].Y = tgy;
                    points[2].X = tgx + mtMainW;
                    points[2].Y = tgy + mtMainH;
                    points[3].X = tgx;
                    points[3].Y = tgy + mtMainH;
                    return points;
                }
            }
            return points;
        }
        #endregion

        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtTargetPath.Text = ofd.FileName;
                imgName = ofd.SafeFileName.Substring(0, ofd.SafeFileName.LastIndexOf('.'));
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtTemplatePath.Text = ofd.FileName;
                imgTemplatePath = txtTemplatePath.Text;
                picBoxTemplate.ImageLocation = imgTemplatePath;
            }
        }

        private void txtRedress_TextChanged(object sender, EventArgs e)
        {
            trackBarRedress.Value = Convert.ToInt32(txtRedress.Text);
        }

        private void trackBarRedress_ValueChanged(object sender, EventArgs e)
        {
            txtRedress.Text = trackBarRedress.Value.ToString();
            int redress = Convert.ToInt32(txtRedress.Text);
            picBoxPrev.ImageLocation = ImageMainMatch(redress);
        }

    }
}
