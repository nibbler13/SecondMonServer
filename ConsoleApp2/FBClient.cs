using System;
using FirebirdSql.Data.FirebirdClient;
using System.Data;

namespace SecondMonServer {
    class FBClient {
        private FbConnection connection;
		private string queryBDay;
		private string queryTreatComplete;
		private string queryMain;
		private string queryFamily;
		private string queryIpAddress;

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

			queryBDay = "Select BDate From Clients Where PCode = @pcode";
			queryTreatComplete = "Select TreatComplete From Schedule Where SchedId = @schedid";
			queryMain = "SELECT * FROM SecondMon WHERE Send = 0";
			queryFamily = "Select Count(*) From Clients Where HFamily = @pcode And (Select * From HowOld (BDate))<18";
			queryIpAddress = "Select Mon$Remote_Address From Mon$Attachments Where Mon$Attachment_Id = @attachId";
		}

		public DateTime GetBirthdayDate(string pcode) {
			if (pcode.Equals(""))
				throw new ArgumentNullException("pcode is empty");

			DataTable table = GetDataTable(queryBDay.Replace("@pcode", pcode));
			if (table == null)
				throw new NullReferenceException("birthday table is null");

			DateTime date = (DateTime)table.Rows[0][0];

			return date;
		}

		public DataTable GetMainTable() {
			DataTable table = GetDataTable(queryMain);

			if (table == null)
				throw new NullReferenceException("main table is null");

			return table;
		}

		public bool IsTreatComplete(string schedId) {
			if (schedId.Equals(""))
				throw new ArgumentNullException("schedid is empty");

			DataTable table = GetDataTable(queryTreatComplete.Replace("@schedid", schedId));
			if (table == null)
				throw new NullReferenceException("treatcomplete table is null");

			if (table.Rows[0][0].ToString().Equals("1"))
				return true;

			return false;
		}

		public bool IsPatientHasChild(string pcode) {
			if (pcode.Equals(""))
				throw new ArgumentNullException("pcode is empty");

			DataTable table = GetDataTable(queryFamily.Replace("@pcode", pcode));

			if (table == null)
				throw new NullReferenceException("child table is null");

			if (Int16.Parse(table.Rows[0][0].ToString()) > 0)
				return true;

			return false;
		}

		public string GetClientIpAddress(string attachId) {
			if (attachId.Equals(""))
				throw new ArgumentNullException("attachId is empty");

			DataTable table = GetDataTable(queryIpAddress.Replace("@attachId", attachId));
			if (table == null)
				throw new NullReferenceException("remoteAddress table is null");
			
			return table.Rows[0][0].ToString();
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

        public bool ExecuteUpdateQuery(string updateCode, string id) {
			try {
				connection.Open();
				string queryUpdate = "Update SecondMon Set Send = @updateCode Where Id = @id";
				FbCommand update = new FbCommand(queryUpdate, connection);
				update.Parameters.AddWithValue("@updateCode", updateCode);
				update.Parameters.AddWithValue("@id", id);
				int updated = update.ExecuteNonQuery();
				connection.Close();

				if (updated > 0)
					return true;
			} catch (Exception e) {
				Console.WriteLine(e.Message + " @ " + e.StackTrace);
			}

			return false;
		}
    }
}
