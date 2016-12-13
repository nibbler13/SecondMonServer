using System;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace SecondMonServer {
	class UserNotification {
		static public void SendNotificationToUser(string address, int port, string message) {
			try {
				TcpClient tcpclnt = new TcpClient();
				Console.WriteLine("Connecting");

				tcpclnt.Connect(address, port);
				Console.WriteLine("Connected");
				Console.WriteLine("String to be transmitted: " + message);
				
				Stream stm = tcpclnt.GetStream();

				ASCIIEncoding asen = new ASCIIEncoding();
				byte[] ba = asen.GetBytes(message);
				Console.WriteLine("transmitting");

				stm.Write(ba, 0, ba.Length);

				byte[] bb = new byte[100];
				int k = stm.Read(bb, 0, 100);

				Console.WriteLine(Encoding.Default.GetString(bb));

				tcpclnt.Close();

			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}
		}
	}
}
