using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SwallowNest.Shikibu.Tests.Helpers.Category;

namespace SwallowNest.Shikibu.Tests
{
    [TestClass]
    public class PriorityQueueTest
    {
        PriorityQueue<string, int> queue;
        (string element, int priority)[] enqueueList;
        (string element, int priority)[] dequeueList;

        [TestInitialize]
        public void Initialize()
        {
            queue = new();
            // キューに追加する要素と優先度
            enqueueList = new[]
            {
                ("A", 4),
                ("B", 3),
                ("C", 1),
                ("D", 3),
            };

            // キューから出ていく要素と優先度
            dequeueList = new[]
            {
                ("C", 1),
                ("B", 3),
                ("D", 3),
                ("A", 4),
            };
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 要素を追加してから優先度順かつ追加した順にDequeueできる()
        {
            foreach (var (element, priority) in enqueueList)
            {
                queue.Enqueue(element, priority);
            }

            foreach (var (element, _) in dequeueList)
            {
                queue.Dequeue().Is(element);
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 要素数を取得できる()
        {
            int count = 0;
            queue.Count.Is(count);
            foreach (var (element, priority) in enqueueList)
            {
                // 要素を追加すると要素数は１増える
                queue.Enqueue(element, priority);
                queue.Count.Is(++count);
            }

            foreach (var (element, priority) in dequeueList)
            {
                // 要素を取り出すと要素数は１減る
                queue.Dequeue();
                queue.Count.Is(--count);
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 次の要素を取得できる()
        {
            foreach (var (element, priority) in enqueueList)
            {
                queue.Enqueue(element, priority);
            }

            foreach (var (element, priority) in dequeueList)
            {
                // 次に取り出される要素（取り出しはしない）
                queue.Peek().Is(element);

                // 取り出す
                queue.Dequeue();
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 次の優先度を取得できる()
        {
            foreach (var (element, priority) in enqueueList)
            {
                queue.Enqueue(element, priority);
            }

            foreach (var (element, priority) in dequeueList)
            {
                // 次に取り出される要素の優先度（取り出しはしない）
                queue.PriorityPeek().Is(priority);

                // 取り出す
                queue.Dequeue();
            }
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void TryDequeueができる()
        {
            queue.Enqueue("A", 4);

            // キューが空でない場合
            queue.TryDequeue(out string result).Is(true);
            result.Is("A");

            // キューが空の場合
            queue.TryDequeue(out string fail).Is(false);
            fail.IsNull();
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 次の要素の取得のTryができる()
        {
            queue.Enqueue("A", 4);

            // キューが空でない場合
            queue.TryPeek(out string result).Is(true);
            result.Is("A");

            queue.Dequeue();

            // キューが空の場合
            queue.TryPeek(out result).Is(false);
            result.IsNull();
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 次の優先度の取得のTryができる()
        {
            queue.Enqueue("A", 4);

            // キューが空でない場合
            queue.TryPriorityPeek(out int result).Is(true);
            result.Is(4);

            queue.Dequeue();

            // キューが空の場合
            queue.TryPriorityPeek(out result).Is(false);
            result.Is(0);
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void キューをクリア()
        {
            queue.Enqueue("A", 4);

            // キューの中身をクリア
            queue.Clear();

            // 要素数は0
            queue.Count.Is(0);

            // 列挙も終わっている
            queue.GetEnumerator().MoveNext().Is(false);
        }

        [TestMethod]
        [TestCategory(Normal)]
        public void 優先度順に列挙できる()
        {
            foreach (var (element, priority) in enqueueList)
            {
                queue.Enqueue(element, priority);                
            }

            foreach (var (actual, expected) in queue.Zip(dequeueList))
            {
                actual.element.Is(expected.element);
                actual.priority.Is(expected.priority);
            }
        }

        [TestMethod]
        [TestCategory(Error)]
        public void 要素が空の時にDequeue()
        {
            // インスタンス作成直後
            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.Dequeue();
            });

            // キューに追加してから空にしたとき
            queue.Enqueue("A", 4);
            queue.Clear();

            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.Dequeue();
            });
        }

        [TestMethod]
        [TestCategory(Error)]
        public void 要素が空の時にPeek()
        {
            // インスタンス作成直後
            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.Peek();
            });

            // キューに追加してから空にしたとき
            queue.Enqueue("A", 4);
            queue.Clear();

            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.Peek();
            });
        }

        [TestMethod]
        [TestCategory(Error)]
        public void 要素が空の時にPriorityPeek()
        {
            // インスタンス作成直後
            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.PriorityPeek();
            });

            // キューに追加してから空にしたとき
            queue.Enqueue("A", 4);
            queue.Clear();

            AssertEx.Throws<InvalidOperationException>(() =>
            {
                queue.PriorityPeek();
            });
        }
    }
}
