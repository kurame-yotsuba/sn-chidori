using System;
using System.Collections.Generic;
using System.Text;

namespace SwallowNest.Chidori
{
	public class ScheduledAction
	{
		Action action;
		public int Id { get; }
		public string Name { get; }

		internal ScheduledAction(Action action, int id, string name)
		{
			this.action = action;
			Id = id;
			Name = name;
		}

		internal void Invoke()
		{
			//if (name != "")
			//{
			//	SharedLogger.Print($"{name}を実行します。", LogLevel.DEBUG);
			//}

			try
			{
				action();
			}
			catch (Exception e)
			{
				//SharedLogger.Print($"{name}で{e.GetType()}が発生しました。{e.Message}", LogLevel.ERROR);
				throw e;
			}

			//if (name != "")
			//{
			//	SharedLogger.Print($"{name}の実行が終了しました。", LogLevel.INFO);
			//}
		}
	}
}
