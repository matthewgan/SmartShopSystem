using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO.Ports;

namespace CashierCapsule
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 底层硬件控制器Arduino
        /// </summary>
        PlatformController pcontroller;

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
                txt.ScrollToCaret(); //function for auto scroll
            }
        }

        public Form1()
        {
            InitializeComponent();

            string[] names = SerialPort.GetPortNames();
            for(int i=0;i<names.Length;i++)
            {
                comboBox1.Items.Add(names[i]);
            }
            if(comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            pcontroller = new PlatformController();

            openControllerBtn.Enabled = true;
            closeControllerBtn.Enabled = false;

            sendBtn1.Enabled = false;
            sendBtn2.Enabled = false;
            sendBtn3.Enabled = false;
            sendBtn4.Enabled = false;
            sendBtn5.Enabled = false;
        }

        /// <summary>
        /// Open serial port of the arduino controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void controllerOpenBtn_Click(object sender, EventArgs e)
        {
            string portname = comboBox1.SelectedItem.ToString();
            pcontroller.Open(portname, 9600);
            pcontroller.MsgRecieved += ProcessControllerMessage;

            openControllerBtn.Enabled = false;
            closeControllerBtn.Enabled = true;

            sendBtn1.Enabled = true;
            sendBtn2.Enabled = true;
            sendBtn3.Enabled = true;
            sendBtn4.Enabled = true;
            sendBtn5.Enabled = true;
        }

        /// <summary>
        /// Get messages from the serial port
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessControllerMessage(object sender, PlatformController.MsgEventArgs e)
        {
            CMDTYPE ct = e.msg.cmdType;

            string txt = "Recieved "
                    + Enum.GetName(typeof(CMDTYPE), ct)
                    + " message, payload: ";

            if (ct == CMDTYPE.DebugMessage)
            {
                for (int i = 0; i < e.msg.len; i++)
                {
                    txt += (char)e.msg.payload[i];
                }
            }
            else
            {

                txt += TextboxMethod.ByteArrayToString(e.msg.payload, 0, e.msg.len);
            }
#if false
            switch (ct)
            {
                case CMDTYPE.DetectCustomerIn:
                    {
                        txt += Enum.GetName(typeof(CMDTYPE), ct);
                        break;
                    }
                case CMDTYPE.DoorCloseMsg:
                    {
                        break;
                    }
                case CMDTYPE.OpenDoorCmdRespond:
                    {
                        break;
                    }
                case CMDTYPE.DetectCustomerOut:
                    {
                        break;
                    }
                case CMDTYPE.DebugMessage:
                    {
                        break;
                    }    
                case CMDTYPE.EmgMsg:
                    {
                        break;
                    }
                case CMDTYPE.ErrorMsg:
                    {
                        break;
                    }
            }
#endif
            SetDataText(richTextBox1, txt);
        }

        /// <summary>
        /// Close the serialport of arduino controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void controllerCloseBtn_Click(object sender, EventArgs e)
        {
            pcontroller.MsgRecieved -= ProcessControllerMessage;
            pcontroller.Close();

            openControllerBtn.Enabled = true;
            closeControllerBtn.Enabled = false;

            sendBtn1.Enabled = false;
            sendBtn2.Enabled = false;
            sendBtn3.Enabled = false;
            sendBtn4.Enabled = false;
            sendBtn5.Enabled = false;
        }

        private void sendBtn1_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CMDTYPE.DetectCustomerInRespond);
        }

        private void sendBtn2_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CMDTYPE.DoorClosedMsgRespond);
        }

        private void sendBtn3_Click(object sender, EventArgs e)
        {
            pcontroller.SendOpenDoorMessage(DIRECTION.INWARDS);
        }

        private void sendBtn4_Click(object sender, EventArgs e)
        {
            pcontroller.SendOpenDoorMessage(DIRECTION.OUTWARDS);
        }

        private void sendBtn5_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CMDTYPE.DetectCustomerOutRespond);
        }

        private void CleanBtn_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }
    }
}
