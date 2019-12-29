using System;

namespace MatrixToolkit {

    // Source: https://stackoverflow.com/questions/3329576/generic-constraint-to-match-numeric-types
    public class Matrix2D<T> where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable {

        protected T[,] Contents;

        public int GetLength(int dim) {
            return Contents.GetLength(dim);
        }

        // Indexer for easier access
        public virtual T this[int idx, int idy] {
            get { return Contents[idx, idy]; }
            set { Contents[idx, idy] = value; }
        }

        // Creates a blank vector of length len
        public Matrix2D(int len1, int len2) {
            Contents = new T[len1, len2];
        }

        // Creates a vector with values from given array
        public Matrix2D(T[,] list) {
            Contents = new T[list.GetLength(0), list.GetLength(1)];
            CopyFrom(list);
        }

        // Creates a copy of another vector of the same type
        public Matrix2D(Matrix2D<T> other) {
            Contents = new T[other.GetLength(0), other.GetLength(1)];
            CopyFrom(other);
        }

        /// <summary>
        /// Copies values from an array of equal size.
        /// </summary>
        /// <param name="list">An array to copy from.</param>
        public void CopyFrom(T[,] list) {
            if(list.GetLength(0) != GetLength(0) || list.GetLength(1) != GetLength(1)) {
                throw new ArgumentException("The array does not have the same size!");
            }
            for(int x = 0; x < list.GetLength(0); x++) {
                for(int y = 0; y < list.GetLength(1); y++) {
                    this[x, y] = list[x, y];
                }
            }
        }

        /// <summary>
        /// Copies values from a matrix of equal size.
        /// </summary>
        /// <param name="other">A matrix to copy from.</param>
        public void CopyFrom(Matrix2D<T> other) {
            if(other.GetLength(0) != GetLength(0) || other.GetLength(1) != GetLength(1)) {
                throw new ArgumentException("The other matrix does not have the same size!");
            }
            for(int x = 0; x < other.GetLength(0); x++) {
                for(int y = 0; y < other.GetLength(1); y++) {
                    this[x, y] = other[x, y];
                }
            }
        }

        // Resets every vector element to default value
        public void Clear() {
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    this[x, y] = new T();
                }
            }
        }

        // Swaps two elements of an array
        public void Swap(int idx1, int idy1, int idx2, int idy2) {
            T tmp = this[idx1, idy1];
            this[idx1, idy1] = this[idx2, idy2];
            this[idx2, idy2] = tmp;
        }

        // Transposes a square matrixs
        public void Transpose() {
            if(GetLength(0) != GetLength(1)) {
                throw new FormatException("Cannot transpose a non-square matrix!");
            }
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = x + 1; y < GetLength(1); y++) {
                    Swap(x, y, y, x);
                }
            }
        }

        // Swaps two slices (rows or columns) of given indices along the specified dimension
        public void SwapSlices(int dim, int id1, int id2) {
            switch(dim) {
                case 0: // X-axis
                    for(int y = 0; y < GetLength(1); y++) {
                        Swap(id1, y, id2, y);
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        Swap(x, id1, x, id2);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 for x or 1 for y!");
            }
        }

        /// <summary>
        /// Slices the 2D matrix along the given dimension at the given index.
        /// </summary>
        /// <param name="dim">Dimension to slice at (0 = x, 1 = y).</param>
        /// <param name="id">Index to slice at.</param>
        /// <returns>A 1D vector slice of the matrix.</returns>
        public Vector<T> GetSlice(int dim, int id) {
            Vector<T> result;
            switch(dim) {
                case 0: // X-axis
                    result = new Vector<T>(GetLength(1));
                    for(int y = 0; y < GetLength(1); y++) {
                        result[y] = this[id, y];
                    }
                    break;
                case 1: // Y-axis
                    result = new Vector<T>(GetLength(0));
                    for(int x = 0; x < GetLength(0); x++) {
                        result[x] = this[x, id];
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 for x or 1 for y!");
            }
            return result;
        }

        /// <summary>
        /// Sets all elements of a slice of the matrix to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearSlice(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int y = 0; y < GetLength(1); y++) {
                        this[id, y] = new T();
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        this[x, id] = new T();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 for x or 1 for y!");
            }
        }

        /// <summary>
        /// Sets all elements below a given index along the given dimension to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearSliceBelowIndex(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < id; x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            this[x, y] = new T();
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < id; y++) {
                            this[x, y] = new T();
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 for x or 1 for y!");
            }
        }

        /// <summary>
        /// Sets all elements except the slice at a given index (exclusive) along the given dimension to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearAllButSlice(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            if(x != id) {
                                this[x, y] = new T();
                            }
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            if(y != id) {
                                this[x, y] = new T();
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 for x or 1 for y!");
            }
        }

        /// <summary>
        /// Sets all elements except the main diagonal to default values.
        /// </summary>
        public void ClearAllButDiagonal() {
            if(GetLength(0) != GetLength(1)) {
                throw new FormatException("Cannot find the diagonal of a non-square matrix!");
            }
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    if(x != y) {
                        this[x, y] = new T();
                    }
                }
            }
        }
    }
}
