using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.ComponentModel;

namespace EntranceGate
{
    class PlatformController
    {
        /// <summary>
        /// 串口声明
        /// </summary>
        static private SerialPort controlPort;        

        /// <summary>
        /// Event类定义
        /// </summary>
        public class MsgEventArgs : EventArgs
        {
            public CommunicationProtocol.DataPacket msg { get; set; }
        }

        /// <summary>
        /// 串口接收到数据包时发生
        /// </summary>
        public event EventHandler<MsgEventArgs> MsgRecieved;//register event

        /// <summary>
        /// 用于接收串口数据的线程
        /// </summary>
        public BackgroundWorker PortListener = new BackgroundWorker();

        /// <summary>
        /// 内部公开方法
        /// </summary>
        /// <param name="msgIn"></param>
        protected virtual void OnMsgRecieved(CommunicationProtocol.DataPacket msgIn)
        {
            if (MsgRecieved != null)
            {
                MsgRecieved(this, new MsgEventArgs() { msg = msgIn });
            }
        }

        /// <summary>
        /// 线程处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortListener_DoWork(object sender, DoWorkEventArgs e)
        {
            //receive data from serial port
            GetPortData();            
        }

        /// <summary>
        /// 打开函数
        /// </summary>
        /// <param name="portname"></param>
        /// <param name="bdrate"></param>
        public void Open(string portname, int bdrate)
        {
            controlPort = new SerialPort(portname, bdrate);

            if (controlPort.IsOpen == true)
            {
                controlPort.Close();
                controlPort.Open();
            }
            else
            {
                controlPort.Open();
            }
            //open another thread to receive data from serialport
            PortListener = new BackgroundWorker();
            PortListener.WorkerReportsProgress = false;
            PortListener.WorkerSupportsCancellation = true;
            PortListener.DoWork += PortListener_DoWork;
            PortListener.RunWorkerAsync();
        }

        public void Close()
        {
            if (controlPort.IsOpen == true)
            {
                controlPort.Close();
                PortListener.DoWork -= PortListener_DoWork;
                PortListener.Dispose();
                //PortListener.CancelAsync();
                //while(PortListener.CancellationPending)
                //{
                //    System.Threading.Thread.Sleep(100);
                //}
            }
        }

        #region ReceiverThread
        /// <summary>
        /// 译码状态机状态枚举
        /// </summary>
        public enum DecodeState
        {
            SearchForHeader1,
            SearchForHeader2,
            SearchForCmdType,
            SearchForLen,
            CheckEnder,
            GetContent,
        }

        /// <summary>
        /// 接收数据译码函数
        /// </summary>
        public void GetPortData()
        {
            //test function to display any log information from Serial Ports
            List<byte> buffer = new List<byte>();//this gives easy access to handy functions in order to process buffer
            Queue<byte> FIFO_Buffer = new Queue<byte>(); //create FIFO type bufffer
            CommunicationProtocol.DataPacket packet = new CommunicationProtocol.DataPacket();
            packet.dataReady = false;
            DecodeState currentDecodeState = DecodeState.SearchForHeader1;
            bool ThreadRunning = true;
            while (controlPort.IsOpen && ThreadRunning)
            {
                try
                {
                    int data = controlPort.ReadByte();
                    if (data != -1)
                    {
                        FIFO_Buffer.Enqueue((byte)data);
                    }
                    ProcessData(ref FIFO_Buffer, ref currentDecodeState, ref packet);

                    if (packet.dataReady)
                    {
                        //raise event
                        if (MsgRecieved != null)
                        {
                            MsgRecieved(this, new MsgEventArgs() { msg = packet });//raise event
                        }
                        packet = new CommunicationProtocol.DataPacket();
                        packet.dataReady = false;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    ThreadRunning = false;
                }

            }
        }

        /// <summary>
        /// 接收数据包格式处理
        /// </summary>
        /// <param name="dataStream"></param>
        /// <param name="state"></param>
        /// <param name="packet"></param>
        static public void ProcessData(ref Queue<byte> dataStream, ref DecodeState state, ref CommunicationProtocol.DataPacket packet)
        {
            //Todo Process fifo data stream from headtracking port
            bool stopSerching = false;
            while (dataStream.Count > 0 && stopSerching == false)
            {
                switch (state)
                {
                    case DecodeState.SearchForHeader1:
                        if (dataStream.Count > 0)
                        {
                            byte data = dataStream.Dequeue();
                            if (data == 0xeb) { state = DecodeState.SearchForHeader2; }
                        }

                        break;

                    case DecodeState.SearchForHeader2:
                        if (dataStream.Count > 0)
                        {
                            byte data = dataStream.Dequeue();
                            if (data == 0x90) { state = DecodeState.SearchForCmdType; }
                        }
                        break;

                    case DecodeState.SearchForCmdType:
                        if (dataStream.Count > 0)
                        {
                            byte data = dataStream.Dequeue();
                            packet.cmdType = (CommunicationProtocol.CMDTYPE)data;
                            state = DecodeState.SearchForLen;
                        }
                        break;

                    case DecodeState.SearchForLen:
                        if (dataStream.Count > 0)
                        {
                            byte data = dataStream.Dequeue();
                            packet.len = data;
                            state = DecodeState.GetContent;
                        }
                        break;

                    case DecodeState.GetContent:
                        if (dataStream.Count >= packet.len)
                        {
                            packet.payload = new byte[packet.len];
                            for (int i = 0; i < packet.len; i++)
                            {
                                packet.payload[i] = dataStream.Dequeue();
                            }
                            state = DecodeState.CheckEnder;
                        }
                        else
                        {
                            stopSerching = true;
                        }
                        break;

                    case DecodeState.CheckEnder:
                        if (dataStream.Count > 0)
                        {
                            byte data = dataStream.Dequeue();
                            if (data == 0xfe)
                            {
                                packet.dataReady = true;
                                state = DecodeState.SearchForHeader1;
                                stopSerching = true;
                            }
                            else
                            {
                                packet = new CommunicationProtocol.DataPacket();
                                state = DecodeState.SearchForHeader1;
                                stopSerching = true;
                            }
                        }
                        break;
                }
            }
        }
        #endregion ReceiverThread

        #region SendCommandToFirmware
        /// <summary>
        /// 串口发送消息函数
        /// </summary>
        /// <param name="ct"></param>
        public void SendMessage(CommunicationProtocol.CMDTYPE ct)
        {
            CommunicationProtocol.ProtocolMessage sMsg = new CommunicationProtocol.ProtocolMessage(ct);
            if (controlPort.IsOpen)
            {
                controlPort.Write(sMsg.MsgFrame, 0, sMsg.MsgTotalLength);
                controlPort.BaseStream.Flush();
            }
        }        

        /// <summary>
        /// 发送闸机开门消息函数
        /// </summary>
        public void SendDoorOpenMessage()
        {
            SendMessage(CommunicationProtocol.CMDTYPE.OpenGate);
        }
        #endregion SendCommandToFirmware
    }
}
