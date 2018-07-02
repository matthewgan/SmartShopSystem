using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;
using System.Collections.Generic;


public class APIClient
{
    private string baseAddress = "https://www.wuzhanggui.shop/";
    //private string baseAddress = "http://127.0.0.1:8000/";

    private string getAuthTokenPath = "api/token/get_auth_token/";

    private string entryByCode = "api/gate/entry_by_code/";

    private string entryByFace = "api/gate/entry_by_face/";

    private string getInfo = "api/customer/info/";

    private string logVisit = "api/gate/log/";

    private string queryInfoPath = "api/merchandise/detail/";

    private string addTagPath = "api/tag/add/";

    private string delTagPath = "api/tag/delete/";

    private string getMerchandisesInfoByTagIDs = "api/tag/query/";

    private string generateOrder = "api/order/create/";

    private string getPayCodeUrl = "api/payment/payOrder/";

    //private string addInfoPath = "api/merchandise/add/";

    public static string token;

    private string username = "matthew";

    private string password = "letmepass1";

    public static string shop_identify = "1";

    class AuthInfo
    {
        public string username { get; set; }
        public string password { get; set; }
    }

    public class Token
    {
        public string token { get; set; }
    }

    public class ID
    {
        public string id { get; set; }
    }

    public class Code
    {
        public string code { get; set; }
    }

    public class Barcode
    {
        public string barcode { get; set; }
    }

    public class MerchandiseInfoShow
    {
        public string ID { get; set; }
        public string Code { get; set; }
        public string Barcode { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Scale { get; set; }
        public string Factory { get; set; }
        public string Unit { get; set; }
    }

    public class MerchandiseInfoCashier
    {
        public string id { get; set; }
        public string name { get; set; }
        public string brand { get; set; }
        public string scale { get; set; }
        public string unit { get; set; }
        public string producePlace { get; set; }
        public string originPrice { get; set; }
        public string promotionPrice { get; set; }
        public string clubPrice { get; set; }
        public string code { get; set; }
        public string picture { get; set; }
    }

    public class Tag
    {
        public string EPC { get; set; }
        public string TID { get; set; }
        public string merchandiseID { get; set; }
    }

    public class TagInfoList
    {
        public int totalNum { get; set; }
        public List<string> EPClist { get; set; }

        public TagInfoList()
        {
            this.totalNum = 0;
            this.EPClist = new List<string>();
            this.EPClist.Clear();
        }
    }

    public class Log
    {
        public string who { get; set; }
        public string where { get; set; }
    }

    public class UserInfo
    {
        public string id { get; set; }
        public string nickName { get; set; }
        public string avatarUrl { get; set; }
        public string level { get; set; }
    }

    public class order_detail
    {
        public string id;
        public int num;
    }

    public class Order
    {
        public string order_method;
        public string user_id;
        public string shop_id;
        public List<order_detail> orderList;

        /// <summary>
        /// 从商品信息生成订单的构造函数
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="infos"></param>
        public Order(string uid, MerchandiseInfoCashier[] infos)
        {
            foreach (MerchandiseInfoCashier info in infos)
            {
                string m_id = info.id;
                bool found_same_id = false;
                foreach (order_detail detail in orderList)
                {
                    if (m_id == detail.id)
                    {
                        detail.num += 1;
                        found_same_id = true;
                        break;
                    }
                }
                if (found_same_id == false)
                {
                    order_detail detail = new order_detail();
                    detail.id = m_id;
                    detail.num = 1;
                    orderList.Add(detail);
                }
            }
            order_method = "1";
            user_id = uid;
            shop_id = shop_identify;
        }
    }

    private class Order_Response
    {
        public string code { get; set; }
        public string tradeNo { get; set; }
    }

    public class Payment_Request
    {
        public string order_method;
        public string user_id;
        public string trade_no;
    }

    public class Payment_Response
    {
        public string status;
        public string alipay_code_url;
        public string wechat_pay_code_url;
    }

    public APIClient()
    {
        token = GetAuthToken();
    }

    /// <summary>
    /// 从后台服务器获取TOKEN
    /// </summary>
    /// <returns></returns>
    public string GetAuthToken()
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + getAuthTokenPath);

        request.Method = "POST";
        request.ContentType = "application/json";

        AuthInfo auth = new AuthInfo() { username = this.username, password = this.password };

        string jsonstring = JsonConvert.SerializeObject(auth);
        byte[] data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        //get response
        WebResponse response = request.GetResponse();

