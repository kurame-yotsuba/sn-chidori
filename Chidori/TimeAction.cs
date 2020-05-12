using System;
using System.Collections.Generic;
using System.Text;

namespace SwallowNest.Chidori
{
	public class TimeAction
	{
		Action action;
		public DateTime ExecTime { get; internal set; }
		public TimeSpan Interval { get; internal set; }
		public string? Name { get; }

		#region constructor

		private TimeAction(Action action, string? name = null)
		{
			this.action = action;
			Name = name;
		}

		internal TimeAction(Action action, DateTime execTime, string? name = null): this(action, name)
		{
			ExecTime = execTime;
		}

		internal TimeAction(Action action, TimeSpan interval, string? name = null): this(action, name)
		{
			ExecTime = DateTime.Now + interval;
			Interval = interval;
		}

		internal TimeAction(Action action, DateTime execTime, TimeSpan interval, string? name = null): this(action, name)
		{
			ExecTime = execTime;
			Interval = interval;
		}

		#endregion

		internal void Invoke()
		{
			try
			{
				action();
			}
			catch (Exception e)
			{
				throw e;
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
