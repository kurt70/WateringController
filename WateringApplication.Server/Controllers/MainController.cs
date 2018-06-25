using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WateringApplication.Server.Infrastructure;
using WateringApplication.Server.Managers;

namespace WateringApplication.Server.Controllers
{
	public interface IMainController
	{
		void Run();

	}
	public class MainController
	{
		private ILogger _logger;

		public bool IsRunning { get; internal set; }

		public MainController(ILogger log)
		{
			_logger = log ?? throw new ArgumentException(nameof(log));

		}

		public void Run()
		{
			
			
			//run main program

		}

		public void Close()
		{
			//Send close message
			//Finish close when all have answered or timout reached
		}

	}
}
