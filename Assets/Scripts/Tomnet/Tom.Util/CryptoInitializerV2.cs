using Tom.Core;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Tom.Util
{
    public class CryptoInitializerV2 : ICryptoInitializer
    {
        private const string KEY_SESSION_TOKEN = "SessToken";

        private const string TARGET_SERVLET = "/BlueBox/CryptoManager";

        private TomOrange sfs;

        private bool useHttps = true;

        public CryptoInitializerV2(TomOrange sfs)
        {
            if (!sfs.IsConnected)
            {
                throw new InvalidOperationException("Cryptography cannot be initialized before connecting to SmartFoxServer!");
            }
            if (sfs.GetSocketEngine().CryptoKey != null)
            {
                throw new InvalidOperationException("Cryptography is already initialized!");
            }
            this.sfs = sfs;
        }

        public void Run()
        {
            Init();
        }

        private async void Init()
        {
            string targetUrl = (useHttps ? "https://" : "http://") + sfs.Config.Host + ":" + (useHttps ? sfs.Config.HttpsPort : sfs.Config.HttpPort) + "/BlueBox/CryptoManager";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(targetUrl);
                FormUrlEncodedContent formContent = new FormUrlEncodedContent(new KeyValuePair<string, string>[1]
                {
                    new KeyValuePair<string, string>("SessToken", sfs.SessionToken)
                });
                try
                {
                    HttpResponseMessage req = await httpClient.PostAsync("", formContent);
                    req.EnsureSuccessStatusCode();
                    string res = req.Content.ReadAsStringAsync().Result;
                    OnHttpResponse(res);
                }
                catch (Exception ex2)
                {
                    Exception ex = ex2;
                    OnHttpError(ex.Message);
                }
            }
        }

        private void OnHttpResponse(string rawData)
        {
            byte[] data = Convert.FromBase64String(rawData);
            ByteArray byteArray = new ByteArray();
            ByteArray byteArray2 = new ByteArray();
            byteArray.WriteBytes(data, 0, 16);
            byteArray2.WriteBytes(data, 16, 16);
            sfs.GetSocketEngine().CryptoKey = new CryptoKey(byteArray2, byteArray);
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = true;
            sfs.DispatchEvent(new SFSEvent(SFSEvent.CRYPTO_INIT, dictionary));
        }

        private void OnHttpError(string errorMsg)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>();
            dictionary["success"] = false;
            dictionary["errorMessage"] = errorMsg;
            sfs.DispatchEvent(new SFSEvent(SFSEvent.CRYPTO_INIT, dictionary));
        }
    }
}
