#define DISPATCHER
//#define TASK
#define RANDOM
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        static WriteableBitmap wb;
        static Window w;
        static Image i;
        static Grid grid;
        static DispatcherTimer _timer;
        static object _obj = new object();
        static Stopwatch _sw = new Stopwatch();

        [STAThread]
        static void Main(string[] args)
        {
            i = new Image();
            RenderOptions.SetBitmapScalingMode(i, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(i, EdgeMode.Aliased);

            w = new Window();
            grid = new Grid();
            w.Content = grid;
            grid.Children.Add(i);
            w.Show();


            wb = new WriteableBitmap(
                (int)grid.ActualWidth,
                (int)grid.ActualHeight,
                96,
                96,
                PixelFormats.Bgr32,
                null);
            i.Source = wb;
            i.Stretch = Stretch.None;
            i.HorizontalAlignment = HorizontalAlignment.Left;
            i.VerticalAlignment = VerticalAlignment.Top;
            //i.MouseMove += OnMouseMove;
            //i.MouseLeftButtonDown += OnLeftButtonDown;
            //i.MouseRightButtonDown += OnRightButtonDown;

            DrawGrid(10, 15);

            var app = new Application();
            //app.Dispatcher.Hooks.OperationPosted += OnDispatcherPosted;
#if DISPATCHER
            _timer = new DispatcherTimer(new TimeSpan(1000), DispatcherPriority.Render, OnDraw, app.Dispatcher);
            _timer.Start();
#endif

#if TASK
            var task = new Task(() =>
            {
                _sw.Restart();
                for(int count = 0; count < 5000; ++count)
                    OnDraw();
                _sw.Stop();
            });

            task.Start();
#endif
            app.Run();
            Console.WriteLine(_sw.ElapsedMilliseconds);
        }

        private static void OnDispatcherPosted(object sender, DispatcherHookEventArgs e)
        {
            var pro = typeof(DispatcherOperation).GetProperty("Name", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.NonPublic);
            var name = pro.GetValue(e.Operation, null);
            Console.WriteLine(name);
        }

        static int lx = -1;
        static int ly = -1;
        static int x = -1;
        static int y = -1;
        static Random r = new Random();
        static bool positiveX = true;
        static bool positiveY = true;
        static Duration duration = new Duration(new TimeSpan(100));

#if DISPATCHER
        private static void OnDraw(object sender, EventArgs e)
#endif
#if TASK
        private static void OnDraw()
#endif
        {
            int w = (int)grid.ActualWidth;
            int h = (int)grid.ActualHeight;
#if RANDOM
            if (positiveX)
            {
                x += r.Next(30);
                if (x >= w)
                {
                    x = w - 1;
                    positiveX = !positiveX;
                }
            }
            else
            {
                x -= r.Next(30);
                if (x < 0)
                {
                    x = 0;
                    positiveX = !positiveX;
                }
            }

            if (positiveY)
            {
                y += r.Next(30);
                if (y >= h)
                {
                    y = h - 1;
                    positiveY = !positiveY;
                }
            }
            else
            {
                y -= r.Next(30);
                if (y < 0)
                {
                    y = 0;
                    positiveY = !positiveY;
                }
            }

            // line
    #if TASK
            Application.Current.Dispatcher.Invoke(() =>
            {
    #endif
                if (wb.TryLock(duration))
                {
                    if (lx != -1)
                    {
                        wb.DrawLine(lx, ly, x, y, Colors.GreenYellow);
                    }

                    //DrawGrid(10, 15);
                    wb.FillEllipseCentered(x, y, 5, 5, Colors.GreenYellow);
                    wb.DrawEllipseCentered(x, y, 6, 6, Colors.Black);

                    lx = x;
                    ly = y;

                    wb.Unlock();
                }
    #if TASK
        });
    #endif
#else
            ++x;
            if (x < w)
            {
                Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (wb.TryLock(new Duration(new TimeSpan(0, 0, 0, 0, 1))))
                        {
                            wb.DrawLine(x, 0, x, h, Colors.Green);
                            wb.AddDirtyRect(new Int32Rect(x, 0, 1, h));
                            wb.Unlock();
                        }
                    });
            }
#endif
        }

        static void DrawGrid(int traceX, int traceY)
        {
            const int margin = 5;
            double x1 = margin, y1 = margin, x2 = (grid.ActualWidth - margin), y2 = (grid.ActualHeight - margin);

            wb.DrawQuad((int)x1, (int)y1, (int)x1, (int)y2, (int)x2, (int)y2, (int)x2, (int)y1 , Colors.White);
            var xStep = (x2 - x1) / traceX;
            var yStep = (y2 - y1) / traceY;
            for(double x = x1 + xStep; x < x2; x += xStep)
            {
                wb.DrawLine((int)x, (int)y1, (int)x, (int)y2, Colors.LightGreen);
            }

            for(double y = y1 + yStep; y < y2; y += yStep)
            {
                wb.DrawLine((int)x1, (int)y, (int)x2, (int)y, Colors.Orange);
            }
        }

        private static void OnRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //ErasePixel(e);
        }

        private static void OnLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //DrawPixel(e);
            wb.Clear();
            DrawGrid(10, 15);
        }

#if false
        static void DrawPixel(MouseEventArgs e)
        {
            var pt = e.GetPosition(i);
            int col = (int)pt.X;
            int row = (int)pt.Y;

            try
            {
                wb.Lock();
                unsafe
                {
                    IntPtr ptr = wb.BackBuffer;
                    ptr += row * wb.BackBufferStride;
                    ptr += col * 4;

                    int color = 255 << 16 | 128 << 8 | 255 << 0;
                    *((int*)ptr) = color;
                }
                wb.AddDirtyRect(new Int32Rect(col, row, 1, 1));
            }
            finally
            {
                wb.Unlock();
            }
        }


        static void ErasePixel(MouseEventArgs e)
        {
            byte[] color = { 0, 0, 0, 0 };
            var pt = e.GetPosition(i);
            var rect = new Int32Rect(
                (int)pt.X,
                (int)pt.Y,
                1,
                1);
            wb.WritePixels(rect, color, 4, 0);
        }

        private static void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DrawPixel(e);
            }
            else
            {
                ErasePixel(e);
            }
        }
#endif
    }
}

