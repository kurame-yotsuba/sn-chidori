namespace SwallowNest.Shikibu
{
	/// <summary>
	/// 繰り返しアクションを加える際のタイミングを表します。
	/// </summary>
	public enum RepeatAdditionType
	{
		/// <summary>
		/// アクション実行前に追加されます。
		/// デフォルトはこれです。
		/// </summary>
		BeforeExecute,

		/// <summary>
		/// アクション実行後に追加されます。
		/// </summary>
		AfterExecute,
	}
}
