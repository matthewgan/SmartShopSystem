using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Reader;

namespace InStockSystem
{
    public partial class Form1 : Form
    {
        TX200Scanner scanner;

        SimpleLogger logger;

        APIClient client;

        ReaderMethod reader;

        public static int tagCount = 0;

        volatile bool isLoop = false;

        System.Timers.Timer UHFtimer;

        public volatile APIClient.MerchandiseInfoShow lastinfo = new APIClient.MerchandiseInfoShow();

        public volatile RXInventoryTag lasttag = new RXInventoryTag();

        /// <summary>
        /// RichTextBox委托赋值
        /// </summary>
        /// <param name="txt">TextBox</param>
        /// <param name="content">内容</param>
        delegate void setDataText(RichTextBox txt, string content);

        /// <summary>
        /// RichTextBox委托赋值函数
        /// </summary>
        /// <param name="txt">TextBox</param>
        /// <param name="content">内容</param>
        private void SetDataText(RichTextBox txt, string content)
        {
            if (txt.InvokeRequired)
            {
                setDataText setThis = new setDataText(SetDataText);

                txt.Invoke(setThis, txt, content);
            }
            else
            {
                txt.Text = content;
            }
        }

        /// <summary>
        /// DataGridView委托赋值
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        delegate void setDataGridViewInfo(DataGridView view, APIClient.MerchandiseInfoShow info);

        /// <summary>
        /// DataGridView委托赋值函数
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        private void SetDataGridViewInfo(DataGridView view, APIClient.MerchandiseInfoShow info)
        {
            if(view.InvokeRequired)
            {
                setDataGridViewInfo setThis = new setDataGridViewInfo(SetDataGridViewInfo);

                view.Invoke(setThis, view, info);
            }
            else
            {
                view.ColumnCount = 8;
                view.Columns[0].Name = "ID";
                view.Columns[1].Name = "店内码";
                view.Columns[2].Name = "条形码";
                view.Columns[3].Name = "名称";
                view.Columns[4].Name = "品牌";
                view.Columns[5].Name = "规模";
                view.Columns[6].Name = "厂家";
                view.Columns[7].Name = "单位";

                string[] row = new string[]
                {
                    info.ID,
                    info.Code,
                    info.Barcode,
                    info.Name,
                    info.Brand,
                    info.Scale,
                    info.Factory,
                    info.Unit
                };
                view.Rows.Clear();
                view.Rows.Add(row);
            }
        }

        /// <summary>
        /// DataGridView委托赋值
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        delegate void setDataGridViewTag(DataGridView view, APIClient.Tag tag, APIClient.MerchandiseInfoShow info, string status);

        /// <summary>
        /// DataGridView委托赋值函数
        /// </summary>
        /// <param name="view"></param>
        /// <param name="info"></param>
        private void SetDataGridViewTag(DataGridView view, APIClient.Tag tag, APIClient.MerchandiseInfoShow info, string status)
        {
            if (view.InvokeRequired)
            {
                setDataGridViewTag setThis = new setDataGridViewTag(SetDataGridViewTag);

                view.Invoke(setThis, view, tag, info, status);
            }
            else
            {
                view.ColumnCount = 7;
                view.Columns[0].Name = "ID";
                view.Columns[1].Name = "店内码";
                view.Columns[2].Name = "条形码";
                view.Columns[3].Name = "名称";
                view.Columns[4].Name = "EPC";
                view.Columns[5].Name = "TID";
                view.Columns[6].Name = "状态";
                //view.Columns[7].Name = "操作";

                string[] row = new string[]
                {
                    tag.merchandiseID,
                    info.Code,
                    info.Barcode,
                    info.Name,
                    tag.EPC,
                    tag.TID,
                    status,
                };
                //view.Rows.Clear();
                view.Rows.Add(row);

                DataGridViewButtonColumn btn = new DataGridViewButtonColumn();
                view.Columns.Add(btn);
                btn.HeaderText = "操作";
                if (status == "添加成功")
                    btn.Text = "点击可添加或删除";
                else if (status == "删除成功")
                    btn.Text = "点击可添加或删除";
                else
                {
                    btn.Text = "查看左侧错误提示";
                }
                btn.Name = "btn";
                btn.UseColumnTextForButtonValue = true;
            }
        }

