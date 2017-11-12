using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public static class AIUtil {

	public const float NEGLIGIBLE_PROBABILITY = 1e-5f;

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
		for(int i1 = 0; i1 < arr.Length; i1++) {
			sum += Mathf.Max(0, arr[i1]);
		}
		if(sum <= 0) {
			Array.Clear(arr, 0, arr.Length);
		} else {
			// Divide all contents by the sum
			for(int i2 = 0; i2 < arr.Length; i2++) {
				arr[i2] = Mathf.Max(0, arr[i2]) / sum;
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

	public static void DisplayVector(string preface, float[] input) {
		int length = input.GetLength(0);

		string output = "" + preface + ":\t";
		for(int i = 0; i < length; i++) {
			output += string.Format(" {0:F6}", input[i]);
		}
		output += ("\nSUM: " + input.Sum().ToString());

		Debug.Log(output);
	}

	public static void DisplayMatrix(string preface, float[,] input) {
		int height = input.GetLength(0);
		int width = input.GetLength(1);

		string output = "" + preface;
		for(int i = 0; i < height; i++) {
			output += "\n";
			for(int j = 0; j < width; j++) {
				output += string.Format(" {0:F6}", input[i, j]);
			}
		}

		Debug.Log(output);
	}

	public static void DisplayMatrix(string preface, int[,] input) {
		int height = input.GetLength(0);
		int width = input.GetLength(1);

		string output = "" + preface;
		for(int i = 0; i < height; i++) {
			output += "\n";
			for(int j = 0; j < width; j++) {
				output += string.Format(" {0:D}", input[i, j]);
			}
		}

		Debug.Log(output);
	}
}
