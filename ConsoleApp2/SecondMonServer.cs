using System;
using System.Threading;
using System.ServiceProcess;

namespace SecondMonServer{
    public static class SecondMonServer {
		public class Service : ServiceBase {
			public Service() {

			}

			protected override void OnStart(string[] args) {
				SecondMonServer.Start(args);
			}

			protected override void OnStop() {
				SecondMonServer.Stop();
			}
		}

        static void Main(string[] args) {
			if (!Environment.UserInteractive)
				using (Service service = new Service())
					ServiceBase.Run(service);
			else {
				Start(args);

				Console.WriteLine("Press any key to stop...");
				Console.ReadKey(true);

				Stop();
			}
        }

		private static void Start(string[] args) {
			LoggingSystem.LogMessageToFile("Starting, cycle interval in seconds: " +
				Properties.Settings.Default.CYCLE_INTERVAL_SECONDS);
			CheckEventsSystem checkEventSystem = new CheckEventsSystem();
			Thread thread = new Thread(() => checkEventSystem.StartChecking());
			thread.Start();
		}

		private static void Stop() {

		}
    }
}