        /// <summary>
        /// Form1窗体
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            logger = new SimpleLogger();

            scanner = new TX200Scanner();
            OpenScanner();

            client = new APIClient();

            reader = new ReaderMethod();

            UHFtimer = new System.Timers.Timer();
            UHFtimer.Interval = 3000;
            UHFtimer.Elapsed += new System.Timers.ElapsedEventHandler(ScanTimeout);

            //Register Callbacks
            reader.m_OnInventoryTag = onInventoryTag;
            reader.m_OnInventoryTagEnd = onInventoryTagEnd;
            reader.m_OnExeCMDStatus = onExeCMDStatus;
            reader.m_RefreshSetting = refreshSetting;
            reader.m_OnGetInventoryBufferTagCount = onGetInventoryBufferTagCount;

            openReader();
            //startInventory();
        }

        /// <summary>
        /// 打开条形码扫描器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void openScannerBtn_Click(object sender, EventArgs e)
        private void OpenScanner()
        {
            TX200Scanner.ScannerReturn ret = scanner.Open();
            if (ret == TX200Scanner.ScannerReturn.SUCCESS)
            {
                //注册处理任务函数
                scanner.CodeFound += ShowCodeInTextBox;
                scanner.CodeFound += ShowMerchandiseInfoInDatagrid;
                scanner.StartDecodeThread();
                logger.Info("Open Barcode Scanner success");

            }
            else
            {
                logger.Error("Open Barcode Scanner fail, failcode=" + ret.ToString());
            }
        }

        /// <summary>
        /// 关闭条形码扫描器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void closeScannerBtn_Click(object sender, EventArgs e)
        private void closeScanner()
        {
            scanner.StopDecodeThread();
            scanner.Close();
            logger.Info("Close Barcode Scanner");

        }

        /// <summary>
        /// 将最近一次条形码扫描结果显示在界面上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowCodeInTextBox(object sender, TX200Scanner.BarcodeScannerEventArgs e)
        {
            if (e.Code != "")
            {
                string code = e.Code.Trim('\0',' ');
                SetDataText(richTextBox1, code);
            }
        }

        /// <summary>
        /// 将条形码查询所得的商品信息显示在界面上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShowMerchandiseInfoInDatagrid(object sender, TX200Scanner.BarcodeScannerEventArgs e)
        {
            if (e.Code != "")
            {
                char[] trimchars = new char[2] { '\0', ' ' };
                string code = e.Code.Trim(trimchars);
                APIClient.MerchandiseInfoShow info = client.GetMerchandiseInfo(new APIClient.Barcode() { barcode = code });
                System.Threading.Thread.Sleep(3000);
                SetDataGridViewInfo(dataGridView1, info);
                //保存本次查询结果
                lastinfo = info;

                //启动RFID扫描器
                startInventory();
                //启动定时器，在后续3s内扫到RFID tag为有效
                UHFtimer.Stop();
                UHFtimer.Interval = 3000;
                UHFtimer.Start();
            }
        }

        /// <summary>
        /// 打开UHF RFID扫描器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void openReaderBtn_Click(object sender, EventArgs e)
        private void openReader()
        {
            string strException = string.Empty;

            int nRet = reader.OpenCom("COM3", 115200, out strException);
            if (nRet != 0)
            {
                string strLog = "Open UHF Reader Connection failed, failure cause: " + strException;
                logger.Error(strLog);

                return;
            }
            else
            {
                string strLog = "Open UHF Reader Connect serialport success";
                logger.Info(strLog);

            }
        }

        /// <summary>
        /// 关闭UHF RFID扫描器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void closeReaderBtn_Click(object sender, EventArgs e)
        private void closeReader()
        {
            reader.CloseCom();
            logger.Info("Close UHF Reader Serial port!");

        }

        /// <summary>
        /// 开始扫描RFID标签
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void startInventoryBtn_Click(object sender, EventArgs e)
        private void startInventory()
        {
            //reader.Inventory((byte)0xFF, (byte)0x05);
            //logger.Info("Start Inventory message sent");
            reader.InventoryReal((byte)0xFF, (byte)0xFF);
            isLoop = true;
            logger.Info("Start Inventory RealTime message sent");

        }

