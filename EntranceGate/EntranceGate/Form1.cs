using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using System.Media;
using System.Timers;
using System.IO;


namespace EntranceGate
{
    public partial class Form1 : Form
    {
        #region 设备实例和全局变量
        /// <summary>
        /// 微光互联扫码器
        /// </summary>
        TX400Scanner scanner;

        /// <summary>
        /// 摄像头
        /// </summary>
        CameraCV camera;

        /// <summary>
        /// 底层硬件控制板
        /// </summary>
        PlatformController pcontroller;

        /// <summary>
        /// Logger
        /// </summary>
        SimpleLogger logger;
       
        /// <summary>
        /// wuzhanggui API接口
        /// </summary>
        APIClient client;

        /// <summary>
        /// 语音合成
        /// </summary>
        BaiduSpeech speech;

        /// <summary>
        /// AIP人脸识别
        /// </summary>
        BaiduFace CVclient;

        /// <summary>
        /// 人脸识别错误次数
        /// </summary>
        int faceError = 0;

        /// <summary>
        /// 固定店铺ID
        /// </summary>
        static string shopid = "1";

        /// <summary>
        /// 定时刷新picturebox
        /// </summary>
        System.Timers.Timer pictureboxRefreshTimer;

        /// <summary>
        /// 定时刷新faceError
        /// </summary>
        System.Timers.Timer faceErrorResetTimer;

        /// <summary>
        /// 系统锁定timer
        /// </summary>
        System.Timers.Timer LockTimer;

        /// <summary>
        /// 每次操作锁定时间间隔ms
        /// </summary>
        static int locktime = 3000;

        /// <summary>
        /// 随机生成文件名，防止冲突
        /// </summary>
        System.Random rand_filename = new Random();

        /// <summary>
        /// 系统锁定状态
        /// </summary>
        private bool sysIsLock = false;

        #endregion 设备实例和全局变量

        #region Delegate methods
        /// <summary>
        /// RichTextBox委托赋值
        /// </summary>
        /// <param name="txt">TextBox</param>
        /// <param name="content">内容</param>
        delegate void setDataTxt(RichTextBox txt, string content);

        /// <summary>
        /// RichTextBox委托赋值函数
        /// </summary>
        /// <param name="txt">TextBox</param>
        /// <param name="content">内容</param>
        private void SetDataText(RichTextBox txt, string content)
        {
            if (txt.InvokeRequired)
            {
                setDataTxt setThis = new setDataTxt(SetDataText);

                txt.Invoke(setThis, txt, content);
            }
            else
            {
                txt.Text = content;
            }
        }

        /// <summary>
        /// PictureBox委托赋值
        /// </summary>
        /// <param name="box"></param>
        /// <param name="bit"></param>
        delegate void setPictureBox(PictureBox box, Bitmap img);

        /// <summary>
        /// PictureBox委托赋值函数
        /// </summary>
        /// <param name="box"></param>
        /// <param name="img"></param>
        private void SetPictureBox(PictureBox box, Bitmap img)
        {
            if(box.InvokeRequired)
            {
                setPictureBox setThis = new setPictureBox(SetPictureBox);

                box.Invoke(setThis, box, img);
            }
            else
            {
                box.Image = img;
            }
        }

        /// <summary>
        /// tilebutton委托赋值
        /// </summary>
        /// <param name="button"></param>
        /// <param name="userInfo"></param>
        delegate void setTileButton(Bunifu.Framework.UI.BunifuTileButton button, APIClient.UserInfo userInfo);

        /// <summary>
        /// tilebutton委托赋值函数
        /// </summary>
        /// <param name="tileButton"></param>
        /// <param name="userInfo"></param>
        private void SetTileButton(Bunifu.Framework.UI.BunifuTileButton tileButton, APIClient.UserInfo userInfo)
        {
            if (tileButton.InvokeRequired)
            {
                setTileButton setThis = new setTileButton(SetTileButton);

                tileButton.Invoke(setThis, tileButton, userInfo);
            }
            else
            {
                tileButton.Image = APIClient.LoadPicture(userInfo.avatarUrl);
                tileButton.LabelText = userInfo.nickName;
            }
        }

