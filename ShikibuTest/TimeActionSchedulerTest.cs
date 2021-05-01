using Microsoft.VisualStudio.TestTools.UnitTesting;
using SwallowNest.Shikibu.Tests.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SwallowNest.Shikibu.Tests.Helpers.Category;

namespace SwallowNest.Shikibu.Tests
{
    [TestClass]
    public class TimeActionSchedulerTest
    {
        private const string CategoryAdd = "Add";
        private const string CategoryExec = "Exec";
        private const string CategoryCollection = "Collection";

        // 実行時刻のずれの許容範囲
        private static readonly TimeSpan delta = TimeSpan.FromSeconds(0.5);

        private static readonly Action EmptyAction = () => { };

        // n sec あとの時刻を返す
        private static DateTime After(int n) => DateTime.Now.AddSeconds(n);

        // n sec + α 待機
        private static Task Wait(int second) => Task.Delay(1000 * second + 100);

        private TimeActionScheduler scheduler;

        private List<DateTime> output;

        // 実行時刻を保存するアクション
        private void OutputNow() => output.Add(DateTime.Now);

        [TestInitialize]
        public void TestInit()
        {
            scheduler = new TimeActionScheduler();
            output = new List<DateTime>();
        }

        [TestMethod]
        public void Constructor()
        {
            scheduler.Count.Is(0, "初期値は0");
            scheduler.TimePeek.Is(DateTime.MaxValue, "空の場合は最大値");
            scheduler.Status.Is(TimeActionSchedulerStatus.Stop, "初期値はStop");
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void アクション追加後に要素数が1増える()
        {
            scheduler.Count.Is(0, "初期値は0");
            scheduler.Add(EmptyAction, default(DateTime));
            scheduler.Count.Is(1, "要素数が1増えている");
        }

        [TestMethod]
        [TestCategory(Normal)]
        public async Task アクション実行後に要素数が1減る()
        {
            TimeAction timeAction = new(EmptyAction, After(1));
            scheduler.Add(timeAction);
            _ = scheduler.Start();

            await Wait(1);

            scheduler.Count.Is(0, "要素数が1減っている");
        }

        [TestMethod]
        [TestCategory(Normal)]
        public async Task 指定した時刻に実行()
        {
            // アクションが実行される予定時刻
            DateTime invokingTime = After(1);

            TimeAction timeAction = new(OutputNow, invokingTime);
            scheduler.Add(timeAction);

            _ = scheduler.Start();
            await Wait(1);
            scheduler.Stop();

            // 実行済み
            output.Count.Is(1);
            (output[0] - invokingTime).WithIn(delta);
        }

        [TestMethod]
        [TestCategory(Normal)]
        public async Task 一定時間で繰り返し実行()
        {
            // アクションが実行される予定時刻
            DateTime[] invokingTimes = new[]
            {
                After(1),
                After(2),
                After(3),
            };

            // アクションを実行した時刻
            List<DateTime> invokedTimes = new();
            TimeAction timeAction = scheduler.Add(() => invokedTimes.Add(DateTime.Now), TimeSpan.FromSeconds(1));

            _ = scheduler.Start();
            await Wait(3);
            scheduler.Stop();

            // スケジューラにはアクションが残っている
            scheduler.Count.Is(1);

            // アクションは3回実行された
            invokedTimes.Count.Is(3);

            // 実行予定時刻と実行された時刻がほぼ同じ
            foreach (var (invokingTime, invokedTime) in invokingTimes.Zip(invokedTimes))
            {
                (invokedTime - invokingTime).WithIn(delta);
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public async Task 指定時刻に実行しその後は一定時間で繰り返し実行()
        {
            // アクションが実行される予定時刻
            DateTime[] invokingTimes = new[]
            {
                After(2),
                After(3),
            };

            // アクションを実行した時刻
            List<DateTime> invokedTimes = new();
            TimeAction timeAction = scheduler.Add(() => invokedTimes.Add(DateTime.Now), After(2), TimeSpan.FromSeconds(1));

            _ = scheduler.Start();
            await Wait(3);
            scheduler.Stop();

            // スケジューラにはアクションが残っている
            scheduler.Count.Is(1);

            // アクションは2回実行された
            invokedTimes.Count.Is(2);

            // 実行予定時刻と実行された時刻がほぼ同じ
            foreach (var (invokingTime, invokedTime) in invokingTimes.Zip(invokedTimes))
            {
                (invokedTime - invokingTime).WithIn(delta);
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void アクションの全削除ができる()
        {
            int n = 5;
            for (int i = 1; i <= n; i++)
            {
                scheduler.Add(EmptyAction, default(DateTime));
            }

            scheduler.Count.Is(n, "n個になってる");
            scheduler.Clear();
            scheduler.Count.Is(0, "0になってる");
        }

        [TestMethod]
        [TestCategory(CategoryAdd)]
        public void マルチスレッドで追加()
        {
            int n = 100;
            Parallel.For(0, n, i =>
            {
                scheduler.Add(EmptyAction, default(DateTime));
            });

            // 全部追加されている
            scheduler.Count.Is(n);
        }

        #region Exec functions

        [TestMethod]
        [TestCategory(Normal)]
        public async Task Stop中は実行されない()
        {
            // この秒数後に実行する
            int n = 1;
            DateTime invokingTime = After(n);
            bool invoked = false;

            // 実行時の時間を記録するアクションの追加
            scheduler.Add(() => invoked = true, invokingTime);

            // スケジューラの開始
            Task schedulerTask = scheduler.Start();

            // すぐにストップ
            scheduler.Stop();
            scheduler.Status.Is(TimeActionSchedulerStatus.Stop, "ストップされてる");

            await Wait(n);

            invoked.IsFalse("実行されていない");
            scheduler.Count.Is(0, "カウントは減っている");
            schedulerTask.IsCompleted.IsFalse("タスクは終わっていない");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task スケジューラのアクションの実行を待ってから終了できる()
        {
            // この秒数後に実行する
            int n = 1;
            DateTime time = After(1);

            // 実行時の時間を記録するアクションの追加
            scheduler.Add(OutputNow, time);
            scheduler.Add(OutputNow, time);
            scheduler.Add(OutputNow, time);

            // スケジューラの開始
            Task schedulerTask = scheduler.Start();
            // スケジューラにあるアクションが終わったら終了
            scheduler.EndWaitAll();
            scheduler.Status.Is(TimeActionSchedulerStatus.EndWaitAll);

            await Wait(n);

            output.Count.Is(3, "すべて出力されている");
            scheduler.Count.Is(0, "カウントは減っている");
            schedulerTask.IsCompleted.IsTrue("タスクは終了している");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task スケジューラのアクションを待たずに終了できる()
        {
            // 実行時の時間を記録するアクションの追加
            // 1つだけ早く実行する
            scheduler.Add(OutputNow, After(1));
            scheduler.Add(OutputNow, After(3));
            scheduler.Add(OutputNow, After(3));

            // スケジューラの開始
            Task schedulerTask = scheduler.Start();

            await Wait(2);

            // スケジューラにあるアクションの終了を待たずに終了
            scheduler.EndImmediately();
            scheduler.Status.Is(TimeActionSchedulerStatus.EndImmediately);

            // 待機
            await Wait(2);

            // 最初のアクションだけ実行されている
            output.Count.Is(1);

            // カウントは減っている
            scheduler.Count.Is(0);

            // タスクは終了している
            schedulerTask.IsCompleted.IsTrue();
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task スタート時点で過去のアクションは実行しない()
        {
            scheduler.Add(OutputNow, After(1));

            await Wait(2);

            _ = scheduler.Start();

            output.Count.Is(0, "過去のアクションは実行されない");

            scheduler.Count.Is(0, "スケジューラのカウントは減っている");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task スタート時点で過去の繰り返しアクションは過去の時刻基準で再度追加()
        {
            DateTime now = DateTime.Now;
            DateTime invokingTime = After(4);
            scheduler.Add(OutputNow, TimeSpan.FromSeconds(2));

            await Wait(3);

            output.Count.Is(0, "スタート直前の出力のカウント");
            _ = scheduler.Start();
            output.Count.Is(0, "スタート直後の出力のカウント");

            await Wait(2);

            output.Count.Is(1, "待機後の出力のカウント");
            (output[0] - invokingTime).WithIn(delta);
            scheduler.Count.Is(1, "カウントは減っていない");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task スタート時点で過去の時刻指定繰り返しアクションは指定時刻基準で再度追加()
        {
            DateTime time = After(2);
            DateTime invokingTime = After(5);
            scheduler.Add(OutputNow, time, TimeSpan.FromSeconds(3));

            await Wait(3);

            output.Count.Is(0, "スタート直前の出力のカウント");
            _ = scheduler.Start();
            output.Count.Is(0, "スタート直後の出力のカウント");

            await Wait(2);

            output.Count.Is(1, "待機後の出力のカウント");
            (output[0] - invokingTime).WithIn(delta);
            scheduler.Count.Is(1, "スケジューラのカウント");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task 繰り返しアクションをアクション実行前に追加()
        {
            // 2秒ごとに繰り返すアクションだけど、
            // アクションが終了するのに3秒以上かかる
            // outputに出力されるのは5秒後と8秒後
            TimeAction timeAction = new TimeAction(
                () => { Wait(3).Wait(); OutputNow(); },
                TimeSpan.FromSeconds(2))
            {
                // アクション実行前にスケジューラに繰り返しアクションを追加するオプション
                AdditionType = RepeatAdditionType.BeforeExecute
            };
            scheduler.Add(timeAction);
            _ = scheduler.Start();

            await Wait(3);

            output.Count.Is(0, "最初のアクションはまだ実行中なので結果は空。");
            scheduler.Count.Is(1, "既に次のアクションが追加されている。");

            await Wait(2);
            output.Count.Is(1, "最初のアクションの出力がされている。");

            await Wait(3);
            output.Count.Is(2, "２番目のアクションの出力がされている。");
        }

        [TestMethod]
        [TestCategory(CategoryExec)]
        public async Task 繰り返しアクションをアクション実行後に追加()
        {
            // 2秒ごとに繰り返すアクションだけど、
            // アクションが終了するのに3秒以上かかる
            // outputに出力されるのは5秒後と10秒後
            TimeAction timeAction = new(
                 () => { Wait(3).Wait(); OutputNow(); },
                TimeSpan.FromSeconds(2))
            {
                // アクション実行後にスケジューラに繰り返しアクションを追加するオプション
                AdditionType = RepeatAdditionType.AfterExecute
            };
            scheduler.Add(timeAction);
            _ = scheduler.Start();

            await Wait(3);

            output.Count.Is(0, "最初のアクションはまだ実行中なので結果は空。");
            scheduler.Count.Is(0, "次のアクションの追加もまだされていない。");

            await Wait(2);
            output.Count.Is(1, "最初のアクションの出力がされている。");
            scheduler.Count.Is(1, "次のアクションの追加がされている。");

            await Wait(3);
            output.Count.Is(1, "２番目のアクションの出力はまだされていない。");

            await Wait(2);
            output.Count.Is(2, "２番目のアクションの出力がされている。");
        }

        #endregion Exec functions

        [TestMethod]
        [TestCategory(Normal)]
        public void TimePeekスケジューラが空()
        {
            scheduler.TimePeek.Is(DateTime.MaxValue);
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void TimePeekスケジューラが空じゃない()
        {
            DateTime time = DateTime.Now.AddSeconds(1);
            scheduler.Add(() => { }, time.AddSeconds(1));
            scheduler.Add(() => { }, time);
            scheduler.Add(() => { }, time.AddSeconds(3));

            scheduler.TimePeek.Is(time);
        }
    }
}