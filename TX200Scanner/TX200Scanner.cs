using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel;


public class TX200Scanner
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
    VbarApi Api = new VbarApi();

    /// <summary>
    /// 扫描结果数据类
    /// </summary>
    public class BarcodeScannerEventArgs : EventArgs
    {
        string code;

        public BarcodeScannerEventArgs(string msg)
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
    public delegate void QRcodeScannerEventHandler(object sender, BarcodeScannerEventArgs e);

    /// <summary>
    /// 扫码成功时发生
    /// </summary>
    public event QRcodeScannerEventHandler CodeFound;

    /// <summary>
    /// 内部公开方法
    /// </summary>
    /// <param name="e"></param>
    protected virtual void OnCodeFound(BarcodeScannerEventArgs e)
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
    public string Decoder()
    {
        byte[] result;
        string sResult = "";
        if (Api.scan(out result) == 1)
        {
            sResult = System.Text.Encoding.Default.GetString(result);
        }
        else
        {
            sResult = "";
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
                    //Api.beepControl(1);//dont have beep control in 5.x.x
                    OnCodeFound(new BarcodeScannerEventArgs(decoderesult));
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
            //MessageBox.Show(ex.ToString());
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
        if (Api.openDevice() == 1)
        {
            //Add QRCODE support, QRCODE format id = 1
            if (Api.addCodetype())
            {
                Api.BackLight(true);
                return ScannerReturn.SUCCESS;
            }
            else
            {
                //if cant add QRCODE, disconnet device and return false
                Api.BackLight(false);
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
            Api.BackLight(false);
            Api.disConnected();
        }
    }
}

