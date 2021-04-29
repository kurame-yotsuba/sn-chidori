using SwallowNest.Shikibu;
using System;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu.Sample
{
    class Program
    {
        static void Main(string[] args)
        {
			Sample();
        }

		public static void Sample()
		{
			var sch = new TimeActionScheduler();
			var ta1 = sch.Add(() => Console.WriteLine(DateTime.Now), TimeSpan.FromSeconds(1));
			var ta2 = sch.Add(() => Console.WriteLine("Hello"), TimeSpan.FromSeconds(5));

			Task schTask = sch.Start();

			Console.ReadLine();
		}

		public static void Alert()
		{
			var sch = new TimeActionScheduler();
			var ta = sch.Add(() =>
			{
				Console.Beep();
				Console.WriteLine("キーを入力してください。");
				string key = Console.ReadLine();
				Console.WriteLine($"{key}が入力されました。");
				if (key == "q") { sch.EndImmediately(); }
			}, TimeSpan.FromMinutes(20));
			ta.AdditionType = RepeatAdditionType.AfterExecute;

			sch.Start().Wait();
		}
	}
}
