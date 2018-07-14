using System;
using System.Text;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Drawing;

namespace CashierCapsule
{
    public class APIClient
    {
        //private string baseAddress = "https://www.wuzhanggui.shop/";
        private string baseAddress = "http://127.0.0.1:8000/";

        private string getAuthTokenPath = "api/get_auth_token/";

        private string entryByCode = "api/entry_by_code/";

        private string entryByFace = "api/entry_by_face/";

        private string getMerchandisesInfoByTagIDs = "api/queryMerchanInfo/";

        public static string token;

        private string username = "matthew";

        private string password = "letmepass1";

        class AuthInfo
        {
            public string username { get; set; }
            public string password { get; set; }
        }

        public class Token
        {
            public string token { get; set; }
        }

        public class Code
        {
            public string code { get; set; }
        }

        public class UserInfo
        {
            public string nickName { get; set; }
            public string avatarUrl { get; set; }
            public string level { get; set; }
        }

        public class TagInfoList
        {
            public int totalNum { get; set; }
            public string[] EPClist { get; set; } 
        }

        public class OrderInfo
        {

        }

        public APIClient()
        {
            token = GetAuthToken();
        }

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
            
            if(((HttpWebResponse)response).StatusDescription == "OK")
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
            catch(Exception ex)
            {
                return ex.ToString();
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
                UserInfo user = JsonConvert.DeserializeObject<UserInfo>(responseMessage);

                return user;
            }
            catch(Exception ex)
            {
                return new UserInfo();
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
                return new UserInfo();
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

        public void QueryMerchandiseInfo(TagInfoList tags)
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
                
            }
            catch (Exception ex)
            {
                
            }

        }
    }
}
