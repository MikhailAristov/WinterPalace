using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class AIUtil {

    public const float NEGLIGIBLE_PROBABILITY = 2e-5f;

	public static T DeepCopy<T>(object objectToCopy) {
		using(MemoryStream memoryStream = new MemoryStream()) {
			BinaryFormatter binaryFormatter = new BinaryFormatter();
			binaryFormatter.Serialize(memoryStream, objectToCopy);
			memoryStream.Seek(0, SeekOrigin.Begin);
			return (T)binaryFormatter.Deserialize(memoryStream);
		}
	}

	public static int GetWeightedRandom(MoveData.DualUtility[] Utilities, float TopRank, float MinimumCutoff) {
		Debug.Assert(Utilities.Length > 0);
		Debug.Assert(MinimumCutoff >= 0);
		// Calculate the sum of all weights except those below cutoff
		float sum = 0;
		for(int i = 0; i < Utilities.Length; i++) {
			if(Utilities[i].Rank == TopRank && Utilities[i].Utility > MinimumCutoff) {
				sum += Utilities[i].Utility - MinimumCutoff;
			}
		}
		Debug.Assert(sum > 0);
		// Get a random float between 0 and the sum
		float rnd = UnityEngine.Random.Range(0f, sum);
		// Calculate the response curve
		int result = 0;
		for(; result < Utilities.Length; result++) {
			// Only consider weights above the cutoff point
			if(Utilities[result].Rank == TopRank && Utilities[result].Utility > MinimumCutoff) {
				// Reduce the random seed by the weight
				rnd -= (Utilities[result].Utility - MinimumCutoff);
				// If this takes the see below zero, stop
				if(rnd <= 0) {
					break;
				}
			}
		}
		return result;
	}

	public static float NormalizeProbabilitiesArray(ref float[] arr) {
		// Calculate the sum
		float sum = 0;
		for(int i0 = 0; i0 < arr.Length; i0++) {
			sum += Mathf.Max(0, arr[i0]);
		}
		if(sum <= 0) {
			Array.Clear(arr, 0, arr.Length);
		} else {
			// Divide all contents by the sum
			for(int i0 = 0; i0 < arr.Length; i0++) {
				arr[i0] = Mathf.Max(0, arr[i0]) / sum;
			}
		}
		return sum;
	}

	public static float GetMeanSquaredError(float[] arr1, float[] arr2, bool IgnoreZerothElement = false) {
		// Check length
		if(arr1.Length != arr2.Length) {
			throw new ArgumentException("Two arrays don't have the same length!");
		}
		// Calculate the sum of squared differences
		float result = 0;
		for(int i = (IgnoreZerothElement ? 1 : 0); i < arr1.Length; i++) {
			result += (arr1[i] - arr2[i]) * (arr1[i] - arr2[i]);
		}
		// Divide by the number of elements and return
		int ElementCount = Mathf.Max(1, (IgnoreZerothElement ? arr1.Length - 1 : arr1.Length));
		return (result / ElementCount);
	}

	public static float GetCrossEntropyError(float[] TargetDistribution, float[] MeasuredDistribution, bool IgnoreZerothElement = false) {
		// Check length
		if(TargetDistribution.Length != MeasuredDistribution.Length) {
			throw new ArgumentException("Two arrays don't have the same length!");
		}
		// Calculate the cross entropy error function
		float result = 0;
		for(int i = (IgnoreZerothElement ? 1 : 0); i < TargetDistribution.Length; i++) {
			result -= TargetDistribution[i] * Mathf.Log(Mathf.Max(NEGLIGIBLE_PROBABILITY, MeasuredDistribution[i])) + (1f - TargetDistribution[i]) * Mathf.Log(Mathf.Max(NEGLIGIBLE_PROBABILITY, 1f - MeasuredDistribution[i]));
			Debug.Assert(!float.IsNaN(result));
		}
		// Divide by the number of elements and return
		int ElementCount = Mathf.Max(1, (IgnoreZerothElement ? TargetDistribution.Length - 1 : TargetDistribution.Length));
		return (result / ElementCount);
	}

	public static float GetAbsoluteExpectedValueError(float[] arr1, float[] arr2) {
		// Check length
		if(arr1.Length != arr2.Length) {
			throw new ArgumentException("Two arrays don't have the same length!");
		}
		// Sum up the differences in probabilities, weighted by the value
		float result = 0;
		for(int i = 0; i < arr1.Length; i++) {
			result += (arr1[i] - arr2[i]) * i;
		}
		return Mathf.Abs(result);
	}

	public static void DisplayVector(string Preface, float[] Input) {
		string output = string.Format("{0}:", Preface);

		float sum = 0;
		for(int i = 0; i < Input.GetLength(0); i++) {
			output += string.Format("\t{0:F6}", Input[i]);
			sum += Input[i];
		}
		output += string.Format("\nSUM: {0:F6}", sum);

		Debug.Log(output);
	}

	public static void DisplayVector(string Preface, int[] Input) {
		string output = string.Format("{0}:", Preface);

		int sum = 0;
		for(int i = 0; i < Input.GetLength(0); i++) {
			output += string.Format("\t{0:D}", Input[i]);
			sum += Input[i];
		}
		output += string.Format("\nSUM: {0:D}", sum);

		Debug.Log(output);
	}

	public static void DisplayMatrix(string Preface, float[,] Input) {
		string output = string.Format("{0}:", Preface);

		for(int i = 0; i < Input.GetLength(0); i++) {
			output += "\n";
			for(int j = 0; j < Input.GetLength(1); j++) {
				output += string.Format("\t{0:F6}", Input[i, j]);
			}
		}

		Debug.Log(output);
	}

	public static void DisplayMatrix(string Preface, int[,] Input) {
		string output = string.Format("{0}:", Preface);

		for(int i = 0; i < Input.GetLength(0); i++) {
			output += "\n";
			for(int j = 0; j < Input.GetLength(1); j++) {
				output += string.Format("\t{0:D}", Input[i, j]);
			}
		}

		Debug.Log(output);
	}

	public static string FormatDictionary<TK,TV>(Dictionary<TK,TV> dict) {
		string result = "";
		foreach(TK key in dict.Keys) {
			result += (result.Length > 0 ? "; " : "") + (key.ToString() + " = " + dict[key].ToString());
		}
		return result;
	}

    // Checks if two floating-point values are equal within the given margin of error
    public static bool Approx(float a, float b, float margin = NEGLIGIBLE_PROBABILITY) {
        return Mathf.Abs(a - b) < margin;
    }

    // Checks if a floating point value is within a given range (inclusive)
    public static bool InRange(float val, float minVal, float maxVal) {
        return (val >= minVal && val <= maxVal);
    }

    // Checks if an integer value is within a given range (min: inclusive, max: exclusive)
    public static bool InRange(int val, int minVal, float maxVal) {
        return (val >= minVal && val < maxVal);
    }

    // Returns the sum of all elements in a 1D array
    public static int SumUpArray(int[] arr) {
        int result = 0;
        for(int i = 0; i < arr.Length; i++) {
            result += arr[i];
        }
        return result;
    }

    /// <summary>
    /// Shifts the array element with the given index to the last place of the array, keeping the others in the same order.
    /// </summary>
    /// <typeparam name="T">Array type.</typeparam>
    /// <param name="arr">Reference to the array.</param>
    /// <param name="id">Index of the element to shift back.</param>
    public static void ShiftToLast<T>(ref T[] arr, int id) {
        T tmp = arr[id];
        // Shift all elements after the target element back by one index
        for(int i = id + 1; i < arr.Length; i++) {
            arr[i - 1] = arr[i];
        }
        // Put the target element to the last index
        arr[arr.Length - 1] = tmp;
    }
}
