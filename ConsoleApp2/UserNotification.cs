using System;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace SecondMonServer {
	class UserNotification {
		static public bool SendNotificationToUser(string address, int port, byte[] message) {
			if (address.Equals("") ||
				port.Equals("") ||
				message.Equals(""))
				throw new ArgumentNullException("some argument is empty");

			try {
				TcpClient tcpClient = new TcpClient();
				Console.WriteLine("Connecting");

				tcpClient.Connect(address, port);
				Console.WriteLine("Connected");
				Console.WriteLine("Message length to be transmitted: " + message.Length);
				
				Stream stm = tcpClient.GetStream();

				Console.WriteLine("transmitting");

				byte[] length = BitConverter.GetBytes(message.Length);

				stm.Write(length, 0, 4);
				stm.Write(message, 0, message.Length);

				byte[] bb = new byte[100];
				int k = stm.Read(bb, 0, 100);

				Console.WriteLine(Encoding.Default.GetString(bb));

				tcpClient.Close();

				if (bb.Length > 0)
					return true;

			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}

			return false;
		}
	}
}
