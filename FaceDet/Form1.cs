using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace FaceDet
{
    public partial class Form1 : Form
    {
        //DragBar
        Point onClickLoc;
        bool isDrag = false;

        //Controls
        Panel titleBar      = new Panel();
        PictureBox closeBtn = new PictureBox();
        ImageBox imb        = new ImageBox();
        Button startBtn     = new Button();
        Button saveFace     = new Button();
        Label lbName        = new Label();
        TextBox profileName = new TextBox();

        //Face Det
        MCvFont font = new MCvFont(Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6, 0.6);//Emgu.CV.CvEnum.FONT.CV_FONT_HERSHEY_TRIPLEX, 0.6, 0.6
        HaarCascade faceDetected;
        Image<Bgr, byte> frame;
        Capture camera;
        Image<Gray, byte> result;
        Image<Gray, byte> trainedFace = null;
        Image<Gray, byte> grayFace = null;
        List<Image<Gray, byte>> trainingImages = new List<Image<Gray, byte>>();
        List<string> labels = new List<string>();
        List<string> users = new List<string>();
        int count, numLabels, t;
        string name, names = null;


        public Form1 ( )
        {
            InitializeComponent();
            faceDetected = new HaarCascade("haarcascade_frontalface_default.xml");//Properties.Resources.haarcascade_frontalface_default
            try
            {
                string labelsInf = File.ReadAllText(Application.StartupPath + "/Faces/Faces.txt");
                string[] Labels = labelsInf.Split(',');
                numLabels = Convert.ToInt16(Labels[0]);
                count = numLabels;
                string loadedFaces;
                for (int i = 1; i < numLabels + 1; i++)
                {
                    loadedFaces = "face" + i + ".bmp";
                    trainingImages.Add(new Image<Gray, byte>(new Bitmap(Application.StartupPath + "/Faces/" + loadedFaces)));
                    labels.Add(Labels[i]);
                }
            } catch (Exception ex) { MessageBox.Show("Mazala makayn 7ata wjeh f DB !"); }

        }

        //Najoutiw biha nos ctrls l parent
        private void addControls (Control[] ct, Panel p = null)
        {
            if (p == null) Controls.AddRange(ct);
            else p.Controls.AddRange(ct);
        }

        //Bach nSettiw new point
        private void setNewLoc (Control ctr, int x, int y)
        {
            ctr.Location = new Point(x, y);
        }

        //frameProcEvIdle
        private void FrameProc (object o, EventArgs e)
        {
            users.Add("");
            frame = camera.QueryFrame().Resize(imb.Width, imb.Height, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            grayFace = frame.Convert<Gray, byte>();
            MCvAvgComp[][] facesDetNow = grayFace.DetectHaarCascade(faceDetected, 1.2, 10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_ROUGH_SEARCH, new Size(20, 20));
            foreach (MCvAvgComp f in facesDetNow[0])
            {
                result = frame.Copy(f.rect).Convert<Gray, byte>().Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
                frame.Draw(f.rect, new Bgr(Color.Green),2);
                if (trainingImages.ToArray().Length != 0)
                {
                    MCvTermCriteria termCriterias = new MCvTermCriteria(count, 0.001);
                    EigenObjectRecognizer recognizer = new EigenObjectRecognizer(trainingImages.ToArray(), labels.ToArray(), 1500, ref termCriterias);
                    name = recognizer.Recognize(result);
                    frame.Draw(name, ref font, new Point(f.rect.X - 2, f.rect.Y - 2), new Bgr(Color.Red));

                }
               // users[t - 1] = name;
                users.Add("");
            }
            imb.Image = frame;
            names = "";
            users.Clear();
        }

        //ev on click startBTN
        private void startDetectionEv (object o, EventArgs e)
        {
            camera = new Capture();
            camera.QueryFrame();
            Application.Idle += new EventHandler(FrameProc);
        }

        private void SaveFace (object o, EventArgs e)
        {
            count += 1;
            grayFace = camera.QueryGrayFrame().Resize(320, 240, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            MCvAvgComp[][] DetectedFaces = grayFace.DetectHaarCascade(faceDetected,1.2,10, Emgu.CV.CvEnum.HAAR_DETECTION_TYPE.DO_CANNY_PRUNING, new Size(20, 20));
            foreach (MCvAvgComp f in DetectedFaces[0])
            {
                trainedFace = frame.Copy(f.rect).Convert<Gray, byte>();
                break;
            }
            trainedFace = result.Resize(100, 100, Emgu.CV.CvEnum.INTER.CV_INTER_CUBIC);
            trainingImages.Add(trainedFace);
            labels.Add(profileName.Text);
            File.WriteAllText(Application.StartupPath + "/Faces/Faces.txt", trainingImages.ToArray().Length.ToString() + ",");
            for (int i = 1; i < trainingImages.ToArray().Length+1; i++)
            {
                trainingImages.ToArray()[i - 1].Save(Application.StartupPath + "/Faces/face"+i+".bmp");
                File.AppendAllText(Application.StartupPath + "/Faces/Faces.txt", labels.ToArray()[i - 1] + ",");
            }
            //MessageBox.Show(profileName.Text + " Tsajal !");
            
        }


        private void Form1_Load (object sender, EventArgs e)
        {
            //form
            Height = 400;
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Color.White;

            //titleBar
            titleBar.Dock = DockStyle.Top;
            titleBar.Height = 40;
            titleBar.BackColor = Color.LightGray;

            titleBar.MouseDown += (o, ev) =>
            {
                isDrag = true;
                onClickLoc = ev.Location;
            };

            titleBar.MouseUp += (o, ev) =>
            {
                isDrag = false;
            };

            titleBar.MouseMove += (o, ev) =>
            {
                if (isDrag) setNewLoc(this, Location.X - (onClickLoc.X - ev.Location.X), Location.Y - (onClickLoc.Y - ev.Location.Y));
            };

            //closeBtn
            closeBtn.Image = Properties.Resources.closeBtn;
            closeBtn.SizeMode = PictureBoxSizeMode.StretchImage;
            closeBtn.Height = 25;
            closeBtn.Width = 30;
            setNewLoc(closeBtn, 10, (titleBar.Height - closeBtn.Height) / 2);
            closeBtn.Click += (o, ev) =>
            {
                if (MessageBox.Show("Do you Wanna Exit ?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes) Environment.Exit(Environment.ExitCode);
            };


            //ImageBox
            imb.Height = Height - titleBar.Height;
            imb.Width = 500;
            setNewLoc(imb, 0, titleBar.Height);
            imb.BackColor = Color.WhiteSmoke;


            //BtnStartFaceDetect
            startBtn.Text = "[ START DET ]";
            startBtn.Height = 35;
            startBtn.Width = 100;
            startBtn.FlatStyle = FlatStyle.System;
            setNewLoc(startBtn, imb.Width + 20, titleBar.Height + 20);
            startBtn.Click += startDetectionEv;


            //labelName
            lbName.Text = "Name : ";
            lbName.Width = 50;
            setNewLoc(lbName, imb.Width + 20, titleBar.Height + 70);

            //TextBoxProfileName
            setNewLoc(profileName, imb.Width + 20 + lbName.Width, titleBar.Height + 70);
            profileName.Height = 35;

            //BtnSaveFace 
            saveFace.Text = "[ SAVE FACE ]";
            saveFace.Height = 35;
            saveFace.Width = 100;
            saveFace.FlatStyle = FlatStyle.System;
            setNewLoc(saveFace, imb.Width + 20, titleBar.Height + 100);
            saveFace.Click += SaveFace;




            //daba we add ctrls
            addControls(new Control[] {
                titleBar,
                imb,
                startBtn,
                lbName,
                profileName,
                saveFace
            });

            addControls(new Control[] { closeBtn }, titleBar);
        }
    }
}
