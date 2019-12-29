using System;

namespace MatrixToolkit {

    // Source: https://stackoverflow.com/questions/3329576/generic-constraint-to-match-numeric-types
    public class Matrix3D<T> where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable {

        protected T[,,] Contents;

        public int GetLength(int dim) {
            return Contents.GetLength(dim);
        }

        // Indexer for easier access
        public virtual T this[int idx, int idy, int idz] {
            get { return Contents[idx, idy, idz]; }
            set { Contents[idx, idy, idz] = value; }
        }

        // Creates a blank vector of length len
        public Matrix3D(int len1, int len2, int len3) {
            Contents = new T[len1, len2, len3];
        }

        // Creates a vector with values from given array
        public Matrix3D(T[,,] list) {
            Contents = new T[list.GetLength(0), list.GetLength(1), list.GetLength(2)];
            CopyFrom(list);
        }

        // Creates a copy of another vector of the same type
        public Matrix3D(Matrix3D<T> other) {
            Contents = new T[other.GetLength(0), other.GetLength(1), other.GetLength(2)];
            CopyFrom(other);
        }

        /// <summary>
        /// Copies values from an array of equal size.
        /// </summary>
        /// <param name="list">An array to copy from.</param>
        public void CopyFrom(T[,,] list) {
            if(list.GetLength(0) != GetLength(0) || list.GetLength(1) != GetLength(1) || list.GetLength(2) != GetLength(2)) {
                throw new ArgumentException("The array does not have the same size!");
            }
            for(int x = 0; x < list.GetLength(0); x++) {
                for(int y = 0; y < list.GetLength(1); y++) {
                    for(int z = 0; z < list.GetLength(2); z++) {
                        this[x, y, z] = list[x, y, z];
                    }
                }
            }
        }

        /// <summary>
        /// Copies values from a matrix of equal size.
        /// </summary>
        /// <param name="other">A matrix to copy from.</param>
        public void CopyFrom(Matrix3D<T> other) {
            if(other.GetLength(0) != GetLength(0) || other.GetLength(1) != GetLength(1) || other.GetLength(2) != GetLength(2)) {
                throw new ArgumentException("The other matrix does not have the same size!");
            }
            for(int x = 0; x < other.GetLength(0); x++) {
                for(int y = 0; y < other.GetLength(1); y++) {
                    for(int z = 0; z < other.GetLength(2); z++) {
                        this[x, y, z] = other[x, y, z];
                    }
                }
            }
        }

        // Resets every vector element to default value
        public void Clear() {
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    for(int z = 0; z < GetLength(2); z++) {
                        this[x, y, z] = new T();
                    }
                }
            }
        }

        // Swaps two elements of an array
        public void Swap(int idx1, int idy1, int idz1, int idx2, int idy2, int idz2) {
            T tmp = this[idx1, idy1, idz1];
            this[idx1, idy1, idz1] = this[idx2, idy2, idz2];
            this[idx2, idy2, idz2] = tmp;
        }

        /// <summary>
        /// Transposes a cubic matrix around the given dimension.
        /// </summary>
        /// <param name="dim">Dimension to keep in place (0 = x, 1 = y, 2 = z).</param>
        public void Transpose(int dim) {
            if(GetLength(0) != GetLength(1) || GetLength(1) != GetLength(2)) {
                throw new FormatException("Cannot transpose a non-cube matrix!");
            }
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = y + 1; z < GetLength(2); z++) {
                                Swap(x, y, z, x, z, y);
                            }
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = x + 1; z < GetLength(2); z++) {
                                Swap(x, y, z, z, y, x);
                            }
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = x + 1; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(2); z++) {
                                Swap(x, y, z, y, x, z);
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

        /// <summary>
        /// Swaps two 2D slices of given indices along the specified dimension.
        /// </summary>
        /// <param name="dim">Dimension to slice at (0 = x, 1 = y, 2 = z).</param>
        /// <param name="id1">Index of the first slice to swap.</param>
        /// <param name="id2">Index of the second slice to swap.</param>
        public void SwapSlices(int dim, int id1, int id2) {
            switch(dim) {
                case 0: // X-axis
                    for(int y = 0; y < GetLength(1); y++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            Swap(id1, y, z, id2, y, z);
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            Swap(x, id1, z, x, id2, z);
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            Swap(x, y, id1, x, y, id2);
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

        /// <summary>
        /// Slices the 3D matrix along the given dimension at the given index.
        /// </summary>
        /// <param name="dim">Dimension to slice at (0 = x, 1 = y, 2 = z).</param>
        /// <param name="id">Index to slice at.</param>
        /// <returns>A 2D matrix slice of the larger 3D matrix.</returns>
        public Matrix2D<T> GetSlice(int dim, int id) {
            Matrix2D<T> result;
            switch(dim) {
                case 0: // X-axis
                    result = new Matrix2D<T>(GetLength(1), GetLength(2));
                    for(int y = 0; y < GetLength(1); y++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            result[y, z] = this[id, y, z];
                        }
                    }
                    break;
                case 1: // Y-axis
                    result = new Matrix2D<T>(GetLength(0), GetLength(2));
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            result[x, z] = this[x, id, z];
                        }
                    }
                    break;
                case 2: // Z-axis
                    result = new Matrix2D<T>(GetLength(0), GetLength(1));
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            result[x, y] = this[x, y, id];
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
            return result;
        }

        /// <summary>
        /// Sets all elements of a slice of the matrix to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y, 2 = z).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearSlice(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int y = 0; y < GetLength(1); y++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            this[id, y, z] = new T();
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            this[x, id, z] = new T();
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            this[x, y, id] = new T();
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

        /// <summary>
        /// Sets all elements below a given index (exclusive) along the given dimension to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y, 2 = z).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearSliceBelowIndex(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < id; x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(2); z++) {
                                this[x, y, z] = new T();
                            }
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < id; y++) {
                            for(int z = 0; z < GetLength(2); z++) {
                                this[x, y, z] = new T();
                            }
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < id; z++) {
                                this[x, y, z] = new T();
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

        /// <summary>
        /// Sets all elements except the slice at a given index along the given dimension to default values.
        /// </summary>
        /// <param name="dim">Dimension of the slice (0 = x, 1 = y, 2 = z).</param>
        /// <param name="id">Index of the slice.</param>
        public void ClearAllButSlice(int dim, int id) {
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(x != id) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(y != id) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(z != id) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

        /// <summary>
        /// Sets all elements except the main diagonal on the given dimension to default values.
        /// </summary>
        /// <param name="dim">Dimension of the diagonal (0 = x, 1 = y, 2 = z).</param>
        public void ClearAllButDiagonal(int dim) {
            if(GetLength(0) != GetLength(1) || GetLength(1) != GetLength(2)) {
                throw new FormatException("Cannot find the diagonal of a non-cube matrix!");
            }
            switch(dim) {
                case 0: // X-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(y != z) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                case 1: // Y-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(x != z) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                case 2: // Z-axis
                    for(int x = 0; x < GetLength(0); x++) {
                        for(int y = 0; y < GetLength(1); y++) {
                            for(int z = 0; z < GetLength(1); z++) {
                                if(x != y) {
                                    this[x, y, z] = new T();
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
        }

    }
}
