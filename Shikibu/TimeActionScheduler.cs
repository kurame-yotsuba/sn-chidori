using System;
using System.Threading.Tasks;

namespace SwallowNest.Shikibu
{
    /// <summary>
    /// 受け取ったタスクを時間順に実行していくクラスです。
    /// </summary>
    public class TimeActionScheduler
    {
        #region private member

        // 実行待ちのアクションを格納する実行時刻をキーとする順序付き辞書
        private readonly PriorityQueue<TimeAction, DateTime> scheduler = new();

        // スケジューラのタスク
        private Task? execTask;

        // スケジューラへのタスクの同時追加防止用
        private readonly object schedulerSync = new();

        #region conditions

        // スケジューラを稼働させるかどうか
        private bool Loop => Status switch
        {
            TimeActionSchedulerStatus.Stop => true,
            TimeActionSchedulerStatus.Running => true,
            TimeActionSchedulerStatus.EndWaitAll when Count > 0 => true,
            _ => false
        };

        private bool Execution => Status switch
        {
            TimeActionSchedulerStatus.Running => true,
            TimeActionSchedulerStatus.EndWaitAll => true,
            _ => false
        };

        private bool Appendition => Status switch
        {
            TimeActionSchedulerStatus.Running => true,
            TimeActionSchedulerStatus.Stop => true,
            _ => false
        };

        #endregion conditions

        // スケジューラから次のアクションを取り出す
        private TimeAction Dequeue()
        {
            lock (schedulerSync)
            {
                return scheduler.Dequeue();
            }
        }

        // 繰り返しアクションを追加する関数
        private void Append(TimeAction timeAction, DateTime nextExecTime)
        {
            if (Appendition && timeAction.Interval != default)
            {
                timeAction.ExecTime = nextExecTime;
                Add(timeAction);
            }
        }

        // アクションを実行する関数
        private async ValueTask Invoke(TimeAction timeAction)
        {
            if (Execution)
            {
                try
                {
                    await timeAction.Invoke();
                }
                catch (Exception e)
                {
                    OnError?.Invoke(e);
                }
            }
        }

        private async Task CreateExecTask()
        {
            while (Loop)
            {
                if (Count == 0 || DateTime.Now < TimePeek)
                {
                    await Task.Delay(1000);
                    continue;
                }
                else
                {
                    TimeAction timeAction = Dequeue();

                    switch (timeAction.AdditionType)
                    {
                        // 実行前に追加する場合
                        case RepeatAdditionType.BeforeExecute:
                            Append(timeAction, timeAction.ExecTime + timeAction.Interval);
                            await Invoke(timeAction);
                            break;
                        // 実行後に追加する場合
                        case RepeatAdditionType.AfterExecute:
                            await Invoke(timeAction);
                            Append(timeAction, DateTime.Now + timeAction.Interval);
                            break;
                    }
                }
            }
            execTask = null;
        }

        private void Reflesh()
        {
            while (Count > 0 && TimePeek < DateTime.Now)
            {
                TimeAction timeAction = Dequeue();

                if (Appendition && timeAction.Interval != default)
                {
                    timeAction.ExecTime += timeAction.Interval;
                    Add(timeAction);
                }
            }
        }

        #endregion private member

        #region Collection functions

        /// <summary>
        /// アクション実行時に投げられるエラーのハンドラ
        /// </summary>
        public event Action<Exception>? OnError;

        /// <summary>
        /// スケジューラに登録されているタスクの個数です。
        /// </summary>
        public int Count => scheduler.Count;

        /// <summary>
        /// スケジューラに登録されているアクションをすべて削除します。
        /// </summary>
        public void Clear()
        {
            lock (schedulerSync)
            {
                scheduler.Clear();
            }
        }

        #endregion Collection functions

        /// <summary>
        /// スケジューラの稼働状態を表します。
        /// </summary>
        public TimeActionSchedulerStatus Status { get; private set; }

        #region Add functions

