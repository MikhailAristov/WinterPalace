﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIEvaluator : MonoBehaviour {

	public int RandomSeed;
	public int[] ExtraGameSeeds;
	public int CurrentGameSeed;

	// The order in which player controllers and their perceptors are listed below
	// must be the same in both arrays, but does not have to match GameController.Players
	public AIPlayerController[] Players;
	public AIGenericPerceptor[] Perceptors;

	protected string[] PerceptorClassNames;

	protected bool UpdatingStatistics;
	public bool READY {
		get { return !UpdatingStatistics; }
	}

	void Awake() {
		// Initialize random seeds for fair evaluation
		if(RandomSeed > 0 && isActiveAndEnabled) {
			UnityEngine.Random.InitState(RandomSeed);
			if(ExtraGameSeeds.Length > 0) {
				for(int i = 0; i < ExtraGameSeeds.Length; i++) {
					ExtraGameSeeds[i] = UnityEngine.Random.Range(1000000, 10000000);
				}
			}
		}
	}

	public void NextGameSeed() {
		if(ExtraGameSeeds.Length > CurrentGameSeed) {
			UnityEngine.Random.InitState(ExtraGameSeeds[CurrentGameSeed]);
			CurrentGameSeed++;
		} else {
			throw new IndexOutOfRangeException("The evaluator has run out of pregenerated game seeds!");
		}
	}

	public class PerceptorStatistics {
		public string ClassName;
		public float DeckMSE;
		public float AdversaryHandMSE;
		public float DeckXEE;
		public float AdversaryHandXEE;
        public Vector3 AdversaryHandMLC;
		public int DeckDataPointCount;
		public int DeckCardsCount;
		public int HandDataPointCount;

		public PerceptorStatistics(string name) {
			ClassName = name;
		}

		public void AppendStatistics(float NextDeckMSE, float NextDeckXEE, int DeckCardsLeft, float NextHandMSE, float NextHandXEE, Vector3 NextHandMLC, int HandsAnalyzed) {
			// Update deck stats
			DeckMSE = (DeckMSE * DeckCardsCount + NextDeckMSE * DeckCardsLeft) / (DeckCardsCount + DeckCardsLeft);
			DeckXEE = (DeckXEE * DeckDataPointCount + NextDeckXEE) / (DeckDataPointCount + 1);
			DeckDataPointCount += 1; 
			DeckCardsCount += DeckCardsLeft;
			// Update hand stats
			AdversaryHandMSE = (AdversaryHandMSE * HandDataPointCount + NextHandMSE * HandsAnalyzed) / (HandDataPointCount + HandsAnalyzed);
			AdversaryHandXEE = (AdversaryHandXEE * HandDataPointCount + NextHandXEE * HandsAnalyzed) / (HandDataPointCount + HandsAnalyzed);
            AdversaryHandMLC = (AdversaryHandMLC * HandDataPointCount + NextHandMLC * HandsAnalyzed) / (HandDataPointCount + HandsAnalyzed);
            HandDataPointCount += HandsAnalyzed;
		}

		public override string ToString() {
			return string.Format("{0}: deck cards RMSE = {1:F3}, XEE = {2:F3} ({3} samples); adversary hands RMSE = {4:F3}, XEE = {5:F3}, MLC1/2/3 = {7:F3}/{8:F3}/{9:F3} (after {6} samples)", 
				ClassName, Mathf.Sqrt(DeckMSE), DeckXEE, DeckDataPointCount, Mathf.Sqrt(AdversaryHandMSE), AdversaryHandXEE, HandDataPointCount, AdversaryHandMLC.x, AdversaryHandMLC.y, AdversaryHandMLC.z);
		}
	}

	protected Dictionary<string, PerceptorStatistics> PerceptorStats;

	// Use this for initialization
	void Start () {
		// Initialize the stats hashmap
		PerceptorStats = new Dictionary<string, PerceptorStatistics>();
		PerceptorClassNames = new string[Perceptors.Length];
		for(int i = 0; i < Perceptors.Length; i++) {
            // Find the active perceptor if necessary
            if(Perceptors[i] == null || !Perceptors[i].isActiveAndEnabled) {
                Perceptors[i] = Players[i].GetFirstActivePerceptor();
            }
            // Add the perceptor's class name to the perceptor stats table
            PerceptorClassNames[i] = Perceptors[i].GetType().ToString();
            if(!PerceptorStats.ContainsKey(PerceptorClassNames[i])) {
                PerceptorStats.Add(PerceptorClassNames[i], new PerceptorStatistics(PerceptorClassNames[i]));
            }
        }
	}

	public void UpdateStatistics(float[] DeckDistribution, int DeckCardsLeft) {
		UpdatingStatistics = true;
		StartCoroutine(WaitAndUpdate(DeckDistribution, DeckCardsLeft));
	}

	protected IEnumerator WaitAndUpdate(float[] DeckDistribution, int DeckCardsLeft) {
		yield return new WaitForSeconds(0.5f);
		for(int i = 0; i < Players.Length; i++) {
			if(Perceptors[i] != null && !Players[i].KnockedOut) {
				// Wait until this perceptor has stopped analyzing the last turn
				yield return new WaitUntil(() => (Perceptors[i].READY));
				UpdateStatisticsForPlayer(i, DeckDistribution, DeckCardsLeft);
			}
		}
		UpdatingStatistics = false;
		DisplayStatistics();
	}

	protected void UpdateStatisticsForPlayer(int SittingOrder, float[] ActualDeckDistribution, int DeckCardsLeft) {
		AIGenericPerceptor Perceptor = Perceptors[SittingOrder];
		// Calculate the deck MSE
		float[] EstimatedDeckDistribution = Perceptor.GetCardProbabilitiesInDeck();
		float nextDeckMSE = AIUtil.GetMeanSquaredError(ActualDeckDistribution, EstimatedDeckDistribution);
		float nextDeckXEE = AIUtil.GetCrossEntropyError(ActualDeckDistribution, EstimatedDeckDistribution);
		// Calculate the card adversary distribution for other players
		float nextAdversaryHandMSE = 0, nextAdversaryHandXEE = 0;
        Vector3 nextAdversaryHandMLC = Vector3.zero;
		float[] ActualHandDistribution = new float[CardController.VALUE_PRINCESS + 1], EstimatedHandDistribution;
		int countHandsChecked = 0;
		for(int p = 0; p < Players.Length; p++) {
			if(p != SittingOrder && !Players[p].KnockedOut) {
				// Simulate the target player's hand distribution (perfect certainty)
				Array.Clear(ActualHandDistribution, 0, ActualHandDistribution.Length);
				ActualHandDistribution[Players[p].GetHand().Value] = 1f;
				// Get MSE for this distribution
				EstimatedHandDistribution = Perceptor.GetCardProbabilitiesInHand(Players[p]);
				nextAdversaryHandMSE += AIUtil.GetMeanSquaredError(ActualHandDistribution, EstimatedHandDistribution);
				nextAdversaryHandXEE += AIUtil.GetCrossEntropyError(ActualHandDistribution, EstimatedHandDistribution);
                nextAdversaryHandMLC += AIUtil.GetMostLikelyCardError(EstimatedHandDistribution, Players[p].GetHand().Value);
				countHandsChecked += 1;
			}
		}
		// Update the statistics for this perceptor class
		if(countHandsChecked > 0) {
			nextAdversaryHandMSE /= countHandsChecked;
            nextAdversaryHandMLC /= countHandsChecked;
            PerceptorStats[PerceptorClassNames[SittingOrder]].AppendStatistics(nextDeckMSE, nextDeckXEE, DeckCardsLeft, nextAdversaryHandMSE, nextAdversaryHandXEE, nextAdversaryHandMLC, countHandsChecked);
		}
	}

	public void DisplayStatistics() {
		string result = "";
		foreach(PerceptorStatistics PerceptorClass in PerceptorStats.Values) {
			result += (result.Length > 0 ? "\r\n" : "") + PerceptorClass.ToString();
		}
		Debug.Log(result);
	}
}
