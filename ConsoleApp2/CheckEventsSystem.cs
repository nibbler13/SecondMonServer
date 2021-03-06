﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace SecondMonServer {
	class CheckEventsSystem {
		private int previousDay = 0;
		private const string QUERY_DELETE_OLD_EVENTS = "DELETE FROM secondmon WHERE CAST (creatdate AS DATE)  < 'today'";

		private const string QUERY_GET_NEW_EVENTS =
			"SELECT smonid, pcode, treatcode, addr, pid " +
			"FROM (SELECT sm.*, s.treatcode AS streatcode, s.treatcomplete, " +
			"mon.mon$remote_address AS addr, mon.mon$remote_pid AS pid, " +
			"CASE " +
			"WHEN s.treatcode IS NOT NULL AND COALESCE(treatcomplete,0) = 0 THEN 0 " +  //лечение через расписание не завершено
			"WHEN s.treatcode IS NOT NULL AND COALESCE(treatcomplete,0) = 1 THEN 1 " +  //лечение через расписание завершено
			"WHEN s.treatcode IS NULL THEN 0 " +                                        //лечение создано через картотеку
			"END AS flag " +
			"FROM secondmon sm " +
			"LEFT JOIN schedule s ON s.treatcode = sm.treatcode " +
			"JOIN mon$attachments mon ON mon.mon$attachment_id = sm.attachid " +
			"WHERE CAST (sm.creatdate AS date) = 'today' AND" +
			"(CAST ('now' AS time) - (CAST (sm.creatdate AS time)) < 301)) " + //прошло не больше 5 минут с момента создания лечения
			"WHERE flag = 0 AND send = 0";
		
		private const string QUERY_UPDATE = 
			"UPDATE SecondMon SET Send = @updateCode WHERE smonid = @smonId";

		private const string QUERY_INSERT_NOTIFICATION =
			"INSERT INTO SECONDMONNOTIFICATIONS " +
			"(IPADDRESS, MISPID, TITLE, TEXT, COLOREXT, COLORMAIN, COLORFONT, TREATCODE) " +
			"VALUES(@ipaddress, @mispid, @title, @text, @colorext, @colormain, @colorfont, @treatcode)";

		private const string QUERY_EVENT_SOCHI =
			"select iif(sum(flag) = 2, 1, 0) from " +
			//«ПОЛИКЛИНИКА(КЛАССИЧЕСКАЯ)» для сотрудников ОСАО «Ингосстрах» и их детей до 16 лет(ИНГОСС-МОСКВА)
			//«ПОЛИКЛИНИКА(КЛАССИЧЕСКАЯ)» для руководителей ОСАО «Ингосстрах» высшего звена  и членов их семей(ИНГОСС-МОСКВА)
			//«ПОЛИКЛИНИКА(КЛАССИЧЕСКАЯ)» для руководителей структурных подразделений  ОСАО «Ингосстрах» и их детей до 16 лет(ИНГОСС-МОСКВА)
			"(select COALESCE (iif(t.lstid in (991313949,991333024,991336874), 1, 0),0) as flag " +
			"from treat t " +
			"left join JPAGREEMENT jp on t.JID = jp.AGRID where t.treatcode = @treatcode " +
			"union all " +
			//'ОТОРИНОЛАРИНГОЛОГИЯ','НЕВРОЛОГИЯ','ТРАВМАТОЛОГИЯ','КАРДИОЛОГИЯ'
			"select COALESCE (iif(t.depnum in (991328713,742,741,765), 1, 0),0) as flag1 " +
			"from treat t " +
			"left join JPAGREEMENT jp on t.JID = jp.AGRID where t.treatcode = @treatcode)";
		
		private FBClient misBase;
		private FBClient notificationBase;

		public CheckEventsSystem() {
			LoggingSystem.LogMessageToFile("Creating CheckEventsSystem");

			misBase = new FBClient(
				Properties.Settings.Default.MIS_BASE_IP_ADDRESS, 
				Properties.Settings.Default.MIS_BASE_NAME);

			notificationBase = new FBClient(
				Properties.Settings.Default.NOTIFICATION_BASE_IP_ADDRESS,
				Properties.Settings.Default.NOTIFICATION_BASE_NAME);
		}

		public void StartChecking() {
			while (true) {
				CheckEvents();
				Thread.Sleep(Properties.Settings.Default.CYCLE_INTERVAL_SECONDS * 1000);
				if (DateTime.Now.Day != previousDay)
					misBase.ExecuteUpdateQuery(QUERY_DELETE_OLD_EVENTS, new Dictionary<string, string>());
				previousDay = DateTime.Now.Day;
			}
		}

		public void CheckEvents() {
			LoggingSystem.LogMessageToFile("--- checking main table");

			DataTable newEventsTable = misBase.GetDataTable(
				QUERY_GET_NEW_EVENTS, 
				new Dictionary<string, string>());

			if (newEventsTable == null || newEventsTable.Rows.Count == 0) {
				LoggingSystem.LogMessageToFile("--- no new events");
				return;
			}

			foreach (DataRow row in newEventsTable.Rows)
				AnalyzeRowForEvents(row);
		}

		private void AnalyzeRowForEvents(DataRow row) {
			string id = "";
			string pCode = "";
			string treatCode = "";
			string ipAddress = "";
			string misPid = "";

			try {
				id = row["SMONID"].ToString();
				pCode = row["PCODE"].ToString();
				treatCode = row["TREATCODE"].ToString();
				ipAddress = row["ADDR"].ToString();
				misPid = row["PID"].ToString();
			} catch (Exception e) {
				LoggingSystem.LogMessageToFile("Cannod read the row: " + row.ToString() + " | " + 
					e.Message + " " + e.StackTrace);
			}
			
			LoggingSystem.LogMessageToFile("--- check row: " + id + " " + pCode + " " + ipAddress);

			Dictionary<string, string> parameters = new Dictionary<string, string>() {
				{ "@treatcode", treatCode }};
			DataTable queryResult = misBase.GetDataTable(QUERY_EVENT_SOCHI, parameters);
			if (queryResult != null && queryResult.Rows.Count > 0) {
				try {
					string result = queryResult.Rows[0]["CASE"].ToString();
					if (!result.Equals("0"))
						CreateNotification(ipAddress, misPid, treatCode, Notification.AvailableTypes.sochi);
				} catch (Exception e) {
					LoggingSystem.LogMessageToFile(e.Message + Environment.NewLine +
						e.StackTrace);
				}
			}
			
			UpdateRowStatus(id);
		}

		private void UpdateRowStatus(string id) {
			Dictionary<string, string> parameters = new Dictionary<string, string>() {
				{"@updateCode", "1"},
				{"@smonId", id}};

			LoggingSystem.LogMessageToFile("UpdateRowStatus, rowID: " + id + ", result: " + 
				misBase.ExecuteUpdateQuery(QUERY_UPDATE, parameters));
		}

		private void CreateNotification(string ipAddress, string misPid, string treatCode, Notification.AvailableTypes type) {
			Notification notification = new Notification(type);
			LoggingSystem.LogMessageToFile("Creating notification: " + 
				"ipAddress: " + ipAddress + " misPID: " + misPid +
				Environment.NewLine + notification.ToString());

			if (string.IsNullOrEmpty(ipAddress) ||
				string.IsNullOrEmpty(misPid) || string.IsNullOrEmpty(treatCode)) {
				LoggingSystem.LogMessageToFile("CreateNotification - некоторые параметры пусты");
				return;
			}

			Dictionary<string, string> parameters = new Dictionary<string, string>() {
				{"@ipaddress", ipAddress},
				{"@mispid", misPid},
				{"@title", notification.GetTitle()},
				{"@text", notification.GetBody()},
				{"@colorext", notification.GetColorExclamationBlinking()},
				{"@colormain", notification.GetColorMain()},
				{"@colorfont", notification.GetColorFont() },
				{"@treatcode", treatCode}};

			LoggingSystem.LogMessageToFile("Create notification status: " +
				notificationBase.ExecuteUpdateQuery(QUERY_INSERT_NOTIFICATION, parameters));
		}
	}
}
