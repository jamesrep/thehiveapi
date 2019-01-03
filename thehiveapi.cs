// james simple example-implementation of thehive-api in c#. 
// tests? hmm... compiles.
// Example:
// ThehiveAPI thehiveAPI = new ThehiveAPI();
// thehiveAPI.strKey = "ASDF";
// thehiveAPI.strURL = "https://...";
// ThehiveAPI.HiveCreateCase hiveCase = new ThehiveAPI.HiveCreateCase();
// Dictionary<string, object> objNewCase =  thehiveAPI.createCase(hiveCase);

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace JamesAPI
{
    public class ThehiveAPI
    {
        const string STR_SEARCH = "/api/case";
        const string STR_PATCH = "PATCH";
        const string STR_POST = "POST";
        const string STR_BOUNDARYTYPE = "application/json, text/plain, */*";
        const string STR_BOUNDARYPREFIX = "multipart/form-data; boundary=";
        const string STR_CONTENTTYPE = "application/json; charset=UTF-8";
        const string STR_AUTHORIZATION = "Authorization";
        const string STR_BEARER = "Bearer ";

        // Default values when creating new hive-case
        public int tlpDefault = 2;
        public string strStatusDefault = "Open";
        public int severityDefault = 2;


        string _strKey = null;
        string _strBearer = null;
        public string strKey
        {
            get { return _strKey; }
            set { _strBearer = STR_BEARER + value; _strKey = value; }
        }

        public string strURL = null;

        public SecureString strPassword = null;
        public string strUsername = null;
        public string strDomain = null;
        bool bSkipSecurity = true;

        public string strProxy = null;
        public int proxyPort = 8080;


        public class HiveCreateCase
        {
            public string status;
            public string title;
            public string description;
            public int tlp;
            public int severity;
            //public int[] metrics;
            public string startDate;
            public string[] tags;
            public string template;
        }



        public class HiveObservable
        {
            //"dataType":"hash","ioc":false,"sighted":true,"tlp":1,"message":"tool","tags":["tool"],"data":"123123123123123123"
            public string dataType;
            public bool ioc = false;
            public bool sighted = true;
            public int tlp = 1;
            public string message = "tool";
            public string[] tags = null;
            public string data;
        }


        public class HiveCaseManipulation
        {
        }

        public class HiveCloseCase : HiveCaseManipulation
        {
            public string status = "Resolved";
            public string resolutionStatus = "TruePositive";
            public string summary = "";
            public string impactStatus = "NoImpact";
        }

        public static bool insecureCertValidation(
                object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        string postPage(string strURL, string strPostData)
        {
            return postPage(strURL, strPostData, null, null);
        }


        string postPage(string strURL, string strPostData, string strContentType, string strAccept)
        {
            return postPage(strURL, strPostData, strContentType, strAccept, STR_POST);
        }



        string postPage(string strURL, string strPostData, string strContentType, string strAccept, string strMethod)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(strPostData);
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(strURL);

            if (strProxy != null)
            {
                request.Proxy = new WebProxy(strProxy, proxyPort);
            }

            request.Method = strMethod;

            if (strAccept != null)
            {
                ((System.Net.HttpWebRequest)request).Accept = strAccept;
            }

            if (strContentType == null)
            {
                request.ContentType = STR_CONTENTTYPE;
            }
            else
            {
                request.ContentType = strContentType;
            }


            if (_strBearer != null)
            {
                request.Headers.Add(STR_AUTHORIZATION,  _strBearer);
            }
            else
            {
                request.Credentials = new NetworkCredential(strUsername, strPassword, strDomain);
            }

            if (bSkipSecurity)
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(insecureCertValidation);
            }

            Stream dataStream = null;


            try
            {
                dataStream = request.GetRequestStream();

                if (dataStream == null)
                {

                    return null;
                }

                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Flush();
                dataStream.Close();
            }
            catch (Exception exSend)
            {
                LogWriter.writeDebug(exSend.ToString());

                if (dataStream != null) dataStream.Close();

                return null;
            }


            WebResponse response = null;
            StreamReader reader = null;

            try
            {
                response = request.GetResponse();

            }
            catch (Exception exResponse)
            {
                if (response != null) response.Close();

                LogWriter.writeDebug(exResponse.ToString());

                return null;
            }

            try
            {
                dataStream = response.GetResponseStream();

                if (dataStream == null)
                {


                    return null;
                }

                reader = new StreamReader(dataStream);

                string strReadResponse = reader.ReadToEnd();

                return strReadResponse;
            }
            catch (Exception exResponse2)
            {
                LogWriter.writeDebug(exResponse2.ToString());

                return null;
            }
            finally
            {
                if (reader != null) reader.Close();
                if (dataStream != null) dataStream.Close();
                if (response != null) response.Close();
            }
        }




        public Dictionary<string, object> addObservable(string strCaseID, HiveObservable hiveObservable)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();
            string strDataObject = js.Serialize(hiveObservable);
            string strRandom = (new System.Random()).Next().ToString();
            string strBoundary = "----WebKitFormBoundary" + strRandom;

            string strData = "--" + strBoundary + "\r\n" +
                            "Content-Disposition: form-data; name=\"_json\"\r\n\r\n" +

                            strDataObject + "\r\n" +
                            "--" + strBoundary + "--\r\n";

            

            string strJson = postPage(strURL + STR_SEARCH + "/" + strCaseID + "/artifact", strData, STR_BOUNDARYPREFIX + strBoundary, STR_BOUNDARYTYPE);


            if (strJson == null) return null;

            Dictionary<string, object> objRetval2 = (Dictionary<string, object>)js.Deserialize(strJson, typeof(Dictionary<string, object>));

            return objRetval2;
        }

        /*
        createCase 

        Example 1:
        POST /api/case

        {"status":"Open","title":"asdf@asdf.se","description":"Email","tlp":2,"severity":1,"metrics":{},"startDate":"20180112T150600+0100","tags":["phish"],"template":"Phishing-temp1"}

        */
        public Dictionary<string, object> createCase(HiveCreateCase hiveCase)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();

            string strData = js.Serialize(hiveCase);
            string strJson = postPage(strURL + STR_SEARCH, strData);

			if (strJson == null) return null;


            Dictionary<string, object> objRetval2 = (Dictionary<string, object>)js.Deserialize(strJson, typeof(Dictionary<string, object>));

            return objRetval2;
        }

        public Dictionary<string, object>[] search(string strQuery, DateTime dtStart, DateTime dtEnd)
        {
            System.Web.Script.Serialization.JavaScriptSerializer js = new System.Web.Script.Serialization.JavaScriptSerializer();


            string strStart = toLongTime(dtStart).ToString();
            string strEnd = toLongTime(dtEnd).ToString();

            // "{\"query\":{\"_and\":[{\"_string\":\"startDate:[1523656800000 TO 1524002399000]\"}]}}";

            string strGet = "/api/case/_search?range=0-100000"; // TODO: dont hardcode this....

            //string strSearchCases = "{\"query\":{\"_and\":[{\"_string\":\"startDate:[" + strStart + " TO " + strEnd + "]\"}]}}";
            string strSearchCases = "{\"query\":{\"_and\":[]}}";

            string strJson = postPage(strURL + strGet, strSearchCases);


            Dictionary<string, object>[] objRetval2 = js.Deserialize<Dictionary<string, object>[]>(strJson);

            return objRetval2;
        }

        public static ulong toLongTime(DateTime dtToConvert)
        {
            DateTime dtStartTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return (ulong)((dtToConvert - dtStartTime).TotalSeconds * 1000);
        }
    }


}