        /// <summary>
        /// PictureBoxVisible委托赋值
        /// </summary>
        /// <param name="box"></param>
        /// <param name="isVisiable"></param>
        delegate void setPictureBoxVisibility(PictureBox box, bool isVisiable);

        /// <summary>
        /// PictureBoxVisible委托赋值函数
        /// </summary>
        /// <param name="box"></param>
        /// <param name="isVisiable"></param>
        private void SetPictureBoxVisibility(PictureBox box, bool isVisiable)
        {
            if (box.InvokeRequired)
            {
                setPictureBoxVisibility setThis = new setPictureBoxVisibility(SetPictureBoxVisibility);

                box.Invoke(setThis, box, isVisiable);
            }
            else
            {
                box.Visible = isVisiable;
            }
        }

        /// <summary>
        /// tilebuttonVisable委托赋值
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isVisiable"></param>
        delegate void setTileButtonVisiable(Bunifu.Framework.UI.BunifuTileButton button, bool isVisiable);

        /// <summary>
        /// tilebuttonVisiable委托赋值函数
        /// </summary>
        /// <param name="tileButton"></param>
        /// <param name="isVisable"></param>
        private void SetTileButtonVisable(Bunifu.Framework.UI.BunifuTileButton tileButton, bool isVisiable)
        {
            if (tileButton.InvokeRequired)
            {
                setTileButtonVisiable setThis = new setTileButtonVisiable(SetTileButtonVisable);

                tileButton.Invoke(setThis, tileButton, isVisiable);
            }
            else
            {
                tileButton.Visible = isVisiable;
            }
        }

        /// <summary>
        /// messageLabel委托赋值
        /// </summary>
        /// <param name="label"></param>
        /// <param name="message"></param>
        delegate void setMessageLabel(Bunifu.Framework.UI.BunifuCustomLabel label, string message);

        /// <summary>
        /// tilebuttonVisiable委托赋值函数
        /// </summary>
        /// <param name="tileButton"></param>
        /// <param name="isVisable"></param>
        private void SetMessageLabel(Bunifu.Framework.UI.BunifuCustomLabel label, string message)
        {
            if (label.InvokeRequired)
            {
                setMessageLabel setThis = new setMessageLabel(SetMessageLabel);

                label.Invoke(setThis, label, message);
            }
            else
            {
                label.Text = message;
            }
        }

        #endregion delegate methods

        #region 屏幕显示和语音提示字符串
        private string welcome_str = "欢迎光临物掌柜";
        private string code_identify_success_str = "欢迎扫码进店购物";
        private string face_identify_success_str = "欢迎扫脸进店购物";
        private string face_identify_error_str = "请尝试使用小程序扫码进店";
        private string identify_hint_str = "欢迎体验扫码或刷脸进店";
        #endregion 屏幕显示和语音提示字符串

        /// <summary>
        /// Main
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            logger = new SimpleLogger();

            scanner = new TX400Scanner();
            OpenScanner();

            camera = new CameraCV();

            pcontroller = new PlatformController();
            OpenController();

            //调试用，直接对接百度API
            CVclient = new BaiduFace();

            speech = new BaiduSpeech();

            client = new APIClient();
            logger.Info("Get Client Token = " + APIClient.token);

            //启动timer，周期刷新picturebox
            pictureboxRefreshTimer = new System.Timers.Timer();
            pictureboxRefreshTimer.Interval = 1000;
            pictureboxRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(pictureboxRefresh);
            pictureboxRefreshTimer.AutoReset = true;
            pictureboxRefreshTimer.Start();

            //启动timer，周期刷新faceError
            faceErrorResetTimer = new System.Timers.Timer();
            faceErrorResetTimer.Elapsed += new System.Timers.ElapsedEventHandler(faceErrorReset);
            faceErrorResetTimer.AutoReset = true;
            faceErrorResetTimer.Interval = 10000;
            faceErrorResetTimer.Start();

