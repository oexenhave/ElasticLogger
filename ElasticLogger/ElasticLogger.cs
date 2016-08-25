using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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
    public class ElasticLogger : IDisposable
    {
        private const string AppSettingPrefix = "ElasticLogger.";
        private readonly SemaphoreSlim _flushSemaphore = new SemaphoreSlim(1);
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ElasticLogger));
        private static readonly DirectoryInfo AppPath = new FileInfo(AppDomain.CurrentDomain.BaseDirectory).Directory;

        /// <summary>
        /// Gets or sets a value indicating whether to automatically flush the cache when the hitting 
        /// the <see cref="BufferSize"/> threshold.
        /// </summary>
        public bool AutoFlush { get; set; }

        /// <summary>
        /// Gets or sets the size of the buffer to use then <see cref="AutoFlush"/> is turned on.
        /// </summary>
        public int BufferSize { get; set; }

        /// <summary>
        /// Gets or sets the number of days before log files are deleted
        /// </summary>
        public int DaysBeforeFileLogExpires { get; set; }

        /// <summary>
        /// Gets or sets the uri for the elastic server
        /// </summary>
        public Uri ElasticServer { get; set; }

        /// <summary>
        /// Gets or sets the path for storing the file logs
        /// </summary>
        public DirectoryInfo FileLogPath { get; set; }

        /// <summary>
        /// Gets or sets the date pattern used when storing the file logs
        /// </summary>
        public string FileLogDatePattern { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this logger is enabled (both Elastic and file)
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the log4net module is enabled for ElasticLogger
        /// </summary>
        public bool IsDebugLog4NetEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the file logger is enabled
        /// </summary>
        public bool IsLogToFileEnabled { get; set; }

        /// <summary>
        /// Gets a list of items currently in the queue to be flushed.
        /// </summary>
        public List<ElasticLoggerItem> Items { get; internal set; }

        /// <summary>
        /// Creates the <see cref="ElasticLogger"/> instance
        /// </summary>
        public ElasticLogger(bool initFromAppSettings = true)
        {
            this.AutoFlush = true;
            this.BufferSize = 1;
            this.DaysBeforeFileLogExpires = 7;
            this.ElasticServer = null;
            this.FileLogDatePattern = "yyyyMMdd";
            this.FileLogPath = new DirectoryInfo(AppPath + "\\Storage");
            this.IsEnabled = true;
            this.IsDebugLog4NetEnabled = false;
            this.IsLogToFileEnabled = false;
            this.Items = new List<ElasticLoggerItem>();

            if (initFromAppSettings)
            {
                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "AutoFlush"]))
                {
                    this.AutoFlush = ConfigurationManager.AppSettings[AppSettingPrefix + "AutoFlush"] == "1";
                }

                int bufferSize;
                int.TryParse(ConfigurationManager.AppSettings[AppSettingPrefix + "BufferSize"], out bufferSize);
                this.BufferSize = bufferSize;

                int daysBeforeFileLogExpires;
                int.TryParse(ConfigurationManager.AppSettings[AppSettingPrefix + "DaysBeforeFileLogExpires"], out daysBeforeFileLogExpires);
                this.DaysBeforeFileLogExpires = daysBeforeFileLogExpires;

                this.ElasticServer = new Uri(ConfigurationManager.AppSettings[AppSettingPrefix + "ElasticServer"].TrimEnd('/'));

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogDatePattern"]))
                {
                    this.FileLogDatePattern = ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogDatePattern"];
                }

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogPath"]))
                {
                    this.FileLogPath = new DirectoryInfo(ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogPath"]);
                }

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "IsEnabled"]))
                {
                    this.IsEnabled = ConfigurationManager.AppSettings[AppSettingPrefix + "IsEnabled"] == "1";
                }

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "IsDebugLog4NetEnabled"]))
                {
                    this.IsDebugLog4NetEnabled = ConfigurationManager.AppSettings[AppSettingPrefix + "IsDebugLog4NetEnabled"] == "1";
                }

                if (!string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings[AppSettingPrefix + "IsLogToFileEnabled"]))
                {
                    this.IsLogToFileEnabled = ConfigurationManager.AppSettings[AppSettingPrefix + "IsLogToFileEnabled"] == "1";
                }
            }
        }

        public async void LogAsync(string index, string type, dynamic obj)
        {
            if (!this.IsEnabled)
            {
                return;
            }

            try
            {
                // Serialize
                var serializerTask = Task.Factory.StartNew(() => JsonConvert.SerializeObject(obj) + Environment.NewLine);
                string json = await serializerTask;
                if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                {
                    Logger.Debug("[" + type + "] Serialized");
                }

                this.Items.Add(new ElasticLoggerItem { Index = index, Type = type, Json = json });
                if (this.Items.Count >= this.BufferSize && this.AutoFlush)
                {
                    await FlushAsync();
                }

                if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                {
                    Logger.Debug("[" + type + "] Done");
                }
            }
            catch (Exception exception)
            {
                if (Logger.IsErrorEnabled)
                {
                    Logger.Error("[" + type + "] Failed to log", exception);
                }

                // ignored
            }
        }

        public async Task FlushAsync()
        {
            try
            {
                await _flushSemaphore.WaitAsync().ConfigureAwait(false);
                // Push to Elastic
                try
                {
                    if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                    {
                        Logger.Debug("LogElasticAsync sending " + this.Items.Count + " items");
                    }

                    string elasticResult = await LogElasticAsync(this.Items);

                    if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                    {
                        Logger.Debug("LogElasticAsync response: " + elasticResult);
                    }
                }
                catch (Exception exception)
                {
                    if (Logger.IsErrorEnabled)
                    {
                        Logger.Error("LogElasticAsync failed", exception);
                    }
                }

                // Push to file
                if (this.IsLogToFileEnabled)
                {
                    try
                    {
                        if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                        {
                            Logger.Debug("LogFileAsync sending " + this.Items.Count + " items");
                        }

                        await LogFileAsync(this.Items);

                        if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                        {
                            Logger.Debug("LogFileAsync response: OK");
                        }
                    }
                    catch (Exception exception)
                    {
                        if (Logger.IsErrorEnabled)
                        {
                            Logger.Error("LogFileAsync failed", exception);
                        }
                    }
                }

                this.Items.Clear();
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        private async Task<string> LogElasticAsync(List<ElasticLoggerItem> items)
        {
            string json = string.Empty;
            foreach (var item in items)
            {
                json += item.ToString("\n");
            }

            if (string.IsNullOrWhiteSpace(json))
            {
                // Nothing to send. Queue is empty.
                return "Nothing to send";
            }

            byte[] byteArray = Encoding.UTF8.GetBytes(json);

            var client = WebRequest.Create(string.Format("{0}/_bulk", this.ElasticServer));
            client.ContentType = "application/json";
            client.Method = "POST";
            client.Timeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
            client.ContentLength = byteArray.Length;

            using (Stream dataStream = client.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                using (WebResponse response = await client.GetResponseAsync())
                {
                    return ((HttpWebResponse)response).StatusDescription;
                }
            }
        }

        private async Task LogFileAsync(List<ElasticLoggerItem> items)
        {
            await Task.Factory.StartNew(() =>
            {
                if (!this.FileLogPath.Exists)
                {
                    this.FileLogPath.Create();
                }

                foreach (var item in items)
                {
                    File.AppendAllText(
                        this.FileLogPath + "\\" +
                        DateTime.Now.ToString(this.FileLogDatePattern) +
                        " ElasticLogger " + item.Index + " " + item.Type + ".json", item.ToString("\r\n"),
                        Encoding.UTF8);
                }
            });
        }

        public void Dispose()
        {
            if (this.Items.Any())
            {
                if (this.IsDebugLog4NetEnabled && Logger.IsDebugEnabled)
                {
                    Logger.Debug("Disposing. Flushing due to queue not empty.");
                }

                // ReSharper disable once UnusedVariable
                var flushAsync = this.FlushAsync();
                flushAsync.Wait(1000);
            }
        }
    }
}