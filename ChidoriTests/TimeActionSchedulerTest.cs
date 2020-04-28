using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Chidori.Tests
{
	[TestClass]
	public class TimeActionSchedulerTest
	{
		const string CategoryAdd = "Add";
		const string CategoryRun = "Run";
		const string CategoryCollection = "Collection";

		TimeActionScheduler scheduler;
		SortedDictionary<DateTime, Queue<TimeAction>> _scheduler;
		Dictionary<string, TimeAction> _names;
		List<string> output;

		DateTime After(int n) => DateTime.Now.AddSeconds(n);
		string NowString => DateTime.Now.ToLongTimeString();

		[TestInitialize]
		public void TestInit()
		{
			scheduler = new TimeActionScheduler();
			_scheduler = scheduler.AsDynamic().scheduler;
			_names = scheduler.AsDynamic().names;
			output = new List<string>();
		}

		#region Add functions

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 時間指定でアクションの追加ができる()
		{
			// 追加できているかprivateメンバの
			// schedulerのカウントを見て確認
			_scheduler.Count.Is(0);

			TimeActionScheduler.AddError result = scheduler.Add(() => { }, After(1), "");

			result.Is(TimeActionScheduler.AddError.None);
			_scheduler.Count.Is(1);

		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void アクションの追加でnameがnamesに追加される()
		{
			string actionName = "sample";
			_names.Count.Is(0);
		
			scheduler.Add(() => { }, After(1), actionName);
			
			_names.Count.Is(1);
			_names.ContainsKey(actionName).IsTrue();
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 過去を指定すると追加しない()
		{
			scheduler.Count.Is(0);
			scheduler.Names.Count.Is(0);
			TimeActionScheduler.AddError result = 
				scheduler.Add(() => { }, DateTime.Now.AddSeconds(-1), "");

			result.Is(TimeActionScheduler.AddError.TimeIsPast);

			scheduler.Count.Is(0);
			scheduler.Names.Count.Is(0);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 既に使われている名前を指定すると追加しない()
		{
			scheduler.Add(() => { }, After(1), "");

			scheduler.Count.Is(1);
			scheduler.Names.Count.Is(1);

			TimeActionScheduler.AddError result =
				scheduler.Add(() => { }, After(1), "");

			result.Is(TimeActionScheduler.AddError.NameIsUsed);
			scheduler.Count.Is(1);
			scheduler.Names.Count.Is(1);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 追加の排他制御()
		{
			int n = 100;
			Parallel.For(0, n, i =>
			{
				scheduler.Add(() => { }, After(1), $"{i}");
			});
			scheduler.Count.Is(n);
		}

		#endregion

		[TestMethod]
		[TestCategory(CategoryRun)]
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

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void 個数カウントができる()
		{
			scheduler.Count.Is(0);
			scheduler.Add(() => { }, After(1), "");
			scheduler.Count.Is(1);
		}

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void 名前の列挙ができる()
		{
			string[] actionNames = new[] { "a", "b", "c" };
			foreach (string actionName in actionNames)
			{
				scheduler.Add(() => { }, After(1), actionName);
			}

			scheduler.Names.OrderBy(x => x).Is(actionNames);
		}

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void アクションの列挙ができる()
		{
			string[] actionNames = new[] { "a", "b", "c" };
			foreach (string actionName in actionNames)
			{
				scheduler.Add(() => { }, After(1), actionName);
			}

			scheduler.Select(x => x.Name).OrderBy(x => x).Is(actionNames);
		}

		[TestMethod]
		public void 名前からTimeActionを検索できる()
		{
			string actionName = "sample";
			scheduler.Add(() => { }, After(1), actionName);

			scheduler[actionName].Name.Is(actionName);
		}

		[TestMethod]
		public void 名前を検索して見つからなかった場合はnull()
		{
			scheduler[""].IsNull();
		}

		[TestMethod]
		public void 次に実行される時間を取得できる()
		{
			DateTime time = After(5);
			scheduler.Add(() => { }, time.AddSeconds(1), "a");
			scheduler.Add(() => { }, time, "b");
			scheduler.Add(() => { }, time.AddSeconds(3), "c");

			scheduler.PeekTime.Is(time);
		}
	}
}
