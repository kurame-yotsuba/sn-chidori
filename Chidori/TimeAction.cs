using System;
using System.Collections.Generic;
using System.Text;

namespace SwallowNest.Chidori
{
	public class TimeAction
	{
		Action action;
		public string Name { get; }

		public TimeAction(Action action, string name)
		{
			this.action = action;
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
