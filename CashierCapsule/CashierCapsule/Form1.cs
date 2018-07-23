using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using Reader;
using Newtonsoft.Json;
using System.Diagnostics;


namespace CashierCapsule
{
    public partial class Form1 : Form
    {
        #region Instances of Components
        /// <summary>
        /// UHF reader interface
        /// </summary>
        public ReaderMethod reader;

        /// <summary>
        /// rest API client
        /// </summary>
        APIClient client;

        /// <summary>
        /// logger
        /// </summary>
        SimpleLogger logger;

        /// <summary>
        /// 底层硬件控制器Arduino MEGA
        /// </summary>
        PlatformController pcontroller;

        /// <summary>
        /// Encoder controller Arduino UNO
        /// </summary>
        PlatformController encoder;

        /// <summary>
        /// 摄像头处理模块
        /// </summary>
        CameraCV camera;

        /// <summary>
        /// AIP人脸识别
        /// </summary>
        BaiduFace CVclient;

        /// <summary>
        /// TTS语音合成
        /// </summary>
        BaiduSpeech speech;
        #endregion Instances of Components

        #region 语音合成提示语句
        private string doorClosingHintStr = "正在关门";
        private string doorOpeningHintStr = "正在开门";
        private string emgAlarmHintStr = "紧急按钮已触发,自动门已停止";
        private string emgReleaseHintStr = "紧急按钮已释放,自动门恢复正常";
        private string cameraHintStr = "请正对屏幕和摄像头,即将进行人脸扫描";
        private string paymentHintStr = "请在60秒内完成付款,如您想返回店内,请按右下角取消按钮";
        private string customerWelcomeHintStr = "欢迎光临物掌柜";
        private string face_identify_hint_str = "请正视摄像头识别人脸";
        private string face_identify_error_str = "请正视摄像头重新识别人脸";
        private string customerLeaveHintStr = "离开时请注意下坡间隙,期待您的再次光临";
        private string inverseInterWarningStr = "请离开收银出口，从左侧闸机进去购物区，谢谢您的配合";
        private string cashierStartHintStr = "正在生成您的订单，请稍后";
        private string closeDoorRetryErrorStr = "门总是关不上啊";
        private string openDoorInwardRetryErrorStr = "转门朝内打开失败啦";
        private string openDoorOutwardRetryErrorStr = "转门朝外打开失败啦";
        private string codeGenerateFailHintStr = "生成支付码失败啦";
        private string payWithBalanceSuccessHintStr = "储值支付成功";
        private string payWithQRCodeSuccessHintStr = "扫码支付成功";
        private string paymentCanceledHintStr = "支付已取消";

        // for Debug
        string readerErrorHintStr = "读卡器读取结果超时，请检修读卡器";
        #endregion 语音合成提示语句

        #region Global Variables
        /// <summary>
        /// encoder读取的累计绝对值
        /// </summary>
        private static int lastEncoderValue = 0;

        /// <summary>
        /// 人脸识别错误次数
        /// </summary>
        private int faceError = 0;

        /// <summary>
        /// 定时刷新picturebox
        /// </summary>
        System.Timers.Timer pictureboxRefreshTimer;

        /// <summary>
        /// 定时刷新faceError
        /// </summary>
        System.Timers.Timer faceErrorResetTimer;

        /// <summary>
        /// 固定店铺ID
        /// </summary>
        static string shopid = "1";

        /// <summary>
        /// 读卡器天线功率: 0-33(0x00-0x21)
        /// </summary>
        static byte output_power = 29;

        /// <summary>
        /// StateMachine全局变量
        /// </summary>
        private static Stages currentStage;

        /// <summary>
        /// 上一次完整扫描Tag清单
        /// </summary>
        private static APIClient.TagInfoList lastScanTagList;

        /// <summary>
        /// 上一次完整扫描Tag数量
        /// </summary>
        private static int lastScanTagCount;

        /// <summary>
        /// 上一次摄像头识别的用户信息
        /// </summary>
        private static APIClient.UserInfo lastUserinfo;

        /// <summary>
        /// countdown timer for payment timeout and result check
        /// </summary>
        private static int counter;

        /// <summary>
        /// 生成订单号全局变量
        /// </summary>
        private static string lastTradeNo;

        /// <summary>
        /// Clear static global variables
        /// </summary>
        private void ClearLastScan()
        {
            lastScanTagCount = -1;//default set to -1
            lastScanTagList = new APIClient.TagInfoList();
        }

        /// <summary>
        /// Clear static global variable lastUserinfo
        /// </summary>
        private void ClearLastUser()
        {
            lastUserinfo = new APIClient.UserInfo()
            {
                id = string.Empty,
                nickName = string.Empty,
                avatarUrl = string.Empty,
                level = string.Empty,
            };
        }
        #endregion Global Variables

        #region Delegate Methods
        /// <summary>
        /// status toolbar委托代理
        /// </summary>
        /// <param name="status"></param>
        /// <param name="label"></param>
        /// <param name="text"></param>
        delegate void setToolStatusBar(StatusStrip status, ToolStripStatusLabel label, string text);

