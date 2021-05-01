using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;


//___x___ 25  Tutorial completed(honor system on this one)
//___x___ 10  Video: colbert1a draws any line
//___x___	10	Video: colbert1a line is white
//___x___	5	Video: colbert1b line is red
//___x___	10	Video: colbert1b line is 2 pixels wide
//___x___	15	Video: colbert2 Put a Bird On It
//___x___	20	Video: colbert3 rotation
//___x___	5	Video: colbert3 line is green
//__100___	Total (please add the points and include the total here)

namespace Rotoscope
{
    /// <summary>
    /// Main form
    /// </summary>
    public partial class MainForm : Form
    {
        Movie inputMovie = null;
        Movie eliMovie = null;
        private string lastSave = null;
        MovieMaker maker = null;

        /// <summary>
        /// initalizes the form
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            DoubleBuffered = true;

            UpdateMenuBar();

        }

        /// <summary>
        /// Handle the resize event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (maker != null)
            {
                SetupMaker();
            }
            Invalidate();
        }

        /// <summary>
        /// Refresh the window
        /// </summary>
        public new void Invalidate()
        {
            base.Invalidate();
        }

        /// <summary>
        /// Clean up files on close
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            maker = null;
            inputMovie = null;
        }

        /// <summary>
        /// Paint the frame
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPaint(PaintEventArgs e)
        {
            if (maker != null)
                maker.OnDraw(e.Graphics);
        }

        /// <summary>
        /// Updates what menu options are enabled and disabled
        /// </summary>
        private void UpdateMenuBar()
        {
            if (maker == null)
            {
                //file menu
                closeItem.Enabled = false;
                saveAsItem.Enabled = false;
                saveItem.Enabled = false;

                //movie menu
                closeAudioItem.Enabled = false;
                closeMovieItem.Enabled = false;
                generateVideoItem.Enabled = false;
                pullAudioItem.Enabled = false;
                generateVideoItem.Enabled = false;

                //frame menu
                createFrameItem.Enabled = false;
                writeFrameItem.Enabled = false;
                writeThenCreateFrameItem.Enabled = false;
                writeThenCreateSecondItem.Enabled = false;
                writeThenCreateRemainingItem.Enabled = false;

            }
            else
            {
                //file menu
                closeItem.Enabled = true;
                saveAsItem.Enabled = true;
                saveItem.Enabled = true;

                //movie menu
                closeMovieItem.Enabled = true;
                generateVideoItem.Enabled = true;

                if (maker.CurFrameCount > 0)
                    generateVideoItem.Enabled = true;
                else
                    generateVideoItem.Enabled = false;

                //Frame menu
                createFrameItem.Enabled = true;
                writeFrameItem.Enabled = true;
                writeThenCreateFrameItem.Enabled = true;

                if (maker.Audio != null)
                {
                    closeAudioItem.Enabled = true;
                    pullAudioItem.Enabled = true;
                }
                else
                {
                    closeAudioItem.Enabled = false;
                    useSourceAudioItem.Checked = false;
                }

                if (maker.SourceMovie != null)
                {
                    writeThenCreateSecondItem.Enabled = true;
                    writeThenCreateRemainingItem.Enabled = true;
                }
                else
                {
                    writeThenCreateSecondItem.Enabled = false;
                    writeThenCreateRemainingItem.Enabled = false;
                }
            }
        }

        /// <summary>
        /// Pulls drawing area for the movie maker, and initalizes if needed.
        /// </summary>
        private void SetupMaker()
        {
            if (maker == null)
                maker = new MovieMaker(this);

            Rectangle r = ClientRectangle;
            r.Y = menuStrip1.Height;
            maker.DrawArea = r;
        }

        #region Menu Handlers
        #region File menu
        private void newItem_Click(object sender, EventArgs e)
        {
            inputMovie = new Movie();
            SetupMaker();

            UpdateMenuBar();
            Invalidate();
        }

        private void openRotoItem_Click(object sender, EventArgs e)
        {
            if (openDlgRoto.ShowDialog() == DialogResult.OK)
            {
                //TODO
                if(maker != null)
                    maker.OnOpenRotoscope(openDlgRoto.FileName);
            }
            UpdateMenuBar();
        }

        private void saveAsRotoItem_Click(object sender, EventArgs e)
        {
            if (saveDlgRoto.ShowDialog() == DialogResult.OK)
            {
                maker.OnSaveRotoscope(saveDlgRoto.FileName);
                lastSave = saveDlgRoto.FileName;
            }
            saveDlgRoto.Dispose();
        }

        private void saveRotoItem_Click(object sender, EventArgs e)
        {
            if (lastSave != null)
            {
                maker.OnSaveRotoscope(lastSave);
            }
            else
            {
                if (saveDlgRoto.ShowDialog() == DialogResult.OK)
                {
                    maker.OnSaveRotoscope(saveDlgRoto.FileName);
                    lastSave = saveDlgRoto.FileName;
                }
            }
        }

        private void closeAllItem_Click(object sender, EventArgs e)
        {
            inputMovie = null;
            maker = null;
            UpdateMenuBar();
            Invalidate();
        }

        private void exitItem_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        #region Movie Menu
        private void openSourceMovieItem_Click(object sender, EventArgs e)
        {
            if (openDlgMovie.ShowDialog() == DialogResult.OK)
            {
                if(inputMovie == null)
                {
                    inputMovie = new Movie();
                }

                inputMovie.Open(openDlgMovie.FileName);

                //the first movie opened, should set the size of the output
                SetupMaker();
                if (maker.SourceMovie == null)
                {
                    maker.Width = inputMovie.Width;
                    maker.Height = inputMovie.Height;
                }

                maker.SourceMovie = inputMovie;

                //pull audio if  desired
                if (useSourceAudioItem.Checked)
                {
                    maker.Audio = inputMovie.GetAudio();
                }
            }
            UpdateMenuBar();
            Invalidate();
        }


        private void generateVideoItem_Click(object sender, EventArgs e)
        {
            if (saveDlgOutMovie.ShowDialog() == DialogResult.OK)
            {
                if (maker != null)
                    maker.ProcClipVideo(saveDlgOutMovie.FileName);
            }
            UpdateMenuBar();
            Invalidate();
            
        }

        private void pullAudioItem_Click(object sender, EventArgs e)
        {
            if (saveDlgAudio.ShowDialog() == DialogResult.OK)
            {
                if (inputMovie != null)
                {
                    Sound sound = inputMovie.GetAudio();
                    sound.SaveAs(saveDlgAudio.FileName, (SoundFileTypes)(saveDlgAudio.FilterIndex-1));
                }
            }

        }

        private void closeMovieItem_Click(object sender, EventArgs e)
        {
            inputMovie.Close();
            inputMovie = null;

            UpdateMenuBar();
            Invalidate();
        }


        private void openAudioItem_Click(object sender, EventArgs e)
        {
            if (openDlgAudio.ShowDialog() == DialogResult.OK)
            {
                if (maker != null)
                {
                    maker.Audio=new Sound(openDlgAudio.FileName);
                    useSourceAudioItem.Checked = false;
                    openAudioItem.Checked = true;
                }
            }

            UpdateMenuBar();
            Invalidate();
        }

        private void closeAudioItem_Click(object sender, EventArgs e)
        {
            maker.Audio= null;
            UpdateMenuBar();
            Invalidate();
        }

        private  void useSourceAudioItem_Click(object sender, EventArgs e)
        {
            useSourceAudioItem.Checked = !useSourceAudioItem.Checked;

            if (maker != null) {
                if (useSourceAudioItem.Checked)
                {
                    maker.Audio=  inputMovie.GetAudio();
                    openAudioItem.Checked = false;
                }
                else
                {
                    maker.Audio.Close();
                    maker.Audio=null;
                }
            }
            UpdateMenuBar();
            
        }
        #endregion

        #region Frame menu

        private void createFrameItem_Click(object sender, EventArgs e)
        {
            maker.CreateOneFrame();
            Invalidate();
        }

        private  void writeThenCreateFrameItem_Click(object sender, EventArgs e)
        {
            maker.WriteFrame();
            maker.CreateOneFrame();
            Invalidate();
        }

        private void writeFrameItem_Click(object sender, EventArgs e)
        {
            maker.WriteFrame();
        }

        private void writeThenCreateSecondItem_Click(object sender, EventArgs e)
        {
            ProgressBar dlg = new ProgressBar();
            dlg.Show();
            int count = (int)(maker.FPS + 0.5);
            for (int i = 0; i < count; i++)
            {
                maker.WriteFrame();
                maker.CreateOneFrame();
               
                //out of bound check?
                if (!inputMovie.HasMoreVideo)
                    break;

                dlg.UpdateProgress(i / count);
            }

            dlg.Dispose();
            UpdateMenuBar();

        }

        private void writeThenCreateRemainingItem_Click(object sender, EventArgs e)
        {
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();
            int count = inputMovie.TotalFrames - inputMovie.Position+1;
            float i = 0;

            ProgressBar dlg = new ProgressBar();
            dlg.Show();

            //TODO remove 1000  later
            while (inputMovie.HasMoreVideo)
            {
                maker.WriteFrame();
                maker.CreateOneFrame();
                dlg.UpdateProgress( i / count);
                i++;
            }
            dlg.Dispose();


            watch.Stop();

            Debug.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");
            UpdateMenuBar();
        }
        #endregion

        #endregion

        #region Mouse handlers
        private bool mouseDown = false;

        /// <summary>
        /// Handles all mouse down and move events
        /// </summary>
        /// <param name="x">X pixel </param>
        /// <param name="y">Y pixel</param>
        public void Mouse(int x, int y)
        {
            if (maker != null)
                maker.Mouse(x, y - menuStrip1.Height); 
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            Mouse(e.X, e.Y);
            mouseDown = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            mouseDown = false;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (mouseDown)
                Mouse(e.X, e.Y);
        }



        #endregion


        /// <summary>
        /// Helper function to delete files that may temporarily be in use while closing a folder.
        /// </summary>
        /// <param name="file">file to delete</param>
        public static void VolitilePermissionDelete(string file)
        {
            //delete file
            int maxAttempts = 10;
            int attempt = 0;
            bool success = false;
            while (!success && attempt < maxAttempts)
            {
                try
                {
                    File.Delete(file);
                    success = true;
                }
                catch (Exception)
                {
                    Debug.WriteLine("File delayed: " + file); //delay slightly for the filesystem to catchup
                    Thread.Sleep(100);
                }
            }

            if (!success)
                Debug.WriteLine("File could not be deleted: " + file);
        }

        private void clearFrameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(maker != null)
                maker.OnEditClearFrame();

            Invalidate();
        }

        private void drawBirdOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(maker != null)
                maker.BirdDraw = true;
            drawBirdOffToolStripMenuItem.Checked = false;
            drawBirdOnToolStripMenuItem.Checked = true;
        }

        private void drawBirdOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(maker != null)
                maker.BirdDraw = false;
            drawBirdOffToolStripMenuItem.Checked = true;
            drawBirdOnToolStripMenuItem.Checked = false;
        }

        private void openBackgroundImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openImage = new OpenFileDialog();
            if (openImage.ShowDialog() == DialogResult.OK)
            {
                if (maker != null)
                {
                    maker.BackgroundImage = new Bitmap(openImage.FileName);
                }
            }

            UpdateMenuBar();
            Invalidate();
        }

        private void openEliVideoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openDlgMovie.ShowDialog() == DialogResult.OK)
            {
                if (eliMovie == null)
                {
                    eliMovie = new Movie();
                }

                eliMovie.Open(openDlgMovie.FileName);

                //the first movie opened, should set the size of the output
                SetupMaker();
                if (maker.EliMovie == null)
                {
                    maker.EliWdith = eliMovie.Width;
                    maker.EliHeight = eliMovie.Height;
                }

                maker.EliMovie = eliMovie;

                //pull audio if  desired
                if (useSourceAudioItem.Checked)
                {
                    maker.Audio = eliMovie.GetAudio();
                }
            }
            UpdateMenuBar();
            Invalidate();
        }
    }
}
