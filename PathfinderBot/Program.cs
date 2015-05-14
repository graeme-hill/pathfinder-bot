using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace PathfinderBot
{
    class Program
    {
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd, IntPtr dc);

        private static readonly Brush DEBUG_BRUSH = new SolidBrush(Color.Yellow);
        private static readonly Pen DEBUG_PEN = new Pen(Color.Yellow, 2.0f);
        private static readonly Size DEBUG_POINT_SIZE = new Size(10, 10);
        private static readonly Rectangle SCREENSHOT_AREA = new Rectangle(450, 150, 20, 340);
        private static readonly int TRAIL_DISTANCE = 30;

        private static readonly Bitmap _screenshotBitmap = new Bitmap(SCREENSHOT_AREA.Width, SCREENSHOT_AREA.Height);
        private static readonly Graphics _screenshotGraphics = Graphics.FromImage(_screenshotBitmap);

        private static volatile bool _running = true;
        private static volatile bool _autoPilotEnabled = false;

        [STAThread]
        static void Main(string[] args)
        {
            Run();
            //var task = Task.Factory.StartNew(() => Run());

            //Console.WriteLine("Press <enter> to quit");
            //Console.ReadLine();

            //_running = false;

            //task.Wait();
        }

        private static void Run()
        {
            IntPtr desktopPtr = GetDC(IntPtr.Zero);
            Graphics g = Graphics.FromHdc(desktopPtr);

            while (_running)
            {
                Update(g);
            }

            g.Dispose();
            ReleaseDC(IntPtr.Zero, desktopPtr);
        }

        private static void Update(Graphics g)
        {
            if (Keyboard.IsKeyDown(Key.RightAlt))
                _autoPilotEnabled = false;

            if (Keyboard.IsKeyDown(Key.RightCtrl))
                _autoPilotEnabled = true;

            //g.Clear(Color.Transparent);

            DrawScreenshotArea(g);
            Screenshot();

            var roadPosition = FindRoad();
            if (roadPosition != null)
            {
                g.FillRectangle(
                    DEBUG_BRUSH, 
                    new Rectangle(Translate(roadPosition.Value), DEBUG_POINT_SIZE));

                if (_autoPilotEnabled)
                {
                    var carPosition = new Point(roadPosition.Value.X - TRAIL_DISTANCE, roadPosition.Value.Y);
                    SetMousePosition(Translate(carPosition));
                }

                Console.WriteLine(":-)        ---->     " + Guid.NewGuid());
            }
            else
            {
                Console.WriteLine(Guid.NewGuid());
            }

            //var pixelColor = GetPixel(29, 11);
            //SolidBrush b = new SolidBrush(pixelColor);
            //g.FillRectangle(b, new Rectangle(0, 0, 30, 30));
        }

        private static Point? FindRoad()
        {
            var roadColor = Color.FromArgb(0x64, 0x90, 0xAD);
            var laneDividerColor = Color.FromArgb(0xCC, 0xCC, 0xCC);

            var lastRoadYIndex = -1;

            for (var y = 4; y < SCREENSHOT_AREA.Height - 4; y++)
            {
                // stop road jumping
                if (lastRoadYIndex >= 0 && y - lastRoadYIndex > 20)
                    break;

                for (var x = 0; x < SCREENSHOT_AREA.Width; x++)
                {
                    var thisPixelColor = _screenshotBitmap.GetPixel(x, y);

                    if (ColorsAreClose(thisPixelColor, roadColor, 1))
                        lastRoadYIndex = y;

                    if (ColorsAreClose(thisPixelColor, laneDividerColor, 1))
                    {
                        // found a pixel that looks like it could be center of car, so look for red
                        // pixels around it

                        var pass = false;
                        for (var y2 = y; y2 < y + 5; y2++)
                        {
                            var otherPixel = _screenshotBitmap.GetPixel(x, y2);
                            if (ColorsAreClose(otherPixel, roadColor, 1))
                            {
                                pass = true;
                                break;
                            }
                        }

                        if (pass)
                        {
                            pass = false;
                            for (var y2 = y - 5; y2 < y; y2++)
                            {
                                var otherPixel = _screenshotBitmap.GetPixel(x, y2);
                                if (ColorsAreClose(otherPixel, roadColor, 1))
                                {
                                    pass = true;
                                    break;
                                }
                            }
                        }

                        if (pass)
                        {
                            return new Point(x, y);
                        }
                    }
                }
            }

            return null;
        }

        private static void SetMousePosition(Point p)
        {
            System.Windows.Forms.Cursor.Position = p;
        }

        private static Point Translate(Point p)
        {
            return new Point(p.X + SCREENSHOT_AREA.X, p.Y + SCREENSHOT_AREA.Y);
        }

        private static void DrawScreenshotArea(Graphics g)
        {
            g.FillRectangle(DEBUG_BRUSH, new Rectangle(SCREENSHOT_AREA.X, SCREENSHOT_AREA.Y - 20, SCREENSHOT_AREA.Width, 20));
            g.FillRectangle(DEBUG_BRUSH, new Rectangle(SCREENSHOT_AREA.X, SCREENSHOT_AREA.Y + SCREENSHOT_AREA.Height, SCREENSHOT_AREA.Width, 20));
            //g.DrawRectangle(DEBUG_PEN, SCREENSHOT_AREA);
        }

        private static bool ColorsAreClose(Color a, Color b, int closiness = 10)
        {
            return Math.Abs(a.R - b.R) <= closiness
                && Math.Abs(a.G - b.G) <= closiness
                && Math.Abs(a.B - b.B) <= closiness;
        }

        static void Screenshot()
        {
            _screenshotGraphics.CopyFromScreen(
                sourceX: SCREENSHOT_AREA.X, 
                sourceY: SCREENSHOT_AREA.Y,
                destinationX: 0, 
                destinationY: 0,
                blockRegionSize: SCREENSHOT_AREA.Size, 
                copyPixelOperation: CopyPixelOperation.SourceCopy);
        }
    }
}