        /// <summary>
        /// status toolbar委托赋值函数
        /// </summary>
        /// <param name="status"></param>
        /// <param name="label"></param>
        /// <param name="text"></param>
        private void SetToolStatusBar(StatusStrip status, ToolStripStatusLabel label, string text)
        {
            if(status.InvokeRequired)
            {
                setToolStatusBar setThis = new setToolStatusBar(SetToolStatusBar);

                status.Invoke(setThis, status, label, text);
            }
            else
            {
                label.Text = text;
                status.Refresh();
            }
        }

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
                //txt.Text = content;
                txt.AppendText(content + Environment.NewLine);
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
            if (box.InvokeRequired)
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
        /// TileButton委托赋值
        /// </summary>
        /// <param name="button"></param>
        /// <param name="userInfo"></param>
        delegate void setTileButton(Bunifu.Framework.UI.BunifuTileButton button, APIClient.UserInfo userInfo);

        /// <summary>
        /// TileButton委托赋值函数
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
        /// DataGridView委托赋值
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        delegate void setDataGridViewInfo(Bunifu.Framework.UI.BunifuCustomDataGrid view, APIClient.MerchandiseInfoCashier[] infos);

        /// <summary>
        /// DataGridView委托赋值函数
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        private void SetDataGridViewInfo(Bunifu.Framework.UI.BunifuCustomDataGrid view, APIClient.MerchandiseInfoCashier[] infos)
        {
            if (view.InvokeRequired)
            {
                setDataGridViewInfo setThis = new setDataGridViewInfo(SetDataGridViewInfo);

                view.Invoke(setThis, view, infos);
            }
            else
            {
                view.ColumnCount = 3;
                view.Columns[0].Name = "ID";
                view.Columns[1].Name = "名称";
                view.Columns[2].Name = "单价";

                foreach (APIClient.MerchandiseInfoCashier info in infos)
                {
                    string[] row = new string[]
                    {
                        info.id,
                        info.name,
                        info.originPrice,
                    };
                    view.Rows.Add(row);
                }
            }
        }

        /// <summary>
        /// bunifucustomerLabel委托赋值
        /// </summary>
        /// <param name="view"></param>
        /// <param name="txt"></param>
        delegate void setLabelText(Bunifu.Framework.UI.BunifuMetroTextbox view, string txt);

        /// <summary>
        /// bunifucustomeLabel委托赋值函数
        /// </summary>
        /// <param name="view"></param>
        /// <param name="txt"></param>
        private void SetLabelText(Bunifu.Framework.UI.BunifuMetroTextbox view, string txt)
        {
            if(view.InvokeRequired)
            {
                setLabelText setThis = new setLabelText(SetLabelText);

                view.Invoke(setThis, view, txt);
            }
            else
            {
                view.Text = txt;
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

        #endregion Delegate Methods

        /// <summary>
        /// Form Main Process
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            //0.Global variable initialization
            currentStage = Stages.IDLE;
            ClearLastScan();

            //1.open the logger first to record all the informations
            logger = new SimpleLogger();

            //2.open the platform controller
            pcontroller = new PlatformController();
            //comment when dont have controller connected
            OpenController("COM3");

            //addtional encoder controller
            encoder = new PlatformController();
            OpenEncoderController("COM5");

            //3.open the UHF reader 
            reader = new ReaderMethod();
            //comment when dont have reader connected
            OpenUHFReader("COM4");
            //read antenna power



            //4.request token from server
            //comment when the internet is off
            client = new APIClient();

            //5.Open the camera and AIP
            camera = new CameraCV();
            //adjust the camera device name in init
            if (camera.Init("C922"))
            {
                logger.Info("Open Camera success");
                OpenCamera();
            }
            else
            {
                logger.Error("Open Camera fail, please check the USB cable.");
            }

            //调试用，直接对接百度API
            CVclient = new BaiduFace();

            //6.Init the speech TTS from baidu
            speech = new BaiduSpeech();

            //7.Reset the arduino to initial state
            pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.Reset);

            //Thread.Sleep(15000); //wait 15s to finish reset
            UpdateStateMachine(Stages.IDLE);

            //pictureBoxWelcome is for show image when no one in the cashier
            //set to false when debugging
            pictureBoxWelcome.Visible = false;

            //setup a timer to reduce time cost on face identify
            pictureboxRefreshTimer = new System.Timers.Timer();
            pictureboxRefreshTimer.Interval = 1000;
            pictureboxRefreshTimer.Elapsed += new System.Timers.ElapsedEventHandler(CameraFastRefresh);
            pictureboxRefreshTimer.AutoReset = true;

            //启动timer，周期刷新faceError
            faceErrorResetTimer = new System.Timers.Timer();
            faceErrorResetTimer.Elapsed += new System.Timers.ElapsedEventHandler(faceErrorReset);
            faceErrorResetTimer.AutoReset = true;
            faceErrorResetTimer.Interval = 10000;
            faceErrorResetTimer.Start();
        }

