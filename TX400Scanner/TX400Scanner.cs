using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;


public class TX400Scanner
{
    /// <summary>
    /// 打开扫码器的返回值定义
    /// </summary>
    public enum ScannerReturn
    {
        SUCCESS = 0,
        OPEN_FAIL,
        ADD_FORMAT_FAIL,
        UNKOWN,
    }

    /// <summary>
    /// 译码线程
    /// </summary>
    public static Thread DecodeThread = null;

    /// <summary>
    /// 是否循环标志
    /// </summary>
    private static bool bIsLoop = false;

    /// <summary>
    /// 设备是否打开标识
    /// </summary>
    public bool isOpen = false;

    public static bool skipCode = false;

    public void Lock()
    {
        skipCode = true;
    }

    public void Unlock()
    {
        skipCode = false;
    }

    /// <summary>
    /// dll入口
    /// </summary>
    Vbarapi Api = new Vbarapi();

    /// <summary>
    /// 扫描结果数据类
    /// </summary>
    public class QRcodeScannerEventArgs : EventArgs
    {
        string code;

        public QRcodeScannerEventArgs(string msg)
        {
            this.code = msg;
        }

        public string Code
        {
            get { return code; }
        }
    }

    /// <summary>
    /// 委托事件模型
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void QRcodeScannerEventHandler(object sender, QRcodeScannerEventArgs e);

    /// <summary>
    /// 扫码成功时发生
    /// </summary>
    public event QRcodeScannerEventHandler CodeFound;

    /// <summary>
    /// 内部公开方法
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCodeFound(QRcodeScannerEventArgs e)
    {
        if (CodeFound != null)
        {
            CodeFound(this, e);
        }
    }

    /// <summary>
    /// 译码方法
    /// </summary>
    /// <returns></returns>
    private string Decoder()
    {
        byte[] result;
        string sResult = null;
        int size;
        if (Api.getResultStr(out result, out size))
        {
            string msg = System.Text.Encoding.Default.GetString(result);
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            sResult = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }
        else
        {
            sResult = null;
        }
        return sResult;
    }

    private void DecodeThreadMethod()
    {
        string decoderesult = null;
        while (bIsLoop)
        {
            if (skipCode)
            {
                //other thread is busy processing last code
                //do nothing
            }
            else
            {
                decoderesult = Decoder();
                if (decoderesult != null)
                {
                    Api.beepControl(1);
                    OnCodeFound(new QRcodeScannerEventArgs(decoderesult));
                    decoderesult = null;
                }
            }
        }
    }

    /// <summary>
    /// start a new thread to decode results from QRcode scanner
    /// </summary>
    public void StartDecodeThread()
    {
        bIsLoop = true;
        try
        {
            DecodeThread = new Thread(new ThreadStart(DecodeThreadMethod));
            DecodeThread.IsBackground = true;
            DecodeThread.Start();
        }
        catch (Exception ex)
        {

        }
    }

    /// <summary>
    /// abort the thread and check if the thread state is aborted
    /// </summary>
    public void StopDecodeThread()
    {
        bIsLoop = false;
        if (DecodeThread != null)
        {
            DecodeThread.Abort();
            while (DecodeThread.ThreadState != ThreadState.Aborted)
            {
                Thread.Sleep(50);
            }
        }
    }

    public ScannerReturn Open()
    {
        //Open device
        if (Api.openDevice(1))
        {
            //Add QRCODE support, QRCODE format id = 1
            if (Api.addCodeFormat((byte)1))
            {
                Api.backlight(true);
                return ScannerReturn.SUCCESS;
            }
            else
            {
                //if cant add QRCODE, disconnet device and return false
                Api.backlight(false);
                Api.disConnected();
                return ScannerReturn.ADD_FORMAT_FAIL;
            }
        }
        else
        {
            return ScannerReturn.OPEN_FAIL;
        }
    }

    public void Close()
    {
        if (isOpen)
        {
            Api.backlight(false);
            Api.disConnected();
        }
    }
}

