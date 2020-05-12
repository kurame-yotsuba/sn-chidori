using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Chidori
{
	/// <summary>
	/// 受け取ったタスクを時間順に実行していくクラスです。
	/// </summary>
	public class TimeActionScheduler : IEnumerable<TimeAction>
	{
		#region private member

		static readonly string timePastErrorMessage = "指定された時刻が過去です。";
		static readonly string intervalMinimumErrorMessage = $"時間間隔は{MinimumInterval.TotalSeconds}秒以上でなければなりません。";
		static readonly string nameUsedErrorMessage = "指定された名前は既に使われています。";

		// 実行待ちのタスクを時間順で格納する辞書
		readonly SortedDictionary<DateTime, Queue<TimeAction>> scheduler;

		// アクション名の辞書
		readonly Dictionary<string, TimeAction> names;

		Task? execTask;

		// スケジューラへのタスクの同時追加防止用
		readonly object schedulerSync = new object();

		// スケジューラを稼働させるかどうか
		bool Loop => Status switch
		{
			TimeActionSchedulerStatus.Stop => true,
			TimeActionSchedulerStatus.Running => true,
			TimeActionSchedulerStatus.EndWaitAll when Count > 0 => true,
			_ => false
		};

		bool Execution => Status switch
		{
			TimeActionSchedulerStatus.Running => true,
			TimeActionSchedulerStatus.EndWaitAll => true,
			_ => false
		};

		bool Appendition => Status switch
		{
			TimeActionSchedulerStatus.Running => true,
			TimeActionSchedulerStatus.Stop => true,
			_ => false
		};

		// スケジューラから次のアクションを取り出す
		TimeAction Dequeue()
		{
			lock (schedulerSync)
			{
				Count--;

				var (time, actionQueue) = scheduler.First();

				// 先頭のキューの先頭のタスクを取り出す。
				var action = actionQueue.Dequeue();

				// 先頭のキューの要素数が0なら削除
				if (scheduler[time].Count == 0)
				{
					scheduler.Remove(time);
				}

				return action;
			}
		}

		async Task CreateExecTask()
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
					if(timeAction.Name is { })
					{
						names.Remove(timeAction.Name);
					}
					if (Execution)
					{
						timeAction.Invoke();
					}
					if (Appendition && timeAction.Interval != default)
					{
						timeAction.ExecTime += timeAction.Interval;
						Add(timeAction);
					}
				}
			}
			execTask = null;
		}

		void Reflesh()
		{
			while (Count > 0 && PeekTime < DateTime.Now)
			{
				TimeAction timeAction = Dequeue();
				if (timeAction.Name is { })
				{
					names.Remove(timeAction.Name);
				}
				if(Appendition && timeAction.Interval != default)
				{
					timeAction.ExecTime += timeAction.Interval;
					Add(timeAction);
				}
			}
		}

		#endregion

		public static readonly TimeSpan MinimumInterval = TimeSpan.FromSeconds(1);


		/// <summary>
		/// コンストラクタ
		/// </summary>
		public TimeActionScheduler()
		{
			scheduler = new SortedDictionary<DateTime, Queue<TimeAction>>();
			names = new Dictionary<string, TimeAction>();
		}

		#region Collection functions

		/// <summary>
		/// アクション名を列挙します。
		/// </summary>
		public IReadOnlyCollection<string> Names => names.Keys;

		/// <summary>
		/// スケジューラに登録されているタスクの個数です。
		/// </summary>
		public int Count { get; private set; }

		/// <summary>
		/// スケジューラに登録されているアクションを削除します。
		/// </summary>
		public void Clear()
		{
			lock (schedulerSync)
			{
				scheduler.Clear();
				names.Clear();
				Count = 0;
			}
		}

		#region Implements of IEnumerable<T>

		/// <summary>
		/// スケジューラに登録されているアクションを列挙します。
		/// </summary>
		/// <returns></returns>
		public IEnumerator<TimeAction> GetEnumerator()
		{
			foreach (var (_, queue) in scheduler)
			{
				foreach (TimeAction action in queue)
				{
					yield return action;
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

		#endregion

		/// <summary>
		/// スケジューラの稼働状態を表します。
		/// </summary>
		public TimeActionSchedulerStatus Status { get; private set; }

		/// <summary>
		/// 名前に紐付くアクションを返します。
		/// アクションが見つからなかった場合、nullを返します。
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public TimeAction? this[string name] => names.ContainsKey(name) ? names[name] : null;

		#region Add functions

		void Add(TimeAction timeAction)
		{
			var (time, name) = (timeAction.ExecTime, timeAction.Name);
			lock (schedulerSync)
			{
				//指定の時間に既にタスクが入っている場合、そのタスクのあとに追加
				if (scheduler.ContainsKey(time))
				{
					scheduler[time].Enqueue(timeAction);
				}
				//そうでない場合は新しくキューを作成して追加
				else
				{
					var q = new Queue<TimeAction>();
					q.Enqueue(timeAction);
					scheduler[time] = q;
				}
				if (name is { })
				{
					names.Add(name, timeAction);
				}
				Count++;
			}
		}

		/// <summary>
		/// スケジューラに指定した時刻に実行されるアクションを追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="execTime"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public void Add(Action action, DateTime execTime, string? name = null)
		{
			// 時刻が過去を指定されていないかチェック
			if (execTime < DateTime.Now)
			{
				throw new ArgumentOutOfRangeException(nameof(execTime), timePastErrorMessage);
			}

			// 名前の重複チェック
			if (name is { } && names.ContainsKey(name))
			{
				throw new ArgumentOutOfRangeException(nameof(name), nameUsedErrorMessage);
			}

			TimeAction timeAction = new TimeAction(action, execTime, name);

			Add(timeAction);
		}

		/// <summary>
		/// スケジューラに一定間隔で繰り返し実行されるアクションを追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="interval"></param>
		/// <param name="name"></param>
		public void Add(Action action, TimeSpan interval, string? name = null)
		{
			Add(action, DateTime.Now + interval, interval, name);
		}

		/// <summary>
		/// スケジューラに指定した時刻に実行され、
		/// その後は一定間隔で繰り返し実行されるアクションを追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="execTime">最初にアクションが実行される時刻</param>
		/// <param name="interval">アクションが実行される間隔</param>
		/// <param name="name"></param>
		/// <returns></returns>
		public void Add(
			Action action,
			DateTime execTime,
			TimeSpan interval,
			string? name = null)
		{
			// 時刻が過去を指定されていないかチェック
			if (execTime < DateTime.Now)
			{
				throw new ArgumentOutOfRangeException(nameof(execTime), timePastErrorMessage);
			}

			// 繰り返し間隔が短すぎないかチェック
			if (interval < MinimumInterval)
			{
				throw new ArgumentOutOfRangeException(nameof(interval), intervalMinimumErrorMessage);
			}

			// 名前の重複チェック
			if (name is { } && names.ContainsKey(name))
			{
				throw new ArgumentOutOfRangeException(nameof(name), nameUsedErrorMessage);
			}

			TimeAction timeAction = new TimeAction(action, execTime, interval, name);
			Add(timeAction);
		}

		

		#endregion

		/// <summary>
		/// スケジューラが次にアクションを実行する時間を返します。
		/// </summary>
		public DateTime PeekTime
		{
			get
			{
				lock (schedulerSync)
				{
					var (time, _) = scheduler.First();
					return time;
				}
			}
		}

		#region Operations of scheduler status

		/// <summary>
		/// 登録されているタスクを順次実行します。
		/// </summary>
		public Task Start()
		{
			Status = TimeActionSchedulerStatus.Running;
			Reflesh();
			execTask ??= CreateExecTask();
			return execTask;
		}

		public void Stop()
		{
			Status = TimeActionSchedulerStatus.Stop;
		}

		public void EndWaitAll()
		{
			Status = TimeActionSchedulerStatus.EndWaitAll;
		}

		public void EndImmediately()
		{
			Status = TimeActionSchedulerStatus.EndImmediately;
			Clear();
		}

		#endregion
	}
}
