using Sphynx.Client.UI;

namespace Sphynx.Test
{
    [TestFixture]
    public class StringGapBufferTests
    {
        [Test]
        public void GapBuffer_ResizeTest()
        {
            const int initialCapacity = StringGapBuffer.DEFAULT_GAP_SIZE;

            var g = new StringGapBuffer(initialCapacity);

            Assert.That(g.GapBegin, Is.EqualTo(0));
            Assert.That(g.GapEnd, Is.EqualTo(initialCapacity));
            Assert.That(g.GapSize, Is.EqualTo(initialCapacity));

            DoResizeAssert(g,10);
            DoResizeAssert(g,-11);
            DoResizeAssert(g,32);

            const string testText = "Hello World";
            g += testText;
            g.MoveAbs(testText.IndexOf(' '));

            DoResizeAssert(g, 10);
            DoResizeAssert(g, 10);
            DoResizeAssert(g, -5);
        }

        private void DoResizeAssert(StringGapBuffer g, int incrementSize)
        {
            int oldSize = g.GapSize;
            int oldGapBegin = g.GapBegin;
            int oldGapEnd = g.GapEnd;
            string oldText = g.Text;
            
            g.ResizeGap(g.GapSize + incrementSize);

            Assert.That(g.GapSize, Is.EqualTo(oldSize + incrementSize));
            Assert.That(g.GapBegin, Is.EqualTo(oldGapBegin));
            Assert.That(g.GapEnd, Is.EqualTo(oldGapEnd + incrementSize));
            Assert.That(g.Text, Is.EqualTo(oldText));
        }

        [Test]
        public void GapBuffer_MoveTest()
        {
            const string initialText = "Test Text";
            var g = new StringGapBuffer(initialText);
            Assert.That(g.GapBegin, Is.EqualTo(initialText.Length));
            Assert.That(g.GapEnd, Is.EqualTo(g.GapBegin + g.GapSize));

            DoMoveAssert(g, -5, initialText);
            DoMoveAssert(g, 3, initialText);
        }

        private void DoMoveAssert(StringGapBuffer g, int moveDelta, string initialText)
        {
            moveDelta = Math.Clamp(moveDelta, -g.GapBegin, g.Count - moveDelta);

            int oldGapBegin = g.GapBegin;
            int oldGapEnd = g.GapEnd;

            g.Move(moveDelta);

            Assert.That(g.GapBegin, Is.EqualTo(oldGapBegin + moveDelta));
            Assert.That(g.GapEnd, Is.EqualTo(g.GapBegin + g.GapSize));
            Assert.That(g.GapEnd, Is.EqualTo(oldGapEnd + moveDelta));
            Assert.That(g.Buffer[oldGapBegin + moveDelta], Is.EqualTo(initialText[oldGapBegin + moveDelta]));
        }

        [Test]
        public void GapBuffer_ShouldInsertText()
        {
            const string initialText = "Test Text";
            const string textAdd = " N ";
            const string finalText = $"Test{textAdd} Text";

            var g = new StringGapBuffer(initialText);

            Assert.That((string)g, Is.EqualTo(initialText));

            g.MoveAbs(initialText.IndexOf(' '));
            g += textAdd;

            Assert.That((string)g, Is.EqualTo(finalText));
        }
    }
}
