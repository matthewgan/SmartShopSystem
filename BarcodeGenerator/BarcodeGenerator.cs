using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Spire.Barcode;

public class BarcodeGenerator
{
    public static Random rand = new Random();

    public static Bitmap GenerateQRCode(string data, string label)
    {
        string filename = rand.Next().ToString() + ".png";
        BarcodeSettings settings = new BarcodeSettings();
        settings.Type = BarCodeType.QRCode;
        settings.Data = data;
        settings.Data2D = label;
        settings.QRCodeDataMode = QRCodeDataMode.AlphaNumber;
        settings.X = 1.0f;
        settings.QRCodeECL = QRCodeECL.H;

        BarCodeGenerator generator = new BarCodeGenerator(settings);
        Image image = generator.GenerateImage();

        return new Bitmap(image);
    }
}