            //设置lock timer
            LockTimer = new System.Timers.Timer();
            LockTimer.Elapsed += new ElapsedEventHandler(UnlockSystem);

            //open pictualbox always on
            SetPictureBoxVisibility(pictureBoxAlwaysOn, true);
        }

        #region Scanner
        /// <summary>
        /// 打开互联微光扫码器
        /// </summary>
        private void OpenScanner()
        {
            TX400Scanner.ScannerReturn ret = scanner.Open();
            if (ret == TX400Scanner.ScannerReturn.SUCCESS)
            {
                scanner.CodeFound += ShowCodeInTextBox;
                scanner.CodeFound += EntryByCodeProcess;
                scanner.StartDecodeThread();
                logger.Info("Open QR Code Scanner success");
            }
            else
            {
                logger.Error("Open QR code Scanner fail, failcode=" + ret.ToString());
            }
        }

        /// <summary>
        /// 接收到扫描QR码事件发生时调用函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowCodeInTextBox(object sender, TX400Scanner.QRcodeScannerEventArgs e)
        {
            //richTextBox1.Text = e.Code.ToString();
            SetDataText(richTextBox1, e.Code.ToString());
        }

        /// <summary>
        /// 扫码进入闸机流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EntryByCodeProcess(object sender, TX400Scanner.QRcodeScannerEventArgs e)
        {
            //scanner is trigged, lock the system 3s
            if(sysIsLock == false)
            {
                //lock the scanner in case this process is interrupted
                scanner.Lock();
                logger.Info("Recieved code=" + e.Code);
                logger.Trace("Lock QR code scanner started " + DateTime.Now.ToString());

                char[] trimchars = new char[2] { '\0', ' ' };
                string trimcode = e.Code.Trim(trimchars);

                APIClient.Code code = new APIClient.Code() { code = trimcode };

                APIClient.UserInfo userinfo = client.EntryByCode(code);
                if (userinfo.nickName != string.Empty)
                {
                    SetTileButton(tileBtn, userinfo);
                    SetTileButtonVisable(tileBtn, true);
                    SetPictureBox(pictureBoxCenter, new Bitmap("success.gif"));
                    SetPictureBoxVisibility(loadBox_small, false);
                    SetMessageLabel(bunifuCustomLabel1, code_identify_success_str);

                    OpenGate();

                    PlayWelcomSound(userinfo.nickName);

                    APIClient.Log log = new APIClient.Log()
                    {
                        who = userinfo.id,
                        where = shopid
                    };

                    client.VistLogToSystem(log);

                    //success code will lock the system for 3s
                    LockSystem();

                    //clear textbox
                    SetDataText(richTextBox1, string.Empty);

                }
                else
                {
                    //服务器没有返回用户信息
                    //todo：提示QRcode错误
                    logger.Error("Server return unvalid user info, QR code Error");
                    scanner.Unlock();
                }
            }
            else
            {
                //skip this scan result if system is lockeds
            }
        }

        /// <summary>
        /// 扫码器成功后锁定3s，timer调用解锁函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnlockScanner()
        {
            scanner.Unlock();
            logger.Trace("Lock QR code scanner stoped " + DateTime.Now.ToString());
        }

        /// <summary>
        /// 每次刷脸和扫码成功，锁定系统3s
        /// </summary>
        private void LockSystem()
        {
            sysIsLock = true;
            LockTimer.Interval = locktime;  //3 second protect time
            LockTimer.AutoReset = false;
            LockTimer.Start();
            scanner.Lock();
        }

        /// <summary>
        /// 系统解锁状态，timer调用解锁
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnlockSystem(object sender, System.Timers.ElapsedEventArgs e)
        {
            sysIsLock = false;
            UnlockScanner();
        }

        /// <summary>
        /// 关闭扫码器
        /// </summary>
        private void CloseScanner()
        {
            scanner.Close();
            logger.Info("Close QR Code Scanner.");
        }
        #endregion Scanner

