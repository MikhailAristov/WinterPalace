using System;

namespace MatrixToolkit {

    // Source: https://stackoverflow.com/questions/3329576/generic-constraint-to-match-numeric-types
    public class Vector<T> where T : struct, IComparable, IComparable<T>, IConvertible, IEquatable<T>, IFormattable {

        protected T[] Contents;
        public int Length => Contents.Length;

        // Indexer for easier access
        // Docu: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/indexers/using-indexers
        public virtual T this[int index] {
            get { return Contents[index]; }
            set { Contents[index] = value; }
        }

        // Creates a blank vector of length len
        public Vector(int len) {
            Contents = new T[len];
        }

        // Creates a vector with values from given array
        public Vector(T[] list) {
            Contents = new T[list.Length];
            CopyFrom(list);
        }

        // Creates a copy of another vector of the same type
        public Vector(Vector<T> other) {
            Contents = new T[other.Length];
            CopyFrom(other);
        }

        /// <summary>
        /// Copies values from an array of equal size.
        /// </summary>
        /// <param name="list">An array to copy from.</param>
        public void CopyFrom(T[] list) {
            if(list.Length != Length) {
                throw new ArgumentException("The array does not have the same size!");
            }
            for(int x = 0; x < list.Length; x++) {
                this[x] = list[x];
            }
        }

        /// <summary>
        /// Copies values from a vector of equal size.
        /// </summary>
        /// <param name="other">A vector to copy from.</param>
        public void CopyFrom(Vector<T> other) {
            if(other.Length != Length) {
                throw new ArgumentException("The other matrix does not have the same size!");
            }
            for(int x = 0; x < other.Length; x++) {
                this[x] = other[x];
            }
        }

        // Swaps two elements of an array
        public void Swap(int idx1, int idx2) {
            T tmp = this[idx1];
            this[idx1] = this[idx2];
            this[idx2] = tmp;
        }

        // Resets every vector element to default value
        public void Clear() {
            for(int i = 0; i < Length; i++) {
                this[i] = new T();
            }
        }

    }
}
