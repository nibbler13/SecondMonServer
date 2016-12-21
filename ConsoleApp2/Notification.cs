using System;
using System.Drawing;

namespace SecondMonServer {
	class Notification {
		public enum AvailableTypes { bday, child }

		private string title;
		private string body;
		private Color exclamation;
		private Color main;

		public Notification(AvailableTypes type) {
			if (type == AvailableTypes.bday) {
				title = "День рождения";
				body = "У текущего пациента день рождения находится в промежутке +/- 7 дней\n" + 
					"Не забудьте поставить скидку 10% в лечениях за наличный расчет";
				exclamation = Color.FromArgb(10, 10, 10);
				main = Color.FromArgb(100, 100, 100);
			} else if (type == AvailableTypes.child) {
				title = "Несовершеннолетние дети";
				body = "У текущего пациента есть дети в возрасте до 18 лет\n" +
					"Можно предложить оформить справку в бассейн";
				exclamation = Color.FromArgb(10, 50, 10);
				main = Color.FromArgb(100, 200, 100);
			} else {
				throw new ArgumentException("not available type");
			}
		}

		public override string ToString() {
			return title + "|" + body + "|" + exclamation.ToArgb().ToString() + "|" + main.ToArgb().ToString();
		}
	}
}
