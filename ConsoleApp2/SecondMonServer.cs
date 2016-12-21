using System;
using System.Data;
using System.Threading;
using System.Text;

namespace SecondMonServer{
    class SecondMonServer {
        static void Main(string[] args) {

            FBClient fbClient = new FBClient("172.16.166.2", "game");

            while (true) {
                CheckMainTable(fbClient);
                Thread.Sleep(10 * 1000);
            }
        }

        static void CheckMainTable(FBClient fbClient) {
			Console.WriteLine("checking main table at: " + DateTime.Now.ToString());

			DataTable table;
			try {
				table = fbClient.GetMainTable();
			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
				return;
			}

			if (table.Rows.Count == 0)
				return;

			DateTime now = DateTime.Now;
			
			foreach (DataRow row in table.Rows) {
				DateTime dateTime = (DateTime)row["CREATEDATE"];
				double dateDiff = now.Subtract(dateTime).TotalMinutes;
				Console.WriteLine("ID: " + row["ID"].ToString() + " - " + dateDiff);

				int updateCode = 0;

				if (dateDiff < 30) {
					Console.WriteLine("Time < 30");

					bool isTreatComplete = false;
					try {
						isTreatComplete = fbClient.IsTreatComplete(row["SCHEDID"].ToString());
					} catch (Exception e) {
						Console.WriteLine(e.Message + " @ " + e.StackTrace);
						continue;
					}

					Console.WriteLine("TreatComplete: '" + isTreatComplete + "'");

					if (isTreatComplete) {
						updateCode = 2;
					} else {
						string pcode = row["PCODE"].ToString();

						bool isBirthday = false;
						try { isBirthday = IsPatientBirthdayNear(fbClient, pcode);
						} catch (Exception e) { Console.WriteLine(e.Message + " @ " + e.StackTrace); }

						bool isChild = false;
						try { isChild = fbClient.IsPatientHasChild(pcode);
						} catch (Exception e) { Console.WriteLine(e.Message + " @ " + e.StackTrace); }

						if (!isBirthday && !isChild)
							continue;

						string ipAddress = "";
						try {
							string attachId = row["ATTACHMENT"].ToString();
							ipAddress = fbClient.GetClientIpAddress(attachId);
						} catch (Exception e) {
							Console.WriteLine(e.Message + " @ " + e.StackTrace);
							updateCode = 4;
						}

						if (ipAddress.Equals(""))
							continue;

						if (isBirthday)
							SendNorification(ipAddress, 8001, Notification.AvailableTypes.bday);

						if (isChild)
							SendNorification(ipAddress, 8001, Notification.AvailableTypes.child);

						updateCode = 1;
					}
				} else {
					updateCode = 3;
				}

				if (updateCode > 0)
					fbClient.ExecuteUpdateQuery(updateCode.ToString(), row["ID"].ToString());
			}
		}

		private static bool SendNorification(string ipAddress, int port, Notification.AvailableTypes type) {
			byte[] sendValue = Encoding.Unicode.GetBytes(new Notification(type).ToString());
			UserNotification.SendNotificationToUser(ipAddress, port, sendValue);

			return true;
		}
		
        private static bool IsPatientBirthdayNear(FBClient fbClient, string pcode) {
			DateTime bDay;
			try {
				bDay = fbClient.GetBirthdayDate(pcode);
			} catch (Exception e) {
				throw e;
			}

			DateTime now = DateTime.Now;
			bDay = new DateTime(now.Year, bDay.Month, bDay.Day);
			TimeSpan dateDiff = DateTime.Now.Subtract(bDay);
			double daysDiff = dateDiff.TotalDays;

			Console.WriteLine("bDay: " + bDay.ToString() + " totalDays: " + daysDiff);

			if (daysDiff >= -7 && daysDiff <= 7)
				return true;

			return false;
		}
    }
}
