using System;
using System.Text;
using System.IO;
using System.Net.Sockets;

namespace SecondMonServer {
	class UserNotification {
		static public bool SendNotificationToUser(string address, int port, string message) {
			if (address.Equals("") ||
				port.Equals("") ||
				message.Equals(""))
				throw new ArgumentNullException("some argument is empty");

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

				if (bb.Length > 0)
					return true;

			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}

			return false;
		}
	}
}
