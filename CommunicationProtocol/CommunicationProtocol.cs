using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class CommunicationProtocol
{
    /// <summary>
    /// 命令字定义枚举
    /// </summary>
    public enum CMDTYPE
    {
        IDLE = 0,                   //IDLE
        DetectCustomerIn = 1,       // Arduino -> PC
        DetectCustomerInRespond,    // PC -> Arduino

        CloseDoorCmd,               // PC -> Arduino
        CloseDoorCmdRespond,        // Arduino -> PC

        OpenDoorCmd,                // PC -> Arduino, dir 0: inwards, 1: outwards
        OpenDoorCmdRespond,         // Arduino -> PC

        DetectCustomerOut,          // Arduino -> PC
        DetectCustomerOutRespond,   // PC -> Arduino

        ErrorMsg,                   // Arduino -> PC, happens when something strage detected, Eg: No one in the room but sensor trigger
        EmgMsg,                     // Arduino -> PC, when emergency button is pushed
        EmgRelease,                 // Arduino -> PC, when emergency button is released
        Reset,

        OpenGate = 0x41,
        HeartBeatMsg,

        ReadEncoderRequest = 0x81,
        ReadEncoderResponse,
        ResetEncoder,
        EncoderUpdate,

        DebugMessage = 0xFF,
    }

    /// <summary>
    /// 数据包定义
    /// </summary>
    public struct DataPacket
    {
        public bool dataReady;
        public CMDTYPE cmdType;
        public byte len;
        public byte[] payload;
    }

    /// <summary>
    /// 通信消息类
    /// </summary>
    public class ProtocolMessage
    {
        public int MsgTotalLength;
        public byte[] MsgHeader = new byte[2];
        public CMDTYPE MsgType;
        public int ContentLen;
        public byte[] MsgFrame = new byte[100];
        public byte MsgEnder = 0xfe;

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProtocolMessage()
        {
            MsgTotalLength = 5;
            MsgHeader[0] = 0xeb;
            MsgHeader[1] = 0x90;
            MsgEnder = 0xfe;
            MsgHeader.CopyTo(MsgFrame, 0);
            MsgType = 0;
            ContentLen = 0;
            MsgFrame[2] = System.Convert.ToByte(MsgType);
            MsgFrame[3] = System.Convert.ToByte(ContentLen);
            MsgFrame[4] = MsgEnder;
        }

        /// <summary>
        /// 仅有命令字的构造函数
        /// </summary>
        /// <param name="ct"></param>
        public ProtocolMessage(CMDTYPE ct)
        {
            MsgTotalLength = 5;
            MsgHeader[0] = 0xeb;
            MsgHeader[1] = 0x90;
            MsgEnder = 0xfe;
            MsgHeader.CopyTo(MsgFrame, 0);
            MsgType = ct;
            ContentLen = 0;
            MsgFrame[2] = System.Convert.ToByte(MsgType);
            MsgFrame[3] = System.Convert.ToByte(ContentLen);
            MsgFrame[4] = MsgEnder;
        }

        /// <summary>
        /// 单字节内容的构造函数
        /// </summary>
        /// <param name="MsgType"></param>
        /// <param name="MsgContent"></param>
        public ProtocolMessage(CMDTYPE MsgType, byte MsgContent)
        {
            MsgHeader[0] = 0xeb;
            MsgHeader[1] = 0x90;
            MsgHeader.CopyTo(MsgFrame, 0);
            this.MsgType = MsgType;
            ContentLen = sizeof(byte);
            MsgFrame[2] = System.Convert.ToByte(MsgType);
            MsgFrame[3] = System.Convert.ToByte(ContentLen);
            MsgFrame[4] = MsgContent;
            MsgTotalLength = ContentLen + 5;
            MsgFrame[MsgTotalLength - 1] = MsgEnder;
        }

        /// <summary>
        /// 多字节内容的构造函数
        /// </summary>
        /// <param name="MsgType"></param>
        /// <param name="MsgContent"></param>
        public ProtocolMessage(CMDTYPE MsgType, byte[] MsgContent)
        {
            MsgHeader[0] = 0xeb;
            MsgHeader[1] = 0x90;
            MsgHeader.CopyTo(MsgFrame, 0);
            this.MsgType = MsgType;
            ContentLen = MsgContent.Length;
            MsgFrame[2] = System.Convert.ToByte(MsgType);
            MsgFrame[3] = System.Convert.ToByte(ContentLen);
            MsgContent.CopyTo(MsgFrame, 4);
            MsgTotalLength = ContentLen + 5;
            MsgFrame[MsgTotalLength - 1] = MsgEnder;
        }

        /// <summary>
        /// 传入数据包的构造函数
        /// </summary>
        /// <param name="packet"></param>
        public ProtocolMessage(ref DataPacket packet)
        {
            int contentlen = packet.len;
            MsgTotalLength = contentlen + 5;
            MsgFrame = new byte[MsgTotalLength];
            MsgHeader[0] = 0xeb;
            MsgHeader[1] = 0x90;
            MsgHeader.CopyTo(MsgFrame, 0);
            MsgType = packet.cmdType;
            MsgFrame[2] = System.Convert.ToByte(MsgType);
            MsgFrame[3] = System.Convert.ToByte(ContentLen);
            packet.payload.CopyTo(MsgFrame, 4);
            MsgFrame[MsgTotalLength - 1] = MsgEnder;
        }

        #endregion 构造函数
    }
}
