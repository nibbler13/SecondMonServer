using System;
using FirebirdSql.Data.FirebirdClient;
using System.Data;

namespace SecondMonServer {
    class FBClient {
        private FbConnection connection;

        public FBClient(string dataSource, string dataBase) {
            FbConnectionStringBuilder cs = new FbConnectionStringBuilder();
            cs.DataSource = dataSource;
            cs.Database = dataBase;
            cs.UserID = "SYSDBA";
            cs.Password = "masterkey";
            cs.Charset = "NONE";
            cs.Pooling = false;

            string fbConnectionString = cs.ToString();

            connection = new FbConnection(fbConnectionString);
        }

		public DataTable GetTableBirthday(string pcode) {
			string queryBday = "Select BDate From Clients Where PCode = " + pcode;
			DataTable dataTable = GetDataTable(queryBday);
			return dataTable;
		}

		public DataTable GetTableTreatComplete(string schedID) {
			string queryTreatComplete = "Select TreatComplete From Schedule Where SchedId = " + schedID;
			DataTable dataTable = GetDataTable(queryTreatComplete);
			return dataTable;
		}

		public DataTable GetMainTable() {
			string queryMain = "SELECT * FROM SecondMon WHERE Send = 0";
			DataTable dataTable = GetDataTable(queryMain);
			return dataTable;
		}

        public DataTable GetDataTable(string query) {
			try {
				connection.Open();
				FbCommand command = new FbCommand(query, connection);
				DataTable dataTable = new DataTable();
				FbDataAdapter fbDataAdapter = new FbDataAdapter(command);
				fbDataAdapter.Fill(dataTable);
				connection.Close();

				return dataTable;
			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}

			return null;
        }

        public void ExecuteUpdateQuery(string updateCode, string id) {
			try {
				connection.Open();
				string queryUpdate = "Update SecondMon Set Send = @updateCode Where Id = @id";
				FbCommand update = new FbCommand(queryUpdate, connection);
				update.Parameters.AddWithValue("@updateCode", updateCode);
				update.Parameters.AddWithValue("@id", id);
				Console.WriteLine("Updated: " + update.ExecuteNonQuery());
				connection.Close();
			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}
		}
    }
}
