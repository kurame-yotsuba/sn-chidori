using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Chidori
{
	/// <summary>
	/// 受け取ったタスクを時間順に実行していくクラスです。
	/// </summary>
	public class TimeActionScheduler : IEnumerable<TimeAction>
	{
		#region innner member

		public enum AddError
		{
			/// <summary>
			/// エラーはありません。
			/// </summary>
			None,

			/// <summary>
			/// 指定された時刻が過去です。
			/// </summary>
			TimeIsPast,

			/// <summary>
			/// 指定された名前は既に使われています。
			/// </summary>
			NameIsUsed,
		}

		#endregion

		#region private member

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
					TimeAction action = Dequeue();
					names.Remove(action.Name);
					if (Status == TimeActionSchedulerStatus.Running
						|| Status == TimeActionSchedulerStatus.EndWaitAll)
					{
						action.Invoke();
					}
				}
			}
		}

		#endregion

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

		/// <summary>
		/// スケジューラに関数を追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="time"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public AddError Add(Action action, DateTime time, string? name = null)
		{
			// 引数チェック
			if (time < DateTime.Now)
			{
				return AddError.TimeIsPast;
			}

			if (name is { } && names.ContainsKey(name))
			{
				return AddError.NameIsUsed;
			}

			lock (schedulerSync)
			{
				TimeAction scheduledAction = new TimeAction(action, name ?? "");

				//指定の時間に既にタスクが入っている場合、そのタスクのあとに追加
				if (scheduler.ContainsKey(time))
				{
					scheduler[time].Enqueue(scheduledAction);
				}
				//そうでない場合は新しくキューを作成して追加
				else
				{
					var q = new Queue<TimeAction>();
					q.Enqueue(scheduledAction);
					scheduler[time] = q;
				}
				if(name is { })
				{
					names.Add(name, scheduledAction);
				}
				Count++;
				return AddError.None;
			}
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

		/// <summary>
		/// 登録されているタスクを順次実行します。
		/// </summary>
		public Task Start()
		{
			Status = TimeActionSchedulerStatus.Running;
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

		public void Clear()
		{
			lock (schedulerSync)
			{
				scheduler.Clear();
				Count = 0;
			}
		}
	}
}
