using System;

namespace MatrixToolkit {

    public class ProbabilityDistribution2D : Matrix2D<float> {

        public ProbabilityDistribution2D(int len1, int len2) : base(len1, len2) { }
        public ProbabilityDistribution2D(float[,] list) : base(list) { }
        public ProbabilityDistribution2D(Matrix2D<float> other) : base(other) { }

        public override float this[int idx, int idy] {
            get { return base[idx, idy]; }
            set {
                if(value < 0) {
                    throw new ArgumentOutOfRangeException("val", value, "Cannot add a negative value to a probability distribution!");
                }
                base[idx, idy] = value;
            }
        }

        // Returns the sum of all vector elements
        public float Sum {
            get {
                float result = 0;
                for(int x = 0; x < GetLength(0); x++) {
                    for(int y = 0; y < GetLength(1); y++) {
                        result += this[x, y];
                    }
                }
                return result;
            }
        }

        // Renormalizes all elements so that they sum up to 1
        public void Renormalize() {
            float oldSum = Sum;
            if(oldSum > 0) {
                MultiplyAll(1f / oldSum);
            } else {
                throw new DivideByZeroException("Cannot renormalize because the elements sum is 0!");
            }
        }

        // Resets the probability distribution to a uniform one
        public void ResetToUniform() {
            float val = 1f / GetLength(0) / GetLength(1);
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    this[x, y] = val;
                }
            }
        }

        // Returns a slice of the matrix at the given index of the given dimension
        public new ProbabilityDistribution1D GetSlice(int dim, int id) {
            return new ProbabilityDistribution1D(base.GetSlice(dim, id));
        }

        /// <summary>
        /// Multiplies each element of an array by the given factor.
        /// </summary>
        /// <param name="factor">The factor to multiply by.</param>
        public void MultiplyAll(float factor) {
            if(factor < 0) {
                throw new ArgumentOutOfRangeException("factor", factor, "Cannot multiply elements by a negative factor!");
            }
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    this[x, y] *= factor;
                }
            }
        }

        /// <summary>
        /// Adds another matrix of same size to the current one, optionally weighting them.
        /// </summary>
        /// <param name="other">The matrix to be added to this one.</param>
        /// <param name="ownWeight">A factor to multiply own values before addion (optional, default: 1).</param>
        /// <param name="otherWeight">A factor to multiply the other's values before addion (optional, default: 1).</param>
        public void Add(ProbabilityDistribution2D other, float ownWeight = 1f, float otherWeight = 1f) {
            if(other.GetLength(0) != GetLength(0) || other.GetLength(1) != GetLength(1)) {
                throw new ArgumentException("Cannot add distributions of different sizes!");
            }
            if(ownWeight < 0) {
                throw new ArgumentOutOfRangeException("ownWeight", ownWeight, "Cannot weight elements by a negative factor!");
            }
            if(otherWeight < 0) {
                throw new ArgumentOutOfRangeException("otherWeight", otherWeight, "Cannot multiply elements by a negative factor!");
            }
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    this[x, y] = this[x, y] * ownWeight + other[x, y] * otherWeight;
                }
            }
        }

        /// <summary>
        /// Returns a tensor product of this matrix (on the X and Y axes) with another vector (on the Y axis).
        /// </summary>
        /// <param name="other">The vector to multiply with.</param>
        /// <returns>A 3D probability distribution representing the tensor product of the matrix and the vector.</returns>
        public ProbabilityDistribution3D GetTensorProduct(ProbabilityDistribution1D other) {
            ProbabilityDistribution3D result = new ProbabilityDistribution3D(GetLength(0), GetLength(1), other.Length);
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    for(int z = 0; z < other.Length; z++) {
                        result[x, y, z] = this[x, y] * other[z];
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Sums up all elements along the given dimension and returns a single projected vector.
        /// </summary>
        /// <param name="dim">Dimension to project onto (0 = x, 1 = y).</param>
        /// <returns>A 1D probability distribution.</returns>
        public ProbabilityDistribution1D Project(int dim) {
            if(dim != 0 && dim != 1) {
                throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0 or 1!");
            }
            ProbabilityDistribution1D result = new ProbabilityDistribution1D(GetLength(dim));
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    int index = (dim == 0) ? x : y;
                    result[index] += this[x, y];
                }
            }
            return result;
        }

        public override string ToString() {
            string result = "{ ";
            for(int x = 0; x < GetLength(0); x++) {
                result += (x > 0 ? ",\n  " : "") + "{";
                for(int y = 0; y < GetLength(1); y++) {
                    result += (y > 0 ? "," : "");
                    result += string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,8:0.00000}", this[x, y]);
                }
                result += " }";
            }
            return result + " }";
        }

    }

}
