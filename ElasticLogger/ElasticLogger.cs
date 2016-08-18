using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;

namespace ElasticLogger
{
    /// <summary>
    /// Library that logs dynamic classes to an Elastic Search instance. It may also dump the same bit to a local storage path
    /// </summary>
    public class ElasticLogger
    {
        private readonly SemaphoreSlim _elasticSemaphore = new SemaphoreSlim(1);
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1);
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ElasticLogger));
        private static ElasticLogger _instance;

        /// <summary>
        /// Gets the current instance of the logger
        /// </summary>
        public static ElasticLogger Instance
        {
            get { return _instance ?? (_instance = new ElasticLogger()); }
        }

        private ElasticLogger()
        {
        }

        public async void LogAsync(string index, string @type, dynamic obj)
        {
            if (!Settings.IsEnabled)
            {
                return;
            }

            try
            {
                // Serialize
                var serializerTask = Task.Factory.StartNew(() => JsonConvert.SerializeObject(obj) + Environment.NewLine);
                string json = await serializerTask;
                if (Settings.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                {
                    Logger.Debug("[" + @type + "] Serialized");
                }

                // Push to Elastic
                try
                {
                    string elasticResult = await LogElasticAsync(index, type, json);
                    if (Settings.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                    {
                        Logger.Debug("[" + @type + "] Logged to Elastic: " + elasticResult);
                    }
                }
                catch (Exception exception)
                {
                    if (Logger.IsErrorEnabled)
                    {
                        Logger.Error("[" + @type + "] LogElasticAsync failed", exception);
                    }
                }

                // Push to file
                if (Settings.IsLogToFileEnabled)
                {
                    try
                    {
                        await LogFileAsync(index, @type, json);

                        if (Settings.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                        {
                            Logger.Debug("[" + @type + "] Logged to file");
                        }
                    }
                    catch (Exception exception)
                    {
                        if (Logger.IsErrorEnabled)
                        {
                            Logger.Error("[" + @type + "] LogFileAsync failed", exception);
                        }
                    }
                }

                if (Settings.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                {
                    Logger.Debug("[" + @type + "] Done");
                }
            }
            catch (Exception exception)
            {
                if (Logger.IsErrorEnabled)
                {
                    Logger.Error("[" + @type + "] Failed to log", exception);
                }

                // ignored
            }
        }

        private async Task<string> LogElasticAsync(string index, string @type, string json)
        {
            try
            {
                await _elasticSemaphore.WaitAsync().ConfigureAwait(false);
                byte[] byteArray = Encoding.UTF8.GetBytes(json);

                var client = WebRequest.Create(string.Format("{0}/{1}/{2}", Settings.ElasticServer, index, type));
                client.ContentType = "application/json";
                client.Method = "POST";
                client.Timeout = (int) TimeSpan.FromSeconds(5).TotalMilliseconds;
                client.ContentLength = byteArray.Length;

                using (Stream dataStream = client.GetRequestStream())
                {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();
                    using (WebResponse response = await client.GetResponseAsync())
                    {
                        return ((HttpWebResponse) response).StatusDescription;
                    }
                }
            }
            finally
            {
                _elasticSemaphore.Release();
            }
        }

        private async Task LogFileAsync(string index, string @type, string json)
        {
            await _fileSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                await Task.Factory.StartNew(() => File.AppendAllText(
                       Settings.FileLogPath + "\\" + DateTime.Now.ToString(Settings.FileLogDatePattern) +
                       " ElasticLogger " + index + " " + @type + ".json", json, Encoding.UTF8));
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }
    }
}