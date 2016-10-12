using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using UltimateFishBot.Classes.Helpers;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Diagnostics;
using System.IO;

namespace UltimateFishBot.Classes.BodyParts
{
    class Eyes
    {
        private Manager m_manager;
        private BackgroundWorker m_backgroundWorker;
        int xPosMin;
        int xPosMax;
        int yPosMin;
        int yPosMax;
        float dpi = 1;
        Rectangle wowRectangle;
        private Win32.CursorInfo m_noFishCursor;

        public Eyes(Manager manager)
        {
            m_manager = manager;

            m_backgroundWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
            m_backgroundWorker.DoWork += new DoWorkEventHandler(EyeProcess_DoWork);
            m_backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(EyeProcess_RunWorkerCompleted);
        }

        public void StartLooking()
        {
            if (m_backgroundWorker.IsBusy)
                return;

            m_manager.SetActualState(Manager.FishingState.SearchingForBobber);
            m_backgroundWorker.RunWorkerAsync();
        }

        private void EyeProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            m_noFishCursor = Win32.GetNoFishCursor();
            wowRectangle = Win32.GetWowRectangle();
            //Console.Out.WriteLine(string.Format("wowrectangle {0} , {1} , {2} , {3}"),wowRectangle.X.ToString(),wowRectangle.Y.ToString(),wowRectangle.Height.ToString(),wowRectangle.Width.ToString());

            if (!Properties.Settings.Default.customScanArea)
            {
                xPosMin = wowRectangle.Width / 4;
                xPosMax = xPosMin * 3;
                yPosMin = wowRectangle.Height / 4;
                yPosMax = yPosMin * 3;
                Console.Out.WriteLine("Using default area");
            }
            else
            {
                xPosMin = Properties.Settings.Default.minScanXY.X;
                yPosMin = Properties.Settings.Default.minScanXY.Y;
                xPosMax = Properties.Settings.Default.maxScanXY.X;
                yPosMax = Properties.Settings.Default.maxScanXY.Y;
                Console.Out.WriteLine("Using custom area");
            }
            Console.Out.WriteLine(string.Format("Scanning area: {0} , {1} , {2} , {3} , ", xPosMin, yPosMin, xPosMax, yPosMax));
            if (Properties.Settings.Default.ImageSearch)
            {
                dpi = Win32.GetCurrentDPI();
                GetBobber();
            }
            else
            {
                if (Properties.Settings.Default.AlternativeRoute)
                    LookForBobber_Spiral();
                else
                    LookForBobber();
            }
        }