        #region Camera
        /// <summary>
        /// 打开摄像头
        /// </summary>
        private void OpenCamera()
        {
            camera.ImageCaptured += ShowImageInPictureBox;
            camera.FaceCaptured += ShowFaceInPictureBox;
            camera.localFaceDetect = true;
            camera.localFaceDistanceFilter = false;

            camera.Open();
            CameraCV.StartProcessOnFrame();
        }

        /// <summary>
        /// 关闭摄像头
        /// </summary>
        private void CloseCamera()
        {
            if (camera.isOpen)
            {
                CameraCV.SkipProcessOnFrame();
                camera.FaceCaptured -= ShowFaceInPictureBox;
                camera.Close();
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
        /// 实时在picturebox中显示摄像头捕获图像
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowImageInPictureBox(object sender, CameraCV.CameraEventArgs e)
        {
            if (e.Image != null)
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
#if false
            if (e.FullImage != null)
            {
                SetPictureBox(pictureBox1, e.FullImage);
                SetPictureBox(pictureBox2, e.FaceImage);
                Bitmap bitmap = (Bitmap)e.FaceImage.Clone();

                try
                {
                    //save the face for API client
                    bitmap.Save("./face.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

                    APIClient.UserInfo userinfo = client.EntryByFace("face.jpg");
                    if (userinfo.nickName != string.Empty)
                    {
                        SetTileButton(bunifuTileButtonFace, userinfo);
                        //if user face is found, skip process
                        CameraCV.SkipProcessOnFrame();
                        //save this user to global variable
                        lastUserinfo = userinfo;
                    }
                    System.IO.File.Delete("./face.jpg");
                }
                catch(Exception ex)
                {
                    logger.Error("Camera Event search faces failed, exception: " + ex.ToString());
                }
            }
#else
            if (e.FaceImage != null)
            {
                Bitmap bitmap = (Bitmap)e.FaceImage.Clone();
                //change face search from server
                //if (faceIdentify(bitmap))
                if (faceIdentify_usingAIP(bitmap))
                {
                    //play welcome sound with nickname
                    //PlayWelcomSound(bunifuTileButtonFace.LabelText);

                    SetPictureBox(pictureBoxFace, e.FaceImage);
                    SetPictureBoxVisibility(pictureBoxFace, true);

                    faceError = 0;

                    //stop the refresh timer
                    pictureboxRefreshTimer.Stop();

                    //update state machine
                    UpdateStateMachine(Stages.CloseDoor);
                }
                else
                {
                    SetPictureBoxVisibility(pictureBoxFace, false);
                    faceError++;
                    if (faceError > 3)
                    {
                        PlayErrorSound();
                        faceError = 0;
                    }
                }
            }
#endif
        }

        /// <summary>
        /// 刷脸识别流程处理，直接调用aip节省阿里云流量费用
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
                        SetTileButton(bunifuTileButtonFace, userinfo);
                        SetTileButtonVisable(bunifuTileButtonFace, true);

                        PlayWelcomSound(userinfo.nickName);

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

        /// <summary>
        /// 刷新picturebox任务，在timer中调用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CameraFastRefresh(object sender, EventArgs e)
        {
            if (camera.isOpen == false)
            {
                return;
            }

            if (camera.isProcessing == false)
            {
                CameraCV.StartProcessOnFrame();
            }
        }
        #endregion Camera

        #region UHFReader
        /// <summary>
        /// Open UHF Reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenUHFReader(string portname)
        {
            string strException = string.Empty;

            if (portname == string.Empty)
            {
                return;
            }

            //regiser all the callback before open the serialport
            reader.m_OnInventoryTag = onInventoryTag;
            reader.m_OnExeCMDStatus = onExeCMDStatus;
            reader.m_RefreshSetting = refreshSetting;
            reader.m_OnGetInventoryBufferTagCount = onGetInventoryBufferTagCount;

            int nRet = reader.OpenCom(portname, 115200, out strException);
            if (nRet != 0)
            {
                string strLog = "Connection failed, failure cause: " + strException;
                logger.Error(strLog);

                return;
            }
            else
            {
                string strLog = "Connect UHF reader serialport success";
                logger.Info(strLog);
            }

            if(SetWorkAntennas()!=true)
            {
                logger.Error("Set UHF reader working atennas failed");
            }

            byte pow = 29;
            if (SetOutputPower(output_power)!=true)
            {
                logger.Error("Set UHF reader output power failed!");
            }
        }

        private bool SetWorkAntennas()
        {
            return (reader.SetWorkAntenna(0xff, 0x00) == 0) && (reader.SetWorkAntenna(0xff, 0x01) == 0);
        }

        private bool SetOutputPower(byte power)
        {
            byte val = power;
            if(val > 0x21)
            {
                val = 0x21;
            }
            return (reader.SetOutputPower(0xff, val) == 0);
        }

        /// <summary>
        /// Close UHF Reader
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseUHFReader()
        {
            reader.CloseCom();
            //Console.WriteLine("close Serial port!");
            logger.Info("Close UHF reader Serial port!");
        }

        #region UHFReaderCallbacks
        void onInventoryTag(RXInventoryTag tag)
        {
            logger.Info("Inventory EPC:" + tag.strEPC);
            //lastScanTagList.totalNum += 1;
            lastScanTagList.EPC.Add(TextBoxMethod.RemoveSpaceFromString(tag.strEPC));
            //lastScanTagList.EPC.Add(new APIClient.Tag_EPC() { EPC = TextBoxMethod.RemoveSpaceFromString(tag.strEPC) });
        }

        void refreshSetting(ReaderSetting readerSetting)
        {
            logger.Info("Version:" + readerSetting.btMajor + "." + readerSetting.btMinor);
        }

        void onExeCMDStatus(byte cmd, byte status)
        {
            logger.Info("CMD execute CMD:" + CMD.format(cmd) + "++Status code:" + ERROR.format(status));
        }

        void onGetInventoryBufferTagCount(int nTagCount)
        {
            //save to global variable
            lastScanTagCount = nTagCount;
            logger.Info("Get Inventory Buffer Tag Count" + nTagCount);
        }
        #endregion

        /// <summary>
        /// Start UHF Reader Inventory process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartIventory()
        {
            reader.Inventory((byte)0xFF, (byte)0x01);
            //logger.Info("Start Inventory message sent");
        }

        /// <summary>
        /// Get Inventory Buffer Tag Count number
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetInventoryBufferTagCount()
        {
            reader.GetInventoryBufferTagCount((byte)0xFF);
        }

        /// <summary>
        /// Stop UHF Reader Inventory process and Get the cached results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopIventoryAndGetResults()
        {
            reader.GetAndResetInventoryBuffer((byte)0xff);
            //logger.Info("Stop Inventory message sent");
        }
        #endregion UHFReader

        #region Arduino Main Controller
        /// <summary>
        /// Open serial port of the arduino controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenController(string portname)
        {
            if(portname == string.Empty)
            {
                return;
            }
            try
            {
                pcontroller.Open(portname, 57600);
                pcontroller.MsgRecieved += ProcessControllerMessage;
                logger.Info("Open Firmware Controller Success");
            }
            catch(Exception ex)
            {
                logger.Error("Open Firmware Controller Failed: " + ex.ToString());
            }
        }

        /// <summary>
        /// Close the serialport of arduino controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseController()
        {
            try
            {
                pcontroller.MsgRecieved -= ProcessControllerMessage;
                pcontroller.PortListener.Dispose();
                pcontroller.Close();
                logger.Info("Close Firmware Controller Success");
            }
            catch (Exception ex)
            {
                logger.Error("Close Firmware Contronller Failed: " + ex.ToString());
            }
        }

        /// <summary>
        /// process on messages recieved from the serial port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessControllerMessage(object sender, PlatformController.MsgEventArgs e)
        {
            CommunicationProtocol.CMDTYPE ct = e.msg.cmdType;

            if (ct == CommunicationProtocol.CMDTYPE.DebugMessage)
            {
                //记录debug char数组信息
                logger.Info("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                + "with Content: " + TextBoxMethod.ByteCharArrayToString(e.msg.payload, 0, e.msg.len));
            }
            else
            {
                //记录其它消息payload byte数组信息
                logger.Info("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                + "with PAYLOAD " + TextBoxMethod.ByteArrayToString(e.msg.payload, 0, e.msg.len));
            }

            switch (ct)
            {
                case CommunicationProtocol.CMDTYPE.DetectCustomerIn:
                    {
                        //because multiple detect customer inside will be raised
                        //Send response message to arduino
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerInRespond);

                        //only update state machine when system is idle
                        if (currentStage == Stages.IDLE)
                        {
                            UpdateStateMachine(Stages.CustomerInside);
                        }
                        else
                        {
                            logger.Error("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                + "Except current stage is IDLE, But current stage is "
                                + Enum.GetName(typeof(Stages), currentStage));
                        }
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.CloseDoorCmdRespond:
                    {
                        if (currentStage == Stages.WaitForDoorClosed)
                        {
                            UpdateStateMachine(Stages.DoCashier);
                        }
                        else if (currentStage == Stages.WaitForDoorClosedWithNobodyInside)
                        {
                            UpdateStateMachine(Stages.OpenDoorToInward);
                        }
                        else
                        {
                            logger.Error("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                + "Except current stage is WaitForDoorClosed, But current stage is "
                                + Enum.GetName(typeof(Stages), currentStage));
                        }
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.OpenDoorCmdRespond:
                    {
                        if (currentStage == Stages.OpenDoorToOutward)
                        {
                            UpdateStateMachine(Stages.WaitForCustomerLeave);
                        }
                        else if (currentStage == Stages.OpenDoorToInward)
                        {
                            UpdateStateMachine(Stages.IDLE);
                        }
                        else
                        {
                            logger.Error("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                    + "Except current stage is OpenDoorToOutward/OpenDoorToInward, But current stage is "
                                    + Enum.GetName(typeof(Stages), currentStage));
                        }
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.DetectCustomerOut:
                    {
                        //send response to arduino here because receive too many
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerOutRespond);

                        if ((currentStage == Stages.WaitForCustomerLeave) || (currentStage == Stages.OpenDoorToOutward))
                        {
                            UpdateStateMachine(Stages.CloseDoorWithNobodyInside);
                        }
                        //else
                        //{
                        //    logger.Error("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                        //        + "Except current stage is WaitForCustomerLeave, But current stage is "
                        //        + Enum.GetName(typeof(Stages), currentStage));
                        //}
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.DebugMessage:
                    {
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.EmgMsg:
                    {
                        logger.Warning("Recieve Emergence Message, Into Emergence Process...");
                        //UpdateStateMachine(Stages.EmergenceAlarm);
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.EmgRelease:
                    {
                        logger.Warning("Recieve Emergence Release Message, Back to Normal...");
                        //UpdateStateMachine(Stages.EmergenceRelease);
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.ErrorMsg:
                    {
                        PlatformController.ERROR_CODE ecode = (PlatformController.ERROR_CODE)e.msg.payload[0];
                        logger.Error("Recieve Error Message, ERROR_CODE = " + Enum.GetName(typeof(PlatformController.ERROR_CODE), ecode));

                        ErrorMessageHandler(ecode);

                        break;
                    }
                default: break;
            }
        }

        /// <summary>
        /// process on error messages recieved from the serial port
        /// </summary>
        /// <param name="code"></param>
        private void ErrorMessageHandler(PlatformController.ERROR_CODE code)
        {
            switch (code)
            {
                case PlatformController.ERROR_CODE.CloseDoorRetryTooManyTimes:
                    {
                        //play some notify sound
                        if (currentStage == Stages.WaitForDoorClosedWithNobodyInside)
                        {
                            //speech.Tts2Play(inverseInterWarningStr);
                            //retry close door
                            UpdateStateMachine(Stages.CloseDoorWithNobodyInside);
                        }
                        else if (currentStage == Stages.WaitForDoorClosed)
                        {
                            speech.Tts2Play(closeDoorRetryErrorStr);
                            //retry close door
                            UpdateStateMachine(Stages.CloseDoor);
                        }

                        break;
                    }
                case PlatformController.ERROR_CODE.GetDetectCustomerInResponseRetryTooManyTimes:
                case PlatformController.ERROR_CODE.GetDetectCustomerOutResponseRetryTooManyTimes:
                    {
                        //when this error happends, this program should not be alive

                        break;
                    }
                case PlatformController.ERROR_CODE.OpenDoorInwardRetryTooManyTimes:
                    {
                        //play some notify sound
                        speech.Tts2Play(openDoorInwardRetryErrorStr);
                        UpdateStateMachine(Stages.OpenDoorToInward);
                        break;
                    }
                case PlatformController.ERROR_CODE.OpenDoorOutwardRetryTooManyTimes:
                    {
                        //play some notify sound
                        speech.Tts2Play(openDoorOutwardRetryErrorStr);

                        break;
                    }
                default: break;
            }
        }
        #endregion Arduino Main Controller

        #region Arduino Encoder
        private void OpenEncoderController(string portname)
        {
            if (portname == string.Empty)
            {
                return;
            }
            try
            {
                encoder.Open(portname, 9600);
                encoder.MsgRecieved += ProcessEncoderMessage;
                logger.Info("Open Encoder Controller Success");
            }
            catch (Exception ex)
            {
                logger.Error("Open Encoder Controller Failed: " + ex.ToString());
            }
        }

        private void ProcessEncoderMessage(object sender, PlatformController.MsgEventArgs e)
        {
            CommunicationProtocol.CMDTYPE ct = e.msg.cmdType;

            switch (ct)
            {
                case CommunicationProtocol.CMDTYPE.EncoderUpdate:
                    {
                        lastEncoderValue = BitConverter.ToInt32(e.msg.payload, 0);
                        SetToolStatusBar(statusStrip1, encoderStatusValue, lastEncoderValue.ToString());
                        logger.Info("Recevied Encoder Value: " + lastEncoderValue.ToString());
                        break;
                    }
                default:break;
            }
        }

        private void CloseEncoderController()
        {
            try
            {
                encoder.Close();
                logger.Info("Close Firmware Controller Success");
            }
            catch (Exception ex)
            {
                logger.Error("Close Firmware Contronller Failed: " + ex.ToString());
            }
        }

        private void ClearEncoderAbsValue()
        {
            encoder.SendMessage(CommunicationProtocol.CMDTYPE.ResetEncoder);
            lastEncoderValue = 0;
        }

        #endregion Arduino Encoder

        #region QRCODE
        /// <summary>
        /// cut spire logo from the generated QR code
        /// </summary>
        /// <param name="source"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public Bitmap CropImage(Bitmap source, Rectangle section)
        {
            // An empty bitmap which will hold the cropped image
            Bitmap bmp = new Bitmap(section.Width, section.Height);

            Graphics g = Graphics.FromImage(bmp);

            // Draw the given area (section) of the source image
            // at location 0,0 on the empty bitmap (bmp)
            g.DrawImage(source, 0, 0, section, GraphicsUnit.Pixel);

            return bmp;
        }

        /// <summary>
        /// convert url to QR code and show image on picturebox for scan
        /// </summary>
        /// <param name="alipay_url"></param>
        /// <param name="wechatpay_url"></param>
        private void ShowQRCode(string alipay_url, string wechatpay_url)
        {
            Bitmap amap = BarcodeGenerator.GenerateQRCode(alipay_url, "支付宝");
            Bitmap bmap = BarcodeGenerator.GenerateQRCode(wechatpay_url, "微信支付");

            Point p = new Point(0, 25);
            Size s = new Size(145, 145);

            SetPictureBox(pictureBoxAlipay, CropImage(amap, new Rectangle(p, s)));
            SetPictureBox(pictureBoxWechatpay, CropImage(bmap, new Rectangle(p, s)));
        }
        #endregion QRCODE

        #region StateMachine

        /// <summary>
        /// State Machine Stages Definations
        /// </summary>
        public enum Stages
        {
            IDLE = 0,

            CustomerInside,
            FaceRecognize,
            CloseDoor,
            WaitForDoorClosed,
            DoCashier,
            WaitForScannerReport,
            GenerateOrder,
            WaitForPaymentFinish,
            OpenDoorToOutward,
            WaitForCustomerLeave,
            CloseDoorWithNobodyInside,
            WaitForDoorClosedWithNobodyInside,
            OpenDoorToInward,

            EmergenceAlarm,
            EmergenceRelease,

            ErrorDebug,
        }

        /// <summary>
        /// Update the State Machine
        /// </summary>
        /// <param name="newStage"></param>
        public void UpdateStateMachine(Stages newStage)
        {
            currentStage = newStage;

            //refresh state machine stage to status bar
            SetToolStatusBar(statusStrip1, doorStateValue, Enum.GetName(typeof(Stages), currentStage));

            switch (currentStage)
            {
                case Stages.IDLE:
                    {
                        //clear last status
                        ClearLastUser();
                        ClearLastScan();

                        SetPictureBoxVisibility(pictureBoxFace, false);
                        SetTileButtonVisable(bunifuTileButtonFace, false);

                        //ClearEncoderAbsValue();
                        break;
                    }
                case Stages.CustomerInside://raised by arduino message
                    {
                        speech.Tts2Play(face_identify_hint_str);

                        //move on to next stage
                        UpdateStateMachine(Stages.FaceRecognize);
                        break;
                    }
                case Stages.FaceRecognize:
                    {
                        //Open camera to do facial recognition
                        //CameraCV.StartProcessOnFrame();
                        //启动timer，周期刷新picturebox
                        pictureboxRefreshTimer.Start();
                        //will automatically stop when customer is recognized
                        break;
                    }
                case Stages.CloseDoor://raised by face identify
                    {
                        //alarm the door will closing
                        speech.Tts2Play(doorClosingHintStr);

                        //Send close door message to arduino
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.CloseDoorCmd);

                        //move on to next stage
                        UpdateStateMachine(Stages.WaitForDoorClosed);
                        break;
                    }
                case Stages.WaitForDoorClosed://raised by state machine
                    {
                        //Start Inventory
                        StartIventory();
                        //waiting for the door close while scanner is working background
                        break;
                    }
                case Stages.DoCashier://raised by arduino mesage door closed
                    {
                        //Get the count first before the result
                        GetInventoryBufferTagCount();

                        //move to next stage
                        UpdateStateMachine(Stages.WaitForScannerReport);

                        break;
                    }
                case Stages.WaitForScannerReport:
                    {
                        CheckScanTagCountWithTimeout();

                        if (lastScanTagCount == -1)
                        {
                            speech.Tts2Play(readerErrorHintStr);
                        }
                        else if(lastScanTagCount == 0)
                        {
                            StopIventoryAndGetResults();

                            //clear global variables, just in case
                            ClearLastScan();

                            //if no tag detect, let customer go outside directly
                            UpdateStateMachine(Stages.OpenDoorToOutward);
                        }
                        else if(lastScanTagCount > 0)
                        {
                            //stop Inventory and get the result
                            StopIventoryAndGetResults();

                            CheckScanTagDetailWithTimeout();
                            
                            speech.Tts2Play("检测到" + lastScanTagList.EPC.Count.ToString() + "个标签");

                            UpdateStateMachine(Stages.GenerateOrder);
                        }                        
                        break;
                    }
                case Stages.GenerateOrder://raised by state machine
                    {
                        speech.Tts2Play(cashierStartHintStr);
                        //check the tags and get the merchandises information
                        APIClient.MerchandiseInfoCashier[] infos = client.QueryMerchandiseInfo(lastScanTagList);

                        if ((infos.Count() == 1) && (infos[0].id == string.Empty))
                        {
                            logger.Error("UHF Reader scan tag existed, but cannot query any merchandise info from server !!!");
                            speech.Tts2Play("奇怪，检测到的标签不在库里");
                        }
                        else
                        {
                            //show on screen
                            SetDataGridViewInfo(bunifuCustomMerchandiseDataGrid, infos);
                            //generate order request
                            string userid = lastUserinfo.id;
                            APIClient.Order order = new APIClient.Order(userid, infos);
                            //generate order online
                            lastTradeNo = client.CreateOrderNo(order);
                            //get qr code url from wechatpay and alipay
                            APIClient.Payment_Response resp = client.GetPaymentCodeUrl(lastUserinfo.id, lastTradeNo);
                            //show QR code on the screen
                            if(resp.status == "0") //&& not pay with balance and sucess with qr code
                            {
                                ShowQRCode(resp.alipay_code_url, resp.wechat_pay_code_url);

                                //add a countdown on the right side to alert user to scan and finish pay
                                //check the payment result every 1 seconds
                                //setup time for countdown
                                counter = 60;
                                SetLabelText(bunifuMetroTextbox1, counter.ToString());
                                speech.Tts2Play(paymentHintStr);

                                timer1.Tick += Timer1_Tick;
                                timer1.Start();

                                UpdateStateMachine(Stages.WaitForPaymentFinish);
                            }
                            //else if(pay with balance)
                            //{
                            //  UpdateStateMachine(Stages.OpenDoorToOutward);
                            //}
                            else if(resp.status == "1")
                            {
                                //play some error hint
                                speech.Tts2Play(codeGenerateFailHintStr);
                                logger.Error("Payment Get QR CODE URL failed !!!");
                            }
                            else if(resp.status == "2")
                            {
                                //pay with balance success
                                speech.Tts2Play(payWithBalanceSuccessHintStr);
                                UpdateStateMachine(Stages.OpenDoorToOutward);
                            }
                        }
                        break;
                    }
                case Stages.WaitForPaymentFinish://raised by state machine
                    {
                        //if user press cancel button
                        //TODO should be a seperate button handler
                        //call user cancel payment API

                        if (counter == 0)
                        {
                            //timeout
                            DialogResult result = MessageBox.Show("支付超时，是否需要重试？", "支付重试", MessageBoxButtons.RetryCancel);
                            if (result == DialogResult.Retry)
                            {
                                counter = 60;
                                SetLabelText(bunifuMetroTextbox1, counter.ToString());
                                speech.Tts2Play(paymentHintStr);

                                timer1.Start();
                                UpdateStateMachine(Stages.WaitForPaymentFinish);
                            }
                            else
                            {
                                timer1.Stop();
                                //should be equal to cancel button pressed
                                this.cancelBtn_Click(this, new EventArgs());
                            }
                        }
                        else
                        {
                            //Check Payment result API
                            bool result = client.CheckPaymentResult(lastUserinfo.id, lastTradeNo);
                            if(result == true)
                            {
                                //payment success
                                timer1.Stop();
                                //play success hint sound
                                speech.Tts2Play(payWithQRCodeSuccessHintStr);
                                //open door to let out
                                UpdateStateMachine(Stages.OpenDoorToOutward);
                            }
                            else
                            {
                                //do nothing, continue check working with timer
                            }
                        }
                        break;
                    }
                case Stages.OpenDoorToOutward://raised by state machine
                    {
                        //alarm the door will opening
                        speech.Tts2Play(doorOpeningHintStr);

                        pcontroller.SendOpenDoorMessage(PlatformController.DIRECTION.OUTWARDS);

                        //fix problem: people will exit the door before the door is fully opened
                        UpdateStateMachine(Stages.WaitForCustomerLeave);
                        break;
                    }
                case Stages.WaitForCustomerLeave:
                    //Not working: raised by arduino open door response message
                    //people will exit the door before the door is fully opened
                    {
                        //alarm the door will closing
                        speech.Tts2Play(customerLeaveHintStr);

                        //should be every 5s repeatly if customer not coming out

                        break;
                    }
                case Stages.CloseDoorWithNobodyInside://raised by arduino detect customer out message
                    {
                        //try to close the door
                        pcontroller.SendCloseDoorMessage();
                        UpdateStateMachine(Stages.WaitForDoorClosedWithNobodyInside);
                        break;
                    }
                case Stages.WaitForDoorClosedWithNobodyInside://raised by state machine
                    {
                        //pcontroller.SendCloseDoorMessage();
                        break;
                    }
                case Stages.OpenDoorToInward://raised by arduino close door response message
                    {
                        pcontroller.SendOpenDoorMessage(PlatformController.DIRECTION.INWARDS);
                        break;
                    }
                case Stages.EmergenceAlarm://raised by arduino emergence message
                    {
                        break;
                    }
                case Stages.EmergenceRelease://should raise by the emergence button is released
                    {
                        break;
                    }
            }
        }

        private void CheckScanTagCountWithTimeout()
        {
            //start a timer to recieve response from serialport
            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            while (lastScanTagCount == -1)
            {
                if (timeout.ElapsedMilliseconds >= 5000)
                {
                    timeout.Stop();
                    logger.Error("UHF Reader didnt recieve tag count response, Timeout !!!");
                    break;
                }
            }
            timeout.Stop();
        }

        private void CheckScanTagDetailWithTimeout()
        {
            //start a timer to deal with timeout
            Stopwatch timeout = new Stopwatch();
            timeout.Start();

            //waiting for the reader to report all the result
            while (lastScanTagList.EPC.Count < lastScanTagCount)
            {
                if (timeout.ElapsedMilliseconds >= 5000)
                {
                    timeout.Stop();
                    logger.Error("UHF Reader didnt recieve enough number tags response, Timeout !!!");
                    break;
                }
            }
            timeout.Stop();
        }

        /// <summary>
        /// countdown timer for payment timeout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer1_Tick(object sender, EventArgs e)
        {
            if(counter > 0)
            {
                counter--;
                SetLabelText(bunifuMetroTextbox1, counter.ToString());

                UpdateStateMachine(Stages.WaitForPaymentFinish);
            }
            else
            {
                timer1.Stop();
            }
        }

        #endregion StateMachine

        /// <summary>
        /// Close all the devices
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            CloseCamera();
            CloseUHFReader();
            CloseController();
        }

        

        private void button3_Click(object sender, EventArgs e)
        {
            string test1 = "https://alipay.com/test1/901291021048291";
            string test2 = "https://wechatpay.com/test2/2098139219321031";
            ShowQRCode(test1, test2);
        }


        private void genOrderBtn_Click(object sender, EventArgs e)
        {
            APIClient.TagInfoList testList = new APIClient.TagInfoList();
            //testList.totalNum = 3;
            testList.EPC.Add("E2806890000000022CD45865");
            testList.EPC.Add("E2806890000000022CD45873");
            testList.EPC.Add("E2806890000000022CD458C5");
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD34306" });
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD35126" });
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD341EE" });
            APIClient.MerchandiseInfoCashier[] infos = client.QueryMerchandiseInfo(testList);
            APIClient.Order order = new APIClient.Order("1", infos);
            //generate order online
            string tradeNo = client.CreateOrderNo(order);
            //get qr code url from wechatpay and alipay
            APIClient.Payment_Response resp = client.GetPaymentCodeUrl("1", tradeNo);

            MessageBox.Show(resp.status.ToString());
        }

        /// <summary>
        /// 商品信息确认按钮，进入支付状态
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmBtn_Click(object sender, EventArgs e)
        {
            //UpdateStateMachine(Stages.OpenDoorToOutward);
        }

        /// <summary>
        /// 取消支付按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelBtn_Click(object sender, EventArgs e)
        {
            speech.Tts2Play(paymentCanceledHintStr);

            UpdateStateMachine(Stages.OpenDoorToInward);

            ClearLastScan();
            ClearLastUser();
            bool ret = client.CancelPayment(lastUserinfo.id, lastTradeNo);
            if (ret)
            {
                lastTradeNo = string.Empty;
                logger.Info("Order canceled successful");
            }
            else
            {
                logger.Error("Order cancel failed");
            }
        }

        /// <summary>
        /// 播放刷脸成功迎宾语
        /// </summary>
        /// <param name="welcome"></param>
        private void PlayWelcomSound(string nickname)
        {
            DateTime tmCur = DateTime.Now;

            if (tmCur.Hour < 8 || tmCur.Hour > 18)
            {
                speech.Tts2Play("晚上好" + nickname + customerWelcomeHintStr);
            }
            else if (tmCur.Hour >= 8 && tmCur.Hour < 12)
            {
                speech.Tts2Play("上午好" + nickname + customerWelcomeHintStr);
            }
            else
            {
                speech.Tts2Play("下午好" + nickname + customerWelcomeHintStr);
            }
        }

        /// <summary>
        /// 播放刷脸错误语音
        /// </summary>
        /// <param name="error"></param>
        private void PlayErrorSound()
        {
            speech.Tts2Play(face_identify_error_str);
        }

        private void TestResetBtn_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.Reset);
            UpdateStateMachine(Stages.IDLE);
        }

        private void TestFaceBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.FaceRecognize);
        }

        private void TestCloseBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.CloseDoor);
        }

        private void TestDoCashierBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.DoCashier);
        }

        private void TestOpenOutBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.OpenDoorToInward);
        }

        private void TestCloseNobodyBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.CloseDoorWithNobodyInside);
        }

        private void TestOpenInBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.OpenDoorToInward);
        }
    }
}
