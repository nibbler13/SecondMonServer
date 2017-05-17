using System;
using System.Drawing;

namespace SecondMonServer {

	[Serializable]
	class Notification {
		public enum AvailableTypes { sochi, child }

		private string title;
		private string body;
		private string colorExclamationBlinking;
		private string colorMain;
		private string colorFont;

		public Notification(AvailableTypes type) {
			colorFont = ConvertColorToHtml(Color.FromArgb(100, 100, 100));

			if (type == AvailableTypes.sochi) {
				title = "Внимание!";
				body = "Сотрудникам СПАО 'Ингосстрах'" + Environment.NewLine + "Вы можете предложить " +
					"реабилитационно - восстановительное лечение в Сочи";
				colorExclamationBlinking = ConvertColorToHtml(Color.FromArgb(125, 212, 116));
				colorMain = ConvertColorToHtml(Color.FromArgb(220, 255, 216));
			} else if (type == AvailableTypes.child) {
				title = "Несовершеннолетние дети";
				body = "У текущего пациента есть дети в возрасте до 18 лет. " +
					"Можно предложить оформить справку в бассейн";
				colorExclamationBlinking = ConvertColorToHtml(Color.FromArgb(224, 134, 219));
				colorMain = ConvertColorToHtml(Color.FromArgb(255, 213, 253));
			} else {
				Console.WriteLine("public Notification(AvailableTypes type) - not available type");
				title = "";
				body = "";
				colorExclamationBlinking = "ffffff";
				colorMain = "ffffff";
			}
		}

		public string GetTitle() {
			return title;
		}

		public string GetBody() {
			return body;
		}

		public string GetColorExclamationBlinking() {
			return colorExclamationBlinking;
		}

		public string GetColorMain() {
			return colorMain;
		}

		public String GetColorFont() {
			return colorFont;
		}

		private string ConvertColorToHtml(Color color) {
			string htmlColor = ColorTranslator.ToHtml(color);
			return htmlColor.Remove(0, 1);
		}

		public override string ToString() {
			return "Title: " + title + Environment.NewLine +
				"Body: " + body + Environment.NewLine +
				"ColorExclamationBlinking: " + colorExclamationBlinking + Environment.NewLine +
				"ColorMain: " + colorMain + Environment.NewLine +
				"ColorFont: " + colorFont;
		}
	}
}
