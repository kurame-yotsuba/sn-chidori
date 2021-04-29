namespace SwallowNest.Shikibu
{
	/// <summary>
	/// TaskQueueの状態を表す列挙型です。
	/// </summary>
	public enum TimeActionSchedulerStatus
	{
		/// <summary>
		/// 停止していることを表します。
		/// </summary>
		Stop,

		/// <summary>
		/// 実行中であることを表します。
		/// </summary>
		Running,

		/// <summary>
		/// 現在登録されているタスクを完了し次第終了することを表します。
		/// </summary>
		EndWaitAll,

		/// <summary>
		/// 現在実行中のタスクとそれに連なるタスクを完了し次第終了します。
		/// </summary>
		EndImmediately,
	}
}
