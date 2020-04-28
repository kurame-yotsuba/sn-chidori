using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Chidori.Tests
{
	[TestClass]
	public class TimeActionSchedulerTest
	{
		TimeActionScheduler scheduler;
		SortedDictionary<DateTime, Queue<TimeAction>> _scheduler;
		List<string> output;

		DateTime After(int n) => DateTime.Now.AddSeconds(n);
		string NowString => DateTime.Now.ToLongTimeString();

		[TestInitialize]
		public void TestInit()
		{
			scheduler = new TimeActionScheduler();
			_scheduler = scheduler.AsDynamic().scheduler;
			output = new List<string>();
		}

		[TestMethod]
		public void 時間指定でアクションの追加ができる()
		{
			// 追加できているかprivateメンバの
			// schedulerのカウントを見て確認
			_scheduler.Count.Is(0);
			scheduler.Add(() => { }, After(1), "");
			_scheduler.Count.Is(1);
		}

		[TestMethod]
		public void 個数カウントができる()
		{
			scheduler.Count.Is(0);
			scheduler.Add(() => { }, After(1), "");
			scheduler.Count.Is(1);
		}

		[TestMethod]
		public void 指定した時間に実行される()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(() => output.Add(NowString), time, "");

			// スケジューラの開始
			var task = scheduler.Run();

			// 指定した秒数＋α待機
			Task.Delay(1000 * n + 500).Wait();

			// 出力確認
			output[0].Is(time.ToLongTimeString());

			// 実行したあとはカウントが減っている
			scheduler.Count.Is(0);
		}
	}
}
