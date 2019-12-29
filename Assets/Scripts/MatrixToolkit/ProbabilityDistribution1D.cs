using System;

namespace MatrixToolkit {

    public class ProbabilityDistribution1D : Vector<float> {

        // Base class constructors
        public ProbabilityDistribution1D(int size) : base(size) { }
        public ProbabilityDistribution1D(float[] list) : base(list) { }
        public ProbabilityDistribution1D(Vector<float> other) : base(other) { }

        public override float this[int index] {
            get { return base[index]; }
            set {
                if(value < 0) {
                    throw new ArgumentOutOfRangeException("val", value, "Cannot add a negative value to a probability distribution!");
                }
                base[index] = value;
            }
        }

        // Returns the sum of all vector elements
        public float Sum {
            get {
                float result = 0;
                for(int i = 0; i < Length; i++) {
                    result += this[i];
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
            float val = 1f / Length;
            for(int i = 0; i < Length; i++) {
                this[i] = val;
            }
        }

        /// <summary>
        /// Multiplies each element of an array by the given factor.
        /// </summary>
        /// <param name="factor">The factor to multiply by.</param>
        public void MultiplyAll(float factor) {
            if(factor < 0) {
                throw new ArgumentOutOfRangeException("factor", factor, "Cannot multiply elements by a negative factor!");
            }
            for(int i = 0; i < Length; i++) {
                this[i] *= factor;
            }
        }

        /// <summary>
        /// Adds another vector of same size to the current one, optionally weighting them.
        /// </summary>
        /// <param name="other">The vector to be added to this one.</param>
        /// <param name="ownWeight">A factor to multiply own values before addion (optional, default: 1).</param>
        /// <param name="otherWeight">A factor to multiply the other's values before addion (optional, default: 1).</param>
        public void Add(ProbabilityDistribution1D other, float ownWeight = 1f, float otherWeight = 1f) {
            if(other.Length != Length) {
                throw new ArgumentException("Cannot add distributions of different sizes!");
            }
            if(ownWeight < 0) {
                throw new ArgumentOutOfRangeException("ownWeight", ownWeight, "Cannot weight elements by a negative factor!");
            }
            if(otherWeight < 0) {
                throw new ArgumentOutOfRangeException("otherWeight", otherWeight, "Cannot multiply elements by a negative factor!");
            }
            for(int i = 0; i < Length; i++) {
                this[i] = this[i] * ownWeight + other[i] * otherWeight;
            }
        }

        /// <summary>
        /// Returns a tensor product of this vector (on the X axis) with another vector (on the Y axis).
        /// </summary>
        /// <param name="other">The vector to multiply with.</param>
        /// <returns>A two-dimensional probability distribution representing the tensor product of the two vectors.</returns>
        public ProbabilityDistribution2D GetTensorProduct(ProbabilityDistribution1D other) {
            ProbabilityDistribution2D result = new ProbabilityDistribution2D(Length, other.Length);
            for(int x = 0; x < Length; x++) {
                for(int y = 0; y < other.Length; y++) {
                    result[x, y] = this[x] * other[y];
                }
            }
            return result;
        }

        // Output vector as a string
        public override string ToString() {
            string result = "{";
            for(int i = 0; i < Length; i++) {
                result += (i > 0 ? "," : "")
                        + string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0,8:0.00000}", this[i]);
            }
            return result + " }";
        }

    }

}
