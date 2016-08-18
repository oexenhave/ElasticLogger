using System;
using System.Configuration;
using System.IO;
using System.Reflection;

namespace ElasticLogger
{
    public class Settings
    {
        private const string AppSettingPrefix = "ElasticLogger.";

        public static readonly DirectoryInfo AppPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

        /// <summary>
        /// Gets the number of days before log files are deleted
        /// </summary>
        public static int DaysBeforeFileLogExpires
        {
            get
            {
                int days;
                if (int.TryParse(ConfigurationManager.AppSettings[AppSettingPrefix + "DaysBeforeFileLogExpires"], out days))
                {
                    days = 7;
                }

                return days;
            }
        }

        public static Uri ElasticServer
        {
            get { return new Uri(ConfigurationManager.AppSettings[AppSettingPrefix + "ElasticServer"].TrimEnd('/')); }
        }

        /// <summary>
        /// Gets a value indicating whether this logger is enabled (both Elastic and file)
        /// </summary>
        public static bool IsEnabled
        {
            get { return ConfigurationManager.AppSettings[AppSettingPrefix + "IsEnabled"] == "1"; }
        }

        /// <summary>
        /// Gets a value indicating whether the file logger is enabled
        /// </summary>
        public static bool IsLogToFileEnabled
        {
            get { return ConfigurationManager.AppSettings[AppSettingPrefix + "IsLogToFileEnabled"] == "1"; }
        }


        /// <summary>
        /// Gets the path for storing the file logs
        /// </summary>
        public static DirectoryInfo FileLogPath
        {
            get
            {
                var logfilepath = ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogPath"];

                if (string.IsNullOrWhiteSpace(logfilepath))
                {
                    logfilepath = AppPath + "\\Storage";
                    Directory.CreateDirectory(logfilepath);
                }

                var info = new DirectoryInfo(logfilepath);
                if (info.Exists)
                {
                    return info;
                }

                throw new ConfigurationErrorsException("The path for " + AppSettingPrefix + "FileLogPath is invalid (" + logfilepath + ")");
            }
        }

        public static string FileLogDatePattern
        {
            get
            {
                var datePattern = ConfigurationManager.AppSettings[AppSettingPrefix + "FileLogDatePattern"];

                if (string.IsNullOrWhiteSpace(datePattern))
                {
                    datePattern = "yyyyMMdd";
                }

                return datePattern;
            }
        }

        public static bool IsDebugLog4NetEnabled
        {
            get { return ConfigurationManager.AppSettings[AppSettingPrefix + "IsDebugLog4NetEnabled"] == "1"; }
        }

    }
}