        private void EyeProcess_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // If not found, exception is sent...
                m_manager.SetActualState(Manager.FishingState.Idle);
                return;
            }

            //... if no exception, we found a fish !
            m_manager.SetActualState(Manager.FishingState.WaitingForFish);
        }

        private void LookForBobber()
        {

            int XPOSSTEP = (int)((xPosMax - xPosMin) / Properties.Settings.Default.ScanningSteps);
            int YPOSSTEP = (int)((yPosMax - yPosMin) / Properties.Settings.Default.ScanningSteps);
            int XOFFSET = (int)(XPOSSTEP / Properties.Settings.Default.ScanningRetries);

            if (Properties.Settings.Default.customScanArea)
            {
                for (int tryCount = 0; tryCount < Properties.Settings.Default.ScanningRetries; ++tryCount)
                {
                    for (int x = (int)(xPosMin + (XOFFSET * tryCount)); x < xPosMax; x += XPOSSTEP)
                    {
                        for (int y = yPosMin; y < yPosMax; y += YPOSSTEP)
                        {
                            if (MoveMouseAndCheckCursor(x, y))
                                return;
                        }
                    }
                }
            }
            else
            {
                for (int tryCount = 0; tryCount < Properties.Settings.Default.ScanningRetries; ++tryCount)
                {
                    for (int x = (int)(xPosMin + (XOFFSET * tryCount)); x < xPosMax; x += XPOSSTEP)
                    {
                        for (int y = yPosMin; y < yPosMax; y += YPOSSTEP)
                        {
                            if (MoveMouseAndCheckCursor(wowRectangle.X + x, wowRectangle.Y + y))
                                return;
                        }
                    }
                }
            }

            throw new Exception("Fish not found"); // Will be catch in Manager:EyeProcess_RunWorkerCompleted
        }

        private void LookForBobber_Spiral()
        {

            int XPOSSTEP = (int)((xPosMax - xPosMin) / Properties.Settings.Default.ScanningSteps);
            int YPOSSTEP = (int)((yPosMax - yPosMin) / Properties.Settings.Default.ScanningSteps);
            int XOFFSET = (int)(XPOSSTEP / Properties.Settings.Default.ScanningRetries);
            int YOFFSET = (int)(YPOSSTEP / Properties.Settings.Default.ScanningRetries);

            if (Properties.Settings.Default.customScanArea)
            {
                for (int tryCount = 0; tryCount < Properties.Settings.Default.ScanningRetries; ++tryCount)
                {
                    int x = (int)((xPosMin + xPosMax) / 2) + XOFFSET * tryCount;
                    int y = (int)((yPosMin + yPosMax) / 2) + YOFFSET * tryCount;

                    for (int i = 0; i <= 2 * Properties.Settings.Default.ScanningSteps; i++)
                    {
                        for (int j = 0; j <= (i / 2); j++)
                        {
                            int dx = 0, dy = 0;

                            if (i % 2 == 0)
                            {
                                if ((i / 2) % 2 == 0)
                                {
                                    dx = XPOSSTEP;
                                    dy = 0;
                                }
                                else
                                {
                                    dx = -XPOSSTEP;
                                    dy = 0;
                                }
                            }
                            else
                            {
                                if ((i / 2) % 2 == 0)
                                {
                                    dx = 0;
                                    dy = YPOSSTEP;
                                }
                                else
                                {
                                    dx = 0;
                                    dy = -YPOSSTEP;
                                }
                            }

                            x += dx;
                            y += dy;

                            if (MoveMouseAndCheckCursor(x, y))
                                return;
                        }
                    }
                }
            }
            else
            {
                for (int tryCount = 0; tryCount < Properties.Settings.Default.ScanningRetries; ++tryCount)
                {
                    int x = (int)((xPosMin + xPosMax) / 2) + XOFFSET * tryCount;
                    int y = (int)((yPosMin + yPosMax) / 2) + YOFFSET * tryCount;

                    for (int i = 0; i <= 2 * Properties.Settings.Default.ScanningSteps; i++)
                    {
                        for (int j = 0; j <= (i / 2); j++)
                        {
                            int dx = 0, dy = 0;

                            if (i % 2 == 0)
                            {
                                if ((i / 2) % 2 == 0)
                                {
                                    dx = XPOSSTEP;
                                    dy = 0;
                                }
                                else
                                {
                                    dx = -XPOSSTEP;
                                    dy = 0;
                                }
                            }
                            else
                            {
                                if ((i / 2) % 2 == 0)
                                {
                                    dx = 0;
                                    dy = YPOSSTEP;
                                }
                                else
                                {
                                    dx = 0;
                                    dy = -YPOSSTEP;
                                }
                            }

                            x += dx;
                            y += dy;

                            if (MoveMouseAndCheckCursor(wowRectangle.X + x, wowRectangle.Y + y))
                                return;
                        }
                    }
                }
            }

            throw new Exception("Fish not found"); // Will be catch in Manager:EyeProcess_RunWorkerCompleted
        }

        private bool MoveMouseAndCheckCursor(int x, int y)
        {
            if (m_manager.IsStoppedOrPaused())
                throw new Exception("Bot paused or stopped");

            Win32.MoveMouse(x, y);

            // Sleep (give the OS a chance to change the cursor)
            Thread.Sleep(Properties.Settings.Default.ScanningDelay);

            Win32.CursorInfo actualCursor = Win32.GetCurrentCursor();

            if (actualCursor.flags == m_noFishCursor.flags &&
                actualCursor.hCursor == m_noFishCursor.hCursor)
                return false;

            // Compare the actual icon with our fishIcon if user want it
            if (Properties.Settings.Default.CheckCursor)
                if (!ImageCompare(Win32.GetCursorIcon(actualCursor), Properties.Resources.fishIcon35x35))
                    return false;

            // We found a fish !
            return true;
        }

        private static bool ImageCompare(Bitmap firstImage, Bitmap secondImage)
        {
            if (firstImage.Width != secondImage.Width || firstImage.Height != secondImage.Height)
                return false;

            for (int i = 0; i < firstImage.Width; i++)
                for (int j = 0; j < firstImage.Height; j++)
                    if (firstImage.GetPixel(i, j).ToString() != secondImage.GetPixel(i, j).ToString())
                        return false;

            return true;
        }
        public void GetBobber()
        {
            Thread.Sleep(2000);
            Bitmap template = new Bitmap(16, 16, PixelFormat.Format24bppRgb);
            try
            {
                Bitmap bmp = Screenshot();
                using (FileStream stream = new FileStream(Properties.Settings.Default.BobberIcon, FileMode.Open, FileAccess.Read))
                {
                    template = (Bitmap)System.Drawing.Image.FromStream(stream);
                }

                const Int32 divisor = 8;
                Bitmap source = new ResizeNearestNeighbor(bmp.Width / divisor, bmp.Height / divisor).Apply(bmp);
                Bitmap test = new ResizeNearestNeighbor(template.Width / divisor, template.Height / divisor).Apply(template);
                // create template matching algorithm's instance
                // (set similarity threshold to 92.1%)

                float match = Properties.Settings.Default.match;
                //ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.901f);
                ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(match);

                // find all matchings with specified above similarity

                TemplateMatch[] matchings = tm.ProcessImage(source, test);

                // highlight found matchings
                if (matchings.Length > 0)
                {
                    BitmapData data = source.LockBits(
                         new Rectangle(0, 0, source.Width, source.Height),
                         ImageLockMode.ReadWrite, source.PixelFormat);
                    BitmapData dataorg = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
                    foreach (TemplateMatch m in matchings)
                    {
                        int placex = (int)(((m.Rectangle.X * divisor) * 1.02f) / dpi);
                        int placey = (int)(((m.Rectangle.Y * divisor) * 1.02f) / dpi);
                        Rectangle rec = new Rectangle(m.Rectangle.X * divisor, m.Rectangle.Y * divisor, m.Rectangle.Width * divisor, m.Rectangle.Height * divisor);
                        if (MoveMouseAndCheckCursor(placex, placey))
                            break;
                        Drawing.Rectangle(dataorg, rec, Color.Red);

                        //MessageBox.Show(m.Rectangle.Location.ToString());
                        // do something else with matching
                    }
                    source.UnlockBits(data);
                    bmp.UnlockBits(dataorg);
                    //bmp.Save(@"D:\temp\Bobbershot.jpg", ImageFormat.Jpeg);
                }
                else
                {
                    throw new Exception("No Bobber Found");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                template.Dispose();
            }
        }


        public Bitmap Screenshot()
        {

            // Scale the rectangle.

            int processWidth = (int)(wowRectangle.Width * dpi);
            int processHeight = (int)(wowRectangle.Height * dpi);


            var bmpScreenshot = new Bitmap(processWidth, processHeight, PixelFormat.Format24bppRgb);
            Graphics graph = Graphics.FromImage(bmpScreenshot);
            graph.CopyFromScreen(wowRectangle.Left, wowRectangle.Top, 0, 0, new Size(processWidth, processHeight), CopyPixelOperation.SourceCopy);

            //bmpScreenshot.Save(@"D:\temp\debugscreen.jpg");
            return bmpScreenshot;
        }



    }
}