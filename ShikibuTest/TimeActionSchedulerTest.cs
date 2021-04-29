using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu.Tests
{
	[TestClass]
	public class TimeActionSchedulerTest
	{
		private const string CategoryAdd = "Add";
		private const string CategoryExec = "Exec";
		private const string CategoryCollection = "Collection";

		// 実行時刻のずれの許容範囲
		private static readonly TimeSpan delta = TimeSpan.FromSeconds(0.1);

		private TimeActionScheduler scheduler;

		// schedulerの内部でTimeActionを管理するコレクション
		private SortedDictionary<DateTime, LinkedList<TimeAction>> _scheduler;

		private List<DateTime> output;

		// n sec あとの時刻を返す
		private DateTime After(int n) => DateTime.Now.AddSeconds(n);

		// n sec + α 待機
		private void Wait(int second) => Task.Delay(1000 * second + 100).Wait();

		// 実行時刻の保存
		private void OutputNow() => output.Add(DateTime.Now);

		[TestInitialize]
		public void TestInit()
		{
			scheduler = new TimeActionScheduler();
			_scheduler = scheduler.AsDynamic().scheduler;
			output = new List<DateTime>();
		}

		[TestMethod]
		public void Constructor()
		{
			TimeActionScheduler scheduler = new TimeActionScheduler();
			scheduler.Count.Is(0, "初期値は0");
			scheduler.PeekTime.Is(DateTime.MaxValue, "空の場合は最大値");
			scheduler.Status.Is(TimeActionSchedulerStatus.Stop, "初期値はStop");
		}

		#region Collection functions

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void 個数カウントができる()
		{
			scheduler.Count.Is(0, "初期値は0");
			scheduler.Add(() => { }, default(DateTime));
			scheduler.Count.Is(1, "1個増えてる");
		}

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void アクションの全削除ができる()
		{
			int n = 5;
			for (int i = 1; i <= n; i++)
			{
				scheduler.Add(() => { }, default(DateTime));
			}

			scheduler.Count.Is(n, "n個になってる");
			scheduler.Clear();
			_scheduler.Count.Is(0, "全部消えてる");
			scheduler.Count.Is(0, "0になってる");
		}

		#endregion Collection functions

		#region Add functions

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void Add_TimeAction()
		{
			TimeAction timeAction = new TimeAction(() => { }, default(DateTime));
			scheduler.Add(timeAction);

			// 追加できているかprivateメンバの
			// schedulerを見て確認
			_scheduler.Count.Is(1);
			_scheduler.ContainsKey(timeAction.ExecTime).IsTrue();
			_scheduler[timeAction.ExecTime].Count.Is(1);
			_scheduler[timeAction.ExecTime].First.Value.Is(timeAction);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void Add_時間指定アクション()
		{
			TimeAction timeAction = scheduler.Add(() => { }, default(DateTime));

			_scheduler.Count.Is(1);
			_scheduler.ContainsKey(timeAction.ExecTime).IsTrue();
			_scheduler[timeAction.ExecTime].Count.Is(1);
			_scheduler[timeAction.ExecTime].First.Value.Is(timeAction);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void Add_一定時間で繰り返すアクション()
		{
			TimeAction timeAction = scheduler.Add(() => { }, TimeSpan.FromSeconds(1));

			_scheduler.Count.Is(1);
			_scheduler.ContainsKey(timeAction.ExecTime).IsTrue();
			_scheduler[timeAction.ExecTime].Count.Is(1);
			_scheduler[timeAction.ExecTime].First.Value.Is(timeAction);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void Add_時刻指定かつ繰り返すアクション()
		{
			TimeAction timeAction = scheduler.Add(() => { }, default, TimeSpan.FromSeconds(1));

			_scheduler.Count.Is(1);
			_scheduler.ContainsKey(timeAction.ExecTime).IsTrue();
			_scheduler[timeAction.ExecTime].Count.Is(1);
			_scheduler[timeAction.ExecTime].First.Value.Is(timeAction);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 追加の排他制御()
		{
			int n = 100;
			Parallel.For(0, n, i =>
			{
				scheduler.Add(() => { }, default(DateTime));
			});
			scheduler.Count.Is(n);
		}

		#endregion Add functions

		#region Exec functions

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void 指定した時間に実行される()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(OutputNow, time);

			// スケジューラの開始
			scheduler.Status.Is(TimeActionSchedulerStatus.Stop, "スケジューラは動いていない");
			var task = scheduler.Start();
			scheduler.Status.Is(TimeActionSchedulerStatus.Running, "スケジューラ実行中");

			Wait(n + 1);

			// 出力確認
			output.Count.Is(1, "アクションが１回実行されている");
			output[0].Is(execTime => execTime - time < delta, "指定した時刻に実行されている");
			scheduler.Count.Is(0, "実行したあとはカウントが減っている");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void 繰り返し実行される()
		{
			DateTime now = DateTime.Now;
			DateTime exec1 = now.AddSeconds(2);
			DateTime exec2 = now.AddSeconds(4);
			scheduler.Add(OutputNow, TimeSpan.FromSeconds(2));
			scheduler.Start();

			Wait(4);

			output.Count.Is(2, "アクションが２回実行されている");
			output[0].Is(execTime => execTime - exec1 < delta);
			output[1].Is(execTime => execTime - exec2 < delta);
			scheduler.Count.Is(1, "カウントは減っていない");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void Stop中は実行されない()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(OutputNow, time);

			// スケジューラの開始
			var task = scheduler.Start();

			// すぐにストップ
			scheduler.Stop();
			scheduler.Status.Is(TimeActionSchedulerStatus.Stop, "ストップされてる");

			Wait(n);

			output.Count.Is(0, "出力されていない");
			scheduler.Count.Is(0, "カウントは減っている");
			task.IsCompleted.IsFalse("タスクは終わっていない");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スケジューラのアクションの実行を待ってから終了できる()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(1);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(OutputNow, time);
			scheduler.Add(OutputNow, time);
			scheduler.Add(OutputNow, time);

			// スケジューラの開始
			var task = scheduler.Start();
			// スケジューラにあるアクションが終わったら終了
			scheduler.EndWaitAll();
			scheduler.Status.Is(TimeActionSchedulerStatus.EndWaitAll);

			Wait(n + 1);

			output.Count.Is(3, "すべて出力されている");
			scheduler.Count.Is(0, "カウントは減っている");
			task.IsCompleted.IsTrue("タスクは終了している");

			Task execTask = scheduler.AsDynamic().execTask;
			execTask.IsNull("execTask変数はnullになっている");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スケジューラのアクションを待たずに終了できる()
		{
			// 実行時の時間を記録するアクションの追加
			// 1つだけ早く実行する
			scheduler.Add(OutputNow, After(1));
			scheduler.Add(OutputNow, After(3));
			scheduler.Add(OutputNow, After(3));

			// スケジューラの開始
			var task = scheduler.Start();

			Wait(2);

			// スケジューラにあるアクションの終了を待たずに終了
			scheduler.EndImmediately();
			scheduler.Status.Is(TimeActionSchedulerStatus.EndImmediately);

			// 待機
			Wait(2);

			// 最初のアクションだけ実行されている
			output.Count.Is(1);

			// カウントは減っている
			scheduler.Count.Is(0);

			// タスクは終了している
			task.IsCompleted.IsTrue();

			// execTask変数はnullになっている
			Task execTask = scheduler.AsDynamic().execTask;
			execTask.IsNull();
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スタート時点で過去のアクションは実行しない()
		{
			scheduler.Add(OutputNow, After(1));

			Wait(2);

			scheduler.Start();

			output.Count.Is(0, "過去のアクションは実行されない");

			scheduler.Count.Is(0, "スケジューラのカウントは減っている");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スタート時点で過去の繰り返しアクションは過去の時刻基準で再度追加()
		{
			DateTime now = DateTime.Now;
			DateTime exec = now.AddSeconds(6);
			scheduler.Add(OutputNow, TimeSpan.FromSeconds(2));

			Wait(3);

			output.Count.Is(0, "スタート直前の出力のカウント");
			scheduler.Start();
			output.Count.Is(0, "スタート直後の出力のカウント");

			Wait(2);

			output.Count.Is(1, "待機後の出力のカウント");
			output[0].Is(execTime => execTime - exec < delta);
			scheduler.Count.Is(1, "カウントは減っていない");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スタート時点で過去の時刻指定繰り返しアクションは指定時刻基準で再度追加()
		{
			DateTime time = After(2);
			DateTime exec = time.AddSeconds(5);
			scheduler.Add(OutputNow, time, TimeSpan.FromSeconds(3));

			Wait(3);

			output.Count.Is(0, "スタート直前の出力のカウント");
			scheduler.Start();
			output.Count.Is(0, "スタート直後の出力のカウント");

			Wait(2);

			output.Count.Is(1, "待機後の出力のカウント");
			output[0].Is(execTime => execTime - exec < delta);
			scheduler.Count.Is(1, "スケジューラのカウント");
		}


		[TestMethod]
		[TestCategory(CategoryExec)]
		public void 繰り返しアクションをアクション実行前に追加()
		{
			// 2秒ごとに繰り返すアクションだけど、
			// アクションが終了するのに3秒以上かかる
			// outputに出力されるのは5秒後と8秒後
			TimeAction timeAction = new TimeAction(
				() => { Wait(3); OutputNow(); },
				TimeSpan.FromSeconds(2))
			{
				// PreExecuteを指定
				AdditionType = RepeatAdditionType.BeforeExecute
			};
			scheduler.Add(timeAction);
			scheduler.Start();

			Wait(3);

			output.Count.Is(0, "最初のアクションはまだ実行中なので結果は空。");
			scheduler.Count.Is(1, "既に次のアクションが追加されている。");

			Wait(2);
			output.Count.Is(1, "最初のアクションの出力がされている。");

			Wait(3);
			output.Count.Is(2, "２番目のアクションの出力がされている。");
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void 繰り返しアクションをアクション実行後に追加()
		{
			// 2秒ごとに繰り返すアクションだけど、
			// アクションが終了するのに3秒以上かかる
			// outputに出力されるのは5秒後と10秒後
			TimeAction timeAction = new TimeAction(
				() => { Wait(3); OutputNow(); },
				TimeSpan.FromSeconds(2))
			{
				// PreExecuteを指定
				AdditionType = RepeatAdditionType.AfterExecute
			};
			scheduler.Add(timeAction);
			scheduler.Start();

			Wait(3);

			output.Count.Is(0, "最初のアクションはまだ実行中なので結果は空。");
			scheduler.Count.Is(0, "次のアクションの追加もまだされていない。");

			Wait(2);
			output.Count.Is(1, "最初のアクションの出力がされている。");
			scheduler.Count.Is(1, "次のアクションの追加がされている。");

			Wait(3);
			output.Count.Is(1, "２番目のアクションの出力はまだされていない。");

			Wait(2);
			output.Count.Is(2, "２番目のアクションの出力がされている。");
		}

		#endregion Exec functions

		[TestMethod]
		public void PeekTime_スケジューラが空()
		{
			scheduler.PeekTime.Is(DateTime.MaxValue);
		}

		[TestMethod]
		public void PeekTime_スケジューラが空じゃない()
		{
			DateTime time = DateTime.Now.AddSeconds(1);
			scheduler.Add(() => { }, time.AddSeconds(1));
			scheduler.Add(() => { }, time);
			scheduler.Add(() => { }, time.AddSeconds(3));

			scheduler.PeekTime.Is(time);
		}
	}
}
