using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ImageFilterApp
{
    public partial class Form1 : Form
    {
        Random random = new Random();
        const int N = 256;
        int noise_procent = 10;
        double sum_kernel;
        double Energy_signal;
        bool f1, f2, f3, f4 = false;
        double min_sko = 1;
        List<List<double>> Image;
        List<List<double>> ImageNs;
        List<List<double>> ImageNs2;
        List<List<double>> SpectrImage;
        List<List<double>> SpectrImageCentr;
        List<List<double>> SpectrImageCentrScale;
        List<List<double>> ImageRestored;
        List<List<double>> FilterImage;
        List<List<double>> ImageSpectr;
        List<List<double>> ImageResult;
        List<List<double>> kernel;
        List<List<Complex>> massPic;
        List<List<Complex>> ImageFur_image;
        List<List<Complex>> ImageFur_filter;
        List<List<Complex>> ImageFres;
        Bitmap image; //Bitmap для открываемого изображения
        Bitmap image_noise; //Bitmap для зашумленного изображения
        Bitmap image_spectr; //Bitmap для cпектра изображения
        Bitmap image_filter; //Bitmap для отфильтрованного изображения
        public Form1()
        {
            InitializeComponent();
            startInitializeElement();
        }

        void loadImage()
        {
            pictureBox1.Image = null; // При загрузке нового изображения стираем все предыдущие
            pictureBox1.Invalidate();
            pictureBox2.Image = null;
            pictureBox2.Invalidate();
            pictureBox4.Image = null;
            pictureBox4.Invalidate();
            pictureBox5.Image = null; 
            pictureBox5.Invalidate();
            textBox1.Text = "10";    //начальное значение процента зашумления в поле для ввода, чтоб что-то было
            OpenFileDialog open_dialog = new OpenFileDialog(); //создание диалогового окна для выбора файла
            open_dialog.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG|All files (*.*)|*.*"; //формат загружаемого файла
            if (open_dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    image = new Bitmap(open_dialog.FileName);
                    this.pictureBox1.Size = image.Size;
                }
                catch
                {
                    DialogResult rezult = MessageBox.Show("Невозможно открыть выбранный файл",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        void FreeMemory(Bitmap image)
        {
            int imgWidth = image.Width;
            image_noise = new Bitmap(image);
            image_spectr = new Bitmap(image);
            image_filter = new Bitmap(image);
            kernel = new List<List<double>>(imgWidth);
            Image = new List<List<double>>(imgWidth);
            ImageNs = new List<List<double>>(imgWidth);
            ImageNs2 = new List<List<double>>(imgWidth);
            SpectrImage = new List<List<double>>(imgWidth);
            SpectrImageCentr = new List<List<double>>(imgWidth);
            SpectrImageCentrScale = new List<List<double>>(imgWidth);
            ImageRestored = new List<List<double>>(imgWidth);
            ImageSpectr = new List<List<double>>(imgWidth);
            FilterImage = new List<List<double>>(imgWidth);
            ImageResult = new List<List<double>>(imgWidth);
            massPic = new List<List<Complex>>(imgWidth);
            ImageFur_image = new List<List<Complex>>(imgWidth);
            ImageFur_filter = new List<List<Complex>>(imgWidth);
            ImageFres = new List<List<Complex>>(imgWidth);
            for (int i = 0; i < imgWidth; i++)
            {
                kernel.Add(new List<double>());
                Image.Add(new List<double>());
                ImageNs.Add(new List<double>());
                ImageNs2.Add(new List<double>());
                SpectrImage.Add(new List<double>());
                SpectrImageCentr.Add(new List<double>());
                SpectrImageCentrScale.Add(new List<double>());
                ImageRestored.Add(new List<double>());
                ImageSpectr.Add(new List<double>());
                FilterImage.Add(new List<double>());
                ImageResult.Add(new List<double>());
                massPic.Add(new List<Complex>());
                ImageFur_image.Add(new List<Complex>());
                ImageFur_filter.Add(new List<Complex>());
                ImageFres.Add(new List<Complex>());
                for (int j = 0; j < imgWidth; j++)
                {
                    kernel[i].Add(0); 
                    Image[i].Add(0);
                    ImageNs[i].Add(0);
                    ImageNs2[i].Add(0);
                    SpectrImage[i].Add(0);
                    SpectrImageCentr[i].Add(0);
                    SpectrImageCentrScale[i].Add(0);
                    ImageRestored[i].Add(0);
                    ImageSpectr[i].Add(0);
                    ImageResult[i].Add(0);
                    massPic[i].Add(0);
                    ImageFur_image[i].Add(new Complex(0,0));
                    ImageFur_filter[i].Add(new Complex(0, 0));
                    ImageFres[i].Add(new Complex(0, 0));
                }
            }
        }

        double Energy2d(List<List<double>> signal)
        {
            double energy = 0;
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    energy += signal[i][j] * signal[i][j]; //вычисление энергии сигнала как E = sum(Aмплитуда^2) 
                                                            //Амплитуда(значение пикселя)
                }
            }
            return energy;
        }

        List<double> GaussRandom(int length)
        {
            List<double> Gauss_noise = new List<double>();
            for (int i = 0; i < length; i++)
            {
                double rand_i = 0;

                for (int k = 0; k < 12; k++)
                {
                    double p = random.NextDouble();
                    rand_i += ((2 * p) - 1);
                }

                double rand_norm_i = rand_i / 12;
                Gauss_noise.Add(rand_norm_i);
            }
            return Gauss_noise;
        }

        List<double> AddNoise(List<double> Signal, double noise_procent, double Energy_signal)
        {
            List<double> noise = GaussRandom(Signal.Count);
            double Energy_noise = Energy(noise);
            noise_procent = Convert.ToDouble(textBox1.Text);
            if (noise_procent > 100)
            {
                noise_procent = 100;
                textBox1.Text = "100";
            }
            if (noise_procent < 0)
            {
                noise_procent = 0;
                textBox1.Text = "0";
            }
            double norm_koeff = Math.Sqrt((Energy_signal/256) * (noise_procent / 100) / Energy_noise); //Energy_signal делим на 256, чтобы найти среднюю энергию строки изображения, а не целого
            List<double> noise_signal = new List<double>();
            for (int i = 0; i < Signal.Count; i++)
            {
                double real = Signal[i] + noise[i] * norm_koeff;
                if (real < 0)
                    real = 0;
                if (real > 255)
                    real = 255;
                noise_signal.Add(real);
            }
            return noise_signal;
        }

        double Energy(List<double> signal)
        {
            double energy = 0;
            for (int i = 0; i < N; i++)
            {
                energy += signal[i] * signal[i];
            }
            return energy;
        }

        //Функция фильтра Гаусса быстрый алгоритм, с учетом линейной сепарабельности
        List<List<double>> GaussFilter(List<List<double>> image, int N, int windowSize, double weight)
        {
            List<List<double>> GaussImage = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                GaussImage.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    GaussImage[i].Add(0);
                }
            }
            //if (windowSize % 2 == 0) // Если размер окна четный, дополняем его до нечетного
                //windowSize += 1;     // можно и не подключать
            List<double> tmpLinePixels = new List<double>();
            for (int i = 0; i < N; i++)
            {
                tmpLinePixels.Add(0);
            }
            //double sigma = windowSize / 6.0f; //весовой коэффициент для полного заполнения окна фильтра берется как windowSize / 6.0
            double sigma = weight;      //тогда все коэффициенты будут иметь ненулевое значение
            double sigma2 = 2 * sigma * sigma;
            double sum_norm;
            double new_pixel;
            int ind_near_pix;
            List<double> GaussWindow = new List<double>(windowSize);
            for (int i = 0; i < windowSize; i++)
            {
                GaussWindow.Add(0);
            }
            for (int i = windowSize / 2 + 1; i < windowSize; i++) //заполнение коэффициентами окна фильтра, в данном алгоритме это вектор а не матрица
            {                                                     //относительно центра вектора его коэффициенты одинаковы(зеркальны), поэтому...
                int k = i - windowSize / 2;                       //(-k*k)-так рассчитывается дистанция от центрального коэф. до считаемого, центральный имеет координаты(0,0)
                GaussWindow[i] = (Math.Exp(-(k * k) / sigma2));   //находим правый от центра коэффициент
                GaussWindow[i - 2 * k] = GaussWindow[i];          //такой же будет и слева от центра
            }
            GaussWindow[windowSize / 2] = 1;                      //чтобы не считать центральный, присвамваем ему 1, т.к. его координаты (0, 0)
                                                                  //подставив в формулу убеждаемся, что (e^0 = 1)

            //фильтрация по горизонтали изображения, по строкам
            for (int i = 0; i < N; i++)                           //двигаемся по строкам изображения 
            {                                                     
                for (int j = 0; j < N; j++)                       //двигаемся по столбцам изображения
                {
                    sum_norm = 0;                                 
                    new_pixel = 0;
                    for (int k = 0; k < windowSize; k++)          //двигаемся по вектору фильтра, в пояснение я объяснял, что центральный коэф. фильтра должен попадать на пиксель который мы фильтруем
                    {
                        ind_near_pix = j + k - windowSize / 2;    //проверяем
                        if (ind_near_pix >= 0 && ind_near_pix < N)//если значение отрицательное или больше размера изображения, значит данные пиксели фильтровать не нужно
                        {
                            new_pixel += image[i][ind_near_pix] * GaussWindow[k]; //иначе производим корреляцию(свертку) данного пикселя с соответствующим коэфом фильтра
                            sum_norm += GaussWindow[k];           //находим сумму коэф. фильтра задействованных для фильтрации пикселей, она нам нужна, чтобы потом найти среднее значение нового пикселя
                        }
                    }
                    new_pixel = new_pixel / sum_norm;             //находим новое значение пикселя, но пока не кладем его в изображение
                                                                  //по сути это замена нормализующего коэффициента 1/(2*pi*sigma^2) по формуле посмотрите
                    tmpLinePixels[j] = new_pixel;                 //а сохраняем во временный массив, чтобы он не влиял на фильтрацию других пикселей
                }
                for (int j = 0; j < N; j++)
                    GaussImage[i][j] = tmpLinePixels[j];          //после того, как произвели фильтрацию всей строки изображения, можно переложить значения из временного массива всех пикселей в строку нового отфильтрованного изобаржения
            }
            //фильтрация по вертикали изображения, по столбцам
            //здесь все то же самое, только двигаемся уже по столбцам
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    sum_norm = 0;
                    new_pixel = 0;
                    for (int k = 0; k < windowSize; k++)
                    {
                        ind_near_pix = j + k - windowSize / 2;
                        if (ind_near_pix >= 0 && ind_near_pix < N)
                        {
                            new_pixel += GaussImage[ind_near_pix][i] * GaussWindow[k];
                            sum_norm += GaussWindow[k];
                        }
                    }
                    new_pixel = new_pixel / sum_norm;
                    tmpLinePixels[j] = new_pixel;
                }
                for (int j = 0; j < N; j++)
                    GaussImage[j][i] = tmpLinePixels[j];
            }

            return GaussImage; //отфильтрованное изображение
        }

        void fur(List<Complex> data, int _is)
        {
            int i, j, istep, n;
            int m, mmax;
            n = data.Count;
            double r, r1, theta, w_r, w_i, temp_r, temp_i;
            double pi = 3.1415926f;

            r = pi * _is;
            j = 0;
            for (i = 0; i < n; i++)
            {
                if (i < j)
                {
                    temp_r = data[j].Real;
                    temp_i = data[j].Imaginary;
                    data[j] = data[i];
                    data[i] = temp_r + (new Complex(0, 1)) * temp_i;
                }
                m = n >> 1;
                while (j >= m) { j -= m; m = (m + 1) / 2; }
                j += m;
            }
            mmax = 1;
            while (mmax < n)
            {
                istep = mmax << 1;
                r1 = r / (double)mmax;
                for (m = 0; m < mmax; m++)
                {
                    theta = r1 * m;
                    w_r = (double)Math.Cos((double)theta);
                    w_i = (double)Math.Sin((double)theta);
                    for (i = m; i < n; i += istep)
                    {
                        j = i + mmax;
                        temp_r = w_r * data[j].Real - w_i * data[j].Imaginary;
                        temp_i = w_r * data[j].Imaginary + w_i * data[j].Real;
                        data[j] = (data[i].Real - temp_r) + (new Complex(0, 1)) * (data[i].Imaginary - temp_i);
                        data[i] += (temp_r) + (new Complex(0, 1)) * (temp_i);
                    }
                }
                mmax = istep;
            }
            if (_is > 0)
                for (i = 0; i < n; i++)
                {
                    data[i] /= (double)n;
                }
        }

        void fur_2d(List<List<double>> Image)
        {
            List<Complex> data_in_line1 = new List<Complex>();
            for (int i = 0; i < N; i++)
            {
                data_in_line1.Add(new Complex(0, 0));
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    data_in_line1[j] = Image[i][j];
                }
                fur(data_in_line1, -1);
                for (int j = 0; j < N; j++)
                {
                    massPic[i][j] = data_in_line1[j];
                }
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    data_in_line1[j] = massPic[j][i];
                }
                fur(data_in_line1, -1);
                for (int j = 0; j < N; j++)
                {
                    massPic[j][i] = data_in_line1[j];
                    SpectrImage[j][i] = Math.Sqrt(massPic[j][i].Real * massPic[j][i].Real + massPic[j][i].Imaginary * massPic[j][i].Imaginary);
                }
            }
            SpectrImage[0][0] = 0;
            SpectrImageCentr = SpectrImageTransform(SpectrImage);

            data_in_line1.Clear();
        }

        List<List<double>> SpectrImageTransform(List<List<double>> SpectrImage)
        {
            List<List<double>> SpectrImageTmp = new List<List<double>>();
            for (int i = 0; i < N / 2; i++)
            {
                SpectrImageTmp.Add(new List<double>());
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImageTmp[i].Add(0);
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImageTmp[i][j] = SpectrImage[i + N / 2][j + N / 2];
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImage[i + N / 2][j + N / 2] = SpectrImage[i][j];
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImage[i][j] = SpectrImageTmp[i][j];
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImageTmp[i][j] = SpectrImage[i + N / 2][j];
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImage[i + N / 2][j] = SpectrImage[i][j + N / 2];
                }
            }
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    SpectrImage[i][j + N / 2] = SpectrImageTmp[i][j];
                }
            }
            return SpectrImage;
        }

        List<List<double>> LinOrLogScale(List<List<double>> SpectrImageCentr)
        {
            for (int i = 0; i < N; i++)
            {
                SpectrImageCentrScale.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    SpectrImageCentrScale[i].Add(0);
                }
            }
            double Max = 0;
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    if (SpectrImageCentr[i][j] > Max)
                    {
                        Max = SpectrImageCentr[i][j];
                    }
                }
            }
            if (radioButton3.Checked)
            {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    SpectrImageCentrScale[i][j] = ((Math.Log(1 + SpectrImageCentr[i][j])) / (Math.Log(1 + Max))) * 255;
                }
            }
            }
            else
            {
                radioButton4.Checked = true;
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        SpectrImageCentrScale[i][j] = (SpectrImageCentr[i][j] / Max) * 255;
                    }
                }
            }
            return SpectrImageCentrScale;
        }

        void fur_2d_obrat(List<List<Complex>> massPic1)
        {
            List<Complex> data_in_line1 = new List<Complex>();
            for (int i = 0; i < N; i++)
            {
                data_in_line1.Add(new Complex(0, 0));
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    data_in_line1[j] = massPic1[i][j];
                }
                fur(data_in_line1, 1);
                for (int j = 0; j < N; j++)
                {
                    massPic1[i][j] = data_in_line1[j];
                }
            }

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    data_in_line1[j] = massPic1[j][i];
                }
                fur(data_in_line1, 1);
                for (int j = 0; j < N; j++)
                {
                    ImageRestored[j][i] = Math.Sqrt(data_in_line1[j].Real * data_in_line1[j].Real + data_in_line1[j].Imaginary * data_in_line1[j].Imaginary);
                }
            }
            data_in_line1.Clear();
        }

        //расчет коэффициентов окна фильтра Гаусса
        List<List<double>> makeGausskernel(int lenght, double weight)
        {
            List<List<double>> kernel = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                kernel.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    kernel[i].Add(0);
                }
            }
            int foff = N / 2 - lenght / 2;      //становимся на самый крайний индекс окна фильтра(самый дальний от центра)
            double distance = 0;                
            for (int y = foff; y < foff+lenght; y++) //двигаемся по строкам
            {
                for (int x = foff; x < foff+lenght; x++) // и столбцам окна фильтра, так будет для всех отсальных рассматриваемых фильтров
                {
                    distance = (((y-N/2) * (y - N / 2)) + ((x-N/2) * (x-N/2))) / (2 * weight * weight); //расстояние от центрального коэф. фильтра до искомого (по теореме Пифагора, если это гипотенуза треугольника)
                    kernel[y][x] = Math.Exp(-distance); //формула Гаусса для расчета коэф. фильтра
                }
            }
            return kernel;
        }

        //Алгоритм пространственной фильтрации
        List<List<double>> ProstrFilter(List<List<double>> sourceImage, List<List<double>> kernel_filt, int lenght)
        {
            int shift = N / 2 - lenght / 2; //смещение по окну фильтра, оно нам еще пригодится
            int ind_near_pix_row, ind_near_pix_col;
            double newpix, sum_norm;
            int windowSize = lenght;
            List<List<double>> temppixels = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                temppixels.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    temppixels[i].Add(0);
                }
            }
            for (int y = 0; y < N; y++)     //двигаемся по строкам
            {
                for (int x = 0; x < N; x++) //и по столбцам изображения
                {
                    newpix = 0;
                    sum_norm = 0;
                    for (int i = shift; i < windowSize + shift; i++)   //двигаемся по строкам
                    {
                        ind_near_pix_row = y + (i - shift) - windowSize / 2; //двигаемся по матрице фильтра, в пояснение я объяснял, что центральный коэф. фильтра должен попадать на пиксель который мы фильтруем 
                        for (int j = shift; j < windowSize + shift; j++) //и по столбцам фильтра
                        {                                                //shift-ом смещаемся к первому коэффициенту окна фильтра
                            ind_near_pix_col = x + (j - shift) - windowSize / 2; //двигаемся по матрице фильтра, в пояснение я объяснял, что центральный коэф. фильтра должен попадать на пиксель который мы фильтруем
                            if ((ind_near_pix_row >= 0 && ind_near_pix_row < N) && (ind_near_pix_col >= 0 && ind_near_pix_col < N))  //если эти значения одновременно отрицательны или больше размера изображения, значит данные пиксели фильтровать не нужно
                            {
                                {
                                    newpix += sourceImage[ind_near_pix_row][ind_near_pix_col] * kernel_filt[i][j]; //иначе производим корреляцию(свертку) данного пикселя с соответствующим коэфом фильтра
                                    sum_norm += kernel_filt[i][j]; //находим сумму коэф. фильтра задействованных для фильтрации пикселей, она нам нужна, чтобы потом найти среднее значение нового пикселя
                                }
                            }
                        }
                    }
                    newpix = newpix / sum_norm; //находим новое значение пикселя, но пока не кладем его в изображение
                    temppixels[y][x] = newpix;  //а сохраняем во временную матрицу
                   
                }
            }
            for (int y = 0; y < N; y++)
            {
                for (int x = 0; x < N; x++)
                {
                    sourceImage[y][x] = temppixels[y][x]; //заносим все временный пиксели в новое изображение
                }
            }
            return sourceImage;
        }

        //расчет коэффициентов окна фильтра Квадрат(он же идеальный ФНЧ)
        List<List<double>> makeSquarekernel(int lenght)
        {
            List<List<double>> kernel = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                kernel.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    kernel[i].Add(0);
                }
            }
            double distance;
            int foff = N / 2 - lenght / 2;
            for (int y = foff; y < foff + lenght; y++)
            {
                for (int x = foff; x < foff + lenght; x++)
                {
                    distance = Math.Sqrt((x - N/2)* (x - N / 2) + (y - N / 2) * (y - N / 2)); //до сюда вся инфа в функции коэф. для фильтра Гаусса
                    if(distance <= lenght / 2)  //если расстояние до коэф. окна фильтра меньше заданного радиуса фильтра
                        kernel[y][x] = 1;       //присваиваем 1
                    else
                        kernel[y][x] = 0;       //иначе 0
                }                               //по сути у нас получается фильтр, который в пространтсве является цилиндром, а квадрат он, потому что получается двумерного ФНЧ прокурткой в 3d
            }                                   //так же находятся и все остальные передаточные характеристики фильтров в пространстве
                                                //треугольник в прокрутке даст конус, косинус -объемный косинус и т.д.
            return kernel;      
        }

        //расчет коэффициентов окна фильтра Баттерворта
        List<List<double>> makeButterworthkernel(int lenght, int n)
        {
            List<List<double>> kernel = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                kernel.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    kernel[i].Add(0);
                }
            }
            int foff = N / 2 - lenght / 2;
            double distance = 0;
            for (int y = foff; y < foff + lenght; y++)
            {
                for (int x = foff; x < foff + lenght; x++)
                {
                    distance = Math.Sqrt((x - N / 2) * (x - N / 2) + (y - N / 2) * (y - N / 2));  //до сюда вся инфа в функции коэф. для фильтра Гаусса
                    kernel[y][x] = 1/(Math.Sqrt(1 + Math.Pow((2*distance/lenght), 2*n))); //коэффицент фильтра Баттерворта, заметьте, что при больших значения n и расстояния большего радиусу фильтра - знаменатель быстро стремится к бесконечности
                                                                                          //т.е. коэффициент фильтра будет стремится к нулю, и наоборот при расстоянии от центра окна фильтра меньшего радиуса фильтра, выражение в знаменателе будет стремится к 0, но + 1 = 1
                                                                                          //соответственно коэффициент фильтра будет стремится к 1 и в итоге при больших n - мы получаем квадратный фильтр (это один из его частных случаев)
                                                                                          //кстати это так же практичсеки частный случай треугольникам (конуса в 3d)
                }
            }
            return kernel;
        }

        //расчет коэффициентов окна фильтра приподнятого косинуса
        List<List<double>> makePripodcos(int lenght, double weight)
        {
            List<List<double>> kernel = new List<List<double>>();
            for (int i = 0; i < N; i++)
            {
                kernel.Add(new List<double>());
                for (int j = 0; j < N; j++)
                {
                    kernel[i].Add(0);
                }
            }
            int foff = N / 2 - lenght / 2;
            double distance = 0;
            for (int y = foff; y < foff + lenght; y++)
            {
                for (int x = foff; x < foff + lenght; x++)
                {
                    distance = Math.Sqrt((x - N / 2) * (x - N / 2) + (y - N / 2) * (y - N / 2)); //до сюда вся инфа в функции коэф. для фильтра Гаусса
                    if (distance > lenght / 2) //если расстояние от центра окна фильтра больше, чем радиус окна
                        kernel[y][x] = 0;      //коэф = 0
                    if (distance <= ((1 - weight) * lenght / 4)) // для понимание этих условий, в википедии найдите фильтр "приподнятый косинус"
                                                                 //там будет его математическое описание, f - это distanse, T = 2/lenght
                        kernel[y][x] = 1;
                    else if (distance <= ((1 + weight) * lenght / 4) && distance > ((1 - weight) * lenght / 4))
                        kernel[y][x] = 0.5 * (1 + Math.Cos(2 * Math.PI / lenght / weight * (distance - ((1 - weight) * lenght / 4))));
                    else
                        kernel[y][x] = 0;
                }
            }
            return kernel;
        }

        double Eps(List<List<double>> Image1, List<List<double>> Image2, int N)
        {
            double res = 0;
            double sum1 = 0;
            double sum2 = 0;
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    sum1 += (double)((Image1[i][j] - Image2[i][j]) * (Image1[i][j] - Image2[i][j]));
                    sum2 += (double)(Image1[i][j] * Image1[i][j]);
                }
            }
            res = Math.Sqrt(sum1 / sum2);
            return res;
        }


        //черно-белое изображение
        void blackWhiteImage(Bitmap image)
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    Image[i][j] = (double)(image.GetPixel(i, j).R * 0.299f + image.GetPixel(i, j).G * 0.587f + image.GetPixel(i, j).B * 0.114f);
                    image.SetPixel(i, j, Color.FromArgb(255, (int)Image[i][j], (int)Image[i][j], (int)Image[i][j]));
                }
            }
            pictureBox1.Image = image; //заносим изображение в окно программы
            pictureBox1.Invalidate();
        }

        void noiseImage()
        {
            for (int i = 0; i < N; i++)
            {
                ImageNs[i] = AddNoise(Image[i], noise_procent, Energy_signal);
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    image_noise.SetPixel(i, j, Color.FromArgb(255, (int)ImageNs[i][j], (int)ImageNs[i][j], (int)ImageNs[i][j]));
                }
            }
            pictureBox2.Image = image_noise;
            pictureBox2.Invalidate();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int filterWindowSize = Convert.ToInt32(textBox2.Text);
            double weight;
            int n;
            double sko = 0;
            sum_kernel = 0;
            if (filterWindowSize < 3) //проверки на ввод пользователя
            {
                filterWindowSize = 3;
                textBox2.Text = "3";
            }
            if (filterWindowSize > 64)
            {
                filterWindowSize = 64;
                textBox2.Text = "64";
            }
            if (radioButton1.Checked) //частотная фильтрация
            {
                if (comboBox1.SelectedIndex == 0) //какой фильтр выбрали из списка
                {
                    FilterImage = makeSquarekernel(filterWindowSize); //получаем окно фильтра размером 256 на 256
                    multFurSpectr(FilterImage);                       //произведение спектра изображения на спектр фильтра
                    fur_2d_obrat(ImageFres);                          //обратное преобразование Фурье (отфильтрованное изображение)
                    drawImage();                                      //перерисовываем полученное изображение
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if(comboBox1.SelectedIndex == 1)
                {
                    weight = Convert.ToDouble(textBox3.Text); //проверки на ввод пользователя
                    if (weight <= 0)
                    {
                        textBox3.Text = "1";
                        weight = Convert.ToDouble(textBox3.Text);
                    }
                    FilterImage = makeGausskernel(filterWindowSize, weight);
                    multFurSpectr(FilterImage);
                    fur_2d_obrat(ImageFres);
                    drawImage();
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if(comboBox1.SelectedIndex == 2)
                {
                    n = Convert.ToInt32(textBox3.Text); //проверки на ввод пользователя
                    if (n < 1)
                    {
                        textBox3.Text = "1";
                        n = Convert.ToInt32(textBox3.Text);
                    }
                    FilterImage = makeButterworthkernel(filterWindowSize, n);
                    multFurSpectr(FilterImage);
                    fur_2d_obrat(ImageFres);
                    drawImage();
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if(comboBox1.SelectedIndex == 3)
                {
                    weight = Convert.ToDouble(textBox3.Text); //проверки на ввод пользователя
                    if (weight < 0 || weight > 1)
                    {
                        textBox3.Text = "1";
                        weight = Convert.ToDouble(textBox3.Text);
                    }
                    FilterImage = makePripodcos(filterWindowSize, weight);
                    multFurSpectr(FilterImage);
                    fur_2d_obrat(ImageFres);
                    drawImage();
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                sko = Math.Round(Eps(Image, ImageResult, 256), 5);
                textBox5.Text = Convert.ToString(sko);
            }
            if(radioButton2.Checked) //пространственная фильтрация
            {
                for (int i = 0; i < N; i++)
                {
                    for (int j = 0; j < N; j++)
                    {
                        ImageNs2[i][j] = ImageNs[i][j]; //переносим изображение шума в дргую матрицу, чтобы не потерять значения
                    }
                }
                if (comboBox1.SelectedIndex == 0) //выбор фильтра из выпадающего списка
                {
                    FilterImage = makeSquarekernel(filterWindowSize); //получаем окно фильтра размером 256 на 256
                    FilterImage = ProstrFilter(ImageNs2, FilterImage, filterWindowSize); //выполняем пространственную фильтрацию 
                    for (int i = 0; i < N; i++) //рисуем полученное изображение
                    {
                        for (int j = 0; j < N; j++)
                        {
                            image_filter.SetPixel(i, j, Color.FromArgb(255, (int)FilterImage[i][j], (int)FilterImage[i][j], (int)FilterImage[i][j]));
                        }
                    }
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if (comboBox1.SelectedIndex == 1)
                {
                    weight = Convert.ToDouble(textBox3.Text); //проверки на ввод пользователя
                    if (weight <= 0)
                    {
                        textBox3.Text = "1";
                        weight = Convert.ToDouble(textBox3.Text);
                    }
                    //FilterImage = makeGausskernel(filterWindowSize, weight); //медленный алгоритм пространственной фильтрации для Гаусса
                    //FilterImage = ProstrFilter(ImageNs2, FilterImage, filterWindowSize);
                    FilterImage = GaussFilter(ImageNs2, 256, filterWindowSize, weight); //быстрый алгоритм пространственной фильтрации для Гаусса
                    for (int i = 0; i < N; i++)
                    {
                        for (int j = 0; j < N; j++)
                        {
                            image_filter.SetPixel(i, j, Color.FromArgb(255, (int)FilterImage[i][j], (int)FilterImage[i][j], (int)FilterImage[i][j]));
                        }
                    }
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if (comboBox1.SelectedIndex == 2)
                {
                    n = Convert.ToInt32(textBox3.Text); //проверки на ввод пользователя
                    if (n < 1)
                    {
                        textBox3.Text = "1";
                        n = Convert.ToInt32(textBox3.Text);
                    }
                    FilterImage = makeButterworthkernel(filterWindowSize, n);
                    FilterImage = ProstrFilter(ImageNs2, FilterImage, filterWindowSize);
                    for (int i = 0; i < N; i++)
                    {
                        for (int j = 0; j < N; j++)
                        {
                            image_filter.SetPixel(i, j, Color.FromArgb(255, (int)FilterImage[i][j], (int)FilterImage[i][j], (int)FilterImage[i][j]));
                        }
                    }
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                else if (comboBox1.SelectedIndex == 3)
                {
                    weight = Convert.ToDouble(textBox3.Text); //проверки на ввод пользователя
                    if (weight < 0 || weight > 1)
                    {
                        textBox3.Text = "1";
                        weight = Convert.ToDouble(textBox3.Text);
                    }
                    FilterImage = makePripodcos(filterWindowSize, weight);
                    FilterImage = ProstrFilter(ImageNs2, FilterImage, filterWindowSize);
                    for (int i = 0; i < N; i++)
                    {
                        for (int j = 0; j < N; j++)
                        {
                            image_filter.SetPixel(i, j, Color.FromArgb(255, (int)FilterImage[i][j], (int)FilterImage[i][j], (int)FilterImage[i][j]));
                        }
                    }
                    pictureBox5.Image = image_filter;
                    pictureBox5.Invalidate();
                }
                sko = Math.Round(Eps(Image, FilterImage, 256), 5); //находим СКО между исходным и отфильтрованным изображениями
                textBox5.Text = Convert.ToString(sko);  
            }
            if (comboBox1.SelectedIndex == 0) //рисуем на графике точку ошибки
            {
                chart1.Series[0].Points.AddXY(filterWindowSize, sko);
                if(f1)
                    chart1.Series[0].ChartType = SeriesChartType.Line; //если точки > 1, рисуем их линией
                f1 = true;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                chart1.Series[1].Points.AddXY(filterWindowSize, sko);
                if(f2)
                    chart1.Series[1].ChartType = SeriesChartType.Line;
                f2 = true;
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                chart1.Series[2].Points.AddXY(filterWindowSize, sko);
                if(f3)
                    chart1.Series[2].ChartType = SeriesChartType.Line;
                f3 = true;
            }
            else if (comboBox1.SelectedIndex == 3)
            {
                chart1.Series[3].Points.AddXY(filterWindowSize, sko);
                if(f4)
                    chart1.Series[3].ChartType = SeriesChartType.Line;
                f4 = true;
            }
            if(sko < min_sko) //если текущее СКО меньше предыдущего
            {
                min_sko = sko; //запоминаем его
                textBox4.Text = (string)comboBox1.SelectedItem;  //и отображаем в окне программы СКО какого это фильтра(критерий наилучшей фильтрации)
            }
        }

        //кнопка загрузить изображение
        private void button1_Click(object sender, EventArgs e)
        {
            button2.Enabled = true; 
            loadImage();
            FreeMemory(image);
            blackWhiteImage(image);
        }

        //кнопка зашумить изображение
        private void button2_Click(object sender, EventArgs e)
        {
            button4.Enabled = true;
            Energy_signal = Energy2d(Image);
            noiseImage();
            drawSpectrImage();
        }

        //меняем параметры фильтров, в зависимости от выбранного
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0)
            {
                label9.Visible = false;
                textBox3.Visible = false;
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                label9.Visible = true;
                textBox3.Visible = true;
                label9.Text = "Коэф. сглаживания";
            }
            else if (comboBox1.SelectedIndex == 2)
            {
                label9.Visible = true;
                textBox3.Visible = true;
                label9.Text = "Порядок фильтра";
            }
            else if (comboBox1.SelectedIndex == 3)
            {
                label9.Visible = true;
                textBox3.Visible = true;
                label9.Text = "Коэф. сглаживания";
            }
        }

        void multFurSpectr(List<List<double>> ImageFilter)
        {
            fur_2d(ImageNs);    //находим спектр зашумленного изображения
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    ImageFur_image[j][i] = massPic[j][i];   //заносим его спектр(дествительную и мнимую часть комплексных чисел) в отдельныую матрицу
                }
            }
            for (int i = 0; i < N; i++)    
            {
                for (int j = 0; j < N; j++)
                {
                    sum_kernel += ImageFilter[i][j];  //находим сумму коэффициентов окна фильтра, чтобы нормировать выходные значения, как для пространтсвенной фильтрации
                }
            }
            fur_2d(ImageFilter);    //находим спектр зашумленного изображения
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    ImageFur_filter[j][i] = massPic[j][i];      //заносим его спектр(дествительную и мнимую часть комплексных чисел) в отдельныую матрицу
                }
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    ImageFres[i][j] = Complex.Multiply(ImageFur_image[i][j], ImageFur_filter[i][j]); //комплексно перемножаем спектр фильтра со спектром зашумленного изображения
                }
            }
        }

        void drawImage()
        {   
            //после обратного преобразование Фурье необходимо перерисовать изображение, потом сброшу скрин чтоб стало понятно
            for (int i = 0; i < N / 2; i++)
            {
                for (int j = 0; j < N / 2; j++)
                {
                    double tmp1 = ImageRestored[i][j];
                    ImageRestored[i][j] = ImageRestored[i + N / 2][j + N / 2];
                    ImageRestored[i + N / 2][j + N / 2] = tmp1; //на месте 1-ой четверти изображения рисуем 4-ую и наоборот
                    double tmp2 = ImageRestored[i][j + N / 2];
                    ImageRestored[i][j + N / 2] = ImageRestored[i + N / 2][j];
                    ImageRestored[i + N / 2][j] = tmp2; //на месте 2-ой четверти изображения рисуем 3-ую
                }
            }
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    ImageResult[i][j] = ImageRestored[i][j] / sum_kernel; //нормировка(усреднение) полученного изображения
                    image_filter.SetPixel(i, j, Color.FromArgb(255, (int)ImageResult[i][j], (int)ImageResult[i][j], (int)ImageResult[i][j]));
                }
            }
        }

        //отрисовка спектра зашумленного изображения
        void drawSpectrImage()
        {
            fur_2d(ImageNs);
            ImageSpectr = LinOrLogScale(SpectrImageCentr); //выбор типа масштабирования(линейный или логарифмический)
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    image_spectr.SetPixel(i, j, Color.FromArgb(255, (int)ImageSpectr[i][j], (int)ImageSpectr[i][j], (int)ImageSpectr[i][j]));
                }
            }
            pictureBox4.Image = image_spectr;
            pictureBox4.Invalidate();
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            drawSpectrImage();
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            drawSpectrImage();
        }

        void startInitializeElement()
        {
            chart1.Series[0].LegendText = "CКО square"; //названия графиков
            chart1.Series[1].LegendText = "CKO Gauss";
            chart1.Series[2].LegendText = "CKO Butterworth";
            chart1.Series[3].LegendText = "CKO сosinus";
            chart1.ChartAreas[0].AxisX.Title = "Размер окна фильтра";
            chart1.ChartAreas[0].AxisY.Title = "CКО"; 
            chart1.ChartAreas[0].AxisX.Minimum = 0; // настройка минимума и максимума оси X
            chart1.ChartAreas[0].AxisX.Maximum = 70;
            chart1.ChartAreas[0].AxisY.Minimum = 0; // настройка минимума и максимума оси Y
            chart1.ChartAreas[0].AxisY.Maximum = 0.1;
            chart1.ChartAreas[0].AxisY.MajorGrid.Interval = 0.005;
            for (int i = 0; i < 4; i++)
            {
                chart1.Series[i].BorderWidth = 3; // толщина линии графика
            }
            button2.Enabled = false; 
            button4.Enabled = false;
            label9.Visible = false;
            textBox3.Visible = false;
            radioButton1.Checked = true;
            comboBox1.Items.Add("Square");
            comboBox1.Items.Add("Gauss");
            comboBox1.Items.Add("Butterworth");
            comboBox1.Items.Add("Cosinus");
            comboBox1.SelectedItem = comboBox1.Items[0];
            textBox2.Text = "3";
            textBox3.Text = "1";
        }
    }
}
