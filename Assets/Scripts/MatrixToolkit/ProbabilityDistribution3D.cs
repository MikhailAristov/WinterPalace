using System;

namespace MatrixToolkit {

    public class ProbabilityDistribution3D : Matrix3D<float> {

        public ProbabilityDistribution3D(int len1, int len2, int len3) : base(len1, len2, len3) { }
        public ProbabilityDistribution3D(float[,,] list) : base(list) { }
        public ProbabilityDistribution3D(Matrix3D<float> other) : base(other) { }

        public override float this[int idx, int idy, int idz] {
            get { return base[idx, idy, idz]; }
            set {
                if(value < 0) {
                    throw new ArgumentOutOfRangeException("val", value, "Cannot add a negative value to a probability distribution!");
                }
                base[idx, idy, idz] = value;
            }
        }

        // Returns the sum of all vector elements
        public float Sum {
            get {
                float result = 0;
                for(int x = 0; x < GetLength(0); x++) {
                    for(int y = 0; y < GetLength(1); y++) {
                        for(int z = 0; z < GetLength(2); z++) {
                            result += this[x, y, z];
                        }
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
            float val = 1f / GetLength(0) / GetLength(1) / GetLength(2);
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    for(int z = 0; z < GetLength(2); z++) {
                        this[x, y, z] = val;
                    }
                }
            }
        }

        // Returns a slice of the matrix at the given index of the given dimension
        public new ProbabilityDistribution2D GetSlice(int dim, int id) {
            return new ProbabilityDistribution2D(base.GetSlice(dim, id));
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
                    for(int z = 0; z < GetLength(2); z++) {
                        this[x, y, z] *= factor;
                    }
                }
            }
        }

        /// <summary>
        /// Adds another matrix of same size to the current one, optionally weighting them.
        /// </summary>
        /// <param name="other">The matrix to be added to this one.</param>
        /// <param name="ownWeight">A factor to multiply own values before addion (optional, default: 1).</param>
        /// <param name="otherWeight">A factor to multiply the other's values before addion (optional, default: 1).</param>
        public void Add(ProbabilityDistribution3D other, float ownWeight = 1f, float otherWeight = 1f) {
            if(other.GetLength(0) != GetLength(0) || other.GetLength(1) != GetLength(1) || other.GetLength(2) != GetLength(2)) {
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
                    for(int z = 0; z < GetLength(2); z++) {
                        this[x, y, z] = this[x, y, z] * ownWeight + other[x, y, z] * otherWeight;
                    }
                }
            }
        }

        /// <summary>
        /// Sums up all elements along the given dimension and returns a single projected vector.
        /// </summary>
        /// <param name="dim">Dimension to project onto (0 = x, 1 = y, 2 = z).</param>
        /// <returns>A 1D probability distribution.</returns>
        public ProbabilityDistribution1D Project(int dim) {
            if(dim != 0 && dim != 1 && dim != 2) {
                throw new ArgumentOutOfRangeException("dim", dim, "Dimension must be 0, 1, or 2!");
            }
            ProbabilityDistribution1D result = new ProbabilityDistribution1D(GetLength(dim));
            for(int x = 0; x < GetLength(0); x++) {
                for(int y = 0; y < GetLength(1); y++) {
                    for(int z = 0; z < GetLength(2); z++) {
                        int index = (dim == 0) ? x : (dim == 1) ? y : z;
                        result[index] += this[x, y, z];
                    }
                }
            }
            return result;
        }

        public override string ToString() {
            string result = "{ ";
            for(int x = 0; x < GetLength(0); x++) {
                result += (x > 0 ? ",\n  " : "") + "{";
                for(int y = 0; y < GetLength(1); y++) {
                    result += (y > 0 ? ",\n    " : " ") + "{";
                    for(int z = 0; z < GetLength(2); z++) {
                        result += (z > 0 ? "," : "");
                        result += string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,8:0.00000}", this[x, y, z]);
                    }
                    result += " }";
                }
                result += " }";
            }
            return result + " }";
        }
    }

}
