using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;

namespace ElasticLogger.SampleApp
{
    public class Program
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Program));
        private static readonly DirectoryInfo AppPath = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

        public static void Main(string[] args)
        {
            // Initialize console and file logging.
            XmlConfigurator.Configure(new FileInfo(AppPath + "\\log4net.config"));
            BasicConfigurator.Configure();

            try
            {
                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Running application");
                }

                ElasticLogger.Instance.LogAsync("soxportal", "event1", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox1@timelog.dk" });
                ElasticLogger.Instance.LogAsync("soxportal", "event2", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox2@timelog.dk" });
                ElasticLogger.Instance.LogAsync("soxportal", "event3", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox3@timelog.dk" });

                if (Logger.IsInfoEnabled)
                {
                    Logger.Info("Logged");
                }
            }
            catch (Exception ex)
            {
                // Catch any exception and report
                if (Logger.IsErrorEnabled)
                {
                    Logger.Error("Application loop exception", ex);
                }
            }

            if (Logger.IsInfoEnabled)
            {
                Logger.Info("---");
                Logger.Info("Application loop ended. Click to exit");
            }

            // Wait for the user to end the application.
            Console.ReadKey();
        }
    }
}
