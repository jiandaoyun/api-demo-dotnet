using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace dotnet
{

    class APIUtils
    {
        private static bool RETRY_IF_LIMITED = true;
        private static string WEBSITE = "https://www.jiandaoyun.com";
        private string urlGetWidgets;
        private string urlGetData;
        private string urlRetrieveData;
        private string urlCreateData;
        private string urlUpdateData;
        private string urlDeleteData;
        private string apiKey;

        public APIUtils(string appId, string entryId, string apiKey)
        {
            this.urlGetWidgets = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/widgets";
            this.urlGetData = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/data";
            this.urlRetrieveData = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/data_retrieve";
            this.urlCreateData = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/data_create";
            this.urlUpdateData = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/data_update";
            this.urlDeleteData = WEBSITE + "/api/v1/app/" + appId + "/entry/" + entryId + "/data_delete";
            this.apiKey = apiKey;
        }


        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        /**
         * 发送HTTP请求
         **/
        public dynamic SendRequest(string method, string url, JObject data)
        {
            method = method.ToUpper();
            HttpWebRequest req;
            // HTTPS
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            if (method.Equals("GET"))
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(url);
                builder.Append("?");
                int i = 0;
                foreach (var item in data)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value.ToString());
                    i++;
                }
                req = (HttpWebRequest)WebRequest.Create(builder.ToString());
                req.Method = "GET";
            }
            else
            {
                req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json;charset=utf-8";
                req.Headers["Authorization"] = "Bearer " + this.apiKey;
                byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
                req.ContentLength = bytes.Length;
                Stream stream = req.GetRequestStream();
                stream.Write(bytes, 0, bytes.Length);
                stream.Close();
            }
            JObject result = new JObject();
            try
            {
                using (Stream responsestream = req.GetResponse().GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(responsestream, Encoding.UTF8))
                    {
                        string content = sr.ReadToEnd();
                        result = JsonConvert.DeserializeObject<JObject>(content);
                    }
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse response = (HttpWebResponse)ex.Response;
                
                if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    using (Stream responsestream = response.GetResponseStream())
                    {
                        using (StreamReader sr = new StreamReader(responsestream, Encoding.UTF8))
                        {
                            string content = sr.ReadToEnd();
                            result = JsonConvert.DeserializeObject<JObject>(content);
                            if ((int)result["code"] == 8303 && RETRY_IF_LIMITED)
                            {
                                Thread.Sleep(5000);
                                return SendRequest(method, url, data);
                            }
                            else
                            {
                                throw new Exception("请求错误 Error Code: " + result["code"] + " Error Msg: " + result["msg"]);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public JArray GetFormWidgets()
        {
            JObject result = SendRequest("POST", urlGetWidgets, new JObject());
            return (JArray) result["widgets"];
        }

        public JArray GetFormData(string dataId, int limit, JArray fields, JObject filter)
        {
            JObject data = new JObject
            {
                ["data_id"] = dataId,
                ["limit"] = limit,
                ["fields"] = fields,
                ["filter"] = filter
            };
            JObject result = SendRequest("POST", urlGetData, data);
            return (JArray) result["data"];
        }

        private void GetNextPage(JArray formData, int limit, JArray fields, JObject filter, string dataId)
        {
            JArray data = GetFormData(dataId, limit, fields, filter);
            if (data != null && data.Count != 0)
            {
                foreach (var item in data)
                {
                    formData.Add(item);
                }
                string lastDataId = (string)data.Last["_id"];
                GetNextPage(formData, limit, fields, filter, lastDataId);
            }
        }

        public JArray GetAllFormData(JArray fields, JObject filter)
        {
            JArray formData = new JArray();
            GetNextPage(formData, 100, fields, filter, "");
            return formData;
        }

        public JObject RetrieveData(string dataId)
        {
            JObject data = new JObject
            {
                ["data_id"] = dataId
            };
            JObject result = SendRequest("POST", urlRetrieveData, data);
            return (JObject) result["data"];
        }

        public JObject UpdateData(string dataId, JObject update)
        {
            JObject data = new JObject
            {
                ["data_id"] = dataId,
                ["data"] = update
            };
            JObject result = SendRequest("POST", urlUpdateData, data);
            return (JObject) result["data"];
        }

        public JObject CreateData(JObject data)
        {
            JObject reqData = new JObject
            {
                ["data"] = data
            };
            JObject result = SendRequest("POST", urlCreateData, reqData);
            return (JObject) result["data"];
        }

        public JObject DeleteData(string dataId)
        {
            JObject data = new JObject
            {
                ["data_id"] = dataId
            };
            return SendRequest("POST", urlDeleteData, data);
        }

    }

    class Program
    {


        static void Main(string[] args)
        {
            string appId = "5b1747e93b708d0a80667400";
            string entryId = "5b1749ae3b708d0a80667408";
            string apiKey = "CTRP5jibfk7qnnsGLCCcmgnBG6axdHiX";

            APIUtils api = new APIUtils(appId, entryId, apiKey);

            // 获取表单字段
            JArray widgets = api.GetFormWidgets();
            Console.WriteLine("表单字段：");
            Console.WriteLine(widgets);

            // 按条件获取表单字段
            JArray cond = new JArray
            {
                new JObject
                {
                    ["field"] = "_widget_1528252846720",
                    ["type"] = "text",
                    ["method"] = "empty"
                }
            };
            JObject filter = new JObject
            {
                ["rel"] = "and",
                ["cond"] = cond
            };
            JArray data = api.GetFormData(null, 100, new JArray { "_widget_1528252846720", "_widget_1528252846801" }, filter);
            Console.WriteLine("按条件查询表单数据：");
            Console.WriteLine(data);

            // 获取表单全部数据
            JArray formData = api.GetAllFormData(null, null);
            Console.WriteLine("全部表单数据：");
            Console.WriteLine(formData);

            // 新建单条数据
            JObject create = new JObject
            {
                // 单行文本
                ["_widget_1528252846720"] = new JObject
                {
                    ["value"] = "123"
                },
                // 子表单
                ["_widget_1528252846801"] = new JObject
                {
                    ["value"] = new JArray
                    {
                        new JObject
                        {
                            ["_widget_1528252846952"] = new JObject
                            {
                                ["value"] = "123"
                            }
                        }
                    }
                },
                // 数字
                ["_widget_1528252847027"] = new JObject
                {
                    ["value"] = 123
                },
                // 地址
                ["_widget_1528252846785"] = new JObject
                {
                    ["value"] = new JObject
                    {
                        ["province"] = "江苏省",
                        ["city"] = "无锡市",
                        ["district"] = "南长区",
                        ["detail"] = "清名桥街道"
                    }
                },
                // 多行文本
                ["_widget_1528252846748"] = new JObject
                {
                    ["value"] = "123123"
                }
            };
            JObject createData = api.CreateData(create);
            Console.WriteLine("创建单条数据：");
            Console.WriteLine(createData);

            // 更新单条数据
            JObject update = new JObject
            {
                // 单行文本
                ["_widget_1528252846720"] = new JObject
                {
                    ["value"] = "12345"
                },
                // 子表单
                ["_widget_1528252846801"] = new JObject
                {
                    ["value"] = new JArray
                    {
                        new JObject
                        {
                            ["_widget_1528252846952"] = new JObject
                            {
                                ["value"] = "12345"
                            }
                        }
                    }
                },
                // 数字
                ["_widget_1528252847027"] = new JObject
                {
                    ["value"] = 12345
                },
                // 地址
                ["_widget_1528252846785"] = new JObject
                {
                    ["value"] = new JObject
                    {
                        ["province"] = "江苏省",
                        ["city"] = "无锡市",
                        ["district"] = "南长区",
                        ["detail"] = "清名桥街道"
                    }
                },
                // 多行文本
                ["_widget_1528252846748"] = new JObject
                {
                    ["value"] = "123123"
                }
            };
            JObject updateResult = api.UpdateData((String) createData["_id"], update);
            Console.WriteLine("更新单条数据：");
            Console.WriteLine(updateResult);


            // 获取单条数据
            JObject retrieveData = api.RetrieveData((String)createData["_id"]);
            Console.WriteLine("查询单条数据：");
            Console.WriteLine(retrieveData);

            // 删除单条数据
            JObject delResult = api.DeleteData((String)createData["_id"]);
            Console.WriteLine("删除单条数据：");
            Console.WriteLine(delResult);

            Console.ReadLine();
        }


    }
}