        #region Camera
        /// <summary>
        /// 摄像头初始化
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            if(camera.Init("UVC"))
            {
                logger.Info("Open Camera success");
                OpenCamera();
            }
            else
            {
                MessageBox.Show("摄像头初始化失败，请检查相机后重启");
                logger.Error("Open Camera fail, please check the USB cable.");
            }
        }

        /// <summary>
        /// 刷新faceError,超时清零
        /// </summary>
        private void faceErrorReset(object sender, EventArgs e)
        {
            faceError = 0;
            faceErrorResetTimer.Close();
        }

        /// <summary>
        /// 打开摄像头
        /// </summary>
        private void OpenCamera()
        {
            //call back function register
            camera.ImageCaptured += ShowImageInPictureBox;
            camera.FaceCaptured += ShowFaceInPictureBox;

            //Open local pre-process
            camera.localFaceDetect = true;
            camera.localFaceDistanceFilter = true;
            //设置人脸过滤的大小阈值, 100*100pixel的最小过滤框
            camera.SetThreshold(185);

            camera.Open();
            CameraCV.StartProcessOnFrame();
        }

        /// <summary>
        /// 实时在picturebox中显示摄像头捕获图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowImageInPictureBox(object sender, CameraCV.CameraEventArgs e)
        {
            if(e.Image != null)
            {
                SetPictureBox(pictureBoxAlwaysOn, e.Image);
            }
        }

        /// <summary>
        /// 收到摄像头event事件调用函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowFaceInPictureBox(object sender, CameraCV.CameraFaceEventArgs e)
        {
            if (sysIsLock == false)
            {
                if (e.FaceImage != null)
                {
                    Bitmap bitmap = (Bitmap)e.FaceImage.Clone();

                    //change face search from server
                    //if (faceIdentify(bitmap))
                    if (faceIdentify_usingAIP(bitmap))
                    {
                        SetPictureBox(pictureBoxCenter, new Bitmap("success.gif"));

                        //hide load box
                        SetPictureBoxVisibility(loadBox_small, false);

                        //play welcome tts with name on the tilebutton
                        PlayWelcomSound(tileBtn.LabelText);

                        SetMessageLabel(bunifuCustomLabel1, face_identify_success_str);

                        faceError = 0;

                        //success face recogn will lock the system 3s
                        LockSystem();
                    }
                    else
                    {
                        faceError++;
                        if (faceError > 3)
                        {
                            //show error text on the screen
                            SetMessageLabel(bunifuCustomLabel1, face_identify_error_str);

                            //play error hint
                            PlayErrorSound();

                            faceError = 0;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 刷新picturebox任务，在timer中调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureboxRefresh(object sender, EventArgs e)
        {
            if(camera.isOpen==false)
            {
                return;
            }

            if(camera.isProcessing == false)
            {
                SetPictureBox(pictureBoxCenter, new Bitmap("face.gif"));

                //SetPictureBoxVisibility(pictureBox2, false);

                SetTileButtonVisable(tileBtn, false);

                SetPictureBoxVisibility(loadBox_small, true);

                CameraCV.StartProcessOnFrame();

                SetMessageLabel(bunifuCustomLabel1, identify_hint_str);
            }
        }

        /// <summary>
        /// 关闭摄像头
        /// </summary>
        private void CloseCamera()
        {
            if(camera.isOpen)
            {
                CameraCV.SkipProcessOnFrame();
                camera.ImageCaptured -= ShowImageInPictureBox;
                camera.FaceCaptured -= ShowFaceInPictureBox;
                camera.Close();
            }
        }

        /// <summary>
        /// 刷脸进门流程处理, Not Used
        /// </summary>
        /// <param name="bmap"></param>
        private bool faceIdentify(Bitmap bmap)
        {
            bool ret = false;
            try
            {
                //random file name, solving conflict problem
                string filepath = rand_filename.Next().ToString() + ".jpg";

                if (File.Exists(filepath))
                {
                    File.Delete(filepath);
                }

                bmap.Save(filepath, System.Drawing.Imaging.ImageFormat.Jpeg);

                APIClient.UserInfo userinfo = client.EntryByFace(filepath);

                if (userinfo.nickName != string.Empty)
                {
                    SetTileButton(tileBtn, userinfo);
                    SetTileButtonVisable(tileBtn, true);

                    OpenGate();

                    //PlayWelcomSound(userinfo.nickName);

                    APIClient.Log log = new APIClient.Log()
                    {
                        who = userinfo.id,
                        where = shopid
                    };

                    client.VistLogToSystem(log);
                    ret = true;
                }
                File.Delete(filepath);
            }
            catch (Exception ex)
            {
                logger.Error("Camera Event search faces failed, exception: " + ex.ToString());
            }
            return ret;
        }

        /// <summary>
        /// 刷脸进门流程处理，直接调用aip节省阿里云流量费用
        /// </summary>
        /// <param name="bmap"></param>
        /// <returns></returns>
        private bool faceIdentify_usingAIP(Bitmap bmap)
        {
            bool ret = false;

            var result = CVclient.face_search_using_aip(bmap);
            logger.Info("Get Face search return message : " + result.ToString());
            if (result.GetValue("error_code").ToString() != "0")
            {
                ret = false;
            }
            else
            {
                var score = result["result"]["user_list"][0]["score"];
                double valid = Convert.ToDouble(score.ToString());
                if (valid >= 90)
                {
                    var id = result["result"]["user_list"][0]["user_id"];
                    APIClient.ID uid = new APIClient.ID() { id = id.ToString() };

                    APIClient.UserInfo userinfo = client.GetUserInfo(uid);

                    if (userinfo.nickName != string.Empty)
                    {
                        SetTileButton(tileBtn, userinfo);
                        SetTileButtonVisable(tileBtn, true);

                        OpenGate();

                        //PlayWelcomSound(userinfo.nickName);

                        APIClient.Log log = new APIClient.Log()
                        {
                            who = userinfo.id,
                            where = shopid
                        };

                        client.VistLogToSystem(log);
                        ret = true;
                    }
                }
            }
            return ret;
        }
        #endregion Camera

        #region Arduino
        /// <summary>
        /// 打开arduino控制器串口
        /// </summary>
        private void OpenController()
        {
            string portname = "COM3";
            pcontroller.Open(portname, 9600);
            logger.Info("Open controller using " + portname + "@9600");
        }

        /// <summary>
        /// 关闭arduino控制器串口
        /// </summary>
        private void CloseController()
        {
            pcontroller.Close();
            logger.Info("Close controller serialport success");
        }

        /// <summary>
        /// 发送闸机开门消息到arduino
        /// </summary>
        private void OpenGate()
        {
            pcontroller.SendGateOpenMessage();
            logger.Info("Send open gate command to controller success");
        }
        #endregion Arduino

        #region Sound
        /// <summary>
        /// 播放迎宾语
        /// </summary>
        /// <param name="welcome"></param>
        private void PlayWelcomSound(string nickname)
        {
            DateTime tmCur = DateTime.Now;

            if (tmCur.Hour < 8 || tmCur.Hour > 18)
            {
                speech.Tts2Play("晚上好" + nickname + welcome_str);
            }
            else if (tmCur.Hour >= 8 && tmCur.Hour < 12)
            {
                speech.Tts2Play("上午好" + nickname + welcome_str);
            }
            else
            {
                speech.Tts2Play("下午好" + nickname + welcome_str);
            }
        }

        /// <summary>
        /// 播放错误语音
        /// </summary>
        /// <param name="error"></param>
        private void PlayErrorSound()
        {
            speech.Tts2Play(face_identify_error_str);
        }
        #endregion Sound

        /// <summary>
        /// 关闭所有已打开资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            pictureboxRefreshTimer.Stop();
            faceErrorResetTimer.Stop();
            CloseScanner();
            CloseCamera();
            CloseController();
            this.Dispose();
        }
    }
}