        /// <summary>
        /// スケジューラにアクションを追加します。
        /// </summary>
        /// <param name="timeAction"></param>
        public void Add(TimeAction timeAction)
        {
            lock (schedulerSync)
            {
                scheduler.Enqueue(timeAction, timeAction.ExecTime);
            }
        }

        /// <summary>
        /// 指定した時刻に一度だけ実行される <see cref="TimeAction"/> をスケジューラに追加します。
        /// </summary>
        /// <param name="action">実行されるデリゲート</param>
        /// <param name="execTime"><paramref name="action"/> が実行される時刻</param>
        /// <param name="name">アクションの表示名</param>
        /// <returns>スケジューラに追加された <see cref="TimeAction"/> インスタンス</returns>
        public TimeAction Add(Action action, DateTime execTime, string name = "")
        {
            TimeAction timeAction = new(action, execTime)
            {
                Name = name
            };

            Add(timeAction);

            return timeAction;
        }

        /// <summary>
        /// 指定した時刻に実行され、
        /// その後は一定間隔で繰り返し実行される <see cref="TimeAction"/> をスケジューラに追加します。
        /// </summary>
        /// <param name="action">実行されるデリゲート</param>
        /// <param name="execTime">最初に <paramref name="action"/> が実行される時刻</param>
        /// <param name="interval"><paramref name="action"/> が実行される間隔</param>
        /// <param name="name">アクションの表示名</param>
        /// <returns>スケジューラに追加された <see cref="TimeAction"/> インスタンス</returns>
        public TimeAction Add(
            Action action,
            DateTime execTime,
            TimeSpan interval,
            string name = "")
        {
            TimeAction timeAction = new(action, execTime, interval)
            {
                Name = name
            };
            Add(timeAction);

            return timeAction;
        }

        /// <summary>
        /// 一定間隔で繰り返し実行される <see cref="TimeAction"/> をスケジューラに追加します。
        /// </summary>
        /// <param name="action">実行されるデリゲート</param>
        /// <param name="interval"><paramref name="action"/> が実行される間隔</param>
        /// <param name="name">アクションの表示名</param>
        /// <returns>スケジューラに追加された <see cref="TimeAction"/> インスタンス</returns>
        public TimeAction Add(Action action, TimeSpan interval, string name = "")
        {
            return Add(action, DateTime.Now + interval, interval, name);
        }

        #endregion Add functions

        /// <summary>
        /// スケジューラが次に <see cref="TimeAction"/> を実行する時間を返します。
        /// スケジューラが空の場合、<see cref="DateTime.MaxValue"/> を返します。
        /// </summary>
        public DateTime TimePeek
        {
            get
            {
                lock (schedulerSync)
                {
                    if (scheduler.TryPriorityPeek(out var time))
                    {
                        return time;
                    }
                    else
                    {
                        return DateTime.MaxValue;
                    }
                }
            }
        }

        #region Operations of scheduler status

        /// <summary>
        /// スケジューラを開始して、登録されているアクションを順次実行します。
        /// </summary>
        public Task Start()
        {
            Status = TimeActionSchedulerStatus.Running;
            Reflesh();
            execTask ??= CreateExecTask();
            return execTask;
        }

        /// <summary>
        /// アクションの実行を停止します。
        /// </summary>
        public void Stop()
        {
            Status = TimeActionSchedulerStatus.Stop;
        }

        /// <summary>
        /// 現在登録されているアクションを実行したのち、
        /// スケジューラを終了します。
        /// </summary>
        public void EndWaitAll()
        {
            Status = TimeActionSchedulerStatus.EndWaitAll;
        }

        /// <summary>
        /// 現在登録されているアクションをすべて削除し、
        /// ただちにスケジューラを終了します。
        /// </summary>
        public void EndImmediately()
        {
            Status = TimeActionSchedulerStatus.EndImmediately;
            Clear();
        }

        #endregion Operations of scheduler status
    }
}