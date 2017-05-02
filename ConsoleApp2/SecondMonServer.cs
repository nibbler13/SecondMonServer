using System;
using System.Threading;

namespace SecondMonServer{
    class SecondMonServer {
        static void Main(string[] args) {
			LoggingSystem.LogMessageToFile("Starting, cycle interval in seconds: " +
				Properties.Settings.Default.CYCLE_INTERVAL_SECONDS);
			CheckEventsSystem checkEventSystem = new CheckEventsSystem();

            while (true) {
				checkEventSystem.CheckEvents();
				Thread.Sleep(Properties.Settings.Default.CYCLE_INTERVAL_SECONDS * 1000);
            }
        }
    }
}
