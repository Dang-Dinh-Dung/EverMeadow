using Evermeadow.Presentation.Menus;
using NUnit.Framework;

namespace Evermeadow.Tests.EditMode.Presentation
{
    /// <summary>Menu focus driven by Navigate steps: rows of different widths, title rows skipped, column remembered, no wrap.</summary>
    public class MenuGridTests
    {
        [Test]
        public void SetLayout_FocusesFirstSelectableCell()
        {
            var g = new MenuGrid();

            g.SetLayout(new[] { 0, 4, 4 });

            Assert.AreEqual((1, 0), (g.Row, g.Column));
        }

        [Test, Description("Title rows (width 0) are skipped; the column is kept across rows of the same width")]
        public void MoveDown_SkipsTitleRows()
        {
            var g = new MenuGrid();
            g.SetLayout(new[] { 4, 0, 4 });
            g.Move(1, 0);
            g.Move(1, 0);

            g.Move(0, 1);

            Assert.AreEqual((2, 2), (g.Row, g.Column));
        }

        [Test, Description("Passing through a narrower row lands back on the remembered column")]
        public void MoveDown_ThroughNarrowRow_RestoresColumn()
        {
            var g = new MenuGrid();
            g.SetLayout(new[] { 4, 2, 4 });
            g.Focus(0, 3);

            g.Move(0, 1);
            int narrow = g.Column;
            g.Move(0, 1);

            Assert.AreEqual(1, narrow, "clamped to the last cell of the 2-wide row");
            Assert.AreEqual((2, 3), (g.Row, g.Column));
        }

        [Test, Description("No wrap-around at the edges")]
        public void Move_AtEdges_StaysPut()
        {
            var g = new MenuGrid();
            g.SetLayout(new[] { 2, 2 });

            bool up = g.Move(0, -1);
            bool left = g.Move(-1, 0);
            g.Focus(1, 1);
            bool down = g.Move(0, 1);
            bool right = g.Move(1, 0);

            Assert.IsFalse(up || left || down || right);
            Assert.AreEqual((1, 1), (g.Row, g.Column));
        }

        [Test]
        public void Focus_OutsideGrid_Refused()
        {
            var g = new MenuGrid();
            g.SetLayout(new[] { 0, 3 });

            Assert.IsFalse(g.Focus(0, 0), "title row");
            Assert.IsFalse(g.Focus(1, 3), "past the row");
            Assert.IsTrue(g.Focus(1, 2));
        }

        [Test]
        public void EmptyGrid_NoFocusNoMove()
        {
            var g = new MenuGrid();

            g.SetLayout(new[] { 0, 0 });

            Assert.AreEqual(-1, g.Row);
            Assert.IsFalse(g.Move(0, 1));
        }
    }
}
