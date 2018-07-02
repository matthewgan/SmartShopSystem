using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


public class BaiduFace
{
    /// <summary>
    /// 百度KEYS
    /// </summary>
    private string APP_ID = "11211624";
    private string API_KEY = "wo7nEAvyNrK30kWG38rTC1qg";
    private string SECRET_KEY = "VcHeSeIARmfXI0TahrgtMyszMsljIKnB";

    Baidu.Aip.Face.Face client;

    public BaiduFace()
    {
        client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
        client.Timeout = 60000;
    }

    public void renew()
    {
        client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
    }

    public string bitmap2BASE64(Bitmap bit)
    {
        using (var ms = new MemoryStream())
        {
            bit.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.GetBuffer());
        }
    }

    public JObject face_detect_using_aip(string userid, Bitmap bmap)
    {
        //client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
        //client.Timeout = 6000;

        string image = bitmap2BASE64(bmap);

        var imageType = "BASE64";

        var options = new Dictionary<string, object>
            {
                {"max_face_num", 1},
                {"face_field", "age,beauty,expression,faceshape,gender,glasses,race,qualities" }
            };

        renew();
        return client.Detect(image, imageType, options);
    }

    public JObject face_register_using_aip(string userid, Bitmap bmap)
    {
        //client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
        //client.Timeout = 6000;

        string image = bitmap2BASE64(bmap);

        var imageType = "BASE64";

        var groupId = "group1";

        var options = new Dictionary<string, object>{
                {"action_type", "replace"}
            };

        try
        {
            renew();
            return client.UserAdd(image, imageType, groupId, userid, options);

        }
        catch (Exception ex)
        {
            return (JObject)ex.ToString();
        }
    }

    public JObject face_search_using_aip(Bitmap bmap)
    {
        //client = new Baidu.Aip.Face.Face(API_KEY, SECRET_KEY);
        //client.Timeout = 6000;

        string image = bitmap2BASE64(bmap);

        var imageType = "BASE64";

        var groupId = "customer";

        var options = new Dictionary<string, object>{
                {"quality_control", "NORMAL"},
                {"liveness_control", "NONE"},
            };

        try
        {
            renew();
            return client.Search(image, imageType, groupId, options);
        }
        catch (Exception ex)
        {
            return (JObject)ex.ToString();
        }
    }
}

