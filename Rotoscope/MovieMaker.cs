using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Events;

namespace Rotoscope
{
    /// <summary>
    /// Main class for managing making a rotoscoped movie.
    /// </summary>
    public class MovieMaker
    {
        #region save locations

        private int clipCount = 0;
        private string clipSaveName = "clip_";
        private string clipSavePath = "clips\\";
        private int clipSize = 60;
        private string frameSaveName = "frame_";
        private string frameSavePath = "frames\\";
        private string tempAudioSavePath = "tempAudioOutput.wav";
        private string tempVideoSavePath = "tempVideoOutput.mp4";

        #endregion


        #region Properties

        /// <summary>
        /// The background audio for the output movie.
        /// </summary>
        public Sound Audio { get => backgroundAudio; set => backgroundAudio = value; }

        /// <summary>
        /// Current Frame being shown
        /// </summary>
        public Frame CurFrame { get => curFrame; }

        /// <summary>
        /// Current frame count written
        /// </summary>
        public int CurFrameCount { get => framenum; }

        /// <summary>
        /// Area to draw the video frame
        /// </summary>
        public Rectangle DrawArea { set => drawArea = value; }

        /// <summary>
        /// Frames per second
        /// </summary>
        public double FPS { get => fps; set => fps = value; }

        /// <summary>
        /// Height of the movie
        /// </summary>
        public int Height { get => height; set => height = value; }

        /// <summary>
        /// Movie from which to pull frames
        /// </summary>
        public Movie SourceMovie
        {
            get => sourceMovie;
            set
            {
                sourceMovie = value;
                fps = sourceMovie.FrameRate;
            }
        }

        public Movie EliMovie
        {
            get => eliMovie;
            set
            {
                eliMovie = value;
            }
        }

        /// <summary>
        /// Width of the movie
        /// </summary>
        public int Width { get => width; set => width = value; }

        public int EliWdith { get => eliWidth; set => eliWidth = value; }
        public int EliHeight { get => eliHeight; set => eliHeight = value; }

        public Bitmap BackgroundImage { get => backgroundImage; set => backgroundImage = value; }

        private Sound backgroundAudio;
        private Frame curFrame;
        private Bitmap backgroundImage;
        private double fps = 30;
        private int framenum = 0;

        private int height = 720;
        private double outputTime = 0;
        private Movie sourceMovie = null;
        private Movie eliMovie = null;
        private int eliWidth;
        private int eliHeight;
        private int width = 1280;
        private ProgressBar bar;
        private Rectangle drawArea = new Rectangle(100, 100, 100, 100);
        private string fmt = "00000";
        private MainForm form;
        private Font drawFont = new Font("Arial", 16);
        private SolidBrush drawBrush = new SolidBrush(Color.Red);
        private string processing = "";
        //physics animation stuff
        private int logoX = 1;
        private int logoY = 1;
        private int speedX = 180;
        private int speedY = 180;

        private int guyX = -260;
        private int guyY = 0;

        #endregion

        /// <summary>
        /// Constructor for a movie maker that taken in the form in which to draw the results
        /// </summary>
        /// <param name="form"></param>
        /// 
        private Rotoscope roto = new Rotoscope();
        private Frame initial = new Frame();
        private Frame initialEli = new Frame();

        private Boolean birdDraw = false;
        public Boolean BirdDraw { get => birdDraw; set => birdDraw = value; }

        Bitmap bird;
        Bitmap bodyReported;
        public MovieMaker(MainForm form)
        {
            Close();
            this.form = form;
            curFrame = new Frame(width, height);

            if (!Directory.Exists(frameSavePath))
                Directory.CreateDirectory(frameSavePath);

            if (!Directory.Exists(clipSavePath))
                Directory.CreateDirectory(clipSavePath);

            bird = new Bitmap("bird.png"); //with bird.png set to copy to output  
            bodyReported = Properties.Resources.bodyReported;

        }

