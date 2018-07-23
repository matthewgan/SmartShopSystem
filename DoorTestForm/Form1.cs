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

namespace DoorTestForm
{
    public partial class Form1 : Form
    {
        /// <summary>
        /// 底层硬件控制器Arduino
        /// </summary>
        PlatformController pcontroller;

        PlatformController encoder;

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
            for (int i = 0; i < names.Length; i++)
            {
                comboBox1.Items.Add(names[i]);
            }
            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }

            for (int i = 0; i < names.Length; i++)
            {
                comboBox2.Items.Add(names[i]);
            }
            if (comboBox2.Items.Count > 0)
            {
                comboBox2.SelectedIndex = 0;
            }

            encoder = new PlatformController();

            pcontroller = new PlatformController();

            openControllerBtn.Enabled = true;
            closeControllerBtn.Enabled = false;

            openEncBtn.Enabled = true;
            closeEncBtn.Enabled = false;

            sendBtn1.Enabled = false;
            sendBtn2.Enabled = false;
            sendBtn3.Enabled = false;
            sendBtn4.Enabled = false;
            sendBtn5.Enabled = false;
            sendBtn6.Enabled = false;
        }

        /// <summary>
        /// Open serial port of the arduino controller
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void openControllerBtn_Click(object sender, EventArgs e)
        {
            string portname = comboBox1.SelectedItem.ToString();
            pcontroller.Open(portname, 57600);
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
            CommunicationProtocol.CMDTYPE ct = e.msg.cmdType;

            string txt = "Recieved "
                    + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                    + " message, payload: ";

            if (ct == CommunicationProtocol.CMDTYPE.DebugMessage)
            {
                for (int i = 0; i < e.msg.len; i++)
                {
                    txt += (char)e.msg.payload[i];
                }
            }
            else
            {

                txt += TextBoxMethod.ByteArrayToString(e.msg.payload, 0, e.msg.len);
            }
#if true
            switch (ct)
            {
                case CommunicationProtocol.CMDTYPE.DetectCustomerIn:
                    {
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerInRespond);
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.OpenDoorCmdRespond:
                    {
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.DetectCustomerOut:
                    {
                        pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerOutRespond);
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.DebugMessage:
                    {
                        break;
                    }    
                case CommunicationProtocol.CMDTYPE.EmgMsg:
                    {
                        break;
                    }
                case CommunicationProtocol.CMDTYPE.ErrorMsg:
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
        private void closeControllerBtn_Click(object sender, EventArgs e)
        {
            pcontroller.MsgRecieved -= ProcessControllerMessage;
            pcontroller.PortListener.Dispose();
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
            pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerInRespond);
            SetDataText(richTextBox1, "Send Message Detect Customer In Response");
        }

        private void sendBtn2_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.CloseDoorCmd);
            SetDataText(richTextBox1, "Send Message Close Door");
        }

        private void sendBtn3_Click(object sender, EventArgs e)
        {
            pcontroller.SendOpenDoorMessage(PlatformController.DIRECTION.INWARDS);
            SetDataText(richTextBox1, "Send Message Open Door Inwards");
        }

        private void sendBtn4_Click(object sender, EventArgs e)
        {
            pcontroller.SendOpenDoorMessage(PlatformController.DIRECTION.OUTWARDS);
            SetDataText(richTextBox1, "Send Message Open Door Outwards");
        }

        private void sendBtn5_Click(object sender, EventArgs e)
        {
            pcontroller.SendMessage(CommunicationProtocol.CMDTYPE.DetectCustomerOutRespond);
            SetDataText(richTextBox1, "Send Message Detect Customer Out Response");
        }

        private void CleanBtn_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void openEncBtn_Click(object sender, EventArgs e)
        {
            string portname = comboBox1.SelectedItem.ToString();
            encoder.Open(portname, 9600);
            encoder.MsgRecieved += GetEncoderMessage;

            openEncBtn.Enabled = false;
            closeEncBtn.Enabled = true;

            sendBtn6.Enabled = true;
        }

        private void GetEncoderMessage(object sender, PlatformController.MsgEventArgs e)
        {
            CommunicationProtocol.CMDTYPE ct = e.msg.cmdType;

            string txt = "Recieved "
                    + Enum.GetName(typeof(CommunicationProtocol.CMDTYPE), ct)
                    + " message, payload: ";

            switch (ct)
            {
                case CommunicationProtocol.CMDTYPE.EncoderUpdate:
                    {
                        int val = BitConverter.ToInt32(e.msg.payload, 0);
                        SetDataText(richTextBox2, val.ToString());
                        break;
                    }
            }
        }

        private void closeEncBtn_Click(object sender, EventArgs e)
        {
            encoder.MsgRecieved -= GetEncoderMessage;
            encoder.PortListener.Dispose();
            encoder.Close();

            openEncBtn.Enabled = true;
            closeEncBtn.Enabled = false;

            sendBtn6.Enabled = false;
        }

        private void sendBtn6_Click(object sender, EventArgs e)
        {
            encoder.SendMessage(CommunicationProtocol.CMDTYPE.ResetEncoder);
        }
    }
}
