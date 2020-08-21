﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SwallowNest.Chidori
{
	/// <summary>
	/// 受け取ったタスクを時間順に実行していくクラスです。
	/// </summary>
	public class TimeActionScheduler
	{
		#region private member

		// 実行待ちのアクションを格納する実行時刻をキーとする順序付き辞書
		private readonly SortedDictionary<DateTime, LinkedList<TimeAction>> scheduler;

		// スケジューラのタスク
		private Task? execTask;

		// スケジューラへのタスクの同時追加防止用
		private readonly object schedulerSync = new object();

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
				Count--;

				var (time, list) = scheduler.First();

				// 先頭のキューの先頭のタスクを取り出す。
				TimeAction action = list.First.Value;
				list.RemoveFirst();

				// 先頭のキューの要素数が0なら削除
				if (scheduler[time].Count == 0)
				{
					scheduler.Remove(time);
				}

				return action;
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
		private void Invoke(TimeAction timeAction)
		{
			if (Execution) { timeAction.Invoke(); }
		}

		private async Task CreateExecTask()
		{
			while (Loop)
			{
				if (Count == 0 || DateTime.Now < PeekTime)
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
							Invoke(timeAction);
							break;
						// 実行後に追加する場合
						case RepeatAdditionType.AfterExecute:
							Invoke(timeAction);
							Append(timeAction, DateTime.Now + timeAction.Interval);
							break;
					}
				}
			}
			execTask = null;
		}

		private void Reflesh()
		{
			while (Count > 0 && PeekTime < DateTime.Now)
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

		/// <summary>
		/// <see cref="TimeAction"/>を管理するインスタンスを生成します。
		/// </summary>
		public TimeActionScheduler()
		{
			scheduler = new SortedDictionary<DateTime, LinkedList<TimeAction>>();
		}

		#region Collection functions

		/// <summary>
		/// スケジューラに登録されているタスクの個数です。
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// スケジューラに登録されているアクションをすべて削除します。
		/// </summary>
		public void Clear()
		{
			lock (schedulerSync)
			{
				scheduler.Clear();
				Count = 0;
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
			DateTime time = timeAction.ExecTime;

			lock (schedulerSync)
			{
				//指定の時間に既にタスクが入っている場合、そのタスクのあとに追加
				if (scheduler.ContainsKey(time))
				{
					scheduler[time].AddLast(timeAction);
				}
				//そうでない場合は新しくキューを作成して追加
				else
				{
					var list = new LinkedList<TimeAction>();
					list.AddLast(timeAction);
					scheduler[time] = list;
				}

				Count++;
			}
		}

		/// <summary>
		/// 指定した時刻に一度だけ実行されるアクションをスケジューラに追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="execTime"></param>
		/// <returns>スケジューラに追加された<see cref="TimeAction"/>インスタンス</returns>
		public TimeAction Add(Action action, DateTime execTime)
		{
			TimeAction timeAction = new TimeAction(action, execTime);

			Add(timeAction);

			return timeAction;
		}

		/// <summary>
		/// 指定した時刻に実行され、
		/// その後は一定間隔で繰り返し実行されるアクションをスケジューラに追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="execTime">最初にアクションが実行される時刻</param>
		/// <param name="interval">アクションが実行される間隔</param>
		/// <returns>スケジューラに追加された<see cref="TimeAction"/>インスタンス</returns>
		public TimeAction Add(
			Action action,
			DateTime execTime,
			TimeSpan interval)
		{
			TimeAction timeAction = new TimeAction(action, execTime, interval);
			Add(timeAction);

			return timeAction;
		}

		/// <summary>
		/// 一定間隔で繰り返し実行されるアクションをスケジューラに追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="interval"></param>
		/// <returns>スケジューラに追加された<see cref="TimeAction"/>インスタンス</returns>
		public TimeAction Add(Action action, TimeSpan interval)
		{
			return Add(action, DateTime.Now + interval, interval);
		}

		#endregion Add functions

		/// <summary>
		/// スケジューラが次にアクションを実行する時間を返します。
		/// スケジューラが空の場合、<see cref="DateTime.MaxValue"/>を返します。
		/// </summary>
		public DateTime PeekTime
		{
			get
			{
				lock (schedulerSync)
				{
					var (time, _) = scheduler.FirstOrDefault();
					return time == default ? DateTime.MaxValue : time;
				}
			}
		}

		#region Operations of scheduler status

		/// <summary>
		/// 登録されているアクションを順次実行します。
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
