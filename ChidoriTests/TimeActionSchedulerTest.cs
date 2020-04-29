﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		const string CategoryExec = "Exec";
		const string CategoryCollection = "Collection";

		TimeActionScheduler scheduler;
		SortedDictionary<DateTime, Queue<TimeAction>> _scheduler;
		Dictionary<string, TimeAction> _names;
		List<string> output;

		DateTime After(int n) => DateTime.Now.AddSeconds(n);
		string NowString => DateTime.Now.ToLongTimeString();
		// n sec + α　待機
		void Wait(int n) => Task.Delay(1000 * n + 500).Wait();
		
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

			TimeActionScheduler.AddError result = scheduler.Add(() => { }, After(1));

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
				scheduler.Add(() => { }, DateTime.Now.AddSeconds(-1));

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
		public void 名前を指定しなくても追加できる()
		{
			scheduler.Add(() => { }, After(1));
			scheduler.Count.Is(1);

			// 名前で検索する辞書の方には登録されない
			scheduler.Names.Count.Is(0);
		}

		[TestMethod]
		[TestCategory(CategoryAdd)]
		public void 追加の排他制御()
		{
			int n = 100;
			Parallel.For(0, n, i =>
			{
				scheduler.Add(() => { }, After(1));
			});
			scheduler.Count.Is(n);
		}

		#endregion

		#region Exec functions

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void 指定した時間に実行される()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(() => output.Add(NowString), time);

			// スケジューラの開始
			var task = scheduler.Start();

			Wait(n);

			// 出力確認
			output[0].Is(time.ToLongTimeString());

			// 実行したあとはカウントが減っている
			scheduler.Count.Is(0);
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void Stop中は実行されない()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(() => output.Add(NowString), time);

			// スケジューラの開始
			var task = scheduler.Start();
			// すぐにストップ
			scheduler.Stop();
			scheduler.Status.Is(TimeActionSchedulerStatus.Stop);

			Wait(n);

			// 出力されていない
			output.Count.Is(0);

			// カウントは減っている
			scheduler.Count.Is(0);

			// タスクは終わっていない
			task.IsCompleted.IsFalse();
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スケジューラのアクションの実行を待ってから終了できる()
		{
			// この秒数後に実行する
			int n = 1;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(() => output.Add(NowString), time);
			scheduler.Add(() => output.Add(NowString), time);
			scheduler.Add(() => output.Add(NowString), time);

			// スケジューラの開始
			var task = scheduler.Start();
			// スケジューラにあるアクションが終わったら終了
			scheduler.EndWaitAll();
			scheduler.Status.Is(TimeActionSchedulerStatus.EndWaitAll);

			Wait(n);

			// 出力されている
			output.Count.Is(3);

			// カウントは減っている
			scheduler.Count.Is(0);

			// タスクは終了している
			task.IsCompleted.IsTrue();
		}

		[TestMethod]
		[TestCategory(CategoryExec)]
		public void スケジューラのアクションを待たずに終了できる()
		{
			// この秒数後に実行する
			int n = 5;
			DateTime time = After(n);

			// 実行時の時間を記録するアクションの追加
			scheduler.Add(() => output.Add(NowString), time);
			scheduler.Add(() => output.Add(NowString), time);
			scheduler.Add(() => output.Add(NowString), time);

			// スケジューラの開始
			var task = scheduler.Start();
			// スケジューラにあるアクションが終わったら終了
			scheduler.EndImmediately();
			scheduler.Status.Is(TimeActionSchedulerStatus.EndImmediately);

			// 少しだけ待機
			Wait(1);

			// 出力されていない
			output.Count.Is(0);

			// カウントは減っている
			scheduler.Count.Is(0);

			// タスクは終了している
			task.IsCompleted.IsTrue();
		}


		#endregion

		#region Collection functions

		[TestMethod]
		[TestCategory(CategoryCollection)]
		public void 個数カウントができる()
		{
			scheduler.Count.Is(0);
			scheduler.Add(() => { }, After(1));
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
		[TestCategory(CategoryCollection)]
		public void アクションの全削除ができる()
		{
			int n = 5;
			for(int i = 0; i < n; i++)
			{
				scheduler.Add(() => { }, After(i), $"{i}");
			}

			scheduler.Clear();

			scheduler.Count.Is(0);
			scheduler.Names.Count.Is(0);
		}

		#endregion

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
			scheduler.Add(() => { }, time.AddSeconds(1));
			scheduler.Add(() => { }, time);
			scheduler.Add(() => { }, time.AddSeconds(3));

			scheduler.PeekTime.Is(time);
		}
	}
}