        if (((HttpWebResponse)response).StatusDescription == "OK")
        {
            using (var stream = response.GetResponseStream())
            {
                StreamReader reader = new StreamReader(stream);
                responseMessage = reader.ReadToEnd();
            }
        }

        try
        {
            Token tok = JsonConvert.DeserializeObject<Token>(responseMessage);
            return tok.token;
        }
        catch (Exception ex)
        {
            return ex.ToString();
        }
    }

    /// <summary>
    /// 从后台服务器查询条形码商品信息
    /// </summary>
    /// <param name="barcode"></param>
    /// <returns></returns>
    public MerchandiseInfoShow GetMerchandiseInfo(Barcode barcode)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + queryInfoPath);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(barcode);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        try
        {
            //post data
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusDescription == "OK")
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();
                }
            }

            MerchandiseInfoShow user = JsonConvert.DeserializeObject<MerchandiseInfoShow>(responseMessage);

            return user;
        }
        catch (Exception ex)
        {
            return new MerchandiseInfoShow();
        }

    }

    /// <summary>
    /// 在后台添加RFID标签和商品绑定，完成入库
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public bool CreateTagInStock(Tag tag, out string msg)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + addTagPath);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(tag);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        try
        {
            //post data
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.Created)
            {
                msg = "添加成功";
                return true;
            }
            else
            {
                msg = "添加失败";
                return false;
            }
        }
        catch (Exception ex)
        {
            msg = "重复EPC，无法添加";
            return false;
        }
    }

    /// <summary>
    /// 在后台删除RFID标签和商品绑定，取消上次操作
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public bool DeleteTagInStock(Tag tag, out string msg)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + delTagPath);

        request.Method = "DELETE";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(tag);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        try
        {
            //post data
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.NoContent)
            {
                msg = "删除成功";
                return true;
            }
            else
            {
                msg = "删除失败";
                return false;
            }
        }
        catch (Exception ex)
        {
            msg = "删除无效EPC";
            return false;
        }
    }

    public UserInfo EntryByCode(Code code)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + entryByCode);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(code);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusDescription == "OK")
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();
                }
            }


            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(responseMessage);

            return user;
        }
        catch (Exception ex)
        {
            return new UserInfo() { id = string.Empty, nickName = string.Empty, avatarUrl = string.Empty, level = string.Empty };
        }

    }

    public UserInfo EntryByFace(string filename)
    {
        string responseMessage = string.Empty;

        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
        byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(baseAddress + entryByFace);
        request.ContentType = "multipart/form-data; boundary=" + boundary;
        request.Method = "POST";
        request.KeepAlive = true;
        request.Headers["Authorization"] = "Token " + token;

        Stream rs = request.GetRequestStream();
        rs.Write(boundarybytes, 0, boundarybytes.Length);

        string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
        string header = string.Format(headerTemplate, "image", filename, "image/jpeg");
        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
        rs.Write(headerbytes, 0, headerbytes.Length);

        FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[4096];
        int bytesRead = 0;
        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            rs.Write(buffer, 0, bytesRead);
        }
        fileStream.Close();

        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
        rs.Write(trailer, 0, trailer.Length);
        rs.Close();

        WebResponse response = null;
        try
        {
            response = request.GetResponse();
            Stream stream2 = response.GetResponseStream();
            StreamReader reader2 = new StreamReader(stream2);
            responseMessage = reader2.ReadToEnd();

            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(responseMessage);

            return user;

        }
        catch (Exception ex)
        {
            if (response != null)
            {
                response.Close();
                response = null;
            }
            return new UserInfo() { id = string.Empty, nickName = string.Empty, avatarUrl = string.Empty, level = string.Empty };
        }
    }

    public bool VistLogToSystem(Log log)
    {
        var request = (HttpWebRequest)WebRequest.Create(baseAddress + logVisit);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(log);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusDescription == "OK")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    public UserInfo GetUserInfo(ID id)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + getInfo);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(id);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusDescription == "OK")
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();
                }
            }

            UserInfo user = JsonConvert.DeserializeObject<UserInfo>(responseMessage);

            return user;
        }
        catch (Exception ex)
        {
            return new UserInfo() { id = string.Empty, nickName = string.Empty, avatarUrl = string.Empty, level = string.Empty };
        }

    }

    public static Bitmap LoadPicture(string url)
    {
        HttpWebRequest wreq;
        HttpWebResponse wresp;
        Stream mystream;
        Bitmap bmp;

        bmp = null;
        mystream = null;
        wresp = null;
        try
        {
            wreq = (HttpWebRequest)WebRequest.Create(url);
            wreq.AllowWriteStreamBuffering = true;

            wresp = (HttpWebResponse)wreq.GetResponse();

            if ((mystream = wresp.GetResponseStream()) != null)
                bmp = new Bitmap(mystream);
        }
        finally
        {
            if (mystream != null)
                mystream.Close();

            if (wresp != null)
                wresp.Close();
        }
        return (bmp);
    }

    public MerchandiseInfoCashier[] QueryMerchandiseInfo(TagInfoList tags)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + getMerchandisesInfoByTagIDs);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(tags);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            //get response
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusDescription == "OK")
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();

                    MerchandiseInfoCashier[] merchandises = JsonConvert.DeserializeObject<MerchandiseInfoCashier[]>(responseMessage);

                    return merchandises;
                }
            }
            else
            {
                MerchandiseInfoCashier[] merchandises = new MerchandiseInfoCashier[1];

                return merchandises;
            }
        }
        catch (Exception ex)
        {
            MerchandiseInfoCashier[] merchandises = new MerchandiseInfoCashier[1];
            merchandises[0].id = string.Empty;

            return merchandises;
        }

    }

    public string CreateOrderNo(Order od)
    {
        string tradeNo = string.Empty;

        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + generateOrder);

        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        string jsonstring = JsonConvert.SerializeObject(od);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.Created)
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();

                    Order_Response resp = JsonConvert.DeserializeObject<Order_Response>(responseMessage);
                    tradeNo = resp.tradeNo;
                    return tradeNo;
                }
            }
        }
        catch (Exception ex)
        {
            return string.Empty;
        }
        return tradeNo;
    }

    public Payment_Response GetPaymentCodeUrl(string uid, string trade)
    {
        string responseMessage = string.Empty;

        var request = (HttpWebRequest)WebRequest.Create(baseAddress + getPayCodeUrl);
        request.Method = "POST";
        request.ContentType = "application/json";
        request.Headers["Authorization"] = "Token " + token;

        Payment_Request msg = new Payment_Request();
        msg.order_method = "1";
        msg.user_id = uid;
        msg.trade_no = trade;

        string jsonstring = JsonConvert.SerializeObject(msg);
        var data = Encoding.UTF8.GetBytes(jsonstring);
        request.ContentLength = data.Length;

        //post data
        using (var stream = request.GetRequestStream())
        {
            stream.Write(data, 0, data.Length);
        }

        try
        {
            WebResponse response = request.GetResponse();

            if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
            {
                using (var stream = response.GetResponseStream())
                {
                    StreamReader reader = new StreamReader(stream);
                    responseMessage = reader.ReadToEnd();

                    Payment_Response resp = JsonConvert.DeserializeObject<Payment_Response>(responseMessage);

                    return resp;
                }
            }
        }
        catch (Exception ex)
        {
            return new Payment_Response() { status = "false" };
        }
        return new Payment_Response() { status = "false" };
    }

    public static void HttpUploadFile(string url, string file, string paramName, string contentType, System.Collections.Specialized.NameValueCollection nvc)
    {
        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
        byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");

        HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
        wr.ContentType = "multipart/form-data; boundary=" + boundary;
        wr.Method = "POST";
        wr.KeepAlive = true;
        wr.Credentials = System.Net.CredentialCache.DefaultCredentials;

        Stream rs = wr.GetRequestStream();

        string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
        foreach (string key in nvc.Keys)
        {
            rs.Write(boundarybytes, 0, boundarybytes.Length);
            string formitem = string.Format(formdataTemplate, key, nvc[key]);
            byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
            rs.Write(formitembytes, 0, formitembytes.Length);
        }
        rs.Write(boundarybytes, 0, boundarybytes.Length);

        string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
        string header = string.Format(headerTemplate, paramName, file, contentType);
        byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
        rs.Write(headerbytes, 0, headerbytes.Length);

        FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[4096];
        int bytesRead = 0;
        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
        {
            rs.Write(buffer, 0, bytesRead);
        }
        fileStream.Close();

        byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
        rs.Write(trailer, 0, trailer.Length);
        rs.Close();

        WebResponse wresp = null;
        try
        {
            wresp = wr.GetResponse();
            Stream stream2 = wresp.GetResponseStream();
            StreamReader reader2 = new StreamReader(stream2);
        }
        catch (Exception ex)
        {
            if (wresp != null)
            {
                wresp.Close();
                wresp = null;
            }
        }
        finally
        {
            wr = null;
        }
    }
}

