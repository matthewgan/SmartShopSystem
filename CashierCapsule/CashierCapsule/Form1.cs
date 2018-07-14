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
        /// 底层硬件控制器Arduino
        /// </summary>
        PlatformController pcontroller;

        /// <summary>
        /// 摄像头处理模块
        /// </summary>
        CameraCV camera;

        /// <summary>
        /// TTS语音合成
        /// </summary>
        BaiduSpeech speech;
        #endregion Instances of Components

        #region 语音合成提示语句
        string doorClosingHintStr = "正在关门";
        string doorOpeningHintStr = "正在开门";
        string emgAlarmHintStr = "紧急按钮已触发,自动门已停止";
        string emgReleaseHintStr = "紧急按钮已释放,自动门恢复正常";
        string cameraHintStr = "请正对屏幕和摄像头,即将进行人脸扫描";
        string paymentHintStr = "请在60秒内完成付款,如您想返回店内,请按右下角取消按钮";
        string customerWelcomeHintStr = "欢迎光临物掌柜";
        string customerLeaveHintStr = "离开时请注意下坡间隙,期待您的再次光临";
        string inverseInterWarningStr = "请离开收银出口，从左侧闸机进去购物区，谢谢您的配合";
        string cashierStartHintStr = "正在生成您的订单，请稍后";
        string closeDoorRetryErrorStr = "门总是关不上啊，请收银仓外面的人退后一点";
        string openDoorInwardRetryErrorStr = "转门朝内打开失败啦";
        string openDoorOutwardRetryErrorStr = "转门朝外打开失败啦";
        string codeGenerateFailHintStr = "生成支付码失败啦";
        string payWithBalanceSuccessHintStr = "储值支付成功";
        string payWithQRCodeSuccessHintStr = "扫码支付成功";
        string paymentCanceledHintStr = "支付已取消";

        // for Debug
        string readerErrorHintStr = "读卡器读取结果超时，请检修读卡器";
        #endregion 语音合成提示语句

        #region Global Variables
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
            //OpenController("COM3");

            //3.open the UHF reader 
            reader = new ReaderMethod();
            //comment when dont have reader connected
            //OpenUHFReader("COM4");

            //4.request token from server
            //comment when the internet is off
            client = new APIClient();

            //5.Open the camera
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

            //6.Init the speech TTS from baidu
            speech = new BaiduSpeech();

            //7.Reset the arduino to initial state
            //pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.Reset);

            //Thread.Sleep(15000); //wait 15s to finish reset

            //pictureBoxWelcome is for show image when no one in the cashier
            //set to false when debugging
            pictureBoxWelcome.Visible = false;
        }

        /// <summary>
        /// 打开摄像头
        /// </summary>
        private void OpenCamera()
        {
            //camera.ImageCaptured += ShowFaceInPictureBox;
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
        /// 收到摄像头event事件调用函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowFaceInPictureBox(object sender, CameraCV.CameraFaceEventArgs e)
        {
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
        }

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

            //pictureBoxAlipay.Image = CropImage(amap, new Rectangle(p, s));
            //pictureBoxWechatpay.Image = CropImage(bmap, new Rectangle(p, s));
            SetPictureBox(pictureBoxAlipay, CropImage(amap, new Rectangle(p, s)));
            SetPictureBox(pictureBoxWechatpay, CropImage(bmap, new Rectangle(p, s)));
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
                        else if(currentStage == Stages.WaitForDoorClosedWithNobodyInside)
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
                        if (currentStage == Stages.WaitForCustomerLeave)
                        {
                            UpdateStateMachine(Stages.CloseDoorWithNobodyInside);
                        }
                        else
                        {
                            logger.Error("Receieve Controller Message: CMDTYPE " + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                                + "Except current stage is WaitForCustomerLeave, But current stage is "
                                + Enum.GetName(typeof(Stages), currentStage));
                        }
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
                default:break;
            }
        }

        private void ErrorMessageHandler(PlatformController.ERROR_CODE code)
        {
            switch(code)
            {
                case PlatformController.ERROR_CODE.CloseDoorRetryTooManyTimes:
                    {
                        //play some notify sound
                        if (currentStage == Stages.WaitForDoorClosedWithNobodyInside)
                        {
                            speech.Tts2Play(inverseInterWarningStr);
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
                default:break;
            }
        }

        #region StateMachine
        /// <summary>
        /// Normal process: 
        ///     IDEL -> Customer Inside (Receieve Notify From Arduino, auto response) 
        ///     -> Close the door (Send command, Start Inventory)
        ///     -> Wait for the door closed (Receieve Notify From Arduino)
        ///     -> Do Cashier (Get Inventory Result, Request Merchandise Info, Generate Order and Show on Screen)
        ///     -> Wait Customer to Finish Payment (Autopay with balance, otherwise generate alipay and wechat pay QRcode on Screen)
        ///     -> Open Door to Outwards (Send message to Arduino)
        ///     -> Wait Customer to go outside (Receieve Notify From Arduino)
        ///     -> Open Door to Inwards (Send message to Arduino)
        ///     -> IDLE
        /// Emergence process:
        ///     Any Stage -> Emergence Happend (Receieve Emergence Message From Arduino)
        ///     -> Emergence Handling (Alarm in the shop, If payment is finished Let Customer Go Outwards, otherwise Inwards)
        ///     -> Open Door Inwards or Outwards depend on Payment Status
        ///     -> Freeze the system until Emergence Button is Released (Receieve Emergence Cancel Message From Arduino)
        ///     -> Open Door to Inwards (Reset the Alarm)
        ///     -> IDLE
        /// Error process: 
        ///     (Debug situation)
        ///     Any stage -> Error Happend （Receieve Error Message From Arduino）
        ///     -> Freeze the system until Reset (For Debugging)
        ///     (Working situation)
        ///     Any stage -> Error Happend (Only record error message to LOGGER)
        /// </summary>

        // State Machine Stages Definations
        public enum Stages
        {
            IDLE = 0,

            CustomerInside,
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
            EmergenceProcessing,
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



            switch (currentStage)
            {
                case Stages.IDLE:
                    {
                        break;
                    }
                case Stages.CustomerInside://raised by arduino message
                    {
                        speech.Tts2Play(customerWelcomeHintStr);

                        //Open camera to do facial recognition
                        ClearLastUser();
                        CameraCV.StartProcessOnFrame();
                        //will automatically stop when customer is recognized

                        //Send response message to arduino
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerInRespond);

                        //move on to next stage
                        UpdateStateMachine(Stages.CloseDoor);
                        break;
                    }
                case Stages.CloseDoor://raised by state machine
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
                        //start a timer to recieve response from serialport
                        Stopwatch timeout = new Stopwatch();
                        timeout.Start();

                        while(lastScanTagCount == -1)
                        {
                            if(timeout.ElapsedMilliseconds >= 5000)
                            {
                                timeout.Stop();
                                logger.Error("UHF Reader didnt recieve tag count response, Timeout !!!");
                                break;
                            }
                        }
                        timeout.Reset();

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

                            //start a timer to deal with timeout
                            //timeout.Reset();
                            timeout.Start();

                            //waiting for the reader to report all the result
                            while(lastScanTagList.EPC.Count < lastScanTagCount)
                            {
                                if (timeout.ElapsedMilliseconds >= 5000)
                                {
                                    timeout.Stop();
                                    logger.Error("UHF Reader didnt recieve enough number tags response, Timeout !!!");
                                    break;
                                }
                            }

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
                        }
                        else
                        {
                            //show on screen
                            SetDataGridViewInfo(bunifuCustomMerchandiseDataGrid, infos);
                            //generate order request
                            string userid = string.Empty;
                            if(lastUserinfo.id == string.Empty)
                            {

                            }
                            else
                            {
                                userid = lastUserinfo.id;
                            }
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

                        //if ()//payment is finished
                        //{
                        //    UpdateStateMachine(Stages.OpenDoorToOutward);
                        //}
                        //else if()//payment is canceled
                        //{
                        //    UpdateStateMachine(Stages.OpenDoorToInward);
                        //}
                        //else//if timeout and user want to keep waiting for payment
                        //{
                        //    UpdateStateMachine(Stages.WaitForPaymentFinish);
                        //}
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
                        //send response to arduino
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerOutRespond);
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
                        //alarm the door will closing
                        speech.Tts2Play(doorClosingHintStr);

                        pcontroller.SendOpenDoorMessage(PlatformController.DIRECTION.INWARDS);
                        break;
                    }

                case Stages.EmergenceAlarm://raised by arduino emergence message
                    {
                        //play alarm sound effect
                        //TODO
                        //speech.Tts2Play(emgAlarmHintStr);

                        UpdateStateMachine(Stages.EmergenceProcessing);

                        break;
                    }
                case Stages.EmergenceProcessing:
                    {
                        //showing emergence info on the screen
                        //guide customer how to handle this

                        break;
                    }
                case Stages.EmergenceRelease://should raise by the emergence button is released
                    {
                        //recover the whole system
                        //speech.Tts2Play(emgReleaseHintStr);

                        break;
                    }
            }
        }

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

        private void test1_Click(object sender, EventArgs e)
        {
            //simulate arduino send customer inside message
            UpdateStateMachine(Stages.CustomerInside);
        }

        private void test2_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.DoCashier);
        }

        private void test3_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.CloseDoorWithNobodyInside);
        }

        private void test4_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.IDLE);
        }

        private void test5_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.EmergenceAlarm);
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.EmergenceRelease);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            counter = 5;
            SetLabelText(bunifuMetroTextbox1, counter.ToString());
            speech.Tts2Play(paymentHintStr);

            timer1.Tick += Timer1_Tick;
            timer1.Start();

            UpdateStateMachine(Stages.WaitForPaymentFinish);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string test1 = "https://alipay.com/test1/901291021048291";
            string test2 = "https://wechatpay.com/test2/2098139219321031";
            ShowQRCode(test1, test2);
        }

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

        private void genOrderBtn_Click(object sender, EventArgs e)
        {
            APIClient.TagInfoList testList = new APIClient.TagInfoList();
            //testList.totalNum = 3;
            testList.EPC.Add("E2806890000000022CD34306");
            testList.EPC.Add("E2806890000000022CD35126");
            testList.EPC.Add("E2806890000000022CD341EE");
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD34306" });
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD35126" });
            //testList.EPC.Add(new APIClient.Tag_EPC() { EPC = "E2806890000000022CD341EE" });
            APIClient.MerchandiseInfoCashier[] infos = client.QueryMerchandiseInfo(testList);
            APIClient.Order order = new APIClient.Order("1", infos);
            //generate order online
            string tradeNo = client.CreateOrderNo(order);
            //get qr code url from wechatpay and alipay
            APIClient.Payment_Response resp = client.GetPaymentCodeUrl("1", tradeNo);
        }

        private void confirmBtn_Click(object sender, EventArgs e)
        {
            UpdateStateMachine(Stages.OpenDoorToOutward);
        }

        private void cancelBtn_Click(object sender, EventArgs e)
        {
            speech.Tts2Play(paymentCanceledHintStr);

            UpdateStateMachine(Stages.OpenDoorToInward);

            ClearLastScan();
            ClearLastUser();
            client.CancelPayment(lastTradeNo);
            lastTradeNo = string.Empty;
        }
    }
}
