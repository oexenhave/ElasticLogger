using System;
using System.IO;
using System.Reflection;
using System.Threading;
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

                using (var logger = new ElasticLogger(true))
                {
                    logger.LogAsync("soxportal", "event1", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox1@timelog.dk" });
                    logger.LogAsync("soxportal", "event1", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox2@timelog.dk" });
                    logger.LogAsync("soxportal", "event1", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox3@timelog.dk" });
                    logger.LogAsync("soxportal", "event4", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox4@timelog.dk" });
                    logger.LogAsync("soxportal", "event5", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox5@timelog.dk" });
                    logger.LogAsync("soxportal", "event6", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox6@timelog.dk" });
                    logger.LogAsync("soxportal", "event7", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox7@timelog.dk" });
                    logger.LogAsync("soxportal", "event8", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox8@timelog.dk" });
                    logger.LogAsync("soxportal", "event9", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox9@timelog.dk" });
                    logger.LogAsync("soxportal", "event10", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox10@timelog.dk" });
                    logger.LogAsync("soxportal", "event11", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox11@timelog.dk" });
                    logger.LogAsync("soxportal", "event12", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox12@timelog.dk" });
                    logger.LogAsync("soxportal", "event13", new { Timestamp = DateTime.Now, Event = "login", Account = "local", Username = "sox13@timelog.dk" });
                }

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
