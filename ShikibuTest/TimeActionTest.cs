using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwallowNest.Shikibu.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static SwallowNest.Shikibu.Tests.Helpers.Category;

namespace SwallowNest.Shikibu.Tests
{
    [TestClass]
    public class TimeActionTest
    {
        #region TestCategory

        public const string Invoke = "アクション実行";

        #endregion TestCategory

        private static readonly TimeSpan delta = TimeSpan.FromSeconds(0.5);
        private List<DateTime> output;
        private DateTime now;

        // 現在時刻を保存する
        private void OutputNow() => output.Add(DateTime.Now);

        [TestInitialize]
        public void TestInit()
        {
            output = new List<DateTime>();
            now = DateTime.Now;
        }

        [TestMethod]
        [TestCategory(Constructor)]
        [TestCategory(Normal)]
        public async Task 時刻指定()
        {
            TimeAction timeAction = new(OutputNow, now);

            // アクションの実行
            await timeAction.Invoke();

            output.Count.Is(1, "アクションが１回実行されている");
            timeAction.ExecTime.Is(now, "実行時間が設定されている");
            timeAction.Interval.Is(default(TimeSpan), "指定しない場合はデフォルト値");
            timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
                $"指定しない場合は{RepeatAdditionType.BeforeExecute}");
        }

        [TestMethod]
        [TestCategory(Constructor)]
        [TestCategory(Normal)]
        public async Task 間隔指定()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            TimeAction timeAction = new(OutputNow, interval);

            // アクションの実行
            await timeAction.Invoke();

            output.Count.Is(1, "アクションが１回実行されている");

            (timeAction.ExecTime - (now + interval)).WithIn(delta,
                $"Addした時刻から{interval}足した時刻に設定されている");

            timeAction.Interval.Is(interval, "時間間隔が設定されている");
            timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
                $"指定しない場合は{RepeatAdditionType.BeforeExecute}");
        }

        [TestMethod]
        [TestCategory(Constructor)]
        [TestCategory(Normal)]
        public async Task 時刻と間隔指定()
        {
            TimeSpan interval = TimeSpan.FromSeconds(1);
            TimeAction timeAction = new(OutputNow, now, interval);

            // アクションの実行
            await timeAction.Invoke();

            output.Count.Is(1, "アクションが１回実行されている");
            timeAction.ExecTime.Is(now, "指定した時刻が設定されている");
            timeAction.Interval.Is(interval, "指定した時間間隔が設定されている");
            timeAction.AdditionType.Is(RepeatAdditionType.BeforeExecute,
                $"指定しない場合は{RepeatAdditionType.BeforeExecute}");
        }

        [TestMethod]
        [TestCategory(Constructor)]
        [TestCategory(Error)]
        public void 間隔短すぎ()
        {
            TimeSpan interval = TimeSpan.FromSeconds(0.5);

            // 間隔のみ指定
            AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            {
                TimeAction timeAction = new(OutputNow, interval);
            }, $"時間間隔は{TimeAction.MinimumInterval}以上でなければならない");

            // 時刻と間隔指定
            AssertEx.Throws<ArgumentOutOfRangeException>(() =>
            {
                TimeAction timeAction = new(OutputNow, now, interval);
            }, $"時間間隔は{TimeAction.MinimumInterval}以上でなければならない");
        }

        [TestMethod]
        [TestCategory(Invoke)]
        [TestCategory(Normal)]
        public async Task CanExecuteがnull()
        {
            TimeAction timeAction = new(OutputNow, now);
            await timeAction.Invoke();

            output.Count.Is(1, "nullの場合は実行する");
        }

        [TestMethod]
        [TestCategory(Invoke)]
        [TestCategory(Normal)]
        public async Task CanExecuteがtrue()
        {
            TimeAction timeAction = new(OutputNow, now);
            timeAction.CanExecute += () => true;

            await timeAction.Invoke();
            output.Count.Is(1, "戻り値がtrueの場合は実行する");
        }

        [TestMethod]
        [TestCategory(Invoke)]
        [TestCategory(Normal)]
        public async Task CanExecuteがfalse()
        {
            TimeAction timeAction = new(OutputNow, now);
            timeAction.CanExecute += () => false;

            // OnScheduleイベントは実行されない
            await timeAction.Invoke();
            output.Count.Is(0, "戻り値がfalseの場合は実行しない");
        }
    }
}