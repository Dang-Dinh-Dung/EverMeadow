using System;

namespace Evermeadow.Presentation.Menus
{
    /// <summary>
    /// Focus on a grid of menu cells driven by Navigate steps (GDD Input Rule 16): rows of different widths, empty rows skipped,
    /// the column remembered while passing through narrower rows (moving down from the keyboard column of a binding row through a
    /// one-button row lands back on the keyboard column). No wrap-around. Engine-free.
    /// </summary>
    public sealed class MenuGrid
    {
        private int[] _widths = Array.Empty<int>();
        private int _wantedColumn;

        /// <summary>Focused row, or −1 when the grid is empty.</summary>
        public int Row { get; private set; } = -1;

        /// <summary>Focused column in <see cref="Row"/>.</summary>
        public int Column { get; private set; }

        /// <summary>Number of rows.</summary>
        public int RowCount => _widths.Length;

        /// <summary>Replaces the layout (<paramref name="widths"/>[r] = cells in row r; 0 = not selectable) and focuses the first cell.</summary>
        public void SetLayout(int[] widths)
        {
            _widths = widths ?? Array.Empty<int>();
            Row = -1;
            Column = 0;
            _wantedColumn = 0;
            for (int r = 0; r < _widths.Length; r++)
            {
                if (_widths[r] <= 0) continue;
                Row = r;
                break;
            }
        }

        /// <summary>Focuses (<paramref name="row"/>, <paramref name="column"/>) when it exists (pointer hover or click).</summary>
        public bool Focus(int row, int column)
        {
            if (row < 0 || row >= _widths.Length || column < 0 || column >= _widths[row]) return false;
            Row = row;
            Column = column;
            _wantedColumn = column;
            return true;
        }

        /// <summary>
        /// Moves focus by one step: <paramref name="dx"/> across the row, <paramref name="dy"/> between rows (+1 = down).
        /// Returns true when the focus changed.
        /// </summary>
        public bool Move(int dx, int dy)
        {
            if (Row < 0) return false;
            int row = Row, column = Column;

            if (dx != 0)
            {
                column = Clamp(Column + Math.Sign(dx), 0, _widths[Row] - 1);
                _wantedColumn = column;
            }
            if (dy != 0)
            {
                int r = Row + Math.Sign(dy);
                while (r >= 0 && r < _widths.Length && _widths[r] <= 0) r += Math.Sign(dy);
                if (r >= 0 && r < _widths.Length)
                {
                    row = r;
                    column = Clamp(_wantedColumn, 0, _widths[r] - 1);
                }
            }

            bool changed = row != Row || column != Column;
            Row = row;
            Column = column;
            return changed;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
    }
}
