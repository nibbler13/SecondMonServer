using System;
using System.Data;
using System.Threading;

namespace SecondMonServer{
    class SecondMonServer {
        static void Main(string[] args) {

            FBClient fbClient = new FBClient("172.16.166.2", "game");

            while (true) {
                CheckMainTable(fbClient);
                Thread.Sleep(30 * 1000);
            }
        }

        static void CheckMainTable(FBClient fbClient) {
			Console.WriteLine("checking main table at: " + DateTime.Now.ToString());

			DataTable dataTable = fbClient.GetMainTable();
			DateTime now = DateTime.Now;

			foreach (DataRow row in dataTable.Rows) {
				DateTime dateTime = (DateTime)row["CREATEDATE"];
				double dateDiff = now.Subtract(dateTime).TotalMinutes;
				Console.WriteLine("ID: " + row["ID"].ToString() + " - " + dateDiff);

				int updateCode = 0;

				if (dateDiff < 30) {
					Console.WriteLine("Time < 30");
					DataTable result = fbClient.GetTableTreatComplete(row["SCHEDID"].ToString());

					Console.WriteLine("TreatComplete: '" + result.Rows[0][0] + "'");

					if (result.Rows[0][0].ToString() == "1") {
						updateCode = 2;
					} else {
						CheckPatientBirthdayNear(fbClient, row["PCODE"].ToString());

						updateCode = 1;
					}

				} else {
					updateCode = 3;
				}

				if (updateCode > 0)
					fbClient.ExecuteUpdateQuery(updateCode.ToString(), row["ID"].ToString());
			}
		}
		
        static void CheckPatientBirthdayNear(FBClient fbClient, string pcode) {
			DataTable table = fbClient.GetTableBirthday(pcode);
			DateTime now = DateTime.Now;
			DateTime bDay = (DateTime)table.Rows[0][0];
			bDay = new DateTime(now.Year, bDay.Month, bDay.Day);
			TimeSpan dateDiff = DateTime.Now.Subtract(bDay);
			double daysDiff = dateDiff.TotalDays;

			Console.WriteLine("bDay: " + bDay.ToString() + " totalDays: " + daysDiff);

			if (daysDiff >= -7 && daysDiff <= 7)
				UserNotification.SendNotificationToUser("172.16.166.12", 8001, "birthday");
		}
    }
}