        /// <summary>
        /// Release data sources, and clean up temporary files
        /// </summary>
        public void Close()
        {
            backgroundAudio = null;
            framenum = 0;

            if (Directory.Exists(frameSavePath))
            {
                string[] files = Directory.GetFiles(frameSavePath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }

            if (Directory.Exists(clipSavePath))
            {
                string[] files = Directory.GetFiles(clipSavePath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
        }

        /// <summary>
        /// Handles all mouse events
        /// </summary>
        /// <param name="x">X pixel</param>
        /// <param name="y">Y pixel</param>
        public void Mouse(int x, int y)
        {
            roto.AddToDrawList(framenum, new Point(x, y));
            BuildFrame();
        }





        /// <summary>
        /// Draw the current state of the makers
        /// </summary>
        /// <param name="graphics">graphics reference to drawn area</param>
        public void OnDraw(Graphics graphics)
        {
            curFrame.OnDraw(graphics, drawArea);

            graphics.DrawString(processing, drawFont, drawBrush, 100, 100);

            if (processing == "Completed")
                processing = "";
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
            curFrame.DrawLine(x1, y1, x2, y2, Color.Green);
        }

        public bool OnSaveRotoscope(string filename)
        {
            //
            // Open an XML document
            //
            XmlDocument doc = new XmlDocument();

            //
            // Make first node
            XmlElement root = doc.CreateElement("movie");

            //
            // Have children save inside this node
            //
            roto.OnSaveRotoscope(doc, root);

            //Save the resulting DOM tree to a file
            doc.AppendChild(root);

            try
            {
                //
                // Make the output indented, and save
                //
                XmlWriterSettings settings = new XmlWriterSettings();
                settings.Indent = true;
                XmlWriter writer = XmlWriter.Create(filename, settings);
                doc.Save(writer);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Save Rotoscope Error");
                return false;
            }

            return true;
        }

        public void OnOpenRotoscope(string filename)
        {

            //
            // Open an XML document
            //
            XmlDocument reader = new XmlDocument();
            reader.Load(filename);

            //
            // Traverse the XML document in memory!!!!
            //

            foreach (XmlNode node in reader.ChildNodes)
            {
                if (node.Name == "movie")
                {
                    roto.OnOpenRotoscope(node);
                }
            }

        }

        /// <summary>
        /// Destructor to clean up files that were made during processing
        /// </summary>
        ~MovieMaker()
        {
            Close(); 
        }


        #region Frame load and save

        /// <summary>
        /// Creates one frame. If a input movie is given, and the are frame left, the frame is pulled from the 
        /// next frame in the movie. If not, a blank, black frame is generated. 
        /// 
        /// Loading is done asyncrounously
        /// </summary>
        /// <returns>a generic task </returns>
        public async Task CreateOneFrame()
        {
            curFrame.Clear();

            if (sourceMovie != null)
            {

                Bitmap newImage = sourceMovie.LoadNextFrameImage();


                // sanity chack that an image is there
                if (newImage != null)
                {
                    try
                    {
                        Graphics g = Graphics.FromImage(curFrame.Image);
                        g.DrawImage(newImage, 0, 0); //this is MUCH faster than looping through
                        newImage.Dispose(); // release the image

                        // Save a copy of the original, unmodified image                    
                        initial = new Frame(curFrame.Image);

                        BuildFrame();
                    }
                    catch (Exception)
                    {
                        // shouldn't happen, but...
                        Debug.WriteLine("Skipped frame!!!!!!!!!!!!!!!!!!!!!!!");
                    }
                }

                form.Invalidate();
            }
        }

        /// <summary>
        /// Takes the currently made clips, and audio if given, and converts them into a movie. 
        /// It will save in in the given location.
        /// 
        /// This is done asyncrounously.
        /// </summary>
        /// <param name="savePath">the location to save the movie</param>
        /// <returns>generic task</returns>
        public void ProcClipVideo(string savePath)
        {
            //sanity check for source images
            if (framenum <= 0)
            {
                MessageBox.Show("No frames currently generated", "Processing error");
                return;
            }

            processing = "Write remaining frames...";
            form.Invalidate();

            //some frames remaining? write to clip
            if (Directory.GetFiles(frameSavePath).Count() > 0)
            {
                Task taskFrames = Task.Run(() => WriteClip());
                taskFrames.Wait();
            }

            outputTime = (double)(framenum) / fps;

            //concatenate clip to the full movie first

            processing = "Concatenating clips..";
            form.Invalidate();

            //one clip, rename to output
            if (Directory.GetFiles(clipSavePath).Count() <= 1)
            {
                string file = Directory.GetFiles(clipSavePath).First();
                if (File.Exists(tempVideoSavePath))
                {
                    File.Delete(tempVideoSavePath);
                }
                File.Move(file, tempVideoSavePath);
            }
            else
            {
                //more than one clip, concat
                try
                {
                    Task taskConcat = Task.Run(() => ConcatClips());
                    taskConcat.Wait();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Processing error");
                }
            }


            //find silent video location
            Task<IMediaInfo> taskVid = Task.Run(() => FFmpeg.GetMediaInfo(tempVideoSavePath));
            IMediaInfo mediaInfoVid = taskVid.Result;

            //grab the video stream 
            IVideoStream video = mediaInfoVid.VideoStreams.First();

            //if there is a sound object, pull the audio information
            IMediaInfo mediaInfo = null;
            if (backgroundAudio != null)
            {
                Task<IMediaInfo> taskAudio = Task.Run(() => 
                                FFmpeg.GetMediaInfo(backgroundAudio.Filename));

                mediaInfo = taskAudio.Result;
            }

            try
            {
                processing = "Adding audio...";
                form.Invalidate();

                //make a new video (overwrite if needed), with the given video stream
                IConversion convert = FFmpeg.Conversions.New()
                    .AddStream(video)
                    .SetOutput(savePath)
                    .SetFrameRate(fps)
                    .SetOverwriteOutput(true);

                //if there is audio, add that stream
                if (mediaInfo != null)
                {
                    convert.AddStream(mediaInfo.AudioStreams);
                }

                //monitor and onvert
                Task taskConvert = Task.Run(() => convert.Start());
                taskConvert.Wait();

                processing = "Completed";
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Processing error");
            }
        }

        /// <summary>
        /// Takes the currently made frames, and audio if given, and converts them into a movie. 
        /// It will save in in the given location.
        /// 
        /// This is done asyncrounously.
        /// </summary>
        /// <param name="savePath">the location to save the movie</param>
        public async void ProcFrameVideo(string savePath)
        {
            //sanity check for source images
            if (framenum <= 0)
            {
                MessageBox.Show("No frames currently generated", "Processing error");
                return;
            }

            bar = new ProgressBar();
            bar.Show();

            //if there is a sound object, pull the audio
            IMediaInfo mediaInfo = null;

            //check if audio is avilable
            if (backgroundAudio != null)
            {
                //save the file locally for ease
                string tempAudio = tempAudioSavePath;
                if (File.Exists(tempAudio))
                {
                    File.Delete(tempAudio);
                }
                backgroundAudio.SaveAs(tempAudio, SoundFileTypes.MP3);

                mediaInfo = await FFmpeg.GetMediaInfo(backgroundAudio.Filename);
            }

            try
            {

                //grab file list, and setup for stitching
                List<string> files = Directory.EnumerateFiles(frameSavePath).ToList();
                outputTime = (double)files.Count / fps;

                //make a new file, overwrite if needed, with the FPS, and using the mp4 file format for movie frames
                IConversion convert = FFmpeg.Conversions.New()
                    .SetOverwriteOutput(true)
                    .SetInputFrameRate(fps)
                    .BuildVideoFromImages(files)
                    .SetFrameRate(fps)
                    .SetPixelFormat(Xabe.FFmpeg.PixelFormat.yuv420p)
                    .SetOutput(savePath);

                //if there is audio, add that stream
                if (mediaInfo != null) {
                    convert.AddStream(mediaInfo.AudioStreams);
                }

                //monitor and onvert
                convert.OnProgress += Progress;
                await convert.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Processing error");
            }
            finally
            {
                bar.Dispose();
            }
        }

        /// <summary>
        /// Creates a image file with the current frame to later be used in making the movie.
        /// </summary>
        /// <returns>generic task</returns>
        public void WriteFrame()
        {
            string name = frameSavePath + frameSaveName + framenum.ToString(fmt) + ".png";

            curFrame.SaveFile(name, ImageFormat.Png);
            framenum++;

            if (framenum % clipSize == 0)
            {
                Task task = Task.Run(() => WriteClip());
                task.Wait();
            }
        }

        /// <summary>
        /// Helper function to concatenate saved, silent, clips in the clip directory
        /// </summary>
        /// <param name="progressBar">should a progress bar be shown</param>
        /// <returns></returns>
        private async Task ConcatClips()
        {
            string[] files = Directory.GetFiles(clipSavePath);

            //nothing to do
            if (files.Length <= 1)
                return;

            try
            {

                IConversion result = await Concatenate(tempVideoSavePath, files);
                await result.Start();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Concatenate failure");
            }
        }


        /// <summary>
        /// Concatenate a list of videos together with no sound.
        /// </summary>
        /// <param name="output">where to save the result</param>
        /// <param name="inputVideos">a list of video file names to concatenate</param>
        /// <returns>a conversion interface that can be started later</returns>
        private async Task<IConversion> Concatenate(string output, params string[] inputVideos)
        {
            if (inputVideos.Length <= 1)
            {
                throw new ArgumentException("You must provide at least 2 files for the concatenation to work", "inputVideos");
            }

            var mediaInfos = new List<IMediaInfo>();

            //make a new video, and overwite old video if there
            IConversion conversion = FFmpeg.Conversions.New().SetOverwriteOutput(true);

            //for all videos, add them to the list
            foreach (string inputVideo in inputVideos)
            {
                IMediaInfo mediaInfo = await FFmpeg.GetMediaInfo(inputVideo);

                mediaInfos.Add(mediaInfo);
                conversion.AddParameter($"-i \"{inputVideo}\" ");
            }

            //set up FFmpeg command line argumetns to concatenate
            conversion.AddParameter($"-filter_complex \"");
            conversion.AddParameter($"concat=n={inputVideos.Length}:v=1:a=0 [v]\" -map \"[v]\"");

            return conversion.SetOutput(output);
        }

        /// <summary>
        /// Monitoring function
        /// </summary>
        /// <param name="o">the objec that sent the event</param>
        /// <param name="args">details aboutthe evernt and progress</param>
        private void Progress(object o, ConversionProgressEventArgs args)
        {
            double percent = args.Duration.TotalSeconds / outputTime;

            bar.UpdateProgress(percent);
        }

        public void OnEditClearFrame()
        {
            roto.ClearFrame(framenum);
            BuildFrame();
        }


        /// <summary>
        /// Save a set of frames to a movie clip. This acts as cacheing 
        /// as movie clips take less space than idependent frames.
        /// </summary>
        /// <returns>generic task</returns>
        private async Task WriteClip()
        {
            string name = clipSavePath + clipSaveName + clipCount.ToString(fmt) + ".mp4";

            try
            {
                //grab file list, and setup for stitching
                List<string> files = Directory.EnumerateFiles(frameSavePath).ToList();

                Debug.WriteLine("Frames: " + files.ToString());
                outputTime = (double)files.Count / fps; //length of time to monitor progress

                //make a new mp4, with the curent video FPS, and frame images. Overwrite allowed
                IConversion convert = FFmpeg.Conversions.New()
                    .SetOverwriteOutput(true)
                    .SetInputFrameRate(fps)
                    .BuildVideoFromImages(files)
                    .SetFrameRate(fps)
                    .SetPixelFormat(Xabe.FFmpeg.PixelFormat.yuv420p)
                    .SetOutput(name);

                await convert.Start();

                //memory cleanup as frame files are no longer needed
                foreach (string file in files)
                {
                    MainForm.VolitilePermissionDelete(file);
                }
                clipCount++;
                System.GC.Collect();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Clip save error");
            }
        }

        private void BuildFrame()
        {
            double time = framenum / 30;
            Bitmap elijahImage = eliMovie.LoadNextFrameImage();


            curFrame = new Frame(initial.Image);
            Graphics g = Graphics.FromImage(curFrame.Image);

           


            // Get cat drawing
            LinkedList<Point> drawList = roto.GetFromDrawList(framenum);
            if (drawList != null)
            {
                //Point first = drawList.First.Value;
                //Point end = drawList.First.Next.Value;
                if (BirdDraw)
                {
                    Point location = drawList.ElementAt(0);
                    bird.SetResolution(g.DpiX, g.DpiY);
                    int x = location.X - (bird.Width / 2);
                    int y = location.Y - (bird.Height);

                    curFrame.DrawImage(x, y, bird);
                }
                else
                {
                    drawList = roto.GetFromDrawList(framenum);
                    if (drawList != null)
                    {
                        foreach (Point p in drawList)
                        {
                            curFrame.DrawDot(p.X, p.Y);
                        }
                    }
                }


            }

            //Greenscreen();
            //if (elijahImage != null)
            //{
            //    elijahImage.SetResolution(g.DpiX, g.DpiY);
            //    GreenscreenNoMask(elijahImage);
            //    g.DrawImage(elijahImage, 800, 0);
            //}

            Bitmap reportPic = Properties.Resources.reportBody;
            reportPic.SetResolution(g.DpiX, g.DpiY);
            g.DrawImage(reportPic, 1280 - reportPic.Width, 720 - reportPic.Height);

            Bitmap imposter = Properties.Resources.amongUsGuyPic;
            imposter.SetResolution(g.DpiX, g.DpiY);
            guyY = height - imposter.Height;

            //PHYSICS******************************************
            Bitmap eliCool = Properties.Resources.eliCool;
            eliCool.SetResolution(g.DpiX, g.DpiY);
            logoX = logoX + (speedX / 30);
            logoY = logoY + (speedY / 30);

            if(logoX + eliCool.Width >= width)
            {
                speedX *= -1;
                logoX = width - eliCool.Width;
            }
            else if(logoX <= 0)
            {
                speedX *= -1;
                logoX = 0;
            }

            if(logoY + eliCool.Height >= height)
            {
                speedY *= -1;
                logoY = height - eliCool.Height;

            }
            else if(logoY <= 0)
            {
                speedY *= -1;
                logoY = 0;
            }
            g.DrawImage(eliCool, logoX, logoY);

            if(time > 19 && guyX < 10)
            {
                guyX += speedX/30;
            }
            g.DrawImage(imposter, guyX, guyY);
            //ending drawing
            if (time > 28.5)
            {
                bodyReported.SetResolution(g.DpiX, g.DpiY);
                g.DrawImage(bodyReported, 0, 0);
            }

            //Greenscreen();

            form.Invalidate();
        }

        public void RotateVideo(int x1, int y1, int x2, int y2)
        {
            float middleX = (x1 + x2) / 2f;
            float middleY = (y1 + y2) / 2f;
            float degreesPerFrame = 360f / 27f;

            Graphics g = Graphics.FromImage(curFrame.Image);
            //get point to transform from
            g.TranslateTransform(middleX, middleY);
            //rotate specific degrees
            g.RotateTransform(degreesPerFrame * (framenum + 1));
            //translate back
            g.TranslateTransform(-middleX, -middleY);
            g.DrawImage(curFrame.Image, new Point(0));

            GraphicsPath graphicsPath = new GraphicsPath();
            GraphicsUnit graphicsUnit = GraphicsUnit.Pixel;
            graphicsPath.AddRectangle(g.ClipBounds);
            graphicsPath.AddRectangle(curFrame.Image.GetBounds(ref graphicsUnit));
            g.FillPath(Brushes.Black, graphicsPath);
        }


        public void Greenscreen()
        {
            // alpha calculation
            double a1 = 6;
            double a2 = 2.75;
            double redForeground;
            double greenForeground;
            double l1, l2 = 0;


            //MASK AND BACKGROUND
            Bitmap image = curFrame.Image;
            Bitmap mask = Properties.Resources.catMask;
            backgroundImage = Properties.Resources.amongUsHallway;


            for (int r = 0; r < backgroundImage.Height; r++)
            {
                for (int c = 0; c < backgroundImage.Width; c++)
                {
                    if(r < image.Height && c < image.Width)
                    {
                        l1 = mask.GetPixel(c, r).GetBrightness();
                        l2 = 1 - l1;

                        int redFore = image.GetPixel(c, r).R;
                        int blueFore = image.GetPixel(c, r).B;
                        int greenFore = image.GetPixel(c, r).G;
                        int redBack = backgroundImage.GetPixel(c, r).R;
                        int blueBack = backgroundImage.GetPixel(c, r).B;
                        int greenBack = backgroundImage.GetPixel(c, r).G;
                        double alpha = (1 - a1 * (greenFore - ((l1 * a2) * redFore)) + l2);
                        alpha = Clamp01(alpha);

                        Color final = Color.FromArgb((int)(alpha * redFore + (1 - alpha) * redBack),
                                        (int)(alpha * redFore + (1 - alpha) * greenBack),
                                        (int)(alpha * blueFore + (1 - alpha) * blueBack));
                        curFrame.Image.SetPixel(c, r, final);

                    }
                }
            }

        }

        public void GreenscreenNoMask(Bitmap image)
        {
            backgroundImage = Properties.Resources.amongUsHallway;
            // alpha calculation
            double a1 = 5;
            double a2 = 1.25;

            int offset = 850;

            for (int r = 0; r < backgroundImage.Height; r++)
            {
                for (int c = 0; c < backgroundImage.Width; c++)
                {
                    if (r < image.Height && c >= offset)
                    {

                        int redFore = image.GetPixel(c - offset, r).R;
                        int blueFore = image.GetPixel(c - offset, r).B;
                        int greenFore = image.GetPixel(c - offset, r).G;
                        int redBack = backgroundImage.GetPixel(c, r).R;
                        int blueBack = backgroundImage.GetPixel(c, r).B;
                        int greenBack = backgroundImage.GetPixel(c, r).G;
                        double alpha = (1 - a1 * (greenFore - ( a2 * redFore)));
                        alpha = Clamp01(alpha);

                        Color final = Color.FromArgb((int)((alpha * redFore) + ((1 - alpha) * redBack)),
                                        (int)((alpha * greenFore) + ((1 - alpha) * greenBack)),
                                        (int)((alpha * blueFore) + ((1 - alpha) * blueBack)));
                        image.SetPixel(c - offset, r, final);

                    }
                }
            }

        }

        private static double Clamp01(double val)
        {
            if (val < 0)
                val = 0;
            else if (val > 1)
                val = 1;

            return val;
        }
        #endregion


    }
}
