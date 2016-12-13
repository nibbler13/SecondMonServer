using System;
using System.IO;
using System.Net;
using System.Text;
using System.Net.Sockets;
using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Timers;
using System.Threading;

namespace SecondMonServer{
    class SecondMonServer {
        static void Main(string[] args) {
            FbConnectionStringBuilder cs = new FbConnectionStringBuilder();
            cs.DataSource = "172.16.166.2";
            cs.Database = "game";
            cs.UserID = "SYSDBA";
            cs.Password = "masterkey";
            cs.Charset = "NONE";
            cs.Pooling = false;

            string fbConnectionString = cs.ToString();

            while (true) {
                CheckMainTable(fbConnectionString);
                Thread.Sleep(30 * 1000);
            }
        }

        static void CheckMainTable(String fbConnectionString) {
            Console.WriteLine("checking main table at: " + DateTime.Now.ToString());
            FbConnection connection = new FbConnection(fbConnectionString);

            try {
                connection.Open();
                string queryMain = "SELECT * FROM SecondMon WHERE Send = 0";
                DataTable dataTable = GetDataTable(connection, queryMain);

                DateTime now = DateTime.Now;

                foreach (DataRow row in dataTable.Rows) {
                    DateTime dateTime = (DateTime)row["CREATEDATE"];
                    double dateDiff = now.Subtract(dateTime).TotalMinutes;
                    Console.WriteLine("ID: " + row["ID"].ToString() + " - " + dateDiff);

                    int updateCode = 0;

                    if (dateDiff < 30) {
                        Console.WriteLine("Time < 30");
                        string queryTreatComplete = "Select TreatComplete From Schedule Where SchedId = " + row["SCHEDID"];
                        DataTable result = GetDataTable(connection, queryTreatComplete);

                        Console.WriteLine("TreatComplete: '" + result.Rows[0][0] + "'");

                        if (result.Rows[0][0].ToString() == "1") {
                            updateCode = 2;
                        } else {
                            CheckPatientBirthdayNear(connection, row["PCODE"].ToString());

                            updateCode = 1;
                        }

                    } else {
                        updateCode = 3;
                    }

                    if (updateCode > 0) {
                        ExecuteUpdateQuery(connection, updateCode.ToString(), row["ID"].ToString());
                    }
                }

                connection.Close();
            } catch (Exception e) {
                Console.WriteLine(e.Message + " @ " + e.StackTrace);
            }
        }

        static void SendNotificationToUser(string address, int port, string message) {
            try {
                TcpClient tcpclnt = new TcpClient();
                Console.WriteLine("Connecting");

                tcpclnt.Connect(address, port);
                Console.WriteLine("Connected");
                Console.WriteLine("String to be transmitted: " + message);

                //String str = Console.ReadLine();
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

        static DataTable GetDataTable(FbConnection connection, string query) {
            FbCommand command = new FbCommand(query, connection);
            DataTable dataTable = new DataTable();
            FbDataAdapter fbDataAdapter = new FbDataAdapter(command);
            fbDataAdapter.Fill(dataTable);

            return dataTable;
        }

        static void ExecuteUpdateQuery(FbConnection connection, string updateCode, string id) {
            string queryUpdate = "Update SecondMon Set Send = @updateCode Where Id = @id";
            FbCommand update = new FbCommand(queryUpdate, connection);
            update.Parameters.AddWithValue("@updateCode", updateCode);
            update.Parameters.AddWithValue("@id", id);
            Console.WriteLine("Updated: " + update.ExecuteNonQuery());
        }

        static void CheckPatientBirthdayNear(FbConnection connection, string pcode) {
            try {
                string queryBday = "Select BDate From Clients Where PCode = " + pcode;
                DataTable table = GetDataTable(connection, queryBday);
                DateTime now = DateTime.Now;
                DateTime bDay = (DateTime)table.Rows[0][0];
                bDay = new DateTime(now.Year, bDay.Month, bDay.Day);
                TimeSpan dateDiff = DateTime.Now.Subtract(bDay);
                double daysDiff = dateDiff.TotalDays;

                Console.WriteLine("bDay: " + bDay.ToString() + " totalDays: " + daysDiff);

                if (daysDiff >= -7 && daysDiff <= 7) {
                    SendNotificationToUser("172.16.166.12", 8001, "birthday");
                }

            } catch (Exception e) {
                Console.WriteLine(e.Message + " @ " + e.StackTrace);
            }
        }
    }
}