        /// <summary>
        /// 获取RFID标签扫描结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void getInventoryBtn_Click(object sender, EventArgs e)
        private void stopInventory()
        {
            //reader.GetAndResetInventoryBuffer((byte)0xff);
            //logger.Info("Stop Inventory message sent");
            isLoop = false;
        }

        #region UHFReaderCallbacks
        void onInventoryTag(RXInventoryTag tag)
        {
            lasttag = tag;
            string EPC = TextBoxMethod.RemoveSpaceFromString(tag.strEPC.Trim('\0'));
            string TID = TextBoxMethod.RemoveSpaceFromString(tag.strPC.Trim('\0'));
            SetDataText(richTextBox2, EPC);
            SetDataText(richTextBox3, TID);
            logger.Info("Inventory EPC:" + EPC);
            logger.Info("Inventory TID:" + TID);
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
            tagCount = nTagCount;
            logger.Info("Get Inventory Buffer Tag Count" + nTagCount);
        }

        void onInventoryTagEnd(RXInventoryTagEnd tagEnd)
        {
            if (isLoop)
            {
                reader.InventoryReal((byte)0xFF, (byte)0xFF);
            }
        }
        #endregion

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeScanner();
            stopInventory();
            closeReader();
        }

        private void ScanTimeout(object sender, System.Timers.ElapsedEventArgs e)
        {
            stopInventory();
        }

        
        private void timer1_Tick(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (lastinfo.ID == null)
            {
                return;
            }

            if (lasttag.strEPC == "")
            {
                return;
            }

            APIClient.Tag tag = new APIClient.Tag();
            tag.EPC = TextBoxMethod.RemoveSpaceFromString(lasttag.strEPC.Trim('\0'));
            tag.TID = TextBoxMethod.RemoveSpaceFromString(lasttag.strPC.Trim('\0'));
            tag.merchandiseID = lastinfo.ID;

            string status = string.Empty;

            if (client.CreateTagInStock(tag, out status))
            {
                stopInventory();
                SetDataGridViewTag(dataGridView2, tag, lastinfo, status);
            }
            else
            {
                stopInventory();
                SetDataGridViewTag(dataGridView2, tag, lastinfo, status);
            }

            System.Threading.Thread.Sleep(200);

            //clear all for next time
            lastinfo = new APIClient.MerchandiseInfoShow();
            lasttag = new RXInventoryTag();
            SetDataText(richTextBox1, string.Empty);
            SetDataText(richTextBox2, string.Empty);
            SetDataText(richTextBox3, string.Empty);
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex+1 == dataGridView2.RowCount)
            {
                return;
            }

            if(e.ColumnIndex == 7)
            {
                string msg = dataGridView2.Rows[e.RowIndex].Cells[6].Value.ToString();

                APIClient.Tag tag = new APIClient.Tag();
                tag.EPC = dataGridView2.Rows[e.RowIndex].Cells[4].Value.ToString();
                tag.TID = dataGridView2.Rows[e.RowIndex].Cells[5].Value.ToString();
                tag.merchandiseID = dataGridView2.Rows[e.RowIndex].Cells[0].Value.ToString();

                string status = string.Empty;

                if (msg == "添加成功")
                {
                    if(CancelAddTag(tag, out status))
                    {
                        dataGridView2.Rows[e.RowIndex].Cells[6].Value = status;
                        dataGridView2.Rows[e.RowIndex].Cells[7].Value = "点击可再次添加";
                    }
                }
                if(msg == "删除成功")
                {
                    if(RetryAddTag(tag, out status))
                    {
                        dataGridView2.Rows[e.RowIndex].Cells[6].Value = status;
                        dataGridView2.Rows[e.RowIndex].Cells[7].Value = "点击可撤销添加";
                    }
                }
            }
        }

        private bool RetryAddTag(APIClient.Tag tag, out string status)
        {
            //string status = string.Empty;
            return client.CreateTagInStock(tag, out status);
        }

        private bool CancelAddTag(APIClient.Tag tag, out string status)
        {
            return client.DeleteTagInStock(tag, out status);
        }
    }
}
