using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;


public class CameraCV
{
    /// <summary>
    /// 摄像头打开标识
    /// </summary>
    public bool isOpen;

    /// <summary>
    /// 本地人脸检测开关
    /// </summary>
    public bool localFaceDetect;

    /// <summary>
    /// 本地人脸距离过滤开关
    /// 过滤掉距离过远的人脸检测
    /// </summary>
    public bool localFaceDistanceFilter;
    private Size ThresholdSize;

    /// <summary>
    /// 调节人脸距离范围门限
    /// </summary>
    /// <param name="num"></param>
    public void SetThreshold(int num)
    {
        ThresholdSize = new Size(num, num);
    }

    /// <summary>
    /// AForge库筛选信息
    /// </summary>
    private FilterInfoCollection webcam;

    /// <summary>
    /// 摄像头对象
    /// </summary>
    static public VideoCaptureDevice cam;

    /// <summary>
    /// 本地保存的摄像头捕捉图片
    /// </summary>
    private Bitmap captureImage;

    /// <summary>
    /// 返回的图片数据类
    /// </summary>
    public class CameraEventArgs : EventArgs
    {
        Bitmap image;

        public CameraEventArgs(Bitmap bit)
        {
            this.image = bit;
        }

        public Bitmap Image
        {
            get { return image; }
        }
    }

    /// <summary>
    /// 返回的图片数据和人脸
    /// </summary>
    public class CameraFaceEventArgs : EventArgs
    {
        Bitmap fullimage;
        Bitmap faceimage;

        public CameraFaceEventArgs(Bitmap full, Bitmap face)
        {
            this.fullimage = full;
            this.faceimage = face;
        }

        public Bitmap FullImage
        {
            get { return fullimage; }
        }

        public Bitmap FaceImage
        {
            get { return faceimage; }
        }
    }

    /// <summary>
    /// 仅图片采集委托事件模型
    /// </summary>
    public delegate void CameraEventHandler(object sender, CameraEventArgs e);

    /// <summary>
    /// 图片采集带人脸的委托事件模型
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void CameraFaceEventHandler(object sender, CameraFaceEventArgs e);

    /// <summary>
    /// 获取图像成功时发生
    /// </summary>
    public event CameraEventHandler ImageCaptured;
    public event CameraFaceEventHandler FaceCaptured;

    /// <summary>
    /// 内部公开方法
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnImageCaptured(CameraEventArgs e)
    {
        if (ImageCaptured != null)
        {
            ImageCaptured(this, e);
        }
    }
    protected virtual void OnFaceCaptured(CameraFaceEventArgs e)
    {
        if (FaceCaptured != null)
        {
            FaceCaptured(this, e);
        }
    }

    /// <summary>
    /// 是否对新数据图片帧进行处理的标识
    /// </summary>
    private static bool processOnNewFrame;

    public bool isProcessing
    {
        get { return processOnNewFrame; }
    }

    /// <summary>
    /// 打开处理方法
    /// </summary>
    public static void StartProcessOnFrame()
    {
        processOnNewFrame = true;
    }

    /// <summary>
    /// 跳过处理方法
    /// </summary>
    public static void SkipProcessOnFrame()
    {
        processOnNewFrame = false;
    }

    /// <summary>
    /// 摄像头初始化方法，筛选设备名，选定制定名称的摄像头
    /// </summary>
    /// <param name="cameraName"></param>
    public bool Init(string cameraName)
    {
        bool ret = false;
        //get the camera ready
        webcam = new FilterInfoCollection(FilterCategory.VideoInputDevice);

        foreach (FilterInfo VideoCapDevice in webcam)
        {
            if (VideoCapDevice.Name.Contains(cameraName))
            {
                cam = new VideoCaptureDevice(VideoCapDevice.MonikerString);
                //select resolution 800*600
                cam.VideoResolution = cam.VideoCapabilities[0];
                //register event handler
                cam.NewFrame += new NewFrameEventHandler(CaptureNewFrame);
                ret = true;

                localFaceDetect = true;
                localFaceDistanceFilter = true;

                SetThreshold(100);
            }
        }
        return ret;
    }

    /// <summary>
    /// 摄像头打开方法
    /// </summary>
    public void Open()
    {
        cam.Start();
        isOpen = true;
    }

    /// <summary>
    /// 摄像头关闭方法
    /// </summary>
    public void Close()
    {
        if (cam.IsRunning)
        {
            cam.Stop();
            isOpen = false;
        }
    }

    /// <summary>
    /// 采集到新图片数据帧的callback函数
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="eventArgs"></param>
    void CaptureNewFrame(Object sender, NewFrameEventArgs eventArgs)
    {
        if (processOnNewFrame)
        {
            //refresh captured image
            captureImage = (Bitmap)eventArgs.Frame.Clone();
            //start once and capture only one image            

            //call the local face detect method
            if (localFaceDetect)
            {
                long detectionTime;
                List<Rectangle> faces = new List<Rectangle>();
                DetectFace.Detect(captureImage, "haarcascade_frontalface_default.xml", faces, out detectionTime);
                if (faces.Count <= 0)
                {
                    //no face in the image
                }
                else
                {
                    if (localFaceDistanceFilter)
                    {
                        for (int i = 0; i < faces.Count; i++)
                        {
                            Rectangle face = faces[i];
                            if ((face.Size.Height <= ThresholdSize.Height) || (face.Size.Width <= ThresholdSize.Width))
                            {
                                faces.Remove(face);
                            }
                        }
                        if (faces.Count > 0)
                        {
                            captureImage = DetectFace.Show(captureImage, faces);

                            //var face = DetectFace.SeprateFace(captureImage, faces);
                            var face = DetectFace.PickOneBigFace(captureImage, faces);

                            //only raise event when there is close enough face
                            //OnImageCaptured(new CameraEventArgs(captureImage));
                            OnFaceCaptured(new CameraFaceEventArgs(captureImage, face));
                        }
                    }
                    else
                    {
                        captureImage = DetectFace.Show(captureImage, faces);
                    }
                }
            }

            //raise the event
            //OnImageCaptured(new CameraEventArgs(captureImage));

            processOnNewFrame = false;
        }
    }
}

