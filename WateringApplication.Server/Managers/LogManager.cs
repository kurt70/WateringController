using Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace WateringApplication.Server.Managers
{
	public interface ILogger
	{
		void Log(string message);
		void Errror(Exception exception, string message);
	}
	class LogManager : ILogger
	{
		public void Errror(Exception exception, string message)
		{
			Console.WriteLine($"{DateTime.Now}\t[ERROR]\t{message} : {exception}.");
		}

		public void Log(string message)
		{
			Console.WriteLine($"{DateTime.Now}\t[INFO]\t{message}");
		}
	}
}
