﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SwallowNest.Chidori
{
	/// <summary>
	/// 受け取ったタスクを時間順に実行していくクラスです。
	/// </summary>
	public class ScheduledActionQueue
	{
		#region innner member

		public enum AddError
		{
			None,
			InvalidDateTime,
		}

		public readonly struct AddResult
		{
			public readonly ScheduledAction? Result;
			public readonly AddError Error;

			internal AddResult(ScheduledAction? result, AddError error)
			{
				Result = result;
				Error = error;
			}
		}

		#endregion

		#region private member

		//実行待ちのタスクを時間順で格納する辞書
		readonly SortedDictionary<DateTime, Queue<ScheduledAction>> scheduler;

		//スケジューラへのタスクの同時追加防止用
		readonly object schedulerSync = new object();

		// スケジューラを稼働させるかどうか
		bool Repeating => Status switch
		{
			ScheduledActionQueueStatus.Stop => true,
			ScheduledActionQueueStatus.Running => true,
			ScheduledActionQueueStatus.WaitAllEnd when Count > 0 => true,
			_ => false
		};

		// スケジューラから次のアクションを取り出す
		ScheduledAction Dequeue()
		{
			lock (schedulerSync)
			{
				Count--;

				var (time, actionQueue) = scheduler.First();

				//先頭のキューの先頭のタスクを取り出す。
				var action = actionQueue.Dequeue();

				//先頭のキューの要素数が0なら削除
				if (scheduler[time].Count == 0)
				{
					scheduler.Remove(time);
				}

				return action;
			}
		}

		#endregion

		/// <summary>
		/// スケジューラの稼働状態を表します。
		/// </summary>
		public ScheduledActionQueueStatus Status { get; set; }

		/// <summary>
		/// コンストラクタ
		/// </summary>
		public ScheduledActionQueue()
		{
			scheduler = new SortedDictionary<DateTime, Queue<ScheduledAction>>();
		}

		/// <summary>
		/// スケジューラに関数を追加します。
		/// </summary>
		/// <param name="action"></param>
		/// <param name="time"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public AddResult Add(Action action, DateTime time, string name = "")
		{
			// 引数チェック
			if (time < DateTime.Now)
			{
				return new AddResult(null, AddError.InvalidDateTime);
			}

			lock (schedulerSync)
			{
				ScheduledAction scheduledAction = new ScheduledAction(action, Count, name);

				//指定の時間に既にタスクが入っている場合、そのタスクのあとに追加
				if (scheduler.ContainsKey(time))
				{
					scheduler[time].Enqueue(scheduledAction);
				}
				//そうでない場合は新しくキューを作成して追加
				else
				{
					var q = new Queue<ScheduledAction>();
					q.Enqueue(scheduledAction);
					scheduler[time] = q;
				}
				Count++;
				return new AddResult(scheduledAction, AddError.None);
			}
		}

		public int Count { get; private set; }

		/// <summary>
		/// スケジューラが次にアクションを実行する時間を返します。
		/// </summary>
		public DateTime PeekTime
		{
			get
			{
				lock (schedulerSync)
				{
					var (time, scheduledTasks) = scheduler.First();
					return time;
				}
			}
		}

		/// <summary>
		/// 登録されているタスクを順次実行します。
		/// </summary>
		public async Task Run()
		{
			Status = ScheduledActionQueueStatus.Running;
			while (Repeating)
			{
				if (Count == 0
					|| Status == ScheduledActionQueueStatus.Stop
					|| DateTime.Now < PeekTime)
				{
					await Task.Delay(1000);
					continue;
				}
				else
				{
					ScheduledAction task = Dequeue();
					task.Invoke();
				}
			}
		}
	}

	

	
}