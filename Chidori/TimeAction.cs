using System;
using System.Collections.Generic;
using System.Text;

namespace SwallowNest.Chidori
{
	public class TimeAction
	{
		Action action;

		/// <summary>
		/// アクションが実行される時刻
		/// </summary>
		public DateTime ExecTime { get; internal set; }

		/// <summary>
		/// アクションの繰り返し間隔
		/// </summary>
		public TimeSpan Interval { get; internal set; }

		/// <summary>
		/// スケジューラから取得する際のアクション名
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// アクションの実行条件。
		/// nullの場合は実行されます。
		/// </summary>
		public Func<bool>? CanExecute { private get; set; }

		#region constructor

		private TimeAction(Action action, string? name = null, Func<bool>? canExecute = null)
		{
			this.action = action;
			Name = name;
			CanExecute = canExecute;
		}

		internal TimeAction(
			Action action,
			DateTime execTime,
			string? name = null,
			Func<bool>? canExecute = null
			) : this(action, name, canExecute)
		{
			ExecTime = execTime;
		}

		internal TimeAction(
			Action action,
			TimeSpan interval,
			string? name = null,
			Func<bool>? canExecute = null
			) : this(action, name, canExecute)
		{
			ExecTime = DateTime.Now + interval;
			Interval = interval;
		}

		internal TimeAction(
			Action action,
			DateTime execTime,
			TimeSpan interval,
			string? name = null,
			Func<bool>? canExecute = null
			) : this(action, name, canExecute)
		{
			ExecTime = execTime;
			Interval = interval;
		}

		#endregion

		internal void Invoke()
		{
			if (CanExecute?.Invoke() ?? true)
			{
				action();
			}
		}

		/// <summary>
		/// アクションを更新します。
		/// </summary>
		/// <param name="action"></param>
		public void Update(Action action)
		{
			this.action = action;
		}
	}
}
