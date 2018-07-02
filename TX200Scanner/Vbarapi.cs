using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;


class VbarApi
{
    /// <summary>
    /// 对应互联微光5.X.X版本，硬件类型TX200系列老款
    /// </summary>
    public VbarApi()
    {
    }
    //连接设备dll
    [DllImport("VguangX1000Dll.dll", EntryPoint = "connectDevice", CallingConvention = CallingConvention.StdCall)]
    public static extern int connectDevice();
    //断开设备dll
    [DllImport("VguangX1000Dll.dll", EntryPoint = "disconnectDevice", CallingConvention = CallingConvention.StdCall)]
    public static extern int disconnectDevice();
    //设置码制dll
    [DllImport("VguangX1000Dll.dll", EntryPoint = "setBarcodeFormat", CallingConvention = CallingConvention.StdCall)]
    public static extern int setBarcodeFormat(int barcodeFormat);
    //得到扫码或者NFC信息
    [DllImport("VguangX1000Dll.dll", EntryPoint = "getResultStr", CallingConvention = CallingConvention.StdCall)]
    public static extern int getResultStr(byte[] result, ref int length, int maxlen, int timeout);
    //开灯
    [DllImport("VguangX1000Dll.dll", EntryPoint = "lightOn", CallingConvention = CallingConvention.StdCall)]
    public static extern int lightOn();
    //关灯
    [DllImport("VguangX1000Dll.dll", EntryPoint = "lightOff", CallingConvention = CallingConvention.StdCall)]
    public static extern int lightOff();

    //连接设备
    public int openDevice()
    {
        int connect = connectDevice(); //连接设备
        if (connect < 0)
        {
            //MessageBox.Show("连接设备失败，请检查设备是否插入或重试!", "设备连接信息");
            return 0;
        }

        //MessageBox.Show("连接设备成功!", "设备连接信息");
        return 1;
    }
    //背光控制
    public void BackLight(bool state)
    {
        if (state)
        {
            lightOn();
        }
        else
        {
            lightOff();
        }
    }

    //断开设备
    public void disConnected()
    {
        int connect = disconnectDevice(); //断开设备
        if (connect < 0)
        {
            //MessageBox.Show("断开设备失败，请检查设备是否插入或重试!", "断开设备状态");
            return;
        }
        //MessageBox.Show("断开设备成功", "断开设备状态");      

    }
    //设置支持的码制
    public bool addCodetype()
    {
        //setBarcodeFormat(1);  //参数类型参照头文件 1对应QR码
        //setBarcodeFormat(2);  //2对应DM码
        setBarcodeFormat(3);  //3对应一维码

        return true;
    }

    //扫码
    public int scan(out byte[] result)
    {
        byte[] c_result = new byte[256];
        int len = 0;
        if (getResultStr(c_result, ref len, 1024, 2) > 0)
        {
            result = c_result;
            return 1;
        }
        else
        {
            result = null;
            return 0;
        }
    }
}

