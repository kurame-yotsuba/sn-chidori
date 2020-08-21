using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace SwallowNest.Chidori.Tests
{
	[TestClass]
	public class TimeActionTest
	{
		private static readonly TimeSpan delta = TimeSpan.FromSeconds(0.5);
		private List<DateTime> output;
		private DateTime now;

		// 現在時刻を保存する
		private void OutputNow() => output.Add(DateTime.Now);

		// イベントを無理矢理実行する用
		private static void InvokeOnSchedule(TimeAction timeAction)
		{
			timeAction.AsDynamic().OnSchedule.Invoke();
		}

		// TimeActionを無理矢理実行する用
		private static void Invoke(TimeAction timeAction)
		{
			timeAction.AsDynamic().Invoke();
		}

		[TestInitialize]
		public void TestInit()
		{
			output = new List<DateTime>();
			now = DateTime.Now;
		}

		[TestMethod]
		public void Constructor_時刻指定()
		{
			TimeAction timeAction = new TimeAction(OutputNow, now);

			InvokeOnSchedule(timeAction);
			output.Count.Is(1, "アクションが１回実行されている");
			timeAction.ExecTime.Is(now, "実行時間が設定されている");
			timeAction.Interval.Is(default(TimeSpan), "指定しない場合はデフォルト値");
			timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
				$"指定しない場合は{RepeatAdditionType.BeforeExecute}");
		}

		[TestMethod]
		public void Constructor_間隔指定()
		{
			TimeSpan span = TimeSpan.FromSeconds(1);
			TimeAction timeAction = new TimeAction(OutputNow, span);

			InvokeOnSchedule(timeAction);
			output.Count.Is(1, "アクションが１回実行されている");
			timeAction.ExecTime.Is(execTime => execTime - (now + span) < delta,
				$"Addした時刻から{span}足した時刻に設定されている");
			timeAction.Interval.Is(span, "時間間隔が設定されている");
			timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
				$"指定しない場合は{RepeatAdditionType.BeforeExecute}");
		}

		[TestMethod]
		public void Constructor_時刻と間隔指定()
		{
			TimeSpan span = TimeSpan.FromSeconds(1);
			TimeAction timeAction = new TimeAction(OutputNow, now, span);

			InvokeOnSchedule(timeAction);
			output.Count.Is(1, "アクションが１回実行されている");
			timeAction.ExecTime.Is(now, "指定した時刻が設定されている");
			timeAction.Interval.Is(span, "指定した時間間隔が設定されている");
			timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
				$"指定しない場合は{RepeatAdditionType.BeforeExecute}");
		}

		[TestMethod]
		public void 時間間隔の最小値()
		{
			TimeAction.MinimumInterval.Is(TimeSpan.FromSeconds(1));
		}

		[TestMethod]
		public void Constructor_間隔短すぎ()
		{
			TimeSpan span = TimeSpan.FromSeconds(0.5);

			// 間隔のみ指定
			AssertEx.Throws<ArgumentOutOfRangeException>(() =>
			{
				TimeAction timeAction = new TimeAction(OutputNow, span);
			}, $"時間間隔は{TimeAction.MinimumInterval}以上でなければならない");

			// 時刻と間隔指定
			AssertEx.Throws<ArgumentOutOfRangeException>(() =>
			{
				TimeAction timeAction = new TimeAction(OutputNow, now, span);
			}, $"時間間隔は{TimeAction.MinimumInterval}以上でなければならない");
		}

		[TestMethod]
		public void Invoke_CanExecuteIsNull()
		{
			TimeAction timeAction = new TimeAction(OutputNow, now);

			Invoke(timeAction);
			output.Count.Is(1, "nullの場合は実行する");
		}

		[TestMethod]
		public void Invoke_CanExecuteIsTrue()
		{
			TimeAction timeAction = new TimeAction(OutputNow, now);
			timeAction.CanExecute += () => true;

			Invoke(timeAction);
			output.Count.Is(1, "戻り値がtrueの場合は実行する");
		}

		[TestMethod]
		public void Invoke_CanExecuteIsFalse()
		{
			TimeAction timeAction = new TimeAction(OutputNow, now);
			timeAction.CanExecute += () => false;

			// OnScheduleイベントは実行されない
			Invoke(timeAction);
			output.Count.Is(0, "戻り値がfalseの場合は実行しない");
		}
	}
}
